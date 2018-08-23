/*
dotNetRDF is free and open source software licensed under the MIT License

-----------------------------------------------------------------------------

Copyright (c) 2009-2012 dotNetRDF Project (dotnetrdf-developer@lists.sf.net)

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is furnished
to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
#if !NO_WEB
using System.Web;
#endif
using VDS.RDF.Configuration;
using VDS.RDF.Parsing;
using VDS.RDF.Parsing.Handlers;
using VDS.RDF.Query;
using VDS.RDF.Writing;
using VDS.RDF.Writing.Formatting;

namespace VDS.RDF.Storage
{
    /// <summary>
    /// Class for connecting to repositories hosted on Dydra
    /// </summary>
    /// <remarks>
    /// <strong>Warning: </strong> This support is experimental and unstable, Dydra has exhibited many API consistencies, transient HTTP errors and other problems in our testing and we do not recommend that you use our support for it in production.
    /// </remarks>
    [Obsolete("The dotNetRDF team does not recommend usage of Dydra because their API frequently exhibits inconsistency, transient failures and other errors", false)]
    public class DydraConnector
        : BaseAsyncHttpConnector, IAsyncUpdateableStorage
#if !NO_SYNC_HTTP
        , IUpdateableStorage
#endif
    {
        private const String DydraBaseUri = "http://dydra.com/";
        private const String DydraApiKeyPassword = "X";
        private String _account, _repo, _apiKey, _username, _pwd;
        private String _baseUri;
        private bool _hasCredentials = false;
        private SparqlQueryParser _parser = new SparqlQueryParser();
        private SparqlFormatter _formatter = new SparqlFormatter();

        /// <summary>
        /// Creates a new connection to Dydra
        /// </summary>
        /// <param name="accountID">Account ID</param>
        /// <param name="repositoryID">Repository ID</param>
        public DydraConnector(String accountID, String repositoryID)
        {
            this._account = accountID;
            this._repo = repositoryID;
            this._baseUri = DydraBaseUri + accountID + "/" + repositoryID;
        }

        /// <summary>
        /// Creates a new connection to Dydra
        /// </summary>
        /// <param name="accountID">Account ID</param>
        /// <param name="repositoryID">Repository ID</param>
        /// <param name="apiKey">API Key</param>
        public DydraConnector(String accountID, String repositoryID, String apiKey)
            : this(accountID, repositoryID)
        {
            this._apiKey = apiKey;
            this._username = this._apiKey;
            this._pwd = DydraApiKeyPassword;
            this._hasCredentials = !String.IsNullOrEmpty(apiKey);
        }

#if !NO_PROXY

        /// <summary>
        /// Creates a new connection to Dydra
        /// </summary>
        /// <param name="accountID">Account ID</param>
        /// <param name="repositoryID">Repository ID</param>
        /// <param name="proxy">Proxy Server</param>
        public DydraConnector(String accountID, String repositoryID, WebProxy proxy)
            : this(accountID, repositoryID)
        {
            this.Proxy = proxy;
        }

        /// <summary>
        /// Creates a new connection to Dydra
        /// </summary>
        /// <param name="accountID">Account ID</param>
        /// <param name="repositoryID">Repository ID</param>
        /// <param name="apiKey">API Key</param>
        /// <param name="proxy">Proxy Server</param>
        public DydraConnector(String accountID, String repositoryID, String apiKey, WebProxy proxy)
            : this(accountID, repositoryID, apiKey)
        {
            this.Proxy = proxy;
        }

#endif

        //public DydraConnector(String accountID, String repositoryID, String username, String password)
        //    : this(accountID, repositoryID)
        //{
        //    this._username = username;
        //    this._pwd = password;
        //    this._hasCredentials = true;
        //}

        /// <summary>
        /// Gets the Account Name under which the repository is located
        /// </summary>
        [Description("The Account Name under which the repository is located.")]
        public String AccountName
        {
            get
            {
                return this._account;
            }
        }

        /// <summary>
        /// Gets the Repository Name
        /// </summary>
        [Description("The Dydra Repository to which this is a connection.")]
        public String RepositoryName
        {
            get
            {
                return this._repo;
            }
        }

        /// <summary>
        /// Gets the IO Behaviour of the Store
        /// </summary>
        public override IOBehaviour IOBehaviour
        {
            get
            {
                return IOBehaviour.GraphStore | IOBehaviour.CanUpdateTriples;
            }
        }

        /// <summary>
        /// Gets whether the Store is ready
        /// </summary>
        public override bool IsReady
        {
            get
            {
                return true;
            }
        }
        
        /// <summary>
        /// Returns false because Dydra stores are always read/write
        /// </summary>
        public override bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Returns true as listing graphs is supported by Dydra
        /// </summary>
        public override bool ListGraphsSupported
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Returns true as Triple Level updates are supported by Dydra
        /// </summary>
        public override bool UpdateSupported
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Gets that deleting Graphs is not supported
        /// </summary>
        public override bool DeleteSupported
        {
            get
            {
                return true;
            }
        }

#if !NO_SYNC_HTTP

        /// <summary>
        /// Saves a Graph to the Store
        /// </summary>
        /// <param name="g">Graph to save</param>
        public void SaveGraph(IGraph g)
        {
            try
            {
                HttpWebRequest request;
                Dictionary<String, String> requestParams = new Dictionary<string, string>();
                if (g.BaseUri != null)
                {
                    requestParams.Add("context", g.BaseUri.AbsoluteUri);
                    request = this.CreateRequest("/statements", MimeTypesHelper.Any, "PUT", requestParams);
                }
                else
                {
                    request = this.CreateRequest("/statements", MimeTypesHelper.Any, "POST", requestParams);
                }

                IRdfWriter rdfWriter = new RdfXmlWriter();
                request.ContentType = MimeTypesHelper.RdfXml[0];
                using (StreamWriter writer = new StreamWriter(request.GetRequestStream()))
                {
                    rdfWriter.Save(g, writer);
                    writer.Close();
                }

                Tools.HttpDebugRequest(request);

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    Tools.HttpDebugResponse(response);
                    //If we get here then operation completed OK
                    response.Close();
                }
            }
            catch (WebException webEx)
            {
                throw StorageHelper.HandleHttpError(webEx, "save a Graph to");
            }
        }

        /// <summary>
        /// Loads a Graph from the Store
        /// </summary>
        /// <param name="g">Graph to load into</param>
        /// <param name="graphUri">URI of the graph to load</param>
        public void LoadGraph(IGraph g, Uri graphUri)
        {
            this.LoadGraph(new GraphHandler(g), graphUri);
        }

        /// <summary>
        /// Loads a Graph from the Store
        /// </summary>
        /// <param name="g">Graph to load into</param>
        /// <param name="graphUri">URI of the graph to load</param>
        public void LoadGraph(IGraph g, String graphUri)
        {
            this.LoadGraph(new GraphHandler(g), graphUri);
        }

        /// <summary>
        /// Loads a Graph from the Store
        /// </summary>
        /// <param name="handler">RDF Handler</param>
        /// <param name="graphUri">URI of the graph to load</param>
        public void LoadGraph(IRdfHandler handler, Uri graphUri)
        {
            this.LoadGraph(handler, graphUri.ToSafeString());
        }

        /// <summary>
        /// Loads a Graph from the Store
        /// </summary>
        /// <param name="handler">RDF Handler</param>
        /// <param name="graphUri">URI of the graph to load</param>
        public void LoadGraph(IRdfHandler handler, String graphUri)
        {
            try
            {
                Dictionary<String, String> requestParams = new Dictionary<string, string>();
                if (graphUri != null && !graphUri.Equals(String.Empty))
                {
                    requestParams.Add("context", "<" + graphUri + ">");
                }

                HttpWebRequest request = this.CreateRequest("/statements", MimeTypesHelper.HttpAcceptHeader, "GET", requestParams);
                Tools.HttpDebugRequest(request);

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    Tools.HttpDebugResponse(response);

                    //If we get here try and parse the response
                    IRdfReader parser = MimeTypesHelper.GetParser(response.ContentType);
                    parser.Load(handler, new StreamReader(response.GetResponseStream()));

                    response.Close();
                }
            }
            catch (WebException webEx)
            {
                throw StorageHelper.HandleHttpError(webEx, "load a Graph from");
            }
        }

        /// <summary>
        /// Lists the Graphs from the Repository
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Uri> ListGraphs()
        {
            try
            {
                //Use the /contexts method to get the Graph URIs
                //HACK: Have to use SPARQL JSON as currently Dydra's SPARQL XML Results are malformed
                HttpWebRequest request = this.CreateRequest("/contexts", MimeTypesHelper.CustomHttpAcceptHeader(MimeTypesHelper.SparqlResultsJson), "GET", new Dictionary<string, string>());
                SparqlResultSet results = new SparqlResultSet();
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    ISparqlResultsReader parser = MimeTypesHelper.GetSparqlParser(response.ContentType);
                    parser.Load(results, new StreamReader(response.GetResponseStream()));
                    response.Close();
                }

                List<Uri> graphUris = new List<Uri>();
                foreach (SparqlResult r in results)
                {
                    if (r.HasValue("contextID"))
                    {
                        INode value = r["contextID"];
                        if (value.NodeType == NodeType.Uri)
                        {
                            graphUris.Add(((IUriNode)value).Uri);
                        }
                        else if (value.NodeType == NodeType.Blank)
                        {
                            //Dydra allows BNode Graph URIs
                            graphUris.Add(UriFactory.Create("dydra:bnode:" + ((IBlankNode)value).InternalID));
                        }
                    }
                }
                return graphUris;
            }
            catch (Exception ex)
            {
                throw StorageHelper.HandleError(ex, "list Graphs from");
            }
        }

        /// <summary>
        /// Updates an existing Graph in the Store by adding and removing triples
        /// </summary>
        /// <param name="graphUri">URI of the graph to update</param>
        /// <param name="additions">Triples to be added</param>
        /// <param name="removals">Triples to be removed</param>
        /// <remarks>
        /// Removals are processed before any additions, to force a specific order of additions and removals you should make multiple calls to this function specifying each set of additions or removals you wish to perform seperately
        /// </remarks>
        public void UpdateGraph(Uri graphUri, IEnumerable<Triple> additions, IEnumerable<Triple> removals)
        {
            this.UpdateGraph(graphUri.ToSafeString(), additions, removals);
        }

        /// <summary>
        /// Updates an existing Graph in the Store by adding and removing triples
        /// </summary>
        /// <param name="graphUri">URI of the graph to update</param>
        /// <param name="additions">Triples to be added</param>
        /// <param name="removals">Triples to be removed</param>
        /// <remarks>
        /// Removals are processed before any additions, to force a specific order of additions and removals you should make multiple calls to this function specifying each set of additions or removals you wish to perform seperately
        /// </remarks>
        public void UpdateGraph(String graphUri, IEnumerable<Triple> additions, IEnumerable<Triple> removals)
        {
            try
            {
                StringBuilder sparqlUpdate = new StringBuilder();

                //Build the SPARQL Update Commands
                if (removals != null && removals.Any())
                {
                    sparqlUpdate.AppendLine("DELETE DATA");
                    sparqlUpdate.AppendLine("{");
                    if (graphUri != null && !graphUri.Equals(String.Empty))
                    {
                        sparqlUpdate.AppendLine("GRAPH <" + this._formatter.FormatUri(graphUri) + "> {");
                    }
                    foreach (Triple t in removals)
                    {
                        sparqlUpdate.AppendLine(t.ToString(this._formatter));
                    }
                    if (graphUri != null && !graphUri.Equals(String.Empty))
                    {
                        sparqlUpdate.AppendLine("}");
                    }
                    sparqlUpdate.AppendLine("}");
                }
                if (additions != null && additions.Any())
                {
                    sparqlUpdate.AppendLine("INSERT DATA");
                    sparqlUpdate.AppendLine("{");
                    if (graphUri != null && !graphUri.Equals(String.Empty))
                    {
                        sparqlUpdate.AppendLine("GRAPH <" + this._formatter.FormatUri(graphUri) + "> {");
                    }
                    foreach (Triple t in additions)
                    {
                        sparqlUpdate.AppendLine(t.ToString(this._formatter));
                    }
                    if (graphUri != null && !graphUri.Equals(String.Empty))
                    {
                        sparqlUpdate.AppendLine("}");
                    }
                    sparqlUpdate.AppendLine("}");
                }

                //Send them to Dydra for processing
                if (sparqlUpdate.Length > 0)
                {
                    this.Update(sparqlUpdate.ToString());
                }
            }
            catch (WebException webEx)
            {
                throw StorageHelper.HandleHttpError(webEx, "updating a Graph in");
            }
        }

        /// <summary>
        /// Deletes a Graph from the Store
        /// </summary>
        /// <param name="graphUri">URI of the Graph to delete</param>
        public void DeleteGraph(string graphUri)
        {
            if (graphUri != null && !graphUri.Equals(String.Empty))
            {
                this.Update("DROP GRAPH <" + this._formatter.FormatUri(graphUri) + ">");
            }
            else
            {
                this.Update("DROP DEFAULT");
            }
        }

        /// <summary>
        /// Deletes a Graph from the Store
        /// </summary>
        /// <param name="graphUri">URI of the Graph to delete</param>
        public void DeleteGraph(Uri graphUri)
        {
            this.DeleteGraph(graphUri.ToSafeString());
        }

        /// <summary>
        /// Performs a SPARQL Query against the underlying Store
        /// </summary>
        /// <param name="rdfHandler">RDF Handler</param>
        /// <param name="resultsHandler">SPARQL Results Handler</param>
        /// <param name="sparqlQuery">SPARQL Query</param>
        /// <returns></returns>
        public void Query(IRdfHandler rdfHandler, ISparqlResultsHandler resultsHandler, string sparqlQuery)
        {
            try
            {
                //First off parse the Query to see what kind of query it is
                SparqlQuery q;
                try
                {
                    q = this._parser.ParseFromString(sparqlQuery);
                }
                catch (RdfParseException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw new RdfStorageException("An unexpected error occurred while trying to parse the SPARQL Query prior to sending it to the Store, see inner exception for details", ex);
                }

                //Now select the Accept Header based on the query type
                String accept = (SparqlSpecsHelper.IsSelectQuery(q.QueryType) || q.QueryType == SparqlQueryType.Ask) ? MimeTypesHelper.HttpSparqlAcceptHeader : MimeTypesHelper.HttpAcceptHeader;

                //Create the Request
                HttpWebRequest request;
                Dictionary<String, String> queryParams = new Dictionary<string, string>();
                if (sparqlQuery.Length < 2048)
                {
                    queryParams.Add("query", sparqlQuery);

                    request = this.CreateRequest("/sparql", accept, "GET", queryParams);
                }
                else
                {
                    request = this.CreateRequest("/sparql", accept, "POST", queryParams);

                    //Build the Post Data and add to the Request Body
                    request.ContentType = MimeTypesHelper.WWWFormURLEncoded;
                    StringBuilder postData = new StringBuilder();
                    postData.Append("query=");
                    postData.Append(HttpUtility.UrlEncode(sparqlQuery));
                    StreamWriter writer = new StreamWriter(request.GetRequestStream());
                    writer.Write(postData);
                    writer.Close();
                }

                Tools.HttpDebugRequest(request);

                //Get the Response and process based on the Content Type
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    Tools.HttpDebugResponse(response);
                    StreamReader data = new StreamReader(response.GetResponseStream());
                    String ctype = response.ContentType;
                    if (SparqlSpecsHelper.IsSelectQuery(q.QueryType) || q.QueryType == SparqlQueryType.Ask)
                    {
                        //ASK/SELECT should return SPARQL Results
                        ISparqlResultsReader resreader = MimeTypesHelper.GetSparqlParser(ctype, q.QueryType == SparqlQueryType.Ask);
                        resreader.Load(resultsHandler, data);
                        response.Close();
                    }
                    else
                    {
                        //CONSTRUCT/DESCRIBE should return a Graph
                        IRdfReader rdfreader = MimeTypesHelper.GetParser(ctype);
                        rdfreader.Load(rdfHandler, data);
                        response.Close();
                    }
                }
            }
            catch (WebException webEx)
            {
                throw StorageHelper.HandleHttpQueryError(webEx);
            }
        }

        /// <summary>
        /// Performs a SPARQL Query against the underlying Store
        /// </summary>
        /// <param name="sparqlQuery">SPARQL Query</param>
        /// <returns></returns>
        public object Query(string sparqlQuery)
        {
            Graph g = new Graph();
            SparqlResultSet results = new SparqlResultSet();
            this.Query(new GraphHandler(g), new ResultSetHandler(results), sparqlQuery);

            if (results.ResultsType != SparqlResultsType.Unknown)
            {
                return results;
            }
            else
            {
                return g;
            }
        }

        /// <summary>
        /// Performs a SPARQL Update request on the Store
        /// </summary>
        /// <param name="sparqlUpdates">SPARQL Updates</param>
        public void Update(String sparqlUpdates)
        {
            try
            {
                HttpWebRequest request = this.CreateRequest("/sparql", MimeTypesHelper.HttpSparqlAcceptHeader, "POST", null);

                using (StreamWriter writer = new StreamWriter(request.GetRequestStream()))
                {
                    writer.Write("query=");
                    writer.Write(HttpUtility.UrlEncode(sparqlUpdates));
                    writer.Close();
                }
                Tools.HttpDebugRequest(request);

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    Tools.HttpDebugResponse(response);
                    //If we get here then it completed OK
                    response.Close();
                }
            }
            catch (WebException webEx)
            {
                throw StorageHelper.HandleHttpError(webEx, "updating");
            }
        }

#endif

        /// <summary>
        /// Saves a Graph to the Store
        /// </summary>
        /// <param name="g">Graph to save</param>
        /// <param name="callback">Callback</param>
        /// <param name="state">State to pass to the callback</param>
        public override void SaveGraph(IGraph g, AsyncStorageCallback callback, Object state)
        {
            HttpWebRequest request;
            Dictionary<String, String> requestParams = new Dictionary<string, string>();
            if (g.BaseUri != null)
            {
                requestParams.Add("context", g.BaseUri.AbsoluteUri);
                request = this.CreateRequest("/statements", MimeTypesHelper.Any, "PUT", requestParams);
            }
            else
            {
                request = this.CreateRequest("/statements", MimeTypesHelper.Any, "POST", requestParams);
            }

            IRdfWriter rdfWriter = new RdfXmlWriter();
            request.ContentType = MimeTypesHelper.RdfXml[0];

            this.SaveGraphAsync(request, rdfWriter, g, callback, state);
        }

        /// <summary>
        /// Loads a Graph from the Store
        /// </summary>
        /// <param name="handler">RDF Handler</param>
        /// <param name="graphUri">URI of the graph to load</param>
        /// <param name="callback">Callback</param>
        /// <param name="state">State to pass to the callback</param>
        public override void LoadGraph(IRdfHandler handler, String graphUri, AsyncStorageCallback callback, Object state)
        {
            Dictionary<String, String> requestParams = new Dictionary<string, string>();
            if (graphUri != null && !graphUri.Equals(String.Empty))
            {
                requestParams.Add("context", "<" + graphUri + ">");
            }

            HttpWebRequest request = this.CreateRequest("/statements", MimeTypesHelper.HttpAcceptHeader, "GET", requestParams);
            this.LoadGraphAsync(request, handler, callback, state);
        }

        /// <summary>
        /// Updates an existing Graph in the Store by adding and removing triples
        /// </summary>
        /// <param name="graphUri">URI of the graph to update</param>
        /// <param name="additions">Triples to be added</param>
        /// <param name="removals">Triples to be removed</param>
        /// <param name="callback">Callback</param>
        /// <param name="state">State to pass to the callback</param>
        /// <remarks>
        /// Removals are processed before any additions, to force a specific order of additions and removals you should make multiple calls to this function specifying each set of additions or removals you wish to perform seperately
        /// </remarks>
        public override void UpdateGraph(String graphUri, IEnumerable<Triple> additions, IEnumerable<Triple> removals, AsyncStorageCallback callback, Object state)
        {
            StringBuilder sparqlUpdate = new StringBuilder();

            //Build the SPARQL Update Commands
            if (removals != null && removals.Any())
            {
                sparqlUpdate.AppendLine("DELETE DATA");
                sparqlUpdate.AppendLine("{");
                if (graphUri != null && !graphUri.Equals(String.Empty))
                {
                    sparqlUpdate.AppendLine("GRAPH <" + this._formatter.FormatUri(graphUri) + "> {");
                }
                foreach (Triple t in removals)
                {
                    sparqlUpdate.AppendLine(t.ToString(this._formatter));
                }
                if (graphUri != null && !graphUri.Equals(String.Empty))
                {
                    sparqlUpdate.AppendLine("}");
                }
                sparqlUpdate.AppendLine("}");
            }
            if (additions != null && additions.Any())
            {
                sparqlUpdate.AppendLine("INSERT DATA");
                sparqlUpdate.AppendLine("{");
                if (graphUri != null && !graphUri.Equals(String.Empty))
                {
                    sparqlUpdate.AppendLine("GRAPH <" + this._formatter.FormatUri(graphUri) + "> {");
                }
                foreach (Triple t in additions)
                {
                    sparqlUpdate.AppendLine(t.ToString(this._formatter));
                }
                if (graphUri != null && !graphUri.Equals(String.Empty))
                {
                    sparqlUpdate.AppendLine("}");
                }
                sparqlUpdate.AppendLine("}");
            }

            //Send them to Dydra for processing
            if (sparqlUpdate.Length > 0)
            {
                this.Update(sparqlUpdate.ToString(), (sender, args, st) =>
                    {
                        callback(this, new AsyncStorageCallbackArgs(AsyncStorageOperation.UpdateGraph, graphUri.ToSafeUri(), args.Error), state);
                    }, state);
            }
        }

        /// <summary>
        /// Deletes a Graph from the Store
        /// </summary>
        /// <param name="graphUri">URI of the Graph to delete</param>
        /// <param name="callback">Callback</param>
        /// <param name="state">State to pass to the callback</param>
        public override void DeleteGraph(string graphUri, AsyncStorageCallback callback, Object state)
        {
            if (graphUri != null && !graphUri.Equals(String.Empty))
            {
                this.Update("DROP GRAPH <" + this._formatter.FormatUri(graphUri) + ">", (sender, args, st) =>
                    {
                        callback(this, new AsyncStorageCallbackArgs(AsyncStorageOperation.DeleteGraph, graphUri.ToSafeUri(), args.Error), state);
                    }, state);
            }
            else
            {
                this.Update("DROP DEFAULT", (sender, args, st) =>
                    {
                        callback(this, new AsyncStorageCallbackArgs(AsyncStorageOperation.DeleteGraph, graphUri.ToSafeUri(), args.Error), state);
                    }, state);
            }
        }

        /// <summary>
        /// Lists the Graphs from the Repository
        /// </summary>
        /// <param name="callback">Callback</param>
        /// <param name="state">State to pass to the callback</param>
        /// <returns></returns>
        public override void ListGraphs(AsyncStorageCallback callback, Object state)
        {
            try
            {
                //Use the /contexts method to get the Graph URIs
                //HACK: Have to use SPARQL JSON as currently Dydra's SPARQL XML Results are malformed
                HttpWebRequest request = this.CreateRequest("/contexts", MimeTypesHelper.CustomHttpAcceptHeader(MimeTypesHelper.SparqlResultsJson), "GET", new Dictionary<string, string>());
                request.BeginGetResponse(r =>
                    {
                        try
                        {
                            HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(r);
                            ISparqlResultsReader parser = MimeTypesHelper.GetSparqlParser(response.ContentType);
                            ListUrisHandler handler = new ListUrisHandler("contextID");
                            parser.Load(handler, new StreamReader(response.GetResponseStream()));
                            response.Close();

                            callback(this, new AsyncStorageCallbackArgs(AsyncStorageOperation.ListGraphs, handler.Uris), state);
                        }
                        catch (WebException webEx)
                        {
                            callback(this, new AsyncStorageCallbackArgs(AsyncStorageOperation.ListGraphs, StorageHelper.HandleHttpError(webEx, "list Graphs asynchronously from")), state);
                        }
                        catch (Exception ex)
                        {
                            callback(this, new AsyncStorageCallbackArgs(AsyncStorageOperation.ListGraphs, StorageHelper.HandleError(ex, "list Graphs asynchronously from")), state);
                        }
                    }, state);
            }
            catch (WebException webEx)
            {
                callback(this, new AsyncStorageCallbackArgs(AsyncStorageOperation.ListGraphs, StorageHelper.HandleHttpError(webEx, "list Graphs asynchronously from")), state);
            }
            catch (Exception ex)
            {
                callback(this, new AsyncStorageCallbackArgs(AsyncStorageOperation.ListGraphs, StorageHelper.HandleError(ex, "list Graphs asynchronously from")), state);
            }
        }

        /// <summary>
        /// Queries the store asynchronously
        /// </summary>
        /// <param name="sparqlQuery">SPARQL Query</param>
        /// <param name="callback">Callback</param>
        /// <param name="state">State to pass to the callback</param>
        public void Query(string sparqlQuery, AsyncStorageCallback callback, object state)
        {
            Graph g = new Graph();
            SparqlResultSet results = new SparqlResultSet();
            this.Query(new GraphHandler(g), new ResultSetHandler(results), sparqlQuery, (sender, args, st) =>
                {
                    if (results.ResultsType != SparqlResultsType.Unknown)
                    {
                        callback(this, new AsyncStorageCallbackArgs(AsyncStorageOperation.SparqlQuery, sparqlQuery, results, args.Error), state);
                    }
                    else
                    {
                        callback(this, new AsyncStorageCallbackArgs(AsyncStorageOperation.SparqlQuery, sparqlQuery, g, args.Error), state);
                    }
                }, state);
        }

        /// <summary>
        /// Queries the store asynchronously
        /// </summary>
        /// <param name="sparqlQuery">SPARQL Query</param>
        /// <param name="rdfHandler">RDF Handler</param>
        /// <param name="resultsHandler">Results Handler</param>
        /// <param name="callback">Callback</param>
        /// <param name="state">State to pass to the callback</param>
        public void Query(IRdfHandler rdfHandler, ISparqlResultsHandler resultsHandler, string sparqlQuery, AsyncStorageCallback callback, object state)
        {
            try
            {
                //First off parse the Query to see what kind of query it is
                SparqlQuery q;
                try
                {
                    q = this._parser.ParseFromString(sparqlQuery);
                }
                catch (RdfParseException parseEx)
                {
                    callback(this, new AsyncStorageCallbackArgs(AsyncStorageOperation.SparqlQueryWithHandler, parseEx), state);
                    return;
                }
                catch (Exception ex)
                {
                    callback(this, new AsyncStorageCallbackArgs(AsyncStorageOperation.SparqlQueryWithHandler, new RdfStorageException("An unexpected error occurred while trying to parse the SPARQL Query prior to sending it to the Store, see inner exception for details", ex)), state);
                    return;
                }

                //Now select the Accept Header based on the query type
                String accept = (SparqlSpecsHelper.IsSelectQuery(q.QueryType) || q.QueryType == SparqlQueryType.Ask) ? MimeTypesHelper.HttpSparqlAcceptHeader : MimeTypesHelper.HttpAcceptHeader;

                //Create the Request, for simplicity async requests are always POST
                HttpWebRequest request;
                Dictionary<String, String> queryParams = new Dictionary<string, string>();
                request = this.CreateRequest("/sparql", accept, "POST", queryParams);
                request.ContentType = MimeTypesHelper.WWWFormURLEncoded;

                Tools.HttpDebugRequest(request);

                request.BeginGetRequestStream(r =>
                    {
                        try
                        {
                            Stream stream = request.EndGetRequestStream(r);
                            using (StreamWriter writer = new StreamWriter(stream))
                            {
                                writer.Write("query=");
                                writer.Write(HttpUtility.UrlEncode(sparqlQuery));
                                writer.Close();
                            }

                            request.BeginGetResponse(r2 =>
                                {
                                    //Get the Response and process based on the Content Type
                                    try
                                    {
                                        HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(r2);
                                        Tools.HttpDebugResponse(response);
                                        StreamReader data = new StreamReader(response.GetResponseStream());
                                        String ctype = response.ContentType;
                                        if (SparqlSpecsHelper.IsSelectQuery(q.QueryType) || q.QueryType == SparqlQueryType.Ask)
                                        {
                                            //ASK/SELECT should return SPARQL Results
                                            ISparqlResultsReader resreader = MimeTypesHelper.GetSparqlParser(ctype, q.QueryType == SparqlQueryType.Ask);
                                            resreader.Load(resultsHandler, data);
                                            response.Close();
                                        }
                                        else
                                        {
                                            //CONSTRUCT/DESCRIBE should return a Graph
                                            IRdfReader rdfreader = MimeTypesHelper.GetParser(ctype);
                                            rdfreader.Load(rdfHandler, data);
                                            response.Close();
                                        }
                                    }
                                    catch (WebException webEx)
                                    {
                                        callback(this, new AsyncStorageCallbackArgs(AsyncStorageOperation.SparqlQueryWithHandler, StorageHelper.HandleHttpQueryError(webEx)), state);
                                    }
                                    catch (Exception ex)
                                    {
                                        callback(this, new AsyncStorageCallbackArgs(AsyncStorageOperation.SparqlQueryWithHandler, StorageHelper.HandleQueryError(ex)), state);
                                    }
                                }, state);
                        }
                        catch (WebException webEx)
                        {
                            callback(this, new AsyncStorageCallbackArgs(AsyncStorageOperation.SparqlQueryWithHandler, StorageHelper.HandleHttpQueryError(webEx)), state);
                        }
                        catch (Exception ex)
                        {
                            callback(this, new AsyncStorageCallbackArgs(AsyncStorageOperation.SparqlQueryWithHandler, StorageHelper.HandleQueryError(ex)), state);
                        }
                    }, state);               
            }
            catch (WebException webEx)
            {
                callback(this, new AsyncStorageCallbackArgs(AsyncStorageOperation.SparqlQueryWithHandler, StorageHelper.HandleHttpQueryError(webEx)), state);
            }
            catch (Exception ex)
            {
                callback(this, new AsyncStorageCallbackArgs(AsyncStorageOperation.SparqlQueryWithHandler, StorageHelper.HandleQueryError(ex)), state);
            }
        }

        /// <summary>
        /// Updates the store asynchronously
        /// </summary>
        /// <param name="sparqlUpdates">SPARQL Update</param>
        /// <param name="callback">Callback</param>
        /// <param name="state">State to pass to the callback</param>
        public void Update(string sparqlUpdates, AsyncStorageCallback callback, object state)
        {
            try
            {
                HttpWebRequest request = this.CreateRequest("/sparql", MimeTypesHelper.HttpSparqlAcceptHeader, "POST", null);
                request.BeginGetRequestStream(r =>
                    {
                        try
                        {
                            Stream stream = request.EndGetRequestStream(r);
                            using (StreamWriter writer = new StreamWriter(stream))
                            {
                                writer.Write("query=");
                                writer.Write(HttpUtility.UrlEncode(sparqlUpdates));
                                writer.Close();
                            }
                            Tools.HttpDebugRequest(request);

                            request.BeginGetResponse(r2 =>
                                {
                                    try
                                    {
                                        HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(r2);
                                        Tools.HttpDebugResponse(response);
                                        //If we get here then it completed OK
                                        response.Close();
                                    }
                                    catch (WebException webEx)
                                    {
                                        callback(this, new AsyncStorageCallbackArgs(AsyncStorageOperation.SparqlQueryWithHandler, StorageHelper.HandleHttpError(webEx, "updating")), state);
                                    }
                                    catch (Exception ex)
                                    {
                                        callback(this, new AsyncStorageCallbackArgs(AsyncStorageOperation.SparqlQueryWithHandler, StorageHelper.HandleError(ex, "updating")), state);
                                    }
                                }, state);
                        }
                        catch (WebException webEx)
                        {
                            callback(this, new AsyncStorageCallbackArgs(AsyncStorageOperation.SparqlQueryWithHandler, StorageHelper.HandleHttpError(webEx, "updating")), state);
                        }
                        catch (Exception ex)
                        {
                            callback(this, new AsyncStorageCallbackArgs(AsyncStorageOperation.SparqlQueryWithHandler, StorageHelper.HandleError(ex, "updating")), state);
                        }
                    }, state);
            }
            catch (WebException webEx)
            {
                callback(this, new AsyncStorageCallbackArgs(AsyncStorageOperation.SparqlQueryWithHandler, StorageHelper.HandleHttpError(webEx, "updating")), state);
            }
            catch (Exception ex)
            {
                callback(this, new AsyncStorageCallbackArgs(AsyncStorageOperation.SparqlQueryWithHandler, StorageHelper.HandleError(ex, "updating")), state);
            }
        }

        /// <summary>
        /// Helper method for creating HTTP Requests to the Store
        /// </summary>
        /// <param name="servicePath">Path to the Service requested</param>
        /// <param name="accept">Acceptable Content Types</param>
        /// <param name="method">HTTP Method</param>
        /// <param name="requestParams">Querystring Parameters</param>
        /// <returns></returns>
        protected HttpWebRequest CreateRequest(String servicePath, String accept, String method, Dictionary<String, String> requestParams)
        {
            //Modify the Accept header appropriately to remove any mention of HTML
            //HACK: Have to do this otherwise Dydra won't HTTP authenticate nicely
            if (accept.Contains("application/xhtml+xml"))
            {
                accept = accept.Replace("application/xhtml+xml,", String.Empty);
                if (accept.Contains(",,")) accept = accept.Replace(",,", ",");
            }
            if (accept.Contains("text/html"))
            {
                accept = accept.Replace("text/html", String.Empty);
                if (accept.Contains(",,")) accept = accept.Replace(",,", ",");
            }
            if (accept.Contains(",;")) accept = accept.Replace(",;", ",");

            //HACK: If the Accept header is */* switch it for application/rdf+xml to make Dydra HTTP authenticate nicely
            if (accept.Equals(MimeTypesHelper.Any)) accept = MimeTypesHelper.RdfXml[0];

            //HACK: If the Accept header contains */* strip that part of the header
            if (accept.Contains("*/*")) accept = accept.Substring(0, accept.IndexOf("*/*"));

            if (accept.EndsWith(",")) accept = accept.Substring(0, accept.Length - 1);

            //Build the Request Uri
            //String requestUri = this._baseUri + servicePath;
            String requestUri = this.GetCredentialedUri() + servicePath;
            //if (this._apiKey != null)
            //{
            //    requestUri += "?auth_token=" + Uri.EscapeDataString(this._apiKey);
            //}
            if (requestParams != null)
            {
                if (requestParams.Count > 0)
                {
                    if (requestUri.Contains("?"))
                    {
                        if (!requestUri.EndsWith("&")) requestUri += "&";
                    }
                    else
                    {
                        requestUri += "?";
                    }
                    foreach (String p in requestParams.Keys)
                    {
                        requestUri += p + "=" + HttpUtility.UrlEncode(requestParams[p]) + "&";
                    }
                    requestUri = requestUri.Substring(0, requestUri.Length - 1);
                }
            }

            //Create our Request
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(requestUri);
            request.Accept = accept;
            request.Method = method;

            //Add Credentials if needed
            if (this._hasCredentials)
            {
                if (Options.ForceHttpBasicAuth)
                {
                    //Forcibly include a HTTP basic authentication header
#if !SILVERLIGHT
                    string credentials = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(this._username + ":" + this._pwd));
                    request.Headers.Add("Authorization", "Basic " + credentials);
#else
                    string credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(this._username + ":" + this._pwd));
                    request.Headers["Authorization"] = "Basic " + credentials;
#endif
                }
                else
                {
                    //Leave .Net to cope with HTTP auth challenge response
                    NetworkCredential credentials = new NetworkCredential(this._username, this._pwd);
                    request.Credentials = credentials;
#if !SILVERLIGHT
                    request.PreAuthenticate = true;
#endif
                }
            }

            return base.GetProxiedRequest(request);
        }

        private String GetCredentialedUri()
        {
            if (this._hasCredentials)
            {
                if (this._apiKey != null)
                {
                    return this._baseUri.Substring(0, 7) + Uri.EscapeUriString(this._apiKey) + "@" + this._baseUri.Substring(7);
                }
                else
                {
                    return this._baseUri.Substring(0, 7) + Uri.EscapeUriString(this._username) + ":" + Uri.EscapeUriString(this._pwd) + "@" + this._baseUri.Substring(7);
                }
            }
            else
            {
                return this._baseUri;
            }
        }

        /// <summary>
        /// Serializes the connection's configuration
        /// </summary>
        /// <param name="context">Configuration Serialization Context</param>
        public void SerializeConfiguration(ConfigurationSerializationContext context)
        {
            INode manager = context.NextSubject;
            INode rdfType = context.Graph.CreateUriNode(UriFactory.Create(RdfSpecsHelper.RdfType));
            INode rdfsLabel = context.Graph.CreateUriNode(UriFactory.Create(NamespaceMapper.RDFS + "label"));
            INode dnrType = context.Graph.CreateUriNode(UriFactory.Create(ConfigurationLoader.PropertyType));
            INode genericManager = context.Graph.CreateUriNode(UriFactory.Create(ConfigurationLoader.ClassStorageProvider));
            INode catalog = context.Graph.CreateUriNode(UriFactory.Create(ConfigurationLoader.PropertyCatalog));
            INode store = context.Graph.CreateUriNode(UriFactory.Create(ConfigurationLoader.PropertyStore));

            context.Graph.Assert(new Triple(manager, rdfType, genericManager));
            context.Graph.Assert(new Triple(manager, rdfsLabel, context.Graph.CreateLiteralNode(this.ToString())));
            context.Graph.Assert(new Triple(manager, dnrType, context.Graph.CreateLiteralNode(this.GetType().FullName)));
            context.Graph.Assert(new Triple(manager, catalog, context.Graph.CreateLiteralNode(this._account)));
            context.Graph.Assert(new Triple(manager, store, context.Graph.CreateLiteralNode(this._repo)));

            if (this._apiKey != null || (this._username != null && this._pwd != null))
            {
                INode username = context.Graph.CreateUriNode(UriFactory.Create(ConfigurationLoader.PropertyUser));
                INode pwd = context.Graph.CreateUriNode(UriFactory.Create(ConfigurationLoader.PropertyPassword));
                if (this._apiKey != null)
                {
                    context.Graph.Assert(new Triple(manager, username, context.Graph.CreateLiteralNode(this._apiKey)));
                }
                else
                {
                    context.Graph.Assert(new Triple(manager, username, context.Graph.CreateLiteralNode(this._username)));
                    context.Graph.Assert(new Triple(manager, pwd, context.Graph.CreateLiteralNode(this._pwd)));
                }
            }

            base.SerializeProxyConfig(manager, context);
        }

        /// <summary>
        /// Disposes of the connection
        /// </summary>
        public override void Dispose()
        {
            //No Dispose actions needed
        }

        /// <summary>
        /// Gets a String representation of the Connection
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "[Dydra] Repository '" + this._repo + "' on Account '" + this._account + "'";
        }
    }
}
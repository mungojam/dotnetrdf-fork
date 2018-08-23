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
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Threading;
#if !NO_WEB
using System.Web;
#endif
using VDS.RDF.Configuration;
using VDS.RDF.Parsing;
using VDS.RDF.Parsing.Handlers;

namespace VDS.RDF.Query
{
    /// <summary>
    /// A Class for connecting to a remote SPARQL Endpoint and executing Queries against it
    /// </summary>
    public class SparqlRemoteEndpoint 
        : BaseEndpoint, IConfigurationSerializable
    {
        private List<String> _defaultGraphUris = new List<string>();
        private List<String> _namedGraphUris = new List<string>();
        private String _resultsAccept, _rdfAccept;

        const int LongQueryLength = 2048;

        #region Constructors

        /// <summary>
        /// Empty Constructor for use by derived classes
        /// </summary>
        protected SparqlRemoteEndpoint()
        {

        }

        /// <summary>
        /// Creates a new SPARQL Endpoint for the given Endpoint URI
        /// </summary>
        /// <param name="endpointUri">Remote Endpoint URI</param>
        public SparqlRemoteEndpoint(Uri endpointUri)
            : base(endpointUri) { }

        /// <summary>
        /// Creates a new SPARQL Endpoint for the given Endpoint URI using the given Default Graph Uri
        /// </summary>
        /// <param name="endpointUri">Remote Endpoint URI</param>
        /// <param name="defaultGraphUri">Default Graph URI to use when Querying the Endpoint</param>
        public SparqlRemoteEndpoint(Uri endpointUri, String defaultGraphUri)
            : this(endpointUri)
        {
            this._defaultGraphUris.Add(defaultGraphUri);
        }

        /// <summary>
        /// Creates a new SPARQL Endpoint for the given Endpoint Uri using the given Default Graph Uri
        /// </summary>
        /// <param name="endpointUri">Remote Endpoint URI</param>
        /// <param name="defaultGraphUri">Default Graph URI to use when Querying the Endpoint</param>
        public SparqlRemoteEndpoint(Uri endpointUri, Uri defaultGraphUri)
            : this(endpointUri)
        {
            if (defaultGraphUri != null)
            {
                this._defaultGraphUris.Add(defaultGraphUri.AbsoluteUri);
            }
        }

        /// <summary>
        /// Creates a new SPARQL Endpoint for the given Endpoint URI using the given Default Graph URI
        /// </summary>
        /// <param name="endpointUri">Remote Endpoint URI</param>
        /// <param name="defaultGraphUri">Default Graph URI to use when Querying the Endpoint</param>
        /// <param name="namedGraphUris">Named Graph URIs to use when Querying the Endpoint</param>
        public SparqlRemoteEndpoint(Uri endpointUri, String defaultGraphUri, IEnumerable<String> namedGraphUris)
            : this(endpointUri, defaultGraphUri)
        {
            this._namedGraphUris.AddRange(namedGraphUris);
        }

        /// <summary>
        /// Creates a new SPARQL Endpoint for the given Endpoint URI using the given Default Graph URI
        /// </summary>
        /// <param name="endpointUri">Remote Endpoint URI</param>
        /// <param name="defaultGraphUri">Default Graph URI to use when Querying the Endpoint</param>
        /// <param name="namedGraphUris">Named Graph URIs to use when Querying the Endpoint</param>
        public SparqlRemoteEndpoint(Uri endpointUri, Uri defaultGraphUri, IEnumerable<String> namedGraphUris)
            : this(endpointUri, defaultGraphUri)
        {
            this._namedGraphUris.AddRange(namedGraphUris);
        }

        /// <summary>
        /// Creates a new SPARQL Endpoint for the given Endpoint URI using the given Default Graph URI
        /// </summary>
        /// <param name="endpointUri">Remote Endpoint URI</param>
        /// <param name="defaultGraphUri">Default Graph URI to use when Querying the Endpoint</param>
        /// <param name="namedGraphUris">Named Graph URIs to use when Querying the Endpoint</param>
        public SparqlRemoteEndpoint(Uri endpointUri, Uri defaultGraphUri, IEnumerable<Uri> namedGraphUris)
            : this(endpointUri, defaultGraphUri)
        {
            this._namedGraphUris.AddRange(namedGraphUris.Where(u => u != null).Select(u => u.AbsoluteUri));
        }

        /// <summary>
        /// Creates a new SPARQL Endpoint for the given Endpoint URI using the given Default Graph URI
        /// </summary>
        /// <param name="endpointUri">Remote Endpoint URI</param>
        /// <param name="defaultGraphUris">Default Graph URIs to use when Querying the Endpoint</param>
        public SparqlRemoteEndpoint(Uri endpointUri, IEnumerable<String> defaultGraphUris)
            : this(endpointUri)
        {
            this._defaultGraphUris.AddRange(defaultGraphUris);
        }

        /// <summary>
        /// Creates a new SPARQL Endpoint for the given Endpoint URI using the given Default Graph URI
        /// </summary>
        /// <param name="endpointUri">Remote Endpoint URI</param>
        /// <param name="defaultGraphUris">Default Graph URIs to use when Querying the Endpoint</param>
        public SparqlRemoteEndpoint(Uri endpointUri, IEnumerable<Uri> defaultGraphUris)
            : this(endpointUri)
        {
            this._defaultGraphUris.AddRange(defaultGraphUris.Where(u => u != null).Select(u => u.AbsoluteUri));
        }

        /// <summary>
        /// Creates a new SPARQL Endpoint for the given Endpoint URI using the given Default Graph URI
        /// </summary>
        /// <param name="endpointUri">Remote Endpoint URI</param>
        /// <param name="defaultGraphUris">Default Graph URIs to use when Querying the Endpoint</param>
        /// <param name="namedGraphUris">Named Graph URIs to use when Querying the Endpoint</param>
        public SparqlRemoteEndpoint(Uri endpointUri, IEnumerable<String> defaultGraphUris, IEnumerable<String> namedGraphUris)
            : this(endpointUri, defaultGraphUris)
        {
            this._namedGraphUris.AddRange(namedGraphUris);
        }

        /// <summary>
        /// Creates a new SPARQL Endpoint for the given Endpoint URI using the given Default Graph URI
        /// </summary>
        /// <param name="endpointUri">Remote Endpoint URI</param>
        /// <param name="defaultGraphUris">Default Graph URIs to use when Querying the Endpoint</param>
        /// <param name="namedGraphUris">Named Graph URIs to use when Querying the Endpoint</param>
        public SparqlRemoteEndpoint(Uri endpointUri, IEnumerable<String> defaultGraphUris, IEnumerable<Uri> namedGraphUris)
            : this(endpointUri, defaultGraphUris)
        {
            this._namedGraphUris.AddRange(namedGraphUris.Where(u => u != null).Select(u => u.AbsoluteUri));
        }

        /// <summary>
        /// Creates a new SPARQL Endpoint for the given Endpoint URI using the given Default Graph URI
        /// </summary>
        /// <param name="endpointUri">Remote Endpoint URI</param>
        /// <param name="defaultGraphUris">Default Graph URIs to use when Querying the Endpoint</param>
        /// <param name="namedGraphUris">Named Graph URIs to use when Querying the Endpoint</param>
        public SparqlRemoteEndpoint(Uri endpointUri, IEnumerable<Uri> defaultGraphUris, IEnumerable<String> namedGraphUris)
            : this(endpointUri, defaultGraphUris)
        {
            this._namedGraphUris.AddRange(namedGraphUris);
        }

        /// <summary>
        /// Creates a new SPARQL Endpoint for the given Endpoint URI using the given Default Graph URI
        /// </summary>
        /// <param name="endpointUri">Remote Endpoint URI</param>
        /// <param name="defaultGraphUris">Default Graph URIs to use when Querying the Endpoint</param>
        /// <param name="namedGraphUris">Named Graph URIs to use when Querying the Endpoint</param>
        public SparqlRemoteEndpoint(Uri endpointUri, IEnumerable<Uri> defaultGraphUris, IEnumerable<Uri> namedGraphUris)
            : this(endpointUri, defaultGraphUris)
        {
            this._namedGraphUris.AddRange(namedGraphUris.Where(u => u != null).Select(u => u.AbsoluteUri));
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the Default Graph URIs for Queries made to the SPARQL Endpoint
        /// </summary>
        public List<String> DefaultGraphs
        {
            get
            {
                return this._defaultGraphUris;
            }
        }

        /// <summary>
        /// Gets the List of Named Graphs used in requests
        /// </summary>
        public List<String> NamedGraphs
        {
            get
            {
                return this._namedGraphUris;
            }
        }

        /// <summary>
        /// Gets/Sets the Accept Header sent with ASK/SELECT queries
        /// </summary>
        /// <remarks>
        /// <para>
        /// Can be used to workaround buggy endpoints which don't like the broad Accept Header that dotNetRDF sends by default.  If not set or explicitly set to null the library uses the default header generated by <see cref="MimeTypesHelper.HttpSparqlAcceptHeader"/>
        /// </para>
        /// </remarks>
        public String ResultsAcceptHeader
        {
            get
            {
                return (this._resultsAccept != null ? this._resultsAccept : MimeTypesHelper.HttpSparqlAcceptHeader);
            }
            set
            {
                this._resultsAccept = value;
            }
        }

        /// <summary>
        /// Gets/Sets the Accept Header sent with CONSTRUCT/DESCRIBE queries
        /// </summary>
        /// <remarks>
        /// <para>
        /// Can be used to workaround buggy endpoints which don't like the broad Accept Header that dotNetRDF sends by default.  If not set or explicitly set to null the library uses the default header generated by <see cref="MimeTypesHelper.HttpAcceptHeader"/>
        /// </para>
        /// </remarks>
        public String RdfAcceptHeader
        {
            get
            {
                return (this._rdfAccept != null ? this._rdfAccept : MimeTypesHelper.HttpAcceptHeader);
            }
            set
            {
                this._rdfAccept = value;
            }
        }

        #endregion

        #region Query Methods

#if !SILVERLIGHT

        /// <summary>
        /// Makes a Query where the expected Result is a <see cref="SparqlResultSet">SparqlResultSet</see> i.e. SELECT and ASK Queries
        /// </summary>
        /// <param name="sparqlQuery">SPARQL Query String</param>
        /// <returns>A SPARQL Result Set</returns>
        public virtual SparqlResultSet QueryWithResultSet(String sparqlQuery)
        {
            //Ready a ResultSet then invoke the other overload
            SparqlResultSet results = new SparqlResultSet();
            this.QueryWithResultSet(new ResultSetHandler(results), sparqlQuery);
            return results;
        }

        /// <summary>
        /// Makes a Query where the expected Result is a <see cref="SparqlResultSet">SparqlResultSet</see> i.e. SELECT and ASK Queries
        /// </summary>
        /// <param name="handler">Results Handler</param>
        /// <param name="sparqlQuery">SPARQL Query String</param>
        public virtual void QueryWithResultSet(ISparqlResultsHandler handler, String sparqlQuery)
        {
            try
            {
                //Make the Query
                HttpWebResponse httpResponse = this.QueryInternal(sparqlQuery, this.ResultsAcceptHeader);

                //Parse into a ResultSet based on Content Type
                String ctype = httpResponse.ContentType;

                if (ctype.Contains(";"))
                {
                    ctype = ctype.Substring(0, ctype.IndexOf(";"));
                }

                ISparqlResultsReader resultsParser = MimeTypesHelper.GetSparqlParser(ctype);
                resultsParser.Load(handler, new StreamReader(httpResponse.GetResponseStream()));
                httpResponse.Close();
            }
            catch (WebException webEx)
            {
                if (webEx.Response != null) Tools.HttpDebugResponse((HttpWebResponse)webEx.Response);
                
                //Some sort of HTTP Error occurred
                throw new RdfQueryException("A HTTP Error occurred while trying to make the SPARQL Query, see inner exception for details", webEx);
            }
            catch (RdfException)
            {
                //Some problem with the RDF or Parsing thereof
                throw;
            }
        }

        /// <summary>
        /// Makes a Query where the expected Result is an RDF Graph ie. CONSTRUCT and DESCRIBE Queries
        /// </summary>
        /// <param name="sparqlQuery">SPARQL Query String</param>
        /// <returns>RDF Graph</returns>
        public virtual IGraph QueryWithResultGraph(String sparqlQuery)
        {
            //Set up an Empty Graph then invoke the other overload
            Graph g = new Graph();
            g.BaseUri = this.Uri;
            this.QueryWithResultGraph(new GraphHandler(g), sparqlQuery);
            return g;
        }

        /// <summary>
        /// Makes a Query where the expected Result is an RDF Graph ie. CONSTRUCT and DESCRIBE Queries
        /// </summary>
        /// <param name="handler">RDF Handler</param>
        /// <param name="sparqlQuery">SPARQL Query String</param>
        public virtual void QueryWithResultGraph(IRdfHandler handler, String sparqlQuery)
        {
            try
            {
                //Make the Query
                using (HttpWebResponse httpResponse = this.QueryInternal(sparqlQuery, this.RdfAcceptHeader))
                {
                    //Parse into a Graph based on Content Type
                    String ctype = httpResponse.ContentType;
                    IRdfReader parser = MimeTypesHelper.GetParser(ctype);
                    parser.Load(handler, new StreamReader(httpResponse.GetResponseStream()));
                    httpResponse.Close();
                }
            }
            catch (WebException webEx)
            {
                if (webEx.Response != null) Tools.HttpDebugResponse((HttpWebResponse)webEx.Response);
                //Some sort of HTTP Error occurred
                throw new RdfQueryException("A HTTP Error occurred when trying to make the SPARQL Query, see inner exception for details", webEx);
            }
            catch (RdfException)
            {
                //Some problem with the RDF or Parsing thereof
                throw;
            }
        }

        /// <summary>
        /// Makes a Query to a SPARQL Endpoint and returns the raw Response
        /// </summary>
        /// <param name="sparqlQuery">SPARQL Query String</param>
        /// <returns></returns>
        public virtual HttpWebResponse QueryRaw(String sparqlQuery)
        {
            try
            {
                //Make the Query
                //HACK: Changed to an accept all for the time being to ensure works OK with DBPedia and Virtuoso
                return this.QueryInternal(sparqlQuery, MimeTypesHelper.Any);
            }
            catch (WebException webEx)
            {
                if (webEx.Response != null) Tools.HttpDebugResponse((HttpWebResponse)webEx.Response);
                //Some sort of HTTP Error occurred
                throw new RdfQueryException("A HTTP Error occurred while trying to make the SPARQL Query", webEx);
            }
        }

        /// <summary>
        /// Makes a Query to a SPARQL Endpoint and returns the raw Response
        /// </summary>
        /// <param name="sparqlQuery">SPARQL Query String</param>
        /// <param name="mimeTypes">MIME Types to use for the Accept Header</param>
        /// <returns></returns>
        public virtual HttpWebResponse QueryRaw(String sparqlQuery, String[] mimeTypes)
        {
            try
            {
                //Make the Query
                return this.QueryInternal(sparqlQuery, MimeTypesHelper.CustomHttpAcceptHeader(mimeTypes));
            }
            catch (WebException webEx)
            {
                if (webEx.Response != null) Tools.HttpDebugResponse((HttpWebResponse)webEx.Response);
                
                //Some sort of HTTP Error occurred
                throw new RdfQueryException("A HTTP Error occurred while trying to make the SPARQL Query", webEx);
            }
        }

        /// <summary>
        /// Makes a Query where the expected Result is a SparqlResultSet ie. SELECT and ASK Queries
        /// </summary>
        /// <param name="sparqlQuery">SPARQL Query String</param>
        /// <returns>A Sparql Result Set</returns>
        /// <remarks>
        /// <para>
        /// Allows for implementation of asynchronous querying.  Note that the overloads of QueryWithResultSet() and QueryWithResultGraph() that take callbacks are already implemented asynchronously so you may wish to use those instead if you don't need to explicitly invoke and wait on an async operation.
        /// </para>
        /// </remarks>
        public delegate SparqlResultSet AsyncQueryWithResultSet(String sparqlQuery);

        /// <summary>
        /// Delegate for making a Query where the expected Result is an RDF Graph ie. CONSTRUCT and DESCRIBE Queries
        /// </summary>
        /// <param name="sparqlQuery">Sparql Query String</param>
        /// <returns>RDF Graph</returns>
        /// <remarks>Allows for implementation of asynchronous querying</remarks>
        /// <remarks>
        /// <para>
        /// Allows for implementation of asynchronous querying.  Note that the overloads of QueryWithResultSet() and QueryWithResultGraph() that take callbacks are already implemented asynchronously so you may wish to use those instead if you don't need to explicitly invoke and wait on an async operation.
        /// </para>
        /// </remarks>
        public delegate IGraph AsyncQueryWithResultGraph(String sparqlQuery);

        /// <summary>
        /// Internal method which builds the Query Uri and executes it via GET/POST as appropriate
        /// </summary>
        /// <param name="sparqlQuery">Sparql Query</param>
        /// <param name="acceptHeader">Accept Header to use for the request</param>
        /// <returns></returns>
        private HttpWebResponse QueryInternal(String sparqlQuery, String acceptHeader)
        {
            //Patched by Alexander Zapirov to handle situations where the SPARQL Query is very long
            //i.e. would exceed the length limit of the Uri class

            //Build the Query Uri
            StringBuilder queryUri = new StringBuilder();
            queryUri.Append(this.Uri.AbsoluteUri);
            bool longQuery = true;
            if (!this.HttpMode.Equals("POST") && sparqlQuery.Length <= LongQueryLength && sparqlQuery.IsAscii())
            {
                longQuery = false;
                try
                {
                    if (!this.Uri.Query.Equals(String.Empty))
                    {
                        queryUri.Append("&query=");
                    }
                    else
                    {
                        queryUri.Append("?query=");
                    }
                    queryUri.Append(HttpUtility.UrlEncode(sparqlQuery));

                    //Add the Default Graph URIs
                    foreach (String defaultGraph in this._defaultGraphUris)
                    {
                        if (!defaultGraph.Equals(String.Empty))
                        {
                            queryUri.Append("&default-graph-uri=");
                            queryUri.Append(HttpUtility.UrlEncode(defaultGraph));
                        }
                    }
                    //Add the Named Graph URIs
                    foreach (String namedGraph in this._namedGraphUris)
                    {
                        if (!namedGraph.Equals(String.Empty))
                        {
                            queryUri.Append("&named-graph-uri=");
                            queryUri.Append(HttpUtility.UrlEncode(namedGraph));
                        }
                    }
                }
                catch (UriFormatException)
                {
                    longQuery = true;
                }
            }

            //Make the Query via HTTP
            HttpWebResponse httpResponse;
            if (longQuery || queryUri.Length > 2048 || this.HttpMode == "POST")
            {
                //Long Uri/HTTP POST Mode so use POST
                StringBuilder postData = new StringBuilder();
                postData.Append("query=");
                postData.Append(HttpUtility.UrlEncode(sparqlQuery));

                //Add the Default Graph URI(s)
                foreach (String defaultGraph in this._defaultGraphUris)
                {
                    if (!defaultGraph.Equals(String.Empty))
                    {
                        queryUri.Append("&default-graph-uri=");
                        queryUri.Append(HttpUtility.UrlEncode(defaultGraph));
                    }
                }
                //Add the Named Graph URI(s)
                foreach (String namedGraph in this._namedGraphUris)
                {
                    if (!namedGraph.Equals(String.Empty))
                    {
                        queryUri.Append("&named-graph-uri=");
                        queryUri.Append(HttpUtility.UrlEncode(namedGraph));
                    }
                }

                httpResponse = this.ExecuteQuery(this.Uri, postData.ToString(), acceptHeader);
            }
            else
            {
                //Make the query normally via GET
                httpResponse = this.ExecuteQuery(UriFactory.Create(queryUri.ToString()), String.Empty, acceptHeader);
            }

            return httpResponse;
        }

        /// <summary>
        /// Internal Helper Method which executes the HTTP Requests against the Sparql Endpoint
        /// </summary>
        /// <param name="target">Uri to make Request to</param>
        /// <param name="postData">Data that is to be POSTed to the Endpoint in <strong>application/x-www-form-urlencoded</strong> format</param>
        /// <param name="accept">The Accept Header that should be used</param>
        /// <returns>HTTP Response</returns>
        private HttpWebResponse ExecuteQuery(Uri target, String postData, String accept)
        {
            //Expect errors in this function to be handled by the calling function

            //Set-up the Request
            HttpWebRequest httpRequest;
            HttpWebResponse httpResponse;
            httpRequest = (HttpWebRequest)WebRequest.Create(target);

            //Use HTTP GET/POST according to user set preference
            httpRequest.Accept = accept;
            if (!postData.Equals(String.Empty))
            {
                httpRequest.Method = "POST";
                httpRequest.ContentType = MimeTypesHelper.WWWFormURLEncoded;
                using (StreamWriter writer = new StreamWriter(httpRequest.GetRequestStream()))
                {
                    writer.Write(postData);
                    writer.Close();
                }
            }
            else
            {
                httpRequest.Method = this.HttpMode;
            }
#if !SILVERLIGHT
            if (this.Timeout > 0) httpRequest.Timeout = this.Timeout;
#endif

            //Apply Credentials to request if necessary
            if (this.Credentials != null)
            {
                if (Options.ForceHttpBasicAuth)
                {
                    //Forcibly include a HTTP basic authentication header
#if !SILVERLIGHT
                    string credentials = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(this.Credentials.UserName + ":" + this.Credentials.Password));
                    httpRequest.Headers.Add("Authorization", "Basic " + credentials);
#else
                    string credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(this.Credentials.UserName + ":" + this.Credentials.Password));
                    httpRequest.Headers["Authorization"] = "Basic " + credentials;
#endif
                }
                else
                {
                    //Leave .Net to handle the HTTP auth challenge response itself
                    httpRequest.Credentials = this.Credentials;
#if !SILVERLIGHT
                    httpRequest.PreAuthenticate = true;
#endif
                }
            }

#if !NO_PROXY
            //Use a Proxy if required
            if (this.Proxy != null)
            {
                httpRequest.Proxy = this.Proxy;
                if (this.UseCredentialsForProxy)
                {
                    httpRequest.Proxy.Credentials = this.Credentials;
                }
            }
#endif

            Tools.HttpDebugRequest(httpRequest);

            httpResponse = (HttpWebResponse)httpRequest.GetResponse();

            Tools.HttpDebugResponse(httpResponse);

            return httpResponse;
        }

#endif

        /// <summary>
        /// Makes a Query asynchronously where the expected Result is a <see cref="SparqlResultSet">SparqlResultSet</see> i.e. SELECT and ASK Queries
        /// </summary>
        /// <param name="query">SPARQL Query String</param>
        /// <param name="callback">Callback to invoke when the query completes</param>
        /// <param name="state">State to pass to the callback</param>
        public void QueryWithResultSet(String query, SparqlResultsCallback callback, Object state)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(this.Uri);
            request.Method = "POST";
            request.ContentType = MimeTypesHelper.WWWFormURLEncoded;
            request.Accept = this.ResultsAcceptHeader;

            Tools.HttpDebugRequest(request);

            request.BeginGetRequestStream(result =>
            {
                Stream stream = request.EndGetRequestStream(result);
                using (StreamWriter writer = new StreamWriter(stream))
                {
                    writer.Write("query=");
                    writer.Write(HttpUtility.UrlEncode(query));

                    foreach (String u in this.DefaultGraphs)
                    {
                        writer.Write("&default-graph-uri=");
                        writer.Write(HttpUtility.UrlEncode(u));
                    }
                    foreach (String u in this.NamedGraphs)
                    {
                        writer.Write("&named-graph-uri=");
                        writer.Write(HttpUtility.UrlEncode(u));
                    }

                    writer.Close();
                }

                request.BeginGetResponse(innerResult =>
                    {
                        using (HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(innerResult))
                        {
                            Tools.HttpDebugResponse(response);
                            
                            ISparqlResultsReader parser = MimeTypesHelper.GetSparqlParser(response.ContentType, false);
                            SparqlResultSet rset = new SparqlResultSet();
                            parser.Load(rset, new StreamReader(response.GetResponseStream()));

                            response.Close();
                            callback(rset, state);
                        }
                    }, null);
            }, null);

        }

        /// <summary>
        /// Makes a Query asynchronously where the expected Result is a <see cref="SparqlResultSet">SparqlResultSet</see> i.e. SELECT and ASK Queries
        /// </summary>
        /// <param name="query">SPARQL Query String</param>
        /// <param name="handler">Results Handler</param>
        /// <param name="callback">Callback to invoke when the query completes</param>
        /// <param name="state">State to pass to the callback</param>
        public void QueryWithResultSet(ISparqlResultsHandler handler, String query, QueryCallback callback, Object state)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(this.Uri);
            request.Method = "POST";
            request.ContentType = MimeTypesHelper.WWWFormURLEncoded;
            request.Accept = this.RdfAcceptHeader;

            Tools.HttpDebugRequest(request);

            request.BeginGetRequestStream(result =>
            {
                Stream stream = request.EndGetRequestStream(result);
                using (StreamWriter writer = new StreamWriter(stream))
                {
                    writer.Write("query=");
                    writer.Write(HttpUtility.UrlEncode(query));

                    foreach (String u in this.DefaultGraphs)
                    {
                        writer.Write("&default-graph-uri=");
                        writer.Write(HttpUtility.UrlEncode(u));
                    }
                    foreach (String u in this.NamedGraphs)
                    {
                        writer.Write("&named-graph-uri=");
                        writer.Write(HttpUtility.UrlEncode(u));
                    }

                    writer.Close();
                }

                request.BeginGetResponse(innerResult =>
                {
                    using (HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(innerResult))
                    {
                        Tools.HttpDebugResponse(response);
                        ISparqlResultsReader parser = MimeTypesHelper.GetSparqlParser(response.ContentType, false);
                        parser.Load(handler, new StreamReader(response.GetResponseStream()));

                        response.Close();
                        callback(null, handler, state);
                    }
                }, null);
            }, null);
        }

        /// <summary>
        /// Makes a Query asynchronously where the expected Result is an RDF Graph ie. CONSTRUCT and DESCRIBE Queries
        /// </summary>
        /// <param name="query">SPARQL Query String</param>
        /// <param name="callback">Callback to invoke when the query completes</param>
        /// <param name="state">State to pass to the callback</param>
        public void QueryWithResultGraph(String query, GraphCallback callback, Object state)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(this.Uri);
            request.Method = "POST";
            request.ContentType = MimeTypesHelper.WWWFormURLEncoded;
            request.Accept = this.RdfAcceptHeader;

            Tools.HttpDebugRequest(request);

            request.BeginGetRequestStream(result =>
            {
                Stream stream = request.EndGetRequestStream(result);
                using (StreamWriter writer = new StreamWriter(stream))
                {
                    writer.Write("query=");
                    writer.Write(HttpUtility.UrlEncode(query));

                    foreach (String u in this.DefaultGraphs)
                    {
                        writer.Write("&default-graph-uri=");
                        writer.Write(HttpUtility.UrlEncode(u));
                    }
                    foreach (String u in this.NamedGraphs)
                    {
                        writer.Write("&named-graph-uri=");
                        writer.Write(HttpUtility.UrlEncode(u));
                    }

                    writer.Close();
                }

                request.BeginGetResponse(innerResult =>
                {
                    HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(innerResult);
                    Tools.HttpDebugResponse(response);
                    IRdfReader parser = MimeTypesHelper.GetParser(response.ContentType);
                    Graph g = new Graph();
                    parser.Load(g, new StreamReader(response.GetResponseStream()));

                    callback(g, state);
                }, null);
            }, null);
        }

        /// <summary>
        /// Makes a Query asynchronously where the expected Result is an RDF Graph ie. CONSTRUCT and DESCRIBE Queries
        /// </summary>
        /// <param name="query">SPARQL Query String</param>
        /// <param name="handler">RDF Handler</param>
        /// <param name="callback">Callback to invoke when the query completes</param>
        /// <param name="state">State to pass to the callback</param>
        public void QueryWithResultGraph(IRdfHandler handler, String query, QueryCallback callback, Object state)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(this.Uri);
            request.Method = "POST";
            request.ContentType = MimeTypesHelper.WWWFormURLEncoded;
            request.Accept = this.ResultsAcceptHeader;

            Tools.HttpDebugRequest(request);

            request.BeginGetRequestStream(result =>
            {
                Stream stream = request.EndGetRequestStream(result);
                using (StreamWriter writer = new StreamWriter(stream))
                {
                    writer.Write("query=");
                    writer.Write(HttpUtility.UrlEncode(query));

                    foreach (String u in this.DefaultGraphs)
                    {
                        writer.Write("&default-graph-uri=");
                        writer.Write(HttpUtility.UrlEncode(u));
                    }
                    foreach (String u in this.NamedGraphs)
                    {
                        writer.Write("&named-graph-uri=");
                        writer.Write(HttpUtility.UrlEncode(u));
                    }

                    writer.Close();
                }

                request.BeginGetResponse(innerResult =>
                {
                    HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(innerResult);
                    Tools.HttpDebugResponse(response);
                    IRdfReader parser = MimeTypesHelper.GetParser(response.ContentType);
                    parser.Load(handler, new StreamReader(response.GetResponseStream()));

                    callback(handler, null, state);
                }, null);
            }, null);
        }

        #endregion

        /// <summary>
        /// Serializes the Endpoint's Configuration
        /// </summary>
        /// <param name="context">Configuration Serialization Context</param>
        public override void SerializeConfiguration(ConfigurationSerializationContext context)
        {
            INode endpoint = context.NextSubject;
            INode endpointClass = context.Graph.CreateUriNode(UriFactory.Create(ConfigurationLoader.ClassSparqlQueryEndpoint));
            INode rdfType = context.Graph.CreateUriNode(UriFactory.Create(RdfSpecsHelper.RdfType));
            INode dnrType = context.Graph.CreateUriNode(UriFactory.Create(ConfigurationLoader.PropertyType));
            INode endpointUri = context.Graph.CreateUriNode(UriFactory.Create(ConfigurationLoader.PropertyQueryEndpointUri));
            INode defGraphUri = context.Graph.CreateUriNode(UriFactory.Create(ConfigurationLoader.PropertyDefaultGraphUri));
            INode namedGraphUri = context.Graph.CreateUriNode(UriFactory.Create(ConfigurationLoader.PropertyNamedGraphUri));

            context.Graph.Assert(new Triple(endpoint, rdfType, endpointClass));
            context.Graph.Assert(new Triple(endpoint, dnrType, context.Graph.CreateLiteralNode(this.GetType().FullName)));
            context.Graph.Assert(new Triple(endpoint, endpointUri, context.Graph.CreateUriNode(this.Uri)));

            foreach (String u in this._defaultGraphUris)
            {
                context.Graph.Assert(new Triple(endpoint, defGraphUri, context.Graph.CreateUriNode(UriFactory.Create(u))));
            }
            foreach (String u in this._namedGraphUris)
            {
                context.Graph.Assert(new Triple(endpoint, namedGraphUri, context.Graph.CreateUriNode(UriFactory.Create(u))));
            }

            context.NextSubject = endpoint;
            base.SerializeConfiguration(context);
        }
    }
}

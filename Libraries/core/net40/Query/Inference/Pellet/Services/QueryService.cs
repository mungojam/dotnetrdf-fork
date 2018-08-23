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
using System.IO;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VDS.RDF.Parsing;
using VDS.RDF.Parsing.Handlers;

namespace VDS.RDF.Query.Inference.Pellet.Services
{
    /// <summary>
    /// Represents the SPARQL Query Service provided by a Pellet Server knowledge base
    /// </summary>
    public class QueryService : PelletService
    {
        private String _sparqlUri;

        /// <summary>
        /// Creates a new SPARQL Query Service
        /// </summary>
        /// <param name="name">Service Name</param>
        /// <param name="obj">JSON Object</param>
        internal QueryService(String name, JObject obj)
            : base(name, obj)
        {
            this._sparqlUri = this.Endpoint.Uri.Substring(0, this.Endpoint.Uri.IndexOf('{'));
        }

#if !SILVERLIGHT

        /// <summary>
        /// Makes a SPARQL Query against the Knowledge Base
        /// </summary>
        /// <param name="sparqlQuery">SPARQL Query</param>
        /// <returns></returns>
        public Object Query(String sparqlQuery)
        {
            SparqlRemoteEndpoint endpoint = new SparqlRemoteEndpoint(UriFactory.Create(this._sparqlUri));

            using (HttpWebResponse response = endpoint.QueryRaw(sparqlQuery))
            {
                try
                {
                    IRdfReader parser = MimeTypesHelper.GetParser(response.ContentType);
                    Graph g = new Graph();
                    parser.Load(g, new StreamReader(response.GetResponseStream()));

                    response.Close();
                    return g;
                }
                catch (RdfParserSelectionException)
                {
                    ISparqlResultsReader sparqlParser = MimeTypesHelper.GetSparqlParser(response.ContentType);
                    SparqlResultSet results = new SparqlResultSet();
                    sparqlParser.Load(results, new StreamReader(response.GetResponseStream()));

                    response.Close();
                    return results;
                }
            }
        }

        /// <summary>
        /// Processes a SPARQL Query against the Knowledge Base passing the results to the RDF or Results handler as appropriate
        /// </summary>
        /// <param name="rdfHandler">RDF Handler</param>
        /// <param name="resultsHandler">Results Handler</param>
        /// <param name="sparqlQuery">SPARQL Query</param>
        public void Query(IRdfHandler rdfHandler, ISparqlResultsHandler resultsHandler, String sparqlQuery)
        {
            SparqlRemoteEndpoint endpoint = new SparqlRemoteEndpoint(UriFactory.Create(this._sparqlUri));

            using (HttpWebResponse response = endpoint.QueryRaw(sparqlQuery))
            {
                try
                {
                    IRdfReader parser = MimeTypesHelper.GetParser(response.ContentType);
                    parser.Load(rdfHandler, new StreamReader(response.GetResponseStream()));
                }
                catch (RdfParserSelectionException)
                {
                    ISparqlResultsReader sparqlParser = MimeTypesHelper.GetSparqlParser(response.ContentType);
                    sparqlParser.Load(resultsHandler, new StreamReader(response.GetResponseStream()));
                }
                response.Close();
            }
        }

#endif

        /// <summary>
        /// Makes a SPARQL Query against the Knowledge Base
        /// </summary>
        /// <param name="sparqlQuery">SPARQL Query</param>
        /// <param name="graphCallback">Callback to invoke for queries that return a Graph</param>
        /// <param name="resultsCallback">Callback to invoke for queries that return a Result Set</param>
        /// <param name="state">State to pass to whichever callback function is invoked</param>
        /// <returns></returns>
        public void Query(String sparqlQuery, GraphCallback graphCallback, SparqlResultsCallback resultsCallback, Object state)
        {
            Graph g = new Graph();
            SparqlResultSet results = new SparqlResultSet();

            this.Query(new GraphHandler(g), new ResultSetHandler(results), sparqlQuery, (gh, rh, _) =>
                {
                    if (results.ResultsType != SparqlResultsType.Unknown)
                    {
                        resultsCallback(results, state);
                    }
                    else
                    {
                        graphCallback(g, state);
                    }
                }, state);
        }

        /// <summary>
        /// Processes a SPARQL Query against the Knowledge Base passing the results to the RDF or Results handler as appropriate
        /// </summary>
        /// <param name="rdfHandler">RDF Handler</param>
        /// <param name="resultsHandler">Results Handler</param>
        /// <param name="sparqlQuery">SPARQL Query</param>
        /// <param name="callback">Callback to invoke once handling of results has completed</param>
        /// <param name="state">State to pass to the callback</param>
        public void Query(IRdfHandler rdfHandler, ISparqlResultsHandler resultsHandler, String sparqlQuery, QueryCallback callback, Object state)
        {
            SparqlQueryParser parser = new SparqlQueryParser();
            SparqlQuery q = parser.ParseFromString(sparqlQuery);

            SparqlRemoteEndpoint endpoint = new SparqlRemoteEndpoint(UriFactory.Create(this._sparqlUri));
            switch (q.QueryType)
            {
                case SparqlQueryType.Ask:
                case SparqlQueryType.Select:
                case SparqlQueryType.SelectAll:
                case SparqlQueryType.SelectAllDistinct:
                case SparqlQueryType.SelectAllReduced:
                case SparqlQueryType.SelectDistinct:
                case SparqlQueryType.SelectReduced:
                    endpoint.QueryWithResultSet(sparqlQuery, (rs, _) =>
                        {
                            resultsHandler.Apply(rs);
                            callback(rdfHandler, resultsHandler, state);
                        }, state);
                    break;
                case SparqlQueryType.Construct:
                case SparqlQueryType.Describe:
                case SparqlQueryType.DescribeAll:
                    endpoint.QueryWithResultGraph(sparqlQuery, (g, _) =>
                        {
                            rdfHandler.Apply(g);
                            callback(rdfHandler, resultsHandler, state);
                        }, state);
                    break;

                default:
                    throw new RdfQueryException("Cannot execute unknown query types against Pellet Server");
            }
        }
    }
}

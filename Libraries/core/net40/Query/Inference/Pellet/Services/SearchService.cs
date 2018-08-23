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
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
#if !NO_WEB
using System.Web;
#endif
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace VDS.RDF.Query.Inference.Pellet.Services
{
    /// <summary>
    /// Represents the Search Service provided by a Pellet Server
    /// </summary>
    public class SearchService
        : PelletService
    {
        private String _searchUri;

        /// <summary>
        /// Creates a new Search Service
        /// </summary>
        /// <param name="name">Service Name</param>
        /// <param name="obj">JSON Object</param>
        internal SearchService(String name, JObject obj)
            : base(name, obj) 
        {
            this._searchUri = this.Endpoint.Uri.Substring(0, this.Endpoint.Uri.IndexOf('{'));
        }

#if !SILVERLIGHT

        /// <summary>
        /// Gets the list of Search Results which match the given search term
        /// </summary>
        /// <param name="text">Search Term</param>
        /// <returns>A list of Search Results representing Nodes in the Knowledge Base that match the search term</returns>
        public List<SearchServiceResult> Search(String text)
        {
            String search = this._searchUri + "?search=" + HttpUtility.UrlEncode(text);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(search);
            request.Method = this.Endpoint.HttpMethods.First();
            request.Accept = "text/json";

            Tools.HttpDebugRequest(request);

            String jsonText;
            JArray json;
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                Tools.HttpDebugResponse(response);
                jsonText = new StreamReader(response.GetResponseStream()).ReadToEnd();
                json = JArray.Parse(jsonText);

                response.Close();
            }

            //Parse the Response into Search Results
            try
            {
                List<SearchServiceResult> results = new List<SearchServiceResult>();

                foreach (JToken result in json.Children())
                {
                    JToken hit = result.SelectToken("hit");
                    String type = (String)hit.SelectToken("type");
                    INode node;
                    if (type.ToLower().Equals("uri"))
                    {
                        node = new UriNode(null, UriFactory.Create((String)hit.SelectToken("value")));
                    }
                    else
                    {
                        node = new BlankNode(null, (String)hit.SelectToken("value"));
                    }
                    double score = (double)result.SelectToken("score");

                    results.Add(new SearchServiceResult(node, score));
                }

                return results;
            }
            catch (WebException webEx)
            {
                if (webEx.Response != null) Tools.HttpDebugResponse((HttpWebResponse)webEx.Response);
                throw new RdfReasoningException("A HTTP error occurred while communicating with Pellet Server", webEx);
            }
            catch (Exception ex)
            {
                throw new RdfReasoningException("Error occurred while parsing Search Results from the Search Service", ex);
            }
        }

#endif

        /// <summary>
        /// Gets the list of Search Results which match the given search term
        /// </summary>
        /// <param name="text">Search Term</param>
        /// <param name="callback">Callback to invoke when the operation completes</param>
        /// <param name="state">State to pass to the callback</param>
        public void Search(String text, PelletSearchServiceCallback callback, Object state)
        {
            String search = this._searchUri + "?search=" + HttpUtility.UrlEncode(text);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(search);
            request.Method = this.Endpoint.HttpMethods.First();
            request.Accept = "text/json";

            Tools.HttpDebugRequest(request);

            String jsonText;
            JArray json;
            request.BeginGetResponse(result =>
                {
                    using (HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(result))
                    {
                        Tools.HttpDebugResponse(response);
                        jsonText = new StreamReader(response.GetResponseStream()).ReadToEnd();
                        json = JArray.Parse(jsonText);

                        response.Close();

                        //Parse the Response into Search Results

                        List<SearchServiceResult> results = new List<SearchServiceResult>();

                        foreach (JToken res in json.Children())
                        {
                            JToken hit = res.SelectToken("hit");
                            String type = (String)hit.SelectToken("type");
                            INode node;
                            if (type.ToLower().Equals("uri"))
                            {
                                node = new UriNode(null, UriFactory.Create((String)hit.SelectToken("value")));
                            }
                            else
                            {
                                node = new BlankNode(null, (String)hit.SelectToken("value"));
                            }
                            double score = (double)res.SelectToken("score");

                            results.Add(new SearchServiceResult(node, score));
                        }

                        callback(results, state);
                    }
                }, null);
        }
    }

    /// <summary>
    /// Represents a Search Result returned from the
    /// </summary>
    public class SearchServiceResult
    {
        private INode _n;
        private double _score;

        /// <summary>
        /// Creates a new Search Service Result
        /// </summary>
        /// <param name="node">Result Node</param>
        /// <param name="score">Result Score</param>
        internal SearchServiceResult(INode node, double score)
        {
            this._n = node;
            this._score = score;
        }

        /// <summary>
        /// Gets the Node for this Result
        /// </summary>
        public INode Node
        {
            get
            {
                return this._n;
            }
        }

        /// <summary>
        /// Gets the Score for this Result
        /// </summary>
        public double Score
        {
            get
            {
                return this._score;
            }
        }

        /// <summary>
        /// Gets the String representation of the Result
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this._n.ToString() + ": " + this._score;
        }
    }
}

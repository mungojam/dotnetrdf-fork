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
using System.Net;
using System.Threading;
using VDS.RDF.Configuration;
using VDS.RDF.Parsing;
using VDS.RDF.Parsing.Handlers;
using VDS.RDF.Storage.Management;

namespace VDS.RDF.Storage
{
    /// <summary>
    /// Abstract Base Class for HTTP based Storage API implementations
    /// </summary>
    /// <remarks>
    /// <para>
    /// Does not actually implement any interface rather it provides common functionality around HTTP Proxying
    /// </para>
    /// <para>
    /// If the library is compiled with the NO_PROXY symbol then this code adds no functionality
    /// </para>
    /// </remarks>
    public abstract class BaseHttpConnector
    {
#if !NO_PROXY
        private WebProxy _proxy;

        /// <summary>
        /// Sets a Proxy Server to be used
        /// </summary>
        /// <param name="address">Proxy Address</param>
        public void SetProxy(String address)
        {
            this._proxy = new WebProxy(address);
        }

        /// <summary>
        /// Sets a Proxy Server to be used
        /// </summary>
        /// <param name="address">Proxy Address</param>
        public void SetProxy(Uri address)
        {
            this._proxy = new WebProxy(address);
        }

        /// <summary>
        /// Gets/Sets a Proxy Server to be used
        /// </summary>
        public WebProxy Proxy
        {
            get
            {
                return this._proxy;
            }
            set
            {
                this._proxy = value;
            }
        }

        /// <summary>
        /// Clears any in-use credentials so subsequent requests will not use a proxy server
        /// </summary>
        public void ClearProxy()
        {
            this._proxy = null;
        }

        /// <summary>
        /// Sets Credentials to be used for Proxy Server
        /// </summary>
        /// <param name="username">Username</param>
        /// <param name="password">Password</param>
        public void SetProxyCredentials(String username, String password)
        {
            if (this._proxy != null)
            {
                this._proxy.Credentials = new NetworkCredential(username, password);
            }
            else
            {
                throw new InvalidOperationException("Cannot set Proxy Credentials when Proxy settings have not been provided");
            }
        }

        /// <summary>
        /// Sets Credentials to be used for Proxy Server
        /// </summary>
        /// <param name="username">Username</param>
        /// <param name="password">Password</param>
        /// <param name="domain">Domain</param>
        public void SetProxyCredentials(String username, String password, String domain)
        {
            if (this._proxy != null)
            {
                this._proxy.Credentials = new NetworkCredential(username, password, domain);
            }
            else
            {
                throw new InvalidOperationException("Cannot set Proxy Credentials when Proxy settings have not been provided");
            }
        }

        /// <summary>
        /// Gets/Sets Credentials to be used for Proxy Server
        /// </summary>
        public ICredentials ProxyCredentials
        {
            get
            {
                if (this._proxy != null)
                {
                    return this._proxy.Credentials;
                }
                else
                {
                    return null;
                }
            }
            set
            {
                if (this._proxy != null)
                {
                    this._proxy.Credentials = value;
                }
                else
                {
                    throw new InvalidOperationException("Cannot set Proxy Credentials when Proxy settings have not been provided");
                }
            }
        }

        /// <summary>
        /// Clears the in-use proxy credentials so subsequent requests still use the proxy server but without credentials
        /// </summary>
        public void ClearProxyCredentials()
        {
            if (this._proxy != null)
            {
                this._proxy.Credentials = null;
            }
        }

#endif

        /// <summary>
        /// Adds Proxy Server to requests if used
        /// </summary>
        /// <param name="request">HTTP Web Request</param>
        /// <returns></returns>
        protected HttpWebRequest GetProxiedRequest(HttpWebRequest request)
        {
#if !NO_PROXY
            if (this._proxy != null)
            {
                request.Proxy = this._proxy;
            }
#endif
            return request;
        }

        /// <summary>
        /// Helper method which adds proxy configuration to serialization
        /// </summary>
        /// <param name="objNode">Object Node representing the <see cref="IStorageProvider">IStorageProvider</see> whose configuration is being serialized</param>
        /// <param name="context">Serialization Context</param>
        protected void SerializeProxyConfig(INode objNode, ConfigurationSerializationContext context)
        {
#if !NO_PROXY
            if (this._proxy != null)
            {
                INode proxy = context.NextSubject;
                INode usesProxy = context.Graph.CreateUriNode(UriFactory.Create(ConfigurationLoader.PropertyProxy));
                INode rdfType = context.Graph.CreateUriNode(UriFactory.Create(RdfSpecsHelper.RdfType));
                INode proxyType = context.Graph.CreateUriNode(UriFactory.Create(ConfigurationLoader.ClassProxy));
                INode server = context.Graph.CreateUriNode(UriFactory.Create(ConfigurationLoader.PropertyServer));
                INode user = context.Graph.CreateUriNode(UriFactory.Create(ConfigurationLoader.PropertyUser));
                INode pwd = context.Graph.CreateUriNode(UriFactory.Create(ConfigurationLoader.PropertyPassword));

                context.Graph.Assert(new Triple(objNode, usesProxy, proxy));
                context.Graph.Assert(new Triple(proxy, rdfType, proxyType));
                context.Graph.Assert(new Triple(proxy, server, context.Graph.CreateLiteralNode(this._proxy.Address.AbsoluteUri)));

                if (this._proxy.Credentials is NetworkCredential)
                {
                    NetworkCredential cred = (NetworkCredential)this._proxy.Credentials;
                    context.Graph.Assert(new Triple(proxy, user, context.Graph.CreateLiteralNode(cred.UserName)));
                    context.Graph.Assert(new Triple(proxy, pwd, context.Graph.CreateLiteralNode(cred.Password)));
                }
            }
#endif
        }
    }

    /// <summary>
    /// Abstract Base Class for HTTP Based <see cref="IAsyncStorageProvider">IAsyncStorageProvider</see> implementations
    /// </summary>
    /// <remarks>
    /// <para>
    /// It is expected that most classes extending from this will also then implement <see cref="IStorageProvider"/> separately for their synchronous communication, this class purely provides partial helper implementations for the asynchronous communication
    /// </para>
    /// </remarks>
    public abstract class BaseAsyncHttpConnector
        : BaseHttpConnector, IAsyncStorageProvider
    {
        private DoRequestSequenceDelgate _d;

        /// <summary>
        /// Creates a new Base Async HTTP Connector
        /// </summary>
        public BaseAsyncHttpConnector()
        {
            this._d = new DoRequestSequenceDelgate(this.DoRequestSequence);
        }

        /// <summary>
        /// Gets the parent server (if any)
        /// </summary>
        public virtual IStorageServer ParentServer
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the parent server (if any)
        /// </summary>
        public virtual IAsyncStorageServer AsyncParentServer
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// Loads a Graph from the Store asynchronously
        /// </summary>
        /// <param name="g">Graph to load into</param>
        /// <param name="graphUri">URI of the Graph to load</param>
        /// <param name="callback">Callback</param>
        /// <param name="state">State to pass to the callback</param>
        public virtual void LoadGraph(IGraph g, Uri graphUri, AsyncStorageCallback callback, Object state)
        {
            this.LoadGraph(g, graphUri.ToSafeString(), callback, state);
        }

        /// <summary>
        /// Loads a Graph from the Store asynchronously
        /// </summary>
        /// <param name="g">Graph to load into</param>
        /// <param name="graphUri">URI of the Graph to load</param>
        /// <param name="callback">Callback</param>
        /// <param name="state">State to pass to the callback</param>
        public virtual void LoadGraph(IGraph g, String graphUri, AsyncStorageCallback callback, Object state)
        {
            this.LoadGraph(new GraphHandler(g), graphUri, (sender, args, st) =>
                {
                    callback(sender, new AsyncStorageCallbackArgs(AsyncStorageOperation.LoadGraph, g, args.Error), st);
                }, state);
        }

        /// <summary>
        /// Loads a Graph from the Store asynchronously
        /// </summary>
        /// <param name="handler">Handler to load with</param>
        /// <param name="graphUri">URI of the Graph to load</param>
        /// <param name="callback">Callback</param>
        /// <param name="state">State to pass to the callback</param>
        public virtual void LoadGraph(IRdfHandler handler, Uri graphUri, AsyncStorageCallback callback, Object state)
        {
            this.LoadGraph(handler, graphUri.ToSafeString(), callback, state);
        }

        /// <summary>
        /// Loads a Graph from the Store asynchronously
        /// </summary>
        /// <param name="handler">Handler to load with</param>
        /// <param name="graphUri">URI of the Graph to load</param>
        /// <param name="callback">Callback</param>
        /// <param name="state">State to pass to the callback</param>
        public abstract void LoadGraph(IRdfHandler handler, String graphUri, AsyncStorageCallback callback, Object state);

        /// <summary>
        /// Helper method for doing async load operations, callers just need to provide an appropriately prepared HTTP request
        /// </summary>
        /// <param name="request">HTTP Request</param>
        /// <param name="handler">Handler to load with</param>
        /// <param name="callback">Callback</param>
        /// <param name="state">State to pass to the callback</param>
        protected internal void LoadGraphAsync(HttpWebRequest request, IRdfHandler handler, AsyncStorageCallback callback, Object state)
        {
            Tools.HttpDebugRequest(request);
            request.BeginGetResponse(r =>
            {
                try
                {
                    HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(r);
                    Tools.HttpDebugResponse(response);
                    //Parse the retrieved RDF
                    IRdfReader parser = MimeTypesHelper.GetParser(response.ContentType);
                    parser.Load(handler, new StreamReader(response.GetResponseStream()));

                    //If we get here then it was OK
                    response.Close();

                    callback(this, new AsyncStorageCallbackArgs(AsyncStorageOperation.LoadWithHandler, handler), state);
                }
                catch (WebException webEx)
                {
                    if (webEx.Response != null)
                    {
                        Tools.HttpDebugResponse((HttpWebResponse)webEx.Response);
                        if (((HttpWebResponse)webEx.Response).StatusCode == HttpStatusCode.NotFound)
                        {
                            callback(this, new AsyncStorageCallbackArgs(AsyncStorageOperation.LoadWithHandler, handler), state);
                            return;
                        }
                    }
                    callback(this, new AsyncStorageCallbackArgs(AsyncStorageOperation.LoadWithHandler, handler, new RdfStorageException("A HTTP Error occurred while trying to load a Graph from the Store", webEx)), state);
                }
                catch (Exception ex)
                {
                    callback(this, new AsyncStorageCallbackArgs(AsyncStorageOperation.LoadWithHandler, handler, StorageHelper.HandleError(ex, "loading a Graph asynchronously from")), state);
                }
            }, state);
        }

        /// <summary>
        /// Saves a Graph to the Store asynchronously
        /// </summary>
        /// <param name="g">Graph to save</param>
        /// <param name="callback">Callback</param>
        /// <param name="state">State to pass to the callback</param>
        public abstract void SaveGraph(IGraph g, AsyncStorageCallback callback, Object state);

        /// <summary>
        /// Helper method for doing async save operations, callers just need to provide an appropriately perpared HTTP requests and a RDF writer which will be used to write the data to the request body
        /// </summary>
        /// <param name="request">HTTP request</param>
        /// <param name="writer">RDF Writer</param>
        /// <param name="g">Graph to save</param>
        /// <param name="callback">Callback</param>
        /// <param name="state">State to pass to the callback</param>
        protected internal void SaveGraphAsync(HttpWebRequest request, IRdfWriter writer, IGraph g, AsyncStorageCallback callback, Object state)
        {
            request.BeginGetRequestStream(r =>
            {
                try
                {
                    Stream reqStream = request.EndGetRequestStream(r);
                    writer.Save(g, new StreamWriter(reqStream));

                    Tools.HttpDebugRequest(request);
                    
                    request.BeginGetResponse(r2 =>
                    {
                        try
                        {
                            HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(r2);
                            Tools.HttpDebugResponse(response);
                            //If we get here then it was OK
                            response.Close();
                            callback(this, new AsyncStorageCallbackArgs(AsyncStorageOperation.SaveGraph, g), state);
                        }
                        catch (WebException webEx)
                        {
                            callback(this, new AsyncStorageCallbackArgs(AsyncStorageOperation.SaveGraph, g, StorageHelper.HandleHttpError(webEx, "saving a Graph asynchronously to")), state);
                        }
                        catch (Exception ex)
                        {
                            callback(this, new AsyncStorageCallbackArgs(AsyncStorageOperation.SaveGraph, g, StorageHelper.HandleError(ex, "saving a Graph asynchronously to")), state);
                        }
                    }, state);
                }
                catch (WebException webEx)
                {
                    callback(this, new AsyncStorageCallbackArgs(AsyncStorageOperation.SaveGraph, g, StorageHelper.HandleHttpError(webEx, "saving a Graph asynchronously to")), state);
                }
                catch (Exception ex)
                {
                    callback(this, new AsyncStorageCallbackArgs(AsyncStorageOperation.SaveGraph, g, StorageHelper.HandleError(ex, "saving a Graph asynchronously to")), state);
                }
            }, state);
        }

        /// <summary>
        /// Updates a Graph in the Store asychronously
        /// </summary>
        /// <param name="graphUri">URI of the Graph to update</param>
        /// <param name="additions">Triples to be added</param>
        /// <param name="removals">Triples to be removed</param>
        /// <param name="callback">Callback</param>
        /// <param name="state">State to pass to the callback</param>
        public virtual void UpdateGraph(Uri graphUri, IEnumerable<Triple> additions, IEnumerable<Triple> removals, AsyncStorageCallback callback, Object state)
        {
            this.UpdateGraph(graphUri.ToSafeString(), additions, removals, callback, state);
        }

        /// <summary>
        /// Updates a Graph in the Store asychronously
        /// </summary>
        /// <param name="graphUri">URI of the Graph to update</param>
        /// <param name="additions">Triples to be added</param>
        /// <param name="removals">Triples to be removed</param>
        /// <param name="callback">Callback</param>
        /// <param name="state">State to pass to the callback</param>
        public abstract void UpdateGraph(String graphUri, System.Collections.Generic.IEnumerable<Triple> additions, System.Collections.Generic.IEnumerable<Triple> removals, AsyncStorageCallback callback, Object state);

        /// <summary>
        /// Helper method for doing async update operations, callers just need to provide an appropriately prepared HTTP request and a RDF writer which will be used to write the data to the request body
        /// </summary>
        /// <param name="request">HTTP Request</param>
        /// <param name="writer">RDF writer</param>
        /// <param name="graphUri">URI of the Graph to update</param>
        /// <param name="ts">Triples</param>
        /// <param name="callback">Callback</param>
        /// <param name="state">State to pass to the callback</param>
        protected internal void UpdateGraphAsync(HttpWebRequest request, IRdfWriter writer, Uri graphUri, IEnumerable<Triple> ts, AsyncStorageCallback callback, Object state)
        {
            Graph g = new Graph();
            g.Assert(ts);

            request.BeginGetRequestStream(r =>
            {
                try
                {
                    Stream reqStream = request.EndGetRequestStream(r);
                    writer.Save(g, new StreamWriter(reqStream));

                    Tools.HttpDebugRequest(request);

                    request.BeginGetResponse(r2 =>
                    {
                        try
                        {
                            HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(r2);
                            Tools.HttpDebugResponse(response);
                            //If we get here then it was OK
                            response.Close();
                            callback(this, new AsyncStorageCallbackArgs(AsyncStorageOperation.UpdateGraph, graphUri), state);
                        }
                        catch (WebException webEx)
                        {
                            callback(this, new AsyncStorageCallbackArgs(AsyncStorageOperation.UpdateGraph, graphUri, StorageHelper.HandleHttpError(webEx, "updating a Graph asynchronously in")), state);
                        }
                        catch (Exception ex)
                        {
                            callback(this, new AsyncStorageCallbackArgs(AsyncStorageOperation.UpdateGraph, graphUri, StorageHelper.HandleError(ex, "updating a Graph asynchronously in")), state);
                        }
                    }, state);
                }
                catch (WebException webEx)
                {
                    callback(this, new AsyncStorageCallbackArgs(AsyncStorageOperation.UpdateGraph, graphUri, StorageHelper.HandleHttpError(webEx, "updating a Graph asynchronously in")), state);
                }
                catch (Exception ex)
                {
                    callback(this, new AsyncStorageCallbackArgs(AsyncStorageOperation.UpdateGraph, graphUri, StorageHelper.HandleError(ex, "updating a Graph asynchronously in")), state);
                }
            }, state);
        }

        /// <summary>
        /// Deletes a Graph from the Store
        /// </summary>
        /// <param name="graphUri">URI of the Graph to delete</param>
        /// <param name="callback">Callback</param>
        /// <param name="state">State to pass to the callback</param>
        public virtual void DeleteGraph(Uri graphUri, AsyncStorageCallback callback, Object state)
        {
            this.DeleteGraph(graphUri.ToSafeString(), callback, state);
        }

        /// <summary>
        /// Deletes a Graph from the Store
        /// </summary>
        /// <param name="graphUri">URI of the Graph to delete</param>
        /// <param name="callback">Callback</param>
        /// <param name="state">State to pass to the callback</param>
        public abstract void DeleteGraph(String graphUri, AsyncStorageCallback callback, Object state);

        /// <summary>
        /// Helper method for doing async delete operations, callers just need to provide an appropriately prepared HTTP request
        /// </summary>
        /// <param name="request">HTTP request</param>
        /// <param name="allow404">Whether a 404 response counts as success</param>
        /// <param name="graphUri">URI of the Graph to delete</param>
        /// <param name="callback">Callback</param>
        /// <param name="state">State to pass to the callback</param>
        protected internal void DeleteGraphAsync(HttpWebRequest request, bool allow404, String graphUri, AsyncStorageCallback callback, Object state)
        {
            Tools.HttpDebugRequest(request);
            request.BeginGetResponse(r =>
                {
                    try
                    {
                        HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(r);

                        //Assume if returns to here we deleted the Graph OK
                        response.Close();
                        callback(this, new AsyncStorageCallbackArgs(AsyncStorageOperation.DeleteGraph, graphUri.ToSafeUri()), state);
                    }
                    catch (WebException webEx)
                    {
                        if (webEx.Response != null) Tools.HttpDebugResponse((HttpWebResponse)webEx.Response);
                        //Don't throw the error if we get a 404 - this means we couldn't do a delete as the graph didn't exist to start with
                        if (webEx.Response == null || (webEx.Response != null && (!allow404 || ((HttpWebResponse)webEx.Response).StatusCode != HttpStatusCode.NotFound)))
                        {
                            callback(this, new AsyncStorageCallbackArgs(AsyncStorageOperation.DeleteGraph, graphUri.ToSafeUri(), new RdfStorageException("A HTTP Error occurred while trying to delete a Graph from the Store asynchronously", webEx)), state);
                        }
                        else
                        {
                            //Consider a 404 as a success in some cases
                            callback(this, new AsyncStorageCallbackArgs(AsyncStorageOperation.DeleteGraph, graphUri.ToSafeUri()), state);
                        }
                    }
                    catch (Exception ex)
                    {
                        callback(this, new AsyncStorageCallbackArgs(AsyncStorageOperation.DeleteGraph, graphUri.ToSafeUri(), StorageHelper.HandleError(ex, "deleting a Graph asynchronously from")), state);
                    }
                }, state);
        }

        /// <summary>
        /// Lists the Graphs in the Store asynchronously
        /// </summary>
        /// <param name="callback">Callback</param>
        /// <param name="state">State to pass to the callback</param>
        public virtual void ListGraphs(AsyncStorageCallback callback, object state)
        {
            if (this is IAsyncQueryableStorage)
            {
                //Use ListUrisHandler and make an async query to list the graphs, when that returns we invoke the correct callback
                ListUrisHandler handler = new ListUrisHandler("g");
                ((IAsyncQueryableStorage)this).Query(null, handler, "SELECT DISTINCT ?g WHERE { GRAPH ?g { ?s ?p ?o } }", (sender, args, st) =>
                {
                    if (args.WasSuccessful)
                    {
                        callback(this, new AsyncStorageCallbackArgs(AsyncStorageOperation.ListGraphs, handler.Uris), state);
                    }
                    else
                    {
                        callback(this, new AsyncStorageCallbackArgs(AsyncStorageOperation.ListGraphs, args.Error), state);
                    }
                }, state);
            }
            else
            {
                callback(this, new AsyncStorageCallbackArgs(AsyncStorageOperation.ListGraphs, new RdfStorageException("Underlying store does not supported listing graphs asynchronously or has failed to appropriately override this method")), state);
            }
        }

        /// <summary>
        /// Indicates whether the Store is ready to accept requests
        /// </summary>
        public abstract bool IsReady
        {
            get;
        }

        /// <summary>
        /// Gets whether the Store is read only
        /// </summary>
        public abstract bool IsReadOnly
        {
            get;
        }

        /// <summary>
        /// Gets the IO Behaviour of the Store
        /// </summary>
        public abstract IOBehaviour IOBehaviour
        {
            get;
        }

        /// <summary>
        /// Gets whether the Store supports Triple level updates via the <see cref="BaseAsyncHttpConnector.UpdateGraph(Uri,IEnumerable{Triple},IEnumerable{Triple},AsyncStorageCallback,Object)">UpdateGraph()</see> method
        /// </summary>
        public abstract bool UpdateSupported
        {
            get;
        }

        /// <summary>
        /// Gets whether the Store supports Graph deletion via the <see cref="BaseAsyncHttpConnector.DeleteGraph(Uri, AsyncStorageCallback, Object)">DeleteGraph()</see> method
        /// </summary>
        public abstract bool DeleteSupported
        {
            get;
        }

        /// <summary>
        /// Gets whether the Store supports listing graphs via the <see cref="BaseAsyncHttpConnector.ListGraphs(AsyncStorageCallback, Object)">ListGraphs()</see> method
        /// </summary>
        public abstract bool ListGraphsSupported
        {
            get;
        }

        /// <summary>
        /// Diposes of the Store
        /// </summary>
        public abstract void Dispose();

        /// <summary>
        /// Helper method for doing async operations where a sequence of HTTP requests must be run
        /// </summary>
        /// <param name="requests">HTTP requests</param>
        /// <param name="callback">Callback</param>
        /// <param name="state">State to pass to the callback</param>
        protected internal void MakeRequestSequence(IEnumerable<HttpWebRequest> requests, AsyncStorageCallback callback, Object state)
        {
            this._d.BeginInvoke(requests, callback, state, this.MakeRequestSequenceCallback, callback);
        }

        private void MakeRequestSequenceCallback(IAsyncResult r)
        {
            AsyncStorageCallback callback = r.AsyncState as AsyncStorageCallback;
            try
            {
                this._d.EndInvoke(r);
                //callback(this, new AsyncStorageCallbackArgs(AsyncStorageOperation.Unknown), null);
            }
            catch (Exception ex)
            {
                if (callback != null)
                {
                    callback(this, new AsyncStorageCallbackArgs(AsyncStorageOperation.Unknown, new RdfStorageException("Unexpected error while making a sequence of asynchronous requests to the Store, see inner exception for details", ex)), null);
                }
            }
        }

        private delegate void DoRequestSequenceDelgate(IEnumerable<HttpWebRequest> requests, AsyncStorageCallback callback, Object state);

        private void DoRequestSequence(IEnumerable<HttpWebRequest> requests, AsyncStorageCallback callback, Object state)
        {
            ManualResetEvent signal = new ManualResetEvent(false);
            foreach (HttpWebRequest request in requests)
            {
                Tools.HttpDebugRequest(request);

                request.BeginGetResponse(r =>
                {
                    try
                    {
                        HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(r);

                        //This request worked OK, close the response and carry on
                        response.Close();
                        signal.Set();
                    }
                    catch (WebException webEx)
                    {
                        if (webEx.Response != null) Tools.HttpDebugResponse((HttpWebResponse)webEx.Response);
                        callback(this, new AsyncStorageCallbackArgs(AsyncStorageOperation.Unknown, new RdfStorageException("A HTTP Error occurred while making a sequence of asynchronous requests to the Store, see inner exception for details", webEx)), state);
                        return;
                    }
                    catch (Exception ex)
                    {
                        callback(this, new AsyncStorageCallbackArgs(AsyncStorageOperation.Unknown, new RdfStorageException("Unexpected error while making a sequence of asynchronous requests to the Store, see inner exception for details", ex)), state);
                        return;
                    }
                }, state);

                signal.WaitOne();
                signal.Reset();
            }

            callback(this, new AsyncStorageCallbackArgs(AsyncStorageOperation.Unknown), state);
        }
    }
}

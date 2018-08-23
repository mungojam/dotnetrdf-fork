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
using System.Net;
using VDS.RDF.Query;
using VDS.RDF.Query.Datasets;
using VDS.RDF.Storage;
using VDS.RDF.Storage.Management;
using VDS.RDF.Update;

namespace VDS.RDF.Configuration
{
    /// <summary>
    /// Factory class for producing <see cref="IStorageProvider">IStorageProvider</see> and <see cref="IStorageServer"/> instances from Configuration Graphs
    /// </summary>
    public class StorageFactory
        : IObjectFactory
    {
        private const String AllegroGraph = "VDS.RDF.Storage.AllegroGraphConnector",
                             AllegroGraphServer = "VDS.RDF.Storage.Management.AllegroGraphServer",
                             DatasetFile = "VDS.RDF.Storage.DatasetFileManager",
                             Dydra = "VDS.RDF.Storage.DydraConnector",
                             FourStore = "VDS.RDF.Storage.FourStoreConnector",
                             Fuseki = "VDS.RDF.Storage.FusekiConnector",
                             InMemory = "VDS.RDF.Storage.InMemoryManager",
                             ReadOnly = "VDS.RDF.Storage.ReadOnlyConnector",
                             ReadOnlyQueryable = "VDS.RDF.Storage.QueryableReadOnlyConnector",
                             ReadWriteSparql = "VDS.RDF.Storage.ReadWriteSparqlConnector",
                             Sesame = "VDS.RDF.Storage.SesameHttpProtocolConnector",
                             SesameV5 = "VDS.RDF.Storage.SesameHttpProtocolVersion5Connector",
                             SesameV6 = "VDS.RDF.Storage.SesameHttpProtocolVersion6Connector",
                             SesameServer = "VDS.RDF.Storage.Management.SesameServer",
                             Sparql = "VDS.RDF.Storage.SparqlConnector",
                             SparqlHttpProtocol = "VDS.RDF.Storage.SparqlHttpProtocolConnector",
                             Stardog = "VDS.RDF.Storage.StardogConnector",
                             StardogServer = "VDS.RDF.Storage.Management.StardogServer"
                             ;

        /// <summary>
        /// Tries to load a Generic IO Manager based on information from the Configuration Graph
        /// </summary>
        /// <param name="g">Configuration Graph</param>
        /// <param name="objNode">Object Node</param>
        /// <param name="targetType">Target Type</param>
        /// <param name="obj">Output Object</param>
        /// <returns></returns>
        public bool TryLoadObject(IGraph g, INode objNode, Type targetType, out object obj)
        {
#if !NO_SYNC_HTTP
            IStorageProvider storageProvider = null;
            IStorageServer storageServer = null;
            SparqlConnectorLoadMethod loadMode;
#else
            IAsyncStorageProvider storageProvider = null;
            IAsyncStorageServer storageServer = null;
#endif
            obj = null;

            String server, user, pwd, store, catalog, loadModeRaw;
            bool isAsync;

            Object temp;
            INode storeObj;

            //Create the URI Nodes we're going to use to search for things
            INode propServer = g.CreateUriNode(UriFactory.Create(ConfigurationLoader.PropertyServer)),
                  propDb = g.CreateUriNode(UriFactory.Create(ConfigurationLoader.PropertyDatabase)),
                  propStore = g.CreateUriNode(UriFactory.Create(ConfigurationLoader.PropertyStore)),
                  propAsync = g.CreateUriNode(UriFactory.Create(ConfigurationLoader.PropertyAsync)),
                  propStorageProvider = g.CreateUriNode(UriFactory.Create(ConfigurationLoader.PropertyStorageProvider));

            switch (targetType.FullName)
            {
                case AllegroGraph:
                    //Get the Server, Catalog and Store
                    server = ConfigurationLoader.GetConfigurationString(g, objNode, propServer);
                    if (server == null) return false;
                    catalog = ConfigurationLoader.GetConfigurationString(g, objNode, g.CreateUriNode(UriFactory.Create(ConfigurationLoader.PropertyCatalog)));
                    store = ConfigurationLoader.GetConfigurationString(g, objNode, propStore);
                    if (store == null) return false;

                    //Get User Credentials
                    ConfigurationLoader.GetUsernameAndPassword(g, objNode, true, out user, out pwd);

                    if (user != null && pwd != null)
                    {
                        storageProvider = new AllegroGraphConnector(server, catalog, store, user, pwd);
                    }
                    else
                    {
                        storageProvider = new AllegroGraphConnector(server, catalog, store);
                    }
                    break;

                case AllegroGraphServer:
                    //Get the Server, Catalog and User Credentials
                    server = ConfigurationLoader.GetConfigurationString(g, objNode, propServer);
                    if (server == null) return false;
                    catalog = ConfigurationLoader.GetConfigurationString(g, objNode, g.CreateUriNode(UriFactory.Create(ConfigurationLoader.PropertyCatalog)));
                    ConfigurationLoader.GetUsernameAndPassword(g, objNode, true, out user, out pwd);

                    if (user != null && pwd != null)
                    {
                        storageServer = new AllegroGraphServer(server, catalog, user, pwd);
                    }
                    else
                    {
                        storageServer = new AllegroGraphServer(server, catalog);
                    }
                    break;

                case DatasetFile:
                    //Get the Filename and whether the loading should be done asynchronously
                    String file = ConfigurationLoader.GetConfigurationString(g, objNode, g.CreateUriNode(UriFactory.Create(ConfigurationLoader.PropertyFromFile)));
                    if (file == null) return false;
                    file = ConfigurationLoader.ResolvePath(file);
                    isAsync = ConfigurationLoader.GetConfigurationBoolean(g, objNode, propAsync, false);
                    storageProvider = new DatasetFileManager(file, isAsync);
                    break;

                case Dydra:
                    //Get the Account Name and Store
                    String account = ConfigurationLoader.GetConfigurationString(g, objNode, g.CreateUriNode(UriFactory.Create(ConfigurationLoader.PropertyCatalog)));
                    if (account == null) return false;
                    store = ConfigurationLoader.GetConfigurationString(g, objNode, propStore);
                    if (store == null) return false;

                    //Get User Credentials
                    ConfigurationLoader.GetUsernameAndPassword(g, objNode, true, out user, out pwd);

                    if (user != null)
                    {
                        storageProvider = new DydraConnector(account, store, user);
                    }
                    else
                    {
                        storageProvider = new DydraConnector(account, store);
                    }
                    break;

                case FourStore:
                    //Get the Server and whether Updates are enabled
                    server = ConfigurationLoader.GetConfigurationString(g, objNode, propServer);
                    if (server == null) return false;
                    bool enableUpdates = ConfigurationLoader.GetConfigurationBoolean(g, objNode, g.CreateUriNode(UriFactory.Create(ConfigurationLoader.PropertyEnableUpdates)), true);
                    storageProvider = new FourStoreConnector(server, enableUpdates);
                    break;

                case Fuseki:
                    //Get the Server URI
                    server = ConfigurationLoader.GetConfigurationString(g, objNode, propServer);
                    if (server == null) return false;
                    storageProvider = new FusekiConnector(server);
                    break;

                case InMemory:
                    //Get the Dataset/Store
                    INode datasetObj = ConfigurationLoader.GetConfigurationNode(g, objNode, g.CreateUriNode(UriFactory.Create(ConfigurationLoader.PropertyUsingDataset)));
                    if (datasetObj != null)
                    {
                        temp = ConfigurationLoader.LoadObject(g, datasetObj);
                        if (temp is ISparqlDataset)
                        {
                            storageProvider = new InMemoryManager((ISparqlDataset)temp);
                        }
                        else
                        {
                            throw new DotNetRdfConfigurationException("Unable to load the In-Memory Manager identified by the Node '" + objNode.ToString() + "' as the value given for the dnr:usingDataset property points to an Object that cannot be loaded as an object which implements the ISparqlDataset interface");
                        }
                    }
                    else
                    {
                        //If no dnr:usingDataset try dnr:usingStore instead
                        storeObj = ConfigurationLoader.GetConfigurationNode(g, objNode, g.CreateUriNode(UriFactory.Create(ConfigurationLoader.PropertyUsingStore)));
                        if (storeObj != null)
                        {
                            temp = ConfigurationLoader.LoadObject(g, storeObj);
                            if (temp is IInMemoryQueryableStore)
                            {
                                storageProvider = new InMemoryManager((IInMemoryQueryableStore)temp);
                            }
                            else
                            {
                                throw new DotNetRdfConfigurationException("Unable to load the In-Memory Manager identified by the Node '" + objNode.ToString() + "' as the value given for the dnr:usingStore property points to an Object that cannot be loaded as an object which implements the IInMemoryQueryableStore interface");
                            }
                        }
                        else
                        {
                            //If no dnr:usingStore either then create a new empty store
                            storageProvider = new InMemoryManager();
                        }
                    }
                    break;

#if !NO_SYNC_HTTP

                case ReadOnly:
                    //Get the actual Manager we are wrapping
                    storeObj = ConfigurationLoader.GetConfigurationNode(g, objNode, propStorageProvider);
                    temp = ConfigurationLoader.LoadObject(g, storeObj);
                    if (temp is IStorageProvider)
                    {
                        storageProvider = new ReadOnlyConnector((IStorageProvider)temp);
                    }
                    else
                    {
                        throw new DotNetRdfConfigurationException("Unable to load the Read-Only Connector identified by the Node '" + objNode.ToString() + "' as the value given for the dnr:genericManager property points to an Object which cannot be loaded as an object which implements the required IStorageProvider interface");
                    }
                    break;

                case ReadOnlyQueryable:
                    //Get the actual Manager we are wrapping
                    storeObj = ConfigurationLoader.GetConfigurationNode(g, objNode, propStorageProvider);
                    temp = ConfigurationLoader.LoadObject(g, storeObj);
                    if (temp is IQueryableStorage)
                    {
                        storageProvider = new QueryableReadOnlyConnector((IQueryableStorage)temp);
                    }
                    else
                    {
                        throw new DotNetRdfConfigurationException("Unable to load the Queryable Read-Only Connector identified by the Node '" + objNode.ToString() + "' as the value given for the dnr:genericManager property points to an Object which cannot be loaded as an object which implements the required IQueryableStorage interface");
                    }
                    break;

#endif

                case Sesame:
                case SesameV5:
                case SesameV6:
                    //Get the Server and Store ID
                    server = ConfigurationLoader.GetConfigurationString(g, objNode, propServer);
                    if (server == null) return false;
                    store = ConfigurationLoader.GetConfigurationString(g, objNode, propStore);
                    if (store == null) return false;
                    ConfigurationLoader.GetUsernameAndPassword(g, objNode, true, out user, out pwd);
                    if (user != null && pwd != null)
                    {
#if !NO_SYNC_HTTP
                        storageProvider = (IStorageProvider)Activator.CreateInstance(targetType, new Object[] { server, store, user, pwd });
#else
                        storageProvider = (IAsyncStorageProvider)Activator.CreateInstance(targetType, new Object[] { server, store, user, pwd });
#endif
                    }
                    else
                    {
#if !NO_SYNC_HTTP
                        storageProvider = (IStorageProvider)Activator.CreateInstance(targetType, new Object[] { server, store });
#else
                        storageProvider = (IAsyncStorageProvider)Activator.CreateInstance(targetType, new Object[] { server, store });
#endif
                    }
                    break;

                case SesameServer:
                    //Get the Server and User Credentials
                    server = ConfigurationLoader.GetConfigurationString(g, objNode, propServer);
                    if (server == null) return false;
                    ConfigurationLoader.GetUsernameAndPassword(g, objNode, true, out user, out pwd);

                    if (user != null && pwd != null)
                    {
                        storageServer = new SesameServer(server, user, pwd);
                    }
                    else
                    {
                        storageServer = new SesameServer(server);
                    }
                    break;

#if !NO_SYNC_HTTP

                case Sparql:
                    //Get the Endpoint URI or the Endpoint
                    server = ConfigurationLoader.GetConfigurationString(g, objNode, new INode[] { g.CreateUriNode(UriFactory.Create(ConfigurationLoader.PropertyQueryEndpointUri)), g.CreateUriNode(UriFactory.Create(ConfigurationLoader.PropertyEndpointUri)) });

                    //What's the load mode?
                    loadModeRaw = ConfigurationLoader.GetConfigurationString(g, objNode, g.CreateUriNode(UriFactory.Create(ConfigurationLoader.PropertyLoadMode)));
                    loadMode = SparqlConnectorLoadMethod.Construct;
                    if (loadModeRaw != null)
                    {
                        try
                        {
#if SILVERLIGHT
                            loadMode = (SparqlConnectorLoadMethod)Enum.Parse(typeof(SparqlConnectorLoadMethod), loadModeRaw, false);
#else
                            loadMode = (SparqlConnectorLoadMethod)Enum.Parse(typeof(SparqlConnectorLoadMethod), loadModeRaw);
#endif
                        }
                        catch
                        {
                            throw new DotNetRdfConfigurationException("Unable to load the SparqlConnector identified by the Node '" + objNode.ToString() + "' as the value given for the property dnr:loadMode is not valid");
                        }
                    }

                    if (server == null)
                    {
                        INode endpointObj = ConfigurationLoader.GetConfigurationNode(g, objNode, new INode[] { g.CreateUriNode(UriFactory.Create(ConfigurationLoader.PropertyQueryEndpoint)), g.CreateUriNode(UriFactory.Create(ConfigurationLoader.PropertyEndpoint)) });
                        if (endpointObj == null) return false;
                        temp = ConfigurationLoader.LoadObject(g, endpointObj);
                        if (temp is SparqlRemoteEndpoint)
                        {
                            storageProvider = new SparqlConnector((SparqlRemoteEndpoint)temp, loadMode);
                        }
                        else
                        {
                            throw new DotNetRdfConfigurationException("Unable to load the SparqlConnector identified by the Node '" + objNode.ToString() + "' as the value given for the property dnr:endpoint points to an Object which cannot be loaded as an object which is of the type SparqlRemoteEndpoint");
                        }
                    }
                    else
                    {
                        //Are there any Named/Default Graph URIs
                        IEnumerable<Uri> defGraphs = from def in ConfigurationLoader.GetConfigurationData(g, objNode, g.CreateUriNode(UriFactory.Create(ConfigurationLoader.PropertyDefaultGraphUri)))
                                                     where def.NodeType == NodeType.Uri
                                                     select ((IUriNode)def).Uri;
                        IEnumerable<Uri> namedGraphs = from named in ConfigurationLoader.GetConfigurationData(g, objNode, g.CreateUriNode(UriFactory.Create(ConfigurationLoader.PropertyNamedGraphUri)))
                                                       where named.NodeType == NodeType.Uri
                                                       select ((IUriNode)named).Uri;
                        if (defGraphs.Any() || namedGraphs.Any())
                        {
                            storageProvider = new SparqlConnector(new SparqlRemoteEndpoint(UriFactory.Create(server), defGraphs, namedGraphs), loadMode);
                        }
                        else
                        {
                            storageProvider = new SparqlConnector(UriFactory.Create(server), loadMode);
                        }                        
                    }
                    break;

                case ReadWriteSparql:
                    SparqlRemoteEndpoint queryEndpoint;
                    SparqlRemoteUpdateEndpoint updateEndpoint;

                    //Get the Query Endpoint URI or the Endpoint
                    server = ConfigurationLoader.GetConfigurationString(g, objNode, new INode[] { g.CreateUriNode(UriFactory.Create(ConfigurationLoader.PropertyUpdateEndpointUri)), g.CreateUriNode(UriFactory.Create(ConfigurationLoader.PropertyEndpointUri)) });

                    //What's the load mode?
                    loadModeRaw = ConfigurationLoader.GetConfigurationString(g, objNode, g.CreateUriNode(UriFactory.Create(ConfigurationLoader.PropertyLoadMode)));
                    loadMode = SparqlConnectorLoadMethod.Construct;
                    if (loadModeRaw != null)
                    {
                        try
                        {
#if SILVERLIGHT
                            loadMode = (SparqlConnectorLoadMethod)Enum.Parse(typeof(SparqlConnectorLoadMethod), loadModeRaw, false);
#else
                            loadMode = (SparqlConnectorLoadMethod)Enum.Parse(typeof(SparqlConnectorLoadMethod), loadModeRaw);
#endif
                        }
                        catch
                        {
                            throw new DotNetRdfConfigurationException("Unable to load the ReadWriteSparqlConnector identified by the Node '" + objNode.ToString() + "' as the value given for the property dnr:loadMode is not valid");
                        }
                    }

                    if (server == null)
                    {
                        INode endpointObj = ConfigurationLoader.GetConfigurationNode(g, objNode, new INode[] { g.CreateUriNode(UriFactory.Create(ConfigurationLoader.PropertyQueryEndpoint)), g.CreateUriNode(UriFactory.Create(ConfigurationLoader.PropertyEndpoint)) });
                        if (endpointObj == null) return false;
                        temp = ConfigurationLoader.LoadObject(g, endpointObj);
                        if (temp is SparqlRemoteEndpoint)
                        {
                            queryEndpoint = (SparqlRemoteEndpoint)temp;
                        }
                        else
                        {
                            throw new DotNetRdfConfigurationException("Unable to load the ReadWriteSparqlConnector identified by the Node '" + objNode.ToString() + "' as the value given for the property dnr:queryEndpoint/dnr:endpoint points to an Object which cannot be loaded as an object which is of the type SparqlRemoteEndpoint");
                        }
                    }
                    else
                    {
                        //Are there any Named/Default Graph URIs
                        IEnumerable<Uri> defGraphs = from def in ConfigurationLoader.GetConfigurationData(g, objNode, g.CreateUriNode(UriFactory.Create(ConfigurationLoader.PropertyDefaultGraphUri)))
                                                     where def.NodeType == NodeType.Uri
                                                     select ((IUriNode)def).Uri;
                        IEnumerable<Uri> namedGraphs = from named in ConfigurationLoader.GetConfigurationData(g, objNode, g.CreateUriNode(UriFactory.Create(ConfigurationLoader.PropertyNamedGraphUri)))
                                                       where named.NodeType == NodeType.Uri
                                                       select ((IUriNode)named).Uri;
                        if (defGraphs.Any() || namedGraphs.Any())
                        {
                            queryEndpoint = new SparqlRemoteEndpoint(UriFactory.Create(server), defGraphs, namedGraphs); ;
                        }
                        else
                        {
                            queryEndpoint = new SparqlRemoteEndpoint(UriFactory.Create(server));
                        }
                    }

                    //Find the Update Endpoint or Endpoint URI
                    server = ConfigurationLoader.GetConfigurationString(g, objNode, new INode[] { g.CreateUriNode(UriFactory.Create(ConfigurationLoader.PropertyUpdateEndpointUri)), g.CreateUriNode(UriFactory.Create(ConfigurationLoader.PropertyEndpointUri)) });

                    if (server == null)
                    {
                        INode endpointObj = ConfigurationLoader.GetConfigurationNode(g, objNode, new INode[] { g.CreateUriNode(UriFactory.Create(ConfigurationLoader.PropertyUpdateEndpoint)), g.CreateUriNode(UriFactory.Create(ConfigurationLoader.PropertyEndpoint)) });
                        if (endpointObj == null) return false;
                        temp = ConfigurationLoader.LoadObject(g, endpointObj);
                        if (temp is SparqlRemoteUpdateEndpoint)
                        {
                            updateEndpoint = (SparqlRemoteUpdateEndpoint)temp;
                        }
                        else
                        {
                            throw new DotNetRdfConfigurationException("Unable to load the ReadWriteSparqlConnector identified by the Node '" + objNode.ToString() + "' as the value given for the property dnr:updateEndpoint/dnr:endpoint points to an Object which cannot be loaded as an object which is of the type SparqlRemoteUpdateEndpoint");
                        }
                    }
                    else
                    {
                        updateEndpoint = new SparqlRemoteUpdateEndpoint(UriFactory.Create(server));
                    }

                    storageProvider = new ReadWriteSparqlConnector(queryEndpoint, updateEndpoint);

                    break;

#endif

                case SparqlHttpProtocol:
                    //Get the Service URI
                    server = ConfigurationLoader.GetConfigurationString(g, objNode, propServer);
                    if (server == null) return false;
                    storageProvider = new SparqlHttpProtocolConnector(UriFactory.Create(server));
                    break;

                case Stardog:
                    //Get the Server and Store
                    server = ConfigurationLoader.GetConfigurationString(g, objNode, propServer);
                    if (server == null) return false;
                    store = ConfigurationLoader.GetConfigurationString(g, objNode, propStore);
                    if (store == null) return false;

                    //Get User Credentials
                    ConfigurationLoader.GetUsernameAndPassword(g, objNode, true, out user, out pwd);

                    //Get Reasoning Mode
                    StardogReasoningMode reasoning = StardogReasoningMode.None;
                    String mode = ConfigurationLoader.GetConfigurationString(g, objNode, g.CreateUriNode(UriFactory.Create(ConfigurationLoader.PropertyLoadMode)));
                    if (mode != null)
                    {
                        try
                        {
                            reasoning = (StardogReasoningMode)Enum.Parse(typeof(StardogReasoningMode), mode, true);
                        }
                        catch
                        {
                            reasoning = StardogReasoningMode.None;
                        }
                    }

                    if (user != null && pwd != null)
                    {
                        storageProvider = new StardogConnector(server, store, reasoning, user, pwd);
                    }
                    else
                    {
                        storageProvider = new StardogConnector(server, store, reasoning);
                    }
                    break;

                case StardogServer:
                    //Get the Server and User Credentials
                    server = ConfigurationLoader.GetConfigurationString(g, objNode, propServer);
                    if (server == null) return false;
                    ConfigurationLoader.GetUsernameAndPassword(g, objNode, true, out user, out pwd);

                    if (user != null && pwd != null)
                    {
                        storageServer = new StardogServer(server, user, pwd);
                    }
                    else
                    {
                        storageServer = new StardogServer(server);
                    }
                    break;
            }

            //Set the return object if one has been loaded
            if (storageProvider != null)
            {
                obj = storageProvider;
            }
            else if (storageServer != null)
            {
                obj = storageServer;
            }

#if !NO_PROXY
            //Check whether this is a proxyable manager and if we need to load proxy settings
            if (obj is BaseHttpConnector)
            {
                INode proxyNode = ConfigurationLoader.GetConfigurationNode(g, objNode, g.CreateUriNode(UriFactory.Create(ConfigurationLoader.PropertyProxy)));
                if (proxyNode != null)
                {
                    temp = ConfigurationLoader.LoadObject(g, proxyNode);
                    if (temp is WebProxy)
                    {
                        ((BaseHttpConnector)obj).Proxy = (WebProxy)temp;
                    }
                    else
                    {
                        throw new DotNetRdfConfigurationException("Unable to load storage provider/server identified by the Node '" + objNode.ToString() + "' as the value given for the dnr:proxy property pointed to an Object which could not be loaded as an object of the required type WebProxy");
                    }
                }
            }
#endif

            return (obj != null);
        }

        /// <summary>
        /// Gets whether this Factory can load objects of the given Type
        /// </summary>
        /// <param name="t">Type</param>
        /// <returns></returns>
        public bool CanLoadObject(Type t)
        {
            switch (t.FullName)
            {
                case AllegroGraph:
                case AllegroGraphServer:
                case DatasetFile:
                case Dydra:
                case FourStore:
                case Fuseki:
                case InMemory:
                case Sesame:
                case SesameV5:
                case SesameV6:
                case SesameServer:
                case ReadOnly:
                case ReadOnlyQueryable:
                case Sparql:
                case ReadWriteSparql:
                case SparqlHttpProtocol:
                case Stardog:
                case StardogServer:
                    return true;
                default:
                    return false;
            }
        }
    }
}

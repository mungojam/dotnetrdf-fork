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
using System.Text;
using VDS.RDF.Configuration;
using VDS.RDF.Parsing;

namespace VDS.RDF
{
    /// <summary>
    /// Abstract Base class for HTTP endpoints
    /// </summary>
    public abstract class BaseEndpoint
        : IConfigurationSerializable
    {
        private Uri _endpoint = null;
        private int _timeout = 30000;
        private String _httpMode = "GET";
        private NetworkCredential _credentials;
#if !NO_PROXY
        private WebProxy _proxy;
        private bool _useCredentialsForProxy = false;
#endif

        /// <summary>
        /// Creates a new Base Endpoint
        /// </summary>
        protected BaseEndpoint()
        {

        }

        /// <summary>
        /// Creates a new Base Endpoint
        /// </summary>
        /// <param name="endpointUri">Endpoint URI</param>
        public BaseEndpoint(Uri endpointUri)
        {
            if (endpointUri == null) throw new ArgumentNullException("endpointUri", "Endpoint URI cannot be null");
            this._endpoint = endpointUri;
        }

        /// <summary>
        /// Gets the Endpoints URI
        /// </summary>
        public Uri Uri
        {
            get
            {
                return this._endpoint;
            }
        }

        /// <summary>
        /// Gets/Sets the HTTP Mode used for requests
        /// </summary>
        /// <remarks>
        /// <para>
        /// Only GET and POST are permitted - implementations may override this property if they wish to support more methods
        /// </para>
        /// </remarks>
        public virtual String HttpMode
        {
            get
            {
                return this._httpMode;
            }
            set
            {
                //Can only use GET/POST
                if (value.Equals("GET", StringComparison.OrdinalIgnoreCase) || value.Equals("POST", StringComparison.OrdinalIgnoreCase))
                {
                    this._httpMode = value.ToUpper();
                }
            }
        }

        /// <summary>
        /// Gets/Sets the HTTP Timeouts used for Queries
        /// </summary>
        /// <remarks>
        /// <para>
        /// Defaults to 30 Seconds
        /// </para>
        /// <para>
        /// Not supported under Silverlight
        /// </para>
        /// </remarks>
        public int Timeout
        {
            get
            {
                return this._timeout;
            }
            set
            {
                if (value >= 0)
                {
                    this._timeout = value;
                }
            }
        }

        #region Credentials and Proxy Server

#if !NO_PROXY

        /// <summary>
        /// Controls whether the Credentials set with the <see cref="BaseEndpoint.SetCredentials(String,String)">SetCredentials()</see> method or the <see cref="BaseEndpoint.Credentials">Credentials</see>are also used for a Proxy (if used)
        /// </summary>
        public bool UseCredentialsForProxy
        {
            get
            {
                return this._useCredentialsForProxy;
            }
            set
            {
                this._useCredentialsForProxy = value;
            }
        }

#endif

        /// <summary>
        /// Sets the HTTP Digest authentication credentials to be used
        /// </summary>
        /// <param name="username">Username</param>
        /// <param name="password">Password</param>
        public void SetCredentials(String username, String password)
        {
            this._credentials = new NetworkCredential(username, password);
        }

        /// <summary>
        /// Sets the HTTP Digest authentication credentials to be used
        /// </summary>
        /// <param name="username">Username</param>
        /// <param name="password">Password</param>
        /// <param name="domain">Domain</param>
        public void SetCredentials(String username, String password, String domain)
        {
            this._credentials = new NetworkCredential(username, password, domain);
        }

        /// <summary>
        /// Gets/Sets the HTTP authentication credentials to be used
        /// </summary>
        public NetworkCredential Credentials
        {
            get
            {
                return this._credentials;
            }
            set
            {
                this._credentials = value;
            }
        }

        /// <summary>
        /// Clears any in-use credentials so subsequent requests will not use HTTP authentication
        /// </summary>
        public void ClearCredentials()
        {
            this._credentials = null;
        }

#if !NO_PROXY

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

        #endregion

        /// <summary>
        /// Serializes the endpoints Credential and Proxy information
        /// </summary>
        /// <param name="context">Configuration Serialization Context</param>
        public virtual void SerializeConfiguration(ConfigurationSerializationContext context)
        {
#if !NO_PROXY
            if (this._credentials != null || this._proxy != null)
#else
            if (this._credentials != null)
#endif
            {
                INode endpoint = context.NextSubject;
                INode user = context.Graph.CreateUriNode(UriFactory.Create(ConfigurationLoader.PropertyUser));
                INode pwd = context.Graph.CreateUriNode(UriFactory.Create(ConfigurationLoader.PropertyPassword));

                if (this._credentials != null)
                {
                    context.Graph.Assert(new Triple(endpoint, user, context.Graph.CreateLiteralNode(this._credentials.UserName)));
                    context.Graph.Assert(new Triple(endpoint, pwd, context.Graph.CreateLiteralNode(this._credentials.Password)));

#if !NO_PROXY
                    if (this._useCredentialsForProxy)
                    {
                        INode useCreds = context.Graph.CreateUriNode(UriFactory.Create(ConfigurationLoader.PropertyUseCredentialsForProxy));
                        context.Graph.Assert(new Triple(endpoint, useCreds, this._useCredentialsForProxy.ToLiteral(context.Graph)));
                    }
#endif
                }
#if !NO_PROXY
                if (this._proxy != null)
                {
                    INode proxy = context.NextSubject;
                    INode rdfType = context.Graph.CreateUriNode(UriFactory.Create(RdfSpecsHelper.RdfType));
                    INode usesProxy = context.Graph.CreateUriNode(UriFactory.Create(ConfigurationLoader.PropertyProxy));
                    INode proxyType = context.Graph.CreateUriNode(UriFactory.Create(ConfigurationLoader.ClassProxy));
                    INode server = context.Graph.CreateUriNode(UriFactory.Create(ConfigurationLoader.PropertyServer));

                    context.Graph.Assert(new Triple(endpoint, usesProxy, proxy));
                    context.Graph.Assert(new Triple(proxy, rdfType, proxyType));
                    context.Graph.Assert(new Triple(proxy, server, context.Graph.CreateLiteralNode(this._proxy.Address.AbsoluteUri)));

                    if (!this._useCredentialsForProxy && this._proxy.Credentials != null)
                    {
                        if (this._proxy.Credentials is NetworkCredential)
                        {
                            NetworkCredential cred = (NetworkCredential)this._proxy.Credentials;
                            context.Graph.Assert(new Triple(proxy, user, context.Graph.CreateLiteralNode(cred.UserName)));
                            context.Graph.Assert(new Triple(proxy, pwd, context.Graph.CreateLiteralNode(cred.Password)));
                        }
                    }
                }
#endif
            }            
        }
    }
}

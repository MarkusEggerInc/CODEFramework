using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Reflection;
using System.ServiceModel;
using System.Windows.Forms;
using CODE.Framework.Core.Configuration;
using CODE.Framework.Core.Utilities;
using CODE.Framework.Services.Server;

namespace CODE.Framework.Services.Tools.Windows
{
    /// <summary>
    /// Test host window
    /// </summary>
    public partial class TestServiceHost : Form, ILogger
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TestServiceHost"/> class.
        /// </summary>
        public TestServiceHost()
        {
            TypeFilter = LogEventType.Undefined;
            LoggingMediator.AddLogger(this);

            InitializeComponent();

            listView1.ListViewItemSorter = _sorter;
            _sorter.SortColumn = 2;
            listView1.Sort();

            // In the test environment, we create default settings for the service in case those settings are not configured
            if (!ConfigurationSettings.Settings.IsSettingSupported("ServiceBaseUrl"))
                ConfigurationSettings.Sources["Memory"].Settings["ServiceBaseUrl"] = "localhost";
            if (!ConfigurationSettings.Settings.IsSettingSupported("ServiceBasePort"))
                ConfigurationSettings.Sources["Memory"].Settings["ServiceBasePort"] = "50000";
            if (!ConfigurationSettings.Settings.IsSettingSupported("ServiceBasePath"))
                ConfigurationSettings.Sources["Memory"].Settings["ServiceBasePath"] = "dev";

            textDomain.Items.Add("localhost");
            textDomain.Enabled = false;
            var baseUrl = ConfigurationSettings.Settings["ServiceBaseUrl"];
            if (!StringHelper.Compare(baseUrl, "localhost")) textDomain.Items.Add(baseUrl);
            textDomain.SelectedIndex = textDomain.Items.Count - 1;
            int basePort;
            if (int.TryParse(ConfigurationSettings.Settings["ServiceBasePort"], out basePort))
            {
                _basePort = basePort;
                textPort.Text = _basePort.ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                _basePort = 50000;
                textPort.Text = "50000";
            }
            textPort.Enabled = false;
            textPath.Enabled = false;
            textPath.Text = ConfigurationSettings.Settings["ServiceBasePath"];
        }

        private int _basePort = 50000;

        /// <summary>
        /// Handles the SelectedIndexChanged event of the textDomain control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void textDomain_SelectedIndexChanged(object sender, EventArgs e)
        {
            ServiceGarden.BaseUrl = textDomain.Text.Trim();
        }

        /// <summary>
        /// Handles the TextChanged event of the textPort control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void textPort_TextChanged(object sender, EventArgs e)
        {
            int port;
            if (int.TryParse(textDomain.Text.Trim(), out port))
                ServiceGarden.BasePort = port;
        }

        private readonly Queue<Tuple<Func<string>, Action<string>>> _queuedExecutions = new Queue<Tuple<Func<string>, Action<string>>>();
        private bool _handleCreated;

        private void Execute(Func<string> worker, Action<string> completeMethod)
        {
            if (_handleCreated)
                worker.BeginInvoke(ar =>
                {
                    var result = worker.EndInvoke(ar);
                    BeginInvoke(completeMethod, result);
                }, null);
            else
                lock (_queuedExecutions)
                    _queuedExecutions.Enqueue(new Tuple<Func<string>, Action<string>>(worker, completeMethod));
        }

        /// <summary>
        /// Raises the <see cref="E:System.Windows.Forms.Form.Shown" /> event.
        /// </summary>
        /// <param name="e">A <see cref="T:System.EventArgs" /> that contains the event data.</param>
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            _handleCreated = true;
            ProcessQueue();
        }

        /// <summary>
        /// Raises the <see cref="E:System.Windows.Forms.Form.Activated" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> that contains the event data.</param>
        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            _handleCreated = true;
            ProcessQueue();
        }

        /// <summary>
        /// Raises the <see cref="E:System.Windows.Forms.Form.Load" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> that contains the event data.</param>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            _handleCreated = true;
            ProcessQueue();
        }

        private void ProcessQueue()
        {
            while (_queuedExecutions.Count > 0)
            {
                Tuple<Func<string>, Action<string>> tuple;
                lock (_queuedExecutions)
                    tuple = _queuedExecutions.Dequeue();
                Execute(tuple.Item1, tuple.Item2);
            }
        }

        /// <summary>
        /// Adds another service to the list of hosted services
        /// </summary>
        /// <param name="serviceType">Type of the service.</param>
        /// <param name="exposeWsdl">if set to <c>true</c> [expose WSDL].</param>
        public void AddServiceHostWsHttp(Type serviceType, bool exposeWsdl)
        {
            Execute(
                () => ServiceGarden.AddServiceHostWsHttp(serviceType, exposeWsdl),
                url => AddServiceInfo(serviceType, serviceType.GetInterfaces()[0], url, "WS HTTP", ServiceGarden.GetHostByEndpointAddress(url))
                );
        }

        /// <summary>
        /// Adds another service to the list of hosted services
        /// </summary>
        /// <param name="serviceType">Type of the service.</param>
        /// <param name="messageSize">Size of the message.</param>
        public void AddServiceHostWsHttp(Type serviceType, MessageSize messageSize)
        {
            var contractType = serviceType.GetInterfaces()[0];
            AddServiceHostWsHttp(serviceType, contractType, contractType.Name, messageSize);
        }

        /// <summary>
        /// Adds another service to the list of hosted services
        /// </summary>
        /// <param name="serviceType">Type of the service.</param>
        /// <param name="messageSize">Size of the message.</param>
        /// <param name="exposeWsdl">if set to <c>true</c> [expose WSDL].</param>
        public void AddServiceHostWsHttp(Type serviceType, MessageSize messageSize, bool exposeWsdl)
        {
            var contractType = serviceType.GetInterfaces()[0];
            AddServiceHostWsHttp(serviceType, contractType, contractType.Name, messageSize, exposeWsdl);
        }

        /// <summary>
        /// Adds another service to the list of hosted services
        /// </summary>
        /// <param name="serviceType">Type of the service.</param>
        /// <param name="contractType">Type of the contract.</param>
        /// <param name="exposeWsdl">if set to <c>true</c> [expose WSDL].</param>
        public void AddServiceHostWsHttp(Type serviceType, Type contractType, bool exposeWsdl)
        {
            Execute(
                () => ServiceGarden.AddServiceHostWsHttp(serviceType, contractType, exposeWsdl),
                url => AddServiceInfo(serviceType, contractType, url, "WS HTTP", ServiceGarden.GetHostByEndpointAddress(url))
                );
        }

        /// <summary>
        /// Adds another service to the list of hosted services
        /// </summary>
        /// <param name="serviceType">Type of the service.</param>
        /// <param name="contractType">Type of the contract.</param>
        /// <param name="serviceId">The service id.</param>
        /// <param name="exposeWsdl">if set to <c>true</c> [expose WSDL].</param>
        public void AddServiceHostWsHttp(Type serviceType, Type contractType, string serviceId, bool exposeWsdl)
        {
            Execute(
                () => ServiceGarden.AddServiceHostWsHttp(serviceType, contractType, serviceId, exposeWsdl),
                url => AddServiceInfo(serviceType, contractType, url, "WS HTTP", ServiceGarden.GetHostByEndpointAddress(url))
                );
        }

        /// <summary>
        /// Adds a WS HTTP based service host
        /// </summary>
        /// <param name="serviceType">Type of the service.</param>
        /// <param name="contractType">Type of the contract.</param>
        /// <param name="serviceId">The service id.</param>
        /// <param name="messageSize">Size of the message.</param>
        /// <param name="exposeWsdl">if set to <c>true</c> [expose WSDL].</param>
        /// <param name="baseAddress">The base address.</param>
        /// <param name="basePath">The base path.</param>
        /// <param name="extension">The extension.</param>
        /// <param name="useHttps">Indicates whether HTTP is to be used</param>
        public void AddServiceHostWsHttp(Type serviceType, Type contractType = null, string serviceId = null, MessageSize messageSize = MessageSize.Undefined, bool exposeWsdl = false, string baseAddress = null, string basePath = null, string extension = null, bool useHttps = false)
        {
            Execute(
                () => ServiceGarden.AddServiceHostWsHttp(serviceType, contractType, serviceId, messageSize, exposeWsdl, baseAddress, basePath, extension, useHttps),
                url => AddServiceInfo(serviceType, contractType, url, "WS HTTP", ServiceGarden.GetHostByEndpointAddress(url))
                );
        }

        /// <summary>
        /// Adds another service to the list of hosted services
        /// </summary>
        /// <param name="serviceType">Type of the service.</param>
        /// <param name="messageSize">Size of the message.</param>
        public void AddServiceHostRestXml(Type serviceType, MessageSize messageSize)
        {
            AddServiceHostRestXml(serviceType, null, null, messageSize);
        }

        /// <summary>Adds a new service ost using REST XML standards</summary>
        /// <param name="serviceType">Type of the service.</param>
        /// <param name="contractType">Type of the contract.</param>
        /// <param name="serviceId">The service id.</param>
        /// <param name="messageSize">Size of the message.</param>
        /// <param name="baseAddress">The base address.</param>
        /// <param name="basePath">The base path.</param>
        /// <param name="extension">The extension.</param>
        /// <param name="useHttps">Indicates whether HTTPS should be used</param>
        public void AddServiceHostRestXml(Type serviceType, Type contractType = null, string serviceId = null, MessageSize messageSize = MessageSize.Undefined, string baseAddress = null, string basePath = null, string extension = null, bool useHttps = false)
        {
            Execute(
                () => ServiceGarden.AddServiceHostRestXml(serviceType, contractType, serviceId, messageSize, baseAddress, basePath, extension, useHttps),
                url => AddServiceInfo(serviceType, contractType, url, "REST HTTP (XML)", ServiceGarden.GetHostByEndpointAddress(url))
                );
        }

        /// <summary>
        /// Adds another service to the list of hosted services
        /// </summary>
        /// <param name="serviceType">Type of the service.</param>
        /// <param name="messageSize">Size of the message.</param>
        public void AddServiceHostRestJson(Type serviceType, MessageSize messageSize)
        {
            AddServiceHostRestJson(serviceType, null, null, messageSize);
        }

        /// <summary>Adds a new REST service using JSON data format</summary>
        /// <param name="serviceType">Type of the service.</param>
        /// <param name="contractType">Type of the contract.</param>
        /// <param name="serviceId">The service id.</param>
        /// <param name="messageSize">Size of the message.</param>
        /// <param name="baseAddress">The base address.</param>
        /// <param name="basePath">The base path.</param>
        /// <param name="extension">The extension.</param>
        /// <param name="useHttps">Indicates whether HTTPS should be used</param>
        public void AddServiceHostRestJson(Type serviceType, Type contractType = null, string serviceId = null, MessageSize messageSize = MessageSize.Undefined, string baseAddress = null, string basePath = null, string extension = null, bool useHttps = false)
        {
            Execute(
                () => ServiceGarden.AddServiceHostRestJson(serviceType, contractType, serviceId, messageSize, baseAddress, basePath, extension, useHttps),
                url => AddServiceInfo(serviceType, contractType, url, "REST HTTP (JSON)", ServiceGarden.GetHostByEndpointAddress(url))
                );
        }

        /// <summary>
        /// Adds another service to the list of hosted services
        /// </summary>
        /// <param name="serviceType">Type of the service.</param>
        /// <param name="exposeWsdl">if set to <c>true</c> [expose WSDL].</param>
        public void AddServiceHostBasicHttp(Type serviceType, bool exposeWsdl)
        {
            Execute(
                () => ServiceGarden.AddServiceHostBasicHttp(serviceType, exposeWsdl),
                url => AddServiceInfo(serviceType, serviceType.GetInterfaces()[0], url, "Basic HTTP", ServiceGarden.GetHostByEndpointAddress(url))
                );
        }

        /// <summary>
        /// Adds another service to the list of hosted services
        /// </summary>
        /// <param name="serviceType">Type of the service.</param>
        /// <param name="messageSize">Size of the message.</param>
        public void AddServiceHostNetTcp(Type serviceType, MessageSize messageSize)
        {
            var contractType = serviceType.GetInterfaces()[0];
            AddServiceHostNetTcp(serviceType, contractType, contractType.Name, messageSize);
        }

        /// <summary>
        /// Adds another service to the list of hosted services
        /// </summary>
        /// <param name="serviceType">Type of the service.</param>
        /// <param name="messageSize">Size of the message.</param>
        public void AddServiceHostBasicHttp(Type serviceType, MessageSize messageSize)
        {
            AddServiceHostBasicHttp(serviceType, null, null, messageSize);
        }

        /// <summary>
        /// Adds another service to the list of hosted services
        /// </summary>
        /// <param name="serviceType">Type of the service.</param>
        /// <param name="messageSize">Size of the message.</param>
        /// <param name="exposeWsdl">if set to <c>true</c> [expose WSDL].</param>
        public void AddServiceHostBasicHttp(Type serviceType, MessageSize messageSize, bool exposeWsdl)
        {
            AddServiceHostBasicHttp(serviceType, null, null, messageSize, exposeWsdl);
        }


        /// <summary>
        /// Adds another service to the list of hosted services
        /// </summary>
        /// <param name="serviceType">Type of the service.</param>
        /// <param name="contractType">Type of the contract.</param>
        /// <param name="exposeWsdl">if set to <c>true</c> [expose WSDL].</param>
        public void AddServiceHostBasicHttp(Type serviceType, Type contractType, bool exposeWsdl)
        {
            Execute(
                () => ServiceGarden.AddServiceHostBasicHttp(serviceType, contractType, exposeWsdl),
                url => AddServiceInfo(serviceType, contractType, url, "Basic HTTP", ServiceGarden.GetHostByEndpointAddress(url))
                );
        }

        /// <summary>
        /// Adds another service to the list of hosted services
        /// </summary>
        /// <param name="serviceType">Type of the service.</param>
        /// <param name="contractType">Type of the contract.</param>
        /// <param name="serviceId">The service id.</param>
        /// <param name="exposeWsdl">if set to <c>true</c> [expose WSDL].</param>
        public void AddServiceHostBasicHttp(Type serviceType, Type contractType, string serviceId, bool exposeWsdl)
        {
            Execute(
                () => ServiceGarden.AddServiceHostBasicHttp(serviceType, contractType, serviceId, exposeWsdl),
                url => AddServiceInfo(serviceType, contractType, url, "Basic HTTP", ServiceGarden.GetHostByEndpointAddress(url))
                );
        }

        /// <summary>Adds a TCP/IP (net.tcp) service host</summary>
        /// <param name="serviceType">Type of the service.</param>
        /// <param name="contractType">Type of the contract.</param>
        /// <param name="serviceId">The service id.</param>
        /// <param name="messageSize">Size of the message.</param>
        public void AddServiceHostNetTcp(Type serviceType, Type contractType = null, string serviceId = null, MessageSize messageSize = MessageSize.Undefined)
        {
            var port = _basePort;
            _basePort++;
            Execute(
                () => ServiceGarden.AddServiceHostNetTcp(serviceType, contractType, serviceId, messageSize, port),
                url => AddServiceInfo(serviceType, contractType, url, "Net.Tcp", ServiceGarden.GetHostByEndpointAddress(url))
                );
        }


        /// <summary>Hosts a Basic HTTP service</summary>
        /// <param name="serviceType">Type of the service.</param>
        /// <param name="contractType">Type of the contract.</param>
        /// <param name="serviceId">The service id.</param>
        /// <param name="messageSize">Size of the message.</param>
        /// <param name="exposeWsdl">if set to <c>true</c> a WSDL endpoint is exposed.</param>
        /// <param name="baseAddress">The base address.</param>
        /// <param name="basePath">The base path.</param>
        /// <param name="extension">The extension.</param>
        /// <param name="useHttps">Indicates whether HTTPS should be used</param>
        public void AddServiceHostBasicHttp(Type serviceType, Type contractType = null, string serviceId = null, MessageSize messageSize = MessageSize.Undefined, bool exposeWsdl = false, string baseAddress = null, string basePath = null, string extension = null, bool useHttps = false)
        {
            Execute(
                () => ServiceGarden.AddServiceHostBasicHttp(serviceType, contractType, serviceId, messageSize, exposeWsdl, baseAddress, basePath, extension, useHttps),
                url => AddServiceInfo(serviceType, contractType, url, "Basic HTTP", ServiceGarden.GetHostByEndpointAddress(url))
                );
        }

        /// <summary>
        /// Adds the service info.
        /// </summary>
        /// <param name="serviceType">Type of the service.</param>
        /// <param name="contractType">Type of the contract.</param>
        /// <param name="url">The URL.</param>
        /// <param name="binding">The binding.</param>
        /// <param name="host">The actual service host</param>
        private void AddServiceInfo(Type serviceType, Type contractType, string url, string binding, ServiceHost host)
        {
            if (contractType == null)
            {
                var interfaces = serviceType.GetInterfaces();
                contractType = interfaces.Length == 1 ? interfaces[0] : serviceType;
            }

            var uri = !string.IsNullOrEmpty(url) ? new Uri(url) : null;
            var port = uri != null ? uri.Port : 0;
            var info = new ServiceInfoWrapper
            {
                ServiceContract = contractType,
                ServiceType = serviceType,
                Url = url,
                Host = host,
                Protocol = binding,
                WsdlExposed = "No",
                Port = port
            };

            long messageSize = 0;
            if (host != null)
                foreach (var dispatcher in host.ChannelDispatchers)
                {
                    var listener = dispatcher.Listener;
                    if (listener != null)
                    {
                        var listenerType = listener.GetType();

                        var propertyInfo = listenerType.GetProperty("MaxReceivedMessageSize", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        if (propertyInfo != null)
                        {
                            messageSize = Math.Max((long) propertyInfo.GetValue(listener, null), messageSize);
                            if (messageSize < 1024*1024*10) info.MessageSize = "Normal";
                            else if (messageSize >= 1024*1024*100) info.MessageSize = "Large";
                            else info.MessageSize = "Medium";
                        }

                        var propertyInfo2 = listenerType.GetProperty("Uri", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        if (propertyInfo2 != null)
                        {
                            var listenerUri = propertyInfo2.GetValue(listener, null) as Uri;
                            if (listenerUri != null) if (listenerUri.AbsoluteUri.ToLower().EndsWith("/wsdl")) info.WsdlExposed = "Yes";
                        }
                    }
                }

            var item = new ListViewItem(new[] {contractType.Name + " (" + serviceType.Name + ")", (!string.IsNullOrEmpty(url) ? "Started" : "Failed"), url, binding, info.MessageSize, info.WsdlExposed}) {Tag = info};
            switch (binding.ToLower())
            {
                case "rest http (xml)":
                    item.ImageIndex = 2;
                    break;
                case "rest http (json)":
                case "http-get":
                    item.ImageIndex = 1;
                    break;
                case "basic http":
                case "ws http":
                    item.ImageIndex = 3;
                    break;
                case "net.tcp":
                    item.ImageIndex = 0;
                    break;
                default:
                    item.ImageIndex = 0;
                    break;
            }

            if (string.IsNullOrEmpty(url))
                item.ForeColor = Color.Red;

            listView1.Items.Add(item);
            listView1.Sort();
        }

        /// <summary>Enables cross-domain service access for HTTP-based callers (such as JavaScript)</summary>
        /// <param name="allowedCallers">Allowed caller domains (URLs or *).</param>
        /// <returns>True if successful</returns>
        /// <remarks>
        /// This works by adding a cross-domain call HTTP header to service responses, which is used by browsers to check if these calls should be allowed.
        /// Note that this is for JavaScript clients and this is different from the Silverlight cross domain calls (AllowCrossDomainCalls)
        /// </remarks>
        /// <example>
        /// // Enables all cross domain calls to the specified domain 
        /// // from the specified domains
        /// host.AllowHttpCrossDomainCalls("www.eps-software.com");
        /// </example>
        public bool AllowHttpCrossDomainCalls(string allowedCallers)
        {
            return ServiceGarden.AllowHttpCrossDomainCalls(allowedCallers);
        }

        /// <summary>Enables cross-domain service access for HTTP-based callers (such as JavaScript)</summary>
        /// <returns>True if successful</returns>
        /// <remarks>
        /// This works by adding a cross-domain call HTTP header to service responses, which is used by browsers to check if these calls should be allowed.
        /// Note that this is for JavaScript clients and this is different from the Silverlight cross domain calls (AllowCrossDomainCalls)
        /// </remarks>
        /// <example>
        /// // Enables all cross domain calls to the specified domain 
        /// // from all domains
        /// host.AllowHttpCrossDomainCalls();
        /// </example>
        public bool AllowHttpCrossDomainCalls()
        {
            return AllowHttpCrossDomainCalls("*");
        }

        /// <summary>
        /// Enables cross-domain service access
        /// </summary>
        /// <remarks>
        /// Cross-access domain calling is of particular importance for Silverlight clients.
        /// </remarks>
        /// <example>
        /// // Enables all cross domain calls for the current service base URL
        /// var host = new TestServiceHost();
        /// host.AllowCrossDomainCalls();
        /// </example>
        public void AllowSilverlightCrossDomainCalls()
        {
            var domain = string.Empty;
            if (ConfigurationSettings.Settings.IsSettingSupported("ServiceBaseUrl"))
                domain = ConfigurationSettings.Settings["ServiceBaseUrl"];
            if (!domain.ToLower(CultureInfo.InvariantCulture).StartsWith("http://")) domain = "http://" + domain;
            AllowSilverlightCrossDomainCalls(new Uri(domain), null);
        }

        /// <summary>
        /// Enables cross-domain service access
        /// </summary>
        /// <param name="allowedCaller">The allowed caller.</param>
        /// <remarks>
        /// Cross-access domain calling is of particular importance for Silverlight clients.
        /// Note that the allowed callers are shared across all root domains if this method is called multiple times
        /// to enable different root domains. (This means that the service garden hosts service calls who's endpoints
        /// are on different domains, which rarely happens).
        /// </remarks>
        /// <example>
        /// // Enables all cross domain calls for the current service base URL
        /// // from the specified domain
        /// var host = new TestServiceHost();
        /// host.AllowCrossDomainCalls(new Uri("www.eps-software.com"));
        /// </example>
        public void AllowSilverlightCrossDomainCalls(Uri allowedCaller)
        {
            var domain = string.Empty;
            if (ConfigurationSettings.Settings.IsSettingSupported("ServiceBaseUrl"))
                domain = ConfigurationSettings.Settings["ServiceBaseUrl"];
            if (!domain.ToLower(CultureInfo.InvariantCulture).StartsWith("http://")) domain = "http://" + domain;
            AllowSilverlightCrossDomainCalls(new Uri(domain), new[] {allowedCaller});
        }

        /// <summary>
        /// Enables cross-domain service access
        /// </summary>
        /// <param name="allowedCallers">Allowed caller domains (URLs).</param>
        /// <remarks>
        /// Cross-access domain calling is of particular importance for Silverlight clients.
        /// Note that the allowed callers are shared across all root domains if this method is called multiple times
        /// to enable different root domains. (This means that the service garden hosts service calls who's endpoints
        /// are on different domains, which rarely happens).
        /// </remarks>
        /// <example>
        /// // Enables all cross domain calls for the current service base URL
        /// // from the specified domain
        /// var host = new TestServiceHost();
        /// host.AllowCrossDomainCalls(new Uri[] {new Uri("www.eps-software.com"), new Uri("www.Microsoft.com")});
        /// </example>
        public void AllowSilverlightCrossDomainCalls(Uri[] allowedCallers)
        {
            var domain = string.Empty;
            if (ConfigurationSettings.Settings.IsSettingSupported("ServiceBaseUrl"))
                domain = ConfigurationSettings.Settings["ServiceBaseUrl"];
            if (!domain.ToLower(CultureInfo.InvariantCulture).StartsWith("http://")) domain = "http://" + domain;
            AllowSilverlightCrossDomainCalls(new Uri(domain), allowedCallers);
        }

        /// <summary>
        /// Enables cross-domain service access for Silverlight service calls
        /// </summary>
        /// <param name="domain">The root domain the call is valid for.</param>
        /// <returns>URL of the hosted policy</returns>
        /// <remarks>
        /// Cross-access domain calling is of particular importance for Silverlight clients.
        /// </remarks>
        /// <example>
        /// // Enables all cross domain calls from any location to www.eps-software.com hosted services
        /// var host = new TestServiceHost();
        /// host.AllowCrossDomainCalls("www.eps-software.com");
        /// </example>
        public void AllowSilverlightCrossDomainCalls(string domain)
        {
            if (!domain.ToLower(CultureInfo.InvariantCulture).StartsWith("http://")) domain = "http://" + domain;
            AllowSilverlightCrossDomainCalls(new Uri(domain), null);
        }

        /// <summary>
        /// Enables cross-domain service access
        /// </summary>
        /// <param name="domain">The root domain the call is valid for.</param>
        /// <param name="allowedCallers">Allowed caller domains (URLs).</param>
        /// <returns>URL of the hosted policy</returns>
        /// <remarks>
        /// Cross-access domain calling is of particular importance for Silverlight clients.
        /// Note that the allowed callers are shared across all root domains if this method is called multiple times
        /// to enable different root domains. (This means that the service garden hosts service calls who's endpoints
        /// are on different domains, which rarely happens).
        /// </remarks>
        /// <example>
        /// // Enables all cross domain calls to the specified domain 
        /// // from the specified domains
        /// var host = new TestServiceHost();
        /// host.AllowCrossDomainCalls(
        ///     new Uri("www.epsservices.net"),
        ///     new Uri[] {new Uri("www.eps-software.com"), new Uri("www.Microsoft.com")}
        ///     );
        /// </example>
        public void AllowSilverlightCrossDomainCalls(Uri domain, Uri[] allowedCallers)
        {
            var policyUrl = ServiceGarden.AllowSilverlightCrossDomainCalls(domain, allowedCallers);
            listView1.Items.Add(new ListViewItem(new[] {"Cross-Domain Access Policy", "Started", policyUrl, "HTTP-GET", "n/a", "n/a"})
            {
                ImageIndex = 0,
                Tag = new ServiceInfoWrapper
                {
                    Protocol = "HTTP-GET",
                    Url = policyUrl,
                    Port = 80
                }
            });
        }

        private readonly ListViewColumnSorter _sorter = new ListViewColumnSorter();

        /// <summary>
        /// Handles the ColumnClick event of the OperationsList control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Forms.ColumnClickEventArgs"/> instance containing the event data.</param>
        private void listView1_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            // Determine if clicked column is already the column that is being sorted.
            if (e.Column == _sorter.SortColumn)
                // Reverse the current sort direction for this column.
                _sorter.Order = _sorter.Order == SortOrder.Ascending ? SortOrder.Descending : SortOrder.Ascending;
            else
            {
                // Set the column number that is to be sorted; default to ascending.
                _sorter.SortColumn = e.Column;
                _sorter.Order = SortOrder.Ascending;
            }

            // Perform the sort with these new sort options.
            listView1.Sort();
        }

        /// <summary>
        /// This class is an implementation of the 'IComparer' interface.
        /// </summary>
        private class ListViewColumnSorter : IComparer
        {
            /// <summary>
            /// Case insensitive comparer object
            /// </summary>
            private readonly CaseInsensitiveComparer _objectCompare;

            /// <summary>
            /// Class constructor.  Initializes various elements
            /// </summary>
            public ListViewColumnSorter()
            {
                SortColumn = -1;
                Order = SortOrder.None;
                _objectCompare = new CaseInsensitiveComparer();
            }

            /// <summary>
            /// This method is inherited from the IComparer interface.  It compares the two objects passed using a case insensitive comparison.
            /// </summary>
            /// <param name="x">First object to be compared</param>
            /// <param name="y">Second object to be compared</param>
            /// <returns>The result of the comparison. "0" if equal, negative if 'x' is less than 'y' and positive if 'x' is greater than 'y'</returns>
            public int Compare(object x, object y)
            {
                if (SortColumn == -1) return 0;

                // Cast the objects to be compared to ListViewItem objects
                var listviewX = (ListViewItem) x;
                var listviewY = (ListViewItem) y;

                // Compare the two items
                var compareResult = _objectCompare.Compare(listviewX.SubItems[SortColumn].Text, listviewY.SubItems[SortColumn].Text);

                // Calculate correct return value based on object comparison
                if (Order == SortOrder.Ascending)
                    // Ascending sort is selected, return normal result of compare operation
                    return compareResult;
                return Order == SortOrder.Descending ? -compareResult : 0;
            }

            /// <summary>
            /// Gets or sets the number of the column to which to apply the sorting operation (Defaults to '0').
            /// </summary>
            public int SortColumn { get; set; }

            /// <summary>
            /// Gets or sets the order of sorting to apply (for example, 'Ascending' or 'Descending').
            /// </summary>
            public SortOrder Order { get; set; }
        }

        /// <summary>
        /// Logs the specified event (text).
        /// </summary>
        /// <param name="logEvent">The event (text).</param>
        /// <param name="type">The event type.</param>
        public void Log(string logEvent, LogEventType type)
        {
            if (logEvent.StartsWith("Successfully started ") && type == LogEventType.Information) return; // We are already showing service start information in the main area

            BeginInvoke(new Action(() =>
            {
                var logItem = new ListViewItem();
                switch (type)
                {
                    case LogEventType.Critical:
                        logItem.ImageIndex = 1;
                        logItem.Text = "Critical";
                        break;
                    case LogEventType.Error:
                        logItem.ImageIndex = 1;
                        logItem.Text = "Error";
                        break;
                    case LogEventType.Exception:
                        logItem.ImageIndex = 6;
                        logItem.Text = "Exception";
                        break;
                    case LogEventType.Information:
                        logItem.ImageIndex = 2;
                        logItem.Text = "Information";
                        break;
                    case LogEventType.Success:
                        logItem.ImageIndex = 5;
                        logItem.Text = "Success";
                        break;
                    case LogEventType.Undefined:
                        logItem.ImageIndex = 0;
                        logItem.Text = "Undefined";
                        break;
                    case LogEventType.Warning:
                        logItem.ImageIndex = 4;
                        logItem.Text = "Critical";
                        break;
                }
                logItem.SubItems.Add(DateTime.Now.ToLongTimeString());
                logItem.SubItems.Add(logEvent);
                lock (listView2)
                {
                    listView2.Items.Add(logItem);
                    listView2.EnsureVisible(listView2.Items.Count - 1);
                }
                Application.DoEvents();
            }));
        }

        /// <summary>
        /// Logs the specified event (object).
        /// </summary>
        /// <param name="logEvent">The event (object).</param>
        /// <param name="type">The event type.</param>
        public void Log(object logEvent, LogEventType type)
        {
            Log(logEvent.ToString(), type);
        }

        /// <summary>
        /// Gets or sets the type filter.
        /// </summary>
        /// <value>The type filter.</value>
        /// <remarks>
        /// Only events that match the type filter will be considered by this logger.
        /// </remarks>
        public LogEventType TypeFilter { get; set; }

        /// <summary>
        /// We will attempt to bring up more information about the double-clicked item
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event arguments</param>
        private void listView1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (listView1.SelectedItems.Count == 0) return;

            var item = listView1.SelectedItems[0];
            var info = item.Tag as ServiceInfoWrapper;
            if (info == null)
            {
                MessageBox.Show("No action is associated with the selected service host.");
                return;
            }

            switch (info.Protocol.ToLower())
            {
                case "http-get":
                    Process.Start(info.Url);
                    break;
                case "rest http (xml)":
                case "rest http (json)":
                case "basic http":
                case "ws http":
                case "net.tcp":
                    var dlg = new ServiceTestHarness();
                    dlg.ShowService(info);
                    dlg.Show();
                    break;
                default:
                    MessageBox.Show("No action is associated with this type of service host (" + info.Protocol + ").");
                    break;
            }
        }

        /// <summary>
        /// Handles the DoubleClick event of the listView2 control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void LogItemDoubleClick(object sender, EventArgs e)
        {
            if (listView2.SelectedItems.Count > 0)
                foreach (ListViewItem item in listView2.SelectedItems)
                    if (item.SubItems.Count > 2)
                        ShowMessageDetail(item.SubItems[2].Text);
        }

        /// <summary>
        /// Displays a window with detailed exception information
        /// </summary>
        /// <param name="text">The text to display.</param>
        private void ShowMessageDetail(string text)
        {
            var window = new Form {Text = "Log Detail", Width = 750, Height = 650};
            var textbox = new TextBox
            {
                Multiline = true,
                AcceptsReturn = true,
                AcceptsTab = true,
                ReadOnly = true,
                Dock = DockStyle.Fill,
                Text = text,
                BackColor = Color.White,
                ScrollBars = ScrollBars.Both,
                Font = new Font(new FontFamily("Consolas"), 11),
                WordWrap = false
            };
            textbox.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.A && e.Modifiers == Keys.Control)
                {
                    e.Handled = true;
                    textbox.SelectAll();
                }
            };
            window.Controls.Add(textbox);
            textbox.SelectionStart = 0;
            textbox.SelectionLength = 0;
            window.Show();
        }
    }

    /// <summary>
    /// Service info wrapper
    /// </summary>
    public class ServiceInfoWrapper
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceInfoWrapper"/> class.
        /// </summary>
        public ServiceInfoWrapper()
        {
            MessageSize = "Medium";
            WsdlExposed = "n/a";
            Port = 80; // HTTP Default
        }

        /// <summary>
        /// Gets or sets the type of the service.
        /// </summary>
        /// <value>The type of the service.</value>
        public Type ServiceType { get; set; }

        /// <summary>
        /// Gets or sets the service contract.
        /// </summary>
        /// <value>The service contract.</value>
        public Type ServiceContract { get; set; }

        /// <summary>
        /// Gets or sets the URL.
        /// </summary>
        /// <value>The URL.</value>
        public string Url { get; set; }

        /// <summary>
        /// Gets or sets the Protocol.
        /// </summary>
        /// <value>The Protocol.</value>
        public string Protocol { get; set; }

        /// <summary>
        /// Actual service host
        /// </summary>
        public ServiceHost Host { get; set; }

        /// <summary>
        /// Supported Message Size
        /// </summary>
        public string MessageSize { get; set; }

        /// <summary>
        /// Information about whether a WSDL file is exposed or not
        /// </summary>
        public string WsdlExposed { get; set; }

        /// <summary>
        /// Service Port
        /// </summary>
        public int Port { get; set; }
    }
}
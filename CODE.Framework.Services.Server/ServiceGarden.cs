using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel.Channels;
using CODE.Framework.Core.Configuration;
using CODE.Framework.Core.Utilities;
using CODE.Framework.Services.Server.Contracts;

namespace CODE.Framework.Services.Server
{
    /// <summary>
    /// Collection ('garden') of hosts for a list of service hosts
    /// </summary>
    public static class ServiceGarden
    {
        /// <summary>Static constructor</summary>
        static ServiceGarden()
        {
            BaseUrl = GetSetting("ServiceBaseUrl");
            BasePort = GetSettingInt("ServiceBasePort", defaultValue: -1);
            BasePath = GetSetting("ServiceBasePath");

            HttpCrossDomainCallsAllowed = false;
            HttpCrossDomainCallsAllowedFrom = string.Empty;
        }

        /// <summary>
        /// Base URL for the service
        /// </summary>
        public static string BaseUrl { get; set; }

        /// <summary>
        /// First used port number for the hosted services (each subsequent service will increase the listening port by 1)
        /// </summary>
        public static int BasePort { get; set; }

        private static SecurityMode GetSecurityMode(string interfaceName)
        {
            var mode = GetSetting("ServiceSecurityMode", interfaceName).ToLower();
            switch (mode)
            {
                case "none":
                    return SecurityMode.None;
                case "message":
                    return SecurityMode.Message;
                case "transport":
                    return SecurityMode.Transport;
                case "transportwithmessagecredential":
                    return SecurityMode.TransportWithMessageCredential;
            }
            return SecurityMode.None;
        }

        private static MessageSize GetMessageSize(string interfaceName)
        {
            var size = GetSetting("ServiceMessageSize", interfaceName).ToLower();
            switch (size)
            {
                case "normal":
                    return MessageSize.Normal;
                case "medium":
                    return MessageSize.Medium;
                case "large":
                    return MessageSize.Large;
                case "verylarge":
                    return MessageSize.VeryLarge;
                case "max":
                    return MessageSize.Max;
            }
            return MessageSize.Medium;
        }

        /// <summary>
        /// Gets or sets the base path ("virtual directory")
        /// </summary>
        /// <value>The base path.</value>
        public static string BasePath { get; set; }

        /// <summary>
        /// Service hosts
        /// </summary>
        private static readonly Dictionary<string, HostWrapper> Hosts = new Dictionary<string, HostWrapper>();

        /// <summary>
        /// Returns the host by its full address
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static ServiceHost GetHostByEndpointAddress(string address)
        {
            return Hosts.ContainsKey(address) ? Hosts[address].Host : null;
        }

        /// <summary>
        /// Creates the service host.
        /// </summary>
        /// <param name="serviceType">Type of the service.</param>
        /// <param name="exposeWsdl">if set to <c>true</c> a WSDL endpoint is exposed.</param>
        /// <returns>
        /// Service URL if successful. Empty string otherwise.
        /// </returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "This is specifically designed to be a catch-all.")]
        public static string AddServiceHostBasicHttp(Type serviceType, bool exposeWsdl)
        {
            return AddServiceHostBasicHttp(serviceType, null, exposeWsdl);
        }
        /// <summary>
        /// Tries to create a service host and logs appropriate information
        /// </summary>
        /// <param name="serviceType">Type of the service.</param>
        /// <param name="exposeWsdl">if set to <c>true</c> a WSDL endpoint is exposed.</param>
        /// <returns>
        /// Service URL if successful. Empty string otherwise.
        /// </returns>
        /// <remarks>Uses the LoggingMediator class to log information. Configure LoggingMediator accordingly.</remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "This is specifically designed to be a catch-all.")]
        public static string TryAddServiceHostBasicHttp(Type serviceType, bool exposeWsdl)
        {
            return TryAddServiceHostBasicHttp(serviceType, null, exposeWsdl);
        }

        /// <summary>
        /// Returns the contract type (service interface/contract) for a given service implementation.
        /// </summary>
        /// <remarks>
        /// This only works if there is only a single interface that is implemented by the service.
        /// </remarks>
        /// <param name="serviceType">Service type (implementation)</param>
        /// <returns>Contract type or IndexOutOfBoundsException is raised if service contract can't be identified</returns>
        private static Type GetContractTypeFromServiceType(Type serviceType)
        {
            Type contractType;
            var interfaces = serviceType.GetInterfaces();
            if (interfaces.Length == 1) contractType = interfaces[0];
            else throw new Core.Exceptions.IndexOutOfBoundsException("Service contract cannot be automatically determined for the specified service type.");
            return contractType;
        }

        /// <summary>
        /// Returns a setting specific for a contract, or the generic setting, when a setting for the contract is not found.
        /// </summary>
        /// <param name="setting">The setting.</param>
        /// <param name="contractName">Name of the contract.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns>System.String.</returns>
        private static string GetSetting(string setting, string contractName = "", string defaultValue = "")
        {
            if (!string.IsNullOrEmpty(contractName))
                if (ConfigurationSettings.Settings.IsSettingSupported(setting + ":" + contractName))
                    return ConfigurationSettings.Settings[setting + ":" + contractName];
            if (ConfigurationSettings.Settings.IsSettingSupported(setting))
                return ConfigurationSettings.Settings[setting];
            return defaultValue;
        }

        /// <summary>
        /// Returns a setting specific (as integer) for a contract, or the generic setting, when a setting for the contract is not found.
        /// </summary>
        /// <param name="setting">The setting.</param>
        /// <param name="contractName">Name of the contract.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns>System.String.</returns>
        private static int GetSettingInt(string setting, string contractName = "", int defaultValue = 0)
        {
            var settingValue = GetSetting(setting, contractName);
            if (string.IsNullOrEmpty(settingValue)) return defaultValue;

            int settingNumber;
            return !int.TryParse(settingValue, out settingNumber) ? defaultValue : settingNumber;
        }

        /// <summary>
        /// Creates and configures the service host.
        /// </summary>
        /// <param name="serviceType">Type of the service.</param>
        /// <param name="contractType">Type of the contract.</param>
        /// <param name="addresses">The addresses.</param>
        /// <returns>ServiceHost.</returns>
        private static ServiceHost CreateServiceHost(Type serviceType, Type contractType, Uri[] addresses)
        {
            var host = new ServiceHost(serviceType, addresses);

            // Setting concurrency
            if (contractType != null)
            {
                var setConcurrency = true;
                var customAttributes = serviceType.GetCustomAttributes(typeof (ServiceBehaviorAttribute), true);
                if (customAttributes.Length > 0)
                    setConcurrency = false; // The attribute is set, but we are not sure whether the concurrency mode is still the default or not. But we have to assume it was set and thus not mess with concurrency manually.

                var serviceId = contractType.Name;
                var concurrencyMode = GetSetting("ServiceConcurrencyMode", serviceId);
                if (!string.IsNullOrEmpty(concurrencyMode)) setConcurrency = true;

                var maxConcurrentSessions = GetSettingInt("ServiceMaxConcurrentSessions", serviceId, 10);
                var maxConcurrentCalls = GetSettingInt("ServiceMaxConcurrentCalls", serviceId, 10);

                if (setConcurrency)
                {
                    var throttleBehaviorFound = false;
                    var serviceBehaviorFound = false;

                    switch (concurrencyMode)
                    {
                        case "single":
                            foreach (var behavior in host.Description.Behaviors)
                            {
                                var behaviorAttribute = behavior as ServiceBehaviorAttribute;
                                if (behaviorAttribute != null)
                                {
                                    serviceBehaviorFound = true;
                                    behaviorAttribute.ConcurrencyMode = ConcurrencyMode.Single;
                                    behaviorAttribute.InstanceContextMode = InstanceContextMode.Single;
                                    behaviorAttribute.MaxItemsInObjectGraph = int.MaxValue;
                                }
                            }

                            if (!serviceBehaviorFound)
                                host.Description.Behaviors.Add(new ServiceBehaviorAttribute { ConcurrencyMode = ConcurrencyMode.Single, InstanceContextMode = InstanceContextMode.Single });

                            break;

                        case "reentrant":
                            foreach (var behavior in host.Description.Behaviors)
                            {
                                var behaviorAttribute = behavior as ServiceBehaviorAttribute;
                                if (behaviorAttribute != null)
                                {
                                    serviceBehaviorFound = true;
                                    behaviorAttribute.ConcurrencyMode = ConcurrencyMode.Reentrant;
                                    behaviorAttribute.InstanceContextMode = InstanceContextMode.Single;
                                    behaviorAttribute.MaxItemsInObjectGraph = int.MaxValue;
                                }
                            }

                            if (!serviceBehaviorFound)
                                host.Description.Behaviors.Add(new ServiceBehaviorAttribute {ConcurrencyMode = ConcurrencyMode.Reentrant, InstanceContextMode = InstanceContextMode.Single});

                            break;

                        default: // multiple
                            foreach (var behavior in host.Description.Behaviors)
                            {
                                var behaviorAttribute = behavior as ServiceBehaviorAttribute;
                                if (behaviorAttribute != null)
                                {
                                    serviceBehaviorFound = true;
                                    behaviorAttribute.ConcurrencyMode = ConcurrencyMode.Multiple;
                                    behaviorAttribute.InstanceContextMode = InstanceContextMode.Single;
                                    behaviorAttribute.MaxItemsInObjectGraph = int.MaxValue;
                                }

                                var throttlingBehavior = behavior as ServiceThrottlingBehavior;
                                if (throttlingBehavior != null)
                                {
                                    throttleBehaviorFound = true;
                                    throttlingBehavior.MaxConcurrentCalls = maxConcurrentCalls;
                                    throttlingBehavior.MaxConcurrentSessions = maxConcurrentSessions;
                                }
                            }

                            if (!serviceBehaviorFound)
                                host.Description.Behaviors.Add(new ServiceBehaviorAttribute {ConcurrencyMode = ConcurrencyMode.Multiple, InstanceContextMode = InstanceContextMode.Single, MaxItemsInObjectGraph = int.MaxValue});
                            if (!throttleBehaviorFound)
                                host.Description.Behaviors.Add(new ServiceThrottlingBehavior {MaxConcurrentSessions = maxConcurrentSessions, MaxConcurrentCalls = maxConcurrentCalls});

                            break;
                    }
                }
            }

            return host;
        }

        /// <summary>
        /// Creates the service host.
        /// </summary>
        /// <param name="serviceType">Type of the service.</param>
        /// <param name="contractType">Type of the contract.</param>
        /// <param name="exposeWsdl">if set to <c>true</c> a WSDL endpoint is exposed.</param>
        /// <returns>
        /// Service URL if successful. Empty string otherwise.
        /// </returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "This is specifically designed to be a catch-all.")]
        public static string AddServiceHostBasicHttp(Type serviceType, Type contractType, bool exposeWsdl)
        {
            return AddServiceHostBasicHttp(serviceType, contractType, null, MessageSize.Undefined, exposeWsdl);
        }
        /// <summary>
        /// Tries to create the service host and logs appropriate information.
        /// </summary>
        /// <param name="serviceType">Type of the service.</param>
        /// <param name="contractType">Type of the contract.</param>
        /// <param name="exposeWsdl">if set to <c>true</c> a WSDL endpoint is exposed.</param>
        /// <returns>
        /// Service URL if successful. Empty string otherwise.
        /// </returns>
        /// <remarks>Uses the LoggingMediator class to log information. Configure LoggingMediator accordingly.</remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "This is specifically designed to be a catch-all.")]
        public static string TryAddServiceHostBasicHttp(Type serviceType, Type contractType, bool exposeWsdl)
        {
            return TryAddServiceHostBasicHttp(serviceType, contractType, null, MessageSize.Undefined, exposeWsdl);
        }

        /// <summary>
        /// Creates the service host.
        /// </summary>
        /// <param name="serviceType">Type of the service.</param>
        /// <param name="contractType">Type of the contract.</param>
        /// <param name="serviceId">The service id (generally, 'virtual directory' part of the service URL).</param>
        /// <param name="exposeWsdl">if set to <c>true</c> [expose WSDL].</param>
        /// <returns>
        /// Service URL if successful. Empty string otherwise.
        /// </returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "This is specifically designed to be a catch-all.")]
        public static string AddServiceHostBasicHttp(Type serviceType, Type contractType, string serviceId, bool exposeWsdl)
        {
            return AddServiceHostBasicHttp(serviceType, contractType, serviceId, MessageSize.Undefined, exposeWsdl);
        }
        /// <summary>
        /// Tries to create the service host and logs appropriate information.
        /// </summary>
        /// <param name="serviceType">Type of the service.</param>
        /// <param name="contractType">Type of the contract.</param>
        /// <param name="serviceId">The service id (generally, 'virtual directory' part of the service URL).</param>
        /// <param name="exposeWsdl">if set to <c>true</c> [expose WSDL].</param>
        /// <returns>
        /// Service URL if successful. Empty string otherwise.
        /// </returns>
        /// <remarks>Uses the LoggingMediator class to log information. Configure LoggingMediator accordingly.</remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "This is specifically designed to be a catch-all.")]
        public static string TryAddServiceHostBasicHttp(Type serviceType, Type contractType, string serviceId, bool exposeWsdl)
        {
            return TryAddServiceHostBasicHttp(serviceType, contractType, serviceId, MessageSize.Undefined, exposeWsdl);
        }

        /// <summary>
        /// For internal use. Memorizes the port offset for port-based services
        /// </summary>
        private static int _portOffset;

        /// <summary>
        /// Creates a TCP/IP (net.tcp) service host
        /// </summary>
        /// <param name="serviceType">Type of the service.</param>
        /// <param name="contractType">Type of the contract.</param>
        /// <param name="serviceId">The service id (generally, 'virtual directory' part of the service URL).</param>
        /// <param name="messageSize">Potential size of the message. (Should be large if the payload could potentially be more than an MB).</param>
        /// <param name="port">The port (-1 = use base port).</param>
        /// <returns>Service URL if successful. Empty string otherwise.</returns>
        /// <exception cref="Core.Exceptions.NullReferenceException">
        /// Static BaseUrl property must be set on the ServiceGarden class before the garden can be populated.
        /// or
        /// Static BasePort property must be set on the ServiceGarden class before the garden can be populated.
        /// </exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "This is specifically designed to be a catch-all.")]
        public static string AddServiceHostNetTcp(Type serviceType, Type contractType = null, string serviceId = null, MessageSize messageSize = MessageSize.Undefined, int port = -1)
        {
            // Before we do anything else, we start looking for optional parameters and setting appropriate defaults
            if (contractType == null) contractType = GetContractTypeFromServiceType(serviceType);
            if (serviceId == null) serviceId = contractType.Name;
            if (messageSize == MessageSize.Undefined) messageSize = GetMessageSize(contractType.Name);

            // Starting our regular method
            var baseAddress = BaseUrl;
            if (string.IsNullOrEmpty(baseAddress)) throw new Core.Exceptions.NullReferenceException("Static BaseUrl property must be set on the ServiceGarden class before the garden can be populated.");
            if (BasePort == -1) throw new Core.Exceptions.NullReferenceException("Static BasePort property must be set on the ServiceGarden class before the garden can be populated.");
            if (port == -1)
            {
                port = BasePort;
                port += _portOffset;
                _portOffset++;
            }
            var path = BasePath;
            if (!string.IsNullOrEmpty(path) && !path.EndsWith("/")) path += "/";
            var serviceFullAddress = "net.tcp://" + baseAddress + ":" + port + "/" + path + serviceId;
            var serviceNamespace = GetServiceNamespace(serviceType, contractType);

            var addresses = new[] { new Uri(serviceFullAddress) };
            var host = CreateServiceHost(serviceType, contractType, addresses);

            // Binding needed
            var securityMode = GetSecurityMode(contractType.Name);
            var binding = new NetTcpBinding(securityMode) { SendTimeout = new TimeSpan(0, 10, 0) };
            if (!string.IsNullOrEmpty(serviceNamespace)) binding.Namespace = serviceNamespace;
            ServiceHelper.ConfigureMessageSizeOnNetTcpBinding(messageSize, binding);

            // Endpoint configuration
            var beforeEndpointAdded = BeforeEndpointAdded;
            if (beforeEndpointAdded != null)
            {
                var endpointAddedArgs = new EndpointAddedEventArgs {Binding = binding, ContractType = contractType, ServiceFullAddress = serviceFullAddress};
                beforeEndpointAdded(null, endpointAddedArgs);
                serviceFullAddress = endpointAddedArgs.ServiceFullAddress;
            }
            var endpoint = host.AddServiceEndpoint(contractType, binding, serviceFullAddress);
            if (!string.IsNullOrEmpty(serviceNamespace) && endpoint.Contract != null && endpoint.Contract.Namespace != serviceNamespace) endpoint.Contract.Namespace = serviceNamespace;

            var serviceKey = serviceFullAddress;
            if (Hosts.ContainsKey(serviceKey))
            {
                if (Hosts[serviceKey].Host.State == CommunicationState.Opened)
                {
                    try
                    {
                        Hosts[serviceKey].Host.Close();
                    }
                    catch { } // Nothing we can do
                }
                Hosts.Remove(serviceKey);
            }

            // We check whether the service we host has a custom service behavior attribute. If not, we make it a per-call service
            var customAttributes = serviceType.GetCustomAttributes(typeof(ServiceBehaviorAttribute), true);
            if (customAttributes.Length == 0) // No explicitly set behavior attribute found
            {
                var serviceBehaviorFound = false;
                foreach (var behavior in host.Description.Behaviors)
                {
                    var serviceBehavior = behavior as ServiceBehaviorAttribute;
                    if (serviceBehavior != null)
                    {
                        serviceBehavior.InstanceContextMode = InstanceContextMode.PerCall;
                        serviceBehaviorFound = true;
                        break;
                    }
                }
                if (!serviceBehaviorFound)
                    host.Description.Behaviors.Add(new ServiceBehaviorAttribute { InstanceContextMode = InstanceContextMode.PerCall });
            }

            // If needed, we fire the static event, so people can tie into this to programmatically manipulate the host or binding if need be);
            if (BeforeHostAdded != null) BeforeHostAdded(null, new HostAddedEventArgs
                                                       {
                                                           Host = host,
                                                           ServiceFullAddress = serviceFullAddress,
                                                           Binding = binding,
                                                           ContractType = contractType,
                                                           MessageSize = messageSize,
                                                           ServiceId = serviceId,
                                                           ServiceType = serviceType
                                                       });

            lock (Hosts)
                Hosts.Add(serviceKey, new HostWrapper(host, serviceFullAddress));

            return StartService(serviceKey);
        }

        /// <summary>
        /// Fires before a new host is added (can be used to manipulate the host before it is opened)
        /// </summary>
        public static event EventHandler<HostAddedEventArgs> BeforeHostAdded;

        /// <summary>
        /// Fires before a new endpoint is added (can be used to manipulate the host before it is opened)
        /// </summary>
        public static event EventHandler<EndpointAddedEventArgs> BeforeEndpointAdded;

        /// <summary>Tries to create the service host and logs appropriate information</summary>
        /// <param name="serviceType">Type of the service.</param>
        /// <param name="contractType">Type of the contract.</param>
        /// <param name="serviceId">The service id (generally, 'virtual directory' part of the service URL).</param>
        /// <param name="messageSize">Potential size of the message. (Should be large if the payload could potentially be more than an MB).</param>
        /// <returns>Service URL if successful. Empty string otherwise.</returns>
        /// <remarks>Uses the LoggingMediator class to log information. Configure LoggingMediator accordingly.</remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "This is specifically designed to be a catch-all.")]
        public static string TryAddServiceHostNetTcp(Type serviceType, Type contractType = null, string serviceId = null, MessageSize messageSize = MessageSize.Undefined)
        {
            try
            {
                var url = AddServiceHostNetTcp(serviceType, contractType, serviceId, messageSize);
                if (contractType == null) contractType = GetContractTypeFromServiceType(serviceType);
                if (!string.IsNullOrEmpty(url))
                {
                    var logText = "Successfully started Net.Tcp Service '" + serviceType + "' (contract: '" + contractType + "').\r\n\r\nFull URL: " + url;
                    logText += "\r\nMessage Size: " + messageSize;
                    LoggingMediator.Log(logText);
                    
                }
                else
                    LoggingMediator.Log("Error starting Net.Tcp Service '" + serviceType + "' (contract: '" + contractType + "').", LogEventType.Error);
                return url;
            }
            catch (Exception ex)
            {
                if (contractType == null) contractType = GetContractTypeFromServiceType(serviceType);
                LoggingMediator.Log("Error starting Net.Tcp Service '" + serviceType + "' (contract: '" + contractType + "').", ex, LogEventType.Error);
                return string.Empty;
            }
        }

        /// <summary>
        /// Creates the service host.
        /// </summary>
        /// <param name="serviceType">Type of the service.</param>
        /// <param name="messageSize">Potential size of the message. (Should be large if the payload could potentially be more than an MB).</param>
        /// <param name="exposeWsdl">if set to <c>true</c> [expose WSDL].</param>
        /// <param name="baseAddress">Base service address (such as www.domain.com)</param>
        /// <param name="basePath">Base path (such as "MyService" to create www.domain.com/MyService/basic)</param>
        /// <returns>
        /// Service URL if successful. Empty string otherwise.
        /// </returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "This is specifically designed to be a catch-all.")]
        public static string AddServiceHostBasicHttp(Type serviceType, MessageSize messageSize, bool exposeWsdl, string baseAddress, string basePath)
        {
            return AddServiceHostBasicHttp(serviceType, null, null, messageSize, exposeWsdl, baseAddress, basePath);
        }
        /// <summary>
        /// Tries to create the service host.
        /// </summary>
        /// <param name="serviceType">Type of the service.</param>
        /// <param name="messageSize">Potential size of the message. (Should be large if the payload could potentially be more than an MB).</param>
        /// <param name="exposeWsdl">if set to <c>true</c> [expose WSDL].</param>
        /// <param name="baseAddress">Base service address (such as www.domain.com)</param>
        /// <param name="basePath">Base path (such as "MyService" to create www.domain.com/MyService/basic)</param>
        /// <returns>
        /// Service URL if successful. Empty string otherwise.
        /// </returns>
        /// <remarks>Uses the LoggingMediator class to log information. Configure LoggingMediator accordingly.</remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "This is specifically designed to be a catch-all.")]
        public static string TryAddServiceHostBasicHttp(Type serviceType, MessageSize messageSize, bool exposeWsdl, string baseAddress, string basePath)
        {
            return TryAddServiceHostBasicHttp(serviceType, null, null, messageSize, exposeWsdl, baseAddress, basePath);
        }

        /// <summary>
        /// Creates the service host.
        /// </summary>
        /// <param name="serviceType">Type of the service.</param>
        /// <param name="contractType">Type of the contract.</param>
        /// <param name="messageSize">Potential size of the message. (Should be large if the payload could potentially be more than an MB).</param>
        /// <param name="exposeWsdl">if set to <c>true</c> [expose WSDL].</param>
        /// <param name="baseAddress">Base service address (such as www.domain.com)</param>
        /// <param name="basePath">Base path (such as "MyService" to create www.domain.com/MyService/basic)</param>
        /// <returns>
        /// Service URL if successful. Empty string otherwise.
        /// </returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "This is specifically designed to be a catch-all.")]
        public static string AddServiceHostBasicHttp(Type serviceType, Type contractType, MessageSize messageSize, bool exposeWsdl, string baseAddress, string basePath)
        {
            return AddServiceHostBasicHttp(serviceType, contractType, null, messageSize, exposeWsdl, baseAddress, basePath);
        }
        /// <summary>
        /// Tries to create the service host.
        /// </summary>
        /// <param name="serviceType">Type of the service.</param>
        /// <param name="contractType">Type of the contract.</param>
        /// <param name="messageSize">Potential size of the message. (Should be large if the payload could potentially be more than an MB).</param>
        /// <param name="exposeWsdl">if set to <c>true</c> [expose WSDL].</param>
        /// <param name="baseAddress">Base service address (such as www.domain.com)</param>
        /// <param name="basePath">Base path (such as "MyService" to create www.domain.com/MyService/basic)</param>
        /// <returns>
        /// Service URL if successful. Empty string otherwise.
        /// </returns>
        /// <remarks>Uses the LoggingMediator class to log information. Configure LoggingMediator accordingly.</remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "This is specifically designed to be a catch-all.")]
        public static string TryAddServiceHostBasicHttp(Type serviceType, Type contractType, MessageSize messageSize, bool exposeWsdl, string baseAddress, string basePath)
        {
            return TryAddServiceHostBasicHttp(serviceType, contractType, null, messageSize, exposeWsdl, baseAddress, basePath);
        }

        /// <summary>
        /// determines the namespace for a given type
        /// </summary>
        /// <param name="serviceType">Type of the service.</param>
        /// <param name="contractType">Type of the contract.</param>
        /// <returns>System.String.</returns>
        private static string GetServiceNamespace(Type serviceType, Type contractType)
        {
            string defaultNamespace = null;

            var attributes = contractType.GetCustomAttributes(typeof (ServiceContractAttribute), true);
            if (attributes.Length > 0)
            {
                var serviceContract = attributes[0] as ServiceContractAttribute;
                if (serviceContract != null)
                {
                    var ns = serviceContract.Namespace;
                    if (!string.IsNullOrEmpty(ns))
                        defaultNamespace = ns;
                }
            }

            var attributes2 = serviceType.GetCustomAttributes(typeof(ServiceBehaviorAttribute), true);
            if (attributes2.Length > 0)
            {
                var serviceBehavior = attributes2[0] as ServiceBehaviorAttribute;
                if (serviceBehavior != null)
                {
                    var ns = serviceBehavior.Namespace;
                    if (!string.IsNullOrEmpty(ns))
                        defaultNamespace = ns;
                    else
                        serviceBehavior.Namespace = defaultNamespace;
                }
            }

            return defaultNamespace;
        }

        /// <summary>
        /// Creates a service host for Basic HTTP (SOAP) hosting.
        /// </summary>
        /// <param name="serviceType">Type of the service.</param>
        /// <param name="contractType">Type of the contract.</param>
        /// <param name="serviceId">The service id (generally, 'virtual directory' part of the service URL).</param>
        /// <param name="messageSize">Potential size of the message. (Should be large if the payload could potentially be more than an MB).</param>
        /// <param name="exposeWsdl">if set to <c>true</c> [expose WSDL].</param>
        /// <param name="baseAddress">Base service address (such as www.domain.com)</param>
        /// <param name="basePath">Base path (such as "MyService" to create www.domain.com/MyService/basic)</param>
        /// <param name="extension">Path extension for basic HTTP services (such as "basic" to create www.domain.com/MyService/basic)</param>
        /// <param name="useHttps">Indicates whether HTTPS should be used</param>
        /// <returns>Service URL if successful. Empty string otherwise.</returns>
        /// <exception cref="Core.Exceptions.NullReferenceException">Static BaseUrl property must be set on the ServiceGarden class before the garden can be populated.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "This is specifically designed to be a catch-all.")]
        public static string AddServiceHostBasicHttp(Type serviceType, Type contractType = null, string serviceId = null, MessageSize messageSize = MessageSize.Undefined, bool exposeWsdl = false, string baseAddress = null, string basePath = null, string extension = null, bool useHttps = false)
        {
            // Before we do anything else, we start looking for optional parameters and setting appropriate defaults
            if (contractType == null) contractType = GetContractTypeFromServiceType(serviceType);
            if (baseAddress == null)
            {
                baseAddress = BaseUrl;
                if (string.IsNullOrEmpty(baseAddress)) throw new Core.Exceptions.NullReferenceException("Static BaseUrl property must be set on the ServiceGarden class before the garden can be populated.");
            }
            if (basePath == null)
            {
                basePath = BasePath;
                if (!string.IsNullOrEmpty(basePath) && !basePath.EndsWith("/")) basePath += "/";
            }
            if (extension == null)
            {
                extension = GetSetting("ServiceBasicHTTPExtension", contractType.Name, "basic");
                if (!string.IsNullOrEmpty(extension) && !extension.StartsWith("/")) extension = "/" + extension;
            }
            if (serviceId == null) serviceId = contractType.Name;
            if (messageSize == MessageSize.Undefined) messageSize = GetMessageSize(contractType.Name);

            // Starting our regular method
            if (!string.IsNullOrEmpty(basePath) && !basePath.EndsWith("/")) basePath += "/";
            if (!string.IsNullOrEmpty(extension) && !extension.StartsWith("/")) extension = "/" + extension;
            var protocol = useHttps ? "https://" : "http://";
            var serviceFullAddress = protocol + baseAddress + "/" + basePath + serviceId + extension;
            var serviceNamespace = GetServiceNamespace(serviceType, contractType);

            var addresses = new[] { new Uri(serviceFullAddress) };
            var host = CreateServiceHost(serviceType, contractType, addresses);

            // Binding needed
            var securityMode = useHttps ? BasicHttpSecurityMode.Transport : BasicHttpSecurityMode.None;
            var binding = new BasicHttpBinding(securityMode) {SendTimeout = new TimeSpan(0, 10, 0)};
            if (!string.IsNullOrEmpty(serviceNamespace)) binding.Namespace = serviceNamespace;
            ServiceHelper.ConfigureMessageSizeOnBasicHttpBinding(messageSize, binding);

            // Endpoint configuration
            var beforeEndpointAdded = BeforeEndpointAdded;
            if (beforeEndpointAdded != null)
            {
                var endpointAddedArgs = new EndpointAddedEventArgs { Binding = binding, ContractType = contractType, ServiceFullAddress = serviceFullAddress };
                beforeEndpointAdded(null, endpointAddedArgs);
                serviceFullAddress = endpointAddedArgs.ServiceFullAddress;
            }
            var endpoint = host.AddServiceEndpoint(contractType, binding, serviceFullAddress);
            if (!string.IsNullOrEmpty(serviceNamespace) && endpoint.Contract != null && endpoint.Contract.Namespace != serviceNamespace) endpoint.Contract.Namespace = serviceNamespace;
            if (HttpCrossDomainCallsAllowed) endpoint.Behaviors.Add(new CrossDomainScriptBehavior());

            // Maybe we expose a WSDL endpoint too
            if (exposeWsdl)
            {
                var wsdlFullPath = serviceFullAddress + "/wsdl";
                var smb = new ServiceMetadataBehavior { HttpGetEnabled = true, HttpGetUrl = new Uri(wsdlFullPath) };
                host.Description.Behaviors.Add(smb);
                var mexBinding = MetadataExchangeBindings.CreateMexHttpBinding();
                host.AddServiceEndpoint(typeof(IMetadataExchange), mexBinding, wsdlFullPath);
            }

            var serviceKey = serviceFullAddress;
            if (Hosts.ContainsKey(serviceKey))
            {
                if (Hosts[serviceKey].Host.State == CommunicationState.Opened)
                {
                    try
                    {
                        Hosts[serviceKey].Host.Close();
                    }
                    catch { } // Nothing we can do
                }
                Hosts.Remove(serviceKey);
            }

            // If needed, we fire the static event, so people can tie into this to programmatically manipulate the host or binding if need be
            if (BeforeHostAdded != null) BeforeHostAdded(null, new HostAddedEventArgs
                                                       {
                                                           Host = host,
                                                           ServiceFullAddress = serviceFullAddress,
                                                           Binding = binding,
                                                           ContractType = contractType,
                                                           MessageSize = messageSize,
                                                           ServiceId = serviceId,
                                                           ServiceType = serviceType
                                                       });

            lock (Hosts)
                Hosts.Add(serviceKey, new HostWrapper(host, serviceFullAddress));

            return StartService(serviceKey);
        }

        /// <summary>
        /// Attempts to create a Basic HTTP (SOAP) service host and handles and logs errors
        /// </summary>
        /// <param name="serviceType">Type of the service.</param>
        /// <param name="contractType">Type of the contract.</param>
        /// <param name="serviceId">The service id (generally, 'virtual directory' part of the service URL).</param>
        /// <param name="messageSize">Potential size of the message. (Should be large if the payload could potentially be more than an MB).</param>
        /// <param name="exposeWsdl">if set to <c>true</c> [expose WSDL].</param>
        /// <param name="baseAddress">Base service address (such as www.domain.com)</param>
        /// <param name="basePath">Base path (such as "MyService" to create www.domain.com/MyService/basic)</param>
        /// <param name="extension">Path extension for basic HTTP services (such as "basic" to create www.domain.com/MyService/basic)</param>
        /// <param name="useHttps">Indicates whether HTTPS should be used</param>
        /// <returns>Service URL if successful. Empty string otherwise.</returns>
        /// <remarks>Uses the LoggingMediator class to log information. Configure LoggingMediator accordingly.</remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "This is specifically designed to be a catch-all.")]
        public static string TryAddServiceHostBasicHttp(Type serviceType, Type contractType = null, string serviceId = null, MessageSize messageSize = MessageSize.Undefined, bool exposeWsdl = false, string baseAddress = null, string basePath = null, string extension = null, bool useHttps = false)
        {
            try
            {
                var url = AddServiceHostBasicHttp(serviceType, contractType, serviceId, messageSize, exposeWsdl, baseAddress, basePath, extension, useHttps);
                if (contractType == null) contractType = GetContractTypeFromServiceType(serviceType);
                if (!string.IsNullOrEmpty(url))
                {
                    var logText = "Successfully started Basic HTTP Service '" + serviceType + "' (contract: '" + contractType + "').\r\n\r\nFull URL: " + url;
                    logText += "\r\nExpose WSDL: " + (exposeWsdl ? "Yes" : "No");
                    logText += "\r\nMessage Size: " + messageSize;
                    LoggingMediator.Log(logText);
                }
                else
                    LoggingMediator.Log("Error starting Basic HTTP Service '" + serviceType + "' (contract: '" + contractType + "').", LogEventType.Error);
                return url;
            }
            catch (Exception ex)
            {
                if (contractType == null) contractType = GetContractTypeFromServiceType(serviceType);
                LoggingMediator.Log("Error starting Basic HTTP Service '" + serviceType + "' (contract: '" + contractType + "').", ex, LogEventType.Error);
                return string.Empty;
            }
        }

        /// <summary>
        /// Starts the service.
        /// </summary>
        /// <param name="serviceId">The service id (key) that is to be started.</param>
        /// <returns></returns>
        private static string StartService(string serviceId)
        {
            if (!Hosts.ContainsKey(serviceId)) return string.Empty;

            if (Hosts[serviceId].Host.State != CommunicationState.Opened)
            {
                try
                {
                    Hosts[serviceId].Host.Open();
                    return Hosts[serviceId].EndpointAddress;
                }
                catch (AddressAlreadyInUseException ex)
                {
                    LoggingMediator.Log("The address '" + Hosts[serviceId].EndpointAddress + "' is already in use by some other application/process. For this reason, a new host could not be established with that URL. To solve this problem, make sure all other apps using that URL are shut down. On servers, make sure you configure the machine with appropriate rights to allow self-hosting at this URL. During development, try launching Visual Studio as administrator, or manually configure your machine to allow hosting at this address. You may also want to check for other applications (such as Skype) that may use the same port to respond for inbound connections and try to reconfigure them to use a different port.\r\n\r\nException: " + ExceptionHelper.GetExceptionText(ex));
                }
                catch (Exception ex)
                {
                    LoggingMediator.Log("Unable to host service "+serviceId+" at address '" + Hosts[serviceId].EndpointAddress + "'.\r\n\r\nException: " + ExceptionHelper.GetExceptionText(ex));
                    throw;
                }
            }
            if (Hosts[serviceId].Host.State == CommunicationState.Opened)
            {
                try
                {
                    // This is in restart mode
                    Hosts[serviceId].Host.Close();
                    Hosts[serviceId].Host.Open();
                    return Hosts[serviceId].EndpointAddress;
                }
                catch (AddressAlreadyInUseException ex)
                {
                    LoggingMediator.Log("The address '" + Hosts[serviceId].EndpointAddress + "' is already in use by some other application/process. For this reason, a new host could not be established with that URL. To solve this problem, make sure all other apps using that URL are shut down. On servers, make sure you configure the machine with appropriate rights to allow self-hosting at this URL. During development, try launching Visual Studio as administrator, or manually configure your machine to allow hosting at this address.\r\n\r\nException: " + ExceptionHelper.GetExceptionText(ex));
                }
                catch (Exception ex)
                {
                    LoggingMediator.Log("Unable to host service " + serviceId + " at address '" + Hosts[serviceId].EndpointAddress + "'.\r\n\r\nException: " + ExceptionHelper.GetExceptionText(ex));
                    throw;
                }
            }
            return string.Empty;
        }

        /// <summary>
        /// Stops the service.
        /// </summary>
        /// <param name="contractType">Type of the service contract.</param>
        /// <returns>True if the service was found and closed successfully</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "This is specifically designed to be a catch-all.")]
        public static bool StopService(Type contractType)
        {
            Type interfaceType;
            if (contractType.IsInterface) interfaceType = contractType;
            else
            {
                var interfaces = contractType.GetInterfaces();
                if (interfaces.Length == 1) interfaceType = interfaces[0];
                else throw new Core.Exceptions.IndexOutOfBoundsException("Service information must be the service contract, not the service implementation type.");
            }

            var foundService = false;
            var mustContinue = true;
            while (mustContinue)
            {
                mustContinue = false;
                foreach (var host in Hosts)
                {
                    foreach (var endpoint in host.Value.Host.Description.Endpoints)
                    {
                        var serviceContractType = endpoint.Contract.ContractType;
                        if (serviceContractType == interfaceType)
                        {
                            try
                            {
                                host.Value.Host.Close();
                                Hosts.Remove(host.Key);
                            }
                            catch (Exception ex)
                            {
                                // Ignoring the exception on purpose
                                LoggingMediator.Log("Unable to stop service '" + contractType + "'.\r\n\r\n" + ExceptionHelper.GetExceptionText(ex));
                            }
                            mustContinue = true;
                            foundService = true;
                            break;
                        }
                    }
                    if (mustContinue) break;
                }
            }

            return foundService;
        }

        /// <summary>
        /// Stops the service.
        /// </summary>
        /// <param name="serviceId">The service id.</param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "This is specifically designed to be a catch-all.")]
        public static bool StopService(string serviceId)
        {
            if (!Hosts.ContainsKey(serviceId)) return true;
            try
            {
                try
                {
                    Hosts[serviceId].Host.Close();
                    Hosts.Remove(serviceId);
                    return true;
                }
                catch (Exception ex)
                {
                    // Ignoring the exception on purpose
                    LoggingMediator.Log("Unable to stop service '" + serviceId + "'.\r\n\r\n" + ExceptionHelper.GetExceptionText(ex));
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Closes all currently open hosts
        /// </summary>
        /// <returns></returns>
        public static bool StopAllServices()
        {
            foreach (var host in Hosts)
                try
                {
                    host.Value.Host.Close();
                }
                catch (Exception ex)
                {
                    // Ignoring the exception on purpose
                    LoggingMediator.Log("Unable to stop service '" + host.Value.EndpointAddress + "'.\r\n\r\n" + ExceptionHelper.GetExceptionText(ex));
                }
            Hosts.Clear();
            return true;
        }

        /// <summary>Enables Silverlight and Flash cross-domain service access</summary>
        /// <remarks>Cross-access domain calling is of particular importance for Silverlight clients.</remarks>
        /// <returns>URL of the hosted policy</returns>
        /// <example>
        /// // Enables all cross domain calls for the current service base URL
        /// ServiceGarden.AllowCrossDomainCalls();
        /// </example>
        public static string AllowSilverlightCrossDomainCalls()
        {
            var domain = GetSetting("ServiceBaseUrl");
            if (!domain.ToLower(System.Globalization.CultureInfo.InvariantCulture).StartsWith("http://")) domain = "http://" + domain;
            return AllowSilverlightCrossDomainCalls(new Uri(domain), null);
        }
        /// <summary>
        /// Tries to enable Silverlight and Flash cross-domain service access and logs information accordingly
        /// </summary>
        /// <returns>URL of the hosted policy</returns>
        /// <remarks>
        /// Cross-access domain calling is of particular importance for Silverlight clients.
        /// </remarks>
        /// <example>
        /// // Enables all cross domain calls for the current service base URL
        /// ServiceGarden.AllowCrossDomainCalls();
        /// </example>
        /// <remarks>Uses the LoggingMediator class to log information. Configure LoggingMediator accordingly.</remarks>
        public static string TryAllowSilverlightCrossDomainCalls()
        {
            var domain = GetSetting("ServiceBaseUrl");
            if (!domain.ToLower(System.Globalization.CultureInfo.InvariantCulture).StartsWith("http://")) domain = "http://" + domain;
            return TryAllowSilverlightCrossDomainCalls(new Uri(domain), null);
        }

        /// <summary>Enables Silverlight and Flash cross-domain service access</summary>
        /// <param name="allowedCaller">The allowed caller.</param>
        /// <returns>URL of the hosted policy</returns>
        /// <remarks>
        /// Cross-access domain calling is of particular importance for Silverlight clients.
        /// Note that the allowed callers are shared across all root domains if this method is called multiple times
        /// to enable different root domains. (This means that the service garden hosts service calls who's endpoints
        /// are on different domains, which rarely happens).
        /// </remarks>
        /// <example>
        /// // Enables all cross domain calls for the current service base URL
        /// // from the specified domain
        /// ServiceGarden.AllowCrossDomainCalls(new Uri("www.eps-software.com"));
        /// </example>
        public static string AllowSilverlightCrossDomainCalls(Uri allowedCaller)
        {
            var domain = GetSetting("ServiceBaseUrl");
            if (!domain.ToLower(System.Globalization.CultureInfo.InvariantCulture).StartsWith("http://")) domain = "http://" + domain;
            return AllowSilverlightCrossDomainCalls(new Uri(domain), new[] { allowedCaller });
        }
        /// <summary>
        /// Tries to enable Silverlight and Flash cross-domain service access and logs information accordingly
        /// </summary>
        /// <param name="allowedCaller">The allowed caller.</param>
        /// <returns>URL of the hosted policy</returns>
        /// <remarks>
        /// Cross-access domain calling is of particular importance for Silverlight clients.
        /// Note that the allowed callers are shared across all root domains if this method is called multiple times
        /// to enable different root domains. (This means that the service garden hosts service calls who's endpoints
        /// are on different domains, which rarely happens).
        /// </remarks>
        /// <example>
        /// // Enables all cross domain calls for the current service base URL
        /// // from the specified domain
        /// ServiceGarden.AllowCrossDomainCalls(new Uri("www.eps-software.com"));
        /// </example>
        /// <remarks>Uses the LoggingMediator class to log information. Configure LoggingMediator accordingly.</remarks>
        public static string TryAllowSilverlightCrossDomainCalls(Uri allowedCaller)
        {
            var domain = GetSetting("ServiceBaseUrl");
            if (!domain.ToLower(System.Globalization.CultureInfo.InvariantCulture).StartsWith("http://")) domain = "http://" + domain;
            return TryAllowSilverlightCrossDomainCalls(new Uri(domain), new[] { allowedCaller });
        }

        /// <summary>
        /// Enables Silverlight and Flash cross-domain service access
        /// </summary>
        /// <param name="allowedCallers">Allowed caller domains (URLs).</param>
        /// <returns>URL of the hosted policy</returns>
        /// <remarks>
        /// Cross-access domain calling is of particular importance for Silverlight clients.
        /// Note that the allowed callers are shared across all root domains if this method is called multiple times
        /// to enable different root domains. (This means that the service garden hosts service calls who's endpoints
        /// are on different domains, which rarely happens).
        /// </remarks>
        /// <example>
        /// // Enables all cross domain calls for the current service base URL
        /// // from the specified domain
        /// ServiceGarden.AllowCrossDomainCalls(new Uri[] {new Uri("www.eps-software.com"), new Uri("www.Microsoft.com")});
        /// </example>
        public static string AllowSilverlightCrossDomainCalls(Uri[] allowedCallers)
        {
            var domain = GetSetting("ServiceBaseUrl");
            if (!domain.ToLower(System.Globalization.CultureInfo.InvariantCulture).StartsWith("http://")) domain = "http://" + domain;
            return AllowSilverlightCrossDomainCalls(new Uri(domain), allowedCallers);
        }
        /// <summary>
        /// Tries to enable Silverlight and Flash cross-domain service access and logs information accordingly
        /// </summary>
        /// <param name="allowedCallers">Allowed caller domains (URLs).</param>
        /// <returns>URL of the hosted policy</returns>
        /// <remarks>
        /// Cross-access domain calling is of particular importance for Silverlight clients.
        /// Note that the allowed callers are shared across all root domains if this method is called multiple times
        /// to enable different root domains. (This means that the service garden hosts service calls who's endpoints
        /// are on different domains, which rarely happens).
        /// </remarks>
        /// <example>
        /// // Enables all cross domain calls for the current service base URL
        /// // from the specified domain
        /// ServiceGarden.AllowCrossDomainCalls(new Uri[] {new Uri("www.eps-software.com"), new Uri("www.Microsoft.com")});
        /// </example>
        /// <remarks>Uses the LoggingMediator class to log information. Configure LoggingMediator accordingly.</remarks>
        public static string TryAllowSilverlightCrossDomainCalls(Uri[] allowedCallers)
        {
            var domain = GetSetting("ServiceBaseUrl");
            if (!domain.ToLower(System.Globalization.CultureInfo.InvariantCulture).StartsWith("http://")) domain = "http://" + domain;
            return TryAllowSilverlightCrossDomainCalls(new Uri(domain), allowedCallers);
        }

        /// <summary>
        /// Enables Silverlight and Flash cross-domain service access
        /// </summary>
        /// <param name="domain">The root domain the call is valid for.</param>
        /// <returns>URL of the hosted policy</returns>
        /// <remarks>
        /// Cross-access domain calling is of particular importance for Silverlight clients.
        /// </remarks>
        /// <example>
        /// // Enables all cross domain calls from any location to www.eps-software.com hosted services
        /// ServiceGarden.AllowCrossDomainCalls("www.eps-software.com");
        /// </example>
        public static string AllowSilverlightCrossDomainCalls(string domain)
        {
            if (!domain.ToLower(System.Globalization.CultureInfo.InvariantCulture).StartsWith("http://")) domain = "http://" + domain;
            return AllowSilverlightCrossDomainCalls(new Uri(domain), null);
        }
        /// <summary>
        /// Tries to enable Flash and Silverlight cross-domain service access and logs information accordingly
        /// </summary>
        /// <param name="domain">The root domain the call is valid for.</param>
        /// <returns>URL of the hosted policy</returns>
        /// <remarks>
        /// Cross-access domain calling is of particular importance for Silverlight clients.
        /// </remarks>
        /// <example>
        /// // Enables all cross domain calls from any location to www.eps-software.com hosted services
        /// ServiceGarden.AllowCrossDomainCalls("www.eps-software.com");
        /// </example>
        /// <remarks>Uses the LoggingMediator class to log information. Configure LoggingMediator accordingly.</remarks>
        public static string TryAllowSilverlightCrossDomainCalls(string domain)
        {
            if (!domain.ToLower(System.Globalization.CultureInfo.InvariantCulture).StartsWith("http://")) domain = "http://" + domain;
            return TryAllowSilverlightCrossDomainCalls(new Uri(domain), null);
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
        /// ServiceGarden.AllowCrossDomainCalls(
        ///     new Uri("www.epsservices.net"),
        ///     new Uri[] {new Uri("www.eps-software.com"), new Uri("www.Microsoft.com")}
        ///     );
        /// </example>
        public static string AllowSilverlightCrossDomainCalls(Uri domain, Uri[] allowedCallers)
        {
            if (allowedCallers != null)
                foreach (Uri allowedCaller in allowedCallers)
                    if (!ClientAccessPolicy.AllowedCallers.Contains(allowedCaller))
                        ClientAccessPolicy.AllowedCallers.Add(allowedCaller);

            var addresses = new[] { domain };
            var host = CreateServiceHost(typeof(ClientAccessPolicy), null, addresses);

            // Endpoint configuration
            var binding = new WebHttpBinding();
            var endpoint = host.AddServiceEndpoint(typeof(IClientAccessPolicy), binding, string.Empty);
            endpoint.Behaviors.Add(new WebHttpBehavior());
            var smb = new ServiceMetadataBehavior { HttpGetEnabled = true };
            host.Description.Behaviors.Add(smb);

            const string serviceId = "__CrossDomainAccessPolicy";
            if (Hosts.ContainsKey(serviceId))
            {
                if (Hosts[serviceId].Host.State == CommunicationState.Opened)
                {
                    try
                    {
                        Hosts[serviceId].Host.Close();
                    }
                    catch { } // Nothing we can do
                }
                Hosts.Remove(serviceId);
            }

            var serviceFullAddress = domain.AbsoluteUri + "clientaccesspolicy.xml";

            // If needed, we fire the static event, so people can tie into this to programmatically manipulate the host or binding if need be
            if (BeforeHostAdded != null) BeforeHostAdded(null, new HostAddedEventArgs
            {
                Host = host,
                ServiceFullAddress = serviceFullAddress,
                Binding = binding,
                MessageSize = MessageSize.Normal,
                ServiceId = serviceId
            });

            lock (Hosts)
                Hosts.Add(serviceId, new HostWrapper(host, serviceFullAddress));

            return StartService(serviceId);
        }

        /// <summary>
        /// Tries to enablecross-domain service access and logs information accordingly
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
        /// ServiceGarden.AllowCrossDomainCalls(
        ///     new Uri("www.epsservices.net"),
        ///     new Uri[] {new Uri("www.eps-software.com"), new Uri("www.Microsoft.com")}
        ///     );
        /// </example>
        /// <remarks>Uses the LoggingMediator class to log information. Configure LoggingMediator accordingly.</remarks>
        public static string TryAllowSilverlightCrossDomainCalls(Uri domain, Uri[] allowedCallers)
        {
            try
            {
                var url = AllowSilverlightCrossDomainCalls(domain, allowedCallers);
                if (!string.IsNullOrEmpty(url))
                {
                    var logText = "Cross-Domain Call Policy established successfully for domain '" + domain + "'.\r\n\r\nFull URL: " + url + "\r\n\r\n";
                    foreach (var callerUri in allowedCallers)
                        logText += "\r\nExplicitly allowed caller: " + callerUri.AbsoluteUri;
                    LoggingMediator.Log(logText);
                }
                else
                    LoggingMediator.Log("Unable to establish Cross-Domain Call Policy for domain '" + domain + "'.", LogEventType.Error);
                return url;
            }
            catch (Exception ex)
            {
                LoggingMediator.Log("Unable to establish Cross-Domain Call Policy for domain '" + domain + "'.", ex, LogEventType.Error);
                return string.Empty;
            }
        }

        /// <summary>Tries to Enable cross-domain service access for HTTP-based callers (such as JavaScript)</summary>
        /// <returns>URL of the hosted policy</returns>
        /// <remarks>
        /// This works by adding a cross-domain call HTTP header to service responses, which is used by browsers to check if these calls should be allowed.
        /// Note that this is for JavaScript clients and this is different from the Silverlight cross domain calls (AllowCrossDomainCalls)
        /// </remarks>
        /// <example>
        /// // Enables all cross domain calls to the specified domain 
        /// // from the specified domains
        /// ServiceGarden.TryAllowHttpCrossDomainCalls();
        /// </example>
        /// <remarks>Uses the LoggingMediator class to log information. Configure LoggingMediator accordingly.</remarks>
        public static bool TryAllowHttpCrossDomainCalls()
        {
            return TryAllowHttpCrossDomainCalls("*");
        }

        /// <summary>Tries to Enable cross-domain service access for HTTP-based callers (such as JavaScript)</summary>
        /// <param name="allowedCallers">Allowed caller domains (URLs).</param>
        /// <returns>URL of the hosted policy</returns>
        /// <remarks>
        /// This works by adding a cross-domain call HTTP header to service responses, which is used by browsers to check if these calls should be allowed.
        /// Note that this is for JavaScript clients and this is different from the Silverlight cross domain calls (AllowCrossDomainCalls)
        /// </remarks>
        /// <example>
        /// // Enables all cross domain calls to the specified domain 
        /// // from the specified domains
        /// ServiceGarden.TryAllowHttpCrossDomainCalls("www.eps-software.com");
        /// </example>
        /// <remarks>Uses the LoggingMediator class to log information. Configure LoggingMediator accordingly.</remarks>
        public static bool TryAllowHttpCrossDomainCalls(string allowedCallers)
        {
            try
            {
                if (AllowHttpCrossDomainCalls(allowedCallers))
                {
                    var logText = "Script (Browser) Cross-Domain Call Policy established successfully.\r\n\r\n";
                    logText += "\r\nExplicitly allowed caller(s): " + allowedCallers;
                    LoggingMediator.Log(logText);
                    return true;
                }
                LoggingMediator.Log("Unable to establish Script (Browser) Cross-Domain Call Policy.", LogEventType.Error);
                return false;
            }
            catch (Exception ex)
            {
                LoggingMediator.Log("Unable to establish Script (Browser) Cross-Domain Call Policy.", ex, LogEventType.Error);
                return false;
            }
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
        /// ServiceGarden.AllowHttpCrossDomainCalls("www.eps-software.com");
        /// </example>
        public static bool AllowHttpCrossDomainCalls(string allowedCallers)
        {
            HttpCrossDomainCallsAllowed = true;
            HttpCrossDomainCallsAllowedFrom = allowedCallers;
            return true;
        }

        /// <summary>Enables cross-domain service access for HTTP-based callers (such as JavaScript)</summary>
        /// <returns>True if successful</returns>
        /// <remarks>
        /// This works by adding a cross-domain call HTTP header to service responses, which is used by browsers to check if these calls should be allowed.
        /// Note that this is for JavaScript clients and this is different from the Silverlight cross domain calls (AllowCrossDomainCalls)
        /// </remarks>
        /// <example>
        /// // Enables all cross domain calls to the specified domain 
        /// // from the specified domains
        /// ServiceGarden.AllowHttpCrossDomainCalls("www.eps-software.com");
        /// </example>
        public static bool AllowHttpCrossDomainCalls()
        {
            return AllowHttpCrossDomainCalls("*");
        }

        /// <summary>
        /// Indicates whether cross domain calls from script clients are allowed
        /// </summary>
        public static bool HttpCrossDomainCallsAllowed { get; private set; }
        /// <summary>
        /// Indicates the URLs cross domain calls are allowed from
        /// </summary>
        public static string HttpCrossDomainCallsAllowedFrom { get; private set; }

        /// <summary>
        /// Creates the service host.
        /// </summary>
        /// <param name="serviceType">Type of the service.</param>
        /// <param name="exposeWsdl">if set to <c>true</c> [expose WSDL].</param>
        /// <returns>
        /// Service URL if successful. Empty string otherwise.
        /// </returns>
        public static string AddServiceHostWsHttp(Type serviceType, bool exposeWsdl)
        {
            return AddServiceHostWsHttp(serviceType, null, exposeWsdl);
        }
        /// <summary>
        /// Tries to create a service host and logs appropriate information
        /// </summary>
        /// <param name="serviceType">Type of the service.</param>
        /// <param name="exposeWsdl">if set to <c>true</c> [expose WSDL].</param>
        /// <returns>
        /// Service URL if successful. Empty string otherwise.
        /// </returns>
        /// <remarks>Uses the LoggingMediator class to log information. Configure LoggingMediator accordingly.</remarks>
        public static string TryAddServiceHostWsHttp(Type serviceType, bool exposeWsdl)
        {
            return TryAddServiceHostWsHttp(serviceType, null, exposeWsdl);
        }

        /// <summary>
        /// Creates the service host.
        /// </summary>
        /// <param name="serviceType">Type of the service.</param>
        /// <param name="contractType">Type of the contract.</param>
        /// <param name="exposeWsdl">if set to <c>true</c> [expose WSDL].</param>
        /// <returns>
        /// Service URL if successful. Empty string otherwise.
        /// </returns>
        public static string AddServiceHostWsHttp(Type serviceType, Type contractType, bool exposeWsdl)
        {
            return AddServiceHostWsHttp(serviceType, contractType, null, MessageSize.Undefined, exposeWsdl);
        }
        /// <summary>
        /// Tries to create the service host and logs appropriate information.
        /// </summary>
        /// <param name="serviceType">Type of the service.</param>
        /// <param name="contractType">Type of the contract.</param>
        /// <param name="exposeWsdl">if set to <c>true</c> [expose WSDL].</param>
        /// <returns>
        /// Service URL if successful. Empty string otherwise.
        /// </returns>
        /// <remarks>Uses the LoggingMediator class to log information. Configure LoggingMediator accordingly.</remarks>
        public static string TryAddServiceHostWsHttp(Type serviceType, Type contractType, bool exposeWsdl)
        {
            return TryAddServiceHostWsHttp(serviceType, contractType, null, MessageSize.Undefined, exposeWsdl);
        }

        /// <summary>
        /// Creates the service host.
        /// </summary>
        /// <param name="serviceType">Type of the service.</param>
        /// <param name="contractType">Type of the contract.</param>
        /// <param name="serviceId">The service id (generally, 'virtual directory' part of the service URL).</param>
        /// <param name="exposeWsdl">if set to <c>true</c> [expose WSDL].</param>
        /// <returns>
        /// Service URL if successful. Empty string otherwise.
        /// </returns>
        public static string AddServiceHostWsHttp(Type serviceType, Type contractType, string serviceId, bool exposeWsdl)
        {
            return AddServiceHostWsHttp(serviceType, contractType, serviceId, MessageSize.Undefined, exposeWsdl);
        }
        /// <summary>
        /// Tries to create the service host and logs appropriate information.
        /// </summary>
        /// <param name="serviceType">Type of the service.</param>
        /// <param name="contractType">Type of the contract.</param>
        /// <param name="serviceId">The service id (generally, 'virtual directory' part of the service URL).</param>
        /// <param name="exposeWsdl">if set to <c>true</c> [expose WSDL].</param>
        /// <returns>
        /// Service URL if successful. Empty string otherwise.
        /// </returns>
        /// <remarks>Uses the LoggingMediator class to log information. Configure LoggingMediator accordingly.</remarks>
        public static string TryAddServiceHostWsHttp(Type serviceType, Type contractType, string serviceId, bool exposeWsdl)
        {
            return TryAddServiceHostWsHttp(serviceType, contractType, serviceId, MessageSize.Undefined, exposeWsdl);
        }

        /// <summary>
        /// Creates the service host.
        /// </summary>
        /// <param name="serviceType">Type of the service.</param>
        /// <param name="messageSize">Potential size of the message. (Should be large if the payload could potentially be more than an MB).</param>
        /// <param name="exposeWsdl">if set to <c>true</c> [expose WSDL].</param>
        /// <param name="baseAddress">Base service address (such as www.domain.com)</param>
        /// <param name="basePath">Base path (such as "MyService" to create www.domain.com/MyService/ws)</param>
        /// <returns>
        /// Service URL if successful. Empty string otherwise.
        /// </returns>
        public static string AddServiceHostWsHttp(Type serviceType, MessageSize messageSize, bool exposeWsdl, string baseAddress, string basePath)
        {
            return AddServiceHostWsHttp(serviceType, null, null, messageSize, exposeWsdl, baseAddress, basePath);
        }
        /// <summary>
        /// Tries to create the service host.
        /// </summary>
        /// <param name="serviceType">Type of the service.</param>
        /// <param name="messageSize">Potential size of the message. (Should be large if the payload could potentially be more than an MB).</param>
        /// <param name="exposeWsdl">if set to <c>true</c> [expose WSDL].</param>
        /// <param name="baseAddress">Base service address (such as www.domain.com)</param>
        /// <param name="basePath">Base path (such as "MyService" to create www.domain.com/MyService/ws)</param>
        /// <returns>
        /// Service URL if successful. Empty string otherwise.
        /// </returns>
        /// <remarks>Uses the LoggingMediator class to log information. Configure LoggingMediator accordingly.</remarks>
        public static string TryAddServiceHostWsHttp(Type serviceType, MessageSize messageSize, bool exposeWsdl, string baseAddress, string basePath)
        {
            return TryAddServiceHostWsHttp(serviceType, null, null, messageSize, exposeWsdl, baseAddress, basePath);
        }

        /// <summary>
        /// Creates the service host.
        /// </summary>
        /// <param name="serviceType">Type of the service.</param>
        /// <param name="contractType">Type of the contract.</param>
        /// <param name="messageSize">Potential size of the message. (Should be large if the payload could potentially be more than an MB).</param>
        /// <param name="exposeWsdl">if set to <c>true</c> [expose WSDL].</param>
        /// <param name="baseAddress">Base service address (such as www.domain.com)</param>
        /// <param name="basePath">Base path (such as "MyService" to create www.domain.com/MyService/ws)</param>
        /// <returns>
        /// Service URL if successful. Empty string otherwise.
        /// </returns>
        public static string AddServiceHostWsHttp(Type serviceType, Type contractType, MessageSize messageSize, bool exposeWsdl, string baseAddress, string basePath)
        {
            return AddServiceHostWsHttp(serviceType, contractType, null, messageSize, exposeWsdl, baseAddress, basePath);
        }

        /// <summary>
        /// Creates a WS HTTP (SOAP) service host
        /// </summary>
        /// <param name="serviceType">Type of the service.</param>
        /// <param name="contractType">Type of the contract.</param>
        /// <param name="serviceId">The service id (generally, 'virtual directory' part of the service URL).</param>
        /// <param name="messageSize">Potential size of the message. (Should be large if the payload could potentially be more than an MB).</param>
        /// <param name="exposeWsdl">if set to <c>true</c> [expose WSDL].</param>
        /// <param name="baseAddress">Base service address (such as www.domain.com)</param>
        /// <param name="basePath">Base path (such as "MyService" to create www.domain.com/MyService/ws)</param>
        /// <param name="extension">Path extension for WS HTTP services (such as "ws" to create www.domain.com/MyService/ws)</param>
        /// <param name="useHttps">Indicates whether HTTPS should be used</param>
        /// <returns>Service URL if successful. Empty string otherwise.</returns>
        /// <exception cref="Core.Exceptions.NullReferenceException">Static BaseUrl property must be set on the ServiceGarden class before the garden can be populated.</exception>
        public static string AddServiceHostWsHttp(Type serviceType, Type contractType = null, string serviceId = null, MessageSize messageSize = MessageSize.Undefined, bool exposeWsdl = false, string baseAddress = null, string basePath = null, string extension = null, bool useHttps = false)
        {
            // Before we do anything else, we start looking for optional parameters and setting appropriate defaults
            if (contractType == null) contractType = GetContractTypeFromServiceType(serviceType);
            if (baseAddress == null)
            {
                baseAddress = BaseUrl;
                if (string.IsNullOrEmpty(baseAddress)) throw new Core.Exceptions.NullReferenceException("Static BaseUrl property must be set on the ServiceGarden class before the garden can be populated.");
            }
            if (basePath == null)
            {
                basePath = BasePath;
                if (!string.IsNullOrEmpty(basePath) && !basePath.EndsWith("/")) basePath += "/";
            }
            if (extension == null)
            {
                extension = GetSetting("ServiceWsHttpExtension", contractType.Name, "ws");
                if (!string.IsNullOrEmpty(extension) && !extension.StartsWith("/")) extension = "/" + extension;
            }
            if (serviceId == null) serviceId = contractType.Name;
            if (messageSize == MessageSize.Undefined) messageSize = GetMessageSize(contractType.Name);

            // Starting our regular method
            if (!string.IsNullOrEmpty(basePath) && !basePath.EndsWith("/")) basePath += "/";
            if (!string.IsNullOrEmpty(extension) && !extension.StartsWith("/")) extension = "/" + extension;
            var protocol = useHttps ? "https://" : "http://";
            var serviceFullAddress = protocol + baseAddress + "/" + basePath + serviceId + extension;
            var serviceNamespace = GetServiceNamespace(serviceType, contractType);

            var addresses = new[] { new Uri(serviceFullAddress) };
            var host = CreateServiceHost(serviceType, contractType, addresses);

            // Binding needed
            var securityMode = useHttps ? SecurityMode.Transport : SecurityMode.None;
            var binding = new WSHttpBinding(securityMode) { SendTimeout = new TimeSpan(0, 10, 0) };
            if (!string.IsNullOrEmpty(serviceNamespace)) binding.Namespace = serviceNamespace;
            ServiceHelper.ConfigureMessageSizeOnWsHttpBinding(messageSize, binding);

            // Endpoint configuration
            var beforeEndpointAdded = BeforeEndpointAdded;
            if (beforeEndpointAdded != null)
            {
                var endpointAddedArgs = new EndpointAddedEventArgs { Binding = binding, ContractType = contractType, ServiceFullAddress = serviceFullAddress };
                beforeEndpointAdded(null, endpointAddedArgs);
                serviceFullAddress = endpointAddedArgs.ServiceFullAddress;
            }
            var endpoint = host.AddServiceEndpoint(contractType, binding, serviceFullAddress);
            if (!string.IsNullOrEmpty(serviceNamespace) && endpoint.Contract != null && endpoint.Contract.Namespace != serviceNamespace) endpoint.Contract.Namespace = serviceNamespace;
            if (HttpCrossDomainCallsAllowed) endpoint.Behaviors.Add(new CrossDomainScriptBehavior());

            // Maybe we expose a WSDL endpoint too
            if (exposeWsdl)
            {
                var wsdlFullPath = serviceFullAddress + "/wsdl";
                var smb = new ServiceMetadataBehavior { HttpGetEnabled = true, HttpGetUrl = new Uri(wsdlFullPath) };
                host.Description.Behaviors.Add(smb);
                var mexBinding = MetadataExchangeBindings.CreateMexHttpBinding();
                host.AddServiceEndpoint(typeof(IMetadataExchange), mexBinding, wsdlFullPath);
            }

            var serviceKey = serviceFullAddress;
            if (Hosts.ContainsKey(serviceKey))
            {
                if (Hosts[serviceKey].Host.State == CommunicationState.Opened)
                {
                    try
                    {
                        Hosts[serviceKey].Host.Close();
                    }
                    catch { } // Nothing we can do
                }
                Hosts.Remove(serviceKey);
            }

            // If needed, we fire the static event, so people can tie into this to programmatically manipulate the host or binding if need be
            if (BeforeHostAdded != null) BeforeHostAdded(null, new HostAddedEventArgs
            {
                Host = host,
                ServiceFullAddress = serviceFullAddress,
                Binding = binding,
                ContractType = contractType,
                MessageSize = messageSize,
                ServiceId = serviceId,
                ServiceType = serviceType
            });

            lock (Hosts)
                Hosts.Add(serviceKey, new HostWrapper(host, serviceFullAddress));

            return StartService(serviceKey);
        }

        /// <summary>Attempts to create a new WS HTTP service host and handles and logs potential exceptions</summary>
        /// <param name="serviceType">Type of the service.</param>
        /// <param name="contractType">Type of the contract.</param>
        /// <param name="serviceId">The service id (generally, 'virtual directory' part of the service URL).</param>
        /// <param name="messageSize">Potential size of the message. (Should be large if the payload could potentially be more than an MB).</param>
        /// <param name="exposeWsdl">if set to <c>true</c> [expose WSDL].</param>
        /// <param name="baseAddress">Base service address (such as www.domain.com)</param>
        /// <param name="basePath">Base path (such as "MyService" to create www.domain.com/MyService/ws)</param>
        /// <param name="extension">Path extension for WS HTTP services (such as "ws" to create www.domain.com/MyService/ws)</param>
        /// <param name="useHttps">Indicates whether HTTPS should be used</param>
        /// <returns>Service URL if successful. Empty string otherwise.</returns>
        /// <remarks>Uses the LoggingMediator class to log information. Configure LoggingMediator accordingly.</remarks>
        public static string TryAddServiceHostWsHttp(Type serviceType, Type contractType = null, string serviceId = null, MessageSize messageSize = MessageSize.Undefined, bool exposeWsdl = false, string baseAddress = null, string basePath = null, string extension = null, bool useHttps = false)
        {
            try
            {
                var url = AddServiceHostWsHttp(serviceType, contractType, serviceId, messageSize, exposeWsdl, baseAddress, basePath, extension, useHttps);
                if (contractType == null) contractType = GetContractTypeFromServiceType(serviceType);
                if (!string.IsNullOrEmpty(url))
                {
                    var logText = "Successfully started WS HTTP Service '" + serviceType + "' (contract: '" + contractType + "').\r\n\r\nFull URL: " + url;
                    logText += "\r\nExpose WSDL: " + (exposeWsdl ? "Yes" : "No");
                    logText += "\r\nMessage Size: " + messageSize;
                    LoggingMediator.Log(logText);
                }
                else
                    LoggingMediator.Log("Error starting WS HTTP Service '" + serviceType + "' (contract: '" + contractType + "').", LogEventType.Error);
                return url;
            }
            catch (Exception ex)
            {
                if (contractType == null) contractType = GetContractTypeFromServiceType(serviceType);
                LoggingMediator.Log("Error starting WS HTTP Service '" + serviceType + "' (contract: '" + contractType + "').", ex, LogEventType.Error);
                return string.Empty;
            }
        }

        /// <summary>
        /// Creates the service host.
        /// </summary>
        /// <param name="serviceType">Type of the service.</param>
        /// <param name="messageSize">Potential size of the message. (Should be large if the payload could potentially be more than an MB).</param>
        /// <param name="baseAddress">Base service address (such as www.domain.com)</param>
        /// <param name="basePath">Base path (such as "MyService" to create www.domain.com/MyService/ws)</param>
        /// <returns>
        /// Service URL if successful. Empty string otherwise.
        /// </returns>
        public static string AddServiceHostRestXml(Type serviceType, MessageSize messageSize, string baseAddress, string basePath)
        {
            return AddServiceHostRestXml(serviceType, null, null, messageSize, baseAddress, basePath);
        }
        /// <summary>
        /// Tries to create the service host.
        /// </summary>
        /// <param name="serviceType">Type of the service.</param>
        /// <param name="messageSize">Potential size of the message. (Should be large if the payload could potentially be more than an MB).</param>
        /// <param name="baseAddress">Base service address (such as www.domain.com)</param>
        /// <param name="basePath">Base path (such as "MyService" to create www.domain.com/MyService/ws)</param>
        /// <returns>
        /// Service URL if successful. Empty string otherwise.
        /// </returns>
        /// <remarks>Uses the LoggingMediator class to log information. Configure LoggingMediator accordingly.</remarks>
        public static string TryAddServiceHostRestXml(Type serviceType, MessageSize messageSize, string baseAddress, string basePath)
        {
            return TryAddServiceHostRestXml(serviceType, null, null, messageSize, baseAddress, basePath);
        }

        /// <summary>
        /// Creates the service host.
        /// </summary>
        /// <param name="serviceType">Type of the service.</param>
        /// <param name="contractType">Type of the contract.</param>
        /// <param name="messageSize">Potential size of the message. (Should be large if the payload could potentially be more than an MB).</param>
        /// <param name="baseAddress">Base service address (such as www.domain.com)</param>
        /// <param name="basePath">Base path (such as "MyService" to create www.domain.com/MyService/ws)</param>
        /// <returns>
        /// Service URL if successful. Empty string otherwise.
        /// </returns>
        public static string AddServiceHostRestXml(Type serviceType, Type contractType, MessageSize messageSize, string baseAddress, string basePath)
        {
            return AddServiceHostRestXml(serviceType, contractType, null, messageSize, baseAddress, basePath);
        }
        /// <summary>
        /// Tries to create the service host.
        /// </summary>
        /// <param name="serviceType">Type of the service.</param>
        /// <param name="contractType">Type of the contract.</param>
        /// <param name="messageSize">Potential size of the message. (Should be large if the payload could potentially be more than an MB).</param>
        /// <param name="baseAddress">Base service address (such as www.domain.com)</param>
        /// <param name="basePath">Base path (such as "MyService" to create www.domain.com/MyService/ws)</param>
        /// <returns>
        /// Service URL if successful. Empty string otherwise.
        /// </returns>
        /// <remarks>Uses the LoggingMediator class to log information. Configure LoggingMediator accordingly.</remarks>
        public static string TryAddServiceHostRestXml(Type serviceType, Type contractType, MessageSize messageSize, string baseAddress, string basePath)
        {
            return TryAddServiceHostRestXml(serviceType, contractType, null, messageSize, baseAddress, basePath);
        }

        /// <summary>
        /// Creates a REST service host with XML data format
        /// </summary>
        /// <param name="serviceType">Type of the service.</param>
        /// <param name="contractType">Type of the contract.</param>
        /// <param name="serviceId">The service id (generally, 'virtual directory' part of the service URL).</param>
        /// <param name="messageSize">Potential size of the message. (Should be large if the payload could potentially be more than an MB).</param>
        /// <param name="baseAddress">Base service address (such as www.domain.com)</param>
        /// <param name="basePath">Base path (such as "MyService" to create www.domain.com/MyService/ws)</param>
        /// <param name="extension">Path extension for REST services (such as "rest/xml" to create www.domain.com/MyService/rest/xml)</param>
        /// <param name="useHttps">Indicates whether HTTPS should be used</param>
        /// <returns>Service URL if successful. Empty string otherwise.</returns>
        /// <exception cref="Core.Exceptions.NullReferenceException">Static BaseUrl property must be set on the ServiceGarden class before the garden can be populated.</exception>
        public static string AddServiceHostRestXml(Type serviceType, Type contractType = null, string serviceId = null, MessageSize messageSize = MessageSize.Undefined, string baseAddress = null, string basePath = null, string extension = null, bool useHttps = false)
        {
            // Before we do anything else, we start looking for optional parameters and setting appropriate defaults
            if (contractType == null) contractType = GetContractTypeFromServiceType(serviceType);
            if (baseAddress == null)
            {
                baseAddress = BaseUrl;
                if (string.IsNullOrEmpty(baseAddress)) throw new Core.Exceptions.NullReferenceException("Static BaseUrl property must be set on the ServiceGarden class before the garden can be populated.");
            }
            if (basePath == null)
            {
                basePath = BasePath;
                if (!string.IsNullOrEmpty(basePath) && !basePath.EndsWith("/")) basePath += "/";
            }
            if (extension == null)
            {
                extension = GetSetting("ServiceRestExtension", contractType.Name, "rest");
                if (!string.IsNullOrEmpty(extension) && !extension.StartsWith("/")) extension = "/" + extension;
                var formatExtension = GetSetting("ServiceRestXmlFormatExtension", contractType.Name, "xml");
                if (!string.IsNullOrEmpty(formatExtension)) extension = extension + "/" + formatExtension;
            }
            if (serviceId == null) serviceId = contractType.Name;
            if (messageSize == MessageSize.Undefined) messageSize = GetMessageSize(contractType.Name);

            // Starting our regular method
            if (!string.IsNullOrEmpty(basePath) && !basePath.EndsWith("/")) basePath += "/";
            if (!string.IsNullOrEmpty(extension) && !extension.StartsWith("/")) extension = "/" + extension;
            var protocol = useHttps ? "https://" : "http://";
            var serviceFullAddress = protocol + baseAddress + "/" + basePath + serviceId + extension;
            var serviceNamespace = GetServiceNamespace(serviceType, contractType);

            var addresses = new[] { new Uri(serviceFullAddress) };
            var host = CreateServiceHost(serviceType, contractType, addresses);

            // Binding needed
            var securityMode = useHttps ? WebHttpSecurityMode.Transport : WebHttpSecurityMode.None;
            var binding = new WebHttpBinding(securityMode) { SendTimeout = new TimeSpan(0, 10, 0) };
            if (!string.IsNullOrEmpty(serviceNamespace)) binding.Namespace = serviceNamespace;
            ServiceHelper.ConfigureMessageSizeOnWebHttpBinding(messageSize, binding);

            // Endpoint configuration
            var beforeEndpointAdded = BeforeEndpointAdded;
            if (beforeEndpointAdded != null)
            {
                var endpointAddedArgs = new EndpointAddedEventArgs { Binding = binding, ContractType = contractType, ServiceFullAddress = serviceFullAddress };
                beforeEndpointAdded(null, endpointAddedArgs);
                serviceFullAddress = endpointAddedArgs.ServiceFullAddress;
            }
            var endpoint = host.AddServiceEndpoint(contractType, binding, serviceFullAddress);
            if (!string.IsNullOrEmpty(serviceNamespace) && endpoint.Contract != null && endpoint.Contract.Namespace != serviceNamespace) endpoint.Contract.Namespace = serviceNamespace;
            endpoint.Behaviors.Add(new RestXmlHttpBehavior()); // REST-specific behavior configuration
            if (HttpCrossDomainCallsAllowed) endpoint.Behaviors.Add(new CrossDomainScriptBehavior());

            var serviceKey = serviceFullAddress;
            if (Hosts.ContainsKey(serviceKey))
            {
                if (Hosts[serviceKey].Host.State == CommunicationState.Opened)
                {
                    try
                    {
                        Hosts[serviceKey].Host.Close();
                    }
                    catch { } // Nothing we can do
                }
                Hosts.Remove(serviceKey);
            }

            // If needed, we fire the static event, so people can tie into this to programmatically manipulate the host or binding if need be
            if (BeforeHostAdded != null) BeforeHostAdded(null, new HostAddedEventArgs
            {
                Host = host,
                ServiceFullAddress = serviceFullAddress,
                Binding = binding,
                ContractType = contractType,
                MessageSize = messageSize,
                ServiceId = serviceId,
                ServiceType = serviceType
            });

            lock (Hosts)
                Hosts.Add(serviceKey, new HostWrapper(host, serviceFullAddress));

            return StartService(serviceKey);
        }

        /// <summary>Tries to create a REST service host with XML data format and handles and logs potential exceptions</summary>
        /// <param name="serviceType">Type of the service.</param>
        /// <param name="contractType">Type of the contract.</param>
        /// <param name="serviceId">The service id (generally, 'virtual directory' part of the service URL).</param>
        /// <param name="messageSize">Potential size of the message. (Should be large if the payload could potentially be more than an MB).</param>
        /// <param name="baseAddress">Base service address (such as www.domain.com)</param>
        /// <param name="basePath">Base path (such as "MyService" to create www.domain.com/MyService/ws)</param>
        /// <param name="extension">Path extension for REST services (such as "rest/xml" to create www.domain.com/MyService/rest/xml)</param>
        /// <param name="useHttps">Indicates whether HTTPS should be used</param>
        /// <returns>Service URL if successful. Empty string otherwise.</returns>
        /// <remarks>Uses the LoggingMediator class to log information. Configure LoggingMediator accordingly.</remarks>
        public static string TryAddServiceHostRestXml(Type serviceType, Type contractType = null, string serviceId = null, MessageSize messageSize = MessageSize.Undefined, string baseAddress = null, string basePath = null, string extension = null, bool useHttps = false)
        {
            try
            {
                var url = AddServiceHostRestXml(serviceType, contractType, serviceId, messageSize, baseAddress, basePath, extension, useHttps);
                if (contractType == null) contractType = GetContractTypeFromServiceType(serviceType);
                if (!string.IsNullOrEmpty(url))
                {
                    var logText = "Successfully started XML-formatted REST Service '" + serviceType + "' (contract: '" + contractType + "').\r\n\r\nFull URL: " + url;
                    logText += "\r\nMessage Size: " + messageSize;
                    LoggingMediator.Log(logText);
                }
                else
                    LoggingMediator.Log("Error starting XML-formatted REST Service '" + serviceType + "' (contract: '" + contractType + "').", LogEventType.Error);
                return url;
            }
            catch (Exception ex)
            {
                if (contractType == null) contractType = GetContractTypeFromServiceType(serviceType);
                LoggingMediator.Log("Error starting XML-formatted REST Service '" + serviceType + "' (contract: '" + contractType + "').", ex, LogEventType.Error);
                return string.Empty;
            }
        }

        /// <summary>
        /// Creates the service host.
        /// </summary>
        /// <param name="serviceType">Type of the service.</param>
        /// <param name="messageSize">Potential size of the message. (Should be large if the payload could potentially be more than an MB).</param>
        /// <param name="baseAddress">Base service address (such as www.domain.com)</param>
        /// <param name="basePath">Base path (such as "MyService" to create www.domain.com/MyService/ws)</param>
        /// <returns>
        /// Service URL if successful. Empty string otherwise.
        /// </returns>
        public static string AddServiceHostRestJson(Type serviceType, MessageSize messageSize, string baseAddress, string basePath)
        {
            return AddServiceHostRestJson(serviceType, null, null, messageSize, baseAddress, basePath);
        }
        /// <summary>
        /// Tries to create the service host.
        /// </summary>
        /// <param name="serviceType">Type of the service.</param>
        /// <param name="messageSize">Potential size of the message. (Should be large if the payload could potentially be more than an MB).</param>
        /// <param name="baseAddress">Base service address (such as www.domain.com)</param>
        /// <param name="basePath">Base path (such as "MyService" to create www.domain.com/MyService/ws)</param>
        /// <returns>
        /// Service URL if successful. Empty string otherwise.
        /// </returns>
        /// <remarks>Uses the LoggingMediator class to log information. Configure LoggingMediator accordingly.</remarks>
        public static string TryAddServiceHostRestJson(Type serviceType, MessageSize messageSize, string baseAddress, string basePath)
        {
            return TryAddServiceHostRestJson(serviceType, null, null, messageSize, baseAddress, basePath);
        }

        /// <summary>
        /// Creates the service host.
        /// </summary>
        /// <param name="serviceType">Type of the service.</param>
        /// <param name="contractType">Type of the contract.</param>
        /// <param name="messageSize">Potential size of the message. (Should be large if the payload could potentially be more than an MB).</param>
        /// <param name="baseAddress">Base service address (such as www.domain.com)</param>
        /// <param name="basePath">Base path (such as "MyService" to create www.domain.com/MyService/ws)</param>
        /// <returns>
        /// Service URL if successful. Empty string otherwise.
        /// </returns>
        public static string AddServiceHostRestJson(Type serviceType, Type contractType, MessageSize messageSize, string baseAddress, string basePath)
        {
            return AddServiceHostRestJson(serviceType, contractType, null, messageSize, baseAddress, basePath);
        }
        /// <summary>
        /// Tries to create the service host.
        /// </summary>
        /// <param name="serviceType">Type of the service.</param>
        /// <param name="contractType">Type of the contract.</param>
        /// <param name="messageSize">Potential size of the message. (Should be large if the payload could potentially be more than an MB).</param>
        /// <param name="baseAddress">Base service address (such as www.domain.com)</param>
        /// <param name="basePath">Base path (such as "MyService" to create www.domain.com/MyService/ws)</param>
        /// <returns>
        /// Service URL if successful. Empty string otherwise.
        /// </returns>
        /// <remarks>Uses the LoggingMediator class to log information. Configure LoggingMediator accordingly.</remarks>
        public static string TryAddServiceHostRestJson(Type serviceType, Type contractType, MessageSize messageSize, string baseAddress, string basePath)
        {
            return TryAddServiceHostRestJson(serviceType, contractType, null, messageSize, baseAddress, basePath);
        }

        /// <summary>
        /// Creates a REST service host with JSON data format
        /// </summary>
        /// <param name="serviceType">Type of the service.</param>
        /// <param name="contractType">Type of the contract.</param>
        /// <param name="serviceId">The service id (generally, 'virtual directory' part of the service URL).</param>
        /// <param name="messageSize">Potential size of the message. (Should be large if the payload could potentially be more than an MB).</param>
        /// <param name="baseAddress">Base service address (such as www.domain.com)</param>
        /// <param name="basePath">Base path (such as "MyService" to create www.domain.com/MyService/ws)</param>
        /// <param name="extension">Path extension for REST services (such as "rest/xml" to create www.domain.com/MyService/rest/xml)</param>
        /// <param name="useHttps">Indicates whether HTTPS should be used.</param>
        /// <returns>Service URL if successful. Empty string otherwise.</returns>
        /// <exception cref="Core.Exceptions.NullReferenceException">Static BaseUrl property must be set on the ServiceGarden class before the garden can be populated.</exception>
        public static string AddServiceHostRestJson(Type serviceType, Type contractType = null, string serviceId = null, MessageSize messageSize = MessageSize.Undefined, string baseAddress = null, string basePath = null, string extension = null, bool useHttps = false)
        {
            // Before we do anything else, we start looking for optional parameters and setting appropriate defaults
            if (contractType == null) contractType = GetContractTypeFromServiceType(serviceType);
            if (baseAddress == null)
            {
                baseAddress = BaseUrl;
                if (string.IsNullOrEmpty(baseAddress)) throw new Core.Exceptions.NullReferenceException("Static BaseUrl property must be set on the ServiceGarden class before the garden can be populated.");
            }
            if (basePath == null)
            {
                basePath = BasePath;
                if (!string.IsNullOrEmpty(basePath) && !basePath.EndsWith("/")) basePath += "/";
            }
            if (extension == null)
            {
                extension = GetSetting("ServiceRestExtension", contractType.Name, "rest");
                if (!string.IsNullOrEmpty(extension) && !extension.StartsWith("/")) extension = "/" + extension;
                var formatExtension = GetSetting("ServiceRestJsonFormatExtension", contractType.Name, "json");
                if (!string.IsNullOrEmpty(formatExtension)) extension = extension + "/" + formatExtension;
            }
            if (serviceId == null) serviceId = contractType.Name;
            if (messageSize == MessageSize.Undefined) messageSize = GetMessageSize(contractType.Name);

            // Starting our regular method
            if (!string.IsNullOrEmpty(basePath) && !basePath.EndsWith("/")) basePath += "/";
            if (!string.IsNullOrEmpty(extension) && !extension.StartsWith("/")) extension = "/" + extension;
            var protocol = useHttps ? "https://" : "http://";
            var serviceFullAddress = protocol + baseAddress + "/" + basePath + serviceId + extension;
            var serviceNamespace = GetServiceNamespace(serviceType, contractType);

            var addresses = new[] { new Uri(serviceFullAddress) };
            var host = CreateServiceHost(serviceType, contractType, addresses);

            // Binding needed
            var securityMode = useHttps ? WebHttpSecurityMode.Transport : WebHttpSecurityMode.None;
            var binding = new WebHttpBinding(securityMode) { SendTimeout = new TimeSpan(0, 10, 0) };
            if (!string.IsNullOrEmpty(serviceNamespace)) binding.Namespace = serviceNamespace;
            ServiceHelper.ConfigureMessageSizeOnWebHttpBinding(messageSize, binding);

            // Endpoint configuration
            var beforeEndpointAdded = BeforeEndpointAdded;
            if (beforeEndpointAdded != null)
            {
                var endpointAddedArgs = new EndpointAddedEventArgs { Binding = binding, ContractType = contractType, ServiceFullAddress = serviceFullAddress };
                beforeEndpointAdded(null, endpointAddedArgs);
                serviceFullAddress = endpointAddedArgs.ServiceFullAddress;
            }
            var endpoint = host.AddServiceEndpoint(contractType, binding, serviceFullAddress);
            if (!string.IsNullOrEmpty(serviceNamespace) && endpoint.Contract != null && endpoint.Contract.Namespace != serviceNamespace) endpoint.Contract.Namespace = serviceNamespace;
            endpoint.Behaviors.Add(new RestJsonHttpBehavior(serviceFullAddress, contractType)); // REST-specific behavior configuration
            if (HttpCrossDomainCallsAllowed) endpoint.Behaviors.Add(new CrossDomainScriptBehavior());

            var serviceKey = serviceFullAddress;
            if (Hosts.ContainsKey(serviceKey))
            {
                if (Hosts[serviceKey].Host.State == CommunicationState.Opened)
                {
                    try
                    {
                        Hosts[serviceKey].Host.Close();
                    }
                    catch { } // Nothing we can do
                }
                Hosts.Remove(serviceKey);
            }

            // If needed, we fire the static event, so people can tie into this to programmatically manipulate the host or binding if need be
            if (BeforeHostAdded != null)
                BeforeHostAdded(null, new HostAddedEventArgs
                    {
                        Host = host,
                        ServiceFullAddress = serviceFullAddress,
                        Binding = binding,
                        ContractType = contractType,
                        MessageSize = messageSize,
                        ServiceId = serviceId,
                        ServiceType = serviceType
                    });

            lock (Hosts)
                Hosts.Add(serviceKey, new HostWrapper(host, serviceFullAddress));

            return StartService(serviceKey);
        }

        /// <summary>
        /// Trues to create a JSON formatted REST service host and handles and logs potential exceptions
        /// </summary>
        /// <param name="serviceType">Type of the service.</param>
        /// <param name="contractType">Type of the contract.</param>
        /// <param name="serviceId">The service id (generally, 'virtual directory' part of the service URL).</param>
        /// <param name="messageSize">Potential size of the message. (Should be large if the payload could potentially be more than an MB).</param>
        /// <param name="baseAddress">Base service address (such as www.domain.com)</param>
        /// <param name="basePath">Base path (such as "MyService" to create www.domain.com/MyService/ws)</param>
        /// <param name="extension">Path extension for REST services (such as "rest/xml" to create www.domain.com/MyService/rest/xml)</param>
        /// <param name="useHttps">Indicates whether HTTPS should be used</param>
        /// <returns>Service URL if successful. Empty string otherwise.</returns>
        /// <remarks>Uses the LoggingMediator class to log information. Configure LoggingMediator accordingly.</remarks>
        public static string TryAddServiceHostRestJson(Type serviceType, Type contractType = null, string serviceId = null, MessageSize messageSize = MessageSize.Undefined, string baseAddress = null, string basePath = null, string extension = null, bool useHttps = false)
        {
            try
            {
                var url = AddServiceHostRestJson(serviceType, contractType, serviceId, messageSize, baseAddress, basePath, extension, useHttps);
                if (contractType == null) contractType = GetContractTypeFromServiceType(serviceType);
                if (!string.IsNullOrEmpty(url))
                {
                    var logText = "Successfully started JSON-formatted REST Service '" + serviceType + "' (contract: '" + contractType + "').\r\n\r\nFull URL: " + url;
                    logText += "\r\nMessage Size: " + messageSize;
                    LoggingMediator.Log(logText);
                }
                else
                    LoggingMediator.Log("Error starting JSON-formatted REST Service '" + serviceType + "' (contract: '" + contractType + "').", LogEventType.Error);
                return url;
            }
            catch (Exception ex)
            {
                if (contractType == null) contractType = GetContractTypeFromServiceType(serviceType);
                LoggingMediator.Log("Error starting JSON-formatted REST Service '" + serviceType + "' (contract: '" + contractType + "').", ex, LogEventType.Error);
                return string.Empty;
            }
        }

        /// <summary>
        /// Wrapper for a service host
        /// </summary>
        private class HostWrapper
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="HostWrapper"/> class.
            /// </summary>
            /// <param name="host">The host.</param>
            /// <param name="endpointAddress">The endpoint address.</param>
            public HostWrapper(ServiceHost host, string endpointAddress)
            {
                Host = host;
                EndpointAddress = endpointAddress;
            }

            /// <summary>
            /// Gets or sets the host.
            /// </summary>
            /// <value>The host.</value>
            public ServiceHost Host { get; private set; }

            /// <summary>
            /// Gets or sets the endpoint address.
            /// </summary>
            /// <value>The endpoint address.</value>
            public string EndpointAddress { get; private set; }
        }
    }

    /// <summary>
    /// Communication Protocol
    /// </summary>
    public enum Protocol
    {
        /// <summary>
        /// Net TCP
        /// </summary>
        NetTcp,
        /// <summary>
        /// Local in process service
        /// </summary>
        InProcess,
        /// <summary>
        /// Basic HTTP
        /// </summary>
        BasicHttp,
        /// <summary>
        /// WS HTTP
        /// </summary>
        WsHttp,
        /// <summary>
        /// XML Formatted REST over HTTP
        /// </summary>
        RestHttpXml,
        /// <summary>
        /// JSON Formatted REST over HTTP
        /// </summary>
        RestHttpJson
    }

    /// <summary>
    /// Message size
    /// </summary>
    public enum MessageSize
    {
        /// <summary>
        /// Normal (default message size as defined by WCF)
        /// </summary>
        Normal,
        /// <summary>
        /// Large (up to 100MB)
        /// </summary>
        Large,
        /// <summary>
        /// Medium (up to 10MB) - this is the default
        /// </summary>
        Medium,
        /// <summary>
        /// For internal use only
        /// </summary>
        Undefined,
        /// <summary>
        /// Very large (up to 1GB)
        /// </summary>
        VeryLarge,
        /// <summary>
        /// Maximum size (equal to int.MaxValue, about 2GB)
        /// </summary>
        Max
    }

    /// <summary>
    /// Event arguments for added hosts
    /// </summary>
    public class HostAddedEventArgs : EventArgs
    {
        /// <summary>
        /// Service host
        /// </summary>
        public ServiceHost Host { get; internal set; }

        /// <summary>
        /// Full address the service will be hosted at
        /// </summary>
        public string ServiceFullAddress { get; internal set; }

        /// <summary>
        /// Utilized binding
        /// </summary>
        public Binding Binding { get; internal set; }

        /// <summary>
        /// Service type (type implementing the service)
        /// </summary>
        public Type ServiceType { get; internal set; }

        /// <summary>
        /// Service contract type
        /// </summary>
        public Type ContractType { get; internal set; }

        /// <summary>
        /// Service ID
        /// </summary>
        public string ServiceId { get; internal set; }

        /// <summary>
        /// Message Size hosted by the service
        /// </summary>
        public MessageSize MessageSize { get; internal set; }
    }

    /// <summary>
    /// Event arguments for added endpoints
    /// </summary>
    public class EndpointAddedEventArgs : EventArgs
    {
        /// <summary>
        /// Full address the service will be hosted at
        /// </summary>
        /// <remarks>This address can be changed to change the actual address the service is hosted at</remarks>
        public string ServiceFullAddress { get; set; }

        /// <summary>
        /// Utilized binding
        /// </summary>
        public Binding Binding { get; internal set; }

        /// <summary>
        /// Service contract type
        /// </summary>
        public Type ContractType { get; internal set; }
    }
}

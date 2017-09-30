using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.Text;
using System.Threading;
using CODE.Framework.Core.Configuration;
using CODE.Framework.Core.Exceptions;
using CODE.Framework.Core.Utilities;

namespace CODE.Framework.Services.Client
{
    /// <summary>
    /// Helper class that provides methods that make it easier to talk to services
    /// </summary>
    public static class ServiceClient
    {
        /// <summary>
        /// Initializes the <see cref="ServiceClient"/> class.
        /// </summary>
        static ServiceClient()
        {
            AutoRetryFailedCalls = false;
            AutoRetryDelay = -1;
            AutoRetryFailedCallsForExceptions = new List<Type>
                {
                    typeof (EndpointNotFoundException),
                    typeof (ServerTooBusyException)
                };

            CacheSettings = true;

            BaseUrl = GetSetting("ServiceBaseUrl", defaultValue: "localhost");
            BasePath = GetSetting("ServiceBasePath", defaultValue: "dev");
        }

        /// <summary>
        /// Internal channel cache
        /// </summary>
        private static readonly OpenChannelInformationList OpenChannels = new OpenChannelInformationList();

        /// <summary>
        /// Almost the same as a List of T, but with a destructor
        /// </summary>
        private class OpenChannelInformationList : List<OpenChannelInformation>
        {
            /// <summary>
            /// Destructor used to clean up potential open channels
            /// </summary>
            ~OpenChannelInformationList()
            {
                try
                {
                    while (Count > 0)
                        CloseChannel(this[0].Channel);
                }
                catch
                {
                    // Oh well... nothing we can do about it now, but the app should be shutting down anyway.
                }
            }
        }

        /// <summary>
        /// Tries to find the desired channel in the cache and if found returns the cache index (otherwise returns -1)
        /// </summary>
        /// <param name="serviceType">Requested service type (interface)</param>
        /// <param name="messageSize">Requested message size</param>
        /// <param name="port">Requested port</param>
        /// <param name="serviceId">Requested service ID</param>
        /// <param name="baseAddress">Requested service base URL</param>
        /// <param name="basePath">Requested service base path</param>
        /// <returns>Cache index or -1 if channel is not cached</returns>
        private static int GetChannelCacheIndex(Type serviceType, MessageSize messageSize, int port, string serviceId, string baseAddress, string basePath)
        {
            var cacheCounter = 0;
            foreach (var channel in OpenChannels)
            {
                if (channel.IsMatch(serviceType, messageSize, port, serviceId, baseAddress, basePath)) return cacheCounter;
                cacheCounter++;
            }
            return -1;
        }

        /// <summary>
        /// Retrieves a channel from the channel cache and verifies that the channel is valid (open)
        /// </summary>
        /// <typeparam name="TServiceType">Expected type of the service</typeparam>
        /// <param name="cacheIndex">Channel index in the cache (usually retrieved using the GetChannelCacheIndex method)</param>
        /// <returns>Open channel</returns>
        private static TServiceType GetCachedChannel<TServiceType>(int cacheIndex) where TServiceType : class
        {
            if (cacheIndex > OpenChannels.Count - 1) return null;
            var channel = OpenChannels[cacheIndex].Channel as TServiceType;
            if (channel == null) return null;
            channel = VerifyChannelIsValid(channel);
            return channel;
        }

        /// <summary>
        /// Retrieves a channel from the channel cache and verifies that the channel is valid (open)
        /// </summary>
        /// <param name="serviceType">Service type (contract)</param>
        /// <param name="cacheIndex">Channel index in the cache (usually retrieved using the GetChannelCacheIndex method)</param>
        /// <returns>Open channel</returns>
        private static object GetCachedChannel(Type serviceType, int cacheIndex)
        {
            if (cacheIndex > OpenChannels.Count - 1) return null;
            var channelType = OpenChannels[cacheIndex].Channel.GetType();
            if (channelType != serviceType) return null;
            var channel = VerifyChannelIsValid(OpenChannels[cacheIndex].Channel);
            return channel;
        }

        /// <summary>
        /// Base URL for the service
        /// </summary>
        public static string BaseUrl { get; set; }

        /// <summary>
        /// Gets or sets the base path ("virtual directory")
        /// </summary>
        /// <value>The base path.</value>
        public static string BasePath { get; set; }

        /// <summary>Indicates whether channel exceptions should be handled (and thus hidden) or thrown to the caller.</summary>
        /// <value>False (default) to throw the exception to the caller or True to handle the exceptions internally</value>
        /// <remarks>See also: ClientException event</remarks>
        public static bool HandleChannelExceptions { get; set; }

        /// <summary>
        /// Indicates whether configuration settings should be cached (default = yes)
        /// </summary>
        /// <value><c>true</c> if [cache settings]; otherwise, <c>false</c>.</value>
        /// <remarks>
        /// When settings get cached, they are only retrieved from the configuration system
        /// the first time they are needed. Subsequent uses will be based on the cache, which improves performance.
        /// However, if settings are meant to change dynamically, caching needs to be turned off, otherwise
        /// new configuration settings will be ignored by the service client.
        /// </remarks>
        public static bool CacheSettings { get; set; }

        /// <summary>Indicates whether calls that caused faults should automatically be re-issued a second time. (True by default)</summary>
        public static bool AutoRetryFailedCalls { get; set; }

        /// <summary>Defines the delay (milliseconds) between auto-retry calls</summary>
        /// <remarks>The delay is defined in milliseconds (-1 = no delay). Note that the delay puts the thread to sleep, so this should not be done on foreground threads.</remarks>
        public static int AutoRetryDelay { get; set; }

        /// <summary>Defines the list of exception types for which to auto-retry calls</summary>
        public static List<Type> AutoRetryFailedCallsForExceptions { get; set; }

        /// <summary>Exception callback for exceptions occurring on the channel</summary>
        public static event EventHandler<ChannelExceptionEventArgs> ChannelException;

        /// <summary>Internal message size cache</summary>
        private static readonly Dictionary<string, MessageSize> CachedMessageSizes = new Dictionary<string, MessageSize>();

        /// <summary>Determines whether a call needs to be auto-retried</summary>
        /// <param name="exception">The exception that caused the original failure.</param>
        /// <returns>True if the call should be retried</returns>
        private static bool MustRetryCall(Exception exception)
        {
            if (!AutoRetryFailedCalls) return false;
            if (AutoRetryFailedCallsForExceptions.Count == 0) return true;

            if (AutoRetryFailedCallsForExceptions.Any(exceptionType => exception.GetType() == exceptionType))
            {
                if (AutoRetryDelay > 0) Thread.Sleep(AutoRetryDelay);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets the allowable message size for a specific interface
        /// </summary>
        /// <typeparam name="TServiceType">The type of the T service type.</typeparam>
        /// <returns>MessageSize.</returns>
        private static MessageSize GetMessageSize<TServiceType>()
        {
            var type = typeof(TServiceType);
            return GetMessageSize(type.Name);
        }

        /// <summary>
        /// Gets the allowable message size for a specific interface
        /// </summary>
        /// <param name="interfaceName">Name of the interface.</param>
        /// <returns>MessageSize.</returns>
        private static MessageSize GetMessageSize(string interfaceName)
        {
            var key = "ServiceMessageSize:" + interfaceName;
            lock (CachedMessageSizes)
                if (CacheSettings && CachedMessageSizes.ContainsKey(key))
                    return CachedMessageSizes[key];

            var messageSize = GetSetting(key).ToLower(CultureInfo.InvariantCulture);
            var size = MessageSize.Medium;

            switch (messageSize)
            {
                case "large":
                    size = MessageSize.Large;
                    break;
                case "normal":
                    size = MessageSize.Normal;
                    break;
                case "verylarge":
                    size = MessageSize.VeryLarge;
                    break;
                case "max":
                    size = MessageSize.Max;
                    break;
            }

            if (CacheSettings)
                lock (CachedMessageSizes)
                    if (CachedMessageSizes.ContainsKey(key))
                        CachedMessageSizes[key] = size;
                    else
                        CachedMessageSizes.Add(key, size);

            return size;
        }

        private static SecurityMode GetSecurityMode(string interfaceName)
        {
            var mode = GetSetting("SecurityMode:" + interfaceName).ToLower();
            if (string.IsNullOrEmpty(mode)) mode = GetSetting("ServiceSecurityMode").ToLower();
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

        /// <summary>
        /// Gets the channel.
        /// </summary>
        /// <typeparam name="TServiceType">The type of the service.</typeparam>
        /// <param name="port">The port.</param>
        /// <returns>Service</returns>
        public static TServiceType GetNetTcpChannel<TServiceType>(int port) where TServiceType : class
        {
            return GetChannel<TServiceType>(port, Protocol.NetTcp);
        }

        /// <summary>
        /// Gets the channel.
        /// </summary>
        /// <typeparam name="TServiceType">The type of the service.</typeparam>
        /// <param name="port">The port.</param>
        /// <param name="serviceId">The service id.</param>
        /// <returns>Service</returns>
        public static TServiceType GetNetTcpChannel<TServiceType>(int port, string serviceId) where TServiceType : class
        {
            return GetChannel<TServiceType>(port, serviceId, Protocol.NetTcp);
        }

        /// <summary>
        /// Gets the channel.
        /// </summary>
        /// <typeparam name="TServiceType">The type of the service.</typeparam>
        /// <param name="port">The port.</param>
        /// <param name="serviceId">The service id.</param>
        /// <param name="messageSize">Size of the message.</param>
        /// <returns>Service</returns>
        public static TServiceType GetNetTcpChannel<TServiceType>(int port, string serviceId, MessageSize messageSize) where TServiceType : class
        {
            return GetChannel<TServiceType>(port, serviceId, Protocol.NetTcp, messageSize);
        }

        /// <summary>
        /// Gets the channel.
        /// </summary>
        /// <typeparam name="TServiceType">The type of the service.</typeparam>
        /// <param name="port">The port.</param>
        /// <param name="serviceId">The service id.</param>
        /// <param name="messageSize">Size of the message.</param>
        /// <param name="baseAddress">Base service address (such as www.domain.com)</param>
        /// <param name="basePath">Base path (such as "MyService" to create www.domain.com/MyService)</param>
        /// <returns>Service</returns>
        public static TServiceType GetNetTcpChannel<TServiceType>(int port, string serviceId, MessageSize messageSize, string baseAddress, string basePath) where TServiceType : class
        {
            return GetInternalNetTcpChannel<TServiceType>(port, serviceId, messageSize, baseAddress, basePath, true);
        }

        /// <summary>
        /// Gets a dedicated channel instance
        /// </summary>
        /// <typeparam name="TServiceType">The type of the service.</typeparam>
        /// <param name="port">The port.</param>
        /// <param name="serviceId">The service id.</param>
        /// <param name="messageSize">Size of the message.</param>
        /// <param name="baseAddress">Base service address (such as www.domain.com)</param>
        /// <param name="basePath">Base path (such as "MyService" to create www.domain.com/MyService)</param>
        /// <remarks>Creates a channel exclusive to this caller. It is up to the caller to close the channel after use!</remarks>
        /// <returns>Service</returns>
        public static TServiceType GetNetTcpChannelDedicated<TServiceType>(int port, string serviceId, MessageSize messageSize, string baseAddress, string basePath) where TServiceType : class
        {
            return GetInternalNetTcpChannel<TServiceType>(port, serviceId, messageSize, baseAddress, basePath, false);
        }

        /// <summary>
        /// Gets the channel.
        /// </summary>
        /// <typeparam name="TServiceType">The type of the service.</typeparam>
        /// <param name="port">The port.</param>
        /// <param name="serviceId">The service id.</param>
        /// <param name="baseAddress">Base service address (such as www.domain.com)</param>
        /// <param name="basePath">Base path (such as "MyService" to create www.domain.com/MyService)</param>
        /// <returns>Service</returns>
        public static TServiceType GetNetTcpChannel<TServiceType>(int port, string serviceId, string baseAddress, string basePath) where TServiceType : class
        {
            return GetInternalNetTcpChannel<TServiceType>(port, serviceId, GetMessageSize(serviceId), baseAddress, basePath, true);
        }

        /// <summary>
        /// Gets a dedicated channel instance
        /// </summary>
        /// <typeparam name="TServiceType">The type of the service.</typeparam>
        /// <param name="port">The port.</param>
        /// <param name="serviceId">The service id.</param>
        /// <param name="baseAddress">Base service address (such as www.domain.com)</param>
        /// <param name="basePath">Base path (such as "MyService" to create www.domain.com/MyService)</param>
        /// <returns>Service</returns>
        /// <remarks>Creates a channel exclusive to this caller. It is up to the caller to close the channel after use!</remarks>
        public static TServiceType GetNetTcpChannelDedicated<TServiceType>(int port, string serviceId, string baseAddress, string basePath) where TServiceType : class
        {
            return GetInternalNetTcpChannel<TServiceType>(port, serviceId, GetMessageSize(serviceId), baseAddress, basePath, false);
        }

        /// <summary>
        /// Gets the channel.
        /// </summary>
        /// <typeparam name="TServiceType">The type of the service.</typeparam>
        /// <param name="port">The port.</param>
        /// <param name="messageSize">Size of the message.</param>
        /// <param name="baseAddress">Base service address (such as www.domain.com)</param>
        /// <param name="basePath">Base path (such as "MyService" to create www.domain.com/MyService)</param>
        /// <returns>Service</returns>
        public static TServiceType GetNetTcpChannel<TServiceType>(int port, MessageSize messageSize, string baseAddress, string basePath) where TServiceType : class
        {
            return GetInternalNetTcpChannel<TServiceType>(port, GetServiceId<TServiceType>(), messageSize, baseAddress, basePath, true);
        }

        /// <summary>
        /// Gets a dedicated channel instance
        /// </summary>
        /// <typeparam name="TServiceType">The type of the service.</typeparam>
        /// <param name="port">The port.</param>
        /// <param name="messageSize">Size of the message.</param>
        /// <param name="baseAddress">Base service address (such as www.domain.com)</param>
        /// <param name="basePath">Base path (such as "MyService" to create www.domain.com/MyService)</param>
        /// <returns>Service</returns>
        /// <remarks>Creates a channel exclusive to this caller. It is up to the caller to close the channel after use!</remarks>
        public static TServiceType GetNetTcpChannelDedicated<TServiceType>(int port, MessageSize messageSize, string baseAddress, string basePath) where TServiceType : class
        {
            return GetInternalNetTcpChannel<TServiceType>(port, GetServiceId<TServiceType>(), messageSize, baseAddress, basePath, false);
        }

        /// <summary>
        /// Gets the channel.
        /// </summary>
        /// <typeparam name="TServiceType">The type of the service.</typeparam>
        /// <param name="port">The port.</param>
        /// <param name="baseAddress">Base service address (such as www.domain.com)</param>
        /// <param name="basePath">Base path (such as "MyService" to create www.domain.com/MyService)</param>
        /// <returns>Service</returns>
        public static TServiceType GetNetTcpChannel<TServiceType>(int port, string baseAddress, string basePath) where TServiceType : class
        {
            var serviceId = GetServiceId<TServiceType>();
            return GetInternalNetTcpChannel<TServiceType>(port, serviceId, GetMessageSize(serviceId), baseAddress, basePath, true);
        }

        /// <summary>
        /// Gets a dedicated channel instance
        /// </summary>
        /// <typeparam name="TServiceType">The type of the service.</typeparam>
        /// <param name="port">The port.</param>
        /// <param name="baseAddress">Base service address (such as www.domain.com)</param>
        /// <param name="basePath">Base path (such as "MyService" to create www.domain.com/MyService)</param>
        /// <returns>Service</returns>
        /// <remarks>Creates a channel exclusive to this caller. It is up to the caller to close the channel after use!</remarks>
        public static TServiceType GetNetTcpChannelDedicated<TServiceType>(int port, string baseAddress, string basePath) where TServiceType : class
        {
            var serviceId = GetServiceId<TServiceType>();
            return GetInternalNetTcpChannel<TServiceType>(port, serviceId, GetMessageSize(serviceId), baseAddress, basePath, false);
        }

        /// <summary>
        /// Gets the channel using Basic HTTP protocol.
        /// </summary>
        /// <typeparam name="TServiceType">The type of the service.</typeparam>
        /// <param name="serviceId">The service id.</param>
        /// <param name="messageSize">Size of the message.</param>
        /// <param name="baseAddress">Base service address (such as www.domain.com)</param>
        /// <param name="basePath">Base path (such as "MyService" to create www.domain.com/MyService/basic)</param>
        /// <param name="extension">Path extension for basic HTTP services (such as "basic" to create www.domain.com/MyService/basic)</param>
        /// <param name="useHttps">Indicates whether HTTPS should be used</param>
        /// <param name="useCachedChannel">If true, the system is free to use a cached channel</param>
        /// <returns>Service</returns>
        /// <exception cref="Core.Exceptions.NullReferenceException">Static BaseUrl property must be set on the ServiceClient class.</exception>
        public static TServiceType GetBasicHttpChannel<TServiceType>(string serviceId = null, MessageSize messageSize = MessageSize.Undefined, string baseAddress = null, string basePath = null, string extension = null, bool useCachedChannel = true, bool useHttps = false) where TServiceType : class
        {
            if (serviceId == null) serviceId = GetServiceId<TServiceType>();
            var interfaceName = typeof(TServiceType).Name;
            if (messageSize == MessageSize.Undefined) messageSize = GetMessageSize(serviceId);
            if (baseAddress == null)
            {
                baseAddress = GetSetting("ServiceBaseUrl:" + interfaceName, defaultValue: BaseUrl);
                if (string.IsNullOrEmpty(baseAddress))
                    throw new Core.Exceptions.NullReferenceException("Static BaseUrl property must be set on the ServiceClient class.");
            }
            if (basePath == null) basePath = GetSetting("ServiceBasePath:" + interfaceName, defaultValue: BasePath);
            if (extension == null)
            {
                extension = GetSetting("ServiceBasicHTTPExtension:" + interfaceName);
                if (string.IsNullOrEmpty(extension))
                    extension = GetSetting("ServiceBasicHTTPExtension", defaultValue: "basic");
            }

            return GetInternalBasicHttpChannel<TServiceType>(serviceId, messageSize, baseAddress, basePath, extension, useCachedChannel, useHttps);
        }

        /// <summary>
        /// Gets a dedicated channel using Basic HTTP protocol.
        /// </summary>
        /// <typeparam name="TServiceType">The type of the service.</typeparam>
        /// <param name="serviceId">The service id.</param>
        /// <param name="messageSize">Size of the message.</param>
        /// <param name="baseAddress">Base service address (such as www.domain.com)</param>
        /// <param name="basePath">Base path (such as "MyService" to create www.domain.com/MyService/basic)</param>
        /// <param name="extension">Path extension for basic HTTP services (such as "basic" to create www.domain.com/MyService/basic)</param>
        /// <param name="useHttps">Indicates whether HTTPS should be used</param>
        /// <returns>Service</returns>
        /// <remarks>Creates a channel exclusive to this caller. It is up to the caller to close the channel after use!</remarks>
        public static TServiceType GetBasicHttpChannelDedicated<TServiceType>(string serviceId = null, MessageSize messageSize = MessageSize.Undefined, string baseAddress = null, string basePath = null, string extension = null, bool useHttps = false) where TServiceType : class
        {
            if (serviceId == null) serviceId = GetServiceId<TServiceType>();
            var interfaceName = typeof(TServiceType).Name;
            if (messageSize == MessageSize.Undefined) messageSize = GetMessageSize(serviceId);
            if (baseAddress == null)
            {
                baseAddress = GetSetting("ServiceBaseUrl:" + interfaceName, defaultValue: BaseUrl);
                if (string.IsNullOrEmpty(baseAddress))
                    throw new Core.Exceptions.NullReferenceException("Static BaseUrl property must be set on the ServiceClient class.");
            }
            if (basePath == null) basePath = GetSetting("ServiceBasePath:" + interfaceName, defaultValue: BasePath);
            if (extension == null)
            {
                extension = GetSetting("ServiceBasicHTTPExtension:" + interfaceName);
                if (string.IsNullOrEmpty(extension))
                    extension = GetSetting("ServiceBasicHTTPExtension", defaultValue: "basic");
            }

            return GetInternalBasicHttpChannel<TServiceType>(serviceId, messageSize, baseAddress, basePath, extension, false, useHttps);
        }

        /// <summary>
        /// Gets the channel using Basic HTTP protocol.
        /// </summary>
        /// <typeparam name="TServiceType">The type of the service.</typeparam>
        /// <param name="serviceId">The service id.</param>
        /// <param name="baseAddress">Base service address (such as www.domain.com)</param>
        /// <param name="basePath">Base path (such as "MyService" to create www.domain.com/MyService/basic)</param>
        /// <returns>Service</returns>
        public static TServiceType GetBasicHttpChannel<TServiceType>(string serviceId, string baseAddress, string basePath) where TServiceType : class
        {
            return GetInternalBasicHttpChannel<TServiceType>(serviceId, MessageSize.Undefined, baseAddress, basePath, true);
        }

        /// <summary>
        /// Gets a dedicated channel using Basic HTTP protocol.
        /// </summary>
        /// <typeparam name="TServiceType">The type of the service.</typeparam>
        /// <param name="serviceId">The service id.</param>
        /// <param name="baseAddress">Base service address (such as www.domain.com)</param>
        /// <param name="basePath">Base path (such as "MyService" to create www.domain.com/MyService/basic)</param>
        /// <returns>Service</returns>
        /// <remarks>Creates a channel exclusive to this caller. It is up to the caller to close the channel after use!</remarks>
        public static TServiceType GetBasicHttpChannelDedicated<TServiceType>(string serviceId, string baseAddress, string basePath) where TServiceType : class
        {
            return GetInternalBasicHttpChannel<TServiceType>(serviceId, MessageSize.Undefined, baseAddress, basePath, false);
        }

        /// <summary>
        /// Gets the channel using Basic HTTP protocol.
        /// </summary>
        /// <typeparam name="TServiceType">The type of the service.</typeparam>
        /// <param name="size">Message size</param>
        /// <param name="baseAddress">Base service address (such as www.domain.com)</param>
        /// <param name="basePath">Base path (such as "MyService" to create www.domain.com/MyService/basic)</param>
        /// <returns>Service</returns>
        public static TServiceType GetBasicHttpChannel<TServiceType>(MessageSize size, string baseAddress, string basePath) where TServiceType : class
        {
            return GetInternalBasicHttpChannel<TServiceType>(null, size, baseAddress, basePath, true);
        }

        /// <summary>
        /// Gets a dedicated channel using Basic HTTP protocol.
        /// </summary>
        /// <typeparam name="TServiceType">The type of the service.</typeparam>
        /// <param name="size">Message size</param>
        /// <param name="baseAddress">Base service address (such as www.domain.com)</param>
        /// <param name="basePath">Base path (such as "MyService" to create www.domain.com/MyService/basic)</param>
        /// <returns>Service</returns>
        /// <remarks>Creates a channel exclusive to this caller. It is up to the caller to close the channel after use!</remarks>
        public static TServiceType GetBasicHttpChannelDedicated<TServiceType>(MessageSize size, string baseAddress, string basePath) where TServiceType : class
        {
            return GetInternalBasicHttpChannel<TServiceType>(null, size, baseAddress, basePath, false);
        }

        /// <summary>
        /// Gets the channel using Basic HTTP protocol.
        /// </summary>
        /// <typeparam name="TServiceType">The type of the service.</typeparam>
        /// <param name="baseAddress">Base service address (such as www.domain.com)</param>
        /// <param name="basePath">Base path (such as "MyService" to create www.domain.com/MyService/basic)</param>
        /// <returns>Service</returns>
        public static TServiceType GetBasicHttpChannel<TServiceType>(string baseAddress, string basePath) where TServiceType : class
        {
            return GetInternalBasicHttpChannel<TServiceType>(null, MessageSize.Undefined, baseAddress, basePath, true);
        }

        /// <summary>
        /// Gets the channel using Basic HTTP protocol.
        /// </summary>
        /// <typeparam name="TServiceType">The type of the service.</typeparam>
        /// <param name="baseAddress">Base service address (such as www.domain.com)</param>
        /// <param name="basePath">Base path (such as "MyService" to create www.domain.com/MyService/basic)</param>
        /// <returns>Service</returns>
        /// <remarks>Creates a channel exclusive to this caller. It is up to the caller to close the channel after use!</remarks>
        public static TServiceType GetBasicHttpChannelDedicated<TServiceType>(string baseAddress, string basePath) where TServiceType : class
        {
            return GetInternalBasicHttpChannel<TServiceType>(null, MessageSize.Undefined, baseAddress, basePath, false);
        }

        /// <summary>Cached protocols</summary>
        private static readonly Dictionary<string, Protocol> CachedProtocols = new Dictionary<string, Protocol>();

        /// <summary>
        /// Gets the protocol for the specified service.
        /// </summary>
        /// <typeparam name="TServiceType">Type of the service (contract).</typeparam>
        /// <returns>Protocol.</returns>
        private static Protocol GetProtocol<TServiceType>()
        {
            var serviceType = typeof(TServiceType);
            var interfaceName = serviceType.Name;
            var key = "ServiceProtocol:" + interfaceName;

            lock (CachedProtocols)
                if (CacheSettings && CachedProtocols.ContainsKey(key))
                    return CachedProtocols[key];

            var protocolName = GetSetting(key).ToLower(CultureInfo.InvariantCulture);
            if (string.IsNullOrEmpty(protocolName)) protocolName = GetSetting("ServiceProtocol").ToLower(CultureInfo.InvariantCulture);
            if (string.IsNullOrEmpty(protocolName)) return Protocol.NetTcp;

            Protocol protocol;
            switch (protocolName)
            {
                case "inprocess":
                    protocol = Protocol.InProcess;
                    break;
                case "wshttp":
                    protocol = Protocol.WsHttp;
                    break;
                case "basichttp":
                    protocol = Protocol.BasicHttp;
                    break;
                case "rest":
                case "restjson":
                case "resthttpjson":
                    protocol = Protocol.RestHttpJson;
                    break;
                case "restxml":
                case "resthttpxml":
                    protocol = Protocol.RestHttpXml;
                    break;
                default:
                    protocol = Protocol.NetTcp;
                    break;
            }

            if (CacheSettings)
                lock (CachedProtocols)
                    if (CachedProtocols.ContainsKey(key))
                        CachedProtocols[key] = protocol;
                    else
                        CachedProtocols.Add(key, protocol);

            return protocol;
        }

        /// <summary>Cached ports</summary>
        private static readonly Dictionary<string, int> CachedPorts = new Dictionary<string, int>();

        /// <summary>
        /// Gets the service port.
        /// </summary>
        /// <typeparam name="TServiceType">The service type (contract).</typeparam>
        /// <returns>System.Int32.</returns>
        /// <exception cref="MissingConfigurationSettingException">ServicePort: + interfaceName +  - setting missing.</exception>
        private static int GetServicePort<TServiceType>()
        {
            var serviceType = typeof(TServiceType);
            var interfaceName = serviceType.Name;
            var key = "ServicePort:" + interfaceName;

            lock (CachedPorts)
                if (CacheSettings && CachedPorts.ContainsKey(key))
                    return CachedPorts[key];

            var portNumber = GetSetting(key);

            if (string.IsNullOrEmpty(portNumber)) throw new MissingConfigurationSettingException("ServicePort:" + interfaceName + " - setting missing.");

            int port;
            if (!int.TryParse(portNumber, out port)) throw new MissingConfigurationSettingException("ServicePort:" + interfaceName + " - invalid port configuration (" + portNumber + "). Must be integer.");

            if (CacheSettings)
                lock (CachedPorts)
                    if (CachedPorts.ContainsKey(key))
                        CachedPorts[key] = port;
                    else
                        CachedPorts.Add(key, port);

            return port;
        }

        /// <summary>
        /// Gets a channel to a data contract.
        /// </summary>
        /// <typeparam name="TServiceType">The type of the service type.</typeparam>
        /// <returns>Operations service</returns>
        /// <example>
        /// var service = ServiceClient.GetChannel&lt;IUserService&gt;();
        /// var result = service.GetUsers();
        /// </example>
        /// <remarks>
        /// Relies on service configurations to figure out which protocol (and so forth)
        /// to use for the desired service.
        /// </remarks>
        public static TServiceType GetChannel<TServiceType>() where TServiceType : class
        {
            var protocol = GetProtocol<TServiceType>();
            var size = GetMessageSize<TServiceType>();
            switch (protocol)
            {
                case Protocol.InProcess:
                    return GetChannel<TServiceType>(-1, string.Empty, protocol, size); // None of the settings other than the protocol matter here
                case Protocol.NetTcp:
                    var port = GetServicePort<TServiceType>();
                    var serviceId = GetServiceId<TServiceType>();
                    return GetChannel<TServiceType>(port, serviceId, protocol, size);

                case Protocol.BasicHttp:
                case Protocol.WsHttp:
                    var serviceId2 = GetServiceId<TServiceType>();
                    return GetChannel<TServiceType>(serviceId2, protocol, GetMessageSize(serviceId2));
                default:
                    return default(TServiceType);
            }
        }

        /// <summary>Opens a dedicated channel, performs an action on it, and immediately closes the channel</summary>
        /// <typeparam name="TServiceType">The type of the service type.</typeparam>
        /// <param name="action">The action that is to be performed</param>
        /// <returns>Operations service</returns>
        /// <example>
        /// ServiceClient.Call&lt;IUserService&gt;(s => s.GetAllUsers());
        /// </example>
        /// <remarks>
        /// Relies on service configurations to figure out which protocol (and so forth) to use for the desired service.
        /// Creates a channel exclusive to this caller. It is up to the caller to close the channel after use!
        /// </remarks>
        public static void Call<TServiceType>(Action<TServiceType> action) where TServiceType : class
        {
            var channel = GetChannelDedicated<TServiceType>();
            if (channel == null) return; // Exception event has fired by now, so callers can see what happened internally

            try
            {
                action(channel);
                CloseChannel(channel, false);
            }
            catch (Exception ex)
            {
                AbortChannel(channel, ex);

                if (MustRetryCall(ex))
                {
                    channel = GetChannelDedicated<TServiceType>();
                    try
                    {
                        action(channel);
                        CloseChannel(channel, false);
                    }
                    catch (Exception ex2)
                    {
                        AbortChannel(channel, ex2);
                    }
                }
            }
        }

        /// <summary>
        /// Gets a dedicated channel to a data contract.
        /// </summary>
        /// <typeparam name="TServiceType">The type of the service type.</typeparam>
        /// <returns>Operations service</returns>
        /// <example>
        /// var service = ServiceClient.GetChannelDedicated&lt;IUserService&gt;();
        /// var result = service.GetUsers();
        /// ServiceClient.CloseChannel(service);
        /// </example>
        /// <remarks>
        /// Relies on service configurations to figure out which protocol (and so forth) to use for the desired service.
        /// Creates a channel exclusive to this caller. It is up to the caller to close the channel after use!
        /// </remarks>
        public static TServiceType GetChannelDedicated<TServiceType>() where TServiceType : class
        {
            var protocol = GetProtocol<TServiceType>();
            var size = GetMessageSize<TServiceType>();
            switch (protocol)
            {
                case Protocol.InProcess:
                    return GetChannelDedicated<TServiceType>(-1, string.Empty, protocol, size); // None of the settings other than the protocol matter here
                case Protocol.NetTcp:
                    var port = GetServicePort<TServiceType>();
                    var serviceId = GetServiceId<TServiceType>();
                    return GetChannelDedicated<TServiceType>(port, serviceId, protocol, size);

                case Protocol.BasicHttp:
                case Protocol.WsHttp:
                    var serviceId2 = GetServiceId<TServiceType>();
                    return GetChannelDedicated<TServiceType>(serviceId2, protocol, GetMessageSize(serviceId2));
                case Protocol.RestHttpJson:
                    var serviceId3 = GetServiceId<TServiceType>();
                    var restUri = new Uri(GetSetting("RestServiceUrl:" + serviceId3));
                    var restHandler = new RestProxyHandler(restUri);
                    var proxy = TransparentProxyGenerator.GetProxy<TServiceType>(restHandler);
                    return proxy;
                default:
                    return default(TServiceType);
            }
        }

        /// <summary>
        /// Gets a channel to a data contract.
        /// </summary>
        /// <typeparam name="TServiceType">The type of the service type.</typeparam>
        /// <param name="messageSize">Size of the message.</param>
        /// <returns>Operations service</returns>
        /// <example>
        /// var service = ServiceClient.GetChannel&lt;IUserService&gt;();
        /// var result = service.GetUsers();
        /// </example>
        /// <remarks>
        /// Relies on service configurations to figure out which protocol (and so forth)
        /// to use for the desired service.
        /// </remarks>
        public static TServiceType GetChannel<TServiceType>(MessageSize messageSize) where TServiceType : class
        {
            var protocol = GetProtocol<TServiceType>();
            switch (protocol)
            {
                case Protocol.InProcess:
                    return GetChannel<TServiceType>(-1, string.Empty, protocol, MessageSize.Normal); // None of the settings other than the protocol matter here
                case Protocol.NetTcp:
                    var port = GetServicePort<TServiceType>();
                    var serviceId = GetServiceId<TServiceType>();
                    return GetChannel<TServiceType>(port, serviceId, protocol, messageSize);
                case Protocol.BasicHttp:
                case Protocol.WsHttp:
                    var serviceId2 = GetServiceId<TServiceType>();
                    return GetChannel<TServiceType>(serviceId2, protocol, messageSize);
                default:
                    return default(TServiceType);
            }
        }

        /// <summary>
        /// Gets a dedicated channel to a data contract.
        /// </summary>
        /// <typeparam name="TServiceType">The type of the service type.</typeparam>
        /// <param name="messageSize">Size of the message.</param>
        /// <returns>Operations service</returns>
        /// <example>
        /// var service = ServiceClient.GetChannelDedicated&lt;IUserService&gt;();
        /// var result = service.GetUsers();
        /// ServiceClient.CloseChannel(service);
        /// </example>
        /// <remarks>
        /// Relies on service configurations to figure out which protocol (and so forth) to use for the desired service.
        /// Creates a channel exclusive to this caller. It is up to the caller to close the channel after use!
        /// </remarks>
        public static TServiceType GetChannelDedicated<TServiceType>(MessageSize messageSize) where TServiceType : class
        {
            var protocol = GetProtocol<TServiceType>();
            switch (protocol)
            {
                case Protocol.InProcess:
                    return GetChannelDedicated<TServiceType>(-1, string.Empty, protocol, MessageSize.Normal); // None of the settings other than the protocol matter here
                case Protocol.NetTcp:
                    var port = GetServicePort<TServiceType>();
                    var serviceId = GetServiceId<TServiceType>();
                    return GetChannelDedicated<TServiceType>(port, serviceId, protocol, messageSize);
                case Protocol.BasicHttp:
                case Protocol.WsHttp:
                    var serviceId2 = GetServiceId<TServiceType>();
                    return GetChannelDedicated<TServiceType>(serviceId2, protocol, messageSize);
                default:
                    return default(TServiceType);
            }
        }

        /// <summary>Opens a channel, immediately performs an action on it, and then closes the channel right away</summary>
        /// <typeparam name="TServiceType">The type of the service type.</typeparam>
        /// <param name="messageSize">Size of the message.</param>
        /// <param name="action">The action that is to be performed</param>
        /// <remarks>Relies on service configurations to figure out which protocol (and so forth) to use for the desired service.</remarks>
        public static void Call<TServiceType>(MessageSize messageSize, Action<TServiceType> action) where TServiceType : class
        {
            var channel = GetChannelDedicated<TServiceType>(messageSize);
            if (channel == null) return; // Exception event has fired by now, so callers can see what happened internally

            try
            {
                action(channel);
                CloseChannel(channel, false);
            }
            catch (Exception ex)
            {
                AbortChannel(channel, ex);

                if (MustRetryCall(ex))
                {
                    channel = GetChannelDedicated<TServiceType>(messageSize);
                    try
                    {
                        action(channel);
                        CloseChannel(channel, false);
                    }
                    catch (Exception ex2)
                    {
                        AbortChannel(channel, ex2);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the channel.
        /// </summary>
        /// <typeparam name="TServiceType">The type of the service.</typeparam>
        /// <param name="port">The port.</param>
        /// <param name="protocol">The protocol used for transmission.</param>
        /// <returns>Service</returns>
        public static TServiceType GetChannel<TServiceType>(int port, Protocol protocol) where TServiceType : class
        {
            var serviceId = GetServiceId<TServiceType>();
            return GetChannel<TServiceType>(port, serviceId, protocol, GetMessageSize(serviceId));
        }

        /// <summary>
        /// Gets the dedicated channel.
        /// </summary>
        /// <typeparam name="TServiceType">The type of the service.</typeparam>
        /// <param name="port">The port.</param>
        /// <param name="protocol">The protocol used for transmission.</param>
        /// <returns>Service</returns>
        /// <remarks>Creates a channel exclusive to this caller. It is up to the caller to close the channel after use!</remarks>
        public static TServiceType GetChannelDedicated<TServiceType>(int port, Protocol protocol) where TServiceType : class
        {
            var serviceId = GetServiceId<TServiceType>();
            return GetChannelDedicated<TServiceType>(port, serviceId, protocol, GetMessageSize(serviceId));
        }

        /// <summary>Opens a channel, immediately performs an action on it, and closes the channel again</summary>
        /// <typeparam name="TServiceType">The type of the service.</typeparam>
        /// <param name="port">The port.</param>
        /// <param name="protocol">The protocol used for transmission.</param>
        /// <returns>Service</returns>
        /// <param name="action">The action that is to be performed</param>
        public static void Call<TServiceType>(int port, Protocol protocol, Action<TServiceType> action) where TServiceType : class
        {
            var channel = GetChannelDedicated<TServiceType>(port, protocol);
            if (channel == null) return; // Exception event has fired by now, so callers can see what happened internally

            try
            {
                action(channel);
                CloseChannel(channel, false);
            }
            catch (Exception ex)
            {
                AbortChannel(channel, ex);

                if (MustRetryCall(ex))
                {
                    channel = GetChannelDedicated<TServiceType>(port, protocol);
                    try
                    {
                        action(channel);
                        CloseChannel(channel, false);
                    }
                    catch (Exception ex2)
                    {
                        AbortChannel(channel, ex2);
                    }
                }
            }
        }

        /// <summary>The cached service ids</summary>
        private static readonly Dictionary<Type, string> CachedServiceIds = new Dictionary<Type, string>();

        /// <summary>
        /// Gets the service id.
        /// </summary>
        /// <typeparam name="TServiceType">The type of the service type.</typeparam>
        /// <returns></returns>
        private static string GetServiceId<TServiceType>()
        {
            lock (CachedServiceIds)
                if (CacheSettings && CachedServiceIds.ContainsKey(typeof(TServiceType)))
                    return CachedServiceIds[typeof(TServiceType)];

            string serviceId;
            var contractType = typeof(TServiceType);
            if (contractType.IsInterface)
                serviceId = contractType.Name;
            else
            {
                var interfaces = contractType.GetInterfaces();
                if (interfaces.Length == 1)
                    serviceId = interfaces[0].Name;
                else
                    throw new IndexOutOfBoundsException("Service information must be the service contract, not the service implementation type.");
            }

            if (CacheSettings)
                lock (CachedServiceIds)
                    if (CachedServiceIds.ContainsKey(typeof(TServiceType)))
                        CachedServiceIds[typeof(TServiceType)] = serviceId;
                    else
                        CachedServiceIds.Add(typeof(TServiceType), serviceId);

            return serviceId;
        }

        /// <summary>
        /// Gets the channel.
        /// </summary>
        /// <typeparam name="TServiceType">The type of the service.</typeparam>
        /// <param name="port">The port.</param>
        /// <param name="serviceId">The service id.</param>
        /// <param name="protocol">The protocol used for transmission.</param>
        /// <returns>Service</returns>
        public static TServiceType GetChannel<TServiceType>(int port, string serviceId, Protocol protocol) where TServiceType : class
        {
            return GetChannel<TServiceType>(port, serviceId, protocol, GetMessageSize(serviceId));
        }

        /// <summary>
        /// Gets the dedicated channel.
        /// </summary>
        /// <typeparam name="TServiceType">The type of the service.</typeparam>
        /// <param name="port">The port.</param>
        /// <param name="serviceId">The service id.</param>
        /// <param name="protocol">The protocol used for transmission.</param>
        /// <returns>Service</returns>
        /// <remarks>Creates a channel exclusive to this caller. It is up to the caller to close the channel after use!</remarks>
        public static TServiceType GetChannelDedicated<TServiceType>(int port, string serviceId, Protocol protocol) where TServiceType : class
        {
            return GetChannelDedicated<TServiceType>(port, serviceId, protocol, GetMessageSize(serviceId));
        }

        /// <summary>
        /// Calls a REST-based service
        /// </summary>
        /// <typeparam name="TServiceType">Service contract type.</typeparam>
        /// <typeparam name="TRequest">Request type</typeparam>
        /// <typeparam name="TResult">Expected result type</typeparam>
        /// <param name="methodName">Name of the method.</param>
        /// <param name="request">The request.</param>
        /// <param name="verb">The HTTP verb.</param>
        /// <param name="dataFormat">The data format.</param>
        /// <returns>Response type or null</returns>
        public static TResult CallREST<TServiceType, TRequest, TResult>(string methodName, TRequest request, RestHttpVerbs verb = RestHttpVerbs.Post, ServiceDataFormat dataFormat = ServiceDataFormat.Json)
            where TServiceType : class
            where TRequest : class, new()
            where TResult : class, new()
        {
            var serviceUrl = GetRESTServiceUrl(typeof(TServiceType).Name, methodName, dataFormat);
            if (string.IsNullOrEmpty(serviceUrl)) return null;

            try
            {

                if (BeforeServiceOperationCall != null)
                    RaiseBeforeServiceOperationCall(new BeforeServiceOperationCallEventArgs
                    {
                        MethodName = methodName,
                        ServiceContract = typeof (TServiceType),
                        InputDataContracts = new object[] {request}
                    });

                var startTimeStamp = Environment.TickCount;
                var data = CallRESTInternal(serviceUrl, request, verb, dataFormat);
                var deserializedData = GetDeserializedData<TResult>(data, dataFormat);

                if (AfterServiceOperationCall != null)
                    RaiseAfterServiceOperationCall(new AfterServiceOperationCallEventArgs
                    {
                        MethodName = methodName,
                        ServiceContract = typeof(TServiceType),
                        InputDataContracts = new object[] { request },
                        Response = deserializedData,
                        ServiceCallDuration = new TimeSpan(Environment.TickCount - startTimeStamp)
                    });

                return deserializedData;
            }
            catch (Exception ex)
            {
                if (MustRetryCall(ex))
                {
                    try
                    {
                        if (BeforeServiceOperationCall != null)
                            RaiseBeforeServiceOperationCall(new BeforeServiceOperationCallEventArgs
                            {
                                MethodName = methodName,
                                ServiceContract = typeof(TServiceType),
                                InputDataContracts = new object[] { request }
                            });

                        var data = CallRESTInternal(serviceUrl, request, verb, dataFormat);
                        var deserializedData = GetDeserializedData<TResult>(data, dataFormat);

                        if (BeforeServiceOperationCall != null)
                            RaiseBeforeServiceOperationCall(new BeforeServiceOperationCallEventArgs
                            {
                                MethodName = methodName,
                                ServiceContract = typeof(TServiceType),
                                InputDataContracts = new object[] { request }
                            });

                        return deserializedData;
                    }
                    catch (Exception ex2)
                    {
                        var channelException = ChannelException;
                        if (channelException != null)
                            channelException(null, new ChannelExceptionEventArgs(null, ex2));
                        return null;
                    }
                }

                var channelException2 = ChannelException;
                if (channelException2 != null)
                    channelException2(null, new ChannelExceptionEventArgs(null, ex));

                return null;
            }
        }

        /// <summary>
        /// Calls a REST-based service
        /// </summary>
        /// <typeparam name="TRequest">Request type</typeparam>
        /// <typeparam name="TResult">Expected result type</typeparam>
        /// <param name="service">The service name.</param>
        /// <param name="methodName">Name of the method.</param>
        /// <param name="request">The request.</param>
        /// <param name="verb">The HTTP verb.</param>
        /// <param name="dataFormat">The data format.</param>
        /// <returns>Response type or null</returns>
        public static TResult CallREST<TRequest, TResult>(string service, string methodName, TRequest request, RestHttpVerbs verb = RestHttpVerbs.Post, ServiceDataFormat dataFormat = ServiceDataFormat.Json)
            where TRequest : class, new()
            where TResult : class, new()
        {
            var serviceUrl = GetRESTServiceUrl(service, methodName, dataFormat);
            if (string.IsNullOrEmpty(serviceUrl)) return null;

            try
            {
                if (BeforeServiceOperationCall != null)
                    RaiseBeforeServiceOperationCall(new BeforeServiceOperationCallEventArgs
                    {
                        MethodName = methodName,
                        InputDataContracts = new object[] { request }
                    });

                var startTimeStamp = Environment.TickCount;
                var data = CallRESTInternal(serviceUrl, request, verb, dataFormat);
                var deserializedData = GetDeserializedData<TResult>(data, dataFormat);

                if (AfterServiceOperationCall != null)
                    RaiseAfterServiceOperationCall(new AfterServiceOperationCallEventArgs
                    {
                        MethodName = methodName,
                        InputDataContracts = new object[] { request },
                        Response = deserializedData,
                        ServiceCallDuration = new TimeSpan(Environment.TickCount - startTimeStamp)
                    });

                return deserializedData;
            }
            catch (Exception ex)
            {
                if (MustRetryCall(ex))
                {
                    try
                    {
                        if (BeforeServiceOperationCall != null)
                            RaiseBeforeServiceOperationCall(new BeforeServiceOperationCallEventArgs
                            {
                                MethodName = methodName,
                                InputDataContracts = new object[] { request }
                            });

                        var startTimeStamp = Environment.TickCount;
                        var data = CallRESTInternal(serviceUrl, request, verb, dataFormat);
                        var deserializedData = GetDeserializedData<TResult>(data, dataFormat);

                        if (AfterServiceOperationCall != null)
                            RaiseAfterServiceOperationCall(new AfterServiceOperationCallEventArgs
                            {
                                MethodName = methodName,
                                InputDataContracts = new object[] { request },
                                Response = deserializedData,
                                ServiceCallDuration = new TimeSpan(Environment.TickCount - startTimeStamp)
                            });

                        return deserializedData;
                    }
                    catch (Exception ex2)
                    {
                        if (ChannelException != null)
                            ChannelException(null, new ChannelExceptionEventArgs(null, ex2));
                        return null;
                    }
                }

                if (ChannelException != null)
                    ChannelException(null, new ChannelExceptionEventArgs(null, ex));

                return null;
            }
        }

        /// <summary>
        /// Calls a REST-based service
        /// </summary>
        /// <typeparam name="TRequest">Request type</typeparam>
        /// <param name="service">The service name.</param>
        /// <param name="methodName">Name of the method.</param>
        /// <param name="request">The request.</param>
        /// <param name="verb">The HTTP verb.</param>
        /// <param name="dataFormat">The data format.</param>
        /// <returns>String</returns>
        public static string CallREST<TRequest>(string service, string methodName, TRequest request, RestHttpVerbs verb = RestHttpVerbs.Post, ServiceDataFormat dataFormat = ServiceDataFormat.Json)
            where TRequest : class, new()
        {
            var serviceUrl = GetRESTServiceUrl(service, methodName, dataFormat);
            if (string.IsNullOrEmpty(serviceUrl)) return null;

            try
            {
                if (BeforeServiceOperationCall != null)
                    RaiseBeforeServiceOperationCall(new BeforeServiceOperationCallEventArgs
                    {
                        MethodName = methodName,
                        InputDataContracts = new object[] { request }
                    });

                var startTimeStamp = Environment.TickCount;
                var data = CallRESTInternal(serviceUrl, request, verb, dataFormat);
                var deserializedData = Encoding.UTF8.GetString(data);

                if (AfterServiceOperationCall != null)
                    RaiseAfterServiceOperationCall(new AfterServiceOperationCallEventArgs
                    {
                        MethodName = methodName,
                        InputDataContracts = new object[] { request },
                        Response = deserializedData,
                        ServiceCallDuration = new TimeSpan(Environment.TickCount - startTimeStamp)
                    });

                return deserializedData;
            }
            catch (Exception ex)
            {
                if (MustRetryCall(ex))
                {
                    try
                    {
                        if (BeforeServiceOperationCall != null)
                            RaiseBeforeServiceOperationCall(new BeforeServiceOperationCallEventArgs
                            {
                                MethodName = methodName,
                                InputDataContracts = new object[] { request }
                            });

                        var startTimeStamp = Environment.TickCount;
                        var data = CallRESTInternal(serviceUrl, request, verb, dataFormat);
                        var deserializedData = Encoding.UTF8.GetString(data);

                        if (AfterServiceOperationCall != null)
                            RaiseAfterServiceOperationCall(new AfterServiceOperationCallEventArgs
                            {
                                MethodName = methodName,
                                InputDataContracts = new object[] { request },
                                Response = deserializedData,
                                ServiceCallDuration = new TimeSpan(Environment.TickCount - startTimeStamp)
                            });

                        return deserializedData;
                    }
                    catch (Exception ex2)
                    {
                        if (ChannelException != null)
                            ChannelException(null, new ChannelExceptionEventArgs(null, ex2));
                        return null;
                    }
                }

                if (ChannelException != null)
                    ChannelException(null, new ChannelExceptionEventArgs(null, ex));

                return null;
            }
        }

        /// <summary>
        /// Calls a REST-based service
        /// </summary>
        /// <param name="service">The service name.</param>
        /// <param name="methodName">Name of the method.</param>
        /// <param name="request">The request.</param>
        /// <param name="verb">The HTTP verb.</param>
        /// <param name="dataFormat">The data format.</param>
        /// <returns>String</returns>
        public static string CallREST(string service, string methodName, string request, RestHttpVerbs verb = RestHttpVerbs.Post, ServiceDataFormat dataFormat = ServiceDataFormat.Json)
        {
            var serviceUrl = GetRESTServiceUrl(service, methodName, dataFormat);
            if (string.IsNullOrEmpty(serviceUrl)) return null;

            try
            {
                if (BeforeServiceOperationCall != null)
                    RaiseBeforeServiceOperationCall(new BeforeServiceOperationCallEventArgs
                    {
                        MethodName = methodName,
                        InputDataContracts = new object[] { request }
                    });

                var startTimeStamp = Environment.TickCount;
                var data = CallRESTInternal(serviceUrl, Encoding.UTF8.GetBytes(request), verb, dataFormat);
                var deserializedData = Encoding.UTF8.GetString(data);

                if (AfterServiceOperationCall != null)
                    RaiseAfterServiceOperationCall(new AfterServiceOperationCallEventArgs
                    {
                        MethodName = methodName,
                        InputDataContracts = new object[] { request },
                        Response = deserializedData,
                        ServiceCallDuration = new TimeSpan(Environment.TickCount - startTimeStamp)
                    });

                return deserializedData;
            }
            catch (Exception ex)
            {
                if (MustRetryCall(ex))
                {
                    try
                    {
                        if (BeforeServiceOperationCall != null)
                            RaiseBeforeServiceOperationCall(new BeforeServiceOperationCallEventArgs
                            {
                                MethodName = methodName,
                                InputDataContracts = new object[] { request }
                            });

                        var startTimeStamp = Environment.TickCount;
                        var data = CallRESTInternal(serviceUrl, Encoding.UTF8.GetBytes(request), verb, dataFormat);
                        var deserializedData = Encoding.UTF8.GetString(data);

                        if (AfterServiceOperationCall != null)
                            RaiseAfterServiceOperationCall(new AfterServiceOperationCallEventArgs
                            {
                                MethodName = methodName,
                                InputDataContracts = new object[] { request },
                                Response = deserializedData,
                                ServiceCallDuration = new TimeSpan(Environment.TickCount - startTimeStamp)
                            });

                        return deserializedData;
                    }
                    catch (Exception ex2)
                    {
                        if (ChannelException != null)
                            ChannelException(null, new ChannelExceptionEventArgs(null, ex2));
                        return null;
                    }
                }

                if (ChannelException != null)
                    ChannelException(null, new ChannelExceptionEventArgs(null, ex));

                return null;
            }
        }

        /// <summary>
        /// Calls a REST-based service
        /// </summary>
        /// <param name="serviceUrl">The complete service URL.</param>
        /// <param name="request">The request.</param>
        /// <param name="verb">The HTTP verb.</param>
        /// <param name="dataFormat">The data format.</param>
        /// <returns>String</returns>
        public static TResult CallREST<TRequest, TResult>(string serviceUrl, TRequest request, RestHttpVerbs verb = RestHttpVerbs.Post, ServiceDataFormat dataFormat = ServiceDataFormat.Json)
            where TRequest : class, new()
            where TResult : class, new()
        {
            try
            {
                if (BeforeServiceOperationCall != null)
                    RaiseBeforeServiceOperationCall(new BeforeServiceOperationCallEventArgs
                    {
                        MethodName = serviceUrl,
                        InputDataContracts = new object[] { request },
                    });

                var startTimeStamp = Environment.TickCount;
                var data = CallRESTInternal(serviceUrl, request, verb, dataFormat);
                var deserializedData = GetDeserializedData<TResult>(data, dataFormat);

                if (AfterServiceOperationCall != null)
                    RaiseAfterServiceOperationCall(new AfterServiceOperationCallEventArgs
                    {
                        MethodName = serviceUrl,
                        InputDataContracts = new object[] { request },
                        Response = deserializedData,
                        ServiceCallDuration = new TimeSpan(Environment.TickCount - startTimeStamp)
                    });

                return deserializedData;
            }
            catch (Exception ex)
            {
                if (MustRetryCall(ex))
                {
                    try
                    {
                        if (BeforeServiceOperationCall != null)
                            RaiseBeforeServiceOperationCall(new BeforeServiceOperationCallEventArgs
                            {
                                MethodName = serviceUrl,
                                InputDataContracts = new object[] { request },
                            });

                        var startTimeStamp = Environment.TickCount;
                        var data = CallRESTInternal(serviceUrl, request, verb, dataFormat);
                        var deserializedData = GetDeserializedData<TResult>(data, dataFormat);

                        if (AfterServiceOperationCall != null)
                            RaiseAfterServiceOperationCall(new AfterServiceOperationCallEventArgs
                            {
                                MethodName = serviceUrl,
                                InputDataContracts = new object[] { request },
                                Response = deserializedData,
                                ServiceCallDuration = new TimeSpan(Environment.TickCount - startTimeStamp)
                            });

                        return deserializedData;
                    }
                    catch (Exception ex2)
                    {
                        if (ChannelException != null)
                            ChannelException(null, new ChannelExceptionEventArgs(null, ex2));
                        return null;
                    }
                }

                if (ChannelException != null)
                    ChannelException(null, new ChannelExceptionEventArgs(null, ex));

                return null;
            }
        }

        /// <summary>
        /// Calls a REST-based service
        /// </summary>
        /// <param name="serviceUrl">The complete service URL.</param>
        /// <param name="request">The request.</param>
        /// <param name="verb">The HTTP verb.</param>
        /// <param name="dataFormat">The data format.</param>
        /// <returns>String</returns>
        public static string CallREST(string serviceUrl, string request, RestHttpVerbs verb = RestHttpVerbs.Post, ServiceDataFormat dataFormat = ServiceDataFormat.Json)
        {
            try
            {
                if (BeforeServiceOperationCall != null)
                    RaiseBeforeServiceOperationCall(new BeforeServiceOperationCallEventArgs
                    {
                        MethodName = serviceUrl,
                        InputDataContracts = new object[] { request }
                    });

                var startTimeStamp = Environment.TickCount;
                var data = CallRESTInternal(serviceUrl, Encoding.UTF8.GetBytes(request), verb, dataFormat);
                var deserializedData = Encoding.UTF8.GetString(data);

                if (AfterServiceOperationCall != null)
                    RaiseAfterServiceOperationCall(new AfterServiceOperationCallEventArgs
                    {
                        MethodName = serviceUrl,
                        InputDataContracts = new object[] { request },
                        Response = deserializedData,
                        ServiceCallDuration = new TimeSpan(Environment.TickCount - startTimeStamp)
                    });

                return deserializedData;
            }
            catch (Exception ex)
            {
                if (MustRetryCall(ex))
                {
                    try
                    {
                        if (BeforeServiceOperationCall != null)
                            RaiseBeforeServiceOperationCall(new BeforeServiceOperationCallEventArgs
                            {
                                MethodName = serviceUrl,
                                InputDataContracts = new object[] { request }
                            });

                        var startTimeStamp = Environment.TickCount;
                        var data = CallRESTInternal(serviceUrl, Encoding.UTF8.GetBytes(request), verb, dataFormat);
                        var deserializedData = Encoding.UTF8.GetString(data);

                        if (AfterServiceOperationCall != null)
                            RaiseAfterServiceOperationCall(new AfterServiceOperationCallEventArgs
                            {
                                MethodName = serviceUrl,
                                InputDataContracts = new object[] { request },
                                Response = deserializedData,
                                ServiceCallDuration = new TimeSpan(Environment.TickCount - startTimeStamp)
                            });

                        return deserializedData;
                    }
                    catch (Exception ex2)
                    {
                        if (ChannelException != null)
                            ChannelException(null, new ChannelExceptionEventArgs(null, ex2));
                        return null;
                    }
                }

                if (ChannelException != null)
                    ChannelException(null, new ChannelExceptionEventArgs(null, ex));

                return null;
            }
        }

        private static byte[] CallRESTInternal<TRequest>(string serviceUrl, TRequest request, RestHttpVerbs verb, ServiceDataFormat dataFormat)
        {
            using (var client = new WebClient())
            {
                client.Headers["Content-type"] = dataFormat == ServiceDataFormat.Json ? "application/json" : "application/xml";
                var uploadData = GetSerializedData(request, dataFormat);
                return client.UploadData(serviceUrl, verb.ToString().ToUpperInvariant(), uploadData);
            }
        }
        private static byte[] CallRESTInternal(string serviceUrl, byte[] request, RestHttpVerbs verb, ServiceDataFormat dataFormat)
        {
            using (var client = new WebClient())
            {
                client.Headers["Content-type"] = dataFormat == ServiceDataFormat.Json ? "application/json" : "application/xml";
                return client.UploadData(serviceUrl, verb.ToString().ToUpperInvariant(), request);
            }
        }

        private static string GetRESTServiceUrl(string service, string methodName, ServiceDataFormat dataFormat)
        {
            var fullAddress = GetSetting("ServiceUrl:" + service);
            if (!string.IsNullOrEmpty(fullAddress))
                return fullAddress + "/" + methodName;

            var baseAddress = GetSetting("ServiceBaseUrl:" + service, defaultValue: BaseUrl);
            if (string.IsNullOrEmpty(baseAddress))
                throw new Core.Exceptions.NullReferenceException("Static BaseUrl property must be set on the ServiceClient class.");
            var basePath = GetSetting("ServiceBasePath:" + methodName, defaultValue: BasePath);
            if (!string.IsNullOrEmpty(basePath))
                baseAddress += "/" + basePath;
            baseAddress += "/" + service;

            var extension = GetSetting("ServiceRestExtension:" + service, defaultValue: "rest");
            if (!string.IsNullOrEmpty(extension) && !extension.StartsWith("/")) extension = "/" + extension;
            if (!string.IsNullOrEmpty(extension)) baseAddress += extension;

            if (dataFormat == ServiceDataFormat.Xml)
            {
                var formatExtension = GetSetting("ServiceRestXmlFormatExtension:" + service, defaultValue: "xml");
                if (!string.IsNullOrEmpty(formatExtension)) baseAddress += "/" + formatExtension;
            }
            else
            {
                var formatExtension = GetSetting("ServiceRestJsonFormatExtension:" + service, defaultValue: "Json");
                if (!string.IsNullOrEmpty(formatExtension)) baseAddress += "/" + formatExtension;
            }

            baseAddress = "http://" + baseAddress + "/" + methodName;

            return baseAddress;
        }

        private static byte[] GetSerializedData<TData>(TData data, ServiceDataFormat dataFormat)
        {
            using (var stream = new MemoryStream())
            {
                if (dataFormat == ServiceDataFormat.Json)
                {
                    var serializer = new DataContractJsonSerializer(typeof(TData));
                    serializer.WriteObject(stream, data);
                }
                else
                {
                    var serializer = new DataContractSerializer(typeof(TData));
                    serializer.WriteObject(stream, data);
                }
                return stream.ToArray();
            }
        }

        private static TData GetDeserializedData<TData>(byte[] data, ServiceDataFormat dataFormat)
        {
            using (var stream = new MemoryStream(data))
            {
                if (dataFormat == ServiceDataFormat.Json)
                {
                    var serializer = new DataContractJsonSerializer(typeof(TData));
                    return (TData)serializer.ReadObject(stream);
                }
                else
                {
                    var serializer = new DataContractSerializer(typeof(TData));
                    return (TData)serializer.ReadObject(stream);
                }
            }
        }

        /// <summary>Opens a dedicated channel, performs an action on it, and immediately closes the channel</summary>
        /// <typeparam name="TServiceType">The type of the service.</typeparam>
        /// <param name="port">The port.</param>
        /// <param name="serviceId">The service id.</param>
        /// <param name="protocol">The protocol used for transmission.</param>
        /// <param name="action">The action that is to be performed</param>
        public static void Call<TServiceType>(int port, string serviceId, Protocol protocol, Action<TServiceType> action) where TServiceType : class
        {
            var channel = GetChannelDedicated<TServiceType>(port, serviceId, protocol);
            if (channel == null) return; // Exception event has fired by now, so callers can see what happened internally

            try
            {
                action(channel);
                CloseChannel(channel, false);
            }
            catch (Exception ex)
            {
                AbortChannel(channel, ex);

                if (MustRetryCall(ex))
                {
                    channel = GetChannelDedicated<TServiceType>(port, serviceId, protocol);
                    try
                    {
                        action(channel);
                        CloseChannel(channel, false);
                    }
                    catch (Exception ex2)
                    {
                        AbortChannel(channel, ex2);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the channel.
        /// </summary>
        /// <typeparam name="TServiceType">The type of the service.</typeparam>
        /// <param name="serviceId">The service id.</param>
        /// <param name="protocol">The protocol used for transmission.</param>
        /// <param name="messageSize">Size of the messages sent back and forth.</param>
        /// <returns>Service</returns>
        public static TServiceType GetChannel<TServiceType>(string serviceId, Protocol protocol, MessageSize messageSize) where TServiceType : class
        {
            return GetChannel<TServiceType>(80, serviceId, protocol, messageSize);
        }

        /// <summary>
        /// Gets the dedicated channel.
        /// </summary>
        /// <typeparam name="TServiceType">The type of the service.</typeparam>
        /// <param name="serviceId">The service id.</param>
        /// <param name="protocol">The protocol used for transmission.</param>
        /// <param name="messageSize">Size of the messages sent back and forth.</param>
        /// <returns>Service</returns>
        public static TServiceType GetChannelDedicated<TServiceType>(string serviceId, Protocol protocol, MessageSize messageSize) where TServiceType : class
        {
            return GetChannelDedicated<TServiceType>(80, serviceId, protocol, messageSize);
        }

        /// <summary>Opens a dedicated channel, immediately performs an action on it, and closes the channel again</summary>
        /// <typeparam name="TServiceType">The type of the service.</typeparam>
        /// <param name="serviceId">The service id.</param>
        /// <param name="protocol">The protocol used for transmission.</param>
        /// <param name="messageSize">Size of the messages sent back and forth.</param>
        /// <param name="action">The action that is to be performed</param>
        public static void Call<TServiceType>(string serviceId, Protocol protocol, MessageSize messageSize, Action<TServiceType> action) where TServiceType : class
        {
            var channel = GetChannelDedicated<TServiceType>(serviceId, protocol, messageSize);
            if (channel == null) return; // Exception event has fired by now, so callers can see what happened internally

            try
            {
                action(channel);
                CloseChannel(channel, false);
            }
            catch (Exception ex)
            {
                AbortChannel(channel, ex);

                if (MustRetryCall(ex))
                {
                    channel = GetChannelDedicated<TServiceType>(serviceId, protocol, messageSize);
                    try
                    {
                        action(channel);
                        CloseChannel(channel, false);
                    }
                    catch (Exception ex2)
                    {
                        AbortChannel(channel, ex2);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the channel.
        /// </summary>
        /// <typeparam name="TServiceType">The type of the service.</typeparam>
        /// <param name="port">The port.</param>
        /// <param name="serviceId">The service id.</param>
        /// <param name="protocol">The protocol used for transmission.</param>
        /// <param name="messageSize">Size of the messages sent back and forth.</param>
        /// <returns>Service</returns>
        public static TServiceType GetChannel<TServiceType>(int port, string serviceId, Protocol protocol, MessageSize messageSize) where TServiceType : class
        {
            switch (protocol)
            {
                case Protocol.NetTcp:
                    return GetInternalNetTcpChannel<TServiceType>(port, serviceId, messageSize, true);
                case Protocol.InProcess:
                    return GetInternalInProcessChannel<TServiceType>();
                case Protocol.BasicHttp:
                    return GetInternalBasicHttpChannel<TServiceType>(serviceId, messageSize, true);
                case Protocol.WsHttp:
                    return GetInternalWsHttpChannel<TServiceType>(serviceId, messageSize, true);
                default:
                    return default(TServiceType);
            }
        }

        /// <summary>
        /// Gets a dedicated channel.
        /// </summary>
        /// <typeparam name="TServiceType">The type of the service.</typeparam>
        /// <param name="port">The port.</param>
        /// <param name="serviceId">The service id.</param>
        /// <param name="protocol">The protocol used for transmission.</param>
        /// <param name="messageSize">Size of the messages sent back and forth.</param>
        /// <returns>Service</returns>
        /// <remarks>Creates a channel exclusive to this caller. It is up to the caller to close the channel after use!</remarks>
        public static TServiceType GetChannelDedicated<TServiceType>(int port, string serviceId, Protocol protocol, MessageSize messageSize) where TServiceType : class
        {
            switch (protocol)
            {
                case Protocol.NetTcp:
                    return GetInternalNetTcpChannel<TServiceType>(port, serviceId, messageSize, false);
                case Protocol.InProcess:
                    return GetInternalInProcessChannel<TServiceType>();
                case Protocol.BasicHttp:
                    return GetInternalBasicHttpChannel<TServiceType>(serviceId, messageSize, false);
                case Protocol.WsHttp:
                    return GetInternalWsHttpChannel<TServiceType>(serviceId, messageSize, false);
                default:
                    return default(TServiceType);
            }
        }

        /// <summary>Opens a dedicated channel, immediately performs an action, and closes the channel</summary>
        /// <typeparam name="TServiceType">The type of the service.</typeparam>
        /// <param name="port">The port.</param>
        /// <param name="serviceId">The service id.</param>
        /// <param name="protocol">The protocol used for transmission.</param>
        /// <param name="messageSize">Size of the messages sent back and forth.</param>
        /// <param name="action">The action that is to be performed</param>
        /// <returns>Service</returns>
        public static void Call<TServiceType>(int port, string serviceId, Protocol protocol, MessageSize messageSize, Action<TServiceType> action) where TServiceType : class
        {
            var channel = GetChannelDedicated<TServiceType>(port, serviceId, protocol, messageSize);
            if (channel == null) return; // Exception event has fired by now, so callers can see what happened internally

            try
            {
                action(channel);
                CloseChannel(channel, false);
            }
            catch (Exception ex)
            {
                AbortChannel(channel, ex);

                if (MustRetryCall(ex))
                {
                    channel = GetChannelDedicated<TServiceType>(port, serviceId, protocol, messageSize);
                    try
                    {
                        action(channel);
                        CloseChannel(channel, false);
                    }
                    catch (Exception ex2)
                    {
                        AbortChannel(channel, ex2);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the channel.
        /// </summary>
        /// <typeparam name="TServiceType">The type of the service.</typeparam>
        /// <param name="port">The port.</param>
        /// <param name="serviceId">The service id.</param>
        /// <param name="protocol">The protocol used for transmission.</param>
        /// <param name="messageSize">Size of the messages sent back and forth.</param>
        /// <param name="serviceUri">The service URI.</param>
        /// <returns>Service</returns>
        public static TServiceType GetChannel<TServiceType>(int port, string serviceId, Protocol protocol, MessageSize messageSize, Uri serviceUri) where TServiceType : class
        {
            switch (protocol)
            {
                case Protocol.NetTcp:
                    return GetInternalNetTcpChannel<TServiceType>(messageSize, serviceUri, true);
                case Protocol.InProcess:
                    return GetInternalInProcessChannel<TServiceType>();
                case Protocol.BasicHttp:
                    return GetInternalBasicHttpChannel<TServiceType>(messageSize, serviceUri, true);
                case Protocol.WsHttp:
                    return GetInternalWsHttpChannel<TServiceType>(messageSize, serviceUri, true);
                default:
                    return default(TServiceType);
            }
        }

        /// <summary>
        /// Opens a dedicated channel and immediately performs an action on it and closes the channel
        /// </summary>
        /// <typeparam name="TServiceType">The type of the service.</typeparam>
        /// <param name="port">The port.</param>
        /// <param name="serviceId">The service id.</param>
        /// <param name="protocol">The protocol used for transmission.</param>
        /// <param name="messageSize">Size of the messages sent back and forth.</param>
        /// <param name="serviceUri">The service URI.</param>
        /// <param name="action">The action that is to be performed on the channel</param>
        public static void GetChannel<TServiceType>(int port, string serviceId, Protocol protocol, MessageSize messageSize, Uri serviceUri, Action<TServiceType> action) where TServiceType : class
        {
            var channel = GetChannelDedicated<TServiceType>(port, serviceId, protocol, messageSize, serviceUri);
            try
            {
                action(channel);
                CloseChannel(channel, false);
            }
            catch (Exception ex)
            {
                AbortChannel(channel, ex);

                if (MustRetryCall(ex))
                {
                    channel = GetChannelDedicated<TServiceType>(port, serviceId, protocol, messageSize, serviceUri);
                    try
                    {
                        action(channel);
                        CloseChannel(channel, false);
                    }
                    catch (Exception ex2)
                    {
                        AbortChannel(channel, ex2);
                    }
                }
            }
        }

        /// <summary>
        /// Gets a dedicated channel
        /// </summary>
        /// <typeparam name="TServiceType">The type of the service.</typeparam>
        /// <param name="port">The port.</param>
        /// <param name="serviceId">The service id.</param>
        /// <param name="protocol">The protocol used for transmission.</param>
        /// <param name="messageSize">Size of the messages sent back and forth.</param>
        /// <param name="serviceUri">The service URI.</param>
        /// <returns>Service</returns>
        /// <remarks>Creates a channel exclusive to this caller. It is up to the caller to close the channel after use!</remarks>
        public static TServiceType GetChannelDedicated<TServiceType>(int port, string serviceId, Protocol protocol, MessageSize messageSize, Uri serviceUri) where TServiceType : class
        {
            switch (protocol)
            {
                case Protocol.NetTcp:
                    return GetInternalNetTcpChannel<TServiceType>(messageSize, serviceUri, false);
                case Protocol.InProcess:
                    return GetInternalInProcessChannel<TServiceType>();
                case Protocol.BasicHttp:
                    return GetInternalBasicHttpChannel<TServiceType>(messageSize, serviceUri, false);
                case Protocol.WsHttp:
                    return GetInternalWsHttpChannel<TServiceType>(messageSize, serviceUri, false);
                default:
                    return default(TServiceType);
            }
        }

        /// <summary>Opens a dedicated channel, immediately performs an action on it and then immediately closes the channel</summary>
        /// <typeparam name="TServiceType">The type of the service.</typeparam>
        /// <param name="port">The port.</param>
        /// <param name="serviceId">The service id.</param>
        /// <param name="protocol">The protocol used for transmission.</param>
        /// <param name="messageSize">Size of the messages sent back and forth.</param>
        /// <param name="serviceUri">The service URI.</param>
        /// <param name="action">The action that is to be performed</param>
        public static void Call<TServiceType>(int port, string serviceId, Protocol protocol, MessageSize messageSize, Uri serviceUri, Action<TServiceType> action) where TServiceType : class
        {
            var channel = GetChannelDedicated<TServiceType>(port, serviceId, protocol, messageSize, serviceUri);
            if (channel == null) return; // Exception event has fired by now, so callers can see what happened internally

            try
            {
                action(channel);
                CloseChannel(channel, false);
            }
            catch (Exception ex)
            {
                AbortChannel(channel, ex);

                if (MustRetryCall(ex))
                {
                    channel = GetChannelDedicated<TServiceType>(port, serviceId, protocol, messageSize, serviceUri);
                    try
                    {
                        action(channel);
                        CloseChannel(channel, false);
                    }
                    catch (Exception ex2)
                    {
                        AbortChannel(channel, ex2);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the channel.
        /// </summary>
        /// <param name="serviceType">Service Type (contract)</param>
        /// <param name="port">The port.</param>
        /// <param name="serviceId">The service id.</param>
        /// <param name="protocol">The protocol used for transmission.</param>
        /// <param name="messageSize">Size of the messages sent back and forth.</param>
        /// <param name="serviceUri">The service URI.</param>
        /// <returns>Service</returns>
        public static object GetChannel(Type serviceType, int port, string serviceId, Protocol protocol, MessageSize messageSize, Uri serviceUri)
        {
            switch (protocol)
            {
                case Protocol.NetTcp:
                    return GetInternalNetTcpChannel(serviceType, messageSize, serviceUri, true);
                case Protocol.InProcess:
                    return GetInternalInProcessChannel(serviceType);
                case Protocol.BasicHttp:
                    return GetInternalBasicHttpChannel(serviceType, messageSize, serviceUri, true);
                case Protocol.WsHttp:
                    return GetInternalWsHttpChannel(serviceType, messageSize, serviceUri, true);
                default:
                    return null;
            }
        }

        /// <summary>
        /// Gets a dedicated channel
        /// </summary>
        /// <param name="serviceType">Service Type (contract)</param>
        /// <param name="port">The port.</param>
        /// <param name="serviceId">The service id.</param>
        /// <param name="protocol">The protocol used for transmission.</param>
        /// <param name="messageSize">Size of the messages sent back and forth.</param>
        /// <param name="serviceUri">The service URI.</param>
        /// <returns>Service</returns>
        /// <remarks>Creates a channel exclusive to this caller. It is up to the caller to close the channel after use!</remarks>
        public static object GetChannelDedicated(Type serviceType, int port, string serviceId, Protocol protocol, MessageSize messageSize, Uri serviceUri)
        {
            switch (protocol)
            {
                case Protocol.NetTcp:
                    return GetInternalNetTcpChannel(serviceType, messageSize, serviceUri, false);
                case Protocol.InProcess:
                    return GetInternalInProcessChannel(serviceType);
                case Protocol.BasicHttp:
                    return GetInternalBasicHttpChannel(serviceType, messageSize, serviceUri, false);
                case Protocol.WsHttp:
                    return GetInternalWsHttpChannel(serviceType, messageSize, serviceUri, false);
                default:
                    return null;
            }
        }

        /// <summary>
        /// Gets the internal in-process channel.
        /// </summary>
        /// <typeparam name="TServiceType">The type of the service type.</typeparam>
        /// <returns></returns>
        private static TServiceType GetInternalInProcessChannel<TServiceType>()
        {
            return ServiceGardenLocal.GetService<TServiceType>();
        }

        /// <summary>
        /// Gets the internal in-process channel.
        /// </summary>
        /// <param name="serviceType">Service Type (contract)</param>
        /// <returns></returns>
        private static object GetInternalInProcessChannel(Type serviceType)
        {
            return ServiceGardenLocal.GetService(serviceType);
        }

        /// <summary>
        /// Gets the internal net TCP channel.
        /// </summary>
        /// <typeparam name="TServiceType">The type of the service type.</typeparam>
        /// <param name="port">The port.</param>
        /// <param name="serviceId">The service id.</param>
        /// <param name="messageSize">Size of the message.</param>
        /// <param name="useCachedChannel">If true, the system will use a channel that was cached rather than creating a new one (if possible)</param>
        /// <returns>TCP/IP channel</returns>
        private static TServiceType GetInternalNetTcpChannel<TServiceType>(int port, string serviceId, MessageSize messageSize, bool useCachedChannel) where TServiceType : class
        {
            return GetInternalNetTcpChannel<TServiceType>(port, serviceId, messageSize, null, null, useCachedChannel);
        }

        /// <summary>
        /// Creates a standard instance of a NetTcp binding and performs some basic configurations.
        /// </summary>
        /// <param name="serviceId">The service identifier.</param>
        /// <returns>NetTcpBinding.</returns>
        private static NetTcpBinding GetStandardNetTcpBinding(string serviceId = "")
        {
            var securityMode = GetSecurityMode(serviceId);
            var binding = new NetTcpBinding(securityMode)
            {
                CloseTimeout = TimeSpan.MaxValue,
                OpenTimeout = TimeSpan.MaxValue,
                ReceiveTimeout = TimeSpan.MaxValue,
                SendTimeout = TimeSpan.MaxValue
            };

            if (binding.ReliableSession.Enabled)
                binding.ReliableSession.InactivityTimeout = TimeSpan.MaxValue;

            return binding;
        }

        /// <summary>
        /// Gets the internal net TCP channel.
        /// </summary>
        /// <typeparam name="TServiceType">The type of the service type.</typeparam>
        /// <param name="port">The port.</param>
        /// <param name="serviceId">The service id.</param>
        /// <param name="messageSize">Size of the message.</param>
        /// <param name="baseAddress">Base service address (such as www.domain.com)</param>
        /// <param name="basePath">Base path (such as "MyService" to create www.domain.com/MyService)</param>
        /// <param name="useCachedChannel">If true, the system will use a channel that was cached rather than creating a new one (if possible)</param>
        /// <returns>TCP/IP channel</returns>
        private static TServiceType GetInternalNetTcpChannel<TServiceType>(int port, string serviceId, MessageSize messageSize, string baseAddress, string basePath, bool useCachedChannel) where TServiceType : class
        {
            var serviceFullAddress = GetSetting("ServiceUrl:" + serviceId);

            // Checking parameter values
            if (serviceId == null) serviceId = GetServiceId<TServiceType>();
            if (messageSize == MessageSize.Undefined) messageSize = GetMessageSize<TServiceType>();
            var interfaceName = typeof(TServiceType).Name;
            if (baseAddress == null)
            {
                baseAddress = GetSetting("ServiceBaseUrl:" + interfaceName, defaultValue: BaseUrl);
                if (string.IsNullOrEmpty(baseAddress))
                    throw new Core.Exceptions.NullReferenceException("Static BaseUrl property must be set on the ServiceClient class.");
            }
            if (basePath == null) basePath = GetSetting("ServiceBasePath:" + interfaceName, defaultValue: BasePath);

            // Ready to start processing
            if (useCachedChannel)
            {
                var cachedChannelIndex = GetChannelCacheIndex(typeof(TServiceType), messageSize, port, serviceId, baseAddress, basePath);
                if (cachedChannelIndex > -1) return GetCachedChannel<TServiceType>(cachedChannelIndex);
            }

            var binding = GetStandardNetTcpBinding(serviceId);
            ServiceHelper.ConfigureMessageSizeOnNetTcpBinding(messageSize, binding);

            if (!string.IsNullOrEmpty(basePath) && !basePath.EndsWith("/")) basePath += "/";
            if (string.IsNullOrEmpty(serviceFullAddress))
                serviceFullAddress = "net.tcp://" + baseAddress + ":" + port + "/" + basePath + serviceId;

            // We allow fiddling of the endpoint address by means of an event
            var beforeEndpointAdded = BeforeEndpointAdded;
            if (beforeEndpointAdded != null)
            {
                var endpointEventArgs = new EndpointAddedEventArgs
                {
                    ServiceFullAddress = serviceFullAddress,
                    Binding = binding,
                    ContractType = typeof(TServiceType),
                    ServiceId = serviceId
                };
                beforeEndpointAdded(null, endpointEventArgs);
                serviceFullAddress = endpointEventArgs.ServiceFullAddress;
            }

            // Finally, we create the new endpoint
            var endpoint = new EndpointAddress(serviceFullAddress);

            TServiceType proxy = null;
            try
            {
                proxy = CreateChannel<TServiceType>(binding, endpoint);

                var channel = GetChannelFromProxy(proxy);
                if (channel != null)
                {
                    var beforeChannelOpens = BeforeChannelOpens;
                    if (beforeChannelOpens != null)
                        beforeChannelOpens(null, new BeforeChannelOpensEventArgs
                        {
                            Channel = channel,
                            BaseAddress = baseAddress,
                            BasePath = basePath,
                            ChannelType = typeof(TServiceType),
                            MessageSize = messageSize,
                            Port = port,
                            ServiceId = serviceId
                        });

                    OpenChannel(channel);
                }

                if (useCachedChannel)
                    lock (OpenChannels)
                        OpenChannels.Add(new OpenChannelInformation { Port = port, ServiceId = serviceId, MessageSize = messageSize, BaseUrl = baseAddress, BasePath = basePath, Channel = proxy, ChannelType = typeof(TServiceType) });

                return proxy;
            }
            catch (Exception ex)
            {
                if (ChannelException != null)
                    ChannelException(null, new ChannelExceptionEventArgs(GetChannelFromProxy(proxy), ex));

                if (!HandleChannelExceptions) throw;

                return null;
            }
        }

        private static void OpenChannel(IClientChannel channel)
        {
            if (channel.State != CommunicationState.Opened && channel.State != CommunicationState.Opening)
                channel.Open();
        }

        /// <summary>
        /// Event fires before a channel opens (can be used to programmatically configure a channel if need be)
        /// </summary>
        public static event EventHandler<BeforeChannelOpensEventArgs> BeforeChannelOpens;

        /// <summary>
        /// Fires before a new endpoint is added (can be used to manipulate the client before it is opened)
        /// </summary>
        public static event EventHandler<EndpointAddedEventArgs> BeforeEndpointAdded;

        /// <summary>
        /// Creates a channel using a (potentially cached) channel factory
        /// </summary>
        /// <typeparam name="TServiceType">Service type to create</typeparam>
        /// <param name="binding">The binding.</param>
        /// <param name="endpoint">The endpoint.</param>
        /// <returns>Service type</returns>
        /// <exception cref="Core">Error creating service client.</exception>
        private static TServiceType CreateChannel<TServiceType>(Binding binding, EndpointAddress endpoint) where TServiceType : class
        {
            //lock (CachedChannelFactories)
            //{
                var bindingName = binding.GetType().ToString();
                //if (CachedChannelFactories.ContainsKey(bindingName) && CachedChannelFactories[bindingName].ContainsKey(endpoint) && CachedChannelFactories[bindingName][endpoint].ContainsKey(typeof(TServiceType)))
                //{
                //    // It looks like we have the channel factory cached, so we try to use it
                //    var cachedFactory = CachedChannelFactories[bindingName][endpoint][typeof(TServiceType)];
                //    if (cachedFactory == null) RemoveCachedChannelFactory<TServiceType>(binding, endpoint);
                //    else if (cachedFactory.State == CommunicationState.Faulted) RemoveCachedChannelFactory<TServiceType>(binding, endpoint);
                //    {
                //        try
                //        {
                //            var typedFactory = cachedFactory as ChannelFactory<TServiceType>;
                //            if (typedFactory == null) RemoveCachedChannelFactory<TServiceType>(binding, endpoint);
                //            else
                //            {
                //                var proxy2 = typedFactory.CreateChannel();
                //                if (proxy2 != null)
                //                {
                //                    var channelCreatedByCachedFactory = GetChannelFromProxy(proxy2);
                //                    if (channelCreatedByCachedFactory != null && channelCreatedByCachedFactory.State == CommunicationState.Faulted)
                //                        proxy2 = null; // We can't use this proxy, since it somehow ended up faulted.
                //                }

                //                if (proxy2 != null)
                //                {
                //                    // Looks like we have a valid proxy, so we return that out
                //                    if (BeforeServiceOperationCall != null || AfterServiceOperationCall != null)
                //                        proxy2 = TransparentProxyGenerator.GetProxy<TServiceType>(new ServiceProxyEventWrapper(proxy2, typeof(TServiceType)), false);
                //                    return proxy2;
                //                }
                //            }
                //        }
                //        catch
                //        {
                //            // Not much we can do other than consider the cached proxy invalid and continue on creating a new one.
                //            RemoveCachedChannelFactory<TServiceType>(binding, endpoint);
                //        }
                //    }
                //}

                // Don't have a cached factory, so we have to create a new one
                var factory = new ChannelFactory<TServiceType>(binding, endpoint);
                if (factory.State == CommunicationState.Faulted) throw new Core.Exceptions.NullReferenceException("Error creating service client.");

                // Increasing the max items in object graph (number of objects that can be handled in a hierarchy) to int.MaxValue (same as the Silverlight default)
                if (factory.Endpoint != null && factory.Endpoint.Contract != null)
                    foreach (var operation in factory.Endpoint.Contract.Operations)
                    {
                        var dataContractBehavior = operation.Behaviors.Find<DataContractSerializerOperationBehavior>();
                        if (dataContractBehavior != null)
                            dataContractBehavior.MaxItemsInObjectGraph = int.MaxValue;
                    }

                var proxy = factory.CreateChannel();
                if (proxy == null) throw new Core.Exceptions.NullReferenceException("Requested service unavailable.");

                // If we need to raise events when calls happen, we need to create another wrapper proxy
                if (BeforeServiceOperationCall != null || AfterServiceOperationCall != null)
                    proxy = TransparentProxyGenerator.GetProxy<TServiceType>(new ServiceProxyEventWrapper(proxy, typeof (TServiceType)), false);

                //// Since everything seems to work, we are adding the factory to the cached items
                //if (!CachedChannelFactories.ContainsKey(bindingName))
                //    CachedChannelFactories.Add(bindingName, new Dictionary<EndpointAddress, Dictionary<Type, ChannelFactory>>());
                //if (!CachedChannelFactories[bindingName].ContainsKey(endpoint))
                //    CachedChannelFactories[bindingName].Add(endpoint, new Dictionary<Type, ChannelFactory>());
                //if (!CachedChannelFactories[bindingName][endpoint].ContainsKey(typeof(TServiceType)))
                //    CachedChannelFactories[bindingName][endpoint].Add(typeof(TServiceType), factory);

                return proxy;
            //}
        }

        /// <summary>
        /// This event fires whenever a service operation/method is called from within ServiceClient.Call()
        /// </summary>
        public static event EventHandler<BeforeServiceOperationCallEventArgs> BeforeServiceOperationCall;

        /// <summary>
        /// Raises the before service operation call event.
        /// </summary>
        /// <param name="args">The <see cref="BeforeServiceOperationCallEventArgs"/> instance containing the event data.</param>
        public static void RaiseBeforeServiceOperationCall(BeforeServiceOperationCallEventArgs args)
        {
            var handler = BeforeServiceOperationCall;
            if (handler == null) return;
            handler(null, args);
        }

        /// <summary>
        /// This event fires whenever a service operation/method is called from within ServiceClient.Call()
        /// </summary>
        public static event EventHandler<AfterServiceOperationCallEventArgs> AfterServiceOperationCall;

        /// <summary>
        /// Raises the after service operation call event.
        /// </summary>
        /// <param name="args">The <see cref="BeforeServiceOperationCallEventArgs"/> instance containing the event data.</param>
        public static void RaiseAfterServiceOperationCall(AfterServiceOperationCallEventArgs args)
        {
            var handler = AfterServiceOperationCall;
            if (handler == null) return;
            handler(null, args);
        }

        /// <summary>
        /// Removes the cached channel factory.
        /// </summary>
        /// <typeparam name="TServiceType">The type of the T service type.</typeparam>
        /// <param name="binding">The binding.</param>
        /// <param name="endpoint">The endpoint.</param>
        private static void RemoveCachedChannelFactory<TServiceType>(Binding binding, EndpointAddress endpoint) where TServiceType : class
        {
            lock (CachedChannelFactories)
                try
                {
                    var bindingName = binding.GetType().ToString();
                    if (CachedChannelFactories.ContainsKey(bindingName) && CachedChannelFactories[bindingName].ContainsKey(endpoint) && CachedChannelFactories[bindingName][endpoint].ContainsKey(typeof(TServiceType)))
                    {
                        CachedChannelFactories[bindingName][endpoint].Remove(typeof(TServiceType));
                        if (CachedChannelFactories[bindingName][endpoint].Count == 0)
                        {
                            CachedChannelFactories[bindingName].Remove(endpoint);
                            if (CachedChannelFactories[bindingName].Count == 0)
                                CachedChannelFactories.Remove(bindingName);
                        }
                    }
                }
                catch (Exception)
                {
                    try
                    {
                        CachedChannelFactories.Clear(); // Desperate attempt to clear everything out and start over
                    }
                    catch (Exception)
                    {
                        // Nothing we can do, really
                    }
                }
                finally
                {
                    Monitor.Exit(CachedChannelFactories);
                }
        }

        /// <summary>
        /// Internal list of cached channel factories
        /// </summary>
        private static readonly Dictionary<string, Dictionary<EndpointAddress, Dictionary<Type, ChannelFactory>>> CachedChannelFactories = new Dictionary<string, Dictionary<EndpointAddress, Dictionary<Type, ChannelFactory>>>();

        /// <summary>
        /// Gets the internal net TCP channel.
        /// </summary>
        /// <typeparam name="TServiceType">The type of the service type.</typeparam>
        /// <param name="messageSize">Size of the message.</param>
        /// <param name="serviceUri">The service URI.</param>
        /// <param name="useCachedChannel">If true, the systme will try to use a cached channel, rather than creating a new one.</param>
        /// <returns>TCP/IP channel</returns>
        private static TServiceType GetInternalNetTcpChannel<TServiceType>(MessageSize messageSize, Uri serviceUri, bool useCachedChannel) where TServiceType : class
        {
            if (messageSize == MessageSize.Undefined) messageSize = GetMessageSize<TServiceType>();
            var serviceId = GetServiceIdFromUri(serviceUri);
            var baseAddress = GetBaseAddressFromUri(serviceUri);
            var basePath = GetBasePathFromUri(serviceUri);
            if (useCachedChannel)
            {
                var cachedChannelIndex = GetChannelCacheIndex(typeof(TServiceType), messageSize, serviceUri.Port, serviceId, baseAddress, basePath);
                if (cachedChannelIndex > -1) return GetCachedChannel<TServiceType>(cachedChannelIndex);
            }

            var binding = GetStandardNetTcpBinding(serviceId);
            ServiceHelper.ConfigureMessageSizeOnNetTcpBinding(messageSize, binding);

            var endpoint = new EndpointAddress(serviceUri.AbsoluteUri);

            TServiceType proxy = null;
            try
            {
                proxy = CreateChannel<TServiceType>(binding, endpoint);

                var channel = GetChannelFromProxy(proxy);
                if (channel != null)
                {
                    if (BeforeChannelOpens != null)
                        BeforeChannelOpens(null, new BeforeChannelOpensEventArgs
                        {
                            Channel = channel,
                            BaseAddress = baseAddress,
                            BasePath = basePath,
                            ChannelType = typeof(TServiceType),
                            MessageSize = messageSize,
                            Port = serviceUri.Port,
                            ServiceId = serviceId
                        });

                    OpenChannel(channel);
                }

                if (useCachedChannel)
                    lock (OpenChannels)
                        OpenChannels.Add(new OpenChannelInformation { Port = serviceUri.Port, ServiceId = serviceId, MessageSize = messageSize, BaseUrl = baseAddress, BasePath = basePath, Channel = proxy, ChannelType = typeof(TServiceType) });

                return proxy;
            }
            catch (Exception ex)
            {
                if (ChannelException != null)
                    ChannelException(null, new ChannelExceptionEventArgs(GetChannelFromProxy(proxy), ex));

                if (!HandleChannelExceptions) throw;

                return null;
            }
        }

        /// <summary>
        /// Dynamically generates a Channel Factory for the provided type, binding, and endpoint 
        /// </summary>
        /// <param name="contract">Contract (type)</param>
        /// <param name="binding">Binding</param>
        /// <param name="address">Endpoint Address</param>
        /// <returns>Channel factory ready to be opened</returns>
        private static ChannelFactory GetChannelFactory(Type contract, Binding binding, EndpointAddress address)
        {
            // TODO: This does not yet support inherited interfaces on the contract
            var factoryType = typeof(ChannelFactory<>);
            var genericFactoryType = factoryType.MakeGenericType(new[] { contract });
            var factory = Activator.CreateInstance(genericFactoryType, binding, address);
            return (ChannelFactory)factory;
        }

        /// <summary>
        /// Uses an abstract channel factory type and invokes the parameter-less CreateChannel() method
        /// </summary>
        /// <param name="factory">Factory</param>
        /// <returns>Channel</returns>
        private static object CreateChannel(ChannelFactory factory)
        {
            try
            {
                dynamic factory2 = factory;
                return factory2.CreateChannel();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the internal net TCP channel.
        /// </summary>
        /// <param name="serviceType">Service type (contract)</param>
        /// <param name="messageSize">Size of the message.</param>
        /// <param name="serviceUri">The service URI.</param>
        /// <param name="useCachedChannel">If true, the system will use a channel that was cached rather than creating a new one (if possible)</param>
        /// <returns>TCP/IP channel</returns>
        private static object GetInternalNetTcpChannel(Type serviceType, MessageSize messageSize, Uri serviceUri, bool useCachedChannel)
        {
            if (messageSize == MessageSize.Undefined) messageSize = GetMessageSize(serviceType.Name);
            var serviceId = GetServiceIdFromUri(serviceUri);
            var baseAddress = GetBaseAddressFromUri(serviceUri);
            var basePath = GetBasePathFromUri(serviceUri);
            if (useCachedChannel)
            {
                var cachedChannelIndex = GetChannelCacheIndex(serviceType, messageSize, serviceUri.Port, serviceId, baseAddress, basePath);
                if (cachedChannelIndex > -1) return GetCachedChannel(serviceType, cachedChannelIndex);
            }

            var binding = GetStandardNetTcpBinding(serviceId);
            ServiceHelper.ConfigureMessageSizeOnNetTcpBinding(messageSize, binding);

            var endpoint = new EndpointAddress(serviceUri.AbsoluteUri);

            var factory = GetChannelFactory(serviceType, binding, endpoint);
            if (factory.State == CommunicationState.Faulted) throw new Core.Exceptions.NullReferenceException("Error creating service client.");

            object proxy = null;
            try
            {
                proxy = CreateChannel(factory);
                if (proxy == null) throw new Core.Exceptions.NullReferenceException("Requested service unavailable.");

                var channel = GetChannelFromProxy(proxy);
                if (channel != null)
                {
                    if (BeforeChannelOpens != null)
                        BeforeChannelOpens(null, new BeforeChannelOpensEventArgs
                        {
                            Channel = channel,
                            BaseAddress = baseAddress,
                            BasePath = basePath,
                            ChannelType = serviceType,
                            MessageSize = messageSize,
                            Port = serviceUri.Port,
                            ServiceId = serviceId
                        });

                    OpenChannel(channel);
                }

                if (useCachedChannel)
                    lock (OpenChannels)
                        OpenChannels.Add(new OpenChannelInformation { Port = serviceUri.Port, ServiceId = serviceId, MessageSize = messageSize, BaseUrl = baseAddress, BasePath = basePath, Channel = proxy, ChannelType = serviceType });

                return proxy;
            }
            catch (Exception ex)
            {
                if (ChannelException != null)
                    ChannelException(null, new ChannelExceptionEventArgs(GetChannelFromProxy(proxy), ex));

                if (!HandleChannelExceptions) throw;

                return null;
            }
        }

        /// <summary>
        /// Creates a basic HTTP channel to the desired service type
        /// </summary>
        /// <typeparam name="TServiceType">The type of the service type.</typeparam>
        /// <param name="serviceId">The service id.</param>
        /// <param name="messageSize">Size of the message.</param>
        /// <param name="useCachedChannel">If true, the system will use a channel that was cached rather than creating a new one (if possible)</param>
        /// <returns>TCP/IP channel</returns>
        private static TServiceType GetInternalBasicHttpChannel<TServiceType>(string serviceId, MessageSize messageSize, bool useCachedChannel) where TServiceType : class
        {
            return GetInternalBasicHttpChannel<TServiceType>(serviceId, messageSize, null, null, null, useCachedChannel, false);
        }

        /// <summary>
        /// Creates a basic HTTP channel to the desired service type
        /// </summary>
        /// <typeparam name="TServiceType">The type of the service type.</typeparam>
        /// <param name="serviceId">The service id.</param>
        /// <param name="messageSize">Size of the message.</param>
        /// <param name="baseAddress">Base service address (such as www.domain.com)</param>
        /// <param name="basePath">Base path (such as "MyService" to create www.domain.com/MyService/basic)</param>
        /// <param name="useCachedChannel">If true, the system will use a channel that was cached rather than creating a new one (if possible)</param>
        /// <returns>TCP/IP channel</returns>
        private static TServiceType GetInternalBasicHttpChannel<TServiceType>(string serviceId, MessageSize messageSize, string baseAddress, string basePath, bool useCachedChannel) where TServiceType : class
        {
            return GetInternalBasicHttpChannel<TServiceType>(serviceId, messageSize, baseAddress, basePath, null, useCachedChannel, false);
        }

        /// <summary>
        /// Creates a basic HTTP channel to the desired service type
        /// </summary>
        /// <typeparam name="TServiceType">The type of the service type.</typeparam>
        /// <param name="serviceId">The service id.</param>
        /// <param name="messageSize">Size of the message.</param>
        /// <param name="baseAddress">Base service address (such as www.domain.com)</param>
        /// <param name="basePath">Base path (such as "MyService" to create www.domain.com/MyService/basic)</param>
        /// <param name="extension">Path extension for basic HTTP services (such as "basic" to create www.domain.com/MyService/basic)</param>
        /// <param name="useCachedChannel">If true, the system will use a channel that was cached rather than creating a new one (if possible)</param>
        /// <param name="useHttps">Indicates whether HTTPS should be used</param>
        /// <returns>TCP/IP channel</returns>
        /// <exception cref="Core.Exceptions.NullReferenceException">Static BaseUrl property must be set on the ServiceClient class.</exception>
        private static TServiceType GetInternalBasicHttpChannel<TServiceType>(string serviceId, MessageSize messageSize, string baseAddress, string basePath, string extension, bool useCachedChannel, bool useHttps) where TServiceType : class
        {
            var interfaceName = typeof(TServiceType).Name;
            var serviceFullAddress = GetSetting("ServiceUrl:" + interfaceName);

            // Checking parameter values
            if (serviceId == null) serviceId = GetServiceId<TServiceType>();
            if (messageSize == MessageSize.Undefined) messageSize = GetMessageSize<TServiceType>();
            if (baseAddress == null)
            {
                baseAddress = GetSetting("ServiceBaseUrl:" + interfaceName, defaultValue: BaseUrl);
                if (string.IsNullOrEmpty(baseAddress) && string.IsNullOrEmpty(serviceFullAddress))
                    throw new Core.Exceptions.NullReferenceException("Static BaseUrl property must be set on the ServiceClient class.");
            }
            if (basePath == null) basePath = GetSetting("ServiceBasePath:" + interfaceName, defaultValue: BasePath);
            if (extension == null)
            {
                extension = GetSetting("ServiceBasicHTTPExtension:" + interfaceName);
                if (string.IsNullOrEmpty(extension))
                    extension = GetSetting("ServiceBasicHTTPExtension", defaultValue: "basic");
            }

            if (!string.IsNullOrEmpty(basePath) && !basePath.EndsWith("/")) basePath += "/";
            if (!string.IsNullOrEmpty(extension) && !extension.StartsWith("/")) extension = "/" + extension;
            var protocol = useHttps ? "https://" : "http://";

            if (string.IsNullOrEmpty(serviceFullAddress))
            {
                if (useCachedChannel)
                {
                    var cachedChannelIndex = GetChannelCacheIndex(typeof(TServiceType), messageSize, 80, serviceId, baseAddress, basePath);
                    if (cachedChannelIndex > -1) return GetCachedChannel<TServiceType>(cachedChannelIndex);
                }
                serviceFullAddress = protocol + baseAddress + "/" + basePath + serviceId + extension;
            }
            else
            {
                if (useHttps && serviceFullAddress.ToLower().StartsWith("http://"))
                    serviceFullAddress = "https://" + serviceFullAddress.Substring(7);
                if (!useHttps && serviceFullAddress.ToLower().StartsWith("https://"))
                    serviceFullAddress = "http://" + serviceFullAddress.Substring(8);
            }

            // Ready to start processing
            var securityMode = useHttps ? BasicHttpSecurityMode.Transport : BasicHttpSecurityMode.None;
            var binding = new BasicHttpBinding(securityMode) { SendTimeout = new TimeSpan(0, 10, 0) };
            ServiceHelper.ConfigureMessageSizeOnBasicHttpBinding(messageSize, binding);

            // We allow for an override of the URL
            serviceFullAddress = GetSetting("ServiceFullUrl:" + interfaceName, defaultValue: serviceFullAddress);

            // We allow fiddling of the endpoint address by means of an event
            var beforeEndpointAdded = BeforeEndpointAdded;
            if (beforeEndpointAdded != null)
            {
                var endpointEventArgs = new EndpointAddedEventArgs
                {
                    ServiceFullAddress = serviceFullAddress,
                    Binding = binding,
                    ContractType = typeof(TServiceType),
                    ServiceId = serviceId
                };
                beforeEndpointAdded(null, endpointEventArgs);
                serviceFullAddress = endpointEventArgs.ServiceFullAddress;
            }

            // Finally, we create the new endpoint
            var endpoint = new EndpointAddress(serviceFullAddress);

            TServiceType proxy = null;
            try
            {
                proxy = CreateChannel<TServiceType>(binding, endpoint);

                var channel = GetChannelFromProxy(proxy);
                if (channel != null)
                {
                    if (BeforeChannelOpens != null)
                        BeforeChannelOpens(null, new BeforeChannelOpensEventArgs
                        {
                            Channel = channel,
                            BaseAddress = baseAddress,
                            BasePath = basePath,
                            ChannelType = typeof(TServiceType),
                            MessageSize = messageSize,
                            Port = 80,
                            ServiceId = serviceId
                        });

                    OpenChannel(channel);
                }

                if (useCachedChannel)
                    lock (OpenChannels)
                        OpenChannels.Add(new OpenChannelInformation { Port = 80, ServiceId = serviceId, MessageSize = messageSize, BaseUrl = baseAddress, BasePath = basePath, Channel = proxy, ChannelType = typeof(TServiceType) });

                return proxy;
            }
            catch (Exception ex)
            {
                if (ChannelException != null)
                    ChannelException(null, new ChannelExceptionEventArgs(GetChannelFromProxy(proxy), ex));

                if (!HandleChannelExceptions) throw;

                return null;
            }
        }

        /// <summary>
        /// Creates a basic HTTP channel to the desired service type
        /// </summary>
        /// <typeparam name="TServiceType">The type of the service type.</typeparam>
        /// <param name="messageSize">Size of the message.</param>
        /// <param name="serviceUri">The service URI.</param>
        /// <param name="useCachedChannel">If true, the system will use a channel that was cached rather than creating a new one (if possible)</param>
        /// <returns>TCP/IP channel</returns>
        private static TServiceType GetInternalBasicHttpChannel<TServiceType>(MessageSize messageSize, Uri serviceUri, bool useCachedChannel) where TServiceType : class
        {
            if (messageSize == MessageSize.Undefined) messageSize = GetMessageSize<TServiceType>();
            var serviceId = GetServiceIdFromUri(serviceUri);
            var baseAddress = GetBaseAddressFromUri(serviceUri);
            var basePath = GetBasePathFromUri(serviceUri);
            if (useCachedChannel)
            {
                int cachedChannelIndex = GetChannelCacheIndex(typeof(TServiceType), messageSize, 80, serviceId, baseAddress, basePath);
                if (cachedChannelIndex > -1) return GetCachedChannel<TServiceType>(cachedChannelIndex);
            }

            var securityMode = serviceUri.AbsoluteUri.ToLower().StartsWith("https://") ? BasicHttpSecurityMode.Transport : BasicHttpSecurityMode.None;
            var binding = new BasicHttpBinding(securityMode) { SendTimeout = new TimeSpan(0, 10, 0) };
            ServiceHelper.ConfigureMessageSizeOnBasicHttpBinding(messageSize, binding);

            var endpoint = new EndpointAddress(serviceUri.AbsoluteUri);

            TServiceType proxy = null;
            try
            {
                proxy = CreateChannel<TServiceType>(binding, endpoint);

                var channel = GetChannelFromProxy(proxy);
                if (channel != null)
                {
                    if (BeforeChannelOpens != null)
                        BeforeChannelOpens(null, new BeforeChannelOpensEventArgs
                        {
                            Channel = channel,
                            BaseAddress = baseAddress,
                            BasePath = basePath,
                            ChannelType = typeof(TServiceType),
                            MessageSize = messageSize,
                            Port = 80,
                            ServiceId = serviceId
                        });

                    OpenChannel(channel);
                }

                if (useCachedChannel)
                    lock (OpenChannels)
                        OpenChannels.Add(new OpenChannelInformation { Port = 80, ServiceId = serviceId, MessageSize = messageSize, BaseUrl = baseAddress, BasePath = basePath, Channel = proxy, ChannelType = typeof(TServiceType) });

                return proxy;
            }
            catch (Exception ex)
            {
                if (ChannelException != null)
                    ChannelException(null, new ChannelExceptionEventArgs(GetChannelFromProxy(proxy), ex));

                if (!HandleChannelExceptions) throw;

                return null;
            }
        }

        /// <summary>
        /// Creates a basic HTTP channel to the desired service type
        /// </summary>
        /// <param name="serviceType">The type of the service type.</param>
        /// <param name="messageSize">Size of the message.</param>
        /// <param name="serviceUri">The service URI.</param>
        /// <param name="useCachedChannel">If true, the system will use a channel that was cached rather than creating a new one (if possible)</param>
        /// <returns>TCP/IP channel</returns>
        private static object GetInternalBasicHttpChannel(Type serviceType, MessageSize messageSize, Uri serviceUri, bool useCachedChannel)
        {
            if (messageSize == MessageSize.Undefined) messageSize = GetMessageSize(serviceType.Name);
            var serviceId = GetServiceIdFromUri(serviceUri);
            var baseAddress = GetBaseAddressFromUri(serviceUri);
            var basePath = GetBasePathFromUri(serviceUri);
            if (useCachedChannel)
            {
                var cachedChannelIndex = GetChannelCacheIndex(serviceType, messageSize, 80, serviceId, baseAddress, basePath);
                if (cachedChannelIndex > -1) return GetCachedChannel(serviceType, cachedChannelIndex);
            }

            var securityMode = serviceUri.AbsoluteUri.ToLower().StartsWith("https://") ? BasicHttpSecurityMode.Transport : BasicHttpSecurityMode.None;
            var binding = new BasicHttpBinding(securityMode) { SendTimeout = new TimeSpan(0, 10, 0) };
            ServiceHelper.ConfigureMessageSizeOnBasicHttpBinding(messageSize, binding);

            var endpoint = new EndpointAddress(serviceUri.AbsoluteUri);

            var factory = GetChannelFactory(serviceType, binding, endpoint);
            if (factory.State == CommunicationState.Faulted) throw new Core.Exceptions.NullReferenceException("Error creating service client.");
            object proxy = null;
            try
            {
                proxy = CreateChannel(factory);
                if (proxy == null) throw new Core.Exceptions.NullReferenceException("Requested service unavailable.");

                var channel = GetChannelFromProxy(proxy);
                if (channel != null)
                {
                    if (BeforeChannelOpens != null)
                        BeforeChannelOpens(null, new BeforeChannelOpensEventArgs
                        {
                            Channel = channel,
                            BaseAddress = baseAddress,
                            BasePath = basePath,
                            ChannelType = serviceType,
                            MessageSize = messageSize,
                            Port = 80,
                            ServiceId = serviceId
                        });

                    OpenChannel(channel);
                }

                if (useCachedChannel)
                    lock (OpenChannels)
                        OpenChannels.Add(new OpenChannelInformation { Port = 80, ServiceId = serviceId, MessageSize = messageSize, BaseUrl = baseAddress, BasePath = basePath, Channel = proxy, ChannelType = serviceType });

                return proxy;
            }
            catch (Exception ex)
            {
                var channelExceptionHandler = ChannelException;
                if (channelExceptionHandler != null)
                    channelExceptionHandler(null, new ChannelExceptionEventArgs(GetChannelFromProxy(proxy), ex));

                if (!HandleChannelExceptions) throw;

                return null;
            }
        }

        /// <summary>
        /// Gets the state of a WCF channel (interface)
        /// </summary>
        /// <param name="channel">Service channel/interface</param>
        /// <returns>Current channel state</returns>
        public static ChannelState GetChannelState(object channel)
        {
            var channel2 = GetChannelFromProxy(channel);
            if (channel2 != null) return (ChannelState)(int)channel2.State;
            return ChannelState.Unknown;
        }

        /// <summary>
        /// Aborts the channel.
        /// </summary>
        /// <param name="channel">The channel.</param>
        /// <param name="exceptionCausingAbort">The exception that lead to causing this abort operation.</param>
        private static void AbortChannel(object channel, Exception exceptionCausingAbort = null)
        {
            var client = GetChannelFromProxy(channel);
            if (client != null)
            {
                var channelExceptionHandler = ChannelException;
                if (channelExceptionHandler != null)
                    channelExceptionHandler(null, new ChannelExceptionEventArgs(client, exceptionCausingAbort));

                for (var channelIndex = 0; channelIndex < OpenChannels.Count; channelIndex++)
                {
                    var openChannel = OpenChannels[channelIndex];
                    if (openChannel.Channel == client)
                    {
                        lock (OpenChannels)
                            OpenChannels.RemoveAt(channelIndex);
                        break;
                    }
                }

                client.Abort();
                client.Dispose();   
            }
            else
            {
                var channelExceptionHandler = ChannelException;
                if (channelExceptionHandler != null)
                    channelExceptionHandler(null, new ChannelExceptionEventArgs(null, exceptionCausingAbort));
            }

            // Not doing this anymore, since if kills further handling on the caller
            // if (!HandleChannelExceptions && ex != null) throw ex;
        }

        private static IClientChannel GetChannelFromProxy(object channel)
        {
            var client = channel as IClientChannel;
            if (client != null) return client;

            var handlerField = channel.GetType().GetField("handler", BindingFlags.NonPublic | BindingFlags.Instance);
            if (handlerField != null)
            {
                var handler = handlerField.GetValue(channel) as ServiceProxyEventWrapper;
                if (handler != null)
                    return GetChannelFromProxy(handler.OriginalProxy);
            }

            return null;
        }

        /// <summary>
        /// Explicitly closes a WCF channel
        /// </summary>
        /// <param name="channel">Service (channel/interface) to close</param>
        /// <param name="channelMayBeShared">If the channel may be a shared channel, some extra steps have to be taken. If one is sure the channel is dedicated, false may be passed for better performance.</param>
        /// <returns>True if channel closed successfully</returns>
        public static bool CloseChannel(object channel, bool channelMayBeShared = true)
        {
            var channel2 = GetChannelFromProxy(channel);
            if (channel2 == null) return false;
            try
            {
                if (channelMayBeShared)
                    for (var channelIndex = 0; channelIndex < OpenChannels.Count; channelIndex++)
                    {
                        var openChannel = OpenChannels[channelIndex];
                        if (openChannel.Channel == channel2)
                        {
                            lock (OpenChannels)
                                OpenChannels.RemoveAt(channelIndex);
                            break;
                        }
                    }

                if (channel2.State == CommunicationState.Opened)
                {
                    channel2.Close();
                    channel2.Dispose();
                }
                return true;
            }
            catch (Exception ex)
            {
                if (ChannelException != null)
                    ChannelException(null, new ChannelExceptionEventArgs(channel2, ex));

                if (!HandleChannelExceptions) throw;

                return false;
            }
        }

        /// <summary>
        /// Inspects a channel to make sure it is usable (open). If the channel is not usable,
        /// this method attempts to create a new channel to the same service.
        /// </summary>
        /// <typeparam name="TChannel">Channel (service interface)</typeparam>
        /// <param name="channel">Channel</param>
        /// <returns>Valid channel if possible</returns>
        public static TChannel VerifyChannelIsValid<TChannel>(TChannel channel) where TChannel : class
        {
            if (channel != null)
            {
                var channel2 = GetChannelFromProxy(channel);
                if (channel2 != null)
                {
                    if (channel2.State != CommunicationState.Opened)
                        try
                        {
                            channel = GetChannel<TChannel>(); // TODO: At times this becomes cyclic, it seems
                        }
                        catch { } // Too bad, but such is life
                }
                else
                    channel = GetChannel<TChannel>();
            }
            else
                channel = GetChannel<TChannel>();

            return channel;
        }

        //// port, serviceId, baseAddress, basePath

        /// <summary>
        /// Returns the base address the specified URI is using
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        private static string GetBaseAddressFromUri(Uri uri)
        {
            var url = ExtractRawUrlFromUri(uri);
            var domainParts = url.Split('/')[0].Split(':');
            return domainParts.Length > 0 ? domainParts[0] : string.Empty;
        }

        /// <summary>
        /// Returns the base path the specified URI is using
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        private static string GetBasePathFromUri(Uri uri)
        {
            var url = ExtractRawUrlFromUri(uri);
            var urlParts = url.Split('/');
            return urlParts.Length > 1 ? urlParts[1] : string.Empty;
        }

        /// <summary>
        /// Returns the service ID the specified URI is using
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        private static string GetServiceIdFromUri(Uri uri)
        {
            var url = ExtractRawUrlFromUri(uri);
            var urlParts = url.Split('/');
            return urlParts.Length > 2 ? urlParts[2] : string.Empty;
        }

        /// <summary>
        /// Returns the URL as a string without the protocol prefix
        /// </summary>
        /// <param name="uri">URI to parse</param>
        private static string ExtractRawUrlFromUri(Uri uri)
        {
            var url = uri.AbsoluteUri;
            if (url.ToLower().StartsWith("http://")) url = url.Substring(7);
            if (url.ToLower().StartsWith("https://")) url = url.Substring(8);
            if (url.ToLower().StartsWith("net.tcp://")) url = url.Substring(10);
            return url;
        }

        /// <summary>
        /// Gets the channel using WS HTTP protocol.
        /// </summary>
        /// <typeparam name="TServiceType">The type of the service.</typeparam>
        /// <param name="serviceId">The service id.</param>
        /// <param name="messageSize">Size of the message.</param>
        /// <param name="baseAddress">Base service address (such as www.domain.com)</param>
        /// <param name="basePath">Base path (such as "MyService" to create www.domain.com/MyService/basic)</param>
        /// <param name="extension">Path extension for basic HTTP services (such as "basic" to create www.domain.com/MyService/basic)</param>
        /// <param name="useCachedChannel">If true, the system is free to use a cached channel</param>
        /// <param name="useHttps">Indicates whether HTTPS should be used</param>
        /// <returns>Service</returns>
        public static TServiceType GetWsHttpChannel<TServiceType>(string serviceId = null, MessageSize messageSize = MessageSize.Undefined, string baseAddress = null, string basePath = null, string extension = null, bool useCachedChannel = true, bool useHttps = false) where TServiceType : class
        {
            if (serviceId == null) serviceId = GetServiceId<TServiceType>();
            var interfaceName = typeof(TServiceType).Name;
            if (messageSize == MessageSize.Undefined) messageSize = GetMessageSize(serviceId);
            if (baseAddress == null)
            {
                baseAddress = GetSetting("ServiceBaseUrl:" + interfaceName, defaultValue: BaseUrl);
                if (string.IsNullOrEmpty(baseAddress))
                    throw new Core.Exceptions.NullReferenceException("Static BaseUrl property must be set on the ServiceClient class.");
            }
            if (basePath == null) basePath = GetSetting("ServiceBasePath:" + interfaceName, defaultValue: BasePath);
            if (extension == null)
            {
                extension = GetSetting("ServiceWsHTTPExtension:" + interfaceName);
                if (string.IsNullOrEmpty(extension))
                    extension = GetSetting("ServiceWsHTTPExtension", defaultValue: "ws");
            }

            return GetInternalWsHttpChannel<TServiceType>(serviceId, messageSize, baseAddress, basePath, extension, useCachedChannel, useHttps);
        }

        /// <summary>
        /// Gets a dedicated channel using WS HTTP protocol.
        /// </summary>
        /// <typeparam name="TServiceType">The type of the service.</typeparam>
        /// <param name="serviceId">The service id.</param>
        /// <param name="messageSize">Size of the message.</param>
        /// <param name="baseAddress">Base service address (such as www.domain.com)</param>
        /// <param name="basePath">Base path (such as "MyService" to create www.domain.com/MyService/basic)</param>
        /// <param name="extension">Path extension for basic HTTP services (such as "basic" to create www.domain.com/MyService/basic)</param>
        /// <param name="useHttps">Indicates whether HTTPS should be used</param>
        /// <returns>Service</returns>
        /// <remarks>Creates a channel exclusive to this caller. It is up to the caller to close the channel after use!</remarks>
        public static TServiceType GetWsHttpChannelDedicated<TServiceType>(string serviceId = null, MessageSize messageSize = MessageSize.Undefined, string baseAddress = null, string basePath = null, string extension = null, bool useHttps = false) where TServiceType : class
        {
            if (serviceId == null) serviceId = GetServiceId<TServiceType>();
            var interfaceName = typeof(TServiceType).Name;
            if (messageSize == MessageSize.Undefined) messageSize = GetMessageSize(serviceId);
            if (baseAddress == null)
            {
                baseAddress = GetSetting("ServiceBaseUrl:" + interfaceName, defaultValue: BaseUrl);
                if (string.IsNullOrEmpty(baseAddress))
                    throw new Core.Exceptions.NullReferenceException("Static BaseUrl property must be set on the ServiceClient class.");
            }
            if (basePath == null) basePath = GetSetting("ServiceBasePath:" + interfaceName, defaultValue: BasePath);
            if (extension == null)
            {
                extension = GetSetting("ServiceWsHTTPExtension:" + interfaceName);
                if (string.IsNullOrEmpty(extension))
                    extension = GetSetting("ServiceWsHTTPExtension", defaultValue: "ws");
            }

            return GetInternalWsHttpChannel<TServiceType>(serviceId, messageSize, baseAddress, basePath, extension, false, useHttps);
        }

        /// <summary>
        /// Gets the channel using WS HTTP protocol.
        /// </summary>
        /// <typeparam name="TServiceType">The type of the service.</typeparam>
        /// <param name="serviceId">The service id.</param>
        /// <param name="baseAddress">Base service address (such as www.domain.com)</param>
        /// <param name="basePath">Base path (such as "MyService" to create www.domain.com/MyService/basic)</param>
        /// <returns>Service</returns>
        public static TServiceType GetWsHttpChannel<TServiceType>(string serviceId, string baseAddress, string basePath) where TServiceType : class
        {
            return GetInternalWsHttpChannel<TServiceType>(serviceId, MessageSize.Undefined, baseAddress, basePath, true);
        }

        /// <summary>
        /// Gets a dedicated channel using WS HTTP protocol.
        /// </summary>
        /// <typeparam name="TServiceType">The type of the service.</typeparam>
        /// <param name="serviceId">The service id.</param>
        /// <param name="baseAddress">Base service address (such as www.domain.com)</param>
        /// <param name="basePath">Base path (such as "MyService" to create www.domain.com/MyService/basic)</param>
        /// <returns>Service</returns>
        /// <remarks>Creates a channel exclusive to this caller. It is up to the caller to close the channel after use!</remarks>
        public static TServiceType GetWsHttpChannelDedicated<TServiceType>(string serviceId, string baseAddress, string basePath) where TServiceType : class
        {
            return GetInternalWsHttpChannel<TServiceType>(serviceId, GetMessageSize(serviceId), baseAddress, basePath, false);
        }

        /// <summary>
        /// Gets the channel using WS HTTP protocol.
        /// </summary>
        /// <typeparam name="TServiceType">The type of the service.</typeparam>
        /// <param name="size">Message size</param>
        /// <param name="baseAddress">Base service address (such as www.domain.com)</param>
        /// <param name="basePath">Base path (such as "MyService" to create www.domain.com/MyService/basic)</param>
        /// <returns>Service</returns>
        public static TServiceType GetWsHttpChannel<TServiceType>(MessageSize size, string baseAddress, string basePath) where TServiceType : class
        {
            return GetInternalWsHttpChannel<TServiceType>(null, size, baseAddress, basePath, true);
        }

        /// <summary>
        /// Gets a dedicated channel using WS HTTP protocol.
        /// </summary>
        /// <typeparam name="TServiceType">The type of the service.</typeparam>
        /// <param name="size">Message size</param>
        /// <param name="baseAddress">Base service address (such as www.domain.com)</param>
        /// <param name="basePath">Base path (such as "MyService" to create www.domain.com/MyService/basic)</param>
        /// <returns>Service</returns>
        /// <remarks>Creates a channel exclusive to this caller. It is up to the caller to close the channel after use!</remarks>
        public static TServiceType GetWsHttpChannelDedicated<TServiceType>(MessageSize size, string baseAddress, string basePath) where TServiceType : class
        {
            return GetInternalWsHttpChannel<TServiceType>(null, size, baseAddress, basePath, false);
        }

        /// <summary>
        /// Gets the channel using WS HTTP protocol.
        /// </summary>
        /// <typeparam name="TServiceType">The type of the service.</typeparam>
        /// <param name="baseAddress">Base service address (such as www.domain.com)</param>
        /// <param name="basePath">Base path (such as "MyService" to create www.domain.com/MyService/basic)</param>
        /// <returns>Service</returns>
        public static TServiceType GetWsHttpChannel<TServiceType>(string baseAddress, string basePath) where TServiceType : class
        {
            return GetInternalWsHttpChannel<TServiceType>(null, MessageSize.Undefined, baseAddress, basePath, true);
        }

        /// <summary>
        /// Gets the channel using WS HTTP protocol.
        /// </summary>
        /// <typeparam name="TServiceType">The type of the service.</typeparam>
        /// <param name="baseAddress">Base service address (such as www.domain.com)</param>
        /// <param name="basePath">Base path (such as "MyService" to create www.domain.com/MyService/basic)</param>
        /// <returns>Service</returns>
        /// <remarks>Creates a channel exclusive to this caller. It is up to the caller to close the channel after use!</remarks>
        public static TServiceType GetWsHttpChannelDedicated<TServiceType>(string baseAddress, string basePath) where TServiceType : class
        {
            return GetInternalWsHttpChannel<TServiceType>(null, MessageSize.Undefined, baseAddress, basePath, false);
        }

        /// <summary>
        /// Creates a basic HTTP channel to the desired service type
        /// </summary>
        /// <typeparam name="TServiceType">The type of the service type.</typeparam>
        /// <param name="serviceId">The service id.</param>
        /// <param name="messageSize">Size of the message.</param>
        /// <param name="useCachedChannel">If true, the system will use a channel that was cached rather than creating a new one (if possible)</param>
        /// <returns>TCP/IP channel</returns>
        private static TServiceType GetInternalWsHttpChannel<TServiceType>(string serviceId, MessageSize messageSize, bool useCachedChannel) where TServiceType : class
        {
            return GetInternalWsHttpChannel<TServiceType>(serviceId, messageSize, null, null, null, useCachedChannel, false);
        }

        /// <summary>
        /// Creates a basic HTTP channel to the desired service type
        /// </summary>
        /// <typeparam name="TServiceType">The type of the service type.</typeparam>
        /// <param name="serviceId">The service id.</param>
        /// <param name="messageSize">Size of the message.</param>
        /// <param name="baseAddress">Base service address (such as www.domain.com)</param>
        /// <param name="basePath">Base path (such as "MyService" to create www.domain.com/MyService/basic)</param>
        /// <param name="useCachedChannel">If true, the system will use a channel that was cached rather than creating a new one (if possible)</param>
        /// <returns>TCP/IP channel</returns>
        private static TServiceType GetInternalWsHttpChannel<TServiceType>(string serviceId, MessageSize messageSize, string baseAddress, string basePath, bool useCachedChannel) where TServiceType : class
        {
            return GetInternalWsHttpChannel<TServiceType>(serviceId, messageSize, baseAddress, basePath, null, useCachedChannel, false);
        }

        /// <summary>
        /// Creates a basic HTTP channel to the desired service type
        /// </summary>
        /// <typeparam name="TServiceType">The type of the service type.</typeparam>
        /// <param name="serviceId">The service id.</param>
        /// <param name="messageSize">Size of the message.</param>
        /// <param name="baseAddress">Base service address (such as www.domain.com)</param>
        /// <param name="basePath">Base path (such as "MyService" to create www.domain.com/MyService/basic)</param>
        /// <param name="extension">Path extension for basic HTTP services (such as "basic" to create www.domain.com/MyService/basic)</param>
        /// <param name="useCachedChannel">If true, the system will use a channel that was cached rather than creating a new one (if possible)</param>
        /// <param name="useHttps">Indicates whether HTTPS should be used</param>
        /// <returns>TCP/IP channel</returns>
        private static TServiceType GetInternalWsHttpChannel<TServiceType>(string serviceId, MessageSize messageSize, string baseAddress, string basePath, string extension, bool useCachedChannel, bool useHttps) where TServiceType : class
        {
            var interfaceName = typeof(TServiceType).Name;
            var serviceFullAddress = GetSetting("ServiceUrl:" + interfaceName);

            // Checking parameter values
            if (serviceId == null) serviceId = GetServiceId<TServiceType>();
            if (messageSize == MessageSize.Undefined) messageSize = GetMessageSize<TServiceType>();
            if (baseAddress == null)
            {
                baseAddress = GetSetting("ServiceBaseUrl:" + interfaceName, defaultValue: BaseUrl);
                if (string.IsNullOrEmpty(baseAddress) && string.IsNullOrEmpty(serviceFullAddress))
                    throw new Core.Exceptions.NullReferenceException("Static BaseUrl property must be set on the ServiceClient class.");
            }
            if (basePath == null) basePath = GetSetting("ServiceBasePath:" + interfaceName, defaultValue: BasePath);
            if (extension == null)
            {
                extension = GetSetting("ServiceWsHTTPExtension:" + interfaceName);
                if (string.IsNullOrEmpty(extension))
                    extension = GetSetting("ServiceWsHTTPExtension", defaultValue: "ws");
            }

            if (!string.IsNullOrEmpty(basePath) && !basePath.EndsWith("/")) basePath += "/";
            if (!string.IsNullOrEmpty(extension) && !extension.StartsWith("/")) extension = "/" + extension;
            var protocol = useHttps ? "https://" : "http://";

            if (string.IsNullOrEmpty(serviceFullAddress))
            {
                if (useCachedChannel)
                {
                    var cachedChannelIndex = GetChannelCacheIndex(typeof(TServiceType), messageSize, 80, serviceId, baseAddress, basePath);
                    if (cachedChannelIndex > -1) return GetCachedChannel<TServiceType>(cachedChannelIndex);
                }
                serviceFullAddress = protocol + baseAddress + "/" + basePath + serviceId + extension;
            }
            else
            {
                if (useHttps && serviceFullAddress.ToLower().StartsWith("http://"))
                    serviceFullAddress = "https://" + serviceFullAddress.Substring(7);
                if (!useHttps && serviceFullAddress.ToLower().StartsWith("https://"))
                    serviceFullAddress = "http://" + serviceFullAddress.Substring(8);
            }

            // Ready to start processing
            var securityMode = useHttps ? SecurityMode.Transport : SecurityMode.None;
            var binding = new WSHttpBinding(securityMode) { SendTimeout = new TimeSpan(0, 10, 0) };
            ServiceHelper.ConfigureMessageSizeOnWsHttpBinding(messageSize, binding);

            // We allow fiddling of the endpoint address by means of an event
            var beforeEndpointAdded = BeforeEndpointAdded;
            if (beforeEndpointAdded != null)
            {
                var endpointEventArgs = new EndpointAddedEventArgs
                {
                    ServiceFullAddress = serviceFullAddress,
                    Binding = binding,
                    ContractType = typeof(TServiceType),
                    ServiceId = serviceId
                };
                beforeEndpointAdded(null, endpointEventArgs);
                serviceFullAddress = endpointEventArgs.ServiceFullAddress;
            }

            // Finally, we create the new endpoint
            var endpoint = new EndpointAddress(serviceFullAddress);

            TServiceType proxy = null;
            try
            {
                proxy = CreateChannel<TServiceType>(binding, endpoint);

                var channel = GetChannelFromProxy(proxy);
                if (channel != null)
                {
                    if (BeforeChannelOpens != null)
                        BeforeChannelOpens(null, new BeforeChannelOpensEventArgs
                        {
                            Channel = channel,
                            BaseAddress = baseAddress,
                            BasePath = basePath,
                            ChannelType = typeof(TServiceType),
                            MessageSize = messageSize,
                            Port = 80,
                            ServiceId = serviceId
                        });

                    OpenChannel(channel);
                }

                if (useCachedChannel)
                    lock (OpenChannels)
                        OpenChannels.Add(new OpenChannelInformation { Port = 80, ServiceId = serviceId, MessageSize = messageSize, BaseUrl = baseAddress, BasePath = basePath, Channel = proxy, ChannelType = typeof(TServiceType) });

                return proxy;
            }
            catch (Exception ex)
            {
                if (ChannelException != null)
                    ChannelException(null, new ChannelExceptionEventArgs(GetChannelFromProxy(proxy), ex));

                if (!HandleChannelExceptions) throw;

                return null;
            }
        }

        /// <summary>
        /// Creates a basic HTTP channel to the desired service type
        /// </summary>
        /// <typeparam name="TServiceType">The type of the service type.</typeparam>
        /// <param name="messageSize">Size of the message.</param>
        /// <param name="serviceUri">The service URI.</param>
        /// <param name="useCachedChannel">If true, the system will use a channel that was cached rather than creating a new one (if possible)</param>
        /// <returns>TCP/IP channel</returns>
        private static TServiceType GetInternalWsHttpChannel<TServiceType>(MessageSize messageSize, Uri serviceUri, bool useCachedChannel) where TServiceType : class
        {
            if (messageSize == MessageSize.Undefined) messageSize = GetMessageSize<TServiceType>();
            var serviceId = GetServiceIdFromUri(serviceUri);
            var baseAddress = GetBaseAddressFromUri(serviceUri);
            var basePath = GetBasePathFromUri(serviceUri);
            if (useCachedChannel)
            {
                var cachedChannelIndex = GetChannelCacheIndex(typeof(TServiceType), messageSize, 80, serviceId, baseAddress, basePath);
                if (cachedChannelIndex > -1) return GetCachedChannel<TServiceType>(cachedChannelIndex);
            }

            var securityMode = serviceUri.AbsoluteUri.ToLower().StartsWith("https://") ? SecurityMode.Transport : SecurityMode.None;
            var binding = new WSHttpBinding(securityMode) { SendTimeout = new TimeSpan(0, 10, 0) };
            ServiceHelper.ConfigureMessageSizeOnWsHttpBinding(messageSize, binding);

            var endpoint = new EndpointAddress(serviceUri.AbsoluteUri);

            TServiceType proxy = null;
            try
            {
                proxy = CreateChannel<TServiceType>(binding, endpoint);

                var channel = GetChannelFromProxy(proxy);
                if (channel != null)
                {
                    if (BeforeChannelOpens != null)
                        BeforeChannelOpens(null, new BeforeChannelOpensEventArgs
                        {
                            Channel = channel,
                            BaseAddress = baseAddress,
                            BasePath = basePath,
                            ChannelType = typeof(TServiceType),
                            MessageSize = messageSize,
                            Port = 80,
                            ServiceId = serviceId
                        });

                    OpenChannel(channel);
                }

                if (useCachedChannel)
                    lock (OpenChannels)
                        OpenChannels.Add(new OpenChannelInformation { Port = 80, ServiceId = serviceId, MessageSize = messageSize, BaseUrl = baseAddress, BasePath = basePath, Channel = proxy, ChannelType = typeof(TServiceType) });

                return proxy;
            }
            catch (Exception ex)
            {
                if (ChannelException != null)
                    ChannelException(null, new ChannelExceptionEventArgs(GetChannelFromProxy(proxy), ex));

                if (!HandleChannelExceptions) throw;

                return null;
            }
        }

        /// <summary>
        /// Creates a basic HTTP channel to the desired service type
        /// </summary>
        /// <param name="serviceType">The type of the service type.</param>
        /// <param name="messageSize">Size of the message.</param>
        /// <param name="serviceUri">The service URI.</param>
        /// <param name="useCachedChannel">If true, the system will use a channel that was cached rather than creating a new one (if possible)</param>
        /// <returns>TCP/IP channel</returns>
        private static object GetInternalWsHttpChannel(Type serviceType, MessageSize messageSize, Uri serviceUri, bool useCachedChannel)
        {
            if (messageSize == MessageSize.Undefined) messageSize = GetMessageSize(serviceType.Name);
            var serviceId = GetServiceIdFromUri(serviceUri);
            var baseAddress = GetBaseAddressFromUri(serviceUri);
            var basePath = GetBasePathFromUri(serviceUri);
            if (useCachedChannel)
            {
                int cachedChannelIndex = GetChannelCacheIndex(serviceType, messageSize, 80, serviceId, baseAddress, basePath);
                if (cachedChannelIndex > -1) return GetCachedChannel(serviceType, cachedChannelIndex);
            }

            var securityMode = serviceUri.AbsoluteUri.ToLower().StartsWith("https://") ? SecurityMode.Transport : SecurityMode.None;
            var binding = new WSHttpBinding(securityMode) { SendTimeout = new TimeSpan(0, 10, 0) };
            ServiceHelper.ConfigureMessageSizeOnWsHttpBinding(messageSize, binding);

            var endpoint = new EndpointAddress(serviceUri.AbsoluteUri);

            var factory = GetChannelFactory(serviceType, binding, endpoint);
            if (factory.State == CommunicationState.Faulted) throw new Core.Exceptions.NullReferenceException("Error creating service client.");
            object proxy = null;
            try
            {
                proxy = CreateChannel(factory);
                if (proxy == null) throw new Core.Exceptions.NullReferenceException("Requested service unavailable.");

                var channel = GetChannelFromProxy(proxy);
                if (channel != null)
                {
                    if (BeforeChannelOpens != null)
                        BeforeChannelOpens(null, new BeforeChannelOpensEventArgs
                        {
                            Channel = channel,
                            BaseAddress = baseAddress,
                            BasePath = basePath,
                            ChannelType = serviceType,
                            MessageSize = messageSize,
                            Port = 80,
                            ServiceId = serviceId
                        });

                    OpenChannel(channel);
                }

                if (useCachedChannel)
                    lock (OpenChannels)
                        OpenChannels.Add(new OpenChannelInformation { Port = 80, ServiceId = serviceId, MessageSize = messageSize, BaseUrl = baseAddress, BasePath = basePath, Channel = proxy, ChannelType = serviceType });

                return proxy;
            }
            catch (Exception ex)
            {
                if (ChannelException != null)
                    ChannelException(null, new ChannelExceptionEventArgs(GetChannelFromProxy(proxy), ex));

                if (!HandleChannelExceptions) throw;

                return null;
            }
        }

        /// <summary>
        /// Retrieves a setting from the configuration system
        /// </summary>
        /// <param name="setting">Name of the setting.</param>
        /// <param name="ignoreCache">If set to <c>true</c> setting caching is ignored.</param>
        /// <param name="defaultValue">Default value in case the setting is not found</param>
        /// <returns>Setting value</returns>
        private static string GetSetting(string setting, bool ignoreCache = false, string defaultValue = "")
        {
            var settingValue = defaultValue;

            if (CacheSettings && !ignoreCache)
                lock (SettingsCache)
                    if (SettingsCache.ContainsKey(setting))
                        return SettingsCache[setting];

            if (ConfigurationSettings.Settings.IsSettingSupported(setting))
                settingValue = ConfigurationSettings.Settings[setting];

            if (CacheSettings && !ignoreCache)
                lock (SettingsCache)
                    if (SettingsCache.ContainsKey(setting))
                        SettingsCache[setting] = settingValue;
                    else
                        SettingsCache.Add(setting, settingValue);

            return settingValue;
        }

        /// <summary>Internal settings cache</summary>
        private static readonly Dictionary<string, string> SettingsCache = new Dictionary<string, string>();
    }

    /// <summary>
    /// State of the WCF Channel
    /// </summary>
    public enum ChannelState
    {
        /// <summary>
        /// Created
        /// </summary>
        Created,
        /// <summary>
        /// Opening
        /// </summary>
        Opening,
        /// <summary>
        /// Opened
        /// </summary>
        Opened,
        /// <summary>
        /// Closing
        /// </summary>
        Closing,
        /// <summary>
        /// Closed
        /// </summary>
        Closed,
        /// <summary>
        /// Faulted
        /// </summary>
        Faulted,
        /// <summary>
        /// Unknown
        /// </summary>
        Unknown = 99
    }

    /// <summary>
    /// Class used internally to cache channel information and actual challens
    /// </summary>
    public class OpenChannelInformation
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public OpenChannelInformation()
        {
            Port = 0;
            ServiceId = string.Empty;
        }

        /// <summary>
        /// Compares two instances of this object and returns 0 if both instances represent the same cached reference
        /// </summary>
        /// <param name="serviceType">Requested service type (interface)</param>
        /// <param name="messageSize">Requested message size</param>
        /// <param name="port">Requested port</param>
        /// <param name="serviceId">Requested service ID</param>
        /// <param name="baseAddress">Requested service base URL</param>
        /// <param name="basePath">Requested service base path</param>
        /// <returns></returns>
        public bool IsMatch(Type serviceType, MessageSize messageSize, int port, string serviceId, string baseAddress, string basePath)
        {
            var localServiceId = ServiceId.Replace("/", string.Empty).Replace("\\", string.Empty);
            var localBaseUrl = BaseUrl.Replace("/", string.Empty).Replace("\\", string.Empty);
            var localBasePath = BasePath.Replace("/", string.Empty).Replace("\\", string.Empty);
            return (ChannelType == serviceType && MessageSize == messageSize && Port == port && localServiceId == serviceId && localBaseUrl == baseAddress && localBasePath == basePath);
        }

        /// <summary>
        /// Message size configured for this instance
        /// </summary>
        public MessageSize MessageSize { get; set; }
        /// <summary>
        /// Port (if applicable)
        /// </summary>
        public int Port { get; set; }
        /// <summary>
        /// Service ID (if applicable)
        /// </summary>
        public string ServiceId { get; set; }
        /// <summary>
        /// Base URL
        /// </summary>
        public string BaseUrl { get; set; }
        /// <summary>
        /// Base Path
        /// </summary>
        public string BasePath { get; set; }

        /// <summary>
        /// Cached channel object
        /// </summary>
        public object Channel { get; set; }

        /// <summary>
        /// Contract type of the channel
        /// </summary>
        public Type ChannelType { get; set; }
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

        /// <summary>
        /// Service ID (name of the interface, typically)
        /// </summary>
        public string ServiceId { get; set; }
    }

    /// <summary>
    /// Event arguments for channel open event
    /// </summary>
    public class BeforeChannelOpensEventArgs : EventArgs
    {
        /// <summary>
        /// Channel that is about to be opened
        /// </summary>
        public IClientChannel Channel { get; internal set; }

        /// <summary>
        /// Port used by the channel
        /// </summary>
        public int Port { get; internal set; }

        /// <summary>
        /// Service ID
        /// </summary>
        public string ServiceId { get; internal set; }

        /// <summary>
        /// Message Size
        /// </summary>
        public MessageSize MessageSize { get; internal set; }

        /// <summary>
        /// Base Address
        /// </summary>
        public string BaseAddress { get; internal set; }

        /// <summary>
        /// Base Path
        /// </summary>
        public string BasePath { get; internal set; }

        /// <summary>
        /// Channel Type (contract)
        /// </summary>
        public Type ChannelType { get; internal set; }
    }

    /// <summary>Special event args used for ChannelException events</summary>
    public class ChannelExceptionEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ChannelExceptionEventArgs" /> class.
        /// </summary>
        /// <param name="channel">The channel.</param>
        /// <param name="exception">The exception.</param>
        public ChannelExceptionEventArgs(IClientChannel channel, Exception exception)
        {
            Channel = channel;
            Exception = exception;
        }

        /// <summary>The channel that caused the exception</summary>
        /// <value>The channel.</value>
        public IClientChannel Channel { get; set; }

        /// <summary>The exception</summary>
        /// <value>The exception.</value>
        public Exception Exception { get; set; }
    }

    /// <summary>
    /// HTTP Verbs used to communicate with REST Services
    /// </summary>
    public enum RestHttpVerbs
    {
        /// <summary>
        /// GET
        /// </summary>
        Get,
        /// <summary>
        /// POST
        /// </summary>
        Post,
        /// <summary>
        /// PUT
        /// </summary>
        Put,
        /// <summary>
        /// DELETE
        /// </summary>
        Delete
    }

    /// <summary>
    /// Data format used by service
    /// </summary>
    public enum ServiceDataFormat
    {
        /// <summary>
        /// JSON data format
        /// </summary>
        Json,
        /// <summary>
        /// XML data format
        /// </summary>
        Xml
    }

    /// <summary>
    /// Event arguments related to events for before service operation calls
    /// </summary>
    public class BeforeServiceOperationCallEventArgs : EventArgs
    {
        /// <summary>
        /// Information about the service method/operation being called
        /// </summary>
        /// <value>The method information.</value>
        public string MethodName { get; set; }

        /// <summary>
        /// Information about the service that is called
        /// </summary>
        /// <value>The service contract.</value>
        public Type ServiceContract { get; set; }

        /// <summary>
        /// Input contracts ("parameters") to be sent to the service
        /// </summary>
        /// <value>The input data contracts.</value>
        public object[] InputDataContracts { get; set; }

        /// <summary>
        /// Instance of the service that is being called
        /// </summary>
        /// <value>The service instance.</value>
        public object ServiceInstance { get; set; }
    }

    /// <summary>
    /// Event arguments related to events for after service operation calls
    /// </summary>
    public class AfterServiceOperationCallEventArgs : BeforeServiceOperationCallEventArgs
    {
        /// <summary>
        /// The result/response created by a service call
        /// </summary>
        /// <value>The response.</value>
        public object Response { get; set; }

        /// <summary>
        /// Provides information about the duration of the call
        /// </summary>
        /// <value>
        /// The duration of the service call.
        /// </value>
        public TimeSpan ServiceCallDuration { get; set; }
    }

    /// <summary>
    /// Wrapper object used to wrap service proxies for the purpose of raising ServiceClient events
    /// </summary>
    /// <seealso cref="CODE.Framework.Core.Utilities.IProxyHandler" />
    public class ServiceProxyEventWrapper : IProxyHandler
    {
        private readonly object _originalProxy;
        private readonly Type _contractType;

        /// <summary>
        /// Gets the original proxy.
        /// </summary>
        /// <value>
        /// The original proxy.
        /// </value>
        public object OriginalProxy
        {
            get { return _originalProxy; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceProxyEventWrapper" /> class.
        /// </summary>
        /// <param name="originalProxy">The original proxy.</param>
        /// <param name="contractType">Type of the contract.</param>
        public ServiceProxyEventWrapper(object originalProxy, Type contractType)
        {
            _originalProxy = originalProxy;
            _contractType = contractType;
        }

        /// <summary>
        /// This method is called when any method on a proxied object is invoked.
        /// </summary>
        /// <param name="method">Information about the method being called.</param>
        /// <param name="args">The arguments passed to the method.</param>
        /// <returns>Result value from the proxy call</returns>
        public object OnMethod(MethodInfo method, object[] args)
        {
            ServiceClient.RaiseBeforeServiceOperationCall(new BeforeServiceOperationCallEventArgs
            {
                MethodName = method.Name,
                ServiceContract = _contractType,
                ServiceInstance = _originalProxy,
                InputDataContracts = args
            });

            var startTimeStamp = Environment.TickCount;
            var result = method.Invoke(_originalProxy, args);

            ServiceClient.RaiseAfterServiceOperationCall(new AfterServiceOperationCallEventArgs
            {
                MethodName = method.Name,
                ServiceContract = _contractType,
                ServiceInstance = _originalProxy,
                InputDataContracts = args,
                Response = result,
                ServiceCallDuration = new TimeSpan(Environment.TickCount - startTimeStamp)
            });

            return result;
        }
    }
}

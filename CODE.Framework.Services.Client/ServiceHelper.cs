using System.ServiceModel;

namespace CODE.Framework.Services.Client
{
    /// <summary>
    /// Generic helper methods used internally by various service components
    /// </summary>
    internal static class ServiceHelper
    {
        /// <summary>
        /// Configures NetTcpBinding message size
        /// </summary>
        /// <param name="messageSize">Message size to configure the binding for</param>
        /// <param name="binding">NetTcpBinding to configure</param>
        public static void ConfigureMessageSizeOnNetTcpBinding(MessageSize messageSize, NetTcpBinding binding)
        {
            if (messageSize == MessageSize.Medium)
            {
                binding.MaxBufferSize = 1024 * 1024 * 10; // 10MB
                binding.MaxBufferPoolSize = binding.MaxBufferSize;
                binding.MaxReceivedMessageSize = binding.MaxBufferSize;
                binding.ReaderQuotas.MaxArrayLength = binding.MaxBufferSize;
                binding.ReaderQuotas.MaxStringContentLength = binding.MaxBufferSize;
            }
            else if (messageSize == MessageSize.Large)
            {
                binding.MaxBufferSize = 1024 * 1024 * 100; // 100MB
                binding.MaxBufferPoolSize = binding.MaxBufferSize;
                binding.MaxReceivedMessageSize = binding.MaxBufferSize;
                binding.ReaderQuotas.MaxArrayLength = binding.MaxBufferSize;
                binding.ReaderQuotas.MaxStringContentLength = binding.MaxBufferSize;
            }
            else if (messageSize == MessageSize.VeryLarge)
            {
                binding.MaxBufferSize = 1024 * 1024 * 1024; // 1GB
                binding.MaxBufferPoolSize = binding.MaxBufferSize;
                binding.MaxReceivedMessageSize = binding.MaxBufferSize;
                binding.ReaderQuotas.MaxArrayLength = binding.MaxBufferSize;
                binding.ReaderQuotas.MaxStringContentLength = binding.MaxBufferSize;
            }
            else if (messageSize == MessageSize.Max)
            {
                binding.MaxBufferSize = int.MaxValue;
                binding.MaxBufferPoolSize = binding.MaxBufferSize;
                binding.MaxReceivedMessageSize = binding.MaxBufferSize;
                binding.ReaderQuotas.MaxArrayLength = binding.MaxBufferSize;
                binding.ReaderQuotas.MaxStringContentLength = binding.MaxBufferSize;
            }
        }

        /// <summary>
        /// Configures Basic HTTP Binding message size
        /// </summary>
        /// <param name="messageSize">Message size to configure the binding for</param>
        /// <param name="binding">BasicHttpBinding to configure</param>
        public static void ConfigureMessageSizeOnBasicHttpBinding(MessageSize messageSize, BasicHttpBinding binding)
        {
            if (messageSize == MessageSize.Medium)
            {
                binding.MaxBufferSize = 1024 * 1024 * 10; // 10MB
                binding.MaxBufferPoolSize = binding.MaxBufferSize;
                binding.MaxReceivedMessageSize = binding.MaxBufferSize;
                binding.ReaderQuotas.MaxArrayLength = binding.MaxBufferSize;
                binding.ReaderQuotas.MaxStringContentLength = binding.MaxBufferSize;
            }
            else if (messageSize == MessageSize.Large)
            {
                binding.MaxBufferSize = 1024 * 1024 * 100; // 100MB
                binding.MaxBufferPoolSize = binding.MaxBufferSize;
                binding.MaxReceivedMessageSize = binding.MaxBufferSize;
                binding.ReaderQuotas.MaxArrayLength = binding.MaxBufferSize;
                binding.ReaderQuotas.MaxStringContentLength = binding.MaxBufferSize;
            }
            else if (messageSize == MessageSize.VeryLarge)
            {
                binding.MaxBufferSize = 1024 * 1024 * 1024; // 1GB
                binding.MaxBufferPoolSize = binding.MaxBufferSize;
                binding.MaxReceivedMessageSize = binding.MaxBufferSize;
                binding.ReaderQuotas.MaxArrayLength = binding.MaxBufferSize;
                binding.ReaderQuotas.MaxStringContentLength = binding.MaxBufferSize;
            }
            else if (messageSize == MessageSize.Max)
            {
                binding.MaxBufferSize = int.MaxValue;
                binding.MaxBufferPoolSize = binding.MaxBufferSize;
                binding.MaxReceivedMessageSize = binding.MaxBufferSize;
                binding.ReaderQuotas.MaxArrayLength = binding.MaxBufferSize;
                binding.ReaderQuotas.MaxStringContentLength = binding.MaxBufferSize;
            }
        }

        /// <summary>
        /// Configures WS HTTP Binding message size
        /// </summary>
        /// <param name="messageSize">Message size to configure the binding for</param>
        /// <param name="binding">BasicHttpBinding to configure</param>
        public static void ConfigureMessageSizeOnWsHttpBinding(MessageSize messageSize, WSHttpBinding binding)
        {
            if (messageSize == MessageSize.Medium)
            {
                binding.MaxBufferPoolSize = 1024 * 1024 * 10; // 10MB
                binding.MaxReceivedMessageSize = binding.MaxBufferPoolSize;
                binding.ReaderQuotas.MaxArrayLength = (int)binding.MaxBufferPoolSize;
                binding.ReaderQuotas.MaxStringContentLength = (int)binding.MaxBufferPoolSize;
            }
            else if (messageSize == MessageSize.Large)
            {
                binding.MaxBufferPoolSize = 1024 * 1024 * 100; // 100MB
                binding.MaxReceivedMessageSize = binding.MaxBufferPoolSize;
                binding.ReaderQuotas.MaxArrayLength = (int)binding.MaxBufferPoolSize;
                binding.ReaderQuotas.MaxStringContentLength = (int)binding.MaxBufferPoolSize;
            }
            else if (messageSize == MessageSize.VeryLarge)
            {
                binding.MaxBufferPoolSize = 1024 * 1024 * 1024; // 1GB
                binding.MaxReceivedMessageSize = binding.MaxBufferPoolSize;
                binding.ReaderQuotas.MaxArrayLength = (int)binding.MaxBufferPoolSize;
                binding.ReaderQuotas.MaxStringContentLength = (int)binding.MaxBufferPoolSize;
            }
            else if (messageSize == MessageSize.Max)
            {
                binding.MaxBufferPoolSize = int.MaxValue;
                binding.MaxReceivedMessageSize = binding.MaxBufferPoolSize;
                binding.ReaderQuotas.MaxArrayLength = (int)binding.MaxBufferPoolSize;
                binding.ReaderQuotas.MaxStringContentLength = (int)binding.MaxBufferPoolSize;
            }
        }
    }
}

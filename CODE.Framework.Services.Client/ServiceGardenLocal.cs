using System;
using System.Collections.Generic;
using CODE.Framework.Core.Exceptions;

namespace CODE.Framework.Services.Client
{
    /// <summary>
    /// Provides an in-process service garden
    /// </summary>
    public static class ServiceGardenLocal
    {
        /// <summary>
        /// Initializes the <see cref="ServiceGardenLocal"/> class.
        /// </summary>
        static ServiceGardenLocal()
        {
            Hosts = new Dictionary<Type, object>();
        }

        /// <summary>
        /// Collection of known hosts
        /// </summary>
        /// <value>Hosts</value>
        private static Dictionary<Type, object> Hosts { get; set; }

        /// <summary>
        /// Adds a local service based on the services type
        /// </summary>
        /// <param name="serviceType">Type of the service.</param>
        /// <returns>True if successful</returns>
        /// <remarks>The interface used by the service is automatically determined.</remarks>
        /// <example>
        /// ServiceGardenLocal.AddServiceHost(typeof(MyNamespace.CustomerService));
        /// </example>
        public static bool AddServiceHost(Type serviceType)
        {
            Type contractType;
            var interfaces = serviceType.GetInterfaces();
            if (interfaces.Length == 1)
                contractType = interfaces[0];
            else
                throw new IndexOutOfBoundsException("Service contract cannot be automatically determined for the specified service type.");
            return AddServiceHost(serviceType, contractType);
        }

        /// <summary>
        /// Adds a local service
        /// </summary>
        /// <param name="serviceType">Type of the service.</param>
        /// <param name="contractType">Type of the operation contract (interface).</param>
        /// <returns>True if successful</returns>
        /// <example>
        /// ServiceGardenLocal.AddServiceHost(typeof(MyNamespace.CustomerService), typeof(MyContracts.ICustomerServicce));
        /// </example>
        public static bool AddServiceHost(Type serviceType, Type contractType)
        {
            Hosts.Add(contractType, Activator.CreateInstance(serviceType));
            return true;
        }

        /// <summary>
        /// Gets the service.
        /// </summary>
        /// <typeparam name="TContractType">The type of the operations ontract (interface).</typeparam>
        /// <returns></returns>
        public static TContractType GetService<TContractType>()
        {
            if (Hosts.ContainsKey(typeof(TContractType)))
                return (TContractType)Hosts[typeof(TContractType)];
            return default(TContractType);
        }

        /// <summary>
        /// Gets the service.
        /// </summary>
        /// <param name="contractType">Contract Type</param>
        /// <returns></returns>
        public static object GetService(Type contractType)
        {
            if (Hosts.ContainsKey(contractType))
                return Hosts[contractType];
            return null;
        }
    }
}

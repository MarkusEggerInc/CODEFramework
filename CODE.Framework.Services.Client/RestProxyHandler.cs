using System;
using System.Net;
using System.Reflection;
using System.ServiceModel;
using System.Text;
using CODE.Framework.Core.Utilities;

namespace CODE.Framework.Services.Client
{
    /// <summary>
    /// Standard implementation for a REST Proxy handler
    /// </summary>
    public class RestProxyHandler : IProxyHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RestProxyHandler"/> class.
        /// </summary>
        /// <param name="serviceUri">The service root URI.</param>
        public RestProxyHandler(Uri serviceUri)
        {
            _serviceUri = serviceUri;
        }

        private readonly Uri _serviceUri;
        private Type _contractType;

        /// <summary>
        /// This method is called when any method on a proxied object is invoked.
        /// </summary>
        /// <param name="method">Information about the method being called.</param>
        /// <param name="args">The arguments passed to the method.</param>
        /// <returns>Result value from the proxy call</returns>
        public object OnMethod(MethodInfo method, object[] args)
        {
            if (args.Length != 1) throw new Exception("Only methods with one parameter can be used through REST proxies.");
            var data = args[0];

            if (_contractType == null)
            {
                var declaringType = method.DeclaringType;
                if (declaringType == null) throw new Exception("Can't determine declaring type of method '" + method.Name + "'.");
                if (declaringType.IsInterface)
                    _contractType = declaringType;
                else
                {
                    var interfaces = declaringType.GetInterfaces();
                    if (interfaces.Length != 1) throw new Exception("Can't determine declaring contract interface for method '" + method.Name + "'.");
                    _contractType = interfaces[0];
                }
            }

            var httpMethod = RestHelper.GetHttpMethodFromContract(method.Name, _contractType);
            var exposedMethodName = RestHelper.GetExposedMethodNameFromContract(method.Name, httpMethod, _contractType);

            try
            {
                using (var client = new WebClient())
                {
                    client.Headers.Add("Content-Type", "application/json; charset=utf-8");
                    client.Encoding = Encoding.UTF8;
                    string restResponse;
                    switch (httpMethod)
                    {
                        case "POST":
                            restResponse = client.UploadString(_serviceUri.AbsoluteUri + "/" + exposedMethodName, JsonHelper.SerializeToRestJson(data));
                            break;
                        case "GET":
                            var serializedData = RestHelper.SerializeToUrlParameters(data);
                            restResponse = client.DownloadString(_serviceUri.AbsoluteUri + "/" + exposedMethodName + serializedData);
                            break;
                        default:
                            restResponse = client.UploadString(_serviceUri.AbsoluteUri + "/" + exposedMethodName, httpMethod, JsonHelper.SerializeToRestJson(data));
                            break;
                    }
                    return JsonHelper.DeserializeFromRestJson(restResponse, method.ReturnType);
                }
            }
            catch (Exception ex)
            {
                throw new CommunicationException("Unable to communicate with REST service.", ex);
            }
        }
    }
}
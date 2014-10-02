using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.Web;

namespace CODE.Framework.Services.Server
{
    /// <summary>
    /// Endpoint behavior configuration specific to XML formatted REST calls
    /// </summary>
    public class RestXmlHttpBehavior : WebHttpBehavior
    {
        /// <summary>Handles REST XML formatting behavior</summary>
        /// <param name="operationDescription"></param>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        protected override IDispatchMessageFormatter GetReplyDispatchFormatter(OperationDescription operationDescription, ServiceEndpoint endpoint)
        {
            var webInvoke = GetBehavior<WebInvokeAttribute>(operationDescription);
            if (webInvoke == null)
            {
                webInvoke = new WebInvokeAttribute();
                operationDescription.Behaviors.Add(webInvoke);
            }
            webInvoke.RequestFormat = WebMessageFormat.Xml;
            webInvoke.ResponseFormat = WebMessageFormat.Xml;
            webInvoke.Method = "POST";

            var formatter = base.GetReplyDispatchFormatter(operationDescription, endpoint);
            return formatter;
        }

        /// <summary>
        /// Tries to find a behavior attribute of a certain type and returns it
        /// </summary>
        /// <typeparam name="T">Type of behavior we are looking for</typeparam>
        /// <param name="operationDescription">Operation description</param>
        /// <returns>Behavior or null</returns>
        private static T GetBehavior<T>(OperationDescription operationDescription) where T : class
        {
            foreach (var behavior in operationDescription.Behaviors)
            {
                var webGetAttribute = behavior as T;
                if (webGetAttribute != null)
                    return webGetAttribute;
            }
            return null;
        }
    }

    /// <summary>
    /// Endpoint behavior configuration specific to XML formatted REST calls
    /// </summary>
    public class RestJsonHttpBehavior : WebHttpBehavior
    {
        /// <summary>Handles REST JSON formatting behavior</summary>
        /// <param name="operationDescription"></param>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        protected override IDispatchMessageFormatter GetReplyDispatchFormatter(OperationDescription operationDescription, ServiceEndpoint endpoint)
        {
            var webInvoke = GetBehavior<WebInvokeAttribute>(operationDescription);
            if (webInvoke == null)
            {
                webInvoke = new WebInvokeAttribute();
                operationDescription.Behaviors.Add(webInvoke);
            }
            webInvoke.RequestFormat = WebMessageFormat.Json;
            webInvoke.ResponseFormat = WebMessageFormat.Json;
            webInvoke.Method = "POST";

            var formatter = base.GetReplyDispatchFormatter(operationDescription, endpoint);
            return formatter;
        }

        /// <summary>
        /// Tries to find a behavior attribute of a certain type and returns it
        /// </summary>
        /// <typeparam name="T">Type of behavior we are looking for</typeparam>
        /// <param name="operationDescription">Operation description</param>
        /// <returns>Behavior or null</returns>
        private static T GetBehavior<T>(OperationDescription operationDescription) where T : class
        {
            foreach (var behavior in operationDescription.Behaviors)
            {
                var webGetAttribute = behavior as T;
                if (webGetAttribute != null)
                    return webGetAttribute;
            }
            return null;
        }
    }
}

using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace CODE.Framework.Services.Server
{
    /// <summary>
    /// Custom endpoint behavior object that gets applied automatically when script-cross-domain-calls are enabled on the ServiceGarden class.
    /// </summary>
    public class CrossDomainScriptBehavior : IEndpointBehavior
    {
        /// <summary>
        /// Implements a modification or extension of the service across an endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint that exposes the contract.</param>
        /// <param name="endpointDispatcher">The endpoint dispatcher to be modified or extended.</param>
        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
        {
            foreach (var channelEndpoint in endpointDispatcher.ChannelDispatcher.Endpoints)
                channelEndpoint.DispatchRuntime.MessageInspectors.Add(new CrossDomainScriptCallMessageInspector());
        }
        /// <summary>
        /// Implement to confirm that the endpoint meets some intended criteria.
        /// </summary>
        /// <param name="endpoint">The endpoint to validate.</param>
        public void Validate(ServiceEndpoint endpoint) { }
        /// <summary>
        /// Applies the dispatch behavior.
        /// </summary>
        /// <param name="operationDescription">The operation description.</param>
        /// <param name="dispatchOperation">The dispatch operation.</param>
        public void ApplyDispatchBehavior(OperationDescription operationDescription, DispatchOperation dispatchOperation) { }
        /// <summary>
        /// Implement to pass data at runtime to bindings to support custom behavior.
        /// </summary>
        /// <param name="endpoint">The endpoint to modify.</param>
        /// <param name="bindingParameters">The objects that binding elements require to support the behavior.</param>
        public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters) { }
        /// <summary>
        /// Implements a modification or extension of the client across an endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint that is to be customized.</param>
        /// <param name="clientRuntime">The client runtime to be customized.</param>
        public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime) { }
    }

    /// <summary>
    /// Inspector object used to add a cross-domain-call HTTP header
    /// </summary>
    public class CrossDomainScriptCallMessageInspector : IDispatchMessageInspector
    {
        /// <summary>
        /// Called after an inbound message has been received but before the message is dispatched to the intended operation.
        /// </summary>
        /// <param name="request">The request message.</param>
        /// <param name="channel">The incoming channel.</param>
        /// <param name="instanceContext">The current service instance.</param>
        /// <returns>
        /// The object used to correlate state. This object is passed back in the <see cref="M:System.ServiceModel.Dispatcher.IDispatchMessageInspector.BeforeSendReply(System.ServiceModel.Channels.Message@,System.Object)"/> method.
        /// </returns>
        public object AfterReceiveRequest(ref Message request, System.ServiceModel.IClientChannel channel, System.ServiceModel.InstanceContext instanceContext)
        {
            return null;
        }

        /// <summary>
        /// Called after the operation has returned but before the reply message is sent.
        /// </summary>
        /// <param name="reply">The reply message. This value is null if the operation is one way.</param>
        /// <param name="correlationState">The correlation object returned from the <see cref="M:System.ServiceModel.Dispatcher.IDispatchMessageInspector.AfterReceiveRequest(System.ServiceModel.Channels.Message@,System.ServiceModel.IClientChannel,System.ServiceModel.InstanceContext)"/> method.</param>
        public void BeforeSendReply(ref Message reply, object correlationState)
        {
            HttpResponseMessageProperty httpResponseMessage;
            object httpResponseMessageObject;
            if (reply.Properties.TryGetValue(HttpResponseMessageProperty.Name, out httpResponseMessageObject))
            {
                httpResponseMessage = httpResponseMessageObject as HttpResponseMessageProperty;
                if (httpResponseMessage != null)
                    if (string.IsNullOrEmpty(httpResponseMessage.Headers["Access-Control-Allow-Origin"]))
                        httpResponseMessage.Headers["Access-Control-Allow-Origin"] = ServiceGarden.HttpCrossDomainCallsAllowedFrom;
            }
            else
            {
                httpResponseMessage = new HttpResponseMessageProperty();
                httpResponseMessage.Headers.Add("Access-Control-Allow-Origin", ServiceGarden.HttpCrossDomainCallsAllowedFrom);
                reply.Properties.Add(HttpResponseMessageProperty.Name, httpResponseMessage);
            }
        }
    }
}

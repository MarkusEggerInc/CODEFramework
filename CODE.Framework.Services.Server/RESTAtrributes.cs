using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.Web;
using System.Text;
using System.Xml;
using CODE.Framework.Core.Utilities;
using CODE.Framework.Services.Contracts;

namespace CODE.Framework.Services.Server
{
    /// <summary>
    /// Helper functionality needed for REST operations
    /// </summary>
    public static class RestHelper
    {
        /// <summary>
        /// Gets the HTTP method/verb from operation description.
        /// </summary>
        /// <param name="operationDescription">The operation description.</param>
        /// <returns>System.String.</returns>
        public static string GetHttpMethodFromOperationDescription(OperationDescription operationDescription)
        {
            const string defaultMethod = "POST"; // CODE Framework default for REST operations
            var contractType = operationDescription.DeclaringContract.ContractType;
            var methodInfo = contractType.GetMethod(operationDescription.Name);
            if (methodInfo == null) return defaultMethod;
            var attributes = methodInfo.GetCustomAttributes(typeof (RestAttribute), true);
            if (attributes.Length <= 0) return defaultMethod;
            var restAttribute = attributes[0] as RestAttribute;
            if (restAttribute == null) return defaultMethod;
            
            var method = restAttribute.Method.ToString().ToUpper();
            return method;
        }

        /// <summary>
        /// Inspects the specified method in the contract for special configuration to see what the REST-exposed method name is supposed to be
        /// </summary>
        /// <param name="actualMethodName">Actual name of the method.</param>
        /// <param name="httpMethod">The HTTP method.</param>
        /// <param name="contractType">Service contract type.</param>
        /// <returns>REST-exposed name of the method</returns>
        public static string GetExposedMethodNameFromContract(string actualMethodName, string httpMethod, Type contractType)
        {
            var methods = ObjectHelper.GetAllMethodsForInterface(contractType).Where(m => m.Name == actualMethodName).ToList();
            foreach (var method in methods)
            {
                var restAttribute = GetRestAttribute(method);
                if (string.Equals(restAttribute.Method.ToString(), httpMethod, StringComparison.OrdinalIgnoreCase))
                {
                    if (restAttribute.Name == null) return method.Name;
                    if (restAttribute.Name == string.Empty) return string.Empty;
                    return restAttribute.Name;
                }
            }
            return actualMethodName;
        }

        /// <summary>
        /// Returns the exposed HTTP-method/verb for the provided method
        /// </summary>
        /// <param name="methodName">Name of the method.</param>
        /// <param name="contractType">Service contract type.</param>
        /// <returns>HTTP Method/Verb</returns>
        public static string GetHttpMethodFromContract(string methodName, Type contractType)
        {
            var method = ObjectHelper.GetAllMethodsForInterface(contractType).FirstOrDefault(m => m.Name == methodName);
            return method == null ? "POST" : GetRestAttribute(method).Method.ToString().ToUpper();
        }

        /// <summary>
        /// Extracts the name of the method a REST call was aimed at based on the provided url "fragment" (URL minus the root URL part),
        /// the HTTP method (get, post, put, ...) and the contract type
        /// </summary>
        /// <param name="urlFragment">The URL fragment.</param>
        /// <param name="httpMethod">The HTTP method.</param>
        /// <param name="contractType">Service contract type.</param>
        /// <returns>Method picked as a match within the contract (or null if no matching method was found)</returns>
        /// <remarks>
        /// Methods are picked based on a number of parameters for each fragment and HTTP method.
        /// 
        /// Example URL Fragment: /CustomerSearch/Smith (HTTP-GET)
        /// 
        /// In this case, the "CustomerSearch" part of the fragment is considered a good candidate for a method name match.
        /// The method thus looks at the contract definition and searches for methods of the same name (case insensitive!)
        /// as well as the Rest(Name="xxx") attribute on each method to see if there is a match. If a match is found, the HTTP-Method is also
        /// compared and has to be a match (there could be two methods of the same exposed name, but differing HTTP methods/verbs).
        /// 
        /// If no matching method is found, "CustomerSearch" is considered to be a parameter rather than a method name, and therefore, the method
        /// name is assumed to be empty (the default method). Therefore, a method with a [Rest(Name="")] with a matching HTTP method is searched for.
        /// For a complete match, the method in question would thus have to have the following attribute declared: [Rest(Name="", Method=RestMethods.Get)]
        /// </remarks>
        public static MethodInfo GetMethodNameFromUrlFragmentAndContract(string urlFragment, string httpMethod, Type contractType)
        {
            if (urlFragment.StartsWith("/")) urlFragment = urlFragment.Substring(1);
            var firstParameter = string.Empty;
            if (urlFragment.IndexOf("/", StringComparison.Ordinal) > -1) firstParameter = urlFragment.Substring(0, urlFragment.IndexOf("/", StringComparison.Ordinal));
            else if (!string.IsNullOrEmpty(urlFragment)) firstParameter = urlFragment;

            var methods = ObjectHelper.GetAllMethodsForInterface(contractType);
            var methodInfos = methods as MethodInfo[] ?? methods.ToArray(); // Preventing multiple enumeration problems

            // We first check for named methods
            foreach (var method in methodInfos)
            {
                var restAttribute = GetRestAttribute(method);
                var methodName = method.Name;
                if (restAttribute != null && restAttribute.Name != null) methodName = restAttribute.Name;
                var httpMethodForMethod = restAttribute?.Method.ToString().ToUpper() ?? "GET";
                if (httpMethodForMethod == httpMethod && string.Equals(methodName, firstParameter, StringComparison.CurrentCultureIgnoreCase)) 
                    return method;
                if (httpMethodForMethod == "POSTORPUT" && (string.Equals("POST", httpMethod, StringComparison.OrdinalIgnoreCase) || string.Equals("PUT", httpMethod, StringComparison.OrdinalIgnoreCase)) && string.Equals(methodName, firstParameter, StringComparison.CurrentCultureIgnoreCase))
                    return method;
            }

            // If we haven't found anything yet, we check for default methods
            foreach (var method in methodInfos)
            {
                var restAttribute = GetRestAttribute(method);
                if (!string.IsNullOrEmpty(restAttribute.Name)) continue; // We are now only intersted in the empty ones
                var httpMethodForMethod = restAttribute.Method.ToString().ToUpper();
                if (restAttribute.Name != null)
                {
                    if (string.IsNullOrEmpty(restAttribute.Name) && string.Equals(httpMethodForMethod, httpMethod, StringComparison.OrdinalIgnoreCase))
                        return method;
                    if (string.IsNullOrEmpty(restAttribute.Name) && httpMethodForMethod == "POSTORPUT" && (string.Equals("POST", httpMethod, StringComparison.OrdinalIgnoreCase) || string.Equals("PUT", httpMethod, StringComparison.OrdinalIgnoreCase)))
                        return method;
                }
            }

            return null;
        }

        /// <summary>
        /// Extracts the RestAttribute from a method's attributes
        /// </summary>
        /// <param name="method">The method to be inspected</param>
        /// <returns>The applied RestAttribute or a default RestAttribute.</returns>
        public static RestAttribute GetRestAttribute(MethodInfo method)
        {
            var customAttributes = method.GetCustomAttributes(typeof (RestAttribute), true);
            if (customAttributes.Length <= 0) return new RestAttribute();
            var restAttribute = customAttributes[0] as RestAttribute;
            return restAttribute ?? new RestAttribute();
        }

        /// <summary>
        /// Extracts the RestUrlParameterAttribute from a property's attributes
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>The applied RestUrlParameterAttribute or a default RestUrlParameterAttribute</returns>
        public static RestUrlParameterAttribute GetRestUrlParameterAttribute(PropertyInfo property)
        {
            var customAttributes = property.GetCustomAttributes(typeof(RestUrlParameterAttribute), true);
            if (customAttributes.Length <= 0) return new RestUrlParameterAttribute();
            var restAttribute = customAttributes[0] as RestUrlParameterAttribute;
            return restAttribute ?? new RestUrlParameterAttribute();
        }

        /// <summary>
        /// Gets a list of all properties that are to be used as inline parameters, sorted by their sequence
        /// </summary>
        /// <param name="contractType">Contract type</param>
        /// <returns>List of properties to be used as inline URL parameters</returns>
        public static List<PropertyInfo> GetOrderedInlinePropertyList(Type contractType)
        {
            var propertiesToSerialize = contractType.GetProperties();
            var inlineParameterProperties = new List<PropertySorter>();
            if (propertiesToSerialize.Length == 1) // If we only have one parameter, we always allow passing it as an inline parameter, unless it is specifically flagged as a named parameter
            {
                var parameterAttribute = GetRestUrlParameterAttribute(propertiesToSerialize[0]);
                if (parameterAttribute == null || parameterAttribute.Mode == UrlParameterMode.Inline)
                    inlineParameterProperties.Add(new PropertySorter {Property = propertiesToSerialize[0]});
            }
            else
                foreach (var property in propertiesToSerialize)
                {
                    var parameterAttribute = GetRestUrlParameterAttribute(property);
                    if (parameterAttribute != null && parameterAttribute.Mode == UrlParameterMode.Inline)
                        inlineParameterProperties.Add(new PropertySorter {Sequence = parameterAttribute.Sequence, Property = property});
                }
            return inlineParameterProperties.OrderBy(inline => inline.Sequence).Select(inline => inline.Property).ToList();
        }

        /// <summary>
        /// Returns a list of all properties of the provided object that are NOT flagged to be used as inline URL parameters
        /// </summary>
        /// <param name="contractType">Contract type</param>
        /// <returns>List of named properties</returns>
        public static List<PropertyInfo> GetNamedPropertyList(Type contractType)
        {
            var propertiesToSerialize = contractType.GetProperties();
            var properties = new List<PropertyInfo>();
            if (propertiesToSerialize.Length == 1) // If there is only one property, we allow passing it as a named parameter, unless it is specifically flagged with a mode
            {
                var parameterAttribute = GetRestUrlParameterAttribute(propertiesToSerialize[0]);
                if (parameterAttribute == null || parameterAttribute.Mode == UrlParameterMode.Named)
                    properties.Add(propertiesToSerialize[0]);
            }
            else
                foreach (var property in propertiesToSerialize)
                {
                    var parameterAttribute = GetRestUrlParameterAttribute(property);
                    if (parameterAttribute == null || parameterAttribute.Mode != UrlParameterMode.Named) continue;
                    properties.Add(property);
                }
            return properties;
        }

        /// <summary>
        /// Serializes an object to URL parameters
        /// </summary>
        /// <param name="objectToSerialize">The object to serialize.</param>
        /// <param name="httpMethod">The HTTP method.</param>
        /// <returns>System.String.</returns>
        /// <remarks>This is used for REST GET operatoins</remarks>
        public static string SerializeToUrlParameters(object objectToSerialize, string httpMethod = "GET")
        {
            var typeToSerialize = objectToSerialize.GetType();
            var inlineParameterProperties = GetOrderedInlinePropertyList(typeToSerialize);
            var namedParameterProperties = GetNamedPropertyList(typeToSerialize);

            var sb = new StringBuilder();
            foreach (var inlineProperty in inlineParameterProperties)
            {
                var propertyValue = inlineProperty.GetValue(objectToSerialize, null);
                sb.Append("/");
                if (propertyValue != null)
                    sb.Append(HttpHelper.UrlEncode(propertyValue.ToString())); // TODO: We need to make sure we are doing well for specific property types
            }
            if (httpMethod == "GET" && namedParameterProperties.Count > 0)
            {
                var isFirst = true;
                foreach (var namedProperty in namedParameterProperties)
                {
                    var propertyValue = namedProperty.GetValue(objectToSerialize, null);
                    if (propertyValue == null) continue;
                    if (isFirst) sb.Append("?");
                    if (!isFirst) sb.Append("&");
                    sb.Append(namedProperty.Name + "=" + HttpHelper.UrlEncode(propertyValue.ToString())); // TODO: We need to make sure we are doing well for specific property types
                    isFirst = false;
                }
            }

            return sb.ToString();
        }

        private class PropertySorter
        {
            public int Sequence { get; set; }
            public PropertyInfo Property { get; set; }
        }

        /// <summary>
        /// Inspects the URL fragment, trims the method name (if appropriate) and returns the remaining parameters as a dictionary
        /// of correlating property names and their values
        /// </summary>
        /// <param name="urlFragment">The URL fragment.</param>
        /// <param name="httpMethod">The HTTP method.</param>
        /// <param name="contractType">Service contract types.</param>
        /// <returns>Dictionary of property values</returns>
        public static Dictionary<string, object> GetUrlParametersFromUrlFragmentAndContract(string urlFragment, string httpMethod, Type contractType)
        {
            if (urlFragment.StartsWith("/")) urlFragment = urlFragment.Substring(1);
            var firstParameter = string.Empty;
            if (urlFragment.IndexOf("/", StringComparison.Ordinal) > -1) firstParameter = urlFragment.Substring(0, urlFragment.IndexOf("/", StringComparison.Ordinal));
            else if (!string.IsNullOrEmpty(urlFragment)) firstParameter = urlFragment;

            var methods = ObjectHelper.GetAllMethodsForInterface(contractType);
            MethodInfo foundMethod = null;
            foreach (var method in methods)
            {
                var restAttribute = GetRestAttribute(method);
                var httpMethodForMethod = restAttribute.Method.ToString().ToUpper();

                if (!string.Equals(httpMethod, httpMethodForMethod, StringComparison.OrdinalIgnoreCase)) continue;
                var methodName = method.Name;
                if (!string.IsNullOrEmpty(restAttribute.Name)) methodName = restAttribute.Name;
                if (!string.Equals(methodName, firstParameter, StringComparison.OrdinalIgnoreCase)) continue;
                urlFragment = urlFragment.Substring(methodName.Length);
                if (urlFragment.StartsWith("/")) urlFragment = urlFragment.Substring(1);
                foundMethod = method;
                break; // We found our methoid
            }

            if (foundMethod == null) // We haven't found our method yet. If there is a default method (a method with an empty REST name) that matches the HTTP method, we will use that instead
                foreach (var method in methods)
                {
                    var restAttribute = GetRestAttribute(method);
                    if (restAttribute.Name != "") continue;
                    var httpMethodForMethod = restAttribute.Method.ToString().ToUpper();

                    if (!string.Equals(httpMethod, httpMethodForMethod, StringComparison.OrdinalIgnoreCase)) continue;
                    foundMethod = method;
                    break; // We found our methoid
                }

            if (foundMethod == null) return new Dictionary<string, object>(); // We didn't find a match, therefore, we can't map anything
            var foundMethodParameters = foundMethod.GetParameters();
            if (foundMethodParameters.Length != 1) return new Dictionary<string, object>(); // The method signature has multiple parameters, so we can't handle it (Note: Other code in the chain will probably throw an exception about it, so we just return out here as we do not want duplicate exceptions)
            var firstParameterType = foundMethodParameters[0].ParameterType;

            // Ready to extract the parameters
            var inlineParameterString = string.Empty;
            var namedParameterString = string.Empty;
            var separatorPosition = urlFragment.IndexOf("?", StringComparison.Ordinal);
            if (separatorPosition > -1)
            {
                inlineParameterString = urlFragment.Substring(0, separatorPosition);
                namedParameterString = urlFragment.Substring(separatorPosition + 1);
            }
            else
            {
                if (urlFragment.IndexOf("=", StringComparison.Ordinal) > -1) namedParameterString = urlFragment;
                else inlineParameterString = urlFragment;
            }

            var dictionary = new Dictionary<string, object>();

            // Parsing the inline parameters
            if (!string.IsNullOrEmpty(inlineParameterString))
            {
                var inlineParameters = inlineParameterString.Split('/');
                var inlineProperties = RestHelper.GetOrderedInlinePropertyList(firstParameterType);
                for (var propertyCounter = 0; propertyCounter < inlineParameters.Length; propertyCounter++)
                {
                    if (propertyCounter >= inlineProperties.Count) break; // We overshot the available parameters for some reason
                    var parameterString = HttpHelper.UrlDecode(inlineParameters[propertyCounter]);
                    var parameterValue = ConvertValue(parameterString, inlineProperties[propertyCounter].PropertyType);
                    dictionary.Add(inlineProperties[propertyCounter].Name, parameterValue);
                }
            }

            // Parsing the named parameters
            if (!string.IsNullOrEmpty(namedParameterString))
            {
                var parameterElements = namedParameterString.Split('&');
                foreach (var parameterElement in parameterElements)
                {
                    var parameterNameValuePair = parameterElement.Split('=');
                    if (parameterNameValuePair.Length != 2) continue;
                    var currentProperty = firstParameterType.GetProperty(parameterNameValuePair[0]);
                    if (currentProperty == null) continue;
                    var currentPropertyString = HttpHelper.UrlDecode(parameterNameValuePair[1]);
                    var currentPropertyValue = ConvertValue(currentPropertyString, currentProperty.PropertyType);
                    dictionary.Add(parameterNameValuePair[0], currentPropertyValue);
                }
            }

            return dictionary;
        }

        private static object ConvertValue(string value, Type propertyType)
        {
            if (propertyType == typeof (string)) return value; // Very likely case, so we handle this right away, even though we are also handling it below
            if (propertyType.IsEnum) return Enum.Parse(propertyType, value);
            if (propertyType == typeof (Guid)) return Guid.Parse(value);
            if (propertyType == typeof (bool)) return Convert.ToBoolean(value);
            if (propertyType == typeof(byte)) return Convert.ToByte(value);
            if (propertyType == typeof(char)) return Convert.ToChar(value);
            if (propertyType == typeof(DateTime)) return Convert.ToDateTime(value);
            if (propertyType == typeof(decimal)) return Convert.ToDecimal(value);
            if (propertyType == typeof(double)) return Convert.ToDouble(value);
            if (propertyType == typeof(Int16)) return Convert.ToInt16(value);
            if (propertyType == typeof(Int32)) return Convert.ToInt32(value);
            if (propertyType == typeof(Int64)) return Convert.ToInt64(value);
            if (propertyType == typeof(sbyte)) return Convert.ToSByte(value);
            if (propertyType == typeof(float)) return Convert.ToSingle(value);
            if (propertyType == typeof(UInt16)) return Convert.ToUInt16(value);
            if (propertyType == typeof(UInt32)) return Convert.ToUInt32(value);
            if (propertyType == typeof(UInt64)) return Convert.ToUInt64(value);
            return value;
        }
    }

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
            webInvoke.Method = RestHelper.GetHttpMethodFromOperationDescription(operationDescription);

            var formatter = base.GetReplyDispatchFormatter(operationDescription, endpoint);
            return formatter;
        }

        /// <summary>
        /// Gets the request dispatch formatter.
        /// </summary>
        /// <param name="operationDescription">The operation description.</param>
        /// <param name="endpoint">The endpoint.</param>
        /// <returns>IDispatchMessageFormatter.</returns>
        protected override IDispatchMessageFormatter GetRequestDispatchFormatter(OperationDescription operationDescription, ServiceEndpoint endpoint)
        {
            if (IsGetOperation(operationDescription))
                // no change for GET operations
                return base.GetRequestDispatchFormatter(operationDescription, endpoint);

            if (operationDescription.Messages[0].Body.Parts.Count == 0)
                // nothing in the body, still use the default
                return base.GetRequestDispatchFormatter(operationDescription, endpoint);

            return new NewtonsoftJsonDispatchFormatter(operationDescription, true);
        }

        /// <summary>
        /// Determines whether the operation is a GET operation.
        /// </summary>
        /// <param name="operation">The operation.</param>
        /// <returns><c>true</c> if [is get operation] [the specified operation]; otherwise, <c>false</c>.</returns>
        private static bool IsGetOperation(OperationDescription operation)
        {
            var wga = operation.Behaviors.Find<WebGetAttribute>();
            if (wga != null) return true;

            var wia = operation.Behaviors.Find<WebInvokeAttribute>();
            if (wia != null) return wia.Method == "HEAD";

            return false;
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
        /// <summary>
        /// Initializes a new instance of the <see cref="RestJsonHttpBehavior" /> class.
        /// </summary>
        /// <param name="rootUrl">The root URL.</param>
        /// <param name="contractType">Type of the contract.</param>
        public RestJsonHttpBehavior(string rootUrl, Type contractType)
        {
            _rootUrl = rootUrl;
            _contractType = contractType;
        }

        private readonly string _rootUrl;
        private readonly Type _contractType;

        /// <summary>Handles REST JSON formatting behavior</summary>
        /// <param name="operationDescription"></param>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        protected override IDispatchMessageFormatter GetReplyDispatchFormatter(OperationDescription operationDescription, ServiceEndpoint endpoint)
        {
            var webInvoke = operationDescription.Behaviors.OfType<WebInvokeAttribute>().FirstOrDefault();
            if (webInvoke == null)
            {
                webInvoke = new WebInvokeAttribute();
                operationDescription.Behaviors.Add(webInvoke);
            }
            webInvoke.RequestFormat = WebMessageFormat.Json;
            webInvoke.ResponseFormat = WebMessageFormat.Json;
            webInvoke.Method = RestHelper.GetHttpMethodFromOperationDescription(operationDescription);

            if (operationDescription.Messages.Count == 1 || operationDescription.Messages[1].Body.ReturnValue.Type == typeof(void))
                return base.GetReplyDispatchFormatter(operationDescription, endpoint);
            return new NewtonsoftJsonDispatchFormatter(operationDescription, false);
        }

        /// <summary>
        /// Implements the <see cref="M:System.ServiceModel.Description.IEndpointBehavior.ApplyDispatchBehavior(System.ServiceModel.Description.ServiceEndpoint,System.ServiceModel.Dispatcher.EndpointDispatcher)" /> method to support modification or extension of the client across an endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint that exposes the contract.</param>
        /// <param name="endpointDispatcher">The endpoint dispatcher to which the behavior is applied.</param>
        public override void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
        {
            base.ApplyDispatchBehavior(endpoint, endpointDispatcher);

            endpointDispatcher.DispatchRuntime.MessageInspectors.Add(new RestDispatchMessageInspector(_rootUrl, _contractType));
            endpointDispatcher.DispatchRuntime.OperationSelector = new RestJsonOperationSelector(_rootUrl, _contractType);

            //foreach (var operation in endpoint.Contract.Operations.Where(o => !o.Behaviors.Contains(typeof (RestJsonOperationInvokerBehavior))))
            //    operation.Behaviors.Add(new RestJsonOperationInvokerBehavior());
        }
    }

    ///// <summary>
    ///// This behavior allows adding a special method invoker for REST calls
    ///// </summary>
    //public class RestJsonOperationInvokerBehavior : IOperationBehavior
    //{
    //    /// <summary>
    //    /// Validates the specified operation description.
    //    /// </summary>
    //    /// <param name="operationDescription">The operation description.</param>
    //    public void Validate(OperationDescription operationDescription)
    //    {
    //    }

    //    /// <summary>
    //    /// Applies the dispatch behavior.
    //    /// </summary>
    //    /// <param name="operationDescription">The operation description.</param>
    //    /// <param name="dispatchOperation">The dispatch operation.</param>
    //    public void ApplyDispatchBehavior(OperationDescription operationDescription, DispatchOperation dispatchOperation)
    //    {
    //        dispatchOperation.Invoker = new RestJsonOperationInvoker(dispatchOperation.Invoker, operationDescription, dispatchOperation);
    //    }

    //    /// <summary>
    //    /// Applies the client behavior.
    //    /// </summary>
    //    /// <param name="operationDescription">The operation description.</param>
    //    /// <param name="clientOperation">The client operation.</param>
    //    public void ApplyClientBehavior(OperationDescription operationDescription, ClientOperation clientOperation)
    //    {
    //    }

    //    /// <summary>
    //    /// Adds the binding parameters.
    //    /// </summary>
    //    /// <param name="operationDescription">The operation description.</param>
    //    /// <param name="bindingParameters">The binding parameters.</param>
    //    public void AddBindingParameters(OperationDescription operationDescription, BindingParameterCollection bindingParameters)
    //    {
    //    }
    //}

    /// <summary>
    /// This selector can match URL parameters on JSON requests to methods on a service
    /// </summary>
    public class RestJsonOperationSelector : IDispatchOperationSelector
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RestJsonOperationSelector" /> class.
        /// </summary>
        /// <param name="rootUrl">The root URL.</param>
        /// <param name="contractType">Type of the hosted service contract.</param>
        public RestJsonOperationSelector(string rootUrl, Type contractType)
        {
            _rootUrl = rootUrl;
            _rootUrlLower = rootUrl.ToLower();
            _contractType = contractType;
        }

        private readonly Type _contractType;
        private readonly string _rootUrl;
        private readonly string _rootUrlLower;

        /// <summary>
        /// Associates a local operation with the incoming method.
        /// </summary>
        /// <param name="message">The incoming <see cref="T:System.ServiceModel.Channels.Message" /> to be associated with an operation.</param>
        /// <returns>The name of the operation to be associated with the <paramref name="message" />.</returns>
        public string SelectOperation(ref Message message)
        {
            var actionString = message.Headers.To.AbsoluteUri;
            if (actionString.ToLower().StartsWith(_rootUrlLower))
            {
                actionString = actionString.Substring(_rootUrl.Length);
                if (actionString.StartsWith("/")) actionString = actionString.Substring(1);
            }

            var httpMethod = "POST";
            if (message.Properties != null && message.Properties.ContainsKey("httpRequest"))
            {
                var httpRequestInfo = message.Properties["httpRequest"] as HttpRequestMessageProperty;
                if (httpRequestInfo != null) httpMethod = httpRequestInfo.Method.ToUpper();
            }

            var matchingMethod = RestHelper.GetMethodNameFromUrlFragmentAndContract(actionString, httpMethod, _contractType);
            return matchingMethod != null ? matchingMethod.Name : string.Empty;
        }
    }

    ///// <summary>
    ///// Special operation invoker used to funnel REST calls to the appropriate actions in the service object
    ///// </summary>
    //public class RestJsonOperationInvoker : IOperationInvoker
    //{
    //    private readonly IOperationInvoker _originalInvoker;
    //    private readonly DispatchOperation _dispatcherOperation;
    //    private readonly OperationDescription _operationDescription;
    //    private readonly MethodInfo _methodInfo;

    //    /// <summary>
    //    /// Initializes a new instance of the <see cref="RestJsonOperationInvoker"/> class.
    //    /// </summary>
    //    /// <param name="originalInvoker">The original invoker.</param>
    //    /// <param name="operationDescription">The operation description.</param>
    //    /// <param name="dispatchOperation">The dispatch operation.</param>
    //    public RestJsonOperationInvoker(IOperationInvoker originalInvoker, OperationDescription operationDescription, DispatchOperation dispatchOperation)
    //    {
    //        _originalInvoker = originalInvoker;
    //        _operationDescription = operationDescription;
    //        _methodInfo = operationDescription.SyncMethod;
    //        _dispatcherOperation = dispatchOperation;
    //    }

    //    /// <summary>
    //    /// Returns an <see cref="T:System.Array" /> of parameter objects.
    //    /// </summary>
    //    /// <returns>The parameters that are to be used as arguments to the operation.</returns>
    //    public object[] AllocateInputs()
    //    {
    //        var inputs = new object[_methodInfo.GetParameters().Length];
    //        return inputs;
    //    }

    //    /// <summary>
    //    /// Returns an object and a set of output objects from an instance and set of input objects.
    //    /// </summary>
    //    /// <param name="instance">The object to be invoked.</param>
    //    /// <param name="inputs">The inputs to the method.</param>
    //    /// <param name="outputs">The outputs from the method.</param>
    //    /// <returns>The return value.</returns>
    //    public object Invoke(object instance, object[] inputs, out object[] outputs)
    //    {
    //        outputs = new object[0]; // We are not really doing anything with outputs. The real "output" is the return value

    //        // Making sure all the input parameters are set correctly

    //        // Making the actual call
    //        var result = _methodInfo.Invoke(instance, inputs);
    //        return result;
    //    }

    //    /// <summary>
    //    /// An asynchronous implementation of the <see cref="M:System.ServiceModel.Dispatcher.IOperationInvoker.Invoke(System.Object,System.Object[],System.Object[]@)" /> method.
    //    /// </summary>
    //    /// <param name="instance">The object to be invoked.</param>
    //    /// <param name="inputs">The inputs to the method.</param>
    //    /// <param name="callback">The asynchronous callback object.</param>
    //    /// <param name="state">Associated state data.</param>
    //    /// <returns>A <see cref="T:System.IAsyncResult" /> used to complete the asynchronous call.</returns>
    //    public IAsyncResult InvokeBegin(object instance, object[] inputs, AsyncCallback callback, object state)
    //    {
    //        throw new NotImplementedException("Async service invokation is not supported through the RestJsonOperationInvoker class");
    //    }

    //    /// <summary>
    //    /// The asynchronous end method.
    //    /// </summary>
    //    /// <param name="instance">The object invoked.</param>
    //    /// <param name="outputs">The outputs from the method.</param>
    //    /// <param name="result">The <see cref="T:System.IAsyncResult" /> object.</param>
    //    /// <returns>The return value.</returns>
    //    public object InvokeEnd(object instance, out object[] outputs, IAsyncResult result)
    //    {
    //        throw new NotImplementedException("Async service invokation is not supported through the RestJsonOperationInvoker class");
    //    }

    //    /// <summary>
    //    /// Gets a value that specifies whether the <see cref="M:System.ServiceModel.Dispatcher.IOperationInvoker.Invoke(System.Object,System.Object[],System.Object[]@)" /> or <see cref="M:System.ServiceModel.Dispatcher.IOperationInvoker.InvokeBegin(System.Object,System.Object[],System.AsyncCallback,System.Object)" /> method is called by the dispatcher.
    //    /// </summary>
    //    /// <value><c>true</c> if this instance is synchronous; otherwise, <c>false</c>.</value>
    //    public bool IsSynchronous
    //    {
    //        get { return true; }
    //    }
    //}

    /// <summary>
    /// Message inspector for REST messages
    /// </summary>
    public class RestDispatchMessageInspector : IDispatchMessageInspector
    {
        private readonly string _rootUrl;
        private readonly string _rootUrlLower;
        private readonly Type _contractType;

        /// <summary>
        /// Initializes a new instance of the <see cref="RestDispatchMessageInspector" /> class.
        /// </summary>
        /// <param name="rootUrl">The root URL.</param>
        /// <param name="contractType">Type of the contract.</param>
        public RestDispatchMessageInspector(string rootUrl, Type contractType)
        {
            _rootUrl = rootUrl;
            _rootUrlLower = rootUrl.ToLower();
            _contractType = contractType;
        }

        /// <summary>
        /// Called after an inbound message has been received but before the message is dispatched to the intended operation.
        /// </summary>
        /// <param name="request">The request message.</param>
        /// <param name="channel">The incoming channel.</param>
        /// <param name="instanceContext">The current service instance.</param>
        /// <returns>The object used to correlate state. This object is passed back in the <see cref="M:System.ServiceModel.Dispatcher.IDispatchMessageInspector.BeforeSendReply(System.ServiceModel.Channels.Message@,System.Object)" /> method.</returns>
        public object AfterReceiveRequest(ref Message request, IClientChannel channel, InstanceContext instanceContext)
        {
            if (!request.Properties.ContainsKey("Via")) return request; // Nothing much we can do here
            if (!request.Properties.ContainsKey("httpRequest")) return request; // Same here

            var httpRequest = request.Properties["httpRequest"] as HttpRequestMessageProperty;
            if (httpRequest == null) return request;
            var httpMethod = httpRequest.Method.ToUpper();

            var uri = request.Properties["Via"] as Uri;
            if (uri == null) return request; // Still nothing much we can do
            var url = uri.AbsoluteUri;
            var urlFragment = url;
            if (urlFragment.ToLower().StartsWith(_rootUrlLower)) urlFragment = urlFragment.Substring(_rootUrlLower.Length);
            var operationInfo = RestHelper.GetMethodNameFromUrlFragmentAndContract(urlFragment, httpMethod, _contractType);
            var urlParameters = RestHelper.GetUrlParametersFromUrlFragmentAndContract(urlFragment, httpMethod, _contractType);

            if (httpMethod == "GET")
            {
                // TODO: Support GET if at all possible
                throw new Exception("REST-GET operations are not currently supported in the chosen hosting environment. Please use a different HTTP Verb, or host in a different environment (such as WebApi). We hope to add this feature in a future version.");

                // This is a REST GET operation. Therefore, there is no posted message. Instead, we have to decode the input parameters from the URL
                var parameters = operationInfo.GetParameters();
                if (parameters.Length != 1) throw new NotSupportedException("Only service methods/operations with a single input parameter can be mapped to REST-GET operations. Method " + operationInfo.Name + " has " + parameters.Length + " parameters. Consider changing the method to have a single object with multiple properties instead.");
                var parameterType = parameters[0].ParameterType;
                var parameterInstance = Activator.CreateInstance(parameterType);
                foreach (var propertyName in urlParameters.Keys)
                {
                    var urlProperty = parameterType.GetProperty(propertyName);
                    if (urlProperty == null) continue;
                    urlProperty.SetValue(parameterInstance, urlParameters[propertyName], null);
                }

                // Seralize the object back into a new request message
                // TODO: We only need to do this for methods OTHER than GET
                var format = GetMessageContentFormat(request);
                switch (format)
                {
                    case WebContentFormat.Xml:
                        var xmlStream = new MemoryStream();
                        var xmlSerializer = new DataContractSerializer(parameterInstance.GetType());
                        xmlSerializer.WriteObject(xmlStream, parameterInstance);
                        var xmlReader = XmlDictionaryReader.CreateTextReader(StreamHelper.ToArray(xmlStream), XmlDictionaryReaderQuotas.Max);
                        var newXmlMessage = Message.CreateMessage(xmlReader, int.MaxValue, request.Version);
                        newXmlMessage.Properties.CopyProperties(request.Properties);
                        newXmlMessage.Headers.CopyHeadersFrom(request.Headers);
                        if (format == WebContentFormat.Default)
                        {
                            if (newXmlMessage.Properties.ContainsKey(WebBodyFormatMessageProperty.Name)) newXmlMessage.Properties.Remove(WebBodyFormatMessageProperty.Name);
                            newXmlMessage.Properties.Add(WebBodyFormatMessageProperty.Name, WebContentFormat.Xml);
                        }
                        request = newXmlMessage;
                        break;
                    case WebContentFormat.Default:
                    case WebContentFormat.Json:
                        var jsonStream = new MemoryStream();
                        var serializer = new DataContractJsonSerializer(parameterInstance.GetType());
                        serializer.WriteObject(jsonStream, parameterInstance);
                        var jsonReader = JsonReaderWriterFactory.CreateJsonReader(StreamHelper.ToArray(jsonStream), XmlDictionaryReaderQuotas.Max);
                        var newMessage = Message.CreateMessage(jsonReader, int.MaxValue, request.Version);
                        newMessage.Properties.CopyProperties(request.Properties);
                        newMessage.Headers.CopyHeadersFrom(request.Headers);
                        if (format == WebContentFormat.Default)
                        {
                            if (newMessage.Properties.ContainsKey(WebBodyFormatMessageProperty.Name)) newMessage.Properties.Remove(WebBodyFormatMessageProperty.Name);
                            newMessage.Properties.Add(WebBodyFormatMessageProperty.Name, WebContentFormat.Json);
                        }
                        request = newMessage;
                        break;
                    default:
                        throw new NotSupportedException("Mesage format " + format.ToString() + " is not supported form REST/JSON operations");
                }
            }

            return null;
        }

        private static WebContentFormat GetMessageContentFormat(Message message)
        {
            if (message.Properties.ContainsKey(WebBodyFormatMessageProperty.Name))
            {
                var bodyFormat = message.Properties[WebBodyFormatMessageProperty.Name] as WebBodyFormatMessageProperty;
                if (bodyFormat != null) return bodyFormat.Format;
            }
            return WebContentFormat.Default;
        }

        /// <summary>
        /// Called after the operation has returned but before the reply message is sent.
        /// </summary>
        /// <param name="reply">The reply message. This value is null if the operation is one way.</param>
        /// <param name="correlationState">The correlation object returned from the <see cref="M:System.ServiceModel.Dispatcher.IDispatchMessageInspector.AfterReceiveRequest(System.ServiceModel.Channels.Message@,System.ServiceModel.IClientChannel,System.ServiceModel.InstanceContext)" /> method.</param>
        public void BeforeSendReply(ref Message reply, object correlationState)
        {
        }
    }
}

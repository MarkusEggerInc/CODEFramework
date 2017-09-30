using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Controllers;
using CODE.Framework.Core.Configuration;
using CODE.Framework.Core.Utilities;
using CODE.Framework.Core.Utilities.Extensions;

namespace CODE.Framework.Services.Server.WebApi
{
    /// <summary>
    /// WebApi controller class used to host CODE Framework services
    /// </summary>
    /// <typeparam name="TServiceImplementation">The implementation type of the service.</typeparam>
    public class ServiceHostController<TServiceImplementation> : ApiController where TServiceImplementation : new()
    {
        private readonly TServiceImplementation _host;
        private Type _contractType;
        private string _allowableCorsOrigin;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceHostController{TServiceImplementation}"/> class.
        /// </summary>
        public ServiceHostController()
        {
            _host = new TServiceImplementation();
            AllowableCorsMethods = "GET, PUT, DELETE, POST, HEAD, OPTIONS";
            AllowableCorsOrigin = "*";
            AllowableCorsHeaders = "Content-Type, Accept";

            if (JsonPropertyCaseMode == JsonPropertyCaseMode.Default)
            {
                if (ConfigurationSettings.Settings.IsSettingSupported("JsonPropertyCaseMode"))
                {
                    var jsonPropertyCaseMode = ConfigurationSettings.Settings["JsonPropertyCaseMode"].ToLower();
                    if (jsonPropertyCaseMode == "forcecamelcase" || jsonPropertyCaseMode == "camelcase")
                        JsonPropertyCaseMode = JsonPropertyCaseMode.ForceCamelCase;
                    else
                        JsonPropertyCaseMode = JsonPropertyCaseMode.UseOriginalPropertyNames;
                }
            }
        }

        /// <summary>
        /// Returns the first contract (interface) implemented by TServiceImplementation.
        /// Override this method to specify a different interface.
        /// </summary>
        /// <returns>The interface to be exposed by the ApiController.</returns>
        /// <exception cref="System.NotSupportedException">The hosted service contract must implement a service interface</exception>
        protected virtual Type GetContractInterface()
        {
            return null;
        }

        /// <summary>
        /// Defines whether the controller automatically handles CORS. True by default.
        /// </summary>
        /// <value><c>true</c> if CORS is enabled, otherwise, <c>false</c>.</value>
        public bool EnableCors { get; set; }

        /// <summary>
        /// Defines origins CORS accepts calls from. Default is * (all). Can be a comma-separated list of origins.
        /// </summary>
        /// <value>The allowable CORS origin.</value>
        /// <example>"*" = all. "codemag.com" = only calls from codemag.com. "codemag.com, microsoft.com" = only calls from those two domains.</example>
        public string AllowableCorsOrigin
        {
            get { return _allowableCorsOrigin; }
            set
            {
                _allowableCorsOrigin = value;
                EnableCors = !string.IsNullOrEmpty(value);
            }
        }

        /// <summary>
        /// Methods supported by CORS (PUT, GET, DELETE, POST, HEAD, OPTIONS,...)
        /// </summary>
        /// <value>The allowable CORS methods.</value>
        public string AllowableCorsMethods { get; set; }

        /// <summary>
        /// Headers supported by CORS
        /// </summary>
        /// <value>The allowable CORS headers.</value>
        public string AllowableCorsHeaders { get; set; }

        /// <summary>
        /// Defines whether HTTPS is required
        /// </summary>
        /// <value>
        /// The HTTPS mode.
        /// </value>
        public ControllerHttpsMode HttpsMode { get; set; }

        /// <summary>
        /// Defines the case used for JSON property names
        /// </summary>
        /// <value>
        /// The JSON property case mode.
        /// </value>
        public JsonPropertyCaseMode JsonPropertyCaseMode { get; set; }

        /// <summary>
        /// execute as an asynchronous operation.
        /// </summary>
        /// <param name="controllerContext">The controller context for a single HTTP operation.</param>
        /// <param name="cancellationToken">The cancellation token assigned for the HTTP operation.</param>
        /// <returns>The newly started task.</returns>
        public override async Task<HttpResponseMessage> ExecuteAsync(HttpControllerContext controllerContext, CancellationToken cancellationToken)
        {
            var lowerFullCalledUri = controllerContext.Request.RequestUri.AbsoluteUri.ToLower();
            if (HttpsMode == ControllerHttpsMode.RequireHttps && !lowerFullCalledUri.StartsWith("https://")) throw new AccessViolationException("Service must be called over a secure connection (HTTPS)");
            if (HttpsMode == ControllerHttpsMode.RequireHttpsExceptLocalhost && !lowerFullCalledUri.StartsWith("https://") && controllerContext.Request.RequestUri.Host.ToLower() != "localhost") throw new AccessViolationException("Service must be called over a secure connection (HTTPS)");

            var fullUrl = controllerContext.Request.RequestUri.PathAndQuery;
            var applicationPath = controllerContext.Request.GetRequestContext().VirtualPathRoot;
            fullUrl = fullUrl.Substring(applicationPath.Length);

            var routeTemplate = controllerContext.RouteData.Route.RouteTemplate;
            fullUrl = RemoveMeaninglessUrlSegments(fullUrl, routeTemplate);
            routeTemplate = RemoveMeaninglessUrlSegments(routeTemplate, routeTemplate);
            var routeTemplateLower = routeTemplate.ToLower();

            // First, we figure out where the controller information is in the URL, which we can then discard, since the controller has already been invoked (otherwise, we wouldn't have gotten here)
            const string controllerPatternString = "{controller}";
            var controllerStart = routeTemplateLower.IndexOf(controllerPatternString, StringComparison.Ordinal);
            if (controllerStart == -1) throw new NotSupportedException("Can only handle host controllers accessed through a route template that includes a {controller} definition.");
            var controllerEnd = controllerStart + controllerPatternString.Length;
            var routeTemplateUpToController = routeTemplateLower.Substring(0, controllerEnd);
            var slashCountUpToController = routeTemplateUpToController.Occurs("/");
            var firstCharAfterController = routeTemplate.Substring(controllerEnd, 1);

            // We get the URL Fragment with everything before the controller removed
            string urlFragmentUpToController;
            if (slashCountUpToController > 0)
            {
                var currentFragment = fullUrl;
                var positionOfLastSlash = 0;
                for (var slashCounter = 0; slashCounter < slashCountUpToController; slashCounter++)
                {
                    var currentIndex = currentFragment.IndexOf('/');
                    if (currentIndex < 0) throw new NotSupportedException("Invalid URL pattern detected. Can't process current request");
                    positionOfLastSlash += currentIndex;
                    currentFragment = currentFragment.Substring(currentIndex + 1);
                }
                urlFragmentUpToController = fullUrl.Substring(positionOfLastSlash + 1);
            }
            else
                urlFragmentUpToController = fullUrl;

            // We find the first special character after the controller, which is the start of the parameters
            var firstCharAfterControllerIndex = urlFragmentUpToController.IndexOf(firstCharAfterController, StringComparison.Ordinal);
            var urlFragment = firstCharAfterControllerIndex < 0 ? string.Empty : urlFragmentUpToController.Substring(firstCharAfterControllerIndex);

            var httpMethod = controllerContext.Request.Method.Method.ToUpper();

            CheckContractInterface();

            if (httpMethod == "OPTIONS" && EnableCors)
            {
                var response = new HttpResponseMessage {Content = new StringContent(string.Empty)};
                HandleCors(controllerContext, response);
                return response;
            }

            var method = RestHelper.GetMethodNameFromUrlFragmentAndContract(urlFragment, httpMethod, _contractType);
            if (method == null) throw new NotSupportedException("Refusing request: " + fullUrl);

            if (httpMethod == "GET" || httpMethod == "HEAD" || httpMethod == "TRACE" || httpMethod == "DELETE" || httpMethod == "CONNECT" || httpMethod == "MKCOL" || httpMethod == "COPY" || httpMethod == "MOVE" || httpMethod == "UNLOCK")
            {
                // These verbs/http-methods do NOT have a body that is posted
                var urlParameters = RestHelper.GetUrlParametersFromUrlFragmentAndContract(urlFragment, httpMethod, _contractType);
                var parameters = method.GetParameters();
                if (parameters.Length != 1) throw new NotSupportedException("Only service methods/operations with a single input parameter can be mapped to REST-GET operations. Method " + method.Name + " has " + parameters.Length + " parameters. Consider changing the method to have a single object with multiple properties instead.");
                var parameterType = parameters[0].ParameterType;
                var parameterObject = Activator.CreateInstance(parameterType);
                foreach (var propertyName in urlParameters.Keys)
                {
                    var urlProperty = parameterType.GetProperty(propertyName);
                    if (urlProperty == null) continue;
                    urlProperty.SetValue(parameterObject, urlParameters[propertyName], null);
                }
                BeforeInvokeMethod(controllerContext, method.Name, httpMethod, method, parameterObject);
                object result;
                try
                {
                    result = method.Invoke(_host, new[] {parameterObject});
                }
                catch
                {
                    var errorResponse = new HttpResponseMessage(HttpStatusCode.NotAcceptable) {ReasonPhrase = "Error invoking service operation."};
                    HandleCors(controllerContext, errorResponse);
                    return errorResponse;
                }
                BeforeCreatingResponse(result, controllerContext, method.Name, httpMethod, method);
                var json = JsonHelper.SerializeToRestJson(result, JsonPropertyCaseMode == JsonPropertyCaseMode.ForceCamelCase);
                var response = new HttpResponseMessage {Content = new StringContent(json, Encoding.UTF8, "application/json")};
                BeforeReturningResponse(response, json, controllerContext, method.Name, httpMethod, method, result);

                HandleCors(controllerContext, response);

                return response;
            }
            else
            {
                var parameters = method.GetParameters();
                if (parameters.Length != 1) throw new NotSupportedException("Only service methods/operations with a single input parameter can be mapped to REST-GET operations. Method " + method.Name + " has " + parameters.Length + " parameters. Consider changing the method to have a single object with multiple properties instead.");
                var parameterType = parameters[0].ParameterType;
                var formatter = new JsonMediaTypeFormatter();
                var stream = await controllerContext.Request.Content.ReadAsStreamAsync();
                object parameterObject;
                try
                {
                    parameterObject = await formatter.ReadFromStreamAsync(parameterType, stream, controllerContext.Request.Content, null);
                }
                catch
                {
                    var errorResponse = new HttpResponseMessage(HttpStatusCode.NotAcceptable) {ReasonPhrase = "Error invoking service operation: Invalid input parameter."};
                    HandleCors(controllerContext, errorResponse);
                    return errorResponse;
                }
                BeforeInvokeMethod(controllerContext, method.Name, httpMethod, method, parameterObject);
                object result;
                try
                {
                    result = method.Invoke(_host, new[] {parameterObject});
                }
                catch
                {
                    var errorResponse = new HttpResponseMessage(HttpStatusCode.NotAcceptable) {ReasonPhrase = "Error invoking service operation."};
                    HandleCors(controllerContext, errorResponse);
                    return errorResponse;
                }
                BeforeCreatingResponse(result, controllerContext, method.Name, httpMethod, method);
                var json = JsonHelper.SerializeToRestJson(result, JsonPropertyCaseMode == JsonPropertyCaseMode.ForceCamelCase);
                var response = new HttpResponseMessage {Content = new StringContent(json, Encoding.UTF8, "application/json")};
                BeforeReturningResponse(response, json, controllerContext, method.Name, httpMethod, method, result);

                HandleCors(controllerContext, response);

                return response;
            }
        }

        private string RemoveMeaninglessUrlSegments(string fullUrl, string routeTemplate)
        {
            var allRouteChars = routeTemplate.ToCharArray();
            var sb = new StringBuilder();
            var inPattern = false;

            for (var charCounter = 0; charCounter < allRouteChars.Length; charCounter++)
            {
                var currentChar = allRouteChars[charCounter];
                if (currentChar == '{')
                {
                    inPattern = true;
                    continue;
                }
                if (currentChar == '}')
                {
                    inPattern = false;
                    if (allRouteChars.Length > charCounter + 1)
                    {
                        var nextChar = allRouteChars[charCounter + 1];
                        var currentIndex = fullUrl.IndexOf(nextChar);
                        if (currentIndex < 0)
                        {
                            // We reached the end of the URL, even though the pattern could have been longer
                            sb.Append(fullUrl);
                            break;
                        };
                        var currentSegment = fullUrl.Substring(0, currentIndex);
                        fullUrl = fullUrl.Substring(currentIndex);
                        sb.Append(currentSegment);
                    }
                    else if (!string.IsNullOrEmpty(fullUrl))
                    {
                        sb.Append(fullUrl);
                        break;
                    }
                    continue;
                }
                if (!inPattern && currentChar == '/')
                {
                    var currentIndex = fullUrl.IndexOf('/');
                    if (currentIndex < 0) throw new NotSupportedException("Invalid URL pattern detected. Can't process current request");
                    var currentSegment = fullUrl.Substring(0, currentIndex + 1);
                    fullUrl = fullUrl.Substring(currentIndex + 1);
                    sb.Append(currentSegment);
                }
                else if (!inPattern)
                {
                    // This char is meaningless to us, so we trim it
                    fullUrl = fullUrl.Substring(1);
                }
            }

            var resultingUrl = sb.ToString();

            while (resultingUrl.Contains("//"))
                resultingUrl = resultingUrl.Replace("//", "/");

            return resultingUrl;
        }

        private void HandleCors(HttpControllerContext controllerContext, HttpResponseMessage response)
        {
            if (!EnableCors) return;

            IEnumerable<string> originEnumerable;
            if (controllerContext.Request.Headers.TryGetValues("Origin", out originEnumerable))
            {
                var origin = originEnumerable.FirstOrDefault() ?? string.Empty;
                if (string.IsNullOrEmpty(origin)) return;

                var isOriginMatch = AllowableCorsOrigin == "*";
                if (!isOriginMatch)
                {
                    var allowableOriginsList = AllowableCorsOrigin.Replace(';', ',').Split(',');
                    foreach (var allowableOrigin in allowableOriginsList)
                        if (string.Equals(origin, allowableOrigin, StringComparison.InvariantCultureIgnoreCase))
                        {
                            isOriginMatch = true;
                            break;
                        }
                }

                if (isOriginMatch)
                {
                    response.Headers.Add("Access-Control-Allow-Origin", origin);
                    response.Headers.Add("Access-Control-Allow-Credentials", "true");
                    if (controllerContext.Request.Method == HttpMethod.Options)
                    {
                        response.Headers.Add("Access-Control-Allow-Methods", AllowableCorsMethods);
                        response.Headers.Add("Access-Control-Allow-Headers", AllowableCorsHeaders);
                    }
                }
            }
            else if (AllowableCorsOrigin == "*")
            {
                response.Headers.Add("Access-Control-Allow-Origin", "*");
                response.Headers.Add("Access-Control-Allow-Credentials", "true");
                if (controllerContext.Request.Method == HttpMethod.Options)
                {
                    response.Headers.Add("Access-Control-Allow-Methods", AllowableCorsMethods);
                    response.Headers.Add("Access-Control-Allow-Headers", AllowableCorsHeaders);
                }
            }
        }

        /// <summary>
        /// Fires before the controller returns the result to the client. (Designed to be overridden in subclasses)
        /// </summary>
        /// <param name="response">The HTTP response.</param>
        /// <param name="json">The json payload.</param>
        /// <param name="controllerContext">The controller context.</param>
        /// <param name="methodName">Name of the method that was invoked to create the response.</param>
        /// <param name="httpVerb">HTTP verb used to call the method (Get, Put,...)</param>
        /// <param name="methodInfo">Detailed information about the method that is about to be invoked</param>
        /// <param name="originalResult">The original result as provided by the service.</param>
        protected virtual void BeforeReturningResponse(HttpResponseMessage response, string json, HttpControllerContext controllerContext, string methodName, string httpVerb, MethodInfo methodInfo, object originalResult)
        {
        }

        /// <summary>
        /// Fires before the controller method is invoked. (Designed to be overridden in subclasses)
        /// </summary>
        /// <param name="controllerContext">The controller context.</param>
        /// <param name="methodName">Name of the method to invoke</param>
        /// <param name="httpVerb">HTTP verb used to call the method (Get, Put,...)</param>
        /// <param name="methodInfo">Detailed information about the method that is about to be invoked</param>
        /// <param name="inputContract">Input contract object. ("The parameter object for the method call")</param>
        protected virtual void BeforeInvokeMethod(HttpControllerContext controllerContext, string methodName, string httpVerb, MethodInfo methodInfo, object inputContract)
        {

        }

        /// <summary>
        /// Fires before the result object is serialized into a response and before the response context object is created. (Designed to be overridden in subclasses)
        /// </summary>
        /// <param name="result">The result object before it is serialized into a JSON response. Can be used to manipulate the result.</param>
        /// <param name="controllerContext">The controller context.</param>
        /// <param name="methodName">Name of the method that was invoked to create the response.</param>
        /// <param name="httpVerb">HTTP verb used to call the method (Get, Put,...)</param>
        /// <param name="methodInfo">Detailed information about the method that has been invoked to retrieve the result.</param>
        protected virtual void BeforeCreatingResponse(object result, HttpControllerContext controllerContext, string methodName, string httpVerb, MethodInfo methodInfo)
        {
        }

        private void CheckContractInterface()
        {
            if (_contractType != null) return;

            _contractType = GetContractInterface();
            if (_contractType != null) return;

            // No explicitly defined contract interface found. Therefore, we try to use one implicitly
            var interfaces = _host.GetType().GetInterfaces();
            if (interfaces.Length != 1) throw new NotSupportedException("The hosted service contract must implement a service interface");
            _contractType = interfaces[0];
        }

        /// <summary>
        /// Provides the caller's IP address
        /// </summary>
        /// <value>The user host address.</value>
        public virtual string UserHostAddress 
        {
            get { return HttpContext.Current.Request.UserHostAddress; } 
        }

        /// <summary>
        /// Provides the caller's DNS name
        /// </summary>
        /// <value>The name of the user host.</value>
        public virtual string UserHostName
        {
            get { return HttpContext.Current.Request.UserHostName; }
        }
    }

    /// <summary>
    /// HTTPS mode of the controller (can be used to define whether the controller requires HTTPS)
    /// </summary>
    public enum ControllerHttpsMode
    {
        /// <summary>Controller can be called over HTTP or HTTPS without restriction</summary>
        Undefined,
        /// <summary>Always requires HTTPS</summary>
        RequireHttps,
        /// <summary>Requires HTTPS, but not if hosted on Localhost</summary>
        RequireHttpsExceptLocalhost
    }

    /// <summary>
    /// Defines which casing is to be used for JSON property names
    /// </summary>
    public enum JsonPropertyCaseMode
    {
        /// <summary>
        /// Default setting (respects settings in configuration)
        /// </summary>
        Default,
        /// <summary>
        /// Always uses the exact property names as defined by the objects
        /// </summary>
        UseOriginalPropertyNames,
        /// <summary>
        /// Forces camelCase for all property names
        /// </summary>
        ForceCamelCase
    }
}

using System;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Controllers;
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
        private readonly Type _contractType;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceHostController{TServiceImplementation}"/> class.
        /// </summary>
        /// <exception cref="System.NotSupportedException">The hosted service contract must implement a service interface</exception>
        public ServiceHostController()
        {
            _host = new TServiceImplementation();

            var interfaces = _host.GetType().GetInterfaces();
            if (interfaces.Length != 1) throw new NotSupportedException("The hosted service contract must implement a service interface");
            _contractType = interfaces[0];
        }

        /// <summary>
        /// execute as an asynchronous operation.
        /// </summary>
        /// <param name="controllerContext">The controller context for a single HTTP operation.</param>
        /// <param name="cancellationToken">The cancellation token assigned for the HTTP operation.</param>
        /// <returns>The newly started task.</returns>
        public override async Task<HttpResponseMessage> ExecuteAsync(HttpControllerContext controllerContext, CancellationToken cancellationToken)
        {
            var fullUrl = controllerContext.Request.RequestUri.PathAndQuery;

            var routeTemplate = controllerContext.RouteData.Route.RouteTemplate;
            var routeTemplateLower = routeTemplate.ToLower();
            var controllerEnd = routeTemplateLower.IndexOf("{controller}", StringComparison.Ordinal);
            if (controllerEnd == -1) throw new NotSupportedException("Can only handle host controllers accessed through a route template that includes a {controller} definition.");
            controllerEnd += 13;
            var routeTemplateFragment = routeTemplate.Substring(0, controllerEnd);
            if (!routeTemplateFragment.EndsWith("/")) routeTemplateFragment = routeTemplateFragment.Substring(0, routeTemplateFragment.Length - 1);
            var slashCount = routeTemplateFragment.Occurs("/");
            if (fullUrl.StartsWith("/") && !routeTemplateFragment.StartsWith("/")) fullUrl = fullUrl.Substring(1);
            var urlFragmentStartPosition = fullUrl.At("/", slashCount);
            var urlFragment = string.Empty;
            if (urlFragmentStartPosition > 0) urlFragment = fullUrl.Substring(urlFragmentStartPosition);

            var httpMethod = controllerContext.Request.Method.Method.ToUpper();

            var method = RestHelper.GetMethodNameFromUrlFragmentAndContract(urlFragment, httpMethod, _contractType);
            if (method == null) throw new NotSupportedException("Refusing request");

            if (httpMethod == "GET")
            {
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
                var result = method.Invoke(_host, new[] {parameterObject});
                var response = new HttpResponseMessage {Content = new ObjectContent(result.GetType(), result, new JsonMediaTypeFormatter(), "application/json")};
                return response;
            }
            else
            {
                var parameters = method.GetParameters();
                if (parameters.Length != 1) throw new NotSupportedException("Only service methods/operations with a single input parameter can be mapped to REST-GET operations. Method " + method.Name + " has " + parameters.Length + " parameters. Consider changing the method to have a single object with multiple properties instead.");
                var parameterType = parameters[0].ParameterType;
                var formatter = new JsonMediaTypeFormatter();
                var stream = await controllerContext.Request.Content.ReadAsStreamAsync();
                var parameterObject = await formatter.ReadFromStreamAsync(parameterType, stream, controllerContext.Request.Content, null);
                var result = method.Invoke(_host, new[] {parameterObject});
                var response = new HttpResponseMessage {Content = new ObjectContent(result.GetType(), result, new JsonMediaTypeFormatter(), "application/json")};
                return response;
            }
        }
    }
}

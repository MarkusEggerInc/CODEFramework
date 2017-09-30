using CODE.Framework.Wpf.Interfaces;
using CODE.Framework.Wpf.Layout;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;

namespace CODE.Framework.Wpf.Mvvm
{
    /// <summary>
    /// Base class for controllers
    /// </summary>
    public class Controller
    {
        /// <summary>
        /// Static constructor
        /// </summary>
        static Controller()
        {
            RegisteredViewEngines = new List<IViewEngine>
            {
                new LooseXamlViewEngine(),
                new StandardViewEngine(),
                new CompiledXamlViewEngine()
            };

            RegisterViewHandler(new WindowShellLauncher<Shell>(), "ShellLauncher");
        }

        private static Dictionary<string, IViewHandler> _viewHandlers;

        /// <summary>
        /// Registers a view handler
        /// </summary>
        /// <param name="handler">Handler to register</param>
        /// <param name="viewHandlerId">Optional ID, so the handler can later be re-identified if needed</param>
        /// <param name="replaceHandlersWithMatchingId">If true (default) then current registered handlers with identical ID will be replaced</param>
        /// <remarks>Document handlers are objects that are given a chance to handle views when they need launching.</remarks>
        public static void RegisterViewHandler(IViewHandler handler, string viewHandlerId = "", bool replaceHandlersWithMatchingId = true)
        {
            if (string.IsNullOrEmpty(viewHandlerId)) viewHandlerId = Guid.NewGuid().ToString();
            if (_viewHandlers == null) _viewHandlers = new Dictionary<string, IViewHandler>();
            if (_viewHandlers.ContainsKey(viewHandlerId))
            {
                if (replaceHandlersWithMatchingId)
                    _viewHandlers[viewHandlerId] = handler;
            }
            else
                _viewHandlers.Add(viewHandlerId, handler);
        }

        /// <summary>
        /// Returns the shell launcher view handler (if one is registered)
        /// </summary>
        /// <returns>IViewHandler.</returns>
        public static IViewHandler GetShellLauncher()
        {
            return _viewHandlers.ContainsKey("ShellLauncher") ? _viewHandlers["ShellLauncher"] : null;
        }

        /// <summary>
        /// Returns a list of all registered view handlers
        /// </summary>
        /// <returns>List of view handlers</returns>
        public static List<IViewHandler> GetRegisteredViewHandlers()
        {
            return _viewHandlers.Values.ToList();
        }

        /// <summary>
        /// Unregisters a previously registered view handler by ID
        /// </summary>
        /// <param name="viewHandlerId">Document handler ID</param>
        /// <returns>True if successful</returns>
        public static bool UnregisterViewHandler(string viewHandlerId)
        {
            if (_viewHandlers.ContainsKey(viewHandlerId))
            {
                _viewHandlers.Remove(viewHandlerId);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Unregisters all registered view handlers
        /// </summary>
        public static void ClearViewHandlers()
        {
            _viewHandlers.Clear();
        }

        /// <summary>
        /// Triggers an action on the specified controller
        /// </summary>
        /// <param name="controller">Controller the action is to be triggered on</param>
        /// <param name="action">Action to be called on the controller</param>
        /// <param name="routeValues">Additional parameters passed to the controller</param>
        /// <param name="eventSinks">Object containing the event handlers that are to be attached to the view result (if the result is in fact a view result)</param>
        /// <param name="executeViewHandlers">Defines whether registered view-handlers are to immediately execute and thus try to show the new result</param>
        /// <returns>Request context (can be ignored except for special cases)</returns>
        /// <example>
        /// Controller.Action("Customer", "List"); // Invokes CustomerController.List
        /// var context = Controller.Action("Customer", "List"); // Invokes CustomerController.List and retrieves the context
        /// Controller.Action("Customer", "Detail", new {id = x}); // Invokes CustomerController.Detail(id) and passes a parameter called "id")
        /// </example>
        public static RequestContext Action(string controller = "Home", string action = "Index", dynamic routeValues = null, ViewResultEventSinks eventSinks = null, bool executeViewHandlers = true)
        {
            if (!controller.EndsWith("Controller")) controller += "Controller";

            if (_controllers == null) PopulateControllers();

            var controllerKey = controller.ToLower();
            if (_controllers != null && _controllers.ContainsKey(controllerKey))
            {
                var context = new RequestContext(new RouteData(action, routeValues));
                _controllers[controllerKey].Instance.Execute(context);

                var viewResult = context.Result as ViewResult;
                if (viewResult != null)
                {
                    if (eventSinks != null)
                    {
                        viewResult.ViewClosed += eventSinks.ViewClosed;
                        viewResult.ViewOpened += eventSinks.ViewOpened;
                    }

                    viewResult.ViewOpened += (o, e) => AfterViewOpened(viewResult);

                    var openable = viewResult.Model as IOpenable;
                    if (openable != null)
                        openable.RaiseOpeningEvent();
                }

                if (executeViewHandlers) ExecuteViewHandlers(context);

                if (viewResult != null)
                {
                    var openable = viewResult.Model as IOpenable;
                    if (openable != null)
                        openable.RaiseOpenedEvent();
                }

                return context;
            }
            return null;
        }

        private static void AfterViewOpened(ViewResult viewResult)
        {
            // This method executes all scheduled actions after a view has been opened, and removes the action from the stack.
            // This method is used internally by the controller to perform actions that need to be taken after the view has been completely loaded.
            while (AfterNextViewOpenActions.Count > 0)
            {
                var action = AfterNextViewOpenActions.Pop();
                action(viewResult);
            }
        }

        /// <summary>Schedules an action to be execute after the next view-open operation has completed</summary>
        /// <param name="action">Action to be executed</param>
        public static void ExecuteAfterNextViewOpen(Action<ViewResult> action)
        {
            AfterNextViewOpenActions.Push(action);
        }

        private static readonly Stack<Action<ViewResult>> AfterNextViewOpenActions = new Stack<Action<ViewResult>>();

        /// <summary>
        /// Executes all the registered view handlers based on the current context
        /// </summary>
        /// <param name="context"></param>
        private static void ExecuteViewHandlers(RequestContext context)
        {
            if (_viewHandlers != null)
            {
                var currentHandlers = _viewHandlers.Values.ToList(); // We get a new list of handlers, because it could actually happen that calling Openview() on a handler registers handlers and thus change the _viewHandlers collection
                foreach (var handler in currentHandlers)
                    handler.OpenView(context);
                var viewResult = context.Result as ViewResult;
                if (viewResult != null)
                    viewResult.RaiseViewOpened();
            }
        }

        /// <summary>Finds a view within the context of the specified controller</summary>
        /// <param name="controller">Controller the action is to be triggered on</param>
        /// <param name="view">Name of the view that is to be found</param>
        /// <returns>Request context (can be ignored except for special cases)</returns>
        public static RequestContext ViewOnly(string controller = "Home", string view = "Index")
        {
            if (!controller.EndsWith("Controller")) controller += "Controller";

            if (_controllers == null) PopulateControllers();

            var controllerKey = controller.ToLower();
            if (_controllers != null && _controllers.ContainsKey(controllerKey))
            {
                var context = new RequestContext(new RouteData(view, null))
                {
                    Result = _controllers[controllerKey].Instance.PartialView(view)
                };

                if (_viewHandlers != null)
                {
                    var currentHandlers = _viewHandlers.Values.ToList(); // We get a new list of handlers, because it could actually happen that calling Openview() on a handler registers handlers and thus change the _viewHandlers collection
                    foreach (var handler in currentHandlers)
                        handler.OpenView(context);

                    var viewResult = context.Result as ViewResult;
                    if (viewResult != null)
                        viewResult.RaiseViewOpened();
                }

                return context;
            }
            return null;
        }

        /// <summary>Closes the view associated with the provided model.</summary>
        /// <param name="model">Model associated with the view that is to be closed</param>
        /// <remarks>This only works if the model is only used with a single view. This will not work (or work unpredictably) for models that are shared across views.</remarks>
        public static void CloseViewForModel(object model)
        {
            if (_viewHandlers == null) return;

            var handlers = new IViewHandler[_viewHandlers.Values.Count];
            _viewHandlers.Values.CopyTo(handlers, 0);
            foreach (var handler in handlers)
                handler.CloseViewForModel(model);
        }

        /// <summary>Activates (shows) the view associated with the provided model.</summary>
        /// <param name="model">Model associated with the view that is to be activate</param>
        /// <remarks>This only works if the model is only used with a single view. This will not work (or work unpredictably) for models that are shared across views.</remarks>
        public static void ActivateViewForModel(object model)
        {
            if (_viewHandlers == null) return;

            var handlers = new IViewHandler[_viewHandlers.Values.Count];
            _viewHandlers.Values.CopyTo(handlers, 0);
            foreach (var handler in handlers)
                handler.ActivateViewForModel(model);
        }

        /// <summary>
        /// Returns the first instance of a model instance of the specified type (or null, if none is instantiated)
        /// </summary>
        /// <typeparam name="TModel">The type of the model.</typeparam>
        /// <returns>Model, or null</returns>
        public static TModel GetOpenModel<TModel>() where TModel : class
        {
            if (_viewHandlers == null) return default(TModel);

            var handlers = new IViewHandler[_viewHandlers.Values.Count];
            _viewHandlers.Values.CopyTo(handlers, 0);
            foreach (var handler in handlers)
            {
                var openModel = handler.GetOpenModel<TModel>();
                if (openModel != null)
                    return openModel;
            }
            
            return default(TModel);
        }

        /// <summary>
        /// Returns the first instance of a model instance of the specified type (or null, if none is instantiated)
        /// </summary>
        /// <typeparam name="TModel">The type of the model.</typeparam>
        /// <returns>Model, or null</returns>
        public static TModel GetOpenModel<TModel>(Func<TModel, bool> selector) where TModel : class
        {
            if (_viewHandlers == null) return default(TModel);

            var handlers = new IViewHandler[_viewHandlers.Values.Count];
            _viewHandlers.Values.CopyTo(handlers, 0);
            foreach (var handler in handlers)
            {
                var openModel = handler.GetOpenModel(selector);
                if (openModel != null)
                    return openModel;
            }

            return default(TModel);
        }

        /// <summary>
        /// Closes all currently open views.
        /// </summary>
        public static void CloseAllViews()
        {
            if (_viewHandlers == null) return;

            var handlers = new IViewHandler[_viewHandlers.Values.Count];
            _viewHandlers.Values.CopyTo(handlers, 0);
            foreach (var handler in handlers)
                handler.CloseAllViews();
        }

        /// <summary>Attempts to find the view associated with the provided model.</summary>
        /// <param name="model">The view model.</param>
        /// <returns>Document if found (null otherwise)</returns>
        /// <remarks>This only works if the model is only used with a single view. This will not work (or work unpredictably) for models that are shared across views.</remarks>
        public static object GetViewForModel(object model)
        {
            if (_viewHandlers == null) return null;
            foreach (var handler in _viewHandlers.Values)
            {
                var view = handler.GetViewForModel(model);
                if (view != null) return view;
            }
            return null;
        }

        /// <summary>Attempts to find the flow document that's part of a view associated with the provided model.</summary>
        /// <param name="model">The model.</param>
        /// <returns>FlowDocument if found, otherwise null.</returns>
        /// <remarks>This only works if the model is only used with a single view. This will not work (or work unpredictably) for models that are shared across views.</remarks>
        public static FlowDocument GetDocumentForModel(object model)
        {
            var element = GetViewForModel(model);
            if (element == null) return null;

            var doc = element as FlowDocument;
            if (doc != null) return doc;

            var view = element as SimpleView;
            if (view != null)
                foreach (var child in view.Items)
                {
                    doc = child as FlowDocument;
                    if (doc == null)
                    {
                        var reader = child as FlowDocumentReader;
                        if (reader != null) doc = reader.Document;
                        if (doc == null)
                        {
                            var viewer = child as DocumentViewerBase;
                            if (viewer != null) doc = viewer.Document as FlowDocument;
                        }
                    }
                    if (doc != null) return doc;
                }
            return null;
        }

        private static void PopulateControllers()
        {
            _controllers = new Dictionary<string, ControllerInformation>();
            lock (_controllers)
                // When we do this, everyone else needs to wait so we do not end up populating this multiple times
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (var assembly in assemblies)
                {
                    string scopeName = assembly.ManifestModule.ScopeName;
                    if (scopeName != "CommonLanguageRuntimeLibrary" && !scopeName.StartsWith("Microsoft.") &&
                        !scopeName.StartsWith("System."))
                    {
                        var assemblyTypes = assembly.GetTypes();
                        foreach (var assemblyType in assemblyTypes)
                            if (assemblyType != null && assemblyType.Namespace != null &&
                                assemblyType.Namespace.EndsWith(".Controllers"))
                                // By naming convention, controllers must be inside a namespace that ends with ".Controllers"
                                if (assemblyType.IsSubclassOf(typeof (Controller)))
                                    _controllers.Add(assemblyType.Name.ToLower(),
                                        new ControllerInformation {ControllerType = assemblyType});
                    }
                }
            }
        }

        private static Dictionary<string, ControllerInformation> _controllers;

        private class ControllerInformation
        {
            public Type ControllerType { private get; set; }
            private Controller _instance;

            public Controller Instance
            {
                get { return _instance ?? (_instance = Activator.CreateInstance(ControllerType) as Controller); }
            }
        }

        /// <summary>
        /// Current request context (used from within controller methods if needed)
        /// </summary>
        /// <example>>
        /// public ActionResult ShowDetails()
        /// {
        ///    if (RequestContext....)
        ///    // more code here...
        /// }
        /// </example>
        public RequestContext RequestContext { get; set; }

        /// <summary>
        /// Internal method that performs execution of actions on controllers (usually triggered by Controller.Action())
        /// </summary>
        /// <param name="requestContext">Executing request context</param>
        protected virtual void Execute(RequestContext requestContext)
        {
            var oldContext = RequestContext;
            try
            {
                RequestContext = requestContext;
                requestContext.ProcessingController = this;

                var methodName = "index";
                if (requestContext.RouteData.Data.ContainsKey("action")) methodName = requestContext.RouteData.Data["action"].ToString();
                else requestContext.RouteData.Data.Add("action", methodName);

                var routedParameters = requestContext.RouteData.Data.Keys.Where(key => key != "controller" && key != "action").ToDictionary(key => key, key => requestContext.RouteData.Data[key].GetType());
                var methods = GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance);

                foreach (var method in methods)
                    if (method.Name.ToLower() == methodName.ToLower() && (method.ReturnType == typeof (ActionResult) || method.ReturnType.IsSubclassOf(typeof (ActionResult))))
                    {
                        // Potential match!
                        var paras = new List<object>();
                        var expectedParameters = method.GetParameters();
                        if (expectedParameters.Length != routedParameters.Count) continue;
                        // We look at each parameter this method expects and see if we have them in the route info
                        foreach (var expectedParameter in expectedParameters)
                        {
                            var expectedParameterName = expectedParameter.Name.ToLower();
                            if (routedParameters.ContainsKey(expectedParameterName) && expectedParameter.ParameterType == routedParameters[expectedParameterName])
                                paras.Add(requestContext.RouteData.Data[expectedParameterName]);
                        }
                        if (paras.Count == expectedParameters.Length) // Looks like we found values for all parameters
                        {
                            requestContext.Result = method.Invoke(this, paras.ToArray()) as ActionResult;
                            return; // We found and invoked the method, so we are done
                        }
                    }

                // We haven't found the desired method yet, but we attempt another way of matching it
                if (routedParameters.Count == 1)
                    foreach (var method in methods)
                        if (method.Name.ToLower() == methodName.ToLower() && (method.ReturnType == typeof (ActionResult) || method.ReturnType.IsSubclassOf(typeof (ActionResult))))
                        {
                            // Potential match!
                            var expectedParameters = method.GetParameters();
                            if (expectedParameters.Length != 1) continue;

                            if (expectedParameters[0].ParameterType != routedParameters.First().Value) continue;
                            // We have a single-parameter method with a parameter type match, so we use that
                            var paras = (from key in requestContext.RouteData.Data.Keys where key != "controller" && key != "action" select requestContext.RouteData.Data[key]).ToList();
                            requestContext.Result = method.Invoke(this, paras.ToArray()) as ActionResult;
                            return; // We found and invoked the method, so we are done
                        }

                // Since we got to this point, we weren't able to find the desired action, so we indicate this to the caller
                requestContext.Result = null;

                var exceptionMessage = "The action '" + methodName + "'";
                if (routedParameters.Count > 0)
                {
                    exceptionMessage += " with parameter(s) named '";
                    var parameterList = string.Empty;
                    foreach (var parameterKey in routedParameters.Keys)
                    {
                        if (!string.IsNullOrEmpty(parameterList)) parameterList += ", ";
                        parameterList += parameterKey;
                    }
                    exceptionMessage += parameterList;
                    exceptionMessage += "'";
                }
                exceptionMessage += " on controller '" + GetType().Name + "' was not found.";
                throw new ActionNotFoundException(exceptionMessage);
            }
            finally
            {
                RequestContext = oldContext;
            }
        }

        /// <summary>
        /// Returns the default view associated with the current action
        /// </summary>
        /// <returns>A view result</returns>
        /// <example>
        /// public ActionResult ShowDetails()
        /// {
        ///     return Document();
        /// }
        /// </example>
        protected virtual ViewResult View()
        {
            var viewName = "Index";
            if (RequestContext.RouteData.Data.ContainsKey("action"))
                viewName = RequestContext.RouteData.Data["action"].ToString();

            return View(viewName);
        }

        /// <summary>
        /// Returns the default view associated with the current action as a partial view
        /// </summary>
        /// <returns>A view result</returns>
        /// <example>
        /// public ActionResult ShowDetails()
        /// {
        ///     return PartialView();
        /// }
        /// </example>
        protected virtual ViewResult PartialView()
        {
            var result = View();
            result.IsPartial = true;
            return result;
        }

        /// <summary>
        /// Returns the default view associated with the current action and passes a view model
        /// </summary>
        /// <param name="model">The model that is to be passed to the view</param>
        /// <param name="forceNewShell">Indicates whether it is desired to launch this view in a new shell (may or may not be respected by each theme)</param>
        /// <returns>A view result</returns>
        /// <example>
        /// public ActionResult ShowDetails()
        /// {
        ///     var model = new MyModel();
        ///     return Document(model);
        /// }
        /// </example>
        protected virtual ViewResult View(object model, bool forceNewShell = false)
        {
            var viewName = "Index";
            if (RequestContext.RouteData.Data.ContainsKey("action"))
                viewName = RequestContext.RouteData.Data["action"].ToString();

            return View(viewName, model, forceNewShell: forceNewShell);
        }

        /// <summary>
        /// Returns the default view associated with the current action as a partial view and passes a view model
        /// </summary>
        /// <param name="model">The model that is to be passed to the view</param>
        /// <returns>A view result</returns>
        /// <example>
        /// public ActionResult ShowDetails()
        /// {
        ///     var model = new MyModel();
        ///     return PartialView(model);
        /// }
        /// </example>
        protected virtual ViewResult PartialView(object model)
        {
            var result = View(model);
            result.IsPartial = true;
            return result;
        }

        /// <summary>
        /// Returns the default view associated with the current action and passes a view model
        /// </summary>
        /// <param name="model">The model that is to be passed to the view</param>
        /// <param name="level">The level the view desires to be</param>
        /// <param name="forceNewShell">Indicates whether it is desired to launch this view in a new shell (may or may not be respected by each theme)</param>
        /// <returns>A view result</returns>
        /// <example>
        /// public ActionResult ShowDetails()
        /// {
        ///     var model = new MyModel();
        ///     return Document(model, ViewLevel.StandAlone);
        /// }
        /// </example>
        protected virtual ViewResult View(object model, ViewLevel level, bool forceNewShell = false)
        {
            var viewName = "Index";
            if (RequestContext.RouteData.Data.ContainsKey("action"))
                viewName = RequestContext.RouteData.Data["action"].ToString();

            return View(viewName, model, level, forceNewShell);
        }

        /// <summary>
        /// Returns a named view associated with the current action and passes a view model
        /// </summary>
        /// <param name="viewName">The name of the view that is to be returned</param>
        /// <param name="model">The model that is to be passed to the view</param>
        /// <returns>A view result</returns>
        /// <example>
        /// public ActionResult ShowDetails()
        /// {
        ///     var model = new MyModel();
        ///     return Document("SomeView", model, ViewLevel.Popup);
        /// }
        /// </example>        
        protected virtual ViewResult PartialView(string viewName, object model = null)
        {
            var result = View(viewName, model);
            result.IsPartial = true;
            return result;
        }

        /// <summary>
        /// Returns a named view associated with the current action and passes a view model
        /// </summary>
        /// <param name="viewName">The name of the view that is to be returned</param>
        /// <param name="model">The model that is to be passed to the view</param>
        /// <param name="level">The level the view desires to be</param>
        /// <param name="forceNewShell">Indicates whether it is desired to launch this view in a new shell (may or may not be respected by each theme)</param>
        /// <returns>A view result</returns>
        /// <exception cref="CODE.Framework.Wpf.Mvvm.ViewNotFoundException"></exception>
        /// <example>
        /// public ActionResult ShowDetails()
        /// {
        ///    var model = new MyModel();
        ///    return Document("SomeView", model, ViewLevel.Popup);
        /// }
        ///   </example>
        protected virtual ViewResult View(string viewName, object model = null, ViewLevel level = ViewLevel.Normal, bool forceNewShell = false)
        {
            var result = new ViewResult {Model = model, ForceNewShell = forceNewShell};

            var locationsSearchedUnsuccessfully = new List<string>();
            if (!FindView(viewName, level, result, locationsSearchedUnsuccessfully))
            {
                var sb = new StringBuilder();
                foreach (var location in locationsSearchedUnsuccessfully) sb.AppendLine(location);
                throw new ViewNotFoundException(sb.ToString());
            }

            return result;
        }

        /// <summary>Returns a standard view associated with the current action and passes a view model</summary>
        /// <param name="standardView">Standard view supported by all themes</param>
        /// <param name="model">The model that is to be passed to the view</param>
        /// <param name="level">The level the view desires to be</param>
        /// <param name="forceNewShell">Indicates whether it is desired to launch this view in a new shell (may or may not be respected by each theme)</param>
        /// <returns>A view result</returns>
        /// <example>
        /// public ActionResult ShowDetails()
        /// {
        ///     var model = new MyModel();
        ///     return Document(StandardViews.Block, model, ViewLevel.Popup);
        /// }
        /// </example>        
        protected virtual ViewResult View(StandardViews standardView, object model = null, ViewLevel level = ViewLevel.Normal, bool forceNewShell = false)
        {
            var viewName = "CODEFrameworkStandardView" + standardView;
            return View(viewName, model, level, forceNewShell);
        }

        /// <summary>
        /// Activates (shows) an existing view
        /// </summary>
        /// <param name="model">The model that is associated with the view that is to be activated.</param>
        /// <returns></returns>
        protected virtual ExistingViewResult ActivateView(object model)
        {
            return new ExistingViewResult {Model = model};
        }

        /// <summary>Returns a named document (view) associated with the current action and assigns the model</summary>
        /// <param name="documentName">Name of the document.</param>
        /// <param name="model">The model.</param>
        /// <returns>Document Result.</returns>
        protected virtual DocumentResult Document(string documentName, object model = null)
        {
            var result = new DocumentResult {Model = model};

            var locationsSearchedUnsuccessfully = new List<string>();
            if (!FindDocument(documentName, result, locationsSearchedUnsuccessfully))
            {
                var sb = new StringBuilder();
                foreach (var location in locationsSearchedUnsuccessfully) sb.AppendLine(location);
                throw new DocumentNotFoundException(sb.ToString());
            }

            return result;
        }

        /// <summary>Returns a named document (view) associated with the current action and assigns the model</summary>
        /// <param name="model">The model.</param>
        /// <returns>Document Result.</returns>
        protected virtual DocumentResult Document(object model = null)
        {
            var documentName = "Index";
            if (RequestContext.RouteData.Data.ContainsKey("action"))
                documentName = RequestContext.RouteData.Data["action"].ToString();
            return Document(documentName, model);
        }

        /// <summary>
        /// Returns a named document associated with the current action and assigns the model and wraps it in a view to it can be loaded in preview mode
        /// </summary>
        /// <param name="documentName">Name of the document.</param>
        /// <param name="model">The model.</param>
        /// <returns>Document Result.</returns>
        protected virtual ViewResult DocumentAsView(string documentName, object model = null)
        {
            var documentResult = Document(documentName, model);
            if (documentResult == null) return null;

            var view = new View();
            view.SetResourceReference(FrameworkElement.StyleProperty, "CODE.Framework-Layout-StandardFormLayout");
            var title = documentResult.Document as ITitle;
            var titleText = title != null ? title.Title : "Preview";
            if (string.IsNullOrEmpty(titleText)) titleText = "Preview";
            SimpleView.SetTitle(view, titleText);
            view.Items.Add(new FlowDocumentReader
            {
                Document = documentResult.Document as FlowDocument,
                Margin = new Thickness(5)
            });
            return new ViewResult
            {
                Model = model,
                View = view,
                ViewTitle = titleText,
                ViewSource = "dynamic"
            };
        }

        /// <summary>
        /// Returns a named document associated with the current action and assigns the model and wraps it in a view to it can be loaded in preview mode
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns>Document Result.</returns>
        protected virtual ViewResult DocumentAsView(object model = null)
        {
            var documentName = "Index";
            if (RequestContext.RouteData.Data.ContainsKey("action"))
                documentName = RequestContext.RouteData.Data["action"].ToString();
            return DocumentAsView(documentName, model);
        }

        /// <summary>Attempts to find the view using all currently registered view engines.</summary>
        /// <param name="viewName">Name of the view to be found</param>
        /// <param name="level">Display level of the view (such as top-level)</param>
        /// <param name="result">The view result object.</param>
        /// <param name="locationsSearchedUnsuccessfully">A list of locations the engines searched unsuccessfully (can be used by callers to display error messages and the like)</param>
        /// <returns>Success indicator (false if no view was found)</returns>
        private bool FindView(string viewName, ViewLevel level, ViewResult result, List<string> locationsSearchedUnsuccessfully = null)
        {
            var foundView = false;
            var controllerName = GetType().Name.ToLower();
            if (controllerName.ToLower().EndsWith("controller"))
                controllerName = controllerName.Substring(0, controllerName.Length - 10);

            foreach (var engine in RegisteredViewEngines)
            {
                var viewResult = engine.GetView(viewName, controllerName);
                if (viewResult.FoundView)
                {
                    result.View = viewResult.View;
                    result.ViewSource = viewResult.ViewSource;
                    result.ViewTitle = SimpleView.GetTitle(viewResult.View);
                    result.ViewIconResourceKey = SimpleView.GetIconResourceKey(viewResult.View);
                    result.View.DataContext = result.Model;
                    var haveViewInformation = result.Model as IHaveViewInformation;
                    if (haveViewInformation != null) haveViewInformation.AssociatedView = result.View;
                    result.ViewLevel = level;
                    foundView = true;
                    break;
                }
                if (locationsSearchedUnsuccessfully != null) locationsSearchedUnsuccessfully.AddRange(viewResult.LocationsSearched);
            }
            return foundView;
        }

        /// <summary>
        /// Attempts to find the document (view) using all currently registered view engines.
        /// </summary>
        /// <param name="documentName">Name of the view to be found</param>
        /// <param name="result">The view result object.</param>
        /// <param name="locationsSearchedUnsuccessfully">A list of locations the engines searched unsuccessfully (can be used by callers to display error messages and the like)</param>
        /// <returns>Success indicator (false if no view was found)</returns>
        private bool FindDocument(string documentName, DocumentResult result, List<string> locationsSearchedUnsuccessfully = null)
        {
            var foundDocument = false;
            var controllerName = GetType().Name.ToLower();
            if (controllerName.ToLower().EndsWith("controller"))
                controllerName = controllerName.Substring(0, controllerName.Length - 10);

            foreach (var engine in RegisteredViewEngines)
            {
                var documentEngine = engine as IDocumentEngine;
                if (documentEngine == null) continue;
                var documentResult = documentEngine.GetDocument(documentName, controllerName);
                if (documentResult.FoundDocument)
                {
                    result.Document = documentResult.Document;
                    result.ViewSource = documentResult.DocumentSource;
                    result.Document.DataContext = result.Model;
                    foundDocument = true;
                    break;
                }
                if (locationsSearchedUnsuccessfully != null) locationsSearchedUnsuccessfully.AddRange(documentResult.LocationsSearched);
            }
            return foundDocument;
        }

        /// <summary>
        /// Returns a model named view associated with the current action and passes a view model
        /// </summary>
        /// <param name="viewName">The name of the view that is to be returned</param>
        /// <param name="model">The model that is to be passed to the view</param>
        /// <param name="level">The level the view desires to be</param>
        /// <param name="scope">The scope of the view (global or local/child).</param>
        /// <returns>A view result</returns>
        /// <example>
        /// public ActionResult ShowDetails()
        /// {
        /// var model = new MyModel();
        /// return ViewModal("SomeView", model, ViewLevel.Popup);
        /// }
        /// </example>
        protected virtual ViewResult ViewModal(string viewName, object model = null, ViewLevel level = ViewLevel.Normal, ViewScope scope = ViewScope.Global)
        {
            var result = View(viewName, model, level);
            result.IsModal = true;
            result.ViewScope = scope;
            return result;
        }

        /// <summary>
        /// Returns a modal default view associated with the current action and passes a view model
        /// </summary>
        /// <param name="model">The model that is to be passed to the view</param>
        /// <param name="level">The level the view desires to be</param>
        /// <param name="scope">The scope of the view (global or local/child).</param>
        /// <returns>A view result</returns>
        /// <example>
        /// public ActionResult ShowDetails()
        /// {
        /// var model = new MyModel();
        /// return ViewModal(model, ViewLevel.Popup);
        /// }
        /// </example>
        protected virtual ViewResult ViewModal(object model = null, ViewLevel level = ViewLevel.Normal, ViewScope scope = ViewScope.Global)
        {
            var result = View(model, level);
            result.IsModal = true;
            result.ViewScope = scope;
            return result;
        }

        /// <summary>
        /// Returns a new shell window
        /// </summary>
        /// <param name="model">Model for the shell</param>
        /// <param name="title">Shell window title</param>
        /// <returns>Shell result</returns>
        protected virtual ShellResult Shell(object model = null, string title = "Application")
        {
            return new ShellResult {Model = model, ViewTitle = title};
        }

        /// <summary>Returns a message box result with an attached model (and potentially an optional view)</summary>
        /// <param name="model">Message box specific view model</param>
        /// <returns>Message box result</returns>
        protected virtual MessageBoxResult MessageBox(MessageBoxViewModel model)
        {
            string viewName = "Index";
            if (RequestContext.RouteData.Data.ContainsKey("action"))
                viewName = RequestContext.RouteData.Data["action"].ToString();

            return MessageBox(model, viewName);
        }

        /// <summary>Returns a message box result with an attached model (and potentially an optional view)</summary>
        /// <param name="model">Message box specific view model</param>
        /// <param name="viewName">Name of the view (ignored if no custom view is found)</param>
        /// <returns>Message box result</returns>
        protected virtual MessageBoxResult MessageBox(MessageBoxViewModel model, string viewName)
        {
            var result = new MessageBoxResult {Model = model, ViewTitle = model.Caption};

            if (_messageBoxResultQueue == null || _messageBoxResultQueue.Count == 0)
                // We are attempting to find the view (and attach it to the result context), but if we can't find it, we are OK with it.
                FindView(viewName, ViewLevel.Popup, result);
            else
            {
                MessageBoxResultQueueItem nextResult;
                lock (_messageBoxResultQueue)
                    nextResult = _messageBoxResultQueue.Dequeue();
                model.Result = nextResult.Result;
                if (nextResult.CustomAction != null)
                    nextResult.CustomAction(model);
                if (model.OnComplete != null)
                    model.OnComplete(result);
            }

            return result;
        }

        /// <summary>Queues a message box result that is to be automatically provided for the next message box request.</summary>
        /// <param name="result">The message box result.</param>
        /// <param name="customAction">An optional custom action that is to be executed after the result is set.</param>
        /// <note>This is a useful feature for testing. Results can be queued up ahead of time causing the message box to not be displayed but to immediately call onComplete with the specified result set.</note>
        public static void QueueMessageBoxResult(MessageBoxResults result, Action<MessageBoxViewModel> customAction = null)
        {
            if (_messageBoxResultQueue == null) _messageBoxResultQueue = new Queue<MessageBoxResultQueueItem>();
            lock (_messageBoxResultQueue)
                _messageBoxResultQueue.Enqueue(new MessageBoxResultQueueItem {Result = result, CustomAction = customAction});
        }

        private static Queue<MessageBoxResultQueueItem> _messageBoxResultQueue;

        private class MessageBoxResultQueueItem
        {
            public MessageBoxResults Result { get; set; }
            public Action<MessageBoxViewModel> CustomAction { get; set; }
        }

        /// <summary>Returns a message box result</summary>
        /// <param name="messageBoxText">Message box text message (plain text)</param>
        /// <param name="caption">Message box caption (title)</param>
        /// <param name="buttons">Standard buttons displayed by the message box</param>
        /// <param name="icon">Standard icon displayed by the message box.</param>
        /// <param name="defaultResult">Default standard button</param>
        /// <returns>Message box result</returns>
        protected virtual MessageBoxResult MessageBox(string messageBoxText, string caption = "Message", MessageBoxButtons buttons = MessageBoxButtons.OK, MessageBoxImages icon = MessageBoxImages.Information, MessageBoxResults defaultResult = MessageBoxResults.OK)
        {
            string viewName = "Index";
            if (RequestContext.RouteData.Data.ContainsKey("action"))
                viewName = RequestContext.RouteData.Data["action"].ToString();
            return MessageBox(viewName, messageBoxText, caption, buttons, icon, defaultResult);
        }

        /// <summary>Displays the specified notification</summary>
        /// <param name="viewName">Name of a custom view to be used by the status.</param>
        /// <param name="text">Main text</param>
        /// <param name="text2">Secondary text</param>
        /// <param name="number">Numeric information (such as an item count) passed as a string</param>
        /// <param name="imageResource">Generic image resource to load a brush from (if this parameter is passed an the resource is found the image parameter is ignored)</param>
        /// <param name="image">A logo image (passed as a brush).</param>
        /// <param name="model">Notification view model (if passed, text, number, image and overrideTimeout parameters are ignored)</param>
        /// <param name="overrideTimeout">Overrides the theme's default notification timeout. If model is passed, set this property in model.</param>
        protected NotificationMessageResult NotificationMessage(string viewName = "", string text = "", string text2 = "", string number = "", string imageResource = "", Brush image = null, NotificationViewModel model = null, TimeSpan? overrideTimeout = null)
        {
            if (model == null)
            {
                model = new NotificationViewModel {Text1 = text, Text2 = text2, Number1 = number, OverrideTimeout = overrideTimeout};
                if (!string.IsNullOrEmpty(imageResource))
                {
                    var resource = Application.Current.FindResource(imageResource);
                    if (resource != null) model.Logo1 = resource as Brush;
                }
                if (model.Logo1 == null && image != null) model.Logo1 = image;
            }

            var result = new NotificationMessageResult {Model = model};

            FindView(viewName, ViewLevel.Normal, result);

            return result;
        }

        /// <summary>Creates a specific status message</summary>
        /// <param name="viewName">Name of the view.</param>
        /// <param name="message">The message to be displayed.</param>
        /// <param name="status">The general status that is to be set.</param>
        /// <param name="model">A custom view model that is to be used.</param>
        /// <remarks>The message and status is ignored if a custom model is passed.</remarks>
        protected StatusMessageResult StatusMessage(string viewName = "", string message = "", ApplicationStatus status = ApplicationStatus.Ready, StatusViewModel model = null)
        {
            if (model == null)
                model = new StatusViewModel {Status = status, Message = message};

            var result = new StatusMessageResult {Model = model};

            FindView(viewName, ViewLevel.Normal, result);

            return result;
        }

        /// <summary>Returns a message box result</summary>
        /// <param name="viewName">Name of the view (ignored if the view is not found).</param>
        /// <param name="messageBoxText">Message box text message (plain text)</param>
        /// <param name="caption">Message box caption (title)</param>
        /// <param name="buttons">Standard buttons displayed by the message box</param>
        /// <param name="icon">Standard icon displayed by the message box.</param>
        /// <param name="defaultResult">Default standard button</param>
        /// <param name="actions">Custom actions to be added to the message box as buttons. (Note: If this parameter is not null, the 'buttons' parameter is ignored)</param>
        /// <param name="model">Custom message box view model. (Note: Only used in exceptional scenarios where the standard view model .</param>
        /// <returns>Message box result</returns>
        protected virtual MessageBoxResult MessageBox(string viewName = "", string messageBoxText = "", string caption = "Message", MessageBoxButtons buttons = MessageBoxButtons.OK, MessageBoxImages icon = MessageBoxImages.Information, MessageBoxResults defaultResult = MessageBoxResults.OK, IEnumerable<IViewAction> actions = null, MessageBoxViewModel model = null)
        {
            bool mustAddButtons = false;
            if (model == null)
            {
                model = new MessageBoxViewModel {Text = messageBoxText, Caption = caption, Icon = icon};
                mustAddButtons = true;
            }

            if (actions == null && mustAddButtons)
                switch (buttons)
                {
                    case MessageBoxButtons.OK:
                        model.Actions.Add(new ViewAction(Properties.Resources.MessageBox_OK, execute: (a, o) =>
                        {
                            model.Result = MessageBoxResults.OK;
                            CloseViewForModel(model);
                        }) {IsDefault = (defaultResult == MessageBoxResults.OK), IsCancel = true});
                        break;
                    case MessageBoxButtons.OKCancel:
                        model.Actions.Add(new ViewAction(Properties.Resources.MessageBox_OK, execute: (a, o) =>
                        {
                            model.Result = MessageBoxResults.OK;
                            CloseViewForModel(model);
                        }) {IsDefault = (defaultResult == MessageBoxResults.OK)});
                        model.Actions.Add(new ViewAction(Properties.Resources.MessageBox_Cancel, execute: (a, o) =>
                        {
                            model.Result = MessageBoxResults.Cancel;
                            CloseViewForModel(model);
                        }) {IsDefault = (defaultResult == MessageBoxResults.Cancel), IsCancel = true});
                        break;
                    case MessageBoxButtons.YesNo:
                        model.Actions.Add(new ViewAction(Properties.Resources.MessageBox_Yes, execute: (a, o) =>
                        {
                            model.Result = MessageBoxResults.Yes;
                            CloseViewForModel(model);
                        }) {IsDefault = (defaultResult == MessageBoxResults.OK || defaultResult == MessageBoxResults.Yes)});
                        model.Actions.Add(new ViewAction(Properties.Resources.MessageBox_No, execute: (a, o) =>
                        {
                            model.Result = MessageBoxResults.No;
                            CloseViewForModel(model);
                        }) {IsDefault = (defaultResult == MessageBoxResults.No), IsCancel = true});
                        break;
                    case MessageBoxButtons.YesNoCancel:
                        model.Actions.Add(new ViewAction(Properties.Resources.MessageBox_Yes, execute: (a, o) =>
                        {
                            model.Result = MessageBoxResults.Yes;
                            CloseViewForModel(model);
                        }) {IsDefault = (defaultResult == MessageBoxResults.OK || defaultResult == MessageBoxResults.Yes)});
                        model.Actions.Add(new ViewAction(Properties.Resources.MessageBox_No, execute: (a, o) =>
                        {
                            model.Result = MessageBoxResults.No;
                            CloseViewForModel(model);
                        }) {IsDefault = (defaultResult == MessageBoxResults.No)});
                        model.Actions.Add(new ViewAction(Properties.Resources.MessageBox_Cancel, execute: (a, o) =>
                        {
                            model.Result = MessageBoxResults.Cancel;
                            CloseViewForModel(model);
                        }) {IsDefault = (defaultResult == MessageBoxResults.Cancel), IsCancel = true});
                        break;
                }
            else if (actions != null)
                foreach (var action in actions)
                    model.Actions.Add(action);

            foreach (var messageAction in model.Actions.OfType<MessageBoxViewAction>())
                messageAction.Model = model;

            return MessageBox(model, viewName);
        }

        /// <summary>This helper method finds the specified view which can often be useful in special cases, such as associating custom views with actions</summary>
        /// <param name="viewName">Name of the view.</param>
        /// <param name="controllerType">Type of the controller (used as a context to find views)</param>
        /// <returns>Document as UIElement, or null if not found.</returns>
        public static FrameworkElement LoadView(string viewName, Type controllerType = null)
        {
            // If a controller type was specified, we try to use it, which provides a context to find views
            Controller controller;
            if (controllerType == null) controller = new Controller();
            else controller = Activator.CreateInstance(controllerType) as Controller;
            if (controller == null) controller = new Controller();
            var result = new ViewResult();
            return controller.FindView(viewName, ViewLevel.Normal, result) ? result.View : null;
        }

        /// <summary>This helper method finds the specified view which can often be useful in special cases, such as associating custom views with actions</summary>
        /// <param name="viewName">Name of the view.</param>
        /// <param name="controller">The controller name.</param>
        /// <returns>Document as UIElement, or null if not found.</returns>
        public static FrameworkElement LoadView(string viewName, string controller)
        {
            if (!controller.EndsWith("Controller")) controller += "Controller";

            if (_controllers == null) PopulateControllers();

            var controllerKey = controller.ToLower();
            if (_controllers != null && _controllers.ContainsKey(controllerKey))
            {
                var controllerInstance = _controllers[controllerKey].Instance;
                var result = new ViewResult();
                return controllerInstance.FindView(viewName, ViewLevel.Normal, result) ? result.View : null;
            }
            return null;
        }

        /// <summary>This helper method finds the specified view which can often be useful in special cases, such as associating custom views with actions</summary>
        /// <param name="standardView">Standard view identifier</param>
        /// <param name="controllerType">Type of the controller (used as a context to find views)</param>
        /// <returns>Document as UIElement, or null if not found.</returns>
        public static FrameworkElement LoadView(StandardViews standardView, Type controllerType = null)
        {
            var viewName = "CODEFrameworkStandardView" + standardView;

            // If a controller type was specified, we try to use it, which provides a context to find views
            Controller controller;
            if (controllerType == null) controller = new Controller();
            else controller = Activator.CreateInstance(controllerType) as Controller;
            if (controller == null) controller = new Controller();
            var result = new ViewResult();
            return controller.FindView(viewName, ViewLevel.Normal, result) ? result.View : null;
        }

        /// <summary>Displays the specified notification</summary>
        /// <param name="text">Main text</param>
        /// <param name="text2">Secondary text</param>
        /// <param name="number">Numeric information (such as an item count) passed as a string</param>
        /// <param name="imageResource">Generic image resource to load a brush from (if this parameter is passed an the resource is found the image parameter is ignored)</param>
        /// <param name="image">A logo image (passed as a brush).</param>
        /// <param name="model">Notification view model (if passed, text, number, image and overrideTimeout parameters are ignored)</param>
        /// <param name="standardView">Standard view to display</param>
        /// <param name="controllerType">Type of the controller (used as a context to find views)</param>
        /// <param name="overrideTimeout">Overrides the theme's default notification timeout. If model is passed, set this property in model.</param>
        public static void Notification(StandardViews standardView, string text = "", string text2 = "", string number = "", string imageResource = "", Brush image = null, NotificationViewModel model = null, Type controllerType = null, TimeSpan? overrideTimeout = null)
        {
            var viewName = "CODEFrameworkStandardView" + standardView;
            Notification(text, text2, number, imageResource, image, model, viewName, controllerType, overrideTimeout);
        }

        /// <summary>Displays the specified notification</summary>
        /// <param name="text">Main text</param>
        /// <param name="text2">Secondary text</param>
        /// <param name="number">Numeric information (such as an item count) passed as a string</param>
        /// <param name="imageResource">Generic image resource to load a brush from (if this parameter is passed an the resource is found the image parameter is ignored)</param>
        /// <param name="image">A logo image (passed as a brush).</param>
        /// <param name="model">Notification view model (if passed, text, number, image and overrideTimeout parameters are ignored)</param>
        /// <param name="viewName">Name of a custom view to be used by the status.</param>
        /// <param name="controllerType">Type of the controller (used as a context to find views)</param>
        /// <param name="overrideTimeout">Overrides the theme's default notification timeout. If model is passed, set this property in model.</param>
        public static void Notification(string text = "", string text2 = "", string number = "", string imageResource = "", Brush image = null, NotificationViewModel model = null, string viewName = "", Type controllerType = null, TimeSpan? overrideTimeout = null)
        {
            var context = new RequestContext(new RouteData("NotificationMessage", new {viewName = string.Empty}));

            // If a controller type was specified, we try to use it, which provides a context to find views
            Controller controller;
            if (controllerType == null) controller = new Controller();
            else controller = Activator.CreateInstance(controllerType) as Controller;
            if (controller == null) controller = new Controller();
            context.ProcessingController = controller;

            context.Result = controller.NotificationMessage(viewName, text, text2, number, imageResource, image, model, overrideTimeout);

            ExecuteViewHandlers(context);
        }

        /// <summary>Sets the application status (typically displayed in a status bar).</summary>
        /// <param name="message">Message that is to be displayed</param>
        /// <param name="status">Application status</param>
        /// <param name="model">Application status view model</param>
        /// <param name="viewName">Name of a custom view to be used by the status.</param>
        /// <param name="controllerType">Type of the controller (used as a context to find views)</param>
        public static void Status(string message = "", ApplicationStatus status = ApplicationStatus.Ready, StatusViewModel model = null, string viewName = "", Type controllerType = null)
        {
            var context = new RequestContext(new RouteData("StatusMessage", new {viewName = string.Empty}));

            // If a controller type was specified, we try to use it, which provides a context to find views
            Controller controller;
            if (controllerType == null) controller = new Controller();
            else controller = Activator.CreateInstance(controllerType) as Controller;
            if (controller == null) controller = new Controller();
            context.ProcessingController = controller;

            context.Result = controller.StatusMessage(viewName, message, status, model);

            ExecuteViewHandlers(context);
        }

        /// <summary>Displays a message box</summary>
        /// <param name="messageBoxText">Message box text message (plain text)</param>
        /// <param name="caption">Message box caption (title)</param>
        /// <param name="buttons">Standard buttons displayed by the message box</param>
        /// <param name="icon">Standard icon displayed by the message box.</param>
        /// <param name="defaultResult">Default standard button</param>
        /// <param name="onComplete">Code to run when the message box closes.</param>
        /// <param name="actions">Custom actions to be added to the message box as buttons. (Note: If this parameter is not null, the 'buttons' parameter is ignored)</param>
        /// <param name="model">Custom message box view model. (Note: Only used in exceptional scenarios where the standard view model .</param>
        /// <param name="viewName">Name of a custom view to be used by the message box (optional).</param>
        /// <param name="controllerType">Type of the controller (used as a context to find views)</param>
        public static void Message(string messageBoxText = "", string caption = "Message", MessageBoxButtons buttons = MessageBoxButtons.OK, MessageBoxImages icon = MessageBoxImages.Information, MessageBoxResults defaultResult = MessageBoxResults.OK, Action<MessageBoxResult> onComplete = null, IEnumerable<IViewAction> actions = null, MessageBoxViewModel model = null, string viewName = "", Type controllerType = null)
        {
            var context = new RequestContext(new RouteData("MessageBox", new {viewName = string.Empty, messageBoxText, caption, buttons, icon, defaultResult}));

            // If a controller type was specified, we try to use it, which provides a context to find views
            Controller controller;
            if (controllerType == null) controller = new Controller();
            else controller = Activator.CreateInstance(controllerType) as Controller;
            if (controller == null) controller = new Controller();
            context.ProcessingController = controller;

            if (model != null && onComplete != null) model.OnComplete = onComplete; // Could be used later, especially for test scenarios where queued results simulate closing of message boxes

            bool mustHandle = _messageBoxResultQueue == null || _messageBoxResultQueue.Count == 0;

            context.Result = controller.MessageBox(viewName, messageBoxText, caption, buttons, icon, defaultResult, actions, model);
            var result = context.Result as MessageBoxResult;
            if (result != null && onComplete != null) result.ViewClosed += (o, e) => onComplete(result);

            if (mustHandle)
                // By default, we let view handlers handle the message box, unless simulated results are queued up.
                ExecuteViewHandlers(context);
        }

        /// <summary>
        /// Collection of view engines registerd and ready to provide views if called upon
        /// </summary>
        public static List<IViewEngine> RegisteredViewEngines { get; set; }
    }

    /// <summary>Document not found exception</summary>
    public class ViewNotFoundException : Exception
    {
        /// <summary>Constructor</summary>
        /// <param name="message">Exception message</param>
        public ViewNotFoundException(string message) : base("The desired view was not found.\r\n\r\nThe following locations were searched:\r\n" + message)
        {
        }
    }

    /// <summary>Document not found exception</summary>
    public class DocumentNotFoundException : Exception
    {
        /// <summary>Constructor</summary>
        /// <param name="message">Exception message</param>
        public DocumentNotFoundException(string message) : base("The desired document was not found.\r\n\r\nThe following locations were searched:\r\n" + message)
        {
        }
    }

    /// <summary>
    /// Request context passed to controllers
    /// </summary>
    public class RequestContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RequestContext"/> class.
        /// </summary>
        /// <param name="routeData">The route data.</param>
        public RequestContext(RouteData routeData)
        {
            RouteData = routeData;
        }

        /// <summary>Gets or sets the route data.</summary>
        /// <value>The route data.</value>
        public RouteData RouteData { get; set; }

        /// <summary>Result produced by the request</summary>
        /// <value>The result.</value>
        public ActionResult Result { get; set; }

        /// <summary>
        /// Reference to the controller that ended up processing this request
        /// </summary>
        public Controller ProcessingController { get; set; }
    }

    /// <summary>
    /// Container class for route data (information passed to a controller)
    /// </summary>
    public class RouteData
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public RouteData()
        {
            Data = new Dictionary<string, object>();
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="action">Action to be called</param>
        /// <param name="data">Additional data (parameters)</param>
        public RouteData(string action, dynamic data)
        {
            Data = new Dictionary<string, object> {{"action", action}};

            if (data != null)
            {
                var properties = data.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
                foreach (var property in properties)
                {
                    var name = property.Name.ToLower();
                    Data.Add(name, property.GetValue(data, null));
                }
            }
        }

        /// <summary>Route data collection</summary>
        public Dictionary<string, object> Data { get; set; }
    }

    /// <summary>Action result base class</summary>
    public class ActionResult
    {
    }

    /// <summary>Action results specific to views</summary>
    public class ViewResult : ActionResult, INotifyPropertyChanged
    {
        /// <summary>Constructor</summary>
        public ViewResult()
        {
            MakeViewVisibleOnLaunch = true;
            ForceNewShell = false;
            LocalViews = new ObservableCollection<ViewResult>();
            SelectedLocalViewIndex = -1;
        }

        private FrameworkElement _view;
        private string _viewTitle;
        private int _selectedLocalViewIndex;

        /// <summary>Document object</summary>
        public FrameworkElement View
        {
            get { return _view; }
            set
            {
                _view = value;
                var view = value as SimpleView;
                if (view != null)
                    view.TitleChanged += (s, e) => { ViewTitle = SimpleView.GetTitle(view); };
            }
        }

        /// <summary>Model object</summary>
        public object Model { get; set; }

        /// <summary>Source the view originated with</summary>
        public string ViewSource { get; set; }

        /// <summary>Display title for the view</summary>
        public string ViewTitle
        {
            get { return _viewTitle; }
            set
            {
                _viewTitle = value;
                OnPropertyChanged("ViewTitle");
            }
        }

        /// <summary>
        /// Group title
        /// </summary>
        /// <value>The group title.</value>
        public string GroupTitle
        {
            get
            {
                if (View == null) return ViewTitle;
                var groupTitle = SimpleView.GetGroupTitle(View);
                if (string.IsNullOrEmpty(groupTitle)) groupTitle = SimpleView.GetGroup(View);
                if (string.IsNullOrEmpty(groupTitle)) return ViewTitle;
                return groupTitle;
            }
        }

        /// <summary>XAML resource key (x:Key) for a brush used as the visual representation of this view</summary>
        public string ViewIconResourceKey { get; set; }

        /// <summary>Defines whether the view is a modal view</summary>
        public bool IsModal { get; set; }

        /// <summary>Defines whether the view is a modal view</summary>
        public bool IsPartial { get; set; }

        /// <summary>Defines the type of UI the view wants to be</summary>
        public ViewLevel ViewLevel { get; set; }

        /// <summary>Defines the scope of the view (whether it is stand-alone or belongs to some other view in hierarchical fashion)</summary>
        public ViewScope ViewScope { get; set; }

        /// <summary>In cases where the view is launched in a top-level window, this property may hold a reference to the window</summary>
        public Window TopLevelWindow { get; set; }

        /// <summary>Indicates whether the view handler is supposed to immediately bring the view to the foreground when it launches (default = true</summary>
        public bool MakeViewVisibleOnLaunch { get; set; }

        /// <summary>Can be used to indicate the desire for the view to launch in a new shell window</summary>
        /// <remarks>It is up to each theme to respect this indicator. Most themes only respect this setting for normal views.</remarks>
        public bool ForceNewShell { get; set; }

        /// <summary>
        /// Occurs when the view has been closed
        /// </summary>
        public event EventHandler<ViewResultEventArgs> ViewClosed;

        /// <summary>
        /// Method used to raise the BeforeViewClosed event
        /// </summary>
        /// <returns>True, if closing has been canceled</returns>
        public bool RaiseBeforeViewClosed()
        {
            var closable = Model as IClosable;
            if (closable != null)
                return closable.RaiseBeforeClosingEvent();

            return false;
        }

        /// <summary>
        /// Method used to raise the ViewClosed event
        /// </summary>
        public void RaiseViewClosed()
        {
            var closable = Model as IClosable;
            if (closable != null)
                closable.RaiseClosingEvent();

            if (ViewClosed != null)
                ViewClosed(this, new ViewResultEventArgs {ViewResult = this});

            if (closable != null)
                closable.RaiseClosedEvent();
        }

        /// <summary>
        /// Occurs when the view has been opened
        /// </summary>
        public event EventHandler<ViewResultEventArgs> ViewOpened;

        /// <summary>
        /// Method used to raise the ViewClosed event
        /// </summary>
        public void RaiseViewOpened()
        {
            if (ViewOpened != null)
                ViewOpened(this, new ViewResultEventArgs {ViewResult = this});
        }

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Called when properties change
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Hierarchical child views (potentially shown as popup dialogs)
        /// </summary>
        /// <remarks>Not used by all themes</remarks>
        public ObservableCollection<ViewResult> LocalViews { get; set; }

        /// <summary>
        /// Index of the currently selected local view
        /// </summary>
        public int SelectedLocalViewIndex
        {
            get { return _selectedLocalViewIndex; }
            set
            {
                _selectedLocalViewIndex = value;
                OnPropertyChanged("SelectedLocalViewIndex");
            }
        }
    }

    /// <summary>Action results specific to documents</summary>
    public class DocumentResult : ActionResult
    {
        /// <summary>Document object</summary>
        public FrameworkContentElement Document { get; set; }

        /// <summary>Model object</summary>
        public object Model { get; set; }

        /// <summary>Source the view originated with</summary>
        public string ViewSource { get; set; }
    }

    /// <summary>Action result used to indicate the desire to activate an existing view/model</summary>
    public class ExistingViewResult : ViewResult
    {
        /// <summary>Model object</summary>
        public object Model { get; set; }
    }

    /// <summary>This class can be used to provide event handlers/sinks that are later attached to view results</summary>
    public class ViewResultEventSinks
    {
        /// <summary>Event handler to be attached to the ViewOpened event</summary>
        public EventHandler<ViewResultEventArgs> ViewOpened { get; set; }

        /// <summary>Event handler to be attached to the ViewClosed event</summary>
        public EventHandler<ViewResultEventArgs> ViewClosed { get; set; }
    }

    /// <summary>
    /// Special result for status messages
    /// </summary>
    public class StatusMessageResult : ViewResult
    {
        /// <summary>Document model specific to status information</summary>
        public new StatusViewModel Model { get; set; }
    }

    /// <summary>
    /// Special result for notification messages
    /// </summary>
    public class NotificationMessageResult : ViewResult
    {
        /// <summary>Document model specific to notification information</summary>
        public new NotificationViewModel Model { get; set; }
    }

    /// <summary>Action result specific to message boxes</summary>
    public class MessageBoxResult : ViewResult
    {
        /// <summary>Constructor</summary>
        public MessageBoxResult()
        {
            ViewLevel = ViewLevel.Popup;
            IsModal = true;
        }

        /// <summary>Document model specific to message boxes</summary>
        public MessageBoxViewModel ModelMessageBox
        {
            get { return Model as MessageBoxViewModel; }
        }
    }

    /// <summary>Special view model used for status information</summary>
    public class StatusViewModel : ViewModel
    {
        /// <summary>Message to be displayed</summary>
        public string Message { get; set; }

        /// <summary>General status to set</summary>
        public ApplicationStatus Status { get; set; }
    }

    /// <summary>Document model class used for notifications</summary>
    public class NotificationViewModel : StandardViewModel
    {
        ///<summary>If not null, overrides the theme's default notification timeout</summary>
        public TimeSpan? OverrideTimeout { get; set; }
    }

    /// <summary>Document model specific to message box results</summary>
    public class MessageBoxViewModel : ViewModel
    {
        /// <summary>Result (indicating which button was pressed in the message box)</summary>
        public MessageBoxResults Result { get; set; }

        /// <summary>Text value of the message box (the actual message)</summary>
        public string Text { get; set; }

        /// <summary>Caption (header/title) of the message box</summary>
        public string Caption { get; set; }

        private MessageBoxImages _icon;

        /// <summary>Icon used by the message box</summary>
        public MessageBoxImages Icon
        {
            get { return _icon; }
            set
            {
                _icon = value;
                switch (value)
                {
                    case MessageBoxImages.None:
                        IconResourceKey = string.Empty;
                        break;
                    case MessageBoxImages.Asterisk:
                        IconResourceKey = "CODE.Framework.Wpf.Mvvm.MessageBox-Icon-Asterisk";
                        break;
                    case MessageBoxImages.Error:
                        IconResourceKey = "CODE.Framework.Wpf.Mvvm.MessageBox-Icon-Error";
                        break;
                    case MessageBoxImages.Exclamation:
                        IconResourceKey = "CODE.Framework.Wpf.Mvvm.MessageBox-Icon-Exclamation";
                        break;
                    case MessageBoxImages.Hand:
                        IconResourceKey = "CODE.Framework.Wpf.Mvvm.MessageBox-Icon-Hand";
                        break;
                    case MessageBoxImages.Information:
                        IconResourceKey = "CODE.Framework.Wpf.Mvvm.MessageBox-Icon-Information";
                        break;
                    case MessageBoxImages.Question:
                        IconResourceKey = "CODE.Framework.Wpf.Mvvm.MessageBox-Icon-Question";
                        break;
                    case MessageBoxImages.Stop:
                        IconResourceKey = "CODE.Framework.Wpf.Mvvm.MessageBox-Icon-Stop";
                        break;
                    case MessageBoxImages.Warning:
                        IconResourceKey = "CODE.Framework.Wpf.Mvvm.MessageBox-Icon-Warning";
                        break;
                }
                _iconBrush = null;
                NotifyChanged("Icon");
                NotifyChanged("IconResourceKey");
                NotifyChanged("IconBrush");
            }
        }

        /// <summary>Method to be called when the message box is done</summary>
        public Action<MessageBoxResult> OnComplete { get; set; }

        /// <summary>Icon resource used by the message box</summary>
        public string IconResourceKey { get; set; }

        private Brush _iconBrush;

        /// <summary>Brush to be used for the icon</summary>
        public Brush IconBrush
        {
            get
            {
                if (_iconBrush == null)
                {
                    if (string.IsNullOrEmpty(IconResourceKey)) _iconBrush = Brushes.Transparent;
                    else
                        try
                        {
                            _iconBrush = Application.Current.FindResource(IconResourceKey) as Brush;
                        }
                        catch (Exception)
                        {
                            _iconBrush = Brushes.Transparent;
                        }
                }
                return _iconBrush;
            }
        }
    }

    /// <summary>
    /// Event args class used for events associated with view results
    /// </summary>
    public class ViewResultEventArgs : EventArgs
    {
        /// <summary>
        /// Document result associated with this event
        /// </summary>
        public ViewResult ViewResult { get; set; }
    }

    /// <summary>Action results specific to views</summary>
    public class ShellResult : ViewResult
    {
        /// <summary>Initializes a new instance of the <see cref="ShellResult"/> class.</summary>
        public ShellResult()
        {
            ViewLevel = ViewLevel.Shell;
        }
    }

    /// <summary>Defines how a view is intended to be displayed</summary>
    public enum ViewLevel
    {
        /// <summary>Standard view within the current application</summary>
        Normal,

        /// <summary>The view desires to be considered a popup 'window'</summary>
        Popup,

        /// <summary>The view desires to be a new top-level window</summary>
        TopLevel,

        /// <summary>The view desires to be a new stand-alone form</summary>
        StandAlone,

        /// <summary>Shell level view ('application window')</summary>
        Shell
    }

    /// <summary>
    /// Scope of the current view (defines whether the view is to be considered a stand-along view (global) or
    /// whehter it conceptually belongs to some other view in a hierarchical relationship (local).
    /// </summary>
    /// <remarks>Not every theme will use this information</remarks>
    public enum ViewScope
    {
        /// <summary>
        /// Global view (stand-alone)
        /// </summary>
        Global,
        /// <summary>
        /// Local view (hierarchical child of other views)
        /// </summary>
        Local
    }

    /// <summary>Document handler interface</summary>
    public interface IViewHandler
    {
        /// <summary>This method is invoked when a view is opened</summary>
        /// <param name="context">Request context (contains information about the view)</param>
        /// <returns>True if handled successfully</returns>
        bool OpenView(RequestContext context);

        /// <summary>This method is invoked when a view that is associated with a certain model should be closed</summary>
        /// <param name="model">Model</param>
        /// <returns>True if successful</returns>
        bool CloseViewForModel(object model);

        /// <summary>This method is invoked when a view that is associated with a certain model should be activated/shown</summary>
        /// <param name="model">Model</param>
        /// <returns>True if successful</returns>
        bool ActivateViewForModel(object model);

        /// <summary>
        /// This method closes all currently open views
        /// </summary>
        /// <returns>True if the handler successfully closed all views. False if it didn't close all views or generally does not handle view closing</returns>
        bool CloseAllViews();

        /// <summary>This method is used to retrieve a view associated with the specified model</summary>
        /// <param name="model">Model</param>
        /// <returns>Document if found (null otherwise)</returns>
        object GetViewForModel(object model);

        /// <summary>
        /// Returns true, if a model instance of the specified type is already open
        /// </summary>
        /// <typeparam name="TModel">The type of the model.</typeparam>
        /// <returns>A reference to the open model instance</returns>
        TModel GetOpenModel<TModel>() where TModel : class;

        /// <summary>
        /// Returns true, if a model instance of the specified type and selector criteria is already open
        /// </summary>
        /// <typeparam name="TModel">The type of the model.</typeparam>
        /// <param name="selector">Selector used to pick an appropriate model instance</param>
        /// <returns>
        /// A reference to the open model instance
        /// </returns>
        TModel GetOpenModel<TModel>(Func<TModel, bool> selector) where TModel : class;
    }

    /// <summary>MessageBox button options</summary>
    public enum MessageBoxButtons
    {
        /// <summary>OK button only</summary>
        OK,

        /// <summary>OK and Cancel buttons</summary>
        OKCancel,

        /// <summary>Yes, No, and Cancel buttons</summary>
        YesNoCancel,

        /// <summary>Yes and No buttons</summary>
        YesNo,
    }

    /// <summary>MessageBox results</summary>
    public enum MessageBoxResults
    {
        /// <summary>No result was determined</summary>
        None = 0,

        /// <summary>The OK button was pressed</summary>
        OK = 1,

        /// <summary>The Cancel button was pressed</summary>
        Cancel = 2,

        /// <summary>The Yes button was pressed</summary>
        Yes = 6,

        /// <summary>The No button was pressed</summary>
        No = 7,
    }

    /// <summary>Images supported as default images in message boxes</summary>
    public enum MessageBoxImages
    {
        /// <summary>No image</summary>
        None,

        /// <summary>Error image</summary>
        Error,

        /// <summary>Hand image</summary>
        Hand,

        /// <summary>Stop sign image</summary>
        Stop,

        /// <summary>Question mark image</summary>
        Question,

        /// <summary>Exclamation point image</summary>
        Exclamation,

        /// <summary>Warning image</summary>
        Warning,

        /// <summary>Asterisk image</summary>
        Asterisk,

        /// <summary>Information image</summary>
        Information,
    }

    /// <summary>List of standard application stati</summary>
    public enum ApplicationStatus
    {
        /// <summary>
        /// Application is ready
        /// </summary>
        Ready,

        /// <summary>
        /// Application is processing
        /// </summary>
        Processing,

        /// <summary>
        /// An error occured
        /// </summary>
        Error,

        /// <summary>
        /// A warning needs to be displayed
        /// </summary>
        Warning
    }

    /// <summary>Standard views provided by all styles</summary>
    public enum StandardViews
    {
        /// <summary>No standard view</summary>
        None,

        /// <summary>Block</summary>
        Block,

        /// <summary>Image</summary>
        Image,

        /// <summary>Large Image</summary>
        LargeImage,

        /// <summary>Large Image and Text 01</summary>
        LargeImageAndText01,

        /// <summary>Large Image and Text 02</summary>
        LargeImageAndText02,

        /// <summary>Large Image Collection</summary>
        LargeImageCollection,

        /// <summary>Peek Image and Text 01</summary>
        PeekImageAndText01,

        /// <summary>Peek Image and Text 02</summary>
        PeekImageAndText02,

        /// <summary>Peek Image and Text 03</summary>
        PeekImageAndText03,

        /// <summary>Peek Image and Text 04</summary>
        PeekImageAndText04,

        /// <summary>Peek Image and Text 05</summary>
        PeekImageAndText05,

        /// <summary>Text 01</summary>
        Text01,

        /// <summary>Text 02</summary>
        Text02,

        /// <summary>Text 03</summary>
        Text03,

        /// <summary>Text 04</summary>
        Text04,

        /// <summary>Text 05</summary>
        Text05,

        /// <summary>Large Block and Text 01</summary>
        LargeBlockAndText01,

        /// <summary>Large Block and Text 02</summary>
        LargeBlockAndText02,

        /// <summary>Large template with a small image and text 01</summary>
        LargeSmallImageAndText01,

        /// <summary>Large template with a small image and text 02</summary>
        LargeSmallImageAndText02,

        /// <summary>Large template with a small image and text 03</summary>
        LargeSmallImageAndText03,

        /// <summary>Large template with a small image and text 04</summary>
        LargeSmallImageAndText04,

        /// <summary>Large template with a small image and text 05</summary>
        LargeSmallImageAndText05,

        /// <summary>Large template with a small image and text 06</summary>
        LargeSmallImageAndText06,

        /// <summary>Large template with a small image and text 07</summary>
        LargeSmallImageAndText07,

        /// <summary>Large Text 01</summary>
        LargeText01,

        /// <summary>Large Text 02</summary>
        LargeText02,

        /// <summary>Large Text 03</summary>
        LargeText03,

        /// <summary>Large Text 04</summary>
        LargeText04,

        /// <summary>Large Text 05</summary>
        LargeText05,

        /// <summary>Large Text 06</summary>
        LargeText06,

        /// <summary>Large Text 07</summary>
        LargeText07,

        /// <summary>Large Text 08</summary>
        LargeText08,

        /// <summary>Large Text 09</summary>
        LargeText09,

        /// <summary>Large Text 10</summary>
        LargeText10,

        /// <summary>Large Text 11</summary>
        LargeText11,

        /// <summary>Large Peek Image Collection 01</summary>
        LargePeekImageCollection01,

        /// <summary>Large Peek Image Collection 02</summary>
        LargePeekImageCollection02,

        /// <summary>Large Peek Image Collection 03</summary>
        LargePeekImageCollection03,

        /// <summary>Large Peek Image Collection 04</summary>
        LargePeekImageCollection04,

        /// <summary>Large Peek Image Collection 04</summary>
        LargePeekImageCollection05,

        /// <summary>Large Peek Image Collection 04</summary>
        LargePeekImageCollection06,

        /// <summary>Large Peek Image and Text 01</summary>
        LargePeekImageAndText01,

        /// <summary>Large Peek Image and Text 02</summary>
        LargePeekImageAndText02,

        /// <summary>Large Peek Image and Text 03</summary>
        LargePeekImageAndText03,

        /// <summary>Large Peek Image and Text 04</summary>
        LargePeekImageAndText04,

        /// <summary>Large Peek Image and Text 05</summary>
        LargePeekImageAndText05,

        /// <summary>Large Peek Image and Text 06</summary>
        LargePeekImageAndText06,

        /// <summary>Document optimized for data display 01</summary>
        Data01,

        /// <summary>Document optimized for data display 02</summary>
        Data02,

        /// <summary>Document optimized for data display 03</summary>
        Data03,

        /// <summary>Document optimized for data and image display 01</summary>
        DataAndImage01,

        /// <summary>Document optimized for data and image display 02</summary>
        DataAndImage02,

        /// <summary>Document optimized for data and image display 03</summary>
        DataAndImage03,

        /// <summary>Document optimized for data and image display in a row 01</summary>
        DataRowAndImage01,

        /// <summary>Small data view 01</summary>
        DataSmall01,

        /// <summary>Small data view 02</summary>
        DataSmall02,

        /// <summary>Small data view 03</summary>
        DataSmall03,

        /// <summary>Template used to display notifications</summary>
        Notification
    }

    /// <summary>
    /// Exception raised whenever a controller action cannot be found
    /// </summary>
    public class ActionNotFoundException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ActionNotFoundException" /> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public ActionNotFoundException(string message) : base(message)
        {

        }
    }
}

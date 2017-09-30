using System;
using System.Windows;

namespace CODE.Framework.Wpf.Mvvm
{
    /// <summary>
    /// Default shell view handler
    /// </summary>
    /// <typeparam name="TViewHandler">The shell class that is to be used to launch shell level views</typeparam>
    public class WindowShellLauncher<TViewHandler> : IViewHandler where TViewHandler : Window, IViewHandler, new()
    {
        /// <summary>
        /// This method is invoked when a view is opened
        /// </summary>
        /// <param name="context">Request context (contains information about the view)</param>
        /// <returns>True if handled successfully</returns>
        public bool OpenView(RequestContext context)
        {
            var shellResult = context.Result as ShellResult;
            if (shellResult != null)
            {
                var shell = new TViewHandler();

                // If we have a title text, the see if the shell has a property that is title-like, and if so, we set it.
                {
                    shell.Title = shellResult.ViewTitle;
                    var realShell = shell as Shell;
                    if (realShell != null) realShell.SetOriginalTitle(shell.Title);
                }

                shell.DataContext = shellResult.Model;
                shell.Show();

                // Every shell also has to act as a view handler in its own right
                Controller.RegisterViewHandler(shell, "Shell");

                var controllerParts = context.ProcessingController.ToString().Split('.');
                _lastLaunchController = controllerParts[controllerParts.Length - 1];
                _lastLaunchAction = context.RouteData.Data["action"].ToString();

                return true;
            }
            return false;
        }

        private string _lastLaunchController;
        private string _lastLaunchAction;

        /// <summary>
        /// This method is invoked when a view that is associated with a certain model should be closed
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public bool CloseViewForModel(object model)
        {
            return false;
        }

        /// <summary>
        /// This method is invoked when a view that is associated with a certain model should be activated/shown
        /// </summary>
        /// <param name="model">Model</param>
        /// <returns>
        /// True if successful
        /// </returns>
        public bool ActivateViewForModel(object model)
        {
            return false;
        }

        /// <summary>
        /// This method closes all currently open views
        /// </summary>
        /// <returns>True if the handler successfully closed all views. False if it didn't close all views or generally does not handle view closing</returns>
        public bool CloseAllViews()
        {
            // This handler does not handle view closing
            return false;
        }

        /// <summary>
        /// This method is used to retrieve a view associated with the specified model
        /// </summary>
        /// <param name="model">Model</param>
        /// <returns>
        /// Document if found (null otherwise)
        /// </returns>
        public object GetViewForModel(object model)
        {
            return null;
        }

        /// <summary>
        /// Returns true, if a model instance of the specified type and selector criteria is already open
        /// </summary>
        /// <typeparam name="TModel">The type of the model.</typeparam>
        /// <param name="selector">Selector used to pick an appropriate model instance</param>
        /// <returns>
        /// A reference to the open model instance
        /// </returns>
        public TModel GetOpenModel<TModel>(Func<TModel, bool> selector) where TModel : class
        {
            return default(TModel);
        }

        /// <summary>
        /// Returns true, if a model instance of the specified type is already open
        /// </summary>
        /// <typeparam name="TModel">The type of the model.</typeparam>
        /// <returns>
        /// A reference to the open model instance
        /// </returns>
        public TModel GetOpenModel<TModel>() where TModel : class
        {
            return default(TModel);
        }

        /// <summary>
        /// Attempts to launch another shell instance, using the same settings the first instance was launched with
        /// </summary>
        /// <remarks>
        /// This method simply fires the same controller action the last shell launch fired.
        /// This method will only work if that controller action does not require any additional parameters
        /// and can be called repeatedly without ill effect (HomeController.Start() usually is safe to call that way)
        /// </remarks>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public bool OpenAnotherShellInstance()
        {
            if (string.IsNullOrEmpty(_lastLaunchAction) || string.IsNullOrEmpty(_lastLaunchController)) return false;

            try
            {
                Controller.Action(_lastLaunchController, _lastLaunchAction);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}

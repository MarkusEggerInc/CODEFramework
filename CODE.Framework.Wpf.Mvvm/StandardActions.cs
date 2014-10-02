using System.Windows;

namespace CODE.Framework.Wpf.Mvvm
{
    /// <summary>This action performs a shutdown of the current application</summary>
    public class ApplicationShutdownViewAction : ViewAction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationShutdownViewAction"/> class.
        /// </summary>
        public ApplicationShutdownViewAction()
        {
            BrushResourceKey = "CODE.Framework-Icon-ClosePane";
        }
        /// <summary>Initializes a new instance of the <see cref="ApplicationShutdownViewAction"/> class.</summary>
        /// <param name="caption">The caption.</param>
        /// <param name="beginGroup">If represented visually, should this action be placed in a new group?</param>
        public ApplicationShutdownViewAction(string caption = "Shutdown", bool beginGroup = false) : base(caption, beginGroup, ExecuteCommand) { }

        private static void ExecuteCommand(IViewAction a, object s)
        {
            Application.Current.Shutdown();
        }
    }

    /// <summary>This action performs a shutdown of the current application</summary>
    public class CloseCurrentViewAction : ViewAction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationShutdownViewAction"/> class.
        /// </summary>
        /// <param name="model">The model associated with the view.</param>
        /// <param name="caption">The caption.</param>
        /// <param name="beginGroup">If represented visually, should this action be placed in a new group?</param>
        /// <param name="category">The category.</param>
        public CloseCurrentViewAction(object model, string caption = "Close", bool beginGroup = false, string category = "") : base(caption, beginGroup, category: category)
        {
            _model = model;
            SetExecutionDelegate(ExecuteCommand);
            BrushResourceKey = "CODE.Framework-Icon-ClosePane";
            IsPinned = true;
        }

        private readonly object _model;

        private void ExecuteCommand(IViewAction a, object s)
        {
            Controller.CloseViewForModel(_model);
        }
    }

    /// <summary>This action moves the keyboard focus to an associated element in the view</summary>
    public class MoveFocusViewAction : ViewAction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationShutdownViewAction"/> class.
        /// </summary>
        /// <param name="model">The model associated with the view.</param>
        /// <param name="moveToElementName">Name of the element to move the cursor to.</param>
        /// <param name="caption">The caption.</param>
        /// <param name="beginGroup">If represented visually, should this action be placed in a new group?</param>
        /// <param name="category">The category.</param>
        public MoveFocusViewAction(IHaveViewInformation model, string moveToElementName, string caption = "", bool beginGroup = false, string category = "") : base(caption, beginGroup, category: category)
        {
            _model = model;
            _targetElementName = moveToElementName;
            SetExecutionDelegate(ExecuteCommand);
        }

        private readonly IHaveViewInformation _model;
        private readonly string _targetElementName;

        private void ExecuteCommand(IViewAction a, object s)
        {
            if (_model.AssociatedView != null)
            {
                var element = _model.AssociatedView as FrameworkElement;
                if (element != null)
                {
                    var element2 = element.FindName(_targetElementName);
                    if (element2 != null)
                    {
                        var element3 = element2 as FrameworkElement;
                        if (element3 != null)
                            element3.Focus();
                    }
                }
            }
        }
    }

    /// <summary>
    /// Standard view action to switch themes
    /// </summary>
    public class SwitchThemeViewAction : ViewAction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationShutdownViewAction" /> class.
        /// </summary>
        /// <param name="theme">The theme to switch to.</param>
        /// <param name="caption">The caption.</param>
        /// <param name="beginGroup">If represented visually, should this action be placed in a new group?</param>
        /// <param name="category">The category.</param>
        /// <param name="categoryAccessKey">The category access key.</param>
        /// <param name="accessKey">The access key.</param>
        public SwitchThemeViewAction(string theme, string caption = "", bool beginGroup = false, string category = "", char categoryAccessKey = ' ', char accessKey = ' ') : base(!string.IsNullOrEmpty(caption) ? caption : theme, beginGroup, category: category, categoryAccessKey: categoryAccessKey, accessKey: accessKey)
        {
            _theme = theme;
            SetExecutionDelegate(ExecuteCommand);
            BrushResourceKey = "CODE.Framework-Icon-Switch";
        }

        private readonly string _theme;

        private void ExecuteCommand(IViewAction a, object s)
        {
            var appEx = Application.Current as ApplicationEx;
            if (appEx != null)
                appEx.Theme = _theme;
        }
    }
}

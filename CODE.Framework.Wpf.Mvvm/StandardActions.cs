using System;
using System.Windows;
using System.Windows.Input;

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
        /// <param name="category">The category.</param>
        /// <param name="standardIcon">The standard icon to be used as a brush resource.</param>
        /// <param name="significance">General significance of the action.</param>
        public ApplicationShutdownViewAction(string caption = "Shutdown",
            bool beginGroup = false,
            string category = "",
            StandardIcons standardIcon = StandardIcons.None,
            ViewActionSignificance significance = ViewActionSignificance.Normal) :
                base(caption, beginGroup, ExecuteCommand, category: category, standardIcon: standardIcon, significance: significance)
        {
            BrushResourceKey = "CODE.Framework-Icon-ClosePane";
        }

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
        /// <param name="standardIcon">The standard icon to be used as a brush resource.</param>
        /// <param name="significance">General significance of the action.</param>
        public CloseCurrentViewAction(object model,
            string caption = "Close",
            bool beginGroup = false,
            string category = "",
            StandardIcons standardIcon = StandardIcons.ClosePane,
            ViewActionSignificance significance = ViewActionSignificance.Normal) :
                base(caption, beginGroup, category: category, standardIcon: standardIcon, significance: significance)
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
        /// <param name="standardIcon">The standard icon to be used as a brush resource.</param>
        /// <param name="significance">General significance of the action.</param>
        public MoveFocusViewAction(IHaveViewInformation model,
            string moveToElementName,
            string caption = "",
            bool beginGroup = false,
            string category = "",
            StandardIcons standardIcon = StandardIcons.None,
            ViewActionSignificance significance = ViewActionSignificance.Normal)
            : base(caption, beginGroup, category: category, standardIcon: standardIcon, significance: significance)
        {
            _model = model;
            _targetElementName = moveToElementName;
            BrushResourceKey = "CODE.Framework-Icon-ClosePane";
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
        /// <param name="standardIcon">The standard icon to be used as a brush resource.</param>
        /// <param name="significance">General significance of the action.</param>
        public SwitchThemeViewAction(string theme,
            string caption = "",
            bool beginGroup = false,
            string category = "",
            char categoryAccessKey = ' ',
            char accessKey = ' ',
            StandardIcons standardIcon = StandardIcons.None,
            ViewActionSignificance significance = ViewActionSignificance.Normal) :
                base(!string.IsNullOrEmpty(caption) ? caption : theme, beginGroup, category: category, standardIcon: standardIcon, significance: significance, categoryAccessKey: categoryAccessKey, accessKey: accessKey)
        {
            _theme = theme;
            BrushResourceKey = "CODE.Framework-Icon-Switch";
            SetExecutionDelegate(ExecuteCommand);
        }

        private readonly string _theme;

        private void ExecuteCommand(IViewAction a, object s)
        {
            var appEx = Application.Current as ApplicationEx;
            if (appEx != null)
                appEx.Theme = _theme;
        }
    }

    /// <summary>
    /// This view action automatically toggles its IsChecked state every time it executes.
    /// </summary>
    public class ToggleViewAction : ViewAction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ToggleViewAction"/> class.
        /// </summary>
        /// <param name="caption">Caption</param>
        /// <param name="beginGroup">Group indicator</param>
        /// <param name="execute">Execution method (delegate)</param>
        /// <param name="canExecute">Can-Execute delegate</param>
        /// <param name="visualResourceKey">Key for XAML resource used for visual representation</param>
        /// <param name="category">Top level category (ID) assigned to this item</param>
        /// <param name="categoryCaption">Display text assigned to the top level category</param>
        /// <param name="categoryOrder">The display order of the category (used for sorting)</param>
        /// <param name="isDefault">Indicates if this is the default action</param>
        /// <param name="isCancel">Indicates if this is the action triggered if the user hits ESC</param>
        /// <param name="significance">General significance of the action.</param>
        /// <param name="userRoles">User roles with access to this action</param>
        /// <param name="brushResourceKey">Resource key for a visual derived from a brush.</param>
        /// <param name="logoBrushResourceKey">Resource key for a visual (used for Logo1) derived from a brush.</param>
        /// <param name="groupTitle">The group title.</param>
        /// <param name="order">The order of the view action (within a group)</param>
        /// <param name="accessKey">The access key for this action (such as the underlined character in a menu if the action is linked to a menu).</param>
        /// <param name="shortcutKey">The shortcut key for the action (usually a hot key that can be pressed without a menu being opened or anything along those lines).</param>
        /// <param name="shortcutKeyModifiers">Modifier for the shortcut key (typically CTRL).</param>
        /// <param name="categoryAccessKey">Access key for the category (only used if a category is assigned).</param>
        /// <param name="isDefaultSelection">Indicates whether this action shall be selected by default</param>
        /// <param name="isPinned">Indicates whether this action is considered to be pinned</param>
        /// <param name="id">Optional unique identifier for the view action (caption is assumed as the ID if no ID is provided)</param>
        /// <param name="standardIcon">The standard icon to be used as a brush resource.</param>
        public ToggleViewAction(string caption = "",
            bool beginGroup = false,
            Action<IViewAction, object> execute = null,
            Func<IViewAction, object, bool> canExecute = null,
            string visualResourceKey = "",
            string category = "", string categoryCaption = "", int categoryOrder = 0,
            bool isDefault = false, bool isCancel = false,
            ViewActionSignificance significance = ViewActionSignificance.Normal,
            string[] userRoles = null,
            string brushResourceKey = "",
            string logoBrushResourceKey = "",
            string groupTitle = "",
            int order = 10000,
            char accessKey = ' ',
            Key shortcutKey = Key.None,
            ModifierKeys shortcutKeyModifiers = ModifierKeys.None,
            char categoryAccessKey = ' ',
            bool isDefaultSelection = false,
            bool isPinned = false,
            string id = "",
            StandardIcons standardIcon = StandardIcons.None) :
                base(caption, beginGroup, null, canExecute, visualResourceKey, category, categoryCaption, categoryOrder, isDefault, isCancel, significance, userRoles,
                    brushResourceKey, logoBrushResourceKey, groupTitle, order, accessKey, shortcutKey, shortcutKeyModifiers, categoryAccessKey, isDefaultSelection, isPinned, id, standardIcon)
        {
            ViewActionType = ViewActionTypes.Toggle;
            SetExecutionDelegate((a, o) =>
            {
                a.IsChecked = !a.IsChecked;
                if (execute != null) execute(a, o);
            });
        }
    }

    /// <summary>
    /// Special view-action associated with a custom view defined by a controller action.
    /// The custom view is typically loaded the first time the action is invoked.
    /// </summary>
    public class OnDemandLoadCustomViewViewAction : ViewAction
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="caption">Caption</param>
        /// <param name="controller">Controller name</param>
        /// <param name="action">Action name (method on the controller)</param>
        /// <param name="routeValues">Optional route values (parameters) to be passed to the controller</param>
        /// <param name="standardIcon">The standard icon to be used as a brush resource.</param>
        /// <param name="category">Top level category (ID) assigned to this item</param>
        /// <param name="significance">General significance of the action.</param>
        public OnDemandLoadCustomViewViewAction(string caption, string controller, string action, dynamic routeValues = null, string category = "", StandardIcons standardIcon = StandardIcons.Switch,
            ViewActionSignificance significance = ViewActionSignificance.Normal) : base(caption, standardIcon: standardIcon, significance: significance, category: category)
        {
            Controller = controller;
            Action = action;
            RouteValues = routeValues;
        }

        /// <summary>
        /// Custom *synchronous* implementation of the execute method to load the view on the spot
        /// </summary>
        /// <param name="parameter"></param>
        public override void Execute(object parameter)
        {
            if (ActionView != null) return;

            var context = Mvvm.Controller.Action(Controller, Action, RouteValues, null, false);
            if (context != null)
            {
                var viewResult = context.Result as ViewResult;
                if (viewResult != null)
                {
                    ActionView = viewResult.View;
                    ActionViewModel = viewResult.Model;
                }
            }
        }

        /// <summary>
        /// Controller name
        /// </summary>
        public string Controller { get; set; }

        /// <summary>
        /// Action name
        /// </summary>
        public string Action { get; set; }

        /// <summary>
        /// Route values (parameters) to be passed to the controller
        /// </summary>
        public dynamic RouteValues { get; set; }

        /// <summary>
        /// If set to true, this action is the one that gets executed by default
        /// </summary>
        public bool IsInitiallySelected { get; set; }
    }
}
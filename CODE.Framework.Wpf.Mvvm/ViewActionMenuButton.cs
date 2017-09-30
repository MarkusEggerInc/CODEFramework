using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using CODE.Framework.Core.Utilities;
using CODE.Framework.Wpf.Utilities;

namespace CODE.Framework.Wpf.Mvvm
{
    /// <summary>
    /// Special button control that can be bound to view actions which are displayed in a drop-down menu style
    /// visual element (actual visualization depends on the theme).
    /// </summary>
    public class ViewActionMenuButton : Button
    {
        /// <summary>
        /// View actions to be displayed in the drop-down
        /// </summary>
        /// <value>The actions.</value>
        public IEnumerable<IViewAction> Actions
        {
            get { return (IEnumerable<IViewAction>)GetValue(ActionsProperty); }
            set { SetValue(ActionsProperty, value); }
        }
        /// <summary>
        /// View actions to be displayed in the drop-down
        /// </summary>
        public static readonly DependencyProperty ActionsProperty = DependencyProperty.Register("Actions", typeof(IEnumerable<IViewAction>), typeof(ViewActionMenuButton), new PropertyMetadata(null, InvalidateActions));

        private static void InvalidateActions(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            var button = d as ViewActionMenuButton;
            if (button == null) return;
            button.GetViewActions(); // Tries to get a list of actions and handles visible/collapsed state of the button if need be
        }

        /// <summary>
        /// Model the view actions collection is a member of
        /// </summary>
        /// <value>The model.</value>
        /// <remarks>Used in combination with the ModelActionsBindingPath property.</remarks>
        public object Model
        {
            get { return GetValue(ModelProperty); }
            set { SetValue(ModelProperty, value); }
        }
        /// <summary>
        /// Model the view actions collection is a member of
        /// </summary>
        /// <remarks>Used in combination with the ModelActionsBindingPath property.</remarks>
        public static readonly DependencyProperty ModelProperty = DependencyProperty.Register("Model", typeof(object), typeof(ViewActionMenuButton), new PropertyMetadata(null, InvalidateActions));

        /// <summary>
        /// Binding path to the view actions collection on the model
        /// </summary>
        /// <value>The model actions binding path.</value>
        /// <remarks>Used in combination with the Model property.</remarks>
        public string ModelActionsBindingPath
        {
            get { return (string)GetValue(ModelActionsBindingPathProperty); }
            set { SetValue(ModelActionsBindingPathProperty, value); }
        }
        /// <summary>
        /// Binding path to the view actions collection on the model
        /// </summary>
        /// <remarks>Used in combination with the Model property.</remarks>
        public static readonly DependencyProperty ModelActionsBindingPathProperty = DependencyProperty.Register("ModelActionsBindingPath", typeof(string), typeof(ViewActionMenuButton), new PropertyMetadata("", InvalidateActions));

        /// <summary>
        /// Defines whether or not the whole button auto-hides when no bound view actions are available
        /// </summary>
        /// <value>True (default) or false</value>
        public bool AutoHideButtonWhenNoActionsAreAvailable
        {
            get { return (bool)GetValue(AutoHideButtonWhenNoActionsAreAvailableProperty); }
            set { SetValue(AutoHideButtonWhenNoActionsAreAvailableProperty, value); }
        }
        /// <summary>
        /// Defines whether or not the whole button auto-hides when no bound view actions are available
        /// </summary>
        /// <value>True (default) or false</value>
        public static readonly DependencyProperty AutoHideButtonWhenNoActionsAreAvailableProperty = DependencyProperty.Register("AutoHideButtonWhenNoActionsAreAvailable", typeof(bool), typeof(ViewActionMenuButton), new PropertyMetadata(true, InvalidateActions));

        /// <summary>
        /// Policy that can be applied to view-actions displayed in the ribbon.
        /// </summary>
        /// <remarks>
        /// This kind of policy can be used to change which view-actions are to be displayed, or which order they are displayed in.
        /// </remarks>
        /// <value>The view action policy.</value>
        public IViewActionPolicy ViewActionPolicy
        {
            get { return (IViewActionPolicy)GetValue(ViewActionPolicyProperty); }
            set { SetValue(ViewActionPolicyProperty, value); }
        }

        /// <summary>
        /// Policy that can be applied to view-actions displayed in the ribbon.
        /// </summary>
        /// <remarks>
        /// This kind of policy can be used to change which view-actions are to be displayed, or which order they are displayed in.
        /// </remarks>
        /// <value>The view action policy.</value>
        public static readonly DependencyProperty ViewActionPolicyProperty = DependencyProperty.Register("ViewActionPolicy", typeof(IViewActionPolicy), typeof(ViewActionMenuButton), new PropertyMetadata(null));

        /// <summary>
        /// Called when a <see cref="T:System.Windows.Controls.Button" /> is clicked.
        /// </summary>
        protected override void OnClick()
        {
            var actions = GetViewActions();
            if (actions == null)
            {
                base.OnClick();
                return;
            }

            var menu = new ContextMenu();
            var isFirst = true;
            foreach (var action in actions.Where(a => a.Availability == ViewActionAvailabilities.Available && a.Visibility == Visibility.Visible))
            {
                var realAction = action as ViewAction;
                var menuItem = new ViewActionMenuItem {DataContext = action, Command = action};
                if (action.ActionView == null)
                {
                    menuItem.Header = GetMenuTitle(action);
                    if (realAction != null)
                        realAction.PropertyChanged += (s, e) =>
                        {
                            if (e.PropertyName == "Caption")
                                menuItem.Header = GetMenuTitle(action);
                        };
                }
                else
                {
                    ElementHelper.DetachElementFromParent(action.ActionView);
                    menuItem.Header = action.ActionView;
                    if (action.ActionViewModel != null)
                        action.ActionView.DataContext = action.ActionViewModel;
                }

                if (!isFirst && action.BeginGroup) menu.Items.Add(new Separator());
                if (action.ViewActionType == ViewActionTypes.Toggle)
                {
                    menuItem.IsCheckable = true;
                    menuItem.IsChecked = action.IsChecked;
                    //menuItem.SetBinding(MenuItem.IsCheckedProperty, new Binding("IsChecked") {Source = action});
                }
                if (realAction != null && realAction.HasBrush)
                {
                    var icon = new ThemeIcon {FallbackIconResourceKey = string.Empty};
                    icon.SetBinding(ThemeIcon.IconResourceKeyProperty, new Binding("BrushResourceKey"));
                    menuItem.Icon = icon;
                }
                HandleMenuShortcutKey(menuItem, action);
                menu.Items.Add(menuItem);
                isFirst = false;
            }
            menu.Placement = PlacementMode.Bottom;
            menu.PlacementTarget = this;
            menu.IsOpen = true;
        }

        /// <summary>
        /// Determines the display title of a menu item
        /// </summary>
        /// <param name="action">The category.</param>
        /// <returns>Title</returns>
        protected virtual string GetMenuTitle(IViewAction action)
        {
            var sb = new StringBuilder();
            var titleChars = action.Caption.ToCharArray();
            var titleCharsLower = action.Caption.ToLower().ToCharArray();
            var foundAccessKey = false;
            var lowerKey = action.AccessKey.ToString(CultureInfo.CurrentUICulture).ToLower().ToCharArray()[0];
            for (var counter = 0; counter < titleChars.Length; counter++)
            {
                var character = titleChars[counter];
                var characterLower = titleCharsLower[counter];
                if (action.AccessKey != ' ' && !foundAccessKey && characterLower == lowerKey)
                {
                    sb.Append("_"); // This is the hotkey indicator in WPF
                    foundAccessKey = true;
                }
                if (character == '_')
                    sb.Append("_"); // Escaping the underscore so it really shows up in the menu rather than being interpreted special
                sb.Append(character);
            }

            if (!foundAccessKey && action.AccessKey != ' ')
            {
                sb.Append(" (_");
                sb.Append(action.AccessKey);
                sb.Append(")");
            }
            return sb.ToString();
        }

        /// <summary>
        /// Handles the assignment of shortcut keys
        /// </summary>
        /// <param name="menuItem">The menu item.</param>
        /// <param name="action">The category.</param>
        protected virtual void HandleMenuShortcutKey(MenuItem menuItem, IViewAction action)
        {
            if (action.ShortcutKey == Key.None) return;

            _menuKeyBindings.Clear();
            var text = action.ShortcutKey.ToString().ToUpper();

            switch (action.ShortcutModifiers)
            {
                case ModifierKeys.Alt:
                    text = "ALT+" + text;
                    break;
                case ModifierKeys.Control:
                    text = "CTRL+" + text;
                    break;
                case ModifierKeys.Shift:
                    text = "SHIFT+" + text;
                    break;
                case ModifierKeys.Windows:
                    text = "Windows+" + text;
                    break;
            }

            menuItem.InputGestureText = text;

            _menuKeyBindings.Add(new ViewActionMenuKeyBinding(action));
        }

        private readonly List<ViewActionMenuKeyBinding> _menuKeyBindings = new List<ViewActionMenuKeyBinding>();

        /// <summary>
        /// Removes all key bindings from the current window that were associated with a view category menu
        /// </summary>
        private void CreateAllMenuKeyBindings()
        {
            var window = ElementHelper.FindVisualTreeParent<Window>(this);
            if (window == null) return;

            foreach (var binding in _menuKeyBindings)
                window.InputBindings.Add(binding);
        }

        /// <summary>
        /// Returns a list of currently used view actions
        /// </summary>
        /// <returns>IEnumerable&lt;IViewAction&gt;.</returns>
        private IEnumerable<IViewAction> GetViewActions()
        {
            if (Actions != null)
            {
                if (AutoHideButtonWhenNoActionsAreAvailable && Visibility != Visibility.Visible) Visibility = Visibility.Visible;
                if (ViewActionPolicy != null) return ViewActionPolicy.ProcessActions(Actions);
                return Actions;
            }

            if (Model != null && !string.IsNullOrEmpty(ModelActionsBindingPath))
            {
                var actions = Model.GetPropertyValue<IEnumerable<IViewAction>>(ModelActionsBindingPath);
                if (actions != null)
                {
                    if (AutoHideButtonWhenNoActionsAreAvailable && Visibility != Visibility.Visible) Visibility = Visibility.Visible;
                    if (ViewActionPolicy != null) return ViewActionPolicy.ProcessActions(actions);
                    return actions;
                }
            }

            if (AutoHideButtonWhenNoActionsAreAvailable && Visibility != Visibility.Collapsed) Visibility = Visibility.Collapsed;
            return null;
        }
    }
}

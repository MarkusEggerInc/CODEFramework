using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using CODE.Framework.Wpf.Utilities;

namespace CODE.Framework.Wpf.Mvvm
{
    /// <summary>
    /// Special menu object that can be bound to a collection of view actions to automatically and dynamically populate the menu.
    /// </summary>
    public class ViewActionMenu : Menu
    {
        /// <summary>
        /// Model used as the data context
        /// </summary>
        public object Model
        {
            get { return GetValue(ModelProperty); }
            set { SetValue(ModelProperty, value); }
        }
        /// <summary>
        /// Model dependency property
        /// </summary>
        public static readonly DependencyProperty ModelProperty = DependencyProperty.Register("Model", typeof(object), typeof(ViewActionMenu), new UIPropertyMetadata(null, ModelChanged));
        /// <summary>
        /// Change handler for model property
        /// </summary>
        /// <param name="d">The dependency object that triggered this change.</param>
        /// <param name="e">The <see cref="System.Windows.DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        static void ModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var menu = d as ViewActionMenu;
            if (menu != null)
            {
                var actionsContainer = e.NewValue as IHaveActions;
                if (actionsContainer != null && actionsContainer.Actions != null)
                {
                    actionsContainer.Actions.CollectionChanged += (s, e2) => menu.PopulateMenu(actionsContainer);
                    menu.Visibility = Visibility.Visible;
                    menu.PopulateMenu(actionsContainer);
                }
                else
                    menu.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Selected view used as the data context
        /// </summary>
        public object SelectedView
        {
            get { return GetValue(SelectedViewProperty); }
            set { SetValue(SelectedViewProperty, value); }
        }
        /// <summary>
        /// Selected view dependency property
        /// </summary>
        public static readonly DependencyProperty SelectedViewProperty = DependencyProperty.Register("SelectedView", typeof(object), typeof(ViewActionMenu), new UIPropertyMetadata(null, SelectedViewChanged));
        /// <summary>
        /// Change handler for selected view property
        /// </summary>
        /// <param name="d">The dependency object that triggered this change.</param>
        /// <param name="e">The <see cref="System.Windows.DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        static void SelectedViewChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d == null) return;
            var menu = d as ViewActionMenu; 
            if (menu == null) return;
            var viewResult = e.NewValue as ViewResult;
            if (viewResult == null)
            {
                menu.PopulateMenu(menu.Model as IHaveActions);
                return;
            }

            var actionsContainer = viewResult.Model as IHaveActions;
            if (actionsContainer != null)
            {
                actionsContainer.Actions.CollectionChanged += (s, e2) => menu.PopulateMenu(menu.Model as IHaveActions, actionsContainer);
                menu.PopulateMenu(menu.Model as IHaveActions, actionsContainer);
            }
            else
                menu.PopulateMenu(menu.Model as IHaveActions);
        }

        /// <summary>
        /// If set to true, the top level menu items will be forced to be upper case
        /// </summary>
        /// <value><c>true</c> if [force top level menu items upper case]; otherwise, <c>false</c>.</value>
        public bool ForceTopLevelMenuItemsUpperCase
        {
            get { return (bool)GetValue(ForceTopLevelMenuItemsUpperCaseProperty); }
            set { SetValue(ForceTopLevelMenuItemsUpperCaseProperty, value); }
        }
        /// <summary>
        /// If set to true, the top level menu items will be forced to be upper case
        /// </summary>
        public static readonly DependencyProperty ForceTopLevelMenuItemsUpperCaseProperty = DependencyProperty.Register("ForceTopLevelMenuItemsUpperCase", typeof(bool), typeof(ViewActionMenu), new PropertyMetadata(false, ForceTopLevelMenuItemsToUpperCaseChanged));

        private static void ForceTopLevelMenuItemsToUpperCaseChanged(DependencyObject d, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            var menu = d as ViewActionMenu;
            if (menu != null) menu.PopulateMenu(menu.Model as IHaveActions);
        }

        /// <summary>
        /// Populates the current menu with items based on the actions collection
        /// </summary>
        /// <param name="actions">List of primary actions</param>
        /// <param name="actions2">List of view specific actions</param>
        private void PopulateMenu(IHaveActions actions, IHaveActions actions2 = null)
        {
            RemoveAllMenuKeyBindings();
            Items.Clear();
            if (actions == null) return;

            var actionList = ViewActionHelper.GetConsolidatedActions(actions, actions2, "File");
            var rootCategories = ViewActionHelper.GetTopLevelActionCategories(actionList, "File", "File");

            foreach (var category in rootCategories)
            {
                var menuItem = new TopLevelViewActionMenuItem {Header = GetMenuTitle(category)};
                menuItem.SetBinding(VisibilityProperty, new Binding("Count") {Source = menuItem.Items, Converter = new ItemsCollectionCountToVisibleConverter(menuItem.Items)});
                PopulateSubCategories(menuItem, category, actionList);
                Items.Add(menuItem);
            }

            CreateAllMenuKeyBindings();
        }

        private class ItemsCollectionCountToVisibleConverter : IValueConverter
        {
            private readonly ItemCollection _items;
            public ItemsCollectionCountToVisibleConverter(ItemCollection items)
            {
                _items = items;
            }
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                // We are bound to a property of the items collection, but we do not really care and always go after the items colletion itself to detirmined visibility
                if (_items == null) return Visibility.Collapsed;
                foreach (var item in _items)
                {
                    var menuItem = item as MenuItem;
                    if (menuItem != null && menuItem.Visibility == Visibility.Visible)
                        return Visibility.Visible;
                }

                return Visibility.Collapsed;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                // Not used
                return null;
            }
        }

        /// <summary>
        /// Adds sub-items for the specified menu item and category
        /// </summary>
        /// <param name="menuItem">Parent menu item</param>
        /// <param name="category">Category we are interested in</param>
        /// <param name="actions">Actions to consider</param>
        /// <param name="indentLevel">Current hierarchical indentation level</param>
        private void PopulateSubCategories(MenuItem menuItem, ViewActionCategory category, IEnumerable<IViewAction> actions, int indentLevel = 0)
        {
            var populatedCategories = new List<string>();
            if (actions == null) return;
            var viewActions = actions as IViewAction[] ?? actions.ToArray();
            var matchingActions = ViewActionHelper.GetAllActionsForCategory(viewActions, category, indentLevel);
            var addedMenuItems = 0;
            foreach (var matchingAction in matchingActions)
            {
                if (addedMenuItems > 0 && matchingAction.BeginGroup) menuItem.Items.Add(new Separator());

                if (matchingAction.Categories != null && matchingAction.Categories.Count > indentLevel + 1 && !populatedCategories.Contains(matchingAction.Categories[indentLevel].Id)) // This is further down in a sub-category even
                {
                    populatedCategories.Add(matchingAction.Categories[indentLevel].Id);
                    var newMenuItem = new ViewActionMenuItem {Header = matchingAction.Categories[indentLevel+1].Caption};
                    var realAction = matchingAction as ViewAction;
                    if (realAction != null && realAction.HasBrush)
                        newMenuItem.Icon = new ViewActionMenuIcon(realAction.PopulatedBrush);
                    CreateMenuItemBinding(matchingAction, newMenuItem);
                    PopulateSubCategories(newMenuItem, matchingAction.Categories[indentLevel+1], viewActions, indentLevel + 1);
                    menuItem.Items.Add(newMenuItem);
                    addedMenuItems++;
                }
                else
                {
                    var newMenuItem1 = new ViewActionMenuItem {Header = GetMenuTitle(matchingAction), Command = matchingAction};
                    HandleMenuShortcutKey(newMenuItem1, matchingAction);
                    if (matchingAction.ViewActionType == ViewActionTypes.Toggle)
                    {
                        newMenuItem1.IsCheckable = true;
                        newMenuItem1.SetBinding(MenuItem.IsCheckedProperty, new Binding("IsChecked") {Source = matchingAction});
                    }
                    var realAction = matchingAction as ViewAction;
                    if (realAction != null && realAction.HasBrush)
                        newMenuItem1.Icon = new ViewActionMenuIcon(realAction.PopulatedBrush);
                    CreateMenuItemBinding(matchingAction, newMenuItem1);
                    menuItem.Items.Add(newMenuItem1);
                    addedMenuItems++;
                }
            }
        }

        /// <summary>
        /// Handles the assignment of shortcut keys
        /// </summary>
        /// <param name="menuItem">The menu item.</param>
        /// <param name="action">The category.</param>
        protected virtual void HandleMenuShortcutKey(MenuItem menuItem, IViewAction action)
        {
            if (action.ShortcutKey == Key.None) return;

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
        /// Removes all key bindings from the current window that were associated with a view category menu
        /// </summary>
        private void RemoveAllMenuKeyBindings()
        {
            _menuKeyBindings.Clear();

            var window = ElementHelper.FindVisualTreeParent<Window>(this);
            if (window == null) return;

            var bindingIndex = 0;
            while (true)
            {
                if (bindingIndex >= window.InputBindings.Count) break;
                var binding = window.InputBindings[bindingIndex];
                if (binding is ViewActionMenuKeyBinding)
                    window.InputBindings.RemoveAt(bindingIndex); // We remove the item from the collection and start over with the remove operation since now all indexes changed
                else
                    bindingIndex++;
            }
        }

        /// <summary>
        /// Detirmines the display title of a menu item
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
        /// Detirmines the display title of a menu item
        /// </summary>
        /// <param name="category">The category.</param>
        /// <returns>Title</returns>
        protected virtual string GetMenuTitle(ViewActionCategory category)
        {
            var sb = new StringBuilder();
            var titleChars = category.Caption.ToCharArray();
            var titleCharsLower = category.Caption.ToLower().ToCharArray();
            var foundAccessKey = false;
            var lowerKey = category.AccessKey.ToString(CultureInfo.CurrentUICulture).ToLower().ToCharArray()[0];
            for (var counter = 0; counter < titleChars.Length; counter++)
            {
                var character = titleChars[counter];
                var characterLower = titleCharsLower[counter];
                if (category.AccessKey != ' ' && !foundAccessKey && characterLower == lowerKey)
                {
                    sb.Append("_"); // This is the hotkey indicator in WPF
                    foundAccessKey = true;
                }
                if (character == '_')
                    sb.Append("_"); // Escaping the underscore so it really shows up in the menu rather than being interpreted special
                sb.Append(character);
            }

            if (!foundAccessKey && category.AccessKey != ' ')
            {
                sb.Append(" (_");
                sb.Append(category.AccessKey);
                sb.Append(")");
            }
            var title = sb.ToString();
            if (ForceTopLevelMenuItemsUpperCase) title = title.ToUpper();
            return title;
        }

        private static void CreateMenuItemBinding(IViewAction action, MenuItem menuItem)
        {
            menuItem.SetBinding(VisibilityProperty, new Binding("Availability") { Source = action, Converter = new AvailabilityToVisibleConverter() });

            // If this is a real ViewAction, we can listen to changed events on the availability property, which can lead us to changing the visibility on the parent menu
            var viewAction = action as ViewAction;
            if (viewAction != null)
                viewAction.PropertyChanged += (s, e) =>
                                                  {
                                                      if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == "Availability")
                                                          // We force the system to re-evaluate the Visibility property on the parent item, just in case the change in availability triggered a visibility change of the entire parent item
                                                          if (menuItem.Parent != null)
                                                          {
                                                              var parentMenu = menuItem.Parent as MenuItem;
                                                              if (parentMenu != null)
                                                                  parentMenu.SetBinding(VisibilityProperty, new Binding("Count") {Source = parentMenu.Items, Converter = new ItemsCollectionCountToVisibleConverter(parentMenu.Items)});
                                                          }
                                                  };
        }

        private class AvailabilityToVisibleConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (!(value is ViewActionAvailabilities)) return Visibility.Collapsed;
                var availability = (ViewActionAvailabilities) value;
                return availability == ViewActionAvailabilities.Available ? Visibility.Visible : Visibility.Collapsed;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                // Not used
                return null;
            }
        }
    }

    /// <summary>
    /// Class used for top level menu items (the ones that are usually going left to right at the top of the screen)
    /// </summary>
    public class TopLevelViewActionMenuItem : MenuItem
    {
    }

    /// <summary>
    /// Standard menu item used by view category menus
    /// </summary>
    public class ViewActionMenuItem : MenuItem
    {
    }

    /// <summary>
    /// Icon placeholder for menu items in view category menus
    /// </summary>
    public class ViewActionMenuIcon : Grid
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ViewActionMenuIcon"/> class.
        /// </summary>
        /// <param name="icon">The icon.</param>
        public ViewActionMenuIcon(Brush icon)
        {
            if (icon == null)
            {
                Background = Brushes.Transparent;
                return;
            }

            var brush = icon.Clone();

            var drawingBrush = brush as DrawingBrush;
            if (drawingBrush != null) drawingBrush.Stretch = Stretch.Uniform;

            var imageBrush = brush as ImageBrush;
            if (imageBrush != null) imageBrush.Stretch = Stretch.Uniform;

            Background = brush;
        }
    }

    /// <summary>
    /// Special key binding used by view category menus
    /// </summary>
    public class ViewActionMenuKeyBinding : KeyBinding
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ViewActionMenuKeyBinding"/> class.
        /// </summary>
        /// <param name="action">The category.</param>
        public ViewActionMenuKeyBinding(IViewAction action)
        {
            Key = action.ShortcutKey;
            Modifiers = action.ShortcutModifiers;
            Command = action;
        }
    }
}

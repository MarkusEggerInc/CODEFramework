using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using CODE.Framework.Wpf.Mvvm;
using CODE.Framework.Wpf.Utilities;

namespace CODE.Framework.Wpf.Theme.Universe.Controls
{
    /// <summary>
    /// Class ViewActionHamburgerMenu.special class used to implement a hamburger menu
    /// </summary>
    public class ViewActionHamburgerMenu : Panel
    {
        /// <summary>
        /// Indicates whether an expand operation is permanent (true) or just overlaps other content (false)
        /// </summary>
        /// <value><c>true</c> if [expands permanently]; otherwise, <c>false</c>.</value>
        public bool ExpandsPermanently
        {
            get { return (bool)GetValue(ExpandsPermanentlyProperty); }
            set { SetValue(ExpandsPermanentlyProperty, value); }
        }
        /// <summary>
        /// Indicates whether an expand operation is permanent (true) or just overlaps other content (false)
        /// </summary>
        public static readonly DependencyProperty ExpandsPermanentlyProperty = DependencyProperty.Register("ExpandsPermanently", typeof(bool), typeof(ViewActionHamburgerMenu), new PropertyMetadata(true));

        /// <summary>
        /// Expanded state of the menu
        /// </summary>
        /// <value>The expanded state</value>
        public HamburgerMenuState Expanded
        {
            get { return (HamburgerMenuState)GetValue(ExpandedProperty); }
            set { SetValue(ExpandedProperty, value); }
        }
        /// <summary>
        /// Expanded state of the menu
        /// </summary>
        public static readonly DependencyProperty ExpandedProperty = DependencyProperty.Register("Expanded", typeof(HamburgerMenuState), typeof(ViewActionHamburgerMenu), new PropertyMetadata(HamburgerMenuState.Collapsed, OnExpandedStateChanged));

        /// <summary>
        /// Fires when the expanded state of the menu changes
        /// </summary>
        /// <param name="d">The menu object</param>
        /// <param name="e">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void OnExpandedStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var menu = d as ViewActionHamburgerMenu;
            if (menu == null) return;
            menu.InvalidateMeasure();
            menu.InvalidateArrange();
            menu.InvalidateVisual();
        }

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
        public static readonly DependencyProperty ModelProperty = DependencyProperty.Register("Model", typeof (object), typeof (ViewActionHamburgerMenu), new UIPropertyMetadata(null, ModelChanged));

        /// <summary>
        /// Change handler for model property
        /// </summary>
        /// <param name="d">The dependency object that triggered this change.</param>
        /// <param name="e">The <see cref="System.Windows.DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void ModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var panel = d as ViewActionHamburgerMenu;
            if (panel == null) return;
            var actionsContainer = e.NewValue as IHaveActions;
            if (actionsContainer != null && actionsContainer.Actions != null)
            {
                actionsContainer.Actions.CollectionChanged += (s, e2) => panel.PopulateStack();
                panel.PopulateStack();
            }
        }

        private readonly Dictionary<string, Brush> _knownBrushes = new Dictionary<string, Brush>();

        /// <summary>
        /// Populates the current ribbon with items based on the actions collection
        /// </summary>
        protected virtual void PopulateStack()
        {
            var actions = Model as IHaveActions;

            Children.Clear();
            if (actions == null)
            {
                Visibility = Visibility.Collapsed;
                return;
            }
            Visibility = Visibility.Visible;
            Children.Add(new HamburgerMenuTopButton(this));

            foreach (var action in actions.Actions.OrderBy(a => a.Order))
                if (action.Categories.Count == 0 || (action.Categories.Count == 1 && (string.IsNullOrEmpty(action.Categories[0].Caption) || action.Categories[0].Caption == "File")))
                {
                    var button = new HamburgerMenuButton {Command = action, ToolTip = action.Caption, DataContext = action, Caption = action.Caption};
                    if (action is ViewAction) button.SetBinding(HamburgerMenuButton.BrushResourceKeyProperty, new Binding("BrushResourceKey") {Source = action});
                    Children.Add(button);
                    button.SetBinding(VisibilityProperty, new Binding("Visibility"));
                }

            var categories = ViewActionHelper.GetTopLevelActionCategories(actions.Actions);
            foreach (var category in categories.OrderBy(c => c.Order))
            {
                if (string.IsNullOrEmpty(category.Caption) || category.Caption == "File") continue;
                var subActions = ViewActionHelper.GetAllActionsForCategory(actions.Actions, category).ToArray();
                if (subActions.Length < 1) continue;
                var button = new HamburgerMenuButton {Caption = category.Caption, DataContext = category, SubActions = subActions, ToolTip = category.Caption};
                button.SetBinding(HamburgerMenuButton.BrushResourceKeyProperty, new Binding("BrushResourceKey") {Source = category});
                Children.Add(button);
            }
        }

        /// <summary>
        /// Measures the override.
        /// </summary>
        /// <param name="availableSize">Size of the available.</param>
        /// <returns>Size.</returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            foreach (var button in Children.OfType<HamburgerMenuButton>()) button.IsExpanded = Expanded != HamburgerMenuState.Collapsed;
            foreach (var child in Children.OfType<UIElement>()) child.Measure(availableSize);
            var height = Children.OfType<UIElement>().Sum(child => Math.Max(child.DesiredSize.Height, 48d));
            var width = 48d;
            if (Expanded == HamburgerMenuState.PermanentlyExpanded) width = (int)Children.OfType<UIElement>().Max(child => Math.Max(child.DesiredSize.Width, 48d));
            return new Size(width, double.IsInfinity(availableSize.Height) ? height : availableSize.Height);
        }

        /// <summary>
        /// Arranges the override.
        /// </summary>
        /// <param name="finalSize">The final size.</param>
        /// <returns>Size.</returns>
        protected override Size ArrangeOverride(Size finalSize)
        {
            var currentTop = 0d;
            var width = finalSize.Width;
            if (Expanded != HamburgerMenuState.Collapsed) width = Children.OfType<UIElement>().Max(child => Math.Max(child.DesiredSize.Width, 150d));
            foreach (var child in Children.OfType<UIElement>())
            {
                if (child is HamburgerMenuTopButton && Expanded != HamburgerMenuState.PermanentlyExpanded)
                    child.Arrange(new Rect(0d, currentTop, 48d, Math.Max(child.DesiredSize.Height, 48d)));
                else
                    child.Arrange(new Rect(0d, currentTop, width, Math.Max(child.DesiredSize.Height, 48d)));
                currentTop += Math.Max(child.DesiredSize.Height, 48d);
            }
            return new Size(finalSize.Width, double.IsInfinity(finalSize.Height) ? currentTop : Math.Max(finalSize.Height, currentTop));
        }
    }

    /// <summary>
    /// Button control used in hamburger menus
    /// </summary>
    public class HamburgerMenuButton : Button
    {
        /// <summary>
        /// Gets or sets the caption.
        /// </summary>
        /// <value>The caption.</value>
        public string Caption
        {
            get { return (string) GetValue(CaptionProperty); }
            set { SetValue(CaptionProperty, value); }
        }

        /// <summary>
        /// The caption property
        /// </summary>
        public static readonly DependencyProperty CaptionProperty = DependencyProperty.Register("Caption", typeof (string), typeof (HamburgerMenuButton), new PropertyMetadata(string.Empty));

        /// <summary>
        /// Resource key to be used for the icon
        /// </summary>
        public string BrushResourceKey
        {
            get { return (string)GetValue(BrushResourceKeyProperty); }
            set { SetValue(BrushResourceKeyProperty, value); }
        }

        /// <summary>
        /// Resource key to be used for the icon
        /// </summary>
        public static readonly DependencyProperty BrushResourceKeyProperty = DependencyProperty.Register("BrushResourceKey", typeof(string), typeof(HamburgerMenuButton), new PropertyMetadata(""));

        /// <summary>
        /// Gets or sets a value indicating whether this instance is expanded.
        /// </summary>
        /// <value><c>true</c> if this instance is expanded; otherwise, <c>false</c>.</value>
        public bool IsExpanded
        {
            get { return (bool) GetValue(IsExpandedProperty); }
            set { SetValue(IsExpandedProperty, value); }
        }

        /// <summary>
        /// The is expanded property
        /// </summary>
        public static readonly DependencyProperty IsExpandedProperty = DependencyProperty.Register("IsExpanded", typeof (bool), typeof (HamburgerMenuButton), new PropertyMetadata(false));

        /// <summary>
        /// Indicates whether the menu has sub items
        /// </summary>
        /// <value><c>true</c> if this instance has sub items; otherwise, <c>false</c>.</value>
        public bool HasSubItems
        {
            get { return (bool)GetValue(HasSubItemsProperty); }
            set { SetValue(HasSubItemsProperty, value); }
        }
        /// <summary>
        /// Indicates whether the menu has sub items
        /// </summary>
        public static readonly DependencyProperty HasSubItemsProperty = DependencyProperty.Register("HasSubItems", typeof(bool), typeof(HamburgerMenuButton), new PropertyMetadata(false));

        /// <summary>
        /// Sub items.
        /// </summary>
        /// <value>The sub actions.</value>
        public IEnumerable<IViewAction> SubActions
        {
            get { return _subActions; }
            set
            {
                _subActions = value;
                HasSubItems = value != null;
            }
        }

        /// <summary>
        /// Called when [click].
        /// </summary>
        protected override void OnClick()
        {
            base.OnClick();

            if (Command != null || SubActions == null) return;

            var menu = new ContextMenu();
            var isFirst = true;

            var replacementBrushes = new ObservableResourceDictionary {{"CODE.Framework-Universe-IconForegroundBrush", FindResource("CODE.Framework-Universe-IconForegroundBrush")}};

            foreach (var action in SubActions.Where(a => a.Availability == ViewActionAvailabilities.Available && a.Visibility == Visibility.Visible))
            {
                var menuItem = new ViewActionMenuItem {DataContext = action, Command = action, Header = GetMenuTitle(action)};
                if (!isFirst && action.BeginGroup) menu.Items.Add(new Separator());
                if (action.ViewActionType == ViewActionTypes.Toggle)
                {
                    menuItem.IsCheckable = true;
                    menuItem.SetBinding(MenuItem.IsCheckedProperty, new Binding("IsChecked") {Source = action});
                }
                var icon = new ThemeIcon {ReplacementBrushes = replacementBrushes};
                icon.SetBinding(ThemeIcon.IconResourceKeyProperty, new Binding("BrushResourceKey"));
                menuItem.Icon = icon;
                HandleMenuShortcutKey(menuItem, action);
                menu.Items.Add(menuItem);
                isFirst = false;
            }
            if (IsExpanded)
                menu.Placement = PlacementMode.Right;
            else
            {
                menu.Placement = PlacementMode.Relative;
                menu.HorizontalOffset = 48d;
            }
            menu.PlacementTarget = this;
            menu.IsOpen = true;
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
        private IEnumerable<IViewAction> _subActions;

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
    }

    /// <summary>
    /// First button in the hamburger menu (the actual "hamburger")
    /// </summary>
    public class HamburgerMenuTopButton : HamburgerMenuButton
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HamburgerMenuTopButton"/> class.
        /// </summary>
        /// <param name="parentMenu">The parent menu.</param>
        public HamburgerMenuTopButton(ViewActionHamburgerMenu parentMenu)
        {
            Click += (s, e) =>
            {
                switch (parentMenu.Expanded)
                {
                    case HamburgerMenuState.Collapsed:
                        parentMenu.Expanded = !parentMenu.ExpandsPermanently ? HamburgerMenuState.Expanded : HamburgerMenuState.PermanentlyExpanded;
                        break;
                    case HamburgerMenuState.Expanded:
                        parentMenu.Expanded = HamburgerMenuState.Collapsed;
                        break;
                    case HamburgerMenuState.PermanentlyExpanded:
                        parentMenu.Expanded = HamburgerMenuState.Collapsed;
                        break;
                }
                var parentParent = parentMenu.Parent as UIElement;
                if (parentParent != null)
                {
                    parentParent.InvalidateMeasure();
                    parentParent.InvalidateArrange();
                }
            };
        }
    }

    /// <summary>
    /// Indicates the state of the hamburger menu
    /// </summary>
    public enum HamburgerMenuState
    {
        /// <summary>
        /// Collapsed (only icons are visible)
        /// </summary>
        Collapsed,
        /// <summary>
        /// Temporarily expanded (text labels overlay other content)
        /// </summary>
        Expanded,
        /// <summary>
        /// Permanently expanded (the menu occupies the full width, including labels)
        /// </summary>
        PermanentlyExpanded
    }
}

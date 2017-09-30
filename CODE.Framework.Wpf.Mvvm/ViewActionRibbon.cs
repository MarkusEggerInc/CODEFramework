using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using CODE.Framework.Wpf.Utilities;

namespace CODE.Framework.Wpf.Mvvm
{
    /// <summary>
    /// Special menu object that can be bound to a collection of view actions to automatically and dynamically populate the menu.
    /// </summary>
    public class ViewActionRibbon : TabControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ViewActionRibbon" /> class.
        /// </summary>
        public ViewActionRibbon()
        {
            SelectionChanged += (s, e) =>
            {
                if (!FirstPageIsSpecial) return;
                if (SelectedIndex == 0)
                {
                    IsSpecialFirstPageActive = true;
                    if (!IsLoaded && !_mustRaiseSpecialFirstPageActivateEventOnLoad)
                        _mustRaiseSpecialFirstPageActivateEventOnLoad = true;
                    if (!IsLoaded && _mustRaiseSpecialFirstPageDeactivateEventOnLoad)
                        _mustRaiseSpecialFirstPageDeactivateEventOnLoad = false;
                }
                else
                {
                    IsSpecialFirstPageActive = false;
                    if (!IsLoaded && _mustRaiseSpecialFirstPageActivateEventOnLoad)
                        _mustRaiseSpecialFirstPageActivateEventOnLoad = false;
                    if (!IsLoaded && !_mustRaiseSpecialFirstPageDeactivateEventOnLoad)
                        _mustRaiseSpecialFirstPageDeactivateEventOnLoad = true;

                    if (SelectedIndex > -1)
                        LastRegularIndex = SelectedIndex;
                }
                Visibility = SelectedIndex == 0 ? Visibility.Hidden : Visibility.Visible;
            };

            Loaded += (s, e) =>
            {
                if (_mustRaiseSpecialFirstPageDeactivateEventOnLoad)
                    RaiseEvent(new RoutedEventArgs(SpecialFirstPageDeactivateEvent));
                if (_mustRaiseSpecialFirstPageActivateEventOnLoad)
                    RaiseEvent(new RoutedEventArgs(SpecialFirstPageActivateEvent));
                if (_setSelectedIndexOnLoadComplete > -1)
                {
                    SelectedIndex = _setSelectedIndexOnLoadComplete;
                    _setSelectedIndexOnLoadComplete = -1;
                }
            };

            Initialized += (o, e) =>
            {
                var window = ElementHelper.FindParent<Window>(this) ?? ElementHelper.FindVisualTreeParent<Window>(this);
                if (window == null) return;
                window.PreviewKeyDown += (s, a) =>
                {
                    var key = a.Key;
                    var systemKey = a.SystemKey;
                    if (GetKeyboardShortcutsActive(this))
                    {
                        if (SelectedIndex < 0 || SelectedIndex >= Items.Count) return;
                        var page = Items[SelectedIndex] as RibbonPage;
                        if (page == null) return;
                        var panel = page.Content as RibbonPageLayoutPanel;
                        if (panel == null) return;
                        foreach (var child in panel.Children)
                        {
                            var button = child as RibbonButton;
                            if (button == null || string.IsNullOrEmpty(button.AccessKey) || button.AccessKey != a.Key.ToString() || button.Command == null) continue;
                            var command = button.Command as ViewAction;
                            if (command == null || !command.CanExecute(button.CommandParameter)) continue;
                            command.Execute(button.CommandParameter);
                            SetKeyboardShortcutsActive(window, false);
                            a.Handled = true;
                            return;
                        }
                    }
                    if ((key == Key.LeftAlt || key == Key.RightAlt) || (key == Key.System && (systemKey == Key.LeftAlt || systemKey == Key.RightAlt)))
                    {
                        if (!ReadyForStatusChange) return;
                        var state = !GetKeyboardShortcutsActive(window);
                        SetKeyboardShortcutsActive(window, state);
                        ReadyForStatusChange = !state;
                        a.Handled = true;
                    }
                    else if (key == Key.Escape)
                    {
                        if (!GetKeyboardShortcutsActive(this)) return;
                        SetKeyboardShortcutsActive(window, false);
                        ReadyForStatusChange = true;
                        a.Handled = true;
                    }
                };
                PreviewMouseDown += (s, a) =>
                {
                    if (!GetKeyboardShortcutsActive(this)) return;
                    SetKeyboardShortcutsActive(window, false);
                    ReadyForStatusChange = true;
                };
                window.PreviewMouseDown += (s, a) =>
                {
                    if (!GetKeyboardShortcutsActive(this)) return;
                    SetKeyboardShortcutsActive(window, false);
                    ReadyForStatusChange = true;
                };
                window.PreviewKeyUp += (s, a) =>
                {
                    var key = a.Key;
                    var systemKey = a.SystemKey;
                    if ((key == Key.LeftAlt || key == Key.RightAlt) || (key == Key.System && (systemKey == Key.LeftAlt || systemKey == Key.RightAlt)))
                        ReadyForStatusChange = true;
                };
            };
        }

        /// <summary>
        /// Background/theme brush used by the ribbon
        /// </summary>
        /// <value>The ribbon theme brush.</value>
        public Brush RibbonThemeBrush
        {
            get { return (Brush)GetValue(RibbonThemeBrushProperty); }
            set { SetValue(RibbonThemeBrushProperty, value); }
        }
        /// <summary>
        /// Background/theme brush used by the ribbon
        /// </summary>
        public static readonly DependencyProperty RibbonThemeBrushProperty = DependencyProperty.Register("RibbonThemeBrush", typeof(Brush), typeof(ViewActionRibbon), new PropertyMetadata(null));

        /// <summary>
        /// Background brush for the selected ribbon page
        /// </summary>
        /// <value>The ribbon selected page brush.</value>
        public Brush RibbonSelectedPageBrush
        {
            get { return (Brush)GetValue(RibbonSelectedPageBrushProperty); }
            set { SetValue(RibbonSelectedPageBrushProperty, value); }
        }
        /// <summary>
        /// Background brush for the selected ribbon page
        /// </summary>
        public static readonly DependencyProperty RibbonSelectedPageBrushProperty = DependencyProperty.Register("RibbonSelectedPageBrush", typeof(Brush), typeof(ViewActionRibbon), new PropertyMetadata(null));

        /// <summary>
        /// For internal use only
        /// </summary>
        protected bool ReadyForStatusChange = true;

        /// <summary>Indicates whether the user has pressed the ALT key and thus activated display of keyboard shortcuts</summary>
        public static readonly DependencyProperty KeyboardShortcutsActiveProperty = DependencyProperty.RegisterAttached("KeyboardShortcutsActive", typeof (bool), typeof (ViewActionRibbon), new FrameworkPropertyMetadata(false) {Inherits = true});

        /// <summary>Indicates whether the user has pressed the ALT key and thus activated display of keyboard shortcuts</summary>
        /// <param name="obj">The obj.</param>
        /// <returns>System.String.</returns>
        public static bool GetKeyboardShortcutsActive(DependencyObject obj)
        {
            return (bool) obj.GetValue(KeyboardShortcutsActiveProperty);
        }

        /// <summary>Indicates whether the user has pressed the ALT key and thus activated display of keyboard shortcuts</summary>
        public static void SetKeyboardShortcutsActive(DependencyObject obj, bool value)
        {
            obj.SetValue(KeyboardShortcutsActiveProperty, value);
        }

        /// <summary>
        /// Indicates whether the first ribbon page is to be handled differently as a file menu
        /// </summary>
        public bool FirstPageIsSpecial
        {
            get { return (bool) GetValue(FirstPageIsSpecialProperty); }
            set { SetValue(FirstPageIsSpecialProperty, value); }
        }

        /// <summary>
        /// Indicates whether the first ribbon page is to be handled differently as a file menu
        /// </summary>
        public static readonly DependencyProperty FirstPageIsSpecialProperty = DependencyProperty.Register("FirstPageIsSpecial", typeof (bool), typeof (ViewActionRibbon), new PropertyMetadata(true));

        /// <summary>
        /// For internal use only
        /// </summary>
        protected int LastRegularIndex = -1;

        /// <summary>
        /// Title for empty global category titles (default: File)
        /// </summary>
        public string EmptyGlobalCategoryTitle
        {
            get { return (string) GetValue(EmptyGlobalCategoryTitleProperty); }
            set { SetValue(EmptyGlobalCategoryTitleProperty, value); }
        }

        /// <summary>
        /// Title for empty global category titles (default: File)
        /// </summary>
        public static readonly DependencyProperty EmptyGlobalCategoryTitleProperty = DependencyProperty.Register("EmptyGlobalCategoryTitle", typeof (string), typeof (ViewActionRibbon), new PropertyMetadata("File"));

        /// <summary>
        /// Title for empty local category titles (default: File)
        /// </summary>
        public string EmptyLocalCategoryTitle
        {
            get { return (string) GetValue(EmptyLocalCategoryTitleProperty); }
            set { SetValue(EmptyLocalCategoryTitleProperty, value); }
        }

        /// <summary>
        /// Title for empty local category titles (default: File)
        /// </summary>
        public static readonly DependencyProperty EmptyLocalCategoryTitleProperty = DependencyProperty.Register("EmptyLocalCategoryTitle", typeof (string), typeof (ViewActionRibbon), new PropertyMetadata("File"));

        /// <summary>
        /// Indicates whether local categories (ribbon pages populated from local/individual view actions) shall use the special colors
        /// </summary>
        public bool HighlightLocalCategories
        {
            get { return (bool) GetValue(HighlightLocalCategoriesProperty); }
            set { SetValue(HighlightLocalCategoriesProperty, value); }
        }

        /// <summary>
        /// Indicates whether local categories (ribbon pages populated from local/individual view actions) shall use the special colors
        /// </summary>
        public static readonly DependencyProperty HighlightLocalCategoriesProperty = DependencyProperty.Register("HighlightLocalCategories", typeof (bool), typeof (ViewActionRibbon), new PropertyMetadata(true));

        /// <summary>
        /// Indicates whether the first special ribbon page is active
        /// </summary>
        public bool IsSpecialFirstPageActive
        {
            get { return (bool) GetValue(IsSpecialFirstPageActiveProperty); }
            set { SetValue(IsSpecialFirstPageActiveProperty, value); }
        }

        /// <summary>
        /// Indicates whether the first special ribbon page is active
        /// </summary>
        public static readonly DependencyProperty IsSpecialFirstPageActiveProperty = DependencyProperty.Register("IsSpecialFirstPageActive", typeof (bool), typeof (ViewActionRibbon), new PropertyMetadata(false, IsSpecialFirstPageActiveChanged));

        /// <summary>
        /// Determines whether [is special first page active changed] [the specified dependency object].
        /// </summary>
        /// <param name="d">The dependency object.</param>
        /// <param name="args">The <see cref="DependencyPropertyChangedEventArgs" /> instance containing the event data.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        private static void IsSpecialFirstPageActiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            var ribbon = d as ViewActionRibbon;
            if (ribbon == null) return;
            var active = (bool) args.NewValue;
            if (!active)
            {
                ribbon.SelectedIndex = ribbon.LastRegularIndex;
                ribbon.Visibility = Visibility.Visible;
                if (ribbon.IsLoaded)
                    ribbon.RaiseEvent(new RoutedEventArgs(SpecialFirstPageDeactivateEvent));
                else
                    ribbon._mustRaiseSpecialFirstPageDeactivateEventOnLoad = true;
            }
            else
            {
                if (ribbon.IsLoaded)
                    ribbon.RaiseEvent(new RoutedEventArgs(SpecialFirstPageActivateEvent));
                else
                    ribbon._mustRaiseSpecialFirstPageActivateEventOnLoad = true;
            }
        }

        private bool _mustRaiseSpecialFirstPageActivateEventOnLoad;

        private bool _mustRaiseSpecialFirstPageDeactivateEventOnLoad;

        /// <summary>
        /// Occurs when the special first page is activated
        /// </summary>
        public static readonly RoutedEvent SpecialFirstPageActivateEvent = EventManager.RegisterRoutedEvent("SpecialFirstPageActivate", RoutingStrategy.Bubble, typeof (RoutedEventHandler), typeof (ViewActionRibbon));

        /// <summary>
        /// Occurs when the special first page is activated
        /// </summary>
        public event RoutedEventHandler SpecialFirstPageActivate
        {
            add { AddHandler(SpecialFirstPageActivateEvent, value); }
            remove { RemoveHandler(SpecialFirstPageActivateEvent, value); }
        }

        /// <summary>
        /// Occurs when the special first page is deactivated
        /// </summary>
        public static readonly RoutedEvent SpecialFirstPageDeactivateEvent = EventManager.RegisterRoutedEvent("SpecialFirstPageDeactivate", RoutingStrategy.Bubble, typeof (RoutedEventHandler), typeof (ViewActionRibbon));

        /// <summary>
        /// Occurs when the special first page is activated
        /// </summary>
        public event RoutedEventHandler SpecialFirstPageDeactivate
        {
            add { AddHandler(SpecialFirstPageDeactivateEvent, value); }
            remove { RemoveHandler(SpecialFirstPageDeactivateEvent, value); }
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
        public static readonly DependencyProperty ModelProperty = DependencyProperty.Register("Model", typeof (object), typeof (ViewActionRibbon), new UIPropertyMetadata(null, ModelChanged));

        /// <summary>
        /// Change handler for model property
        /// </summary>
        /// <param name="d">The dependency object that triggered this change.</param>
        /// <param name="e">The <see cref="System.Windows.DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void ModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ribbon = d as ViewActionRibbon;
            if (ribbon == null) return;
            ribbon.RepopulateRibbon(e.NewValue);
        }

        private void RepopulateRibbon(object model)
        {
            var actionsContainer = model as IHaveActions;
            if (actionsContainer != null && actionsContainer.Actions != null)
            {
                actionsContainer.Actions.CollectionChanged += (s, e2) => PopulateRibbon(actionsContainer);
                Visibility = Visibility.Visible;
                PopulateRibbon(actionsContainer);
            }
            else
                Visibility = Visibility.Collapsed;
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
        public static readonly DependencyProperty SelectedViewProperty = DependencyProperty.Register("SelectedView", typeof (object), typeof (ViewActionRibbon), new UIPropertyMetadata(null, SelectedViewChanged));

        /// <summary>
        /// Change handler for selected view property
        /// </summary>
        /// <param name="d">The dependency object that triggered this change.</param>
        /// <param name="e">The <see cref="System.Windows.DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void SelectedViewChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d == null) return;
            var ribbon = d as ViewActionRibbon;
            if (ribbon == null) return;
            var viewResult = e.NewValue as ViewResult;
            if (viewResult == null)
            {
                ribbon.PopulateRibbon(ribbon.Model as IHaveActions);
                return;
            }

            var actionsContainer = viewResult.Model as IHaveActions;
            if (actionsContainer != null)
            {
                actionsContainer.Actions.CollectionChanged += (s, e2) => ribbon.PopulateRibbon(ribbon.Model as IHaveActions, actionsContainer);
                ribbon.PopulateRibbon(ribbon.Model as IHaveActions, actionsContainer, viewResult.ViewTitle);
            }
            else
                ribbon.PopulateRibbon(ribbon.Model as IHaveActions);
        }

        /// <summary>
        /// If set to true, the top level menu items will be forced to be upper case
        /// </summary>
        /// <value><c>true</c> if [force top level menu items upper case]; otherwise, <c>false</c>.</value>
        public bool ForceTopLevelTitlesUpperCase
        {
            get { return (bool) GetValue(ForceTopLevelTitlesUpperCaseProperty); }
            set { SetValue(ForceTopLevelTitlesUpperCaseProperty, value); }
        }

        /// <summary>
        /// If set to true, the top level menu items will be forced to be upper case
        /// </summary>
        public static readonly DependencyProperty ForceTopLevelTitlesUpperCaseProperty = DependencyProperty.Register("ForceTopLevelTitlesUpperCase", typeof (bool), typeof (ViewActionRibbon), new PropertyMetadata(true, ForceTopLevelTitlesUpperCaseChanged));

        private static void ForceTopLevelTitlesUpperCaseChanged(DependencyObject d, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            var ribbon = d as ViewActionRibbon;
            if (ribbon != null) ribbon.PopulateRibbon(ribbon.Model as IHaveActions);
        }

        /// <summary>
        /// Attached property used to reference the command/action on any custom ribbon item
        /// </summary>
        public static readonly DependencyProperty RibbonItemCommandProperty = DependencyProperty.RegisterAttached("RibbonItemCommand", typeof (ICommand), typeof (ViewActionRibbon), new PropertyMetadata(null));

        /// <summary>
        /// Attached property used to reference the command/action on any custom ribbon item
        /// </summary>
        /// <param name="d">The dependency object the item is set on</param>
        /// <returns>ICommand.</returns>
        public static ICommand GetRibbonItemCommand(DependencyObject d)
        {
            return (ICommand) d.GetValue(RibbonItemCommandProperty);
        }

        /// <summary>
        /// Attached property used to reference the command/action on any custom ribbon item
        /// </summary>
        /// <param name="d">The dependency object the item is set on</param>
        /// <param name="value">The value.</param>
        public static void SetRibbonItemCommand(DependencyObject d, ICommand value)
        {
            d.SetValue(RibbonItemCommandProperty, value);
        }

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
        public static readonly DependencyProperty ViewActionPolicyProperty = DependencyProperty.Register("ViewActionPolicy", typeof(IViewActionPolicy), typeof(ViewActionRibbon), new PropertyMetadata(null));

        /// <summary>
        /// Populates the current ribbon with items based on the actions collection
        /// </summary>
        /// <param name="actions">List of primary actions</param>
        /// <param name="actions2">List of view specific actions</param>
        /// <param name="selectedViewTitle">The selected view title.</param>
        protected virtual void PopulateRibbon(IHaveActions actions, IHaveActions actions2 = null, string selectedViewTitle = "")
        {
            var oldSelectedPage = SelectedIndex;

            RemoveAllMenuKeyBindings();
            Items.Clear();
            if (actions == null) return;

            var actionList = ViewActionPolicy != null ? ViewActionPolicy.GetConsolidatedActions(actions, actions2, selectedViewTitle, viewModel: Model) : ViewActionHelper.GetConsolidatedActions(actions, actions2, selectedViewTitle);
            var rootCategories = ViewActionPolicy != null ? ViewActionPolicy.GetTopLevelActionCategories(actionList, EmptyGlobalCategoryTitle, EmptyLocalCategoryTitle, viewModel: Model) : ViewActionHelper.GetTopLevelActionCategories(actionList, EmptyGlobalCategoryTitle, EmptyLocalCategoryTitle);

            var pageCounter = 0;
            var selectedIndex = -1;
            var standardSelectedIndexSet = false;
            var specialSelectedIndexSet = false;

            var viewActionCategories = rootCategories as ViewActionCategory[] ?? rootCategories.ToArray();
            foreach (var category in viewActionCategories)
            {
                RibbonPage tab;
                var tabVisibilityBinding = new MultiBinding {Converter = new MaximumVisibilityMultiConverter()};
                if (category.IsLocalCategory && HighlightLocalCategories)
                {
                    var caption = category.Caption;
                    if (string.IsNullOrEmpty(caption)) caption = ForceTopLevelTitlesUpperCase ? selectedViewTitle.Trim().ToUpper() : selectedViewTitle.Trim();
                    tab = new RibbonSpecialPage {Header = caption};
                    if (!specialSelectedIndexSet)
                    {
                        selectedIndex = pageCounter;
                        specialSelectedIndexSet = true;
                    }
                }
                else
                {
                    if (pageCounter == 0 && FirstPageIsSpecial)
                        tab = new RibbonFirstPage {Header = ForceTopLevelTitlesUpperCase ? category.Caption.Trim().ToUpper() : category.Caption.Trim()};
                    else
                    {
                        tab = new RibbonPage {Header = ForceTopLevelTitlesUpperCase ? category.Caption.Trim().ToUpper() : category.Caption.Trim()};
                        if (!standardSelectedIndexSet && !specialSelectedIndexSet)
                        {
                            selectedIndex = pageCounter;
                            standardSelectedIndexSet = true;
                        }
                    }
                }
                var items = new RibbonPageLayoutPanel();
                tab.Content = items;
                PopulateSubCategories(items, category, actionList, ribbonPage: tab, visibilityBinding: tabVisibilityBinding);
                Items.Add(tab);
                tab.SetBinding(VisibilityProperty, tabVisibilityBinding);

                if (category.AccessKey != ' ')
                {
                    var pageIndexToSelect = pageCounter;
                    var pageAccessKey = (Key) Enum.Parse(typeof (Key), category.AccessKey.ToString(CultureInfo.InvariantCulture).ToUpper());
                    MenuKeyBindings.Add(new ViewActionMenuKeyBinding(new ViewAction(execute: (a, o) =>
                    {
                        SelectedIndex = pageIndexToSelect;
                        var window = ElementHelper.FindParent<Window>(this) ?? ElementHelper.FindVisualTreeParent<Window>(this);
                        SetKeyboardShortcutsActive(window, true);
                        ReadyForStatusChange = false;
                    })
                    {
                        ShortcutKey = pageAccessKey,
                        ShortcutModifiers = ModifierKeys.Alt
                    }));

                    tab.PageAccessKey = category.AccessKey.ToString(CultureInfo.InvariantCulture).Trim().ToUpper();
                }

                pageCounter++;
            }

            // We are checking for a selected default page
            pageCounter = 0;
            if (actionList.Count(a => a.IsDefaultSelection) > 0)
                foreach (var category in viewActionCategories)
                {
                    var matchingActions = ViewActionPolicy != null ? ViewActionPolicy.GetAllActionsForCategory(actionList, category, viewModel: Model) : ViewActionHelper.GetAllActionsForCategory(actionList, category);
                    foreach (var matchingAction in matchingActions)
                        if (matchingAction.IsDefaultSelection)
                        {
                            selectedIndex = pageCounter;
                            break;
                        }
                    pageCounter++;
                }

            if (selectedIndex == -1) selectedIndex = oldSelectedPage;
            if (selectedIndex == -1) selectedIndex = 0;
            if (selectedIndex >= Items.Count) selectedIndex = Items.Count - 1;

            LastRegularIndex = selectedIndex;
            SelectedIndex = selectedIndex;
            _setSelectedIndexOnLoadComplete = !IsLoaded ? SelectedIndex : -1;

            CreateAllMenuKeyBindings();
        }

        /// <summary>
        /// Called after tabs are created. Can be overridden in subclasses.
        /// </summary>
        protected virtual void AfterTabCreated()
        {

        }

        /// <summary>
        /// Adds sub-items for the specified tab item and category
        /// </summary>
        /// <param name="parentPanel">Parent item container</param>
        /// <param name="category">Category we are interested in</param>
        /// <param name="actions">Actions to consider</param>
        /// <param name="indentLevel">Current hierarchical indentation level</param>
        /// <param name="ribbonPage">The ribbon page.</param>
        /// <param name="visibilityBinding">The visibility binding.</param>
        protected virtual void PopulateSubCategories(Panel parentPanel, ViewActionCategory category, IEnumerable<IViewAction> actions, int indentLevel = 0, RibbonPage ribbonPage = null, MultiBinding visibilityBinding = null)
        {
            var populatedCategories = new List<string>();
            if (actions == null) return;
            var viewActions = actions as IViewAction[] ?? actions.ToArray();
            var matchingActions = ViewActionPolicy != null ? ViewActionPolicy.GetAllActionsForCategory(viewActions, category, indentLevel, EmptyGlobalCategoryTitle, false, Model) : ViewActionHelper.GetAllActionsForCategory(viewActions, category, indentLevel, EmptyGlobalCategoryTitle, false);
            var addedRibbonItems = 0;
            foreach (var matchingAction in matchingActions)
            {
                if (addedRibbonItems > 0 && matchingAction.BeginGroup) parentPanel.Children.Add(new RibbonSeparator());

                if (matchingAction.Categories != null && matchingAction.Categories.Count > indentLevel + 1 && !populatedCategories.Contains(matchingAction.Categories[indentLevel].Id)) // This is further down in a sub-category even
                {
                    // TODO: Add a drop-down menu capable button
                    populatedCategories.Add(matchingAction.Categories[indentLevel].Id);
                    var newRibbonButton = new RibbonButtonLarge {Content = matchingAction.Categories[indentLevel + 1].Caption, Visibility = matchingAction.Visibility};
                    CreateMenuItemBinding(matchingAction, newRibbonButton);
                    if (visibilityBinding != null) visibilityBinding.Bindings.Add(new Binding("Visibility") {Source = newRibbonButton});
                    PopulateSubCategories(parentPanel, matchingAction.Categories[indentLevel], viewActions, indentLevel + 1);
                    newRibbonButton.AccessKey = matchingAction.AccessKey.ToString(CultureInfo.CurrentUICulture).Trim().ToUpper();
                    parentPanel.Children.Add(newRibbonButton);
                    addedRibbonItems++;
                }
                else
                {
                    if (matchingAction.ActionView != null)
                    {
                        if (matchingAction.ActionViewModel != null && matchingAction.ActionView.DataContext == null) matchingAction.ActionView.DataContext = matchingAction.ActionViewModel;
                        var command = GetRibbonItemCommand(matchingAction.ActionView);
                        if (command == null) SetRibbonItemCommand(matchingAction.ActionView, matchingAction);
                        if (visibilityBinding != null) visibilityBinding.Bindings.Add(new Binding("Visibility") {Source = matchingAction.ActionView});
                        matchingAction.ActionView.IsVisibleChanged += (s, e) => InvalidateAll();

                        var isInFirstPage = false;
                        if (ribbonPage != null)
                            isInFirstPage = ribbonPage is RibbonFirstPage;

                        if (!isInFirstPage)
                        {
                            ElementHelper.DetachElementFromParent(matchingAction.ActionView);
                            if (matchingAction.ActionView.Style == null)
                            {
                                var resource = TryFindResource("CODE.Framework-Ribbon-CustomControlContainerStyle");
                                if (resource != null)
                                {
                                    var genericStyle = resource as Style;
                                    if (genericStyle != null)
                                        matchingAction.ActionView.Style = genericStyle;
                                }
                            }
                            parentPanel.Children.Add(matchingAction.ActionView);
                        }
                    }
                    else
                    {
                        RibbonButton newRibbonButton;
                        if (matchingAction.Significance == ViewActionSignificance.AboveNormal || matchingAction.Significance == ViewActionSignificance.Highest)
                            newRibbonButton = new RibbonButtonLarge
                            {
                                Content = matchingAction.Caption,
                                Command = matchingAction,
                                Visibility = matchingAction.Visibility
                            };
                        else
                            newRibbonButton = new RibbonButtonSmall
                            {
                                Content = matchingAction.Caption,
                                Command = matchingAction,
                                Visibility = matchingAction.Visibility
                            };
                        newRibbonButton.IsVisibleChanged += (s, e) => InvalidateAll();
                        newRibbonButton.SetBinding(ContentControl.ContentProperty, new Binding("Caption") {Source = matchingAction, Mode = BindingMode.OneWay});
                        CreateMenuItemBinding(matchingAction, newRibbonButton);
                        if (visibilityBinding != null) visibilityBinding.Bindings.Add(new Binding("Visibility") {Source = newRibbonButton});
                        if (matchingAction.ViewActionType == ViewActionTypes.Toggle) newRibbonButton.SetBinding(RibbonButton.IsCheckedProperty, new Binding("IsChecked") {Source = matchingAction});
                        if (matchingAction.AccessKey != ' ') newRibbonButton.AccessKey = matchingAction.AccessKey.ToString(CultureInfo.CurrentUICulture).Trim().ToUpper();
                        parentPanel.Children.Add(newRibbonButton);
                        HandleRibbonShortcutKey(newRibbonButton, matchingAction, ribbonPage);
                    }
                    addedRibbonItems++;
                }
            }
            if (addedRibbonItems > 0) parentPanel.Children.Add(new RibbonSeparator());
        }

        private void InvalidateAll()
        {
            InvalidateMeasure();
            InvalidateArrange();
            InvalidateVisual();

            foreach (var tab in Items)
            {
                var tabItem = tab as TabItem;
                if (tabItem == null) continue;
                tabItem.InvalidateMeasure();
                tabItem.InvalidateArrange();
                tabItem.InvalidateVisual();
                if (tabItem.Content == null) continue;
                var tabContent = tabItem.Content as FrameworkElement;
                if (tabContent == null) continue;
                tabContent.InvalidateMeasure();
                tabContent.InvalidateArrange();
                tabContent.InvalidateVisual();
            }
        }

        /// <summary>
        /// Handles the assignment of shortcut keys
        /// </summary>
        /// <param name="button">The button.</param>
        /// <param name="action">The category.</param>
        /// <param name="ribbonPage">The ribbon page.</param>
        protected virtual void HandleRibbonShortcutKey(RibbonButton button, IViewAction action, RibbonPage ribbonPage)
        {
            if (action.ShortcutKey == Key.None) return;
            MenuKeyBindings.Add(new ViewActionMenuKeyBinding(action));
        }

        /// <summary>
        /// For internal use only
        /// </summary>
        protected readonly List<ViewActionMenuKeyBinding> MenuKeyBindings = new List<ViewActionMenuKeyBinding>();

        /// <summary>
        /// For internal use only
        /// </summary>
        private int _setSelectedIndexOnLoadComplete = -1;

        /// <summary>
        /// Removes all key bindings from the current window that were associated with a view category menu
        /// </summary>
        protected virtual void CreateAllMenuKeyBindings()
        {
            var window = ElementHelper.FindVisualTreeParent<Window>(this);
            if (window == null) return;

            foreach (var binding in MenuKeyBindings)
                window.InputBindings.Add(binding);
        }

        /// <summary>
        /// Removes all key bindings from the current window that were associated with a view category menu
        /// </summary>
        protected virtual void RemoveAllMenuKeyBindings()
        {
            MenuKeyBindings.Clear();

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
        /// Creates the menu item binding.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="ribbonButton">The ribbon button.</param>
        private void CreateMenuItemBinding(IViewAction action, FrameworkElement ribbonButton)
        {
            var binding = new MultiBinding {Converter = new MinimumVisibilityMultiConverter()};
            binding.Bindings.Add(new Binding("Availability") {Source = action, Converter = new AvailabilityToVisibleConverter()});
            binding.Bindings.Add(new Binding("Visibility") {Source = action});
            ribbonButton.SetBinding(VisibilityProperty, binding);
        }
    }

    /// <summary>
    /// Ribbon page
    /// </summary>
    public class RibbonPage : TabItem
    {
        /// <summary>Access key to be displayed for the page</summary>
        public string PageAccessKey
        {
            get { return (string) GetValue(PageAccessKeyProperty); }
            set { SetValue(PageAccessKeyProperty, value); }
        }

        /// <summary>Access key to be displayed for the page</summary>
        public static readonly DependencyProperty PageAccessKeyProperty = DependencyProperty.Register("PageAccessKey", typeof (string), typeof (RibbonPage), new PropertyMetadata(string.Empty, OnPageAccessKeyChanged));

        /// <summary>Fires when the page access key changes</summary>
        /// <param name="d">The dependency object.</param>
        /// <param name="args">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void OnPageAccessKeyChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            var page = d as RibbonPage;
            if (page == null) return;
            page.PageAccessKeySet = !string.IsNullOrEmpty(args.NewValue.ToString());
        }

        /// <summary>Indicates whether a page access key has been set</summary>
        /// <value><c>true</c> if [page access key set]; otherwise, <c>false</c>.</value>
        public bool PageAccessKeySet
        {
            get { return (bool) GetValue(PageAccessKeySetProperty); }
            set { SetValue(PageAccessKeySetProperty, value); }
        }

        /// <summary>Indicates whether a page access key has been set</summary>
        public static readonly DependencyProperty PageAccessKeySetProperty = DependencyProperty.Register("PageAccessKeySet", typeof (bool), typeof (RibbonPage), new PropertyMetadata(false));
    }

    /// <summary>
    /// Special page class for the first page in a ribbon
    /// </summary>
    public class RibbonFirstPage : RibbonPage
    {
    }

    /// <summary>
    /// Special page class for special pages in a ribbon
    /// </summary>
    public class RibbonSpecialPage : RibbonPage
    {
    }

    /// <summary>
    /// Default ribbon button
    /// </summary>
    public class RibbonButton : Button
    {
        /// <summary>Access key to be displayed for the button</summary>
        public string AccessKey
        {
            get { return (string) GetValue(AccessKeyProperty); }
            set { SetValue(AccessKeyProperty, value); }
        }

        /// <summary>Access key to be displayed for the button</summary>
        public static readonly DependencyProperty AccessKeyProperty = DependencyProperty.Register("AccessKey", typeof (string), typeof (RibbonButton), new PropertyMetadata(string.Empty, OnAccessKeyChanged));

        /// <summary>Fires when the access key changes</summary>
        /// <param name="d">The dependency object.</param>
        /// <param name="args">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void OnAccessKeyChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            var button = d as RibbonButton;
            if (button == null) return;
            button.AccessKeySet = !string.IsNullOrEmpty(args.NewValue.ToString());
        }

        /// <summary>Indicates whether a access key has been set</summary>
        /// <value><c>true</c> if [access key set]; otherwise, <c>false</c>.</value>
        public bool AccessKeySet
        {
            get { return (bool) GetValue(AccessKeySetProperty); }
            set { SetValue(AccessKeySetProperty, value); }
        }

        /// <summary>Indicates whether a page access key has been set</summary>
        public static readonly DependencyProperty AccessKeySetProperty = DependencyProperty.Register("AccessKeySet", typeof (bool), typeof (RibbonButton), new PropertyMetadata(false));

        /// <summary>Indicates whether the button is to be rendered as "checked" (often used in Toggle-style actions)</summary>
        public bool IsChecked
        {
            get { return (bool) GetValue(IsCheckedProperty); }
            set { SetValue(IsCheckedProperty, value); }
        }

        /// <summary>Indicates whether the button is to be rendered as "checked" (often used in Toggle-style actions)</summary>
        public static readonly DependencyProperty IsCheckedProperty = DependencyProperty.Register("IsChecked", typeof (bool), typeof (RibbonButton), new PropertyMetadata(false));
    }

    /// <summary>
    /// Large button element used in ribbons
    /// </summary>
    public class RibbonButtonLarge : RibbonButton
    {
    }

    /// <summary>
    /// Small button element used in ribbons
    /// </summary>
    public class RibbonButtonSmall : RibbonButton
    {
    }

    /// <summary>
    /// Separator element used in ribbons
    /// </summary>
    public class RibbonSeparator : Control
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RibbonSeparator"/> class.
        /// </summary>
        public RibbonSeparator()
        {
            Focusable = false;
        }
    }

    /// <summary>
    /// This panel is used to lay out items within a ribbon page
    /// </summary>
    public class RibbonPageLayoutPanel : Panel
    {
        /// <summary>Font Size used to render group titles</summary>
        public double GroupTitleFontSize
        {
            get { return (double) GetValue(GroupTitleFontSizeProperty); }
            set { SetValue(GroupTitleFontSizeProperty, value); }
        }

        /// <summary>Font Size used to render group titles</summary>
        public static readonly DependencyProperty GroupTitleFontSizeProperty = DependencyProperty.Register("GroupTitleFontSize", typeof (double), typeof (RibbonPageLayoutPanel), new PropertyMetadata(10d));

        /// <summary>Font family used to render group titles</summary>
        public FontFamily GroupTitleFontFamily
        {
            get { return (FontFamily) GetValue(GroupTitleFontFamilyProperty); }
            set { SetValue(GroupTitleFontFamilyProperty, value); }
        }

        /// <summary>Font family used to render group titles</summary>
        public static readonly DependencyProperty GroupTitleFontFamilyProperty = DependencyProperty.Register("GroupTitleFontFamily", typeof (FontFamily), typeof (RibbonPageLayoutPanel), new PropertyMetadata(new FontFamily("Segoe UI")));

        /// <summary>Font weight used to render group titles</summary>
        public FontWeight GroupTitleFontWeight
        {
            get { return (FontWeight) GetValue(GroupTitleFontWeightProperty); }
            set { SetValue(GroupTitleFontWeightProperty, value); }
        }

        /// <summary>Font weight used to render group titles</summary>
        public static readonly DependencyProperty GroupTitleFontWeightProperty = DependencyProperty.Register("GroupTitleFontWeight", typeof (FontWeight), typeof (RibbonPageLayoutPanel), new PropertyMetadata(FontWeights.Normal));

        /// <summary>Foreground brush used to render group titles</summary>
        public Brush GroupTitleForegroundBrush
        {
            get { return (Brush) GetValue(GroupTitleForegroundBrushProperty); }
            set { SetValue(GroupTitleForegroundBrushProperty, value); }
        }

        /// <summary>Foreground brush used to render group titles</summary>
        public static readonly DependencyProperty GroupTitleForegroundBrushProperty = DependencyProperty.Register("GroupTitleForegroundBrush", typeof (Brush), typeof (RibbonPageLayoutPanel), new PropertyMetadata(Brushes.Black));

        /// <summary>Foreground brush opacity used to render group titles</summary>
        public double GroupTitleForegroundBrushOpacity
        {
            get { return (double) GetValue(GroupTitleForegroundBrushOpacityProperty); }
            set { SetValue(GroupTitleForegroundBrushOpacityProperty, value); }
        }

        /// <summary>Foreground brush opacity used to render group titles</summary>
        public static readonly DependencyProperty GroupTitleForegroundBrushOpacityProperty = DependencyProperty.Register("GroupTitleForegroundBrushOpacity", typeof (double), typeof (RibbonPageLayoutPanel), new PropertyMetadata(.6d));

        /// <summary>
        /// When overridden in a derived class, measures the size in layout required for child elements and determines a size for the <see cref="T:System.Windows.FrameworkElement" />-derived class.
        /// </summary>
        /// <param name="availableSize">The available size that this element can give to child elements. Infinity can be specified as a value to indicate that the element will size to whatever content is available.</param>
        /// <returns>The size that this element determines it needs during layout, based on its calculations of child element sizes.</returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            var availableWitdh = availableSize.Width;
            var availableHeight = availableSize.Height;
            if (double.IsInfinity(availableSize.Height))
                availableHeight = Height > 0 ? Height : 70;
            else
                availableHeight -= 18;
            var availableSizeForControls = new Size(availableWitdh, availableHeight);

            foreach (UIElement child in Children)
                if (child is RibbonSeparator)
                    child.Measure(availableSize);
                else
                    child.Measure(availableSizeForControls);

            var left = 0d;
            var top = 0d;
            var lastLargestWidth = 0d;
            var lastElementWasFullHeight = true;


            foreach (UIElement child in Children)
            {
                var largeButton = child as RibbonButtonLarge;
                var smallButton = child as RibbonButtonSmall;
                var separator = child as RibbonSeparator;

                if (largeButton != null)
                {
                    top = 0d;
                    left += lastLargestWidth;
                    lastElementWasFullHeight = true;
                    lastLargestWidth = largeButton.DesiredSize.Width;
                }
                else if (smallButton != null)
                {
                    if (lastElementWasFullHeight)
                    {
                        left += lastLargestWidth;
                        lastLargestWidth = 0d;
                        top = 0d;
                    }

                    if (top + smallButton.DesiredSize.Height > availableHeight)
                    {
                        left += lastLargestWidth;
                        lastLargestWidth = 0d;
                        top = 0d;
                    }
                    lastElementWasFullHeight = false;
                    lastLargestWidth = Math.Max(lastLargestWidth, smallButton.DesiredSize.Width);
                    top += smallButton.DesiredSize.Height;
                }
                else if (separator != null)
                {
                    top = 0d;
                    left += lastLargestWidth;
                    lastElementWasFullHeight = true;
                    lastLargestWidth = separator.DesiredSize.Width;
                }
                else if (child != null)
                {
                    if (lastElementWasFullHeight)
                    {
                        left += lastLargestWidth;
                        lastLargestWidth = 0d;
                        top = 0d;
                    }

                    var childHeight = child.DesiredSize.Height;
                    if (childHeight >= availableHeight)
                    {
                        childHeight = availableHeight;
                        lastElementWasFullHeight = true;
                    }
                    else
                        lastElementWasFullHeight = false;
                    if (top + childHeight > availableHeight)
                    {
                        left += lastLargestWidth;
                        lastLargestWidth = 0d;
                        top = 0d;
                    }
                    lastLargestWidth = Math.Max(lastLargestWidth, child.DesiredSize.Width);
                    top += childHeight;
                }
            }

            left += lastLargestWidth;

            var baseSize = base.MeasureOverride(availableSize);
            var finalHeight = baseSize.Height;
            if (finalHeight < 1 || double.IsNaN(finalHeight)) finalHeight = Height;
            if (finalHeight < 1 || double.IsNaN(finalHeight)) finalHeight = 70;

            return new Size(left, finalHeight);
        }

        private readonly List<GroupTitleRenderInfo> _groupTitles = new List<GroupTitleRenderInfo>();

        /// <summary>
        /// When overridden in a derived class, positions child elements and determines a size for a <see cref="T:System.Windows.FrameworkElement" /> derived class.
        /// </summary>
        /// <param name="finalSize">The final area within the parent that this element should use to arrange itself and its children.</param>
        /// <returns>The actual size used.</returns>
        protected override Size ArrangeOverride(Size finalSize)
        {
            _groupTitles.Clear();

            var left = 0d;
            var top = 0d;
            var lastLargestWidth = 0d;
            var lastElementWasFullHeight = true;

            var availableHeight = finalSize.Height;
            if (double.IsInfinity(finalSize.Height))
                availableHeight = Height > 0 ? Height : 70;
            else
                availableHeight -= 14;

            var currentGroupLeft = 0d;
            var isGroupStart = true;
            GroupTitleRenderInfo currentRenderInfo = null;

            foreach (UIElement child in Children)
            {
                var largeButton = child as RibbonButtonLarge;
                var smallButton = child as RibbonButtonSmall;
                var separator = child as RibbonSeparator;

                if (isGroupStart)
                {
                    currentGroupLeft = left;
                    currentRenderInfo = new GroupTitleRenderInfo();
                    if (largeButton != null && largeButton.Command != null)
                    {
                        var action = largeButton.Command as IViewAction;
                        if (action != null) currentRenderInfo.GroupTitle = action.GroupTitle;
                    }
                    else if (smallButton != null && smallButton.Command != null)
                    {
                        var action = smallButton.Command as IViewAction;
                        if (action != null) currentRenderInfo.GroupTitle = action.GroupTitle;
                    }
                    else if (child != null)
                    {
                        var action = ViewActionRibbon.GetRibbonItemCommand(child) as IViewAction;
                        if (action != null) currentRenderInfo.GroupTitle = action.GroupTitle;
                    }
                    _groupTitles.Add(currentRenderInfo);
                    isGroupStart = false;
                }

                if (largeButton != null)
                {
                    top = 0d;
                    left += lastLargestWidth;
                    lastElementWasFullHeight = true;
                    lastLargestWidth = largeButton.IsVisible ? largeButton.DesiredSize.Width : 0d;
                    if (largeButton.IsVisible) largeButton.Arrange(new Rect(left, top, largeButton.DesiredSize.Width, largeButton.DesiredSize.Height));
                }
                else if (smallButton != null)
                {
                    if (lastElementWasFullHeight)
                    {
                        left += lastLargestWidth;
                        lastLargestWidth = 0d;
                        top = 0d;
                    }
                    if (top + smallButton.DesiredSize.Height > availableHeight)
                    {
                        left += lastLargestWidth;
                        lastLargestWidth = 0d;
                        top = 0d;
                    }
                    lastElementWasFullHeight = false;
                    if (smallButton.IsVisible) smallButton.Arrange(new Rect(left, top, smallButton.DesiredSize.Width, smallButton.DesiredSize.Height));
                    lastLargestWidth = Math.Max(lastLargestWidth, smallButton.IsVisible ? smallButton.DesiredSize.Width : 0d);
                    top += smallButton.DesiredSize.Height;
                }
                else if (separator != null)
                {
                    top = 0d;
                    left += lastLargestWidth;
                    currentRenderInfo.RenderRect = new Rect(currentGroupLeft, 0d, left - currentGroupLeft, finalSize.Height);
                    if (currentRenderInfo.RenderRect.Width < 2 || string.IsNullOrEmpty(currentRenderInfo.GroupTitle))
                        _groupTitles.Remove(currentRenderInfo);
                    isGroupStart = true;
                    separator.Arrange(new Rect(left, top, separator.DesiredSize.Width, separator.DesiredSize.Height));
                    lastElementWasFullHeight = true;
                    lastLargestWidth = separator.DesiredSize.Width;
                }
                else if (child != null)
                {
                    if (lastElementWasFullHeight)
                    {
                        left += lastLargestWidth;
                        lastLargestWidth = 0d;
                        top = 0d;
                    }
                    var childHeight = child.DesiredSize.Height;
                    if (childHeight >= availableHeight)
                    {
                        childHeight = availableHeight;
                        lastElementWasFullHeight = true;
                    }
                    else
                        lastElementWasFullHeight = false;
                    if (top + childHeight > availableHeight)
                    {
                        left += lastLargestWidth;
                        lastLargestWidth = 0d;
                        top = 0d;
                    }
                    if (child.IsVisible) child.Arrange(new Rect(left, top, child.DesiredSize.Width, childHeight));
                    lastLargestWidth = Math.Max(lastLargestWidth, child.IsVisible ? child.DesiredSize.Width : 0d);
                    top += childHeight;
                }
            }

            if (currentRenderInfo != null)
            {
                left += lastLargestWidth;
                currentRenderInfo.RenderRect = new Rect(currentGroupLeft, 0d, left - currentGroupLeft, finalSize.Height);
                if (currentRenderInfo.RenderRect.Width < 2 || string.IsNullOrEmpty(currentRenderInfo.GroupTitle))
                    _groupTitles.Remove(currentRenderInfo);
            }

            return base.ArrangeOverride(finalSize);
        }

        /// <summary>
        /// Draws the content of a <see cref="T:System.Windows.Media.DrawingContext" /> object during the render pass of a <see cref="T:System.Windows.Controls.Panel" /> element.
        /// </summary>
        /// <param name="dc">The <see cref="T:System.Windows.Media.DrawingContext" /> object to draw.</param>
        protected override void OnRender(DrawingContext dc)
        {
            var typeFace = new Typeface(GroupTitleFontFamily, FontStyles.Normal, GroupTitleFontWeight, FontStretches.Normal);
            var brush = GroupTitleForegroundBrush.Clone();
            brush.Opacity = GroupTitleForegroundBrushOpacity;

            foreach (var title in _groupTitles)
            {
                if (string.IsNullOrEmpty(title.GroupTitle)) continue;
                if (title.RenderRect.Width < 20) continue;
                var format = new FormattedText(title.GroupTitle, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, typeFace, GroupTitleFontSize, brush)
                {
                    TextAlignment = TextAlignment.Center,
                    MaxLineCount = 1,
                    Trimming = TextTrimming.CharacterEllipsis,
                    MaxTextWidth = title.RenderRect.Width - 10,
                    MaxTextHeight = title.RenderRect.Height,
                };

                var yOffset = title.RenderRect.Height - format.Height + 1;
                dc.PushTransform(new TranslateTransform(title.RenderRect.X, yOffset));
                dc.DrawText(format, new Point(10d, 0d));
                dc.Pop();
            }
            base.OnRender(dc);
        }

        /// <summary>
        /// Class GroupTitleRenderInfo
        /// </summary>
        private class GroupTitleRenderInfo
        {
            public string GroupTitle { get; set; }
            public Rect RenderRect { get; set; }
        }
    }

    /// <summary>
    /// Special button used to close the special ribbon UI
    /// </summary>
    public class CloseSpecialRibbonUiButton : Button
    {
        /// <summary>
        /// Reference to the ribbon control
        /// </summary>
        public ViewActionRibbon Ribbon
        {
            get { return (ViewActionRibbon) GetValue(RibbonProperty); }
            set { SetValue(RibbonProperty, value); }
        }

        /// <summary>
        /// Reference to the ribbon control
        /// </summary>
        public static readonly DependencyProperty RibbonProperty = DependencyProperty.Register("Ribbon", typeof (ViewActionRibbon), typeof (CloseSpecialRibbonUiButton), new PropertyMetadata(null));

        /// <summary>
        /// Called when a <see cref="T:System.Windows.Controls.Button" /> is clicked.
        /// </summary>
        protected override void OnClick()
        {
            base.OnClick();

            if (Ribbon == null) return;
            if (Ribbon.Items.Count > 1)
            {
                if (!Ribbon.IsSpecialFirstPageActive) Ribbon.IsSpecialFirstPageActive = true; // This can happen in theme switching scenarios. We trigger the change here so everything associated with this property changing actually fires.
                Ribbon.IsSpecialFirstPageActive = false;
            }
        }
    }

    /// <summary>
    /// List of action items for first page controls
    /// </summary>
    public class SpecialFirstPageActionList : Panel
    {
        private double _widest;

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
        public static readonly DependencyProperty ViewActionPolicyProperty = DependencyProperty.Register("ViewActionPolicy", typeof(IViewActionPolicy), typeof(SpecialFirstPageActionList), new PropertyMetadata(null));


        /// <summary>
        /// Current active view associated with the current action list
        /// </summary>
        /// <value>The active view.</value>
        public FrameworkElement ActiveView
        {
            get { return (FrameworkElement)GetValue(ActiveViewProperty); }
            set { SetValue(ActiveViewProperty, value); }
        }
        /// <summary>
        /// Current active view associated with the current action list
        /// </summary>
        /// <value>The active view.</value>
        public static readonly DependencyProperty ActiveViewProperty = DependencyProperty.Register("ActiveView", typeof(FrameworkElement), typeof(SpecialFirstPageActionList), new PropertyMetadata(null));

        /// <summary>
        /// When overridden in a derived class, positions child elements and determines a size for a <see cref="T:System.Windows.FrameworkElement" /> derived class.
        /// </summary>
        /// <param name="finalSize">The final area within the parent that this element should use to arrange itself and its children.</param>
        /// <returns>The actual size used.</returns>
        protected override Size ArrangeOverride(Size finalSize)
        {
            var top = 0d;

            foreach (UIElement child in Children)
            {
                child.Arrange(new Rect(0d, top, _widest, child.DesiredSize.Height));
                top += child.DesiredSize.Height;
            }

            return base.ArrangeOverride(new Size(_widest, finalSize.Height));
        }

        /// <summary>
        /// When overridden in a derived class, measures the size in layout required for child elements and determines a size for the <see cref="T:System.Windows.FrameworkElement" />-derived class.
        /// </summary>
        /// <param name="availableSize">The available size that this element can give to child elements. Infinity can be specified as a value to indicate that the element will size to whatever content is available.</param>
        /// <returns>The size that this element determines it needs during layout, based on its calculations of child element sizes.</returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            var widest = 0d;
            var height = 0d;
            var size = new Size(1000, 1000);

            foreach (UIElement child in Children)
            {
                child.Measure(size);
                widest = Math.Max(widest, child.DesiredSize.Width);
                height += child.DesiredSize.Height;
            }
            _widest = widest;
            var newFinalSize = new Size(widest, height);

            return base.ArrangeOverride(newFinalSize);
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
        public static readonly DependencyProperty ModelProperty = DependencyProperty.Register("Model", typeof (object), typeof (SpecialFirstPageActionList), new UIPropertyMetadata(null, ModelChanged));

        /// <summary>
        /// Change handler for model property
        /// </summary>
        /// <param name="d">The dependency object that triggered this change.</param>
        /// <param name="e">The <see cref="System.Windows.DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void ModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var list = d as SpecialFirstPageActionList;
            if (list == null) return;
            var actionsContainer = e.NewValue as IHaveActions;
            if (actionsContainer != null && actionsContainer.Actions != null)
            {
                actionsContainer.Actions.CollectionChanged += (s, e2) => list.PopulateList(actionsContainer);
                list.Visibility = Visibility.Visible;
                list.PopulateList(actionsContainer);
            }
            else
                list.Visibility = Visibility.Collapsed;
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
        public static readonly DependencyProperty SelectedViewProperty = DependencyProperty.Register("SelectedView", typeof (object), typeof (SpecialFirstPageActionList), new UIPropertyMetadata(null, SelectedViewChanged));

        /// <summary>
        /// For internal use only
        /// </summary>
        public bool IsActivating
        {
            get { return (bool)GetValue(IsActivatingProperty); }
            set { SetValue(IsActivatingProperty, value); }
        }

        /// <summary>
        /// Reference to a special button that has most recently been activated
        /// </summary>
        /// <value>
        /// The last selected button.
        /// </value>
        public SpecialFirstPageRibbonButton LastSelectedButton { get; set; }

        /// <summary>
        /// For internal use only
        /// </summary>
        public static readonly DependencyProperty IsActivatingProperty = DependencyProperty.Register("IsActivating", typeof(bool), typeof(SpecialFirstPageActionList), new PropertyMetadata(false, OnIsActivatingChanged));
        /// <summary>
        /// Fires when IsActivating changes
        /// </summary>
        /// <param name="d">The list object</param>
        /// <param name="e">Event args</param>
        private static void OnIsActivatingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(bool) e.NewValue) return; // Nothing to do
            var list = d as SpecialFirstPageActionList;
            if (list == null) return;
            var selectedItem = list.Children.OfType<SpecialFirstPageRibbonButton>().FirstOrDefault(b => b.IsSelected);
            if (selectedItem == null || list.ActiveView == null || list.ActiveView != selectedItem.ActionView)
            {
                SpecialFirstPageRibbonButton initialButton = null;
                if (list.LastSelectedButton != null)
                {
                    // We had a previously selected button. However, the reference to the button may 
                    // be outdated, as the list may be repopulated. Therefore, we can't use the button 
                    // directly, but instead we look at the actions associated with the button
                    initialButton = list.Children.OfType<SpecialFirstPageRibbonButton>().FirstOrDefault(b => b.Command == list.LastSelectedButton.Command);
                }
                if (initialButton == null)
                    initialButton = list.Children.OfType<SpecialFirstPageRibbonButton>().FirstOrDefault(b =>
                    {
                        var command = b.Command as OnDemandLoadCustomViewViewAction;
                        if (command == null) return false;
                        return command.IsInitiallySelected;
                    });
                if (initialButton != null)
                {
                    if (list.ActiveView != null)
                        list.ActiveView = null;
                    initialButton.ActivateButton(list);
                }
            }
        }

        /// <summary>
        /// Change handler for selected view property
        /// </summary>
        /// <param name="d">The dependency object that triggered this change.</param>
        /// <param name="e">The <see cref="System.Windows.DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void SelectedViewChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d == null) return;
            var list = d as SpecialFirstPageActionList;
            if (list == null) return;
            var viewResult = e.NewValue as ViewResult;
            if (viewResult == null)
            {
                list.PopulateList(list.Model as IHaveActions);
                return;
            }

            var actionsContainer = viewResult.Model as IHaveActions;
            if (actionsContainer != null)
            {
                actionsContainer.Actions.CollectionChanged += (s, e2) => list.PopulateList(list.Model as IHaveActions, actionsContainer);
                list.PopulateList(list.Model as IHaveActions, actionsContainer, viewResult.ViewTitle);
            }
            else
                list.PopulateList(list.Model as IHaveActions);
        }

        /// <summary>
        /// Populates the current list with items based on the actions collection
        /// </summary>
        /// <param name="actions">List of primary actions</param>
        /// <param name="actions2">List of view specific actions</param>
        /// <param name="selectedViewTitle">The selected view title.</param>
        private void PopulateList(IHaveActions actions, IHaveActions actions2 = null, string selectedViewTitle = "")
        {
            Children.Clear();

            if (actions == null) return;
            var actionList = ViewActionPolicy != null ? ViewActionPolicy.GetConsolidatedActions(actions, actions2, selectedViewTitle, viewModel: this) : ViewActionHelper.GetConsolidatedActions(actions, actions2, selectedViewTitle);

            var globalCategory = "File";
            var ribbon = ElementHelper.FindParent<ViewActionRibbon>(this);
            if (ribbon != null)
                globalCategory = ribbon.EmptyGlobalCategoryTitle;
            var rootCategories = ViewActionPolicy != null ? ViewActionPolicy.GetTopLevelActionCategories(actionList, globalCategory, globalCategory, this) : ViewActionHelper.GetTopLevelActionCategories(actionList, globalCategory, globalCategory);

            var viewActionCategories = rootCategories as ViewActionCategory[] ?? rootCategories.ToArray();
            if (viewActionCategories.Length > 0)
            {
                var category = viewActionCategories[0];
                var matchingActions = ViewActionPolicy != null ? ViewActionPolicy.GetAllActionsForCategory(actionList, category, orderByGroupTitle: false, viewModel: this) : ViewActionHelper.GetAllActionsForCategory(actionList, category, orderByGroupTitle: false);
                var matchingActionsArray = matchingActions as IViewAction[] ?? matchingActions.ToArray(); // Prevents multiple enumeration
                foreach (var action in matchingActionsArray)
                    if (action.ActionView == null && action.ActionViewModel == null && !(action is OnDemandLoadCustomViewViewAction))
                    {
                        // This is a standard button which triggers the associated action
                        var button = new SpecialFirstPageRibbonButton
                        {
                            Content = action.Caption,
                            Command = action,
                            AccessKey = action.AccessKey.ToString(CultureInfo.CurrentUICulture).Trim().ToUpper()
                        };
                        CreateMenuItemBinding(action, button);
                        Children.Add(button);
                    }
                    else
                    {
                        // This action has a custom view associated which is displayed when the button is clicked
                        var button = new SpecialFirstPageRibbonButton
                        {
                            Content = action.Caption,
                            Command = action,
                            AccessKey = action.AccessKey.ToString(CultureInfo.CurrentUICulture).Trim().ToUpper(),
                            ActionView = action.ActionView,
                            ActionViewModel = action.ActionViewModel
                        };
                        CreateMenuItemBinding(action, button);
                        Children.Add(button);
                    }
            }
        }

        /// <summary>
        /// Creates the menu item binding.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="ribbonButton">The ribbon button.</param>
        private static void CreateMenuItemBinding(IViewAction action, FrameworkElement ribbonButton)
        {
            ribbonButton.SetBinding(VisibilityProperty, new Binding("Availability") {Source = action, Converter = new AvailabilityToVisibleConverter()});
        }
    }

    /// <summary>
    /// For internal use only
    /// </summary>
    public class AvailabilityToVisibleConverter : IValueConverter
    {
        /// <summary>
        /// Converts a value.
        /// </summary>
        /// <param name="value">The value produced by the binding source.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>A converted value. If the method returns null, the valid null value is used.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is ViewActionAvailabilities)) return Visibility.Collapsed;
            var availability = (ViewActionAvailabilities) value;
            return availability == ViewActionAvailabilities.Available ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// Converts a value.
        /// </summary>
        /// <param name="value">The value that is produced by the binding target.</param>
        /// <param name="targetType">The type to convert to.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>A converted value. If the method returns null, the valid null value is used.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Not used
            return null;
        }
    }

    /// <summary>
    /// Looks at multiple Visibility values and returns the highest visibility level
    /// </summary>
    public class MaximumVisibilityMultiConverter : IMultiValueConverter
    {
        /// <summary>
        /// Converts source values to a value for the binding target. The data binding engine calls this method when it propagates the values from source bindings to the binding target.
        /// </summary>
        /// <param name="values">The array of values that the source bindings in the <see cref="T:System.Windows.Data.MultiBinding" /> produces. The value <see cref="F:System.Windows.DependencyProperty.UnsetValue" /> indicates that the source binding has no value to provide for conversion.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>A converted value.If the method returns null, the valid null value is used.A return value of <see cref="T:System.Windows.DependencyProperty" />.<see cref="F:System.Windows.DependencyProperty.UnsetValue" /> indicates that the converter did not produce a value, and that the binding will use the <see cref="P:System.Windows.Data.BindingBase.FallbackValue" /> if it is available, or else will use the default value.A return value of <see cref="T:System.Windows.Data.Binding" />.<see cref="F:System.Windows.Data.Binding.DoNothing" /> indicates that the binding does not transfer the value or use the <see cref="P:System.Windows.Data.BindingBase.FallbackValue" /> or the default value.</returns>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var result = Visibility.Collapsed;
            foreach (var value in values)
            {
                var visibility = (Visibility) value;

                if (result == Visibility.Collapsed && (visibility == Visibility.Hidden || visibility == Visibility.Visible)) result = visibility;
                else if (result == Visibility.Hidden && visibility == Visibility.Visible) return Visibility.Visible; // It can't be more than visible, so we can simply return this

                if (result == Visibility.Visible) return Visibility.Visible;
            }
            return result;
        }

        /// <summary>
        /// Converts a binding target value to the source binding values.
        /// </summary>
        /// <param name="value">The value that the binding target produces.</param>
        /// <param name="targetTypes">The array of types to convert to. The array length indicates the number and types of values that are suggested for the method to return.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>An array of values that have been converted from the target value back to the source values.</returns>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            // Not needed
            return null;
        }
    }

    /// <summary>
    /// Looks at multiple Visibility values and returns the lowest visibility level
    /// </summary>
    public class MinimumVisibilityMultiConverter : IMultiValueConverter
    {
        /// <summary>
        /// Converts source values to a value for the binding target. The data binding engine calls this method when it propagates the values from source bindings to the binding target.
        /// </summary>
        /// <param name="values">The array of values that the source bindings in the <see cref="T:System.Windows.Data.MultiBinding" /> produces. The value <see cref="F:System.Windows.DependencyProperty.UnsetValue" /> indicates that the source binding has no value to provide for conversion.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>A converted value.If the method returns null, the valid null value is used.A return value of <see cref="T:System.Windows.DependencyProperty" />.<see cref="F:System.Windows.DependencyProperty.UnsetValue" /> indicates that the converter did not produce a value, and that the binding will use the <see cref="P:System.Windows.Data.BindingBase.FallbackValue" /> if it is available, or else will use the default value.A return value of <see cref="T:System.Windows.Data.Binding" />.<see cref="F:System.Windows.Data.Binding.DoNothing" /> indicates that the binding does not transfer the value or use the <see cref="P:System.Windows.Data.BindingBase.FallbackValue" /> or the default value.</returns>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var result = Visibility.Visible;
            foreach (var value in values)
            {
                var visibility = (Visibility) value;
                if (visibility == Visibility.Collapsed) return Visibility.Collapsed;
                if (result == Visibility.Visible && visibility == Visibility.Hidden) result = Visibility.Hidden;
            }
            return result;
        }

        /// <summary>
        /// Converts a binding target value to the source binding values.
        /// </summary>
        /// <param name="value">The value that the binding target produces.</param>
        /// <param name="targetTypes">The array of types to convert to. The array length indicates the number and types of values that are suggested for the method to return.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>An array of values that have been converted from the target value back to the source values.</returns>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            // Not needed
            return null;
        }
    }

    /// <summary>
    /// For internal use only
    /// </summary>
    public class ChildrenCollectionCountToVisibleConverter : IValueConverter
    {
        private readonly UIElementCollection _children;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChildrenCollectionCountToVisibleConverter" /> class.
        /// </summary>
        /// <param name="children">The children.</param>
        public ChildrenCollectionCountToVisibleConverter(UIElementCollection children)
        {
            _children = children;
        }

        /// <summary>
        /// Converts a value.
        /// </summary>
        /// <param name="value">The value produced by the binding source.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>A converted value. If the method returns null, the valid null value is used.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // We are bound to a property of the items collection, but we do not really care and always go after the items collection itself to determined visibility
            if (_children == null) return Visibility.Collapsed;
            foreach (var item in _children)
            {
                var uiElement = item as UIElement;
                if (uiElement != null && !(uiElement is RibbonSeparator) && uiElement.Visibility == Visibility.Visible)
                    return Visibility.Visible;
            }

            return Visibility.Collapsed;
        }

        /// <summary>
        /// Converts a value.
        /// </summary>
        /// <param name="value">The value that is produced by the binding target.</param>
        /// <param name="targetType">The type to convert to.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>A converted value. If the method returns null, the valid null value is used.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Not used
            return null;
        }
    }

    /// <summary>
    /// Special button class for the first page in the ribbon
    /// </summary>
    public class SpecialFirstPageRibbonButton : Button
    {
        /// <summary>Access key to be displayed for the button</summary>
        public string AccessKey
        {
            get { return (string) GetValue(AccessKeyProperty); }
            set { SetValue(AccessKeyProperty, value); }
        }

        /// <summary>Access key to be displayed for the button</summary>
        public static readonly DependencyProperty AccessKeyProperty = DependencyProperty.Register("AccessKey", typeof (string), typeof (SpecialFirstPageRibbonButton), new PropertyMetadata(string.Empty, OnAccessKeyChanged));

        /// <summary>Fires when the access key changes</summary>
        /// <param name="d">The dependency object.</param>
        /// <param name="args">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void OnAccessKeyChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            var button = d as SpecialFirstPageRibbonButton;
            if (button == null) return;
            button.AccessKeySet = !string.IsNullOrEmpty(args.NewValue.ToString());
        }

        /// <summary>Indicates whether a access key has been set</summary>
        /// <value><c>true</c> if [access key set]; otherwise, <c>false</c>.</value>
        public bool AccessKeySet
        {
            get { return (bool) GetValue(AccessKeySetProperty); }
            set { SetValue(AccessKeySetProperty, value); }
        }

        /// <summary>Indicates whether a page access key has been set</summary>
        public static readonly DependencyProperty AccessKeySetProperty = DependencyProperty.Register("AccessKeySet", typeof(bool), typeof(SpecialFirstPageRibbonButton), new PropertyMetadata(false));

        /// <summary>
        /// Indicates whether the current button is considered to be "selected"
        /// </summary>
        /// <value><c>true</c> if this instance is selected; otherwise, <c>false</c>.</value>
        public bool IsSelected
        {
            get { return (bool)GetValue(IsSelectedProperty); }
            set { SetValue(IsSelectedProperty, value); }
        }
        /// <summary>
        /// Indicates whether the current button is considered to be "selected"
        /// </summary>
        /// <value><c>true</c> if this instance is selected; otherwise, <c>false</c>.</value>
        public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.Register("IsSelected", typeof(bool), typeof(SpecialFirstPageRibbonButton), new PropertyMetadata(false));

        /// <summary>
        /// Custom view associated with this button
        /// </summary>
        /// <value>The action view.</value>
        public FrameworkElement ActionView { get; set; }

        /// <summary>
        /// Custom view-model associated with this action
        /// </summary>
        /// <value>The action view model.</value>
        public object ActionViewModel { get; set; }

        /// <summary>
        /// Called when the button is clicked
        /// </summary>
        protected override void OnClick()
        {
            if (Command != null)
            {
                var onDemandLoadViewAction = Command as OnDemandLoadCustomViewViewAction;
                if (onDemandLoadViewAction == null)
                {
                    // We have a standard action.command that handles everything
                    base.OnClick();
                    return;
                }
            }

            ActivateButton();
        }

        /// <summary>
        /// Activates the button
        /// </summary>
        /// <param name="list">Parent action list</param>
        public void ActivateButton(SpecialFirstPageActionList list = null)
        {
            if (list == null)
                list = ElementHelper.FindVisualTreeParent<SpecialFirstPageActionList>(this);
            if (list == null) return;

            foreach (var button in list.Children.OfType<SpecialFirstPageRibbonButton>().Where(b => b.IsSelected))
                button.IsSelected = false;

            if (ActionView == null && Command != null)
            {
                var onDemandLoadViewAction = Command as OnDemandLoadCustomViewViewAction;
                if (onDemandLoadViewAction != null)
                {
                    onDemandLoadViewAction.Execute(null); // Makes sure the desired view is loaded on demand
                    ActionView = onDemandLoadViewAction.ActionView;
                    ActionViewModel = onDemandLoadViewAction.ActionViewModel;
                }
            }

            if (list.ActiveView != null && list.ActiveView != ActionView)
            {
                //ElementHelper.DetachElementFromParent(list.ActiveView);
                list.ActiveView = null;
            }
            if (ActionView != null && list.ActiveView != ActionView)
            {
                if (ActionView.DataContext == null) ActionView.DataContext = ActionViewModel;
                //ElementHelper.DetachElementFromParent(ActionView);
                list.ActiveView = ActionView;
            }

            IsSelected = true;

            if (ActionView != null)
                list.LastSelectedButton = this;
        }
    }
}
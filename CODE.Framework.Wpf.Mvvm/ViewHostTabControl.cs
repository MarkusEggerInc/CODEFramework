using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using CODE.Framework.Wpf.Layout;
using CODE.Framework.Wpf.Utilities;

namespace CODE.Framework.Wpf.Mvvm
{
    /// <summary>
    /// Special tab control used to host views (typically normal views in the shell)
    /// </summary>
    /// <remarks>Designed for internal use only</remarks>
    public class ViewHostTabControl : TabControl
    {
        private int _previousSelectedIndex = -1;

        /// <summary>
        /// Initializes a new instance of the <see cref="ViewHostTabControl"/> class.
        /// </summary>
        public ViewHostTabControl()
        {
            SelectionChanged += (s, e) => { HandleSelectionChanged(); };
        }

        /// <summary>
        /// Updates all properties related to changing view selection
        /// </summary>
        protected virtual void HandleSelectionChanged()
        {
            HasVisibleItems = SelectedItem != null;

            if (SelectedIndex != _previousSelectedIndex && SelectedIndex > -1 && SelectedIndex > _previousSelectedIndex) RaiseNextViewSelected();
            else if (SelectedIndex != _previousSelectedIndex && SelectedIndex > -1 && SelectedIndex < _previousSelectedIndex) RaisePreviousViewSelected();

            _previousSelectedIndex = SelectedIndex;

            var viewResult = SelectedItem as ViewResult;
            if (viewResult != null)
                HasVisibleLocalViews = viewResult.SelectedLocalViewIndex > -1;
        }

        /// <summary>
        /// Fires when the next view is selected (either a new view, or a view with a higher index than the previously selected one)
        /// </summary>
        public static readonly RoutedEvent NextViewSelectedEvent = EventManager.RegisterRoutedEvent("NextViewSelected", RoutingStrategy.Direct, typeof (RoutedEventHandler), typeof (ViewHostTabControl));

        /// <summary>
        /// Fires when the next view is selected (either a new view, or a view with a higher index than the previously selected one)
        /// </summary>
        public event RoutedEventHandler NextViewSelected
        {
            add { AddHandler(NextViewSelectedEvent, value); }
            remove { RemoveHandler(NextViewSelectedEvent, value); }
        }

        /// <summary>
        /// Raises the next view selected event.
        /// </summary>
        protected void RaiseNextViewSelected()
        {
            var newEventArgs = new RoutedEventArgs(NextViewSelectedEvent);
            RaiseEvent(newEventArgs);
        }

        /// <summary>
        /// Fires when the previous view is selected (a view with a lower index than the previously selected one)
        /// </summary>
        public static readonly RoutedEvent PreviousViewSelectedEvent = EventManager.RegisterRoutedEvent("PreviousViewSelected", RoutingStrategy.Direct, typeof(RoutedEventHandler), typeof(ViewHostTabControl));

        /// <summary>
        /// Fires when the previous view is selected (a view with a lower index than the previously selected one)
        /// </summary>
        public event RoutedEventHandler PreviousViewSelected
        {
            add { AddHandler(PreviousViewSelectedEvent, value); }
            remove { RemoveHandler(PreviousViewSelectedEvent, value); }
        }

        /// <summary>
        /// Raises the previous view selected event.
        /// </summary>
        private void RaisePreviousViewSelected()
        {
            var newEventArgs = new RoutedEventArgs(PreviousViewSelectedEvent);
            RaiseEvent(newEventArgs);
        }

        /// <summary>Defines whether tab headers shall be displayed</summary>
        /// <remarks>It is up to each theme to respect this property</remarks>
        public bool ShowHeaders
        {
            get { return (bool)GetValue(ShowHeadersProperty); }
            set { SetValue(ShowHeadersProperty, value); }
        }
        /// <summary>Defines whether tab headers shall be displayed</summary>
        /// <remarks>It is up to each theme to respect this property</remarks>
        public static readonly DependencyProperty ShowHeadersProperty = DependencyProperty.Register("ShowHeaders", typeof(bool), typeof(ViewHostTabControl), new PropertyMetadata(true));

        /// <summary>Main model (typically the start view model)</summary>
        public object MainModel
        {
            get { return GetValue(MainModelProperty); }
            set { SetValue(MainModelProperty, value); }
        }
        /// <summary>Main model (typically the start view model)</summary>
        public static readonly DependencyProperty MainModelProperty = DependencyProperty.Register("MainModel", typeof(object), typeof(ViewHostTabControl), new PropertyMetadata(null));

        /// <summary>
        /// Desired zoom for the content within the tab control
        /// </summary>
        public double ContentZoom
        {
            get { return (double)GetValue(ContentZoomProperty); }
            set { SetValue(ContentZoomProperty, value); }
        }
        /// <summary>
        /// Desired zoom for the content within the tab control
        /// </summary>
        public static readonly DependencyProperty ContentZoomProperty = DependencyProperty.Register("ContentZoom", typeof(double), typeof(ViewHostTabControl), new PropertyMetadata(1d));

        /// <summary>
        /// Indicates whether the control has visible items
        /// </summary>
        public bool HasVisibleItems
        {
            get { return (bool)GetValue(HasVisibleItemsProperty); }
            set { SetValue(HasVisibleItemsProperty, value); }
        }
        /// <summary>
        /// Indicates whether the control has visible items
        /// </summary>
        public static readonly DependencyProperty HasVisibleItemsProperty = DependencyProperty.Register("HasVisibleItems", typeof(bool), typeof(ViewHostTabControl), new PropertyMetadata(false));

        /// <summary>
        /// Indicates whether local child views are visible
        /// </summary>
        public bool HasVisibleLocalViews
        {
            get { return (bool)GetValue(HasVisibleLocalViewsProperty); }
            set { SetValue(HasVisibleLocalViewsProperty, value); }
        }
        /// <summary>
        /// Indicates whether local child views are visible
        /// </summary>
        public static readonly DependencyProperty HasVisibleLocalViewsProperty = DependencyProperty.Register("HasVisibleLocalViews", typeof(bool), typeof(ViewHostTabControl), new PropertyMetadata(false));
    }

    /// <summary>
    /// Specialized version of the view-host tab control class capable of creating hierarchies of tabs
    /// </summary>
    /// <seealso cref="CODE.Framework.Wpf.Mvvm.ViewHostTabControl" />
    public class HierarchicalViewHostTabControl : ViewHostTabControl
    {
        /// <summary>
        /// For internal use only (activates the view of a certain index within the hierarchy)
        /// </summary>
        /// <value>
        /// The index of the activate view.
        /// </value>
        public int ActivateViewIndex
        {
            get { return (int) GetValue(ActivateViewIndexProperty); }
            set { SetValue(ActivateViewIndexProperty, value); }
        }

        /// <summary>
        /// For internal use only (activates the view of a certain index within the hierarchy)
        /// </summary>
        public static readonly DependencyProperty ActivateViewIndexProperty = DependencyProperty.Register("ActivateViewIndex", typeof(int), typeof(HierarchicalViewHostTabControl), new PropertyMetadata(-1, OnActivateViewIndexChanged));

        /// <summary>
        /// Fires when the ActivateViewIndex property changed
        /// </summary>
        /// <param name="d">The d.</param>
        /// <param name="e">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void OnActivateViewIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var tabs = d as HierarchicalViewHostTabControl;
            if (tabs == null) return;
            if (tabs.ActivateViewIndex == -1) return; // -1 means "don't do anything"
            if (tabs.ActivateViewIndex >= tabs.HierarchicalViews.Count)
            {
                // Nothing we can do here
                tabs.ActivateViewIndex = -1; // resetting to -1, so the next time this fires for real, we are guaranteed to get a changed event
                return;
            }

            var groups = tabs.GetGroupedViews(tabs.HierarchicalViews);
            var viewToSelect = tabs.HierarchicalViews[tabs.ActivateViewIndex];
            foreach (var groupName in groups.Keys)
            {
                var group = groups[groupName];

                var groupCounter = -1;
                foreach (var view in group)
                {
                    groupCounter++;
                    if (view == viewToSelect)
                    {
                        var groupTab = tabs.GetNewOrExistingGroupTab(group, groupName);
                        if (groupTab.Content == null) break;
                        var subTabs = groupTab.Content as TabControl;
                        if (subTabs == null) break;
                        subTabs.SelectedItem = subTabs.Items[groupCounter];
                    }
                }
            }
        }

        /// <summary>
        /// Updates all properties related to changing view selection
        /// </summary>
        protected override void HandleSelectionChanged()
        {
            if (SelectedIndex < 0)
            {
                if (SelectedViewResult != null) SelectedViewResult = null;
                return;
            }

            var tab = Items[SelectedIndex] as TabItem;
            if (tab == null) return;
            var subTabControl = tab.Content as TabControl;
            if (subTabControl == null) return;
            if (subTabControl.SelectedIndex < 0)
            {
                if (SelectedViewResult != null) SelectedViewResult = null;
                return;
            }
            var tab2 = subTabControl.Items[subTabControl.SelectedIndex] as TabItem;
            if (tab2 == null) return;

            var viewResult = tab2.Content as ViewResult;
            SelectedViewResult = viewResult;
            RaiseNextViewSelected();
        }

        /// <summary>
        /// References the selected view result
        /// </summary>
        /// <value>The selected view result.</value>
        public ViewResult SelectedViewResult
        {
            get { return (ViewResult)GetValue(SelectedViewResultProperty); }
            set { SetValue(SelectedViewResultProperty, value); }
        }
        /// <summary>
        /// References the selected view result
        /// </summary>
        public static readonly DependencyProperty SelectedViewResultProperty = DependencyProperty.Register("SelectedViewResult", typeof(ViewResult), typeof(HierarchicalViewHostTabControl), new PropertyMetadata(null));

        /// <summary>
        /// Defines the title/label that is to be used for empty groups
        /// </summary>
        /// <value>The empty group title.</value>
        public string EmptyGroupTitle
        {
            get { return (string) GetValue(EmptyGroupTitleProperty); }
            set { SetValue(EmptyGroupTitleProperty, value); }
        }

        /// <summary>
        /// Defines the title/label that is to be used for empty groups
        /// </summary>
        public static readonly DependencyProperty EmptyGroupTitleProperty = DependencyProperty.Register("EmptyGroupTitle", typeof (string), typeof (HierarchicalViewHostTabControl), new PropertyMetadata("File"));

        /// <summary>
        /// If set to true, all views that do not have a group defined will be rolled up into a single category
        /// Note: The title of that category is defined through the EmptyGroupTitle property.
        /// </summary>
        /// <value><c>true</c> if [combine all empty groups]; otherwise, <c>false</c>.</value>
        public bool CombineAllEmptyGroups
        {
            get { return (bool) GetValue(CombineAllEmptyGroupsProperty); }
            set { SetValue(CombineAllEmptyGroupsProperty, value); }
        }

        /// <summary>
        /// If set to true, all views that do not have a group defined will be rolled up into a single category
        /// Note: The title of that category is defined through the EmptyGroupTitle property.
        /// </summary>
        public static readonly DependencyProperty CombineAllEmptyGroupsProperty = DependencyProperty.Register("CombineAllEmptyGroups", typeof (bool), typeof (HierarchicalViewHostTabControl), new PropertyMetadata(false, OnCombineAllEmptyGroupsChanged));

        private static void OnCombineAllEmptyGroupsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var tabs = d as HierarchicalViewHostTabControl;
            if (tabs == null) return;
            tabs.RepopulateViews();
        }

        /// <summary>
        /// For internal use only
        /// </summary>
        public static readonly DependencyProperty AssociatedGroupNameProperty = DependencyProperty.RegisterAttached("AssociatedGroupName", typeof(string), typeof(HierarchicalViewHostTabControl), new PropertyMetadata(""));

        /// <summary>
        /// For internal use only
        /// </summary>
        /// <param name="dep">The object the property was set on</param>
        /// <returns>Associated group name</returns>
        public static string GetAssociatedGroupName(DependencyObject dep)
        {
            return (string)dep.GetValue(AssociatedGroupNameProperty);
        }
        /// <summary>
        /// For internal use only
        /// </summary>
        /// <param name="dep">The object the property was set on</param>
        /// <param name="value">The associated group name.</param>
        public static void SetAssociatedGroupName(DependencyObject dep, string value)
        {
            dep.SetValue(AssociatedGroupNameProperty, value);
        }

        /// <summary>
        /// Collection of open views
        /// </summary>
        /// <value>The hierarchical views.</value>
        public ObservableCollection<ViewResult> HierarchicalViews
        {
            get { return (ObservableCollection<ViewResult>) GetValue(HierarchicalViewsProperty); }
            set { SetValue(HierarchicalViewsProperty, value); }
        }

        /// <summary>
        /// Collection of open views
        /// </summary>
        public static readonly DependencyProperty HierarchicalViewsProperty = DependencyProperty.Register("HierarchicalViews", typeof (ObservableCollection<ViewResult>), typeof (HierarchicalViewHostTabControl), new PropertyMetadata(null, OnHierarchicalViewsChanged));

        /// <summary>
        /// Style applied to the hierarchical sub-TabControl
        /// </summary>
        /// <value>The sub tab control style.</value>
        public Style SubTabControlStyle
        {
            get { return (Style) GetValue(SubTabControlStyleProperty); }
            set { SetValue(SubTabControlStyleProperty, value); }
        }

        /// <summary>
        /// Style applied to the hierarchical sub-TabControl
        /// </summary>
        public static readonly DependencyProperty SubTabControlStyleProperty = DependencyProperty.Register("SubTabControlStyle", typeof (Style), typeof (HierarchicalViewHostTabControl), new PropertyMetadata(null));

        /// <summary>
        /// Fires when the views collection changes
        /// </summary>
        /// <param name="d">The d.</param>
        /// <param name="e">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void OnHierarchicalViewsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var tabs = d as HierarchicalViewHostTabControl;
            if (tabs == null) return;
            tabs.RepopulateViews();
            var views = e.NewValue as ObservableCollection<ViewResult>;
            if (views != null)
                views.CollectionChanged += (s, e2) =>
                {
                    if (e2.Action == NotifyCollectionChangedAction.Remove && e2.OldItems != null)
                        tabs.RemoveViews(e2.OldItems.OfType<ViewResult>().ToList());
                    else if (e2.Action == NotifyCollectionChangedAction.Add && e2.NewItems != null)
                        tabs.AddViews(e2.NewItems.OfType<ViewResult>().ToList());
                    else
                        tabs.RepopulateViews();
                }; 
        }

        /// <summary>
        /// Closes the specified views
        /// </summary>
        /// <param name="removedViewResults"></param>
        private void RemoveViews(List<ViewResult> removedViewResults)
        {
            var groupPagesToRemove = new List<TabItem>();
            foreach (var groupPage in Items.OfType<TabItem>())
            {
                var pagesToRemove = new List<TabItem>();
                var subTabControl = groupPage.Content as TabControl;
                if (subTabControl == null) continue;
                foreach (var page in subTabControl.Items.OfType<TabItem>())
                {
                    var viewResultContent = page.Content as ViewResult;
                    if (viewResultContent == null) continue;
                    if (removedViewResults.Contains(viewResultContent))
                        pagesToRemove.Add(page);
                }
                foreach (var pageToRemove in pagesToRemove)
                    subTabControl.Items.Remove(pageToRemove);
                if (subTabControl.Items.Count < 1)
                    groupPagesToRemove.Add(groupPage);
            }
            foreach (var pageToRemove in groupPagesToRemove)
                Items.Remove(pageToRemove);
        }

        /// <summary>
        /// Adds new views to the current list of open views
        /// </summary>
        /// <param name="addedViews">The added views.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        private void AddViews(List<ViewResult> addedViews)
        {
            if (addedViews == null) return;
            var groups = GetGroupedViews(addedViews);
            PopulateNewViews(groups);
        }

        /// <summary>
        /// Repopulates the views from scratch.
        /// </summary>
        private void RepopulateViews()
        {
            Items.Clear();
            if (HierarchicalViews == null) return;
            var groups = GetGroupedViews(HierarchicalViews);
            PopulateNewViews(groups);
        }

        /// <summary>
        /// Adds new views into the tabs.
        /// </summary>
        /// <param name="groups">The groups.</param>
        private void PopulateNewViews(Dictionary<string, List<ViewResult>> groups)
        {
            // Adding new items into the tabs
            foreach (var groupName in groups.Keys)
            {
                var group = groups[groupName];
                if (group.Count < 1) continue;

                var groupTab = GetNewOrExistingGroupTab(group, groupName);
                if (groupTab.Content == null) continue;
                var subTabs = groupTab.Content as TabControl;
                if (subTabs == null) continue;

                foreach (var view in group)
                {
                    var newSubTab = new HierarchicalViewHostTabItem { Content = view};
                    var foregroundBrush2 = SimpleView.GetTitleColor2(group[0].View);
                    if (foregroundBrush2 != null) newSubTab.Foreground = foregroundBrush2;
                    subTabs.Items.Add(newSubTab);
                    subTabs.SelectedItem = newSubTab;
                }
            }
        }

        /// <summary>
        /// Creates a new tab, or returns the existing tab for the group
        /// </summary>
        /// <param name="group">The group.</param>
        /// <param name="groupName">Name of the group.</param>
        /// <returns>TabItem.</returns>
        private TabItem GetNewOrExistingGroupTab(IList<ViewResult> group, string groupName)
        {
            var tab = Items.OfType<TabItem>().FirstOrDefault(t => GetAssociatedGroupName(t) == groupName);
            if (tab != null)
            {
                SelectedItem = tab;
                return tab;
            }

            var newTab = new TabItem();
            SetAssociatedGroupName(newTab, groupName);
            var backgroundColor = SimpleView.GetViewThemeColor(group[0].View);
            if (backgroundColor != Colors.Transparent) newTab.Background = new SolidColorBrush(backgroundColor);
            var foregroundBrush = SimpleView.GetTitleColor(group[0].View);
            if (foregroundBrush != null) newTab.Foreground = foregroundBrush;
            newTab.DataContext = group; // We assign the whole collection here so we can reference to it later
            var subTabs = new TabControl();
            if (SubTabControlStyle != null) subTabs.Style = SubTabControlStyle;
            newTab.Content = subTabs;
            Items.Add(newTab);
            SelectedItem = newTab;
            return newTab;
        }

        /// <summary>
        /// Returns a list of grouped views.
        /// </summary>
        /// <param name="views">The views.</param>
        /// <returns>Dictionary&lt;System.String, List&lt;ViewResult&gt;&gt;.</returns>
        private Dictionary<string, List<ViewResult>> GetGroupedViews(IEnumerable<ViewResult> views)
        {
            var groups = new Dictionary<string, List<ViewResult>>();
            foreach (var view in views.Where(v => v.View != null))
            {
                var group = SimpleView.GetGroup(view.View).Trim();
                if (string.IsNullOrEmpty(group) && !CombineAllEmptyGroups)
                    group = SimpleView.GetTitle(view.View);
                if (!groups.ContainsKey(group))
                    groups.Add(group, new List<ViewResult> { view });
                else
                    groups[group].Add(view);
            }
            return groups;
        }
    }

    /// <summary>
    /// Specific tab item class used for hierarchical view hosts
    /// </summary>
    public class HierarchicalViewHostTabItem : TabItem
    {
        /// <summary>
        /// Style used for the window that hosts the undock content
        /// </summary>
        public Style UndockWindowStyle
        {
            get { return (Style)GetValue(UndockWindowStyleProperty); }
            set { SetValue(UndockWindowStyleProperty, value); }
        }
        /// <summary>
        /// Style used for the window that hosts the undock content
        /// </summary>

        public static readonly DependencyProperty UndockWindowStyleProperty = DependencyProperty.Register("UndockWindowStyle", typeof(Style), typeof(HierarchicalViewHostTabControl), new PropertyMetadata(null));

        /// <summary>
        /// Undocks the current content into its own window
        /// </summary>
        public void UndockContent()
        {
            var viewResult = Content as ViewResult;
            if (viewResult == null) return;

            var parentTabControl = ElementHelper.FindParent<TabControl>(this);
            if (parentTabControl == null) parentTabControl = ElementHelper.FindVisualTreeParent<TabControl>(this);
            if (parentTabControl != null)
            {
                var oldIndex = parentTabControl.SelectedIndex;
                var thisIndex = GetItemIndex(parentTabControl, this);
                if (oldIndex == thisIndex)
                {
                    var nextIndex = GetNextVisibleItemIndex(parentTabControl, oldIndex);
                    if (nextIndex != -1)
                        parentTabControl.SelectedIndex = nextIndex;
                    else
                    {
                        var previousIndex = GetPreviousVisibleItemIndex(parentTabControl, oldIndex);
                        if (previousIndex > -1)
                            parentTabControl.SelectedIndex = previousIndex;
                        else
                        {
                            parentTabControl.Visibility = Visibility.Collapsed;
                            SetParentTabItemVisibility(parentTabControl, Visibility.Collapsed);
                        }
                    }
                }
            }
            Visibility = Visibility.Collapsed;

            var window = new UndockedHierarchicalViewWindow(viewResult, this);
            window.Show();
            window.Activate();
        }

        /// <summary>
        /// Docks the content back into the tab.
        /// </summary>
        public void DockContent()
        {
            Visibility = Visibility.Visible;

            var parentTabControl = ElementHelper.FindParent<TabControl>(this);
            if (parentTabControl == null) parentTabControl = ElementHelper.FindVisualTreeParent<TabControl>(this);
            if (parentTabControl != null)
            {
                if (parentTabControl.Visibility == Visibility.Collapsed) parentTabControl.Visibility = Visibility.Visible;

                var thisIndex = GetItemIndex(parentTabControl, this);
                if (thisIndex > -1)
                {
                    SetParentTabItemVisibility(parentTabControl, Visibility.Visible);
                    parentTabControl.SelectedIndex = thisIndex;
                }
            }
        }

        /// <summary>
        /// Sets the visibility of the parent tab control of the current main tab within the hierarchy
        /// </summary>
        /// <param name="containedTabControl">The contained tab control.</param>
        /// <param name="visibility">The visibility.</param>
        private static void SetParentTabItemVisibility(TabControl containedTabControl, Visibility visibility)
        {
            var parentTab = ElementHelper.FindParent<TabItem>(containedTabControl);
            if (parentTab == null) parentTab = ElementHelper.FindVisualTreeParent<TabItem>(containedTabControl);
            if (parentTab == null) return;

            parentTab.Visibility = visibility;

            var parentTabControl = ElementHelper.FindParent<TabControl>(parentTab);
            if (parentTabControl == null) ElementHelper.FindVisualTreeParent<TabControl>(parentTab);
            if (parentTabControl == null) return;

            var parentTabIndex = GetItemIndex(parentTabControl, parentTab);

            if (visibility == Visibility.Visible)
                parentTabControl.SelectedIndex = parentTabIndex;
            else
            {
                var nextIndex = GetNextVisibleItemIndex(parentTabControl, parentTabIndex);
                if (nextIndex > -1)
                    parentTabControl.SelectedIndex = nextIndex;
                else
                    parentTabControl.SelectedIndex = GetPreviousVisibleItemIndex(parentTabControl, parentTabIndex); // Could be -1, but so be it
            }
        }

        /// <summary>
        /// Finds the index of the specified item in the items control
        /// </summary>
        /// <param name="itemsControl">The items control.</param>
        /// <param name="tabToSearch">The tab to search.</param>
        /// <returns>System.Int32.</returns>
        private static int GetItemIndex(ItemsControl itemsControl, TabItem tabToSearch)
        {
            var thisIndex = -1;
            foreach (var tabItem in itemsControl.Items)
            {
                thisIndex++;
                if (tabItem == tabToSearch) return thisIndex;
            }
            return -1;
        }

        /// <summary>
        /// Gets the index of the next visible item within an items control, relative to the index passed along.
        /// </summary>
        /// <param name="itemsControl">The items control.</param>
        /// <param name="referenceIndex">Index of the reference item.</param>
        /// <returns>System.Int32.</returns>
        private static int GetNextVisibleItemIndex(ItemsControl itemsControl, int referenceIndex)
        {
            for (var counter = referenceIndex + 1; counter < itemsControl.Items.Count; counter++)
            {
                var element = itemsControl.Items[counter] as FrameworkElement;
                if (element == null) continue;
                if (element.Visibility == Visibility.Visible)
                    return counter;
            }

            return -1;
        }

        /// <summary>
        /// Gets the index of the previous visible item within an items control, relative to the index passed along.
        /// </summary>
        /// <param name="itemsControl">The items control.</param>
        /// <param name="referenceIndex">Index of the reference item.</param>
        /// <returns>System.Int32.</returns>
        private static int GetPreviousVisibleItemIndex(ItemsControl itemsControl, int referenceIndex)
        {
            for (var counter = referenceIndex - 1; counter >= 0; counter--)
            {
                var element = itemsControl.Items[counter] as FrameworkElement;
                if (element == null) continue;
                if (element.Visibility == Visibility.Visible)
                    return counter;
            }

            return -1;
        }
    }

    /// <summary>
    /// Special window class used to host undocked views
    /// </summary>
    /// <seealso cref="System.Windows.Window" />
    public class UndockedHierarchicalViewWindow : Window
    {
        private readonly HierarchicalViewHostTabItem _hierarchicalViewHostTabItem;

        /// <summary>
        /// Initializes a new instance of the <see cref="UndockedHierarchicalViewWindow"/> class.
        /// </summary>
        /// <param name="viewResult">The view result.</param>
        /// <param name="hierarchicalViewHostTabItem">The hierarchical view host tab item.</param>
        public UndockedHierarchicalViewWindow(ViewResult viewResult, HierarchicalViewHostTabItem hierarchicalViewHostTabItem)
        {
            Closing += (s, e) => { DockContent(); };

            DataContext = viewResult;
            _hierarchicalViewHostTabItem = hierarchicalViewHostTabItem;
            if (hierarchicalViewHostTabItem.UndockWindowStyle != null) Style = hierarchicalViewHostTabItem.UndockWindowStyle;

            if (viewResult != null)
            {
                Title = viewResult.ViewTitle;

                if (viewResult.View != null)
                {
                    var viewColor = SimpleView.GetViewThemeColor(viewResult.View);
                    if (viewColor != Colors.Transparent) Background = new SolidColorBrush(viewColor);
                }
            }
        }

        /// <summary>
        /// Docks the content back into the parent tab control.
        /// </summary>
        public void DockContent()
        {
            _hierarchicalViewHostTabItem.DockContent();
        }
    }

    /// <summary>
    /// Special undock icon for undocking the hierarchical tab control
    /// </summary>
    /// <seealso cref="CODE.Framework.Wpf.Mvvm.ThemeIcon" />
    public class UndockIcon : ThemeIcon
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UndockIcon"/> class.
        /// </summary>
        public UndockIcon()
        {
            ToolTip = "Pop-Out";
            IsHitTestVisible = true;
            Background = Brushes.Transparent;
            Loaded += (s, e) => { IconResourceKey = "CODE.Framework-Icon-UnPin"; };
        }

        /// <summary>
        /// Handles the <see cref="E:MouseDown" /> event.
        /// </summary>
        /// <param name="e">The <see cref="MouseButtonEventArgs"/> instance containing the event data.</param>
        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                var tab = ElementHelper.FindParent<HierarchicalViewHostTabItem>(this);
                if (tab == null)
                    tab = ElementHelper.FindVisualTreeParent<HierarchicalViewHostTabItem>(this);
                if (tab != null)
                {
                    tab.UndockContent();
                    e.Handled = true;
                    return;
                }
            }

            base.OnMouseDown(e);
        }
    }

    /// <summary>
    /// Special class that is able to close the current view (assuming it exposes a CloseViewAction)
    /// </summary>
    public class CloseViewTabIcon : ThemeIcon
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CloseViewTabIcon"/> class.
        /// </summary>
        public CloseViewTabIcon()
        {
            ToolTip = "Close";
            IsHitTestVisible = true;
            Background = Brushes.Transparent;
            Loaded += (s, e) =>
            {
                IconResourceKey = "CODE.Framework-Icon-ClosePane";

                var actionHost = GetActionsHost();
                if (actionHost != null)
                {
                    var closeAction = actionHost.Actions.OfType<CloseCurrentViewAction>().FirstOrDefault();
                    CanClose = closeAction != null;
                }
            };

            DataContextChanged += (s2, e2) =>
            {
                var actionHost = GetActionsHost();
                if (actionHost != null)
                {
                    var closeAction = actionHost.Actions.OfType<CloseCurrentViewAction>().FirstOrDefault();
                    CanClose = closeAction != null;
                }
            };
        }

        /// <summary>
        /// Indicates whether the object is able to close the current view
        /// </summary>
        public bool CanClose
        {
            get { return (bool)GetValue(CanCloseProperty); }
            set { SetValue(CanCloseProperty, value); }
        }

        /// <summary>
        /// Indicates whether the object is able to close the current view
        /// </summary>
        public static readonly DependencyProperty CanCloseProperty = DependencyProperty.Register("CanClose", typeof(bool), typeof(CloseViewTabIcon), new PropertyMetadata(false));

        /// <summary>
        /// Retrieves the model that contains a list of actions (if applicable)
        /// </summary>
        /// <returns></returns>
        private IHaveActions GetActionsHost()
        {
            var tab = ElementHelper.FindParent<TabItem>(this);
            if (tab == null)
                tab = ElementHelper.FindVisualTreeParent<TabItem>(this);
            if (tab == null) return null;

            var viewResult = tab.Content as ViewResult;
            if (viewResult == null)
            {
                var resultList = tab.DataContext as IEnumerable<ViewResult>;
                if (resultList != null)
                    viewResult = resultList.FirstOrDefault();
            }
            if (viewResult != null)
                return viewResult.Model as IHaveActions;

            return null;
        }

        /// <summary>
        /// Handles the <see cref="E:MouseDown" /> event.
        /// </summary>
        /// <param name="e">The <see cref="MouseButtonEventArgs"/> instance containing the event data.</param>
        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                var actionHost = GetActionsHost();
                if (actionHost != null)
                {
                    var closeAction = actionHost.Actions.OfType<CloseCurrentViewAction>().FirstOrDefault();
                    if (closeAction != null && closeAction.CanExecute(null))
                    {
                        closeAction.Execute(null);
                        e.Handled = true;
                        return;
                    }
                }
            }
            base.OnMouseDown(e);
        }
    }

    /// <summary>
    /// Special tab control class for top-level views
    /// </summary>
    public class TopLevelViewHostTabControl : ViewHostTabControl
    {
        /// <summary>Link to a normal view host, which can potentially be disabled whenever top level views are active</summary>
        public UIElement NormalViewHost
        {
            get { return (UIElement)GetValue(NormalViewHostProperty); }
            set { SetValue(NormalViewHostProperty, value); }
        }

        /// <summary>Link to a normal view host, which can potentially be disabled whenever top level views are active</summary>
        public static readonly DependencyProperty NormalViewHostProperty = DependencyProperty.Register("NormalViewHost", typeof (UIElement), typeof (TopLevelViewHostTabControl), new FrameworkPropertyMetadata(null) {BindsTwoWayByDefault = false});

        /// <summary>Defines whether normal views should be disabled when top level views are active</summary>
        public bool DisableNormalViewHostWhenTopLevelIsActive
        {
            get { return (bool)GetValue(DisableNormalViewHostWhenTopLevelIsActiveProperty); }
            set { SetValue(DisableNormalViewHostWhenTopLevelIsActiveProperty, value); }
        }
        /// <summary>Defines whether normal views should be disabled when top level views are active</summary>
        public static readonly DependencyProperty DisableNormalViewHostWhenTopLevelIsActiveProperty = DependencyProperty.Register("DisableNormalViewHostWhenTopLevelIsActive", typeof(bool), typeof(TopLevelViewHostTabControl), new PropertyMetadata(true));

        /// <summary>InputBindingsSet</summary>
        /// <param name="obj">Object to get value from</param>
        /// <returns>True/False</returns>
        public static bool GetInputBindingsSet(DependencyObject obj)
        {
            return (bool)obj.GetValue(InputBindingsSetProperty);
        }

        /// <summary>InputBindingsSet</summary>
        /// <param name="obj">Object to set the value on</param>
        /// <param name="value">Value to set</param>
        public static void SetInputBindingsSet(DependencyObject obj, bool value)
        {
            obj.SetValue(InputBindingsSetProperty, value);
        }

        /// <summary>Indicates whether input bindings have been set on a certain control of interest</summary>
        public static readonly DependencyProperty InputBindingsSetProperty = DependencyProperty.RegisterAttached("InputBindingsSet", typeof(bool), typeof(TopLevelViewHostTabControl), new PropertyMetadata(false));

        /// <summary>Called to update the current selection when items change.</summary>
        /// <param name="e">The event data for the System.Windows.Controls.ItemContainerGenerator.ItemsChanged event</param>
        protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnItemsChanged(e);

            if (NormalViewHost != null && DisableNormalViewHostWhenTopLevelIsActive)
                NormalViewHost.IsEnabled = Items.Count < 1;
        }

        /// <summary>Raises the System.Windows.Controls.Primitives.Selector.SelectionChanged routed event</summary>
        /// <param name="e">Provides data for System.Windows.Controls.SelectionChangedEventArgs.</param>
        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            base.OnSelectionChanged(e);

            var result = SelectedContent as ViewResult;
            if (result == null || result.View == null) return;

            FocusHelper.FocusDelayed(this);

            InputBindings.Clear();
            foreach (InputBinding binding in result.View.InputBindings)
                InputBindings.Add(binding);
        }
    }
}

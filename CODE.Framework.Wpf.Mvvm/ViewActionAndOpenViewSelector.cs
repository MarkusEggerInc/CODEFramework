using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CODE.Framework.Wpf.Layout;

namespace CODE.Framework.Wpf.Mvvm
{
    /// <summary>
    /// This control combines a view action menu as well as an open-view selector (tab)
    /// </summary>
    public class ViewActionAndOpenViewSelector : ItemsControl
    {
        /// <summary>
        /// Reference to a provider of actions
        /// </summary>
        public IHaveActions Actions
        {
            get { return (IHaveActions)GetValue(ActionsProperty); }
            set { SetValue(ActionsProperty, value); }
        }
        /// <summary>
        /// Reference to a provider of actions
        /// </summary>
        public static readonly DependencyProperty ActionsProperty = DependencyProperty.Register("Actions", typeof(IHaveActions), typeof(ViewActionAndOpenViewSelector), new PropertyMetadata(null, OnActionsChanged));

        /// <summary>
        /// Called when the assigned actions changed
        /// </summary>
        /// <param name="d">The d.</param>
        /// <param name="args">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void OnActionsChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            var selector = d as ViewActionAndOpenViewSelector;
            if (selector == null) return;
            selector.Repopulate();
            var actions = args.NewValue as IHaveActions;
            if (actions == null) return;
            actions.Actions.CollectionChanged += (s, e) => selector.Repopulate();
        }

        /// <summary>
        /// A collection of views
        /// </summary>
        public IEnumerable<ViewResult> Views
        {
            get { return (IEnumerable<ViewResult>)GetValue(ViewsProperty); }
            set { SetValue(ViewsProperty, value); }
        }
        /// <summary>
        /// A collection of views
        /// </summary>
        public static readonly DependencyProperty ViewsProperty = DependencyProperty.Register("Views", typeof(IEnumerable<ViewResult>), typeof(ViewActionAndOpenViewSelector), new PropertyMetadata(null, OnViewsChanged));

        /// <summary>
        /// Tab control this control is controlling
        /// </summary>
        public TabControl ViewTabs
        {
            get { return (TabControl)GetValue(ViewTabsProperty); }
            set { SetValue(ViewTabsProperty, value); }
        }
        /// <summary>
        /// Tab control this control is controlling
        /// </summary>
        public static readonly DependencyProperty ViewTabsProperty = DependencyProperty.Register("ViewTabs", typeof(TabControl), typeof(ViewActionAndOpenViewSelector), new PropertyMetadata(null));

        /// <summary>
        /// Called when views change
        /// </summary>
        /// <param name="d">The d.</param>
        /// <param name="args">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void OnViewsChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            var selector = d as ViewActionAndOpenViewSelector;
            if (selector == null) return;
            selector.Repopulate();

            var observable = args.NewValue as ObservableCollection<ViewResult>;
            if (observable != null)
                observable.CollectionChanged += (s, e) => selector.Repopulate();
        }

        /// <summary>
        /// Allows setting an item on any object, and when that object is clicked, the item will be executed appropriately
        /// </summary>
        public static readonly DependencyProperty TriggerItemProperty = DependencyProperty.RegisterAttached("TriggerItem", typeof(ViewActionAndOpenViewItem), typeof(ViewActionAndOpenViewSelector), new PropertyMetadata(null, OnTriggerItemChanged));

        /// <summary>
        /// Fires when the trigger item changes
        /// </summary>
        /// <param name="d">The d.</param>
        /// <param name="args">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        private static void OnTriggerItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            var element = d as UIElement;
            if (element == null) return;
            var item = args.NewValue as ViewActionAndOpenViewItem;
            if (item == null) return;
            element.MouseLeftButtonUp += (s, e) =>
            {
                if (item.SubItems.Count == 0)
                {
                    if (item.Action != null && item.Action.CanExecute(null))
                    {
                        item.Action.Execute(null);
                        return;
                    }

                    if (item.View != null && item.ViewIndex > -1 && item.ViewTabs != null)
                    {
                        item.ViewTabs.SelectedIndex = item.ViewIndex;
                        return;
                    }
                }

                if (item.SubItems.Count(i => i.View != null) == 1 && item.ViewTabs != null && item.Action != null)
                {
                    var viewAction = item.Action as ViewAction;
                    if (viewAction != null && viewAction.SingleExecute) // This action now acts as a selector, even though sub-items are open
                    {
                        var viewItem = item.SubItems.FirstOrDefault(i => i.View != null);
                        if (viewItem != null)
                        {
                            item.ViewTabs.SelectedIndex = viewItem.ViewIndex;
                            return;
                        }
                    }
                }

                item.MenuOpen = false; // Setting it first to false and then to true to make sure we actual have a property change
                item.MenuOpen = true;
            };
        }

        /// <summary>
        /// Setting the trigger item
        /// </summary>
        /// <param name="d">The d.</param>
        /// <param name="value">The value.</param>
        public static void SetTriggerItem(DependencyObject d, ViewActionAndOpenViewItem value)
        {
            d.SetValue(TriggerItemProperty, value);
        }
        /// <summary>
        /// Getting the trigger item
        /// </summary>
        /// <param name="d">The d.</param>
        /// <returns>ViewActionAndOpenViewItem.</returns>
        public static ViewActionAndOpenViewItem GetTriggerItem(DependencyObject d)
        {
            return (ViewActionAndOpenViewItem)d.GetValue(TriggerItemProperty);
        }

        /// <summary>
        /// Populates the list of items from actions and views.
        /// </summary>
        private void Repopulate()
        {
            var newItems = new List<ViewActionAndOpenViewItem>();

            if (Actions != null)
                foreach (var action in Actions.Actions)
                {
                    if (!string.IsNullOrEmpty(action.GroupTitle))
                    {
                        var found = false;
                        foreach (var otherAction in newItems)
                            if (otherAction.Group == action.GroupTitle)
                            {
                                otherAction.SubItems.Add(new ViewActionAndOpenViewItem(action, ViewTabs));
                                found = true;
                            }
                        if (!found)
                            newItems.Add(new ViewActionAndOpenViewItem(action, ViewTabs));
                    }
                    else
                        newItems.Add(new ViewActionAndOpenViewItem(action, ViewTabs));
                }

            if (Views != null)
            {
                var viewIndex = -1;
                foreach (var view in Views)
                {
                    viewIndex++;
                    var groupTitle = string.Empty;
                    if (view.View != null) groupTitle = SimpleView.GetGroup(view.View);
                    if (!string.IsNullOrEmpty(groupTitle))
                    {
                        var found = false;
                        foreach (var otherAction in newItems)
                            if (otherAction.Group == groupTitle)
                            {
                                otherAction.SubItems.Add(new ViewActionAndOpenViewItem(view, viewIndex, ViewTabs));
                                found = true;
                            }
                        if (!found)
                            newItems.Add(new ViewActionAndOpenViewItem(view, viewIndex, ViewTabs));
                    }
                    else
                        newItems.Add(new ViewActionAndOpenViewItem(view, viewIndex, ViewTabs));
                }
            }

            ItemsSource = newItems;
        }
    }

    /// <summary>
    /// Represents an individual item within a list of views and open items
    /// </summary>
    public class ViewActionAndOpenViewItem : DependencyObject, INotifyPropertyChanged
    {
        private IViewAction _action;
        private string _group;
        private bool _menuOpen;

        /// <summary>
        /// Initializes a new instance of the <see cref="ViewActionAndOpenViewItem"/> class.
        /// </summary>
        /// <param name="viewTabs">The view tabs control.</param>
        /// <param name="action">The action.</param>
        public ViewActionAndOpenViewItem(IViewAction action, TabControl viewTabs)
        {
            Action = action;
            CreateSubItemsCollection();
            ViewTabs = viewTabs;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ViewActionAndOpenViewItem" /> class.
        /// </summary>
        /// <param name="viewResult">The view result.</param>
        /// <param name="viewIndex">Index of the view within the tab control hosting it.</param>
        /// <param name="viewTabs">The view tabs control.</param>
        public ViewActionAndOpenViewItem(ViewResult viewResult, int viewIndex, TabControl viewTabs)
        {
            View = viewResult;
            ViewIndex = viewIndex;
            ViewTabs = viewTabs;
            CreateSubItemsCollection();
        }

        private void CreateSubItemsCollection()
        {
            SubItems = new ObservableCollection<ViewActionAndOpenViewItem>();
            SubItems.CollectionChanged += (s, e) =>
            {
                NotifyChanged("OpenSubItemDisplayCount");
                NotifyChanged("OpenSubItemDisplayCountVisible");
            };
            MenuItems = new ObservableCollection<ButtonWithIcon>();
        }

        /// <summary>
        /// Sub items
        /// </summary>
        public ObservableCollection<ViewActionAndOpenViewItem> SubItems { get; set; }

        /// <summary>
        /// Group title
        /// </summary>
        public string Group
        {
            get
            {
                if (_group == null)
                {
                    if (Action != null)
                        _group = Action.GroupTitle;
                    else if (View != null && View.View != null)
                        _group = SimpleView.GetGroup(View.View);
                }
                return _group;
            }
            set { _group = value; }
        }

        /// <summary>
        /// Maximum number of sut-items count to be displayed 
        /// </summary>
        public int MaxSubItemCount
        {
            get { return (int)GetValue(MaxSubItemCountProperty); }
            set { SetValue(MaxSubItemCountProperty, value); }
        }
        /// <summary>
        /// Maximum number of sut-items count to be displayed 
        /// </summary>
        public static readonly DependencyProperty MaxSubItemCountProperty = DependencyProperty.Register("MaxSubItemCount", typeof(int), typeof(ViewActionAndOpenViewItem), new PropertyMetadata(9, OnMaxSubItemCountChanged));

        /// <summary>
        /// Fires when the max sub item count changes
        /// </summary>
        /// <param name="d"></param>
        /// <param name="args"></param>
        private static void OnMaxSubItemCountChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            var item = d as ViewActionAndOpenViewItem;
            if (item == null) return;
            item.NotifyChanged("OpenSubItemDisplayCount");
            item.NotifyChanged("OpenSubItemDisplayCountVisible");
        }

        /// <summary>
        /// Displays the number of open sub items that are views
        /// </summary>
        public string OpenSubItemDisplayCount
        {
            get
            {
                var count = SubItems.Count(s => s.Action == null && s.View != null);
                if (MaxSubItemCount > -1 && count > MaxSubItemCount) return "+";
                return count.ToString(CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// Indicates whether the count property shall be visible
        /// </summary>
        public Visibility OpenSubItemDisplayCountVisible
        {
            get
            {
                var count = SubItems.Count(s => s.Action == null && s.View != null);
                if (count == 0) return Visibility.Collapsed;
                if (Action != null)
                {
                    var viewAction = Action as ViewAction;
                    if (viewAction != null)
                        if (viewAction.SingleExecute)
                            return Visibility.Collapsed; // Singleton actions do not show sub-item counts since they just select the existing item when clicked again
                }
                return Visibility.Visible;
            }
        }

        /// <summary>
        /// Index of the view within the view host
        /// </summary>
        public int ViewIndex { get; private set; }

        /// <summary>
        /// Action the item is tied to
        /// </summary>
        public IViewAction Action
        {
            get { return _action; }
            private set
            {
                _action = value;
                if (_action == null) return;
                _action.CanExecuteChanged += (s, e) => NotifyChanged("Visible");
                var inpc = _action as INotifyPropertyChanged;
                if (inpc != null)
                    inpc.PropertyChanged += (s, e) => NotifyChanged();
            }
        }

        /// <summary>
        /// Caption
        /// </summary>
        public string Caption 
        {
            get
            {
                if (Action != null) return Action.Caption;
                if (View != null) return View.ViewTitle;
                return string.Empty;
            }
        }

        /// <summary>
        /// Icon
        /// </summary>
        public Brush Icon
        {
            get
            {
                if (Action != null)
                {
                    var viewAction = Action as ViewAction;
                    if (viewAction != null) return viewAction.PopulatedBrush;
                    return null;
                }

                return null;
            }
        }

        /// <summary>
        /// Indicates whether this action should be visible
        /// </summary>
        public Visibility Visible
        {
            get
            {
                if (Action != null)
                    if (Action.Availability != ViewActionAvailabilities.Available || !Action.CanExecute(null)) return Visibility.Collapsed;
                return Visibility.Visible;
            }
        }

        /// <summary>
        /// Indicates whether the context menu is open
        /// </summary>
        public bool MenuOpen
        {
            get { return _menuOpen; }
            set
            {
                _menuOpen = value;
                if (_menuOpen)
                {
                    MenuItems.Clear();
                    foreach (var item in SubItems)
                    {
                        var newMenu = new ButtonWithIcon();
                        if (item.Action != null)
                        {
                            newMenu.Command = item.Action;
                            newMenu.Content = item.Action.Caption;
                            var viewAction = item.Action as ViewAction;
                            if (viewAction != null)
                                newMenu.IconBrush = viewAction.PopulatedBrush;
                            newMenu.Click += (s, e) => { MenuOpen = false; };
                        }
                        else if (item.View != null)
                        {
                            newMenu.Content = item.View.ViewTitle;
                            if (!string.IsNullOrEmpty(item.View.ViewIconResourceKey))
                                newMenu.IconBrush = Application.Current.FindResource(item.View.ViewIconResourceKey) as Brush;
                            if (newMenu.IconBrush == null && item.View.View is SimpleView)
                            {
                                var simpleView = item.View.View as DependencyObject;
                                var resourceKey = SimpleView.GetIconResourceKey(simpleView);
                                if (!string.IsNullOrEmpty(resourceKey))
                                newMenu.IconBrush = Application.Current.FindResource(resourceKey) as Brush;
                            }
                            if (newMenu.IconBrush == null && item.Action != null)
                            {
                                var viewAction = item.Action as ViewAction;
                                if (viewAction != null)
                                    newMenu.IconBrush = viewAction.PopulatedBrush;
                            }
                            if (newMenu.IconBrush == null && Icon != null)
                                newMenu.IconBrush = Icon;
                            var enclosureItem = item;
                            newMenu.Click += (s, e) =>
                            {
                                if (enclosureItem.ViewIndex > -1)
                                    ViewTabs.SelectedIndex = enclosureItem.ViewIndex;
                                MenuOpen = false;
                            };
                        }
                        MenuItems.Add(newMenu);
                    }
                }
                NotifyChanged("MenuOpen");
            }
        }

        /// <summary>
        /// Menu items for sub-item display
        /// </summary>
        public ObservableCollection<ButtonWithIcon> MenuItems { get; set; }

        /// <summary>
        /// View (result) the item is tied to
        /// </summary>
        public ViewResult View { get; private set; }

        /// <summary>
        /// Associated view tab control
        /// </summary>
        public TabControl ViewTabs { get; set; }

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Can be used to trigger change notification
        /// </summary>
        /// <param name="property">Name of the property.</param>
        protected virtual void NotifyChanged(string property = "")
        {
            if (PropertyChanged != null) 
                PropertyChanged(this, new PropertyChangedEventArgs(property));
        }
    }

    /// <summary>
    /// Special button subclass that provides an icon brush
    /// </summary>
    public class ButtonWithIcon : Button
    {
        /// <summary>
        /// Brush used to display an icon
        /// </summary>
        public Brush IconBrush
        {
            get { return (Brush)GetValue(IconBrushProperty); }
            set { SetValue(IconBrushProperty, value); }
        }
        /// <summary>
        /// Brush used to display an icon
        /// </summary>
        public static readonly DependencyProperty IconBrushProperty = DependencyProperty.Register("IconBrush", typeof(Brush), typeof(ButtonWithIcon), new PropertyMetadata(null));
    }
}

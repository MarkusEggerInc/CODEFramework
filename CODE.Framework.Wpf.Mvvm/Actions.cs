using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using CODE.Framework.Core.Exceptions;
using CODE.Framework.Core.Utilities;
using CODE.Framework.Wpf.Layout;
using CODE.Framework.Wpf.Utilities;

namespace CODE.Framework.Wpf.Mvvm
{
    /// <summary>
    /// Interface used to indicate that a view model supports a list of available actions
    /// </summary>
    public interface IHaveActions
    {
        /// <summary>
        /// Collection of actions
        /// </summary>
        ViewActionsCollection Actions { get; }

        /// <summary>
        /// Fires when the list of actions changed (assuming change notification is active)
        /// </summary>
        event NotifyCollectionChangedEventHandler ActionsChanged;
    }

    /// <summary>
    /// Collection of view actions
    /// </summary>
    public class ViewActionsCollection : ObservableCollection<IViewAction>
    {
        /// <summary>Returns the view action specified by Id</summary>
        /// <param name="id">The view action id.</param>
        /// <returns>IViewAction</returns>
        /// <exception cref="IndexOutOfBoundsException">ViewAction with Id ' + id + ' not found in collection.</exception>
        public IViewAction this[string id]
        {
            get
            {
                foreach (var action in this)
                    if (action.Id == id)
                        return action;

                var id2 = id.Replace(" ", "");
                foreach (var action in this)
                    if (action.Id.Replace(" ", "") == id2)
                        return action;

                int id3;
                if (int.TryParse(id, out id3))
                    return base[id3];

                return null;
            }
        }
    }

    /// <summary>
    /// Interface defining action features beyond basic command features
    /// </summary>
    public interface IViewAction : ICommand
    {
        /// <summary>
        /// String identifier to identify an action independent of its caption (and independent of the locale)
        /// </summary>
        string Id { get; set; }

        /// <summary>
        /// Caption (can be used to display in the UI)
        /// </summary>
        string Caption { get; set; }

        /// <summary>
        /// Indicates whether this action starts a new group
        /// </summary>
        bool BeginGroup { get; set; }

        /// <summary>
        /// Indicates the group title for items that start a new group
        /// </summary>
        string GroupTitle { get; set; }

        /// <summary>
        /// Is this the default action?
        /// </summary>
        bool IsDefault { get; set; }

        /// <summary>
        /// Is this the cancel action?
        /// </summary>
        bool IsCancel { get; set; }

        /// <summary>
        /// Indicates whether an action is pinned (which is used for different things in different themes)
        /// </summary>
        bool IsPinned { get; set; }

        /// <summary>
        /// Indicates whether the action is to be considered "checked"
        /// </summary>
        /// <remarks>
        /// Cecked actions may be presented in various ways in different themes, such as having a check-mark in menus
        /// Most themes will only respect this property when ViewActionType = Toggle
        /// </remarks>
        bool IsChecked { get; set; }

        /// <summary>
        /// Indicates the type of the view action
        /// </summary>
        ViewActionTypes ViewActionType { get; set; }

        /// <summary>
        /// Indicates that this view action is selected by default if the theme supports pre-selecting actions in some way (such as showing the page of the ribbon the action is in, or triggering the action in a special Office-style file menu).
        /// </summary>
        /// <remarks>If more than one action is flagged as the default selection, then the last one (in instantiation order) 'wins'</remarks>
        bool IsDefaultSelection { get; set; }

        /// <summary>
        /// Indicates whether or not this action is at all available (often translates directly to being visible or invisible)
        /// </summary>
        ViewActionAvailabilities Availability { get; }

        /// <summary>
        /// Defines view action visibility (collapsed or hidden items are may be removed from menus or ribbons independent of their availability or can-execute state)
        /// </summary>
        Visibility Visibility { get; set; }

        /// <summary>
        /// Significance of the action
        /// </summary>
        ViewActionSignificance Significance { get; set; }

        /// <summary>
        /// Logical list of categories
        /// </summary>
        List<ViewActionCategory> Categories { get; set; }

        /// <summary>
        /// Sort order for the category
        /// </summary>
        int CategoryOrder { get; }

        /// <summary>
        /// Sort order for the action (within a group)
        /// </summary>
        int Order { get; }

        /// <summary>
        /// Returns the ID of the first category or an empty string if no categories have been added
        /// </summary>
        string FirstCategoryId { get; }

        /// <summary>
        /// A view model dedicated to this action
        /// </summary>
        object ActionViewModel { get; set; }

        /// <summary>
        /// A view specific to this action
        /// </summary>
        FrameworkElement ActionView { get; set; }

        /// <summary>
        /// List of roles with access to this action
        /// </summary>
        string[] UserRoles { get; set; }

        /// <summary>
        /// Defines the access key of the action (such as the underlined key in the menu)
        /// </summary>
        /// <remarks>Not all themes will pick this setting up</remarks>
        char AccessKey { get; set; }

        /// <summary>
        /// Shortcut key
        /// </summary>
        /// <value>The shortcut key.</value>
        /// <remarks>Not all themes will pick this setting up</remarks>
        Key ShortcutKey { get; set; }

        /// <summary>
        /// Modifier for the shortcut key
        /// </summary>
        /// <value>The shortcut modifier keys.</value>
        /// <remarks>Not all themes will pick this setting up</remarks>
        ModifierKeys ShortcutModifiers { get; set; }

        /// <summary>Indicates that previous CanExecute() results have become invalid and need to be re-evaluated.</summary>
        /// <remarks>This method should simply fire the CanExecuteChanged event.</remarks>
        void InvalidateCanExecute();
    }

    /// <summary>
    /// Types of view actions
    /// </summary>
    public enum ViewActionTypes
    {
        /// <summary>
        /// Standard (triggers an action)
        /// </summary>
        Standard,

        /// <summary>
        /// Toggle (triggers actions that can be considered to 'toggle' something - these types of actions respect the IsChecked flag)
        /// </summary>
        Toggle
    }

    /// <summary>
    /// Indicates the availability state of a view action
    /// </summary>
    public enum ViewActionAvailabilities
    {
        /// <summary>
        /// Document action availability is unknown (has not yet een evaluated)
        /// </summary>
        Unknown,

        /// <summary>
        /// Document action is available
        /// </summary>
        Available,

        /// <summary>
        /// Document action is currently not available
        /// </summary>
        Unavailable
    }

    /// <summary>
    /// Document action category
    /// </summary>
    public class ViewActionCategory : IComparable, INotifyPropertyChanged
    {
        private string _caption;
        private bool _isLocalCategory;
        private int _order;
        private char _accessKey;
        private string _brushResourceKey;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="id">The unique and culturally invariant identifier of the category.</param>
        /// <param name="caption">The caption (if not provides, the id is used as the caption).</param>
        /// <param name="accessKey">The keyboard access key for the category.</param>
        public ViewActionCategory(string id, string caption = "", char accessKey = ' ')
        {
            Id = id;
            Caption = string.IsNullOrEmpty(caption) ? id : caption;
            AccessKey = accessKey;
        }

        /// <summary>
        /// Language independent ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Caption
        /// </summary>
        public string Caption
        {
            get { return _caption; }
            set
            {
                _caption = value;
                NotifyChanged("Caption");
            }
        }

        /// <summary>
        /// Indicates whether the category belongs to local views
        /// </summary>
        public bool IsLocalCategory
        {
            get { return _isLocalCategory; }
            set
            {
                _isLocalCategory = value;
                NotifyChanged("IsLocalCategory");
            }
        }

        /// <summary>
        /// Category order
        /// </summary>
        public int Order
        {
            get { return _order; }
            set
            {
                _order = value; 
                NotifyChanged("Order");
            }
        }

        /// <summary>
        /// Access key for this category
        /// </summary>
        public char AccessKey
        {
            get { return _accessKey; }
            set
            {
                _accessKey = value;
                NotifyChanged("AccessKey");
            }
        }

        /// <summary>
        /// Icon resource key to be used for the category.
        /// </summary>
        public string BrushResourceKey
        {
            get { return _brushResourceKey; }
            set
            {
                _brushResourceKey = value;
                NotifyChanged("BrushResourceKey");
            }
        }

        /// <summary>
        /// Compares the current instance with another object of the same type and returns an integer that indicates whether the current instance precedes, follows, or occurs in the same position in the sort order as the other object.
        /// </summary>
        /// <param name="obj">An object to compare with this instance.</param>
        /// <returns>
        /// A value that indicates the relative order of the objects being compared. The return value has these meanings: Value Meaning Less than zero This instance is less than <paramref name="obj"/>. Zero This instance is equal to <paramref name="obj"/>. Greater than zero This instance is greater than <paramref name="obj"/>.
        /// </returns>
        /// <exception cref="T:System.ArgumentException">
        ///   <paramref name="obj"/> is not the same type as this instance. </exception>
        public int CompareTo(object obj)
        {
            var otherCategory = obj as ViewActionCategory;
            if (otherCategory == null) return 0; // Nothing else we can do, really.
            if (otherCategory.IsLocalCategory != IsLocalCategory) return -1;
            return string.CompareOrdinal(Id, otherCategory.Id);
        }

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Can be used to indicate a property changed
        /// </summary>
        /// <param name="propertyName">Name of the changed property (or empty string to indicate a refresh of all properties)</param>
        protected virtual void NotifyChanged(string propertyName = "")
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Significance of a view action
    /// </summary>
    public enum ViewActionSignificance
    {
        /// <summary>
        /// Normal
        /// </summary>
        Normal,

        /// <summary>
        /// Higher than normal
        /// </summary>
        AboveNormal,

        /// <summary>
        /// Lower than normal
        /// </summary>
        BelowNormal,

        /// <summary>
        /// Highest
        /// </summary>
        Highest,

        /// <summary>
        /// Lowest
        /// </summary>
        Lowest
    }

    /// <summary>
    /// Fundamental implementation of an action
    /// </summary>
    public class ViewAction : IViewAction, INotifyPropertyChanged, IStandardViewModel
    {
        /// <summary>
        /// Constructor
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
        public ViewAction(string caption = "",
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
            StandardIcons standardIcon = StandardIcons.None)
        {

            PropertyChanged += (s, e) =>
            {
                if (_inBrushUpdating) return;
                if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName.StartsWith("Image") || e.PropertyName.StartsWith("Logo"))
                    CheckAllBrushesForResources();
            };

            Caption = caption;
            Id = string.IsNullOrEmpty(id) ? caption : id;

            BeginGroup = beginGroup;
            _executeDelegate = execute;
            _canExecuteDelegate = canExecute;
            VisualResourceKey = visualResourceKey;
            BrushResourceKey = brushResourceKey;
            if (standardIcon != StandardIcons.None) StandardIcon = standardIcon;
            LogoBrushResourceKey = logoBrushResourceKey;

            CategoryOrder = categoryOrder;

            GroupTitle = groupTitle;
            Order = order;

            Categories = new List<ViewActionCategory>();
            Significance = ViewActionSignificance.Normal;

            UserRoles = userRoles ?? new string[0];

            IsDefault = isDefault;
            IsCancel = isCancel;
            IsDefaultSelection = isDefaultSelection;
            IsPinned = isPinned;

            Significance = significance;

            if (!string.IsNullOrEmpty(category))
            {
                if (string.IsNullOrEmpty(categoryCaption)) categoryCaption = category;
                Categories.Add(new ViewActionCategory(category, categoryCaption, categoryAccessKey));
            }

            AccessKey = accessKey;
            ShortcutKey = shortcutKey;
            ShortcutModifiers = shortcutKeyModifiers;
        }

        /// <summary>
        /// To the string.
        /// </summary>
        /// <returns>System.String.</returns>
        public override string ToString()
        {
            var baseName = base.ToString();
            return baseName + " (" + Caption + ")";
        }

        /// <summary>
        /// String identifier to identify an action independent of its caption (and independent of the locale)
        /// </summary>
        /// <value>The identifier.</value>
        public string Id { get; set; }

        /// <summary>Caption associated with this action</summary>
        public string Caption
        {
            get { return _caption; }
            set
            {
                _caption = value;
                NotifyChanged("Caption");
                NotifyChanged("Text1");
            }
        }

        private string _caption;

        /// <summary>Key of a visual XAML resource associated with this action (such as an icon)</summary>
        public string VisualResourceKey
        {
            get { return _visualResourceKey; }
            set
            {
                _visualResourceKey = value;
                _latestBrush = null;
                NotifyChanged("VisualResourceKey");
                NotifyChanged("Visual");
                NotifyChanged("Brush");
                NotifyChanged("PopulatedBrush");
                NotifyChanged("PopulatedVisual");
            }
        }

        private string _visualResourceKey;

        /// <summary>Key of a visual XAML resource associated with this action (such as an icon)</summary>
        public string BrushResourceKey
        {
            get { return _brushResourceKey; }
            set
            {
                _brushResourceKey = value;
                _latestBrush = null;
                NotifyChanged("BrushResourceKey");
                NotifyChanged("Visual");
                NotifyChanged("Brush");
                NotifyChanged("PopulatedBrush");
                NotifyChanged("PopulatedVisual");
                NotifyChanged("Image1");
            }
        }

        /// <summary>
        /// StandardIconHelper icon to be used as the brush resource
        /// </summary>
        /// <value>The standard icon.</value>
        public StandardIcons StandardIcon
        {
            get { return _standardIcon; }
            set
            {
                _standardIcon = value;
                NotifyChanged("StandardIcon");
                if (value != StandardIcons.None || string.IsNullOrEmpty(BrushResourceKey))
                    BrushResourceKey = StandardIconHelper.GetStandardIconKeyFromEnum(value);
            }
        }

        private string _brushResourceKey;
        private string _fallbackBrushResource;

        private string _categoryBrushResourceKey;

        /// <summary>Key of a visual XAML resource associated with this action (such as an icon)</summary>
        public string CategoryBrushResourceKey
        {
            get
            {
                return string.IsNullOrEmpty(_categoryBrushResourceKey) ? BrushResourceKey : _categoryBrushResourceKey;
            }
            set
            {
                _categoryBrushResourceKey = value;
                NotifyChanged("CategoryBrushResourceKey");
            }
        }

        /// <summary>Key of a visual XAML resource for Logo1 associated with this action (such as an icon)</summary>
        public string LogoBrushResourceKey
        {
            get { return _logoBrushResourceKey; }
            set
            {
                _logoBrushResourceKey = value;
                _latestLogoBrush = null;
                NotifyChanged("LogoBrushResourceKey");
                NotifyChanged("Logo1");
            }
        }

        private string _logoBrushResourceKey;

        /// <summary>Indicates whether this is a new group of actions</summary>
        public bool BeginGroup { get; set; }

        /// <summary>Indicates the group title for items that start a new group</summary>
        public string GroupTitle { get; set; }

        /// <summary>Indicates that this action shall only be executed once (only supported by some themes)</summary>
        public bool SingleExecute { get; set; }

        /// <summary>Returns the ID of the first category or an empty string if no categories have been created</summary>
        public string FirstCategoryId
        {
            get
            {
                var firstCategory = string.Empty;
                if (Categories.Count > 0)
                    firstCategory = Categories[0].Id;
                if (string.IsNullOrEmpty(firstCategory))
                    firstCategory = CategoryOrder.ToString(CultureInfo.InvariantCulture);
                return firstCategory;
            }
        }

        /// <summary>
        /// If true, this indicates that the action is part of an individual view rather than an app-global action.
        /// </summary>
        /// <value><c>true</c> if this instance is local action; otherwise, <c>false</c>.</value>
        public bool IsLocalAction { get; set; }

        /// <summary>
        /// Actual visual associated with an action (such as an icon). This visual is set (identified) by the VisualResourceKey property
        /// </summary>
        public Visual Visual
        {
            get
            {
                try
                {
                    if (!string.IsNullOrEmpty(VisualResourceKey))
                        return (Visual) Application.Current.FindResource(VisualResourceKey);
                    if (!string.IsNullOrEmpty(BrushResourceKey))
                        return new Rectangle
                        {
                            Fill = Application.Current.FindResource(BrushResourceKey) as Brush,
                            MinHeight = 16,
                            MinWidth = 16
                        };
                    return null;
                }
                catch
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Like Visual, but when no visual resource is found, it attempts to load a standard icon
        /// </summary>
        public Visual PopulatedVisual
        {
            get
            {
                try
                {
                    var visual = Visual;
                    if (visual == null)
                    {
                        var rectangle = new Rectangle
                        {
                            MinHeight = 16,
                            MinWidth = 16,
                            Fill = Application.Current.FindResource("CODE.Framework-Icon-More") as Brush
                        };

                        visual = rectangle;
                    }
                    return visual;
                }
                catch
                {
                    return null;
                }
            }
        }

        private Brush _latestBrush;
        private Brush _latestLogoBrush;

        /// <summary>Tries to find a named XAML resource of type brush and returns it.</summary>
        /// <param name="resourceName">Name of the resource.</param>
        /// <returns>Brush or null</returns>
        /// <remarks>The returned brush is a clone, so it can be manipulated at will without impacting other users of the same brush.</remarks>
        public Brush GetBrushFromResource(string resourceName)
        {
            var resource = Application.Current.FindResource(resourceName);
            if (resource == null) return null;

            var brush = resource as Brush;
            if (brush == null) return null;

            return brush.Clone();
        }

        /// <summary>Generic color setting (expressed as a brush)</summary>
        public Brush Color1
        {
            get { return _color1; }
            set
            {
                _color1 = value;
                NotifyChanged("Color1");
            }
        }

        /// <summary>Generic color setting (expressed as a brush)</summary>
        public Brush Color2
        {
            get { return _color2; }
            set
            {
                _color2 = value;
                NotifyChanged("Color2");
            }
        }

        /// <summary>
        /// Returns a brush of a brush resource is defined.
        /// </summary>
        public Brush Brush
        {
            get
            {
                if (_latestBrush != null) return _latestBrush;

                try
                {
                    var brushResourceKey = !string.IsNullOrEmpty(BrushResourceKey) ? BrushResourceKey : _fallbackBrushResource;
                    if (!string.IsNullOrEmpty(brushResourceKey))
                    {
                        var brushResources = new Dictionary<object, Brush>();
                        FrameworkElement resourceSearchContext = null;

                        if (ActionView != null)
                        {
                            resourceSearchContext = ActionView;
                            ResourceHelper.GetBrushResources(resourceSearchContext, brushResources);
                            var standardGrid = resourceSearchContext as StandardViewGrid;
                            if (standardGrid != null && standardGrid.Children.Count > 0)
                            {
                                var firstChild = standardGrid.Children[0] as FrameworkElement;
                                if (firstChild != null)
                                {
                                    resourceSearchContext = firstChild;
                                    ResourceHelper.GetBrushResources(resourceSearchContext, brushResources);
                                }
                            }
                        }

                        if (resourceSearchContext == null && ResourceContextObject != null)
                        {
                            resourceSearchContext = (FrameworkElement) ResourceContextObject;
                            ResourceHelper.GetBrushResources(resourceSearchContext, brushResources);
                        }

                        var icon = resourceSearchContext != null ? resourceSearchContext.FindResource(brushResourceKey) as Brush : Application.Current.FindResource(brushResourceKey) as Brush;

                        if (brushResources.Count > 0) // We may have some resources we need to replace
                            If.Real<DrawingBrush>(icon, drawing => ResourceHelper.ReplaceDynamicDrawingBrushResources(drawing, brushResources));

                        _latestBrush = icon;
                        //NotifyChanged();
                    }
                    return _latestBrush;
                }
                catch
                {
                    return null;
                }
            }
        }

        /// <summary>Indicates whether this action has an assigned brush</summary>
        public bool HasBrush
        {
            get { return !string.IsNullOrEmpty(BrushResourceKey); }
        }

        /// <summary>
        /// Returns a brush if defined, otherwise loads a default brush
        /// </summary>
        public Brush PopulatedBrush
        {
            get
            {
                try
                {
                    if (string.IsNullOrEmpty(BrushResourceKey))
                    {
                        _latestBrush = null;
                        _fallbackBrushResource = StandardIconHelper.GetStandardIconKeyFromEnum(StandardIcons.MissingIcon);
                    }
                    return Brush;
                }
                catch
                {
                    return Brushes.Transparent;
                }
            }
        }


        /// <summary>This property is mostly userd internally only. It can be used to set objects that provide resource dictionaries which can then be considered for brush resources</summary>
        /// <value>The resource context object.</value>
        public object ResourceContextObject
        {
            get { return _resourceContextObject; }
            set
            {
                _resourceContextObject = value as FrameworkElement;
                NotifyChanged("Brush");
                NotifyChanged("PopulatedBrush");
            }
        }

        private FrameworkElement _resourceContextObject;

        private Func<IViewAction, object, bool> _canExecuteDelegate;

        /// <summary>
        /// Indicates whether the current action can execute (belongs to the ICommand interface)
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        public bool CanExecute(object parameter)
        {
            if (_canExecuteDelegate != null) return _canExecuteDelegate(this, parameter);
            return true;
        }

        /// <summary>Event that fires when the CanExecute state changed</summary>
        public event EventHandler CanExecuteChanged;

        /// <summary>
        /// Indicates that previous CanExecute() results have become invalid and need to be re-evaluated.
        /// </summary>
        /// <remarks>This method should simply fire the CanExecuteChanged event.</remarks>
        public void InvalidateCanExecute()
        {
            var handler = CanExecuteChanged;
            if (handler != null)
                handler(this, new EventArgs());
            NotifyChanged("CanExecuteVisibility");
        }

        /// <summary>
        /// Returns Visible if the action can execute, otherwise Collapsed.
        /// </summary>
        public Visibility CanExecuteVisibility
        {
            get { return CanExecute(null) ? Visibility.Visible : Visibility.Collapsed; }
        }

        private Action<IViewAction, object> _executeDelegate;

        /// <summary>
        /// Sets a method as the delegate for execution
        /// </summary>
        /// <param name="method"></param>
        protected void SetExecutionDelegate(Action<IViewAction, object> method)
        {
            _executeDelegate = method;
        }

        /// <summary>
        /// Sets a method as the delegate for can execute
        /// </summary>
        /// <param name="method"></param>
        protected void SetCanExecuteDelegate(Func<IViewAction, object, bool> method)
        {
            _canExecuteDelegate = method;
        }

        /// <summary>
        /// Method used to execute an action
        /// </summary>
        /// <param name="parameter"></param>
        public virtual void Execute(object parameter)
        {
            if (_executeDelegate != null)
            {
                lock (_executionQueue)
                    _executionQueue.Enqueue(new ExecutionQueueItem
                    {
                        Execute = _executeDelegate,
                        Parameter = parameter,
                        Source = this
                    });

                if (Availability == ViewActionAvailabilities.Available)
                    ExecuteQueue();
            }
        }

        /// <summary>Indicates whether the current action has an execute delegate assigned</summary>
        public bool HasExecuteDelegate
        {
            get { return _executeDelegate != null; }
        }

        private void ExecuteQueue()
        {
            lock (_executionQueue)
                while (_executionQueue.Count > 0)
                {
                    var nextItem = _executionQueue.Dequeue();
                    if (nextItem.Execute != null)
                        nextItem.Execute(nextItem.Source, nextItem.Parameter);
                }
        }

        private readonly Queue<ExecutionQueueItem> _executionQueue = new Queue<ExecutionQueueItem>();

        private class ExecutionQueueItem
        {
            public Action<IViewAction, object> Execute { get; set; }
            public object Parameter { get; set; }
            public ViewAction Source { get; set; }
        }

        /// <summary>
        /// Is this the default action?
        /// </summary>
        public bool IsDefault { get; set; }

        /// <summary>
        /// Is this the cancel action?
        /// </summary>
        public bool IsCancel { get; set; }

        /// <summary>
        /// Indicates that this view action is selected by default if the theme supports pre-selecting actions in some way (such as showing the page of the ribbon the action is in, or triggering the action in a special Office-style file menu).
        /// </summary>
        /// <remarks>If more than one action is flagged as the default selection, then the last one (in instantiation order) 'wins'</remarks>
        public bool IsDefaultSelection { get; set; }

        /// <summary>Indicates whether an action is pinned (which is used for different things in different themes)</summary>
        public bool IsPinned
        {
            get { return _isPinned; }
            set
            {
                _isPinned = value;
                NotifyChanged("IsPinned");
            }
        }

        /// <summary>
        /// Indicates whether the action is to be considered "checked"
        /// </summary>
        /// <remarks>
        /// Checked actions may be presented in various ways in different themes, such as having a check-mark in menus
        /// Most themes will only respect this property when ViewActionType = Toggle
        /// </remarks>
        public bool IsChecked
        {
            get { return _isChecked; }
            set
            {
                _isChecked = value;
                NotifyChanged("IsChecked");
                NotifyChanged("IsChecked_Visible");
            }
        }

        /// <summary>
        /// Returns Visible if IsChecked and Collapsed otherwise
        /// </summary>
        /// <value>The is checked_ visible.</value>
        public Visibility IsChecked_Visible
        {
            get { return IsChecked ? Visibility.Visible : Visibility.Collapsed; }
            set { IsChecked = value == Visibility.Visible; }
        }

        /// <summary>
        /// Indicates the type of the view action
        /// </summary>
        public ViewActionTypes ViewActionType
        {
            get { return _viewActionType; }
            set
            {
                _viewActionType = value;
                NotifyChanged("ViewActionType");
            }
        }

        /// <summary>
        /// Indicates whether or not this action is at all available (often translates directly to being visible or invisible)
        /// </summary>
        public ViewActionAvailabilities Availability
        {
            get
            {
                if (_availability == ViewActionAvailabilities.Unknown)
                {
                    if (UserRoles.Length == 0) _availability = ViewActionAvailabilities.Available;
                    else
                    {
                        _availability = ViewActionAvailabilities.Unavailable;
                        AsyncWorker.Execute(() =>
                        {
                            var availability = ViewActionAvailabilities.Unavailable;
                            foreach (var role in UserRoles)
                                if (Thread.CurrentPrincipal.IsInRole(role))
                                {
                                    availability = ViewActionAvailabilities.Available;
                                    ExecuteQueue(); // We run all the executions that may have been on hold due to unknown status
                                    break;
                                }
                            return availability;
                        }, a =>
                        {
                            lock (this)
                                _availability = a;
                            NotifyChanged("Availability");
                        });
                    }
                }
                lock (this)
                    return _availability;
            }
        }

        /// <summary>
        /// Defines view action visibility (collapsed or hidden items are may be removed from menus or ribbons independent of their availability or can-execute state)
        /// </summary>
        /// <value>The visibility.</value>
        public Visibility Visibility
        {
            get { return _visibility; }
            set
            {
                _visibility = value;
                NotifyChanged("Visibility");
            }
        }

        private ViewActionAvailabilities _availability = ViewActionAvailabilities.Unknown;

        /// <summary>
        /// Significance of the action
        /// </summary>
        public ViewActionSignificance Significance { get; set; }

        /// <summary>
        /// Logical list of categories
        /// </summary>
        public List<ViewActionCategory> Categories { get; set; }

        /// <summary>
        /// Sort order for the category
        /// </summary>
        public int CategoryOrder { get; set; }

        /// <summary>
        /// Sort order for the action (within a group)
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// A view model dedicated to this action
        /// </summary>
        public object ActionViewModel { get; set; }

        /// <summary>
        /// A view specific to this action
        /// </summary>
        public FrameworkElement ActionView { get; set; }

        /// <summary>
        /// List of roles with access to this action
        /// </summary>
        public string[] UserRoles { get; set; }

        /// <summary>
        /// Defines the access key of the action (such as the underlined key in the menu)
        /// </summary>
        /// <value>The access key.</value>
        /// <remarks>Not all themes will pick this setting up</remarks>
        public char AccessKey { get; set; }

        /// <summary>
        /// Shortcut key
        /// </summary>
        /// <value>The shortcut key.</value>
        /// <remarks>Not all themes will pick this setting up</remarks>
        public Key ShortcutKey { get; set; }

        /// <summary>
        /// Modifier for the shortcut key
        /// </summary>
        /// <value>The shortcut modifier keys.</value>
        /// <remarks>Not all themes will pick this setting up</remarks>
        public ModifierKeys ShortcutModifiers { get; set; }

        /// <summary>Occurs when a property value changes.</summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Notifies the changed.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        protected virtual void NotifyChanged(string propertyName = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>Text Element 1</summary>
        public string Text1
        {
            get { return Caption; }
            set { Caption = value; }
        }

        /// <summary>Text Element 2</summary>
        public string Text2 { get; set; }

        /// <summary>Text Element 3</summary>
        public string Text3 { get; set; }

        /// <summary>Text Element 4</summary>
        public string Text4 { get; set; }

        /// <summary>Text Element 5</summary>
        public string Text5 { get; set; }

        /// <summary>Text Element 6</summary>
        public string Text6 { get; set; }

        /// <summary>Text Element 7</summary>
        public string Text7 { get; set; }

        /// <summary>Text Element 8</summary>
        public string Text8 { get; set; }

        /// <summary>Text Element 9</summary>
        public string Text9 { get; set; }

        /// <summary>Text Element 10</summary>
        public string Text10 { get; set; }

        /// <summary>Identifier Text Element 1</summary>
        public string Identifier1 { get; set; }

        /// <summary>Identifier Text Element 2</summary>
        public string Identifier2 { get; set; }

        /// <summary>Text Element representing a number (such as an item count)</summary>
        public string Number1 { get; set; }

        /// <summary>Second Text Element representing a number (such as an item count)</summary>
        public string Number2 { get; set; }

        ///<summary>The text to display on the tool tip when this item is hovered over with the mouse</summary>
        public string ToolTipText { get; set; }

        private Brush _image1;

        /// <summary>Image Element 1</summary>
        public Brush Image1
        {
            get
            {
                if (_image1 != null) return _image1;
                var brush = Brush;
                if (brush == null && !string.IsNullOrEmpty(VisualResourceKey))
                {
                    var visual = Visual;
                    if (visual != null)
                        brush = new VisualBrush(visual) {Stretch = Stretch.Uniform};
                }
                return brush;
            }
            set
            {
                _image1 = value;
            }
        }

        /// <summary>Image Element 2</summary>
        public Brush Image2 { get; set; }

        /// <summary>Image Element 3</summary>
        public Brush Image3 { get; set; }

        /// <summary>Image Element 4</summary>
        public Brush Image4 { get; set; }

        /// <summary>Image Element 5</summary>
        public Brush Image5 { get; set; }

        /// <summary>Logo Element 1</summary>
        public Brush Logo1
        {
            get
            {
                if (_latestLogoBrush != null) return _latestLogoBrush;

                try
                {
                    if (!string.IsNullOrEmpty(LogoBrushResourceKey))
                    {
                        var brushResources = new Dictionary<object, Brush>();
                        FrameworkElement resourceSearchContext = null;

                        if (ActionView != null)
                        {
                            resourceSearchContext = ActionView;
                            ResourceHelper.GetBrushResources(resourceSearchContext, brushResources);
                            var standardGrid = resourceSearchContext as StandardViewGrid;
                            if (standardGrid != null && standardGrid.Children.Count > 0)
                            {
                                var firstChild = standardGrid.Children[0] as FrameworkElement;
                                if (firstChild != null)
                                {
                                    resourceSearchContext = firstChild;
                                    ResourceHelper.GetBrushResources(resourceSearchContext, brushResources);
                                }
                            }
                        }

                        if (resourceSearchContext == null && ResourceContextObject != null)
                        {
                            resourceSearchContext = (FrameworkElement) ResourceContextObject;
                            ResourceHelper.GetBrushResources(resourceSearchContext, brushResources);
                        }

                        var icon = resourceSearchContext != null ? resourceSearchContext.FindResource(LogoBrushResourceKey) as Brush : Application.Current.FindResource(LogoBrushResourceKey) as Brush;

                        if (brushResources.Count > 0) // We may have some resources we need to replace
                            If.Real<DrawingBrush>(icon, drawing => ResourceHelper.ReplaceDynamicDrawingBrushResources(drawing, brushResources));

                        _latestLogoBrush = icon;
                        NotifyChanged();
                    }
                }
                catch
                {
                }
                return _latestLogoBrush;
            }
            set
            {
                /* Nothing to do*/
            }
        }

        /// <summary>Logo Element 2</summary>
        public Brush Logo2 { get; set; }

        private bool _inBrushUpdating;
        private ViewActionTypes _viewActionType;
        private bool _isChecked;
        private bool _isPinned;
        private Visibility _visibility = Visibility.Visible;
        private Brush _color2;
        private Brush _color1;
        private StandardIcons _standardIcon = StandardIcons.None;

        private void CheckAllBrushesForResources()
        {
            if (ResourceContextObject == null) return;
            if (Image1 == null && Image2 == null && Image3 == null && Image4 == null && Image5 == null && Logo1 == null && Logo2 == null) return;

            var brushResources = ResourceHelper.GetBrushResources(ResourceContextObject as FrameworkElement);
            if (brushResources.Count == 0) return;

            _inBrushUpdating = true;

            If.Real<DrawingBrush>(Image1, drawing =>
            {
                ResourceHelper.ReplaceDynamicDrawingBrushResources(drawing, brushResources);
                NotifyChanged("Image1");
            });
            If.Real<DrawingBrush>(Image2, drawing =>
            {
                ResourceHelper.ReplaceDynamicDrawingBrushResources(drawing, brushResources);
                NotifyChanged("Image2");
            });
            If.Real<DrawingBrush>(Image3, drawing =>
            {
                ResourceHelper.ReplaceDynamicDrawingBrushResources(drawing, brushResources);
                NotifyChanged("Image3");
            });
            If.Real<DrawingBrush>(Image4, drawing =>
            {
                ResourceHelper.ReplaceDynamicDrawingBrushResources(drawing, brushResources);
                NotifyChanged("Image4");
            });
            If.Real<DrawingBrush>(Image5, drawing =>
            {
                ResourceHelper.ReplaceDynamicDrawingBrushResources(drawing, brushResources);
                NotifyChanged("Image5");
            });
            If.Real<DrawingBrush>(Logo1, drawing =>
            {
                ResourceHelper.ReplaceDynamicDrawingBrushResources(drawing, brushResources);
                NotifyChanged("Logo1");
            });
            If.Real<DrawingBrush>(Logo2, drawing =>
            {
                ResourceHelper.ReplaceDynamicDrawingBrushResources(drawing, brushResources);
                NotifyChanged("Logo2");
            });

            _inBrushUpdating = false;
        }
    }

    /// <summary>
    /// View action wrapper based on a DependencyObject (and thus can be used for better binding)
    /// </summary>
    public class DependencyViewActionWrapper : DependencyObject, IViewAction, INotifyPropertyChanged, IStandardViewModel
    {
        /// <summary>
        /// Wrapped action
        /// </summary>
        private readonly IViewAction _wrappedAction;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="wrappedAction">Real action that has been wrapped</param>
        public DependencyViewActionWrapper(IViewAction wrappedAction)
        {
            _wrappedAction = wrappedAction;

            var inpf = wrappedAction as INotifyPropertyChanged;
            if (inpf != null)
                inpf.PropertyChanged += (s, e) =>
                {
                    if (PropertyChanged != null)
                        PropertyChanged(s, e);
                    PopulateLocalPropertiesFromRealViewAction(e.PropertyName);
                };
            _wrappedAction.CanExecuteChanged += (s, e) =>
            {
                if (CanExecuteChanged != null)
                    CanExecuteChanged(s, e);
            };
            PopulateLocalPropertiesFromRealViewAction();
        }

        private bool _inLocalUpdate;
        private bool _inOriginalUpdate;

        /// <summary>
        /// Populates the local properties from the real view action.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        private void PopulateLocalPropertiesFromRealViewAction(string propertyName = "")
        {
            if (_inOriginalUpdate) return;

            _inLocalUpdate = true;
            var standardViewModel = _wrappedAction as IStandardViewModel;
            if (standardViewModel != null)
            {
                if (string.IsNullOrEmpty(propertyName) || propertyName == "Text1") Text1 = standardViewModel.Text1;
                if (string.IsNullOrEmpty(propertyName) || propertyName == "Text2") Text2 = standardViewModel.Text2;
                if (string.IsNullOrEmpty(propertyName) || propertyName == "Text3") Text3 = standardViewModel.Text3;
                if (string.IsNullOrEmpty(propertyName) || propertyName == "Text4") Text4 = standardViewModel.Text4;
                if (string.IsNullOrEmpty(propertyName) || propertyName == "Text5") Text5 = standardViewModel.Text5;
                if (string.IsNullOrEmpty(propertyName) || propertyName == "Text6") Text6 = standardViewModel.Text6;
                if (string.IsNullOrEmpty(propertyName) || propertyName == "Text7") Text7 = standardViewModel.Text7;
                if (string.IsNullOrEmpty(propertyName) || propertyName == "Text8") Text8 = standardViewModel.Text8;
                if (string.IsNullOrEmpty(propertyName) || propertyName == "Text9") Text9 = standardViewModel.Text9;
                if (string.IsNullOrEmpty(propertyName) || propertyName == "Text10") Text10 = standardViewModel.Text10;
                if (string.IsNullOrEmpty(propertyName) || propertyName == "Identifier1") Identifier1 = standardViewModel.Identifier1;
                if (string.IsNullOrEmpty(propertyName) || propertyName == "Identifier2") Identifier2 = standardViewModel.Identifier2;
                if (string.IsNullOrEmpty(propertyName) || propertyName == "Number1") Number1 = standardViewModel.Number1;
                if (string.IsNullOrEmpty(propertyName) || propertyName == "Number2") Number2 = standardViewModel.Number2;
                if (string.IsNullOrEmpty(propertyName) || propertyName == "ToolTipText") ToolTipText = standardViewModel.ToolTipText;
                if (string.IsNullOrEmpty(propertyName) || propertyName == "Image1") Image1 = standardViewModel.Image1;
                if (string.IsNullOrEmpty(propertyName) || propertyName == "Image2") Image2 = standardViewModel.Image2;
                if (string.IsNullOrEmpty(propertyName) || propertyName == "Image3") Image3 = standardViewModel.Image3;
                if (string.IsNullOrEmpty(propertyName) || propertyName == "Image4") Image4 = standardViewModel.Image4;
                if (string.IsNullOrEmpty(propertyName) || propertyName == "Image5") Image5 = standardViewModel.Image5;
                if (string.IsNullOrEmpty(propertyName) || propertyName == "Logo1") Logo1 = standardViewModel.Logo1;
                if (string.IsNullOrEmpty(propertyName) || propertyName == "Logo2") Logo2 = standardViewModel.Logo2;
            }
            var action = _wrappedAction as ViewAction;
            if (action != null)
            {
                if (string.IsNullOrEmpty(propertyName) || propertyName == "IsLocalAction") IsLocalAction = action.IsLocalAction;
                if (string.IsNullOrEmpty(propertyName) || propertyName == "IsChecked_Visible") IsChecked_Visible = action.IsChecked_Visible;
            }
            if (string.IsNullOrEmpty(propertyName) || propertyName == "Id") Id = _wrappedAction.Id;
            if (string.IsNullOrEmpty(propertyName) || propertyName == "Caption") Caption = _wrappedAction.Caption;
            if (string.IsNullOrEmpty(propertyName) || propertyName == "BeginGroup") BeginGroup = _wrappedAction.BeginGroup;
            if (string.IsNullOrEmpty(propertyName) || propertyName == "GroupTitle") GroupTitle = _wrappedAction.GroupTitle;
            if (string.IsNullOrEmpty(propertyName) || propertyName == "IsCancel") IsCancel = _wrappedAction.IsCancel;
            if (string.IsNullOrEmpty(propertyName) || propertyName == "IsChecked") IsChecked = _wrappedAction.IsChecked;
            if (string.IsNullOrEmpty(propertyName) || propertyName == "IsDefault") IsDefault = _wrappedAction.IsDefault;
            if (string.IsNullOrEmpty(propertyName) || propertyName == "IsDefaultSelection") IsDefaultSelection = _wrappedAction.IsDefaultSelection;
            if (string.IsNullOrEmpty(propertyName) || propertyName == "IsPinned") IsPinned = _wrappedAction.IsPinned;
            if (string.IsNullOrEmpty(propertyName) || propertyName == "ViewActionType") ViewActionType = _wrappedAction.ViewActionType;
            if (string.IsNullOrEmpty(propertyName) || propertyName == "Availability") Availability = _wrappedAction.Availability;
            if (string.IsNullOrEmpty(propertyName) || propertyName == "Visibility") Visibility = _wrappedAction.Visibility;
            if (string.IsNullOrEmpty(propertyName) || propertyName == "Significance")
            {
                Significance = _wrappedAction.Significance;
                switch (Significance)
                {
                    case ViewActionSignificance.Normal:
                        MetroTiles.SetTileWidthMode(this, TileWidthModes.Default);
                        break;
                    case ViewActionSignificance.Lowest:
                    case ViewActionSignificance.BelowNormal:
                        MetroTiles.SetTileWidthMode(this, TileWidthModes.Tiny);
                        break;
                    case ViewActionSignificance.AboveNormal:
                        MetroTiles.SetTileWidthMode(this, TileWidthModes.Double);
                        break;
                    case ViewActionSignificance.Highest:
                        MetroTiles.SetTileWidthMode(this, TileWidthModes.DoubleSquare);
                        break;
                }
            }
            if (string.IsNullOrEmpty(propertyName) || propertyName == "Categories") Categories = _wrappedAction.Categories;
            if (string.IsNullOrEmpty(propertyName) || propertyName == "CategoryOrder") CategoryOrder = _wrappedAction.CategoryOrder;
            if (string.IsNullOrEmpty(propertyName) || propertyName == "Order") Order = _wrappedAction.Order;
            if (string.IsNullOrEmpty(propertyName) || propertyName == "FirstCategoryId") FirstCategoryId = _wrappedAction.FirstCategoryId;
            if (string.IsNullOrEmpty(propertyName) || propertyName == "ActionViewModel") ActionViewModel = _wrappedAction.ActionViewModel;
            if (string.IsNullOrEmpty(propertyName) || propertyName == "ActionView") ActionView = _wrappedAction.ActionView;
            if (string.IsNullOrEmpty(propertyName) || propertyName == "UserRoles") UserRoles = _wrappedAction.UserRoles;
            if (string.IsNullOrEmpty(propertyName) || propertyName == "AccessKey") AccessKey = _wrappedAction.AccessKey;
            if (string.IsNullOrEmpty(propertyName) || propertyName == "ShortcutKey") ShortcutKey = _wrappedAction.ShortcutKey;
            if (string.IsNullOrEmpty(propertyName) || propertyName == "ShortcutModifiers") ShortcutModifiers = _wrappedAction.ShortcutModifiers;
            _inLocalUpdate = false;
        }

        /// <summary>
        /// Updates the view action from local properties.
        /// </summary>
        /// <param name="o">The o.</param>
        /// <param name="propertyName">Name of the property.</param>
        private static void UpdateViewActionFromLocalProperties(DependencyObject o, string propertyName = "")
        {
            var wrapper = o as DependencyViewActionWrapper;
            if (wrapper == null) return;
            wrapper.UpdateViewActionFromLocalProperties(propertyName);
        }

        /// <summary>
        /// Updates the view action from local properties.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        private void UpdateViewActionFromLocalProperties(string propertyName = "")
        {
            if (_inLocalUpdate) return;

            _inOriginalUpdate = true;
            var standardViewModel = _wrappedAction as IStandardViewModel;
            if (standardViewModel != null)
            {
                if (string.IsNullOrEmpty(propertyName) || propertyName == "Text1") standardViewModel.Text1 = Text1;
                if (string.IsNullOrEmpty(propertyName) || propertyName == "Text2") standardViewModel.Text2 = Text2;
                if (string.IsNullOrEmpty(propertyName) || propertyName == "Text3") standardViewModel.Text3 = Text3;
                if (string.IsNullOrEmpty(propertyName) || propertyName == "Text4") standardViewModel.Text4 = Text4;
                if (string.IsNullOrEmpty(propertyName) || propertyName == "Text5") standardViewModel.Text5 = Text5;
                if (string.IsNullOrEmpty(propertyName) || propertyName == "Text6") standardViewModel.Text6 = Text6;
                if (string.IsNullOrEmpty(propertyName) || propertyName == "Text7") standardViewModel.Text7 = Text7;
                if (string.IsNullOrEmpty(propertyName) || propertyName == "Text8") standardViewModel.Text8 = Text8;
                if (string.IsNullOrEmpty(propertyName) || propertyName == "Text9") standardViewModel.Text9 = Text9;
                if (string.IsNullOrEmpty(propertyName) || propertyName == "Text10") standardViewModel.Text10 = Text10;
                if (string.IsNullOrEmpty(propertyName) || propertyName == "Identifier1") standardViewModel.Identifier1 = Identifier1;
                if (string.IsNullOrEmpty(propertyName) || propertyName == "Identifier2") standardViewModel.Identifier2 = Identifier2;
                if (string.IsNullOrEmpty(propertyName) || propertyName == "Number1") standardViewModel.Number1 = Number1;
                if (string.IsNullOrEmpty(propertyName) || propertyName == "Number2") standardViewModel.Number2 = Number2;
                if (string.IsNullOrEmpty(propertyName) || propertyName == "ToolTipText") standardViewModel.ToolTipText = ToolTipText;
                if (string.IsNullOrEmpty(propertyName) || propertyName == "Image1") standardViewModel.Image1 = Image1;
                if (string.IsNullOrEmpty(propertyName) || propertyName == "Image2") standardViewModel.Image2 = Image2;
                if (string.IsNullOrEmpty(propertyName) || propertyName == "Image3") standardViewModel.Image3 = Image3;
                if (string.IsNullOrEmpty(propertyName) || propertyName == "Image4") standardViewModel.Image4 = Image4;
                if (string.IsNullOrEmpty(propertyName) || propertyName == "Image5") standardViewModel.Image5 = Image5;
                if (string.IsNullOrEmpty(propertyName) || propertyName == "Logo1") standardViewModel.Logo1 = Logo1;
                if (string.IsNullOrEmpty(propertyName) || propertyName == "Logo2") standardViewModel.Logo2 = Logo2;
            }
            var action = _wrappedAction as ViewAction;
            if (action != null)
            {
                if (string.IsNullOrEmpty(propertyName) || propertyName == "IsLocalAction") action.IsLocalAction = IsLocalAction;
                if (string.IsNullOrEmpty(propertyName) || propertyName == "IsChecked_Visible") action.IsChecked_Visible = IsChecked_Visible;
                if (string.IsNullOrEmpty(propertyName) || propertyName == "CategoryOrder") action.CategoryOrder = CategoryOrder;
                if (string.IsNullOrEmpty(propertyName) || propertyName == "Order") action.Order = Order;
            }
            if (string.IsNullOrEmpty(propertyName) || propertyName == "Id") _wrappedAction.Id = Id;
            if (string.IsNullOrEmpty(propertyName) || propertyName == "Caption") _wrappedAction.Caption = Caption;
            if (string.IsNullOrEmpty(propertyName) || propertyName == "BeginGroup") _wrappedAction.BeginGroup = BeginGroup;
            if (string.IsNullOrEmpty(propertyName) || propertyName == "GroupTitle") _wrappedAction.GroupTitle = GroupTitle;
            if (string.IsNullOrEmpty(propertyName) || propertyName == "IsCancel") _wrappedAction.IsCancel = IsCancel;
            if (string.IsNullOrEmpty(propertyName) || propertyName == "IsChecked") _wrappedAction.IsChecked = IsChecked;
            if (string.IsNullOrEmpty(propertyName) || propertyName == "IsDefault") _wrappedAction.IsDefault = IsDefault;
            if (string.IsNullOrEmpty(propertyName) || propertyName == "IsDefaultSelection") _wrappedAction.IsDefaultSelection = IsDefaultSelection;
            if (string.IsNullOrEmpty(propertyName) || propertyName == "IsPinned") _wrappedAction.IsPinned = IsPinned;
            if (string.IsNullOrEmpty(propertyName) || propertyName == "ViewActionType") _wrappedAction.ViewActionType = ViewActionType;
            if (string.IsNullOrEmpty(propertyName) || propertyName == "Visibility") _wrappedAction.Visibility = Visibility;
            if (string.IsNullOrEmpty(propertyName) || propertyName == "Significance") _wrappedAction.Significance = Significance;
            if (string.IsNullOrEmpty(propertyName) || propertyName == "Categories") _wrappedAction.Categories = Categories;
            if (string.IsNullOrEmpty(propertyName) || propertyName == "ActionViewModel") _wrappedAction.ActionViewModel = ActionViewModel;
            if (string.IsNullOrEmpty(propertyName) || propertyName == "ActionView") _wrappedAction.ActionView = ActionView;
            if (string.IsNullOrEmpty(propertyName) || propertyName == "UserRoles") _wrappedAction.UserRoles = UserRoles;
            if (string.IsNullOrEmpty(propertyName) || propertyName == "AccessKey") _wrappedAction.AccessKey = AccessKey;
            if (string.IsNullOrEmpty(propertyName) || propertyName == "ShortcutKey") _wrappedAction.ShortcutKey = ShortcutKey;
            if (string.IsNullOrEmpty(propertyName) || propertyName == "ShortcutModifiers") _wrappedAction.ShortcutModifiers = ShortcutModifiers;
            _inOriginalUpdate = false;
        }

        /// <summary>
        /// Property changed event
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Text Element 1
        /// </summary>
        /// <value>The text1.</value>
        public string Text1
        {
            get { return (string) GetValue(Text1Property); }
            set { SetValue(Text1Property, value); }
        }

        /// <summary>
        /// Text Element 1
        /// </summary>
        public static readonly DependencyProperty Text1Property = DependencyProperty.Register("Text1", typeof (string), typeof (DependencyViewActionWrapper), new PropertyMetadata("", (o, args) => UpdateViewActionFromLocalProperties(o, "Text1")));

        /// <summary>
        /// Text Element 2
        /// </summary>
        /// <value>The text2.</value>
        public string Text2
        {
            get { return (string) GetValue(Text2Property); }
            set { SetValue(Text2Property, value); }
        }

        /// <summary>
        /// Text Element 2
        /// </summary>
        public static readonly DependencyProperty Text2Property = DependencyProperty.Register("Text2", typeof (string), typeof (DependencyViewActionWrapper), new PropertyMetadata("", (o, args) => UpdateViewActionFromLocalProperties(o, "Text2")));

        /// <summary>
        /// Text Element 3
        /// </summary>
        /// <value>The text3.</value>
        public string Text3
        {
            get { return (string) GetValue(Text3Property); }
            set { SetValue(Text3Property, value); }
        }

        /// <summary>
        /// Text Element 3
        /// </summary>
        public static readonly DependencyProperty Text3Property = DependencyProperty.Register("Text3", typeof (string), typeof (DependencyViewActionWrapper), new PropertyMetadata("", (o, args) => UpdateViewActionFromLocalProperties(o, "Text3")));

        /// <summary>
        /// Text Element 4
        /// </summary>
        /// <value>The text4.</value>
        public string Text4
        {
            get { return (string) GetValue(Text4Property); }
            set { SetValue(Text4Property, value); }
        }

        /// <summary>
        /// Text Element 4
        /// </summary>
        public static readonly DependencyProperty Text4Property = DependencyProperty.Register("Text4", typeof (string), typeof (DependencyViewActionWrapper), new PropertyMetadata("", (o, args) => UpdateViewActionFromLocalProperties(o, "Text4")));

        /// <summary>
        /// Text Element 5
        /// </summary>
        /// <value>The text5.</value>
        public string Text5
        {
            get { return (string) GetValue(Text5Property); }
            set { SetValue(Text5Property, value); }
        }

        /// <summary>
        /// Text Element 5
        /// </summary>
        public static readonly DependencyProperty Text5Property = DependencyProperty.Register("Text5", typeof (string), typeof (DependencyViewActionWrapper), new PropertyMetadata("", (o, args) => UpdateViewActionFromLocalProperties(o, "Text5")));

        /// <summary>
        /// Text Element 6
        /// </summary>
        /// <value>The text6.</value>
        public string Text6
        {
            get { return (string) GetValue(Text6Property); }
            set { SetValue(Text6Property, value); }
        }

        /// <summary>
        /// Text Element 6
        /// </summary>
        public static readonly DependencyProperty Text6Property = DependencyProperty.Register("Text6", typeof (string), typeof (DependencyViewActionWrapper), new PropertyMetadata("", (o, args) => UpdateViewActionFromLocalProperties(o, "Text6")));

        /// <summary>
        /// Text Element 7
        /// </summary>
        /// <value>The text7.</value>
        public string Text7
        {
            get { return (string) GetValue(Text7Property); }
            set { SetValue(Text7Property, value); }
        }

        /// <summary>
        /// Text Element 7
        /// </summary>
        public static readonly DependencyProperty Text7Property = DependencyProperty.Register("Text7", typeof (string), typeof (DependencyViewActionWrapper), new PropertyMetadata("", (o, args) => UpdateViewActionFromLocalProperties(o, "Text7")));

        /// <summary>
        /// Text Element 8
        /// </summary>
        /// <value>The text8.</value>
        public string Text8
        {
            get { return (string) GetValue(Text8Property); }
            set { SetValue(Text8Property, value); }
        }

        /// <summary>
        /// Text Element 8
        /// </summary>
        public static readonly DependencyProperty Text8Property = DependencyProperty.Register("Text8", typeof (string), typeof (DependencyViewActionWrapper), new PropertyMetadata("", (o, args) => UpdateViewActionFromLocalProperties(o, "Text8")));

        /// <summary>
        /// Text Element 9
        /// </summary>
        /// <value>The text9.</value>
        public string Text9
        {
            get { return (string) GetValue(Text9Property); }
            set { SetValue(Text9Property, value); }
        }

        /// <summary>
        /// Text Element 9
        /// </summary>
        public static readonly DependencyProperty Text9Property = DependencyProperty.Register("Text9", typeof (string), typeof (DependencyViewActionWrapper), new PropertyMetadata("", (o, args) => UpdateViewActionFromLocalProperties(o, "Text9")));

        /// <summary>
        /// Text Element 10
        /// </summary>
        /// <value>The text10.</value>
        public string Text10
        {
            get { return (string) GetValue(Text10Property); }
            set { SetValue(Text10Property, value); }
        }

        /// <summary>
        /// Text Element 10
        /// </summary>
        public static readonly DependencyProperty Text10Property = DependencyProperty.Register("Text10", typeof (string), typeof (DependencyViewActionWrapper), new PropertyMetadata("", (o, args) => UpdateViewActionFromLocalProperties(o, "Text10")));

        /// <summary>
        /// Identifier Text Element 1
        /// </summary>
        /// <value>The identifier1.</value>
        public string Identifier1
        {
            get { return (string) GetValue(Identifier1Property); }
            set { SetValue(Identifier1Property, value); }
        }

        /// <summary>
        /// Identifier Text Element 1
        /// </summary>
        public static readonly DependencyProperty Identifier1Property = DependencyProperty.Register("Identifier1", typeof (string), typeof (DependencyViewActionWrapper), new PropertyMetadata("", (o, args) => UpdateViewActionFromLocalProperties(o, "Identifier1")));

        /// <summary>
        /// Identifier Text Element 2
        /// </summary>
        /// <value>The identifier2.</value>
        public string Identifier2
        {
            get { return (string) GetValue(Identifier2Property); }
            set { SetValue(Identifier2Property, value); }
        }

        /// <summary>
        /// Identifier Text Element 2
        /// </summary>
        public static readonly DependencyProperty Identifier2Property = DependencyProperty.Register("Identifier2", typeof (string), typeof (DependencyViewActionWrapper), new PropertyMetadata("", (o, args) => UpdateViewActionFromLocalProperties(o, "Identifier2")));

        /// <summary>
        /// Text Element representing a number (such as an item count)
        /// </summary>
        /// <value>The number1.</value>
        public string Number1
        {
            get { return (string) GetValue(Number1Property); }
            set { SetValue(Number1Property, value); }
        }

        /// <summary>
        /// Text Element representing a number (such as an item count)
        /// </summary>
        public static readonly DependencyProperty Number1Property = DependencyProperty.Register("Number1", typeof (string), typeof (DependencyViewActionWrapper), new PropertyMetadata("", (o, args) => UpdateViewActionFromLocalProperties(o, "Number1")));

        /// <summary>
        /// Second Text Element representing a number (such as an item count)
        /// </summary>
        /// <value>The number2.</value>
        public string Number2
        {
            get { return (string) GetValue(Number2Property); }
            set { SetValue(Number2Property, value); }
        }

        /// <summary>
        /// Second Text Element representing a number (such as an item count)
        /// </summary>
        public static readonly DependencyProperty Number2Property = DependencyProperty.Register("Number2", typeof (string), typeof (DependencyViewActionWrapper), new PropertyMetadata("", (o, args) => UpdateViewActionFromLocalProperties(o, "Number2")));

        /// <summary>
        /// The text to display on the tool tip when this item is hovered over with the mouse
        /// </summary>
        /// <value>The tool tip text.</value>
        public string ToolTipText
        {
            get { return (string) GetValue(ToolTipTextProperty); }
            set { SetValue(ToolTipTextProperty, value); }
        }

        /// <summary>
        /// The text to display on the tool tip when this item is hovered over with the mouse
        /// </summary>
        public static readonly DependencyProperty ToolTipTextProperty = DependencyProperty.Register("ToolTipText", typeof (string), typeof (DependencyViewActionWrapper), new PropertyMetadata("", (o, args) => UpdateViewActionFromLocalProperties(o, "ToolTipText")));

        /// <summary>
        /// Image Element 1
        /// </summary>
        /// <value>The image1.</value>
        public Brush Image1
        {
            get { return (Brush) GetValue(Image1Property); }
            set { SetValue(Image1Property, value); }
        }

        /// <summary>
        /// Image Element 1
        /// </summary>
        public static readonly DependencyProperty Image1Property = DependencyProperty.Register("Image1", typeof (Brush), typeof (DependencyViewActionWrapper), new PropertyMetadata(null, (o, args) => UpdateViewActionFromLocalProperties(o, "Image1")));

        /// <summary>
        /// Image Element 2
        /// </summary>
        /// <value>The image2.</value>
        public Brush Image2
        {
            get { return (Brush) GetValue(Image2Property); }
            set { SetValue(Image2Property, value); }
        }

        /// <summary>
        /// Image Element 2
        /// </summary>
        public static readonly DependencyProperty Image2Property = DependencyProperty.Register("Image2", typeof (Brush), typeof (DependencyViewActionWrapper), new PropertyMetadata(null, (o, args) => UpdateViewActionFromLocalProperties(o, "Image2")));

        /// <summary>
        /// Image Element 3
        /// </summary>
        /// <value>The image3.</value>
        public Brush Image3
        {
            get { return (Brush) GetValue(Image3Property); }
            set { SetValue(Image3Property, value); }
        }

        /// <summary>
        /// Image Element 3
        /// </summary>
        public static readonly DependencyProperty Image3Property = DependencyProperty.Register("Image3", typeof (Brush), typeof (DependencyViewActionWrapper), new PropertyMetadata(null, (o, args) => UpdateViewActionFromLocalProperties(o, "Image3")));

        /// <summary>
        /// Image Element 4
        /// </summary>
        /// <value>The image4.</value>
        public Brush Image4
        {
            get { return (Brush) GetValue(Image4Property); }
            set { SetValue(Image4Property, value); }
        }

        /// <summary>
        /// Image Element 4
        /// </summary>
        public static readonly DependencyProperty Image4Property = DependencyProperty.Register("Image4", typeof (Brush), typeof (DependencyViewActionWrapper), new PropertyMetadata(null, (o, args) => UpdateViewActionFromLocalProperties(o, "Image4")));

        /// <summary>
        /// Image Element 5
        /// </summary>
        /// <value>The image5.</value>
        public Brush Image5
        {
            get { return (Brush) GetValue(Image5Property); }
            set { SetValue(Image5Property, value); }
        }

        /// <summary>
        /// Image Element 5
        /// </summary>
        public static readonly DependencyProperty Image5Property = DependencyProperty.Register("Image5", typeof (Brush), typeof (DependencyViewActionWrapper), new PropertyMetadata(null, (o, args) => UpdateViewActionFromLocalProperties(o, "Image5")));

        /// <summary>
        /// Logo Element 1
        /// </summary>
        /// <value>The logo1.</value>
        public Brush Logo1
        {
            get { return (Brush) GetValue(Logo1Property); }
            set { SetValue(Logo1Property, value); }
        }

        /// <summary>
        /// Logo Element 1
        /// </summary>
        public static readonly DependencyProperty Logo1Property = DependencyProperty.Register("Logo1", typeof (Brush), typeof (DependencyViewActionWrapper), new PropertyMetadata(null, (o, args) => UpdateViewActionFromLocalProperties(o, "Logo1")));

        /// <summary>
        /// Logo Element 2
        /// </summary>
        /// <value>The logo2.</value>
        public Brush Logo2
        {
            get { return (Brush) GetValue(Logo2Property); }
            set { SetValue(Logo2Property, value); }
        }

        /// <summary>Generic color setting (expressed as a brush)</summary>
        public Brush Color1
        {
            get { return (Brush)GetValue(Color1Property); }
            set { SetValue(Color1Property, value); }
        }
        /// <summary>Generic color setting (expressed as a brush)</summary>
        public static readonly DependencyProperty Color1Property = DependencyProperty.Register("Color1", typeof(Brush), typeof(DependencyViewActionWrapper), new PropertyMetadata(null));

        /// <summary>Generic color setting (expressed as a brush)</summary>
        public Brush Color2
        {
            get { return (Brush)GetValue(Color2Property); }
            set { SetValue(Color2Property, value); }
        }
        /// <summary>Generic color setting (expressed as a brush)</summary>
        public static readonly DependencyProperty Color2Property = DependencyProperty.Register("Color2", typeof(Brush), typeof(DependencyViewActionWrapper), new PropertyMetadata(null));

        /// <summary>
        /// Logo Element 2
        /// </summary>
        public static readonly DependencyProperty Logo2Property = DependencyProperty.Register("Logo2", typeof (Brush), typeof (DependencyViewActionWrapper), new PropertyMetadata(null, (o, args) => UpdateViewActionFromLocalProperties(o, "Logo2")));

        /// <summary>
        /// String identifier to identify an action independent of its caption (and independent of the locale)
        /// </summary>
        /// <value>The identifier.</value>
        public string Id
        {
            get { return (string) GetValue(IdProperty); }
            set { SetValue(IdProperty, value); }
        }

        /// <summary>
        /// String identifier to identify an action independent of its caption (and independent of the locale)
        /// </summary>
        public static readonly DependencyProperty IdProperty = DependencyProperty.Register("Id", typeof (string), typeof (DependencyViewActionWrapper), new PropertyMetadata("", (o, args) => UpdateViewActionFromLocalProperties(o, "Id")));

        /// <summary>
        /// Caption (can be used to display in the UI)
        /// </summary>
        /// <value>The caption.</value>
        public string Caption
        {
            get { return (string) GetValue(CaptionProperty); }
            set { SetValue(CaptionProperty, value); }
        }

        /// <summary>
        /// Caption (can be used to display in the UI)
        /// </summary>
        public static readonly DependencyProperty CaptionProperty = DependencyProperty.Register("Caption", typeof (string), typeof (DependencyViewActionWrapper), new PropertyMetadata("", (o, args) => UpdateViewActionFromLocalProperties(o, "Caption")));

        /// <summary>
        /// Indicates whether this action starts a new group
        /// </summary>
        /// <value><c>true</c> if [begin group]; otherwise, <c>false</c>.</value>
        public bool BeginGroup
        {
            get { return (bool) GetValue(BeginGroupProperty); }
            set { SetValue(BeginGroupProperty, value); }
        }

        /// <summary>
        /// Indicates whether this action starts a new group
        /// </summary>
        public static readonly DependencyProperty BeginGroupProperty = DependencyProperty.Register("BeginGroup", typeof (bool), typeof (DependencyViewActionWrapper), new PropertyMetadata(false, (o, args) => UpdateViewActionFromLocalProperties(o, "BeginGroup")));

        /// <summary>
        /// Indicates the group title for items that start a new group
        /// </summary>
        /// <value>The group title.</value>
        public string GroupTitle
        {
            get { return (string) GetValue(GroupTitleProperty); }
            set { SetValue(GroupTitleProperty, value); }
        }

        /// <summary>
        /// Indicates the group title for items that start a new group
        /// </summary>
        public static readonly DependencyProperty GroupTitleProperty = DependencyProperty.Register("GroupTitle", typeof (string), typeof (DependencyViewActionWrapper), new PropertyMetadata("", (o, args) => UpdateViewActionFromLocalProperties(o, "GroupTitle")));

        /// <summary>
        /// Is this the default action?
        /// </summary>
        /// <value><c>true</c> if this instance is default; otherwise, <c>false</c>.</value>
        public bool IsDefault
        {
            get { return (bool) GetValue(IsDefaultProperty); }
            set { SetValue(IsDefaultProperty, value); }
        }

        /// <summary>
        /// Is this the default action?
        /// </summary>
        public static readonly DependencyProperty IsDefaultProperty = DependencyProperty.Register("IsDefault", typeof (bool), typeof (DependencyViewActionWrapper), new PropertyMetadata(false, (o, args) => UpdateViewActionFromLocalProperties(o, "IsDefault")));

        /// <summary>
        /// Is this the cancel action?
        /// </summary>
        /// <value><c>true</c> if this instance is cancel; otherwise, <c>false</c>.</value>
        public bool IsCancel
        {
            get { return (bool) GetValue(IsCancelProperty); }
            set { SetValue(IsCancelProperty, value); }
        }

        /// <summary>
        /// Is this the cancel action?
        /// </summary>
        public static readonly DependencyProperty IsCancelProperty = DependencyProperty.Register("IsCancel", typeof (bool), typeof (DependencyViewActionWrapper), new PropertyMetadata(false, (o, args) => UpdateViewActionFromLocalProperties(o, "IsCancel")));

        /// <summary>
        /// Indicates whether an action is pinned (which is used for different things in different themes)
        /// </summary>
        /// <value><c>true</c> if this instance is pinned; otherwise, <c>false</c>.</value>
        public bool IsPinned
        {
            get { return (bool) GetValue(IsPinnedProperty); }
            set { SetValue(IsPinnedProperty, value); }
        }

        /// <summary>
        /// Indicates whether an action is pinned (which is used for different things in different themes)
        /// </summary>
        public static readonly DependencyProperty IsPinnedProperty = DependencyProperty.Register("IsPinnedl", typeof (bool), typeof (DependencyViewActionWrapper), new PropertyMetadata(false, (o, args) => UpdateViewActionFromLocalProperties(o, "IsPinned")));

        /// <summary>
        /// Indicates whether the action is to be considered "checked"
        /// </summary>
        /// <value><c>true</c> if this instance is checked; otherwise, <c>false</c>.</value>
        /// <remarks>Cecked actions may be presented in various ways in different themes, such as having a check-mark in menus
        /// Most themes will only respect this property when ViewActionType = Toggle</remarks>
        public bool IsChecked
        {
            get { return (bool) GetValue(IsCheckedProperty); }
            set { SetValue(IsCheckedProperty, value); }
        }

        /// <summary>
        /// Indicates whether the action is to be considered "checked"
        /// </summary>
        public static readonly DependencyProperty IsCheckedProperty = DependencyProperty.Register("IsChecked", typeof (bool), typeof (DependencyViewActionWrapper), new PropertyMetadata(false, (o, args) => UpdateViewActionFromLocalProperties(o, "IsChecked")));

        /// <summary>
        /// Indicates that this view action is selected by default if the theme supports pre-selecting actions in some way (such as showing the page of the ribbon the action is in, or triggering the action in a special Office-style file menu).
        /// </summary>
        /// <value><c>true</c> if this instance is default selection; otherwise, <c>false</c>.</value>
        /// <remarks>If more than one action is flagged as the default selection, then the last one (in instantiation order) 'wins'</remarks>
        public bool IsDefaultSelection
        {
            get { return (bool) GetValue(IsDefaultSelectionProperty); }
            set { SetValue(IsDefaultSelectionProperty, value); }
        }

        /// <summary>
        /// Indicates that this view action is selected by default if the theme supports pre-selecting actions in some way (such as showing the page of the ribbon the action is in, or triggering the action in a special Office-style file menu).
        /// </summary>
        public static readonly DependencyProperty IsDefaultSelectionProperty = DependencyProperty.Register("IsDefaultSelection", typeof (bool), typeof (DependencyViewActionWrapper), new PropertyMetadata(false, (o, args) => UpdateViewActionFromLocalProperties(o, "IsDefaultSelection")));

        /// <summary>
        /// Gets or sets a value indicating whether this instance is local action.
        /// </summary>
        /// <value><c>true</c> if this instance is local action; otherwise, <c>false</c>.</value>
        public bool IsLocalAction
        {
            get { return (bool) GetValue(IsLocalActionProperty); }
            set { SetValue(IsLocalActionProperty, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is local action.
        /// </summary>
        public static readonly DependencyProperty IsLocalActionProperty = DependencyProperty.Register("IsLocalAction", typeof (bool), typeof (DependencyViewActionWrapper), new PropertyMetadata(false, (o, args) => UpdateViewActionFromLocalProperties(o, "IsLocalAction")));

        /// <summary>
        /// Indicates the type of the view action
        /// </summary>
        /// <value>The type of the view action.</value>
        public ViewActionTypes ViewActionType
        {
            get { return (ViewActionTypes) GetValue(ViewActionTypeProperty); }
            set { SetValue(ViewActionTypeProperty, value); }
        }

        /// <summary>
        /// Indicates the type of the view action
        /// </summary>
        public static readonly DependencyProperty ViewActionTypeProperty = DependencyProperty.Register("ViewActionType", typeof(ViewActionTypes), typeof(DependencyViewActionWrapper), new PropertyMetadata(ViewActionTypes.Standard, (o, args) => UpdateViewActionFromLocalProperties(o, "ViewActionType")));

        /// <summary>
        /// Indicates whether or not this action is at all available (often translates directly to being visible or invisible)
        /// </summary>
        /// <value>The availability.</value>
        public ViewActionAvailabilities Availability
        {
            get { return (ViewActionAvailabilities) GetValue(AvailabilityProperty); }
            set { SetValue(AvailabilityProperty, value); }
        }

        /// <summary>
        /// Indicates whether or not this action is at all available (often translates directly to being visible or invisible)
        /// </summary>
        public static readonly DependencyProperty AvailabilityProperty = DependencyProperty.Register("Availability", typeof(ViewActionAvailabilities), typeof(DependencyViewActionWrapper), new PropertyMetadata(ViewActionAvailabilities.Unknown, (o, args) => UpdateViewActionFromLocalProperties(o, "Availability")));

        /// <summary>
        /// Defines view action visibility (collapsed or hidden items are may be removed from menus or ribbons independent of their availability or can-execute state)
        /// </summary>
        /// <value>The visibility.</value>
        public Visibility Visibility
        {
            get { return (Visibility) GetValue(VisibilityProperty); }
            set { SetValue(VisibilityProperty, value); }
        }

        /// <summary>
        /// Defines view action visibility (collapsed or hidden items are may be removed from menus or ribbons independent of their availability or can-execute state)
        /// </summary>
        public static readonly DependencyProperty VisibilityProperty = DependencyProperty.Register("Visibility", typeof(Visibility), typeof(DependencyViewActionWrapper), new PropertyMetadata(Visibility.Visible, (o, args) => UpdateViewActionFromLocalProperties(o, "Visibility")));

        /// <summary>
        /// Visible if IsChecked = true
        /// </summary>
        /// <value>The IsChecked visibility.</value>
        public Visibility IsChecked_Visible
        {
            get { return (Visibility)GetValue(IsChecked_VisibleProperty); }
            set { SetValue(IsChecked_VisibleProperty, value); }
        }
        /// <summary>
        /// Visible if IsChecked = true
        /// </summary>
        public static readonly DependencyProperty IsChecked_VisibleProperty = DependencyProperty.Register("IsChecked_Visible", typeof(Visibility), typeof(DependencyViewActionWrapper), new PropertyMetadata(Visibility.Collapsed));
        
        /// <summary>
        /// Significance of the action
        /// </summary>
        /// <value>The significance.</value>
        public ViewActionSignificance Significance
        {
            get { return (ViewActionSignificance) GetValue(SignificanceProperty); }
            set { SetValue(SignificanceProperty, value); }
        }

        /// <summary>
        /// Significance of the action
        /// </summary>
        public static readonly DependencyProperty SignificanceProperty = DependencyProperty.Register("Significance", typeof(ViewActionSignificance), typeof(DependencyViewActionWrapper), new PropertyMetadata(ViewActionSignificance.Normal, (o, args) => UpdateViewActionFromLocalProperties(o, "Significance")));

        /// <summary>
        /// Logical list of categories
        /// </summary>
        /// <value>The categories.</value>
        public List<ViewActionCategory> Categories
        {
            get { return (List<ViewActionCategory>) GetValue(CategoriesProperty); }
            set { SetValue(CategoriesProperty, value); }
        }

        /// <summary>
        /// Logical list of categories
        /// </summary>
        public static readonly DependencyProperty CategoriesProperty = DependencyProperty.Register("Categories", typeof (List<ViewActionCategory>), typeof (DependencyViewActionWrapper), new PropertyMetadata(null, (o, args) => UpdateViewActionFromLocalProperties(o, "Categories")));

        /// <summary>
        /// Sort order for the category
        /// </summary>
        /// <value>The category order.</value>
        public int CategoryOrder
        {
            get { return (int) GetValue(CategoryOrderProperty); }
            set { SetValue(CategoryOrderProperty, value); }
        }

        /// <summary>
        /// Sort order for the category
        /// </summary>
        public static readonly DependencyProperty CategoryOrderProperty = DependencyProperty.Register("CategoryOrder", typeof (int), typeof (DependencyViewActionWrapper), new PropertyMetadata(0, (o, args) => UpdateViewActionFromLocalProperties(o, "CategoryOrder")));

        /// <summary>
        /// Sort order for the action (within a group)
        /// </summary>
        /// <value>The order.</value>
        public int Order
        {
            get { return (int) GetValue(OrderProperty); }
            set { SetValue(OrderProperty, value); }
        }

        /// <summary>
        /// Sort order for the action (within a group)
        /// </summary>
        public static readonly DependencyProperty OrderProperty = DependencyProperty.Register("Order", typeof (int), typeof (DependencyViewActionWrapper), new PropertyMetadata(0, (o, args) => UpdateViewActionFromLocalProperties(o, "Order")));

        /// <summary>
        /// Returns the ID of the first category or an empty string if no categories have been added
        /// </summary>
        /// <value>The first category identifier.</value>
        public string FirstCategoryId
        {
            get { return (string) GetValue(FirstCategoryIdProperty); }
            set { SetValue(FirstCategoryIdProperty, value); }
        }

        /// <summary>
        /// Returns the ID of the first category or an empty string if no categories have been added
        /// </summary>
        public static readonly DependencyProperty FirstCategoryIdProperty = DependencyProperty.Register("FirstCategoryId", typeof (string), typeof (DependencyViewActionWrapper), new PropertyMetadata(string.Empty, (o, args) => UpdateViewActionFromLocalProperties(o, "FirstCategoryId")));

        /// <summary>
        /// A view model dedicated to this action
        /// </summary>
        /// <value>The action view model.</value>
        public object ActionViewModel
        {
            get { return GetValue(ActionViewModelProperty); }
            set { SetValue(ActionViewModelProperty, value); }
        }

        /// <summary>
        /// A view model dedicated to this action
        /// </summary>
        public static readonly DependencyProperty ActionViewModelProperty = DependencyProperty.Register("ActionViewModel", typeof (object), typeof (DependencyViewActionWrapper), new PropertyMetadata(null, (o, args) => UpdateViewActionFromLocalProperties(o, "ActionViewModel")));

        /// <summary>
        /// A view specific to this action
        /// </summary>
        /// <value>The action view.</value>
        public FrameworkElement ActionView
        {
            get { return (FrameworkElement) GetValue(ActionViewProperty); }
            set { SetValue(ActionViewProperty, value); }
        }

        /// <summary>
        /// A view specific to this action
        /// </summary>
        public static readonly DependencyProperty ActionViewProperty = DependencyProperty.Register("ActionView", typeof (FrameworkElement), typeof (DependencyViewActionWrapper), new PropertyMetadata(null, (o, args) => UpdateViewActionFromLocalProperties(o, "ActionView")));

        /// <summary>
        /// List of roles with access to this action
        /// </summary>
        /// <value>The user roles.</value>
        public string[] UserRoles
        {
            get { return (string[]) GetValue(UserRolesProperty); }
            set { SetValue(UserRolesProperty, value); }
        }

        /// <summary>
        /// List of roles with access to this action
        /// </summary>
        public static readonly DependencyProperty UserRolesProperty = DependencyProperty.Register("UserRoles", typeof (string[]), typeof (DependencyViewActionWrapper), new PropertyMetadata(new string[0], (o, args) => UpdateViewActionFromLocalProperties(o, "UserRoles")));

        /// <summary>
        /// Defines the access key of the action (such as the underlined key in the menu)
        /// </summary>
        /// <value>The access key.</value>
        /// <remarks>Not all themes will pick this setting up</remarks>
        public char AccessKey
        {
            get { return (char) GetValue(AccessKeyProperty); }
            set { SetValue(AccessKeyProperty, value); }
        }

        /// <summary>
        /// Defines the access key of the action (such as the underlined key in the menu)
        /// </summary>
        public static readonly DependencyProperty AccessKeyProperty = DependencyProperty.Register("AccessKey", typeof (char), typeof (DependencyViewActionWrapper), new PropertyMetadata(' ', (o, args) => UpdateViewActionFromLocalProperties(o, "AccessKey")));

        /// <summary>
        /// Shortcut key
        /// </summary>
        /// <value>The shortcut key.</value>
        /// <remarks>Not all themes will pick this setting up</remarks>
        public Key ShortcutKey
        {
            get { return (Key) GetValue(ShortcutKeyProperty); }
            set { SetValue(ShortcutKeyProperty, value); }
        }

        /// <summary>
        /// Shortcut key
        /// </summary>
        public static readonly DependencyProperty ShortcutKeyProperty = DependencyProperty.Register("ShortcutKey", typeof (Key), typeof (DependencyViewActionWrapper), new PropertyMetadata((Key)0, (o, args) => UpdateViewActionFromLocalProperties(o, "ShortcutKey")));

        /// <summary>
        /// Modifier for the shortcut key
        /// </summary>
        /// <value>The shortcut modifier keys.</value>
        /// <remarks>Not all themes will pick this setting up</remarks>
        public ModifierKeys ShortcutModifiers
        {
            get { return (ModifierKeys) GetValue(ShortcutModifiersProperty); }
            set { SetValue(ShortcutModifiersProperty, value); }
        }

        /// <summary>
        /// Modifier for the shortcut key
        /// </summary>
        public static readonly DependencyProperty ShortcutModifiersProperty = DependencyProperty.Register("ShortcutModifiers", typeof (ModifierKeys), typeof (DependencyViewActionWrapper), new PropertyMetadata((ModifierKeys)0, (o, args) => UpdateViewActionFromLocalProperties(o, "ShortcutModifiers")));

        /// <summary>
        /// Indicates that previous CanExecute() results have become invalid and need to be re-evaluated.
        /// </summary>
        /// <remarks>This method should simply fire the CanExecuteChanged event.</remarks>
        public void InvalidateCanExecute()
        {
            _wrappedAction.InvalidateCanExecute();
        }

        /// <summary>
        /// Defines the method that determines whether the command can execute in its current state.
        /// </summary>
        /// <param name="parameter">Data used by the command.  If the command does not require data to be passed, this object can be set to null.</param>
        /// <returns>true if this command can be executed; otherwise, false.</returns>
        public bool CanExecute(object parameter)
        {
            return _wrappedAction.CanExecute(parameter);
        }

        /// <summary>
        /// Occurs when changes occur that affect whether or not the command should execute.
        /// </summary>
        public event EventHandler CanExecuteChanged;

        /// <summary>
        /// Defines the method to be called when the command is invoked.
        /// </summary>
        /// <param name="parameter">Data used by the command.  If the command does not require data to be passed, this object can be set to null.</param>
        public void Execute(object parameter)
        {
            _wrappedAction.Execute(parameter);
        }
    }

    /// <summary>Interface that can be implemented by objects that support explicit close events</summary>
    public interface IClosable
    {
        /// <summary>Occurs when the system is getting ready to close (has not started closing yet)</summary>
        event EventHandler<CancelEventArgs> BeforeClosing;

        /// <summary>Occurs when the object is closing (has not closed yet)</summary>
        event EventHandler Closing;

        /// <summary>Occurs when the object has closed (has finished closing)</summary>
        event EventHandler Closed;

        /// <summary>This method can be used to raise the before closing event</summary>
        /// <returns>True, if closing has been canceled</returns>
        bool RaiseBeforeClosingEvent();

        /// <summary>This method can be used to raise the closing event</summary>
        void RaiseClosingEvent();

        /// <summary>This method can be used to raise the closed event</summary>
        void RaiseClosedEvent();
    }

    /// <summary>
    /// This class provides attached property related to the IClosable interface
    /// </summary>
    /// <seealso cref="System.Windows.DependencyObject" />
    public class Closable : DependencyObject
    {
        /// <summary>
        /// If set to true on a window object, and the data context implements IClosable, then closing events will automatically be forwarded to the data context object
        /// </summary>
        public static bool GetRaiseClosingEvents(DependencyObject d)
        {
            return (bool) d.GetValue(RaiseClosingEventsProperty);
        }
        /// <summary>
        /// If set to true on a window object, and the data context implements IClosable, then closing events will automatically be forwarded to the data context object
        /// </summary>
        /// <param name="d">The d.</param>
        /// <param name="value">if set to <c>true</c> [value].</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public static void SetRaiseClosingEvents(DependencyObject d, bool value)
        {
            d.SetValue(RaiseClosingEventsProperty, value);
        }

        /// <summary>
        /// If set to true on a window object, and the data context implements IClosable, then closing events will automatically be forwarded to the data context object
        /// </summary>
        public static readonly DependencyProperty RaiseClosingEventsProperty = DependencyProperty.RegisterAttached("RaiseClosingEvents", typeof(bool), typeof(Closable), new PropertyMetadata(false, OnRaiseClosingEventsChanged));

        /// <summary>
        /// Fires when the property changes
        /// </summary>
        /// <param name="d">The d.</param>
        /// <param name="e">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void OnRaiseClosingEventsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(bool) e.NewValue) return;
            var window = d as Window;
            if (window == null) return;

            var closable = window.DataContext as IClosable;
            if (closable == null) return;

            window.Closing += (s, e2) =>
            {
                var window2 = s as Window;
                if (window2 == null) return;
                var closable2 = window2.DataContext as IClosable;
                if (closable2 == null) return;
                Controller.CloseViewForModel(closable2);
            };
        }
    }

    /// <summary>Interface that can be implemented by objects that support explicit open events</summary>
    public interface IOpenable
    {
        /// <summary>Occurs when the object is opening (has not opened yet)</summary>
        event EventHandler Opening;

        /// <summary>Occurs when the object has opened (has finished opening)</summary>
        event EventHandler Opened;

        /// <summary>This method can be used to raise the opening event</summary>
        void RaiseOpeningEvent();

        /// <summary>This method can be used to raise the open event</summary>
        void RaiseOpenedEvent();
    }

    /// <summary>Special view action used by message boxes</summary>
    public class MessageBoxViewAction : ViewAction
    {
        /// <summary>Constructor</summary>
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
        public MessageBoxViewAction(string caption = "",
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
                base(caption, beginGroup, execute, canExecute, visualResourceKey, category, categoryCaption, categoryOrder, isDefault, isCancel, significance, userRoles,
                    brushResourceKey, logoBrushResourceKey, groupTitle, order, accessKey, shortcutKey, shortcutKeyModifiers, categoryAccessKey, isDefaultSelection, isPinned, id, standardIcon)
        {
        }

        /// <summary>
        /// Reference to the utilized message box view model
        /// </summary>
        public MessageBoxViewModel Model { get; set; }
    }
}
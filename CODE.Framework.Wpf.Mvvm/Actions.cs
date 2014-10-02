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
using CODE.Framework.Core.Utilities;
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
        ObservableCollection<IViewAction> Actions { get; }

        /// <summary>
        /// Fires when the list of actions changed (assuming change notification is active)
        /// </summary>
        event NotifyCollectionChangedEventHandler ActionsChanged;
    }

    /// <summary>
    /// Interface defining action features beyond basic command features
    /// </summary>
    public interface IViewAction : ICommand
    {
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
        /// Indicates that this view action is selected by default if the theme supports pre-selecting actions in some way (such as showing the page of the ribbon the action is in, or triggering the action in a special Office-style file menu).
        /// </summary>
        /// <remarks>If more than one action is flagged as the default selection, then the last one (in instantiation order) 'wins'</remarks>
        bool IsDefaultSelection { get; set; }
        /// <summary>
        /// Indicates whether or not this action is at all available (often translates directly to being visible or invisible)
        /// </summary>
        ViewActionAvailabilities Availability { get; }
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
        int CategoryOrder { get;  }

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
    public class ViewActionCategory : IComparable
    {
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
        public string Caption { get; set; }

        /// <summary>
        /// Indicates whether the category belongs to local views
        /// </summary>
        public bool IsLocalCategory { get; set; }

        /// <summary>
        /// Category order
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// Access key for this category
        /// </summary>
        public char AccessKey { get; set; }

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
        /// <param name="brushResourceKey">Resource key for a visual derrived from a brush.</param>
        /// <param name="logoBrushResourceKey">Resource key for a visual (used for Logo1) derrived from a brush.</param>
        /// <param name="groupTitle">The group title.</param>
        /// <param name="order">The order of the view action (within a group)</param>
        /// <param name="accessKey">The access key for this action (such as the underlined character in a menu if the action is linked to a menu).</param>
        /// <param name="shortcutKey">The shortcut key for the action (usually a hot key that can be pressed without a menu being opened or anything along those lines).</param>
        /// <param name="shortcutKeyModifiers">Modifier for the shortcut key (typically CTRL).</param>
        /// <param name="categoryAccessKey">Access key for the category (only used if a category is assigned).</param>
        /// <param name="isDefaultSelection">Indicates whether this action shall be selected by default</param>
        /// <param name="isPinned">Indicates whether this action is considered to be pinned</param>
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
            bool isPinned = false)
        {

            PropertyChanged += (s, e) =>
                                   {
                                       if (_inBrushUpdating) return;
                                       if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName.StartsWith("Image") || e.PropertyName.StartsWith("Logo"))
                                           CheckAllBrushesForResources();
                                   };
            
            Caption = caption;
            BeginGroup = beginGroup;
            _executeDelegate = execute;
            _canExecuteDelegate = canExecute;
            VisualResourceKey = visualResourceKey;
            BrushResourceKey = brushResourceKey;
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
        private string _brushResourceKey;

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
                    if (!string.IsNullOrEmpty(BrushResourceKey))
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

                        var icon = resourceSearchContext != null ? resourceSearchContext.FindResource(BrushResourceKey) as Brush : Application.Current.FindResource(BrushResourceKey) as Brush;

                        if (brushResources.Count > 0) // We may have some resources we need to replace
                                If.Real<DrawingBrush>(icon, drawing => ResourceHelper.ReplaceDynamicDrawingBrushResources(drawing, brushResources));

                        _latestBrush = icon;
                        NotifyChanged();
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
                        _brushResourceKey = "CODE.Framework-Icon-MissingIcon"; // Must use internal field here, otherwise all kinds of stuff gets triggered!!!
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

        /// <summary>Fires the CanExecuteChanged event to force a re-evaluation of the CanExecute method</summary>
        public void InvalidateCanExecute()
        {
            if (CanExecuteChanged != null)
                CanExecuteChanged(this, new EventArgs());
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
        public void Execute(object parameter)
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
        public bool IsPinned { get; set; }

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
                    //NotifyChanged("Availability");
                }
                lock (this)
                    return _availability;
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
        public int CategoryOrder { get; set;  }

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

        /// <summary>Image Element 1</summary>
        public Brush Image1
        {
            get
            {
                var brush = Brush;
                if (brush == null && !string.IsNullOrEmpty(VisualResourceKey))
                {
                    var visual = Visual;
                    if (visual != null)
                        brush = new VisualBrush(visual) {Stretch = Stretch.Uniform};
                }
                return brush;
            }
            set { /* Nothing to do*/ }
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
                            resourceSearchContext = (FrameworkElement)ResourceContextObject;
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
            set { /* Nothing to do*/ }
        }

        /// <summary>Logo Element 2</summary>
        public Brush Logo2 { get; set; }

        private bool _inBrushUpdating;

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

    /// <summary>Interface that can be implemented by objects that support explicit close events</summary>
    public interface IClosable
    {
        /// <summary>Occurs when the object is closing (has not closed yet)</summary>
        event EventHandler Closing;
        /// <summary>Occurs when the object has closed (has finished closing)</summary>
        event EventHandler Closed;

        /// <summary>This method can be used to raise the closing event</summary>
        void RaiseClosingEvent();
        /// <summary>This method can be used to raise the closed event</summary>
        void RaiseClosedEvent();
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
        public MessageBoxViewAction(string caption = "",
            bool beginGroup = false,
            Action<IViewAction, object> execute = null,
            Func<IViewAction, object, bool> canExecute = null,
            string visualResourceKey = "",
            string category = "", string categoryCaption = "", int categoryOrder = 0,
            bool isDefault = false, bool isCancel = false,
            ViewActionSignificance significance = ViewActionSignificance.Normal,
            string[] userRoles = null) :
            base(caption, beginGroup, execute, canExecute, visualResourceKey, category, categoryCaption, categoryOrder, isDefault, isCancel, significance, userRoles)
        {
        }

        /// <summary>
        /// Reference to the utilized message box view model
        /// </summary>
        public MessageBoxViewModel  Model { get; set; }
    }
}

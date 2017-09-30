using CODE.Framework.Wpf.Interfaces;
using CODE.Framework.Wpf.Layout;
using CODE.Framework.Wpf.Utilities;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Shapes;

namespace CODE.Framework.Wpf.Mvvm
{
    /// <summary>
    /// Default top level shell
    /// </summary>
    public class Shell : Window, IViewHandler
    {
        /// <summary>
        /// Reference to the current (last loaded) Shell
        /// </summary>
        /// <value>The current Shell.</value>
        public static Shell Current { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Shell"/> class.
        /// </summary>
        public Shell()
        {
            NormalViews = new ObservableCollection<ViewResult>();
            TopLevelViews = new ObservableCollection<ViewResult>();

            SetResourceReference(StyleProperty, "CODE.Framework.Wpf.Mvvm.Shell-Style");

            Current = this;

            Activated += (s, e) =>
                {
                    Current = this; // If there is more than one open Shell window, we switch the Current pointer whenever the shell is activated
                    Controller.RegisterViewHandler(this, "Shell"); // Similarly, we switch the shell view handler, so whatever views need to be handled by the shell, will be handled by the active one
                };
        }

        /// <summary>
        /// Currently open top-level views
        /// </summary>
        public ObservableCollection<ViewResult> TopLevelViews
        {
            get { return (ObservableCollection<ViewResult>)GetValue(TopLevelViewsProperty); }
            set { SetValue(TopLevelViewsProperty, value); }
        }
        /// <summary>
        /// Dependency property for all modal views
        /// </summary>
        public static readonly DependencyProperty TopLevelViewsProperty = DependencyProperty.Register("TopLevelViews", typeof(ObservableCollection<ViewResult>), typeof(Shell), new UIPropertyMetadata(null));

        /// <summary>Defines the launch mode (in-place or popup) for top level views</summary>
        public ViewLaunchMode TopLevelViewLaunchMode
        {
            get { return (ViewLaunchMode)GetValue(TopLevelViewLaunchModeProperty); }
            set { SetValue(TopLevelViewLaunchModeProperty, value); }
        }
        /// <summary>Defines the launch mode (in-place or popup) for top level views</summary>
        public static readonly DependencyProperty TopLevelViewLaunchModeProperty = DependencyProperty.Register("TopLevelViewLaunchMode", typeof(ViewLaunchMode), typeof(Shell), new UIPropertyMetadata(ViewLaunchMode.Popup));

        /// <summary>Maximum number of concurrently opened top level views (-1 = unlimited)</summary>
        /// <remarks>
        /// Only counts in-place activated top level views. This does not limit the number of potentially launched windows.
        /// When the Shell tries to open more views than allowed, the oldest view in the list will be closed.
        /// </remarks>
        public int MaximumTopLevelViewCount
        {
            get { return (int)GetValue(MaximumTopLevelViewCountProperty); }
            set { SetValue(MaximumTopLevelViewCountProperty, value); }
        }
        /// <summary>Maximum number of concurrently opened top level views (-1 = unlimited)</summary>
        /// <remarks>
        /// Only counts in-place activated top level views. This does not limit the number of potentially launched windows.
        /// When the Shell tries to open more views than allowed, the oldest view in the list will be closed.
        /// </remarks>
        public static readonly DependencyProperty MaximumTopLevelViewCountProperty = DependencyProperty.Register("MaximumTopLevelViewCount", typeof(int), typeof(Shell), new PropertyMetadata(-1));

        /// <summary>
        /// Currently open standard views
        /// </summary>
        public ObservableCollection<ViewResult> NormalViews
        {
            get { return (ObservableCollection<ViewResult>)GetValue(NormalViewsProperty); }
            set { SetValue(NormalViewsProperty, value); }
        }
        /// <summary>
        /// Dependency property for all normal views
        /// </summary>
        public static readonly DependencyProperty NormalViewsProperty = DependencyProperty.Register("NormalViews", typeof(ObservableCollection<ViewResult>), typeof(Shell), new UIPropertyMetadata(null));

        /// <summary>Defines the launch mode (in-place or popup) for top level views</summary>
        public ViewLaunchMode NormalViewLaunchMode
        {
            get { return (ViewLaunchMode)GetValue(NormalViewLaunchModeProperty); }
            set { SetValue(NormalViewLaunchModeProperty, value); }
        }
        /// <summary>Defines the launch mode (in-place or popup) for top level views</summary>
        public static readonly DependencyProperty NormalViewLaunchModeProperty = DependencyProperty.Register("NormalViewLaunchMode", typeof(ViewLaunchMode), typeof(Shell), new UIPropertyMetadata(ViewLaunchMode.InPlace));

        /// <summary>Maximum number of concurrently opened top level views (-1 = unlimited)</summary>
        /// <remarks>When the Shell tries to open more views than allowed, the oldest view in the list will be closed.</remarks>
        public int MaximumNormalViewCount
        {
            get { return (int)GetValue(MaximumNormalViewCountProperty); }
            set { SetValue(MaximumNormalViewCountProperty, value); }
        }
        /// <summary>Maximum number of concurrently opened normal views (-1 = unlimited)</summary>
        /// <remarks>When the Shell tries to open more views than allowed, the oldest view in the list will be closed.</remarks>
        public static readonly DependencyProperty MaximumNormalViewCountProperty = DependencyProperty.Register("MaximumNormalViewCount", typeof(int), typeof(Shell), new PropertyMetadata(-1));

        /// <summary>Index of the currently selected normal view (-1 if no view is selected)</summary>
        public int SelectedNormalView
        {
            get { return (int)GetValue(SelectedNormalViewProperty); }
            set { SetValue(SelectedNormalViewProperty, value); }
        }
        /// <summary>Index of the currently selected normal view (-1 if no view is selected)</summary>
        public static readonly DependencyProperty SelectedNormalViewProperty = DependencyProperty.Register("SelectedNormalView", typeof(int), typeof(Shell), new UIPropertyMetadata(-1, OnSelectedNormalViewChanged));

        /// <summary>Desired zoom factor for hosted views (may or may not be supported by individual themes)</summary>
        public double DesiredContentZoomFactor
        {
            get { return (double)GetValue(DesiredContentZoomFactorProperty); }
            set { SetValue(DesiredContentZoomFactorProperty, value); }
        }
        /// <summary>Desired zoom factor for hosted views (may or may not be supported by individual themes)</summary>
        public static readonly DependencyProperty DesiredContentZoomFactorProperty = DependencyProperty.Register("DesiredContentZoomFactor", typeof(double), typeof(Shell), new PropertyMetadata(1d));

        /// <summary>
        /// Called when the selected normal view changes
        /// </summary>
        /// <param name="source">The dependency object.</param>
        /// <param name="args">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        private static void OnSelectedNormalViewChanged(DependencyObject source, DependencyPropertyChangedEventArgs args)
        {
            var shell = source as Shell;
            if (shell == null) return;

            var viewIndex = -1;
            try
            {
                viewIndex = (int) args.NewValue;
            }
            catch (Exception)
            {
                return;
            }

            if (viewIndex == -1 || viewIndex > shell.NormalViews.Count - 1)
            {
                shell.Title = shell._originalTitle;
                return;
            }

            switch (shell.TitleMode)
            {
                case ShellTitleMode.OriginalTitleOnly:
                    shell.Title = shell._originalTitle;
                    break;
                case ShellTitleMode.OriginalTitleDashViewTitle:
                    if (shell.NormalViews.Count == 0)
                        shell.Title = shell._originalTitle;
                    else
                        shell.Title = shell._originalTitle + " - " + shell.NormalViews[viewIndex].ViewTitle;
                    break;
                case ShellTitleMode.ViewTitleDashOriginalTitle:
                    if (shell.NormalViews.Count == 0)
                        shell.Title = shell._originalTitle;
                    else
                        shell.Title = shell.NormalViews[viewIndex].ViewTitle + " - " + shell._originalTitle;
                    break;
                case ShellTitleMode.ViewTitleOnly:
                    if (shell.NormalViews.Count == 0)
                        shell.Title = shell._originalTitle;
                    else
                        shell.Title = shell.NormalViews[viewIndex].ViewTitle;
                    break;
            }

            var view = shell.NormalViews[viewIndex].View;
            var activatingView = view as IUserInterfaceActivation;
            if (activatingView != null) activatingView.RaiseActivated();
        }

        /// <summary>Complete view result for the currently selected normal view (null if no view is selected)</summary>
        /// <remarks>Can be used to bind the current view into a content control using {Binding SelectedNormalViewResult.Document}</remarks>
        public ViewResult SelectedNormalViewResult
        {
            get { return (ViewResult)GetValue(SelectedNormalViewResultProperty); }
            set { SetValue(SelectedNormalViewResultProperty, value); }
        }
        /// <summary>Complete view result for the currently selected normal view (null if no view is selected)</summary>
        /// <remarks>Can be used to bind the current view into a content control using {Binding SelectedNormalViewResult.Document}</remarks>
        public static readonly DependencyProperty SelectedNormalViewResultProperty = DependencyProperty.Register("SelectedNormalViewResult", typeof(ViewResult), typeof(Shell), new UIPropertyMetadata(null));

        /// <summary>Number of open normal views</summary>
        public int NormalViewCount
        {
            get { return (int)GetValue(NormalViewCountProperty); }
            set { SetValue(NormalViewCountProperty, value); }
        }
        /// <summary>Number of open normal views</summary>
        public static readonly DependencyProperty NormalViewCountProperty = DependencyProperty.Register("NormalViewCount", typeof(int), typeof(Shell), new UIPropertyMetadata(0));

        /// <summary>Index of the currently selected top-level view (-1 if no view is selected)</summary>
        public int SelectedTopLevelView
        {
            get { return (int)GetValue(SelectedTopLevelViewProperty); }
            set { SetValue(SelectedTopLevelViewProperty, value); }
        }
        /// <summary>Index of the currently selected top-level view (-1 if no view is selected)</summary>
        public static readonly DependencyProperty SelectedTopLevelViewProperty = DependencyProperty.Register("SelectedTopLevelView", typeof(int), typeof(Shell), new UIPropertyMetadata(-1, OnSelectedTopLevelViewChanged));

        /// <summary>
        /// Fires when the selected top leve view changes
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="args">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        private static void OnSelectedTopLevelViewChanged(DependencyObject source, DependencyPropertyChangedEventArgs args)
        {
            var shell = source as Shell;
            if (shell == null) return;

            var viewIndex = -1;
            try
            {
                viewIndex = (int)args.NewValue;
            }
            catch (Exception)
            {
                return;
            }
            if (viewIndex == -1 || viewIndex > shell.TopLevelViews.Count - 1) return;

            var view = shell.TopLevelViews[viewIndex].View;
            var activatingView = view as IUserInterfaceActivation;
            if (activatingView != null) activatingView.RaiseActivated();
        }

        /// <summary>
        /// For internal use only
        /// </summary>
        /// <value>
        /// The view index to activate.
        /// </value>
        public int ViewIndexToActivate
        {
            get { return (int)GetValue(ViewIndexToActivateProperty); }
            set { SetValue(ViewIndexToActivateProperty, value); }
        }

        /// <summary>
        /// For internal use only
        /// </summary>
        /// <value>
        /// The view index to activate.
        /// </value>
        public static readonly DependencyProperty ViewIndexToActivateProperty = DependencyProperty.Register("ViewIndexToActivate", typeof(int), typeof(Shell), new PropertyMetadata(-1));

        /// <summary>
        /// For internal use only
        /// </summary>
        /// <value>
        /// The view index to activate.
        /// </value>
        public int TopViewIndexToActivate
        {
            get { return (int)GetValue(TopViewIndexToActivateProperty); }
            set { SetValue(TopViewIndexToActivateProperty, value); }
        }

        /// <summary>
        /// For internal use only
        /// </summary>
        /// <value>
        /// The view index to activate.
        /// </value>
        public static readonly DependencyProperty TopViewIndexToActivateProperty = DependencyProperty.Register("TopViewIndexToActivate", typeof(int), typeof(Shell), new PropertyMetadata(-1));

        /// <summary>Complete view result for the currently selected top-level view (null if no view is selected)</summary>
        /// <remarks>Can be used to bind the current view into a content control using {Binding SelectedTopLevelViewResult.Document}</remarks>
        public ViewResult SelectedTopLevelViewResult
        {
            get { return (ViewResult)GetValue(SelectedTopLevelViewResultProperty); }
            set { SetValue(SelectedTopLevelViewResultProperty, value); }
        }
        /// <summary>Complete view result for the currently selected top-level view (null if no view is selected)</summary>
        /// <remarks>Can be used to bind the current view into a content control using {Binding SelectedTopLevelViewResult.Document}</remarks>
        public static readonly DependencyProperty SelectedTopLevelViewResultProperty = DependencyProperty.Register("SelectedTopLevelViewResult", typeof(ViewResult), typeof(Shell), new UIPropertyMetadata(null));

        /// <summary>Number of open top level views</summary>
        public int TopLevelViewCount
        {
            get { return (int)GetValue(TopLevelViewCountProperty); }
            set { SetValue(TopLevelViewCountProperty, value); }
        }
        /// <summary>Number of open top level views</summary>
        public static readonly DependencyProperty TopLevelViewCountProperty = DependencyProperty.Register("TopLevelViewCount", typeof(int), typeof(Shell), new UIPropertyMetadata(0));

        /// <summary>Current application/shell status</summary>
        public StatusViewResultWrapper Status
        {
            get { return (StatusViewResultWrapper)GetValue(StatusProperty); }
            set { SetValue(StatusProperty, value); }
        }
        /// <summary>Current application/shell status</summary>
        public static readonly DependencyProperty StatusProperty = DependencyProperty.Register("Status", typeof(StatusViewResultWrapper), typeof(Shell), new UIPropertyMetadata(null));

        /// <summary>Current status view</summary>
        public UIElement CurrentStatusView
        {
            get { return (UIElement)GetValue(CurrentStatusViewProperty); }
            set { SetValue(CurrentStatusViewProperty, value); }
        }
        /// <summary>Current status view</summary>
        public static readonly DependencyProperty CurrentStatusViewProperty = DependencyProperty.Register("CurrentStatusView", typeof(UIElement), typeof(Shell), new UIPropertyMetadata(null));

        /// <summary>Overall application status (based on status message updates)</summary>
        public ApplicationStatus CurrentApplicationStatus
        {
            get { return (ApplicationStatus)GetValue(CurrentApplicationStatusProperty); }
            set { SetValue(CurrentApplicationStatusProperty, value); }
        }
        /// <summary>Overall application status (based on status message updates)</summary>
        public static readonly DependencyProperty CurrentApplicationStatusProperty = DependencyProperty.Register("CurrentApplicationStatus", typeof(ApplicationStatus), typeof(Shell), new UIPropertyMetadata(ApplicationStatus.Ready));

        /// <summary>This event fires whenever a new status is set</summary>
        public static readonly RoutedEvent StatusChangedEvent = EventManager.RegisterRoutedEvent("StatusChanged", RoutingStrategy.Direct, typeof (RoutedEventHandler), typeof (Shell));
        /// <summary>This event fires whenever a new status is set</summary>
        public event RoutedEventHandler StatusChanged
        {
            add { AddHandler(StatusChangedEvent, value); }
            remove { RemoveHandler(StatusChangedEvent, value); }
        }

        /// <summary>This event fires whenever a new notification is set</summary>
        public static readonly RoutedEvent NotificationChangedEvent = EventManager.RegisterRoutedEvent("NotificationChanged", RoutingStrategy.Direct, typeof(RoutedEventHandler), typeof(Shell));
        /// <summary>This event fires whenever a new notification is set</summary>
        public event RoutedEventHandler NotificationChanged
        {
            add { AddHandler(NotificationChangedEvent, value); }
            remove { RemoveHandler(NotificationChangedEvent, value); }
        }

        /// <summary>Defines the maximum number of notifications the Shell displays at any time</summary>
        public int MaximumNotificationCount
        {
            get { return (int)GetValue(MaximumNotificationCountProperty); }
            set { SetValue(MaximumNotificationCountProperty, value); }
        }
        /// <summary>Defines the maximum number of notifications the Shell displays at any time</summary>
        public static readonly DependencyProperty MaximumNotificationCountProperty = DependencyProperty.Register("MaximumNotificationCount", typeof(int), typeof(Shell), new UIPropertyMetadata(5));

        /// <summary>Timeout (time span) notifications are shown</summary>
        public TimeSpan NotificationTimeout
        {
            get { return (TimeSpan)GetValue(NotificationTimeoutProperty); }
            set { SetValue(NotificationTimeoutProperty, value); }
        }
        /// <summary>Timeout (time span) notifications are shown</summary>
        public static readonly DependencyProperty NotificationTimeoutProperty = DependencyProperty.Register("NotificationTimeout", typeof(TimeSpan), typeof(Shell), new UIPropertyMetadata(new TimeSpan(0, 0, 10)));

        /// <summary>List of currently open notifications</summary>
        public ObservableCollection<NotificationViewResultWrapper> CurrentNotifications
        {
            get { return (ObservableCollection<NotificationViewResultWrapper>)GetValue(CurrentNotificationsProperty); }
            set { SetValue(CurrentNotificationsProperty, value); }
        }
        /// <summary>List of currently open notifications</summary>
        public static readonly DependencyProperty CurrentNotificationsProperty = DependencyProperty.Register("CurrentNotifications", typeof(ObservableCollection<NotificationViewResultWrapper>), typeof(Shell), new UIPropertyMetadata(new ObservableCollection<NotificationViewResultWrapper>()));

        /// <summary>Indicates number of currently displayed notifications</summary>
        public int CurrentNotificationsCount
        {
            get { return (int)GetValue(CurrentNotificationsCountProperty); }
            set { SetValue(CurrentNotificationsCountProperty, value); }
        }
        /// <summary>Indicates number of currently displayed notifications</summary>
        public static readonly DependencyProperty CurrentNotificationsCountProperty = DependencyProperty.Register("CurrentNotificationsCount", typeof(int), typeof(Shell), new UIPropertyMetadata(0));

        /// <summary>Defines how notifications are sorted</summary>
        public NotificationSort NotificationSort
        {
            get { return (NotificationSort)GetValue(NotificationSortProperty); }
            set { SetValue(NotificationSortProperty, value); }
        }
        /// <summary>Defines how notifications are sorted</summary>
        public static readonly DependencyProperty NotificationSortProperty = DependencyProperty.Register("NotificationSort", typeof(NotificationSort), typeof(Shell), new PropertyMetadata(NotificationSort.NewestFirst));

        /// <summary>Defines what should show in the title bar of the shell window</summary>
        public ShellTitleMode TitleMode
        {
            get { return (ShellTitleMode)GetValue(TitleModeProperty); }
            set { SetValue(TitleModeProperty, value); }
        }
        /// <summary>Defines what should show in the title bar of the shell window</summary>
        public static readonly DependencyProperty TitleModeProperty = DependencyProperty.Register("TitleMode", typeof(ShellTitleMode), typeof(Shell), new PropertyMetadata(ShellTitleMode.OriginalTitleOnly));

        /// <summary>If set to true, the current shell can handle views with a local scope special by adding it to the TopLevelViewsLocal collection</summary>
        public bool HandleLocalViewsSpecial
        {
            get { return (bool)GetValue(HandleLocalViewsSpecialProperty); }
            set { SetValue(HandleLocalViewsSpecialProperty, value); }
        }
        /// <summary>If set to true, the current shell can handle views with a local scope special by adding it to the TopLevelViewsLocal collection</summary>
        public static readonly DependencyProperty HandleLocalViewsSpecialProperty = DependencyProperty.Register("HandleLocalViewsSpecial", typeof(bool), typeof(Shell), new PropertyMetadata(false));

        /// <summary>Handles opening of views that are status messages</summary>
        /// <param name="context">The request context.</param>
        /// <returns>True of view was handled</returns>
        protected virtual bool HandleStatusMessage(RequestContext context)
        {
            var statusResult = context.Result as StatusMessageResult;
            if (statusResult == null) return false;

            if (Status == null) Status = new StatusViewResultWrapper();

            Status.Model = statusResult.Model;

            if (statusResult.View != null)
            {
                Status.View = statusResult.View;
                var element = Status.View as FrameworkElement;
                if (element != null)
                    element.DataContext = Status.Model;
            }
            else
            {
                var grid = new Grid();
                var text = new TextBlock();
                grid.Children.Add(text);
                var binding = new Binding("Message");
                text.SetBinding(TextBlock.TextProperty, binding);
                grid.DataContext = Status.Model;
                Status.View = grid;
            }

            CurrentStatusView = Status.View;
            CurrentApplicationStatus = Status.Model.Status;
            RaiseEvent(new RoutedEventArgs(StatusChangedEvent));

            return true;
        }

        /// <summary>Handles opening of views that are notification messages</summary>
        /// <param name="context">The request context.</param>
        /// <param name="overrideTimeout">Overrides the theme's default notification timeout.</param>
        /// <returns>True of view was handled</returns>
        protected virtual bool HandleNotificationMessage(RequestContext context, TimeSpan? overrideTimeout = null)
        {
            var notificationResult = context.Result as NotificationMessageResult;
            if (notificationResult == null) return false;

            var wrapper = new NotificationViewResultWrapper {Model = notificationResult.Model};

            if (notificationResult.View != null)
            {
                wrapper.View = notificationResult.View;
                notificationResult.View.DataContext = wrapper.Model;
            }
            else
            {
                wrapper.View = Controller.LoadView(StandardViews.Notification);
                if (wrapper.View != null)
                    wrapper.View.DataContext = wrapper.Model;
            }

            if (NotificationSort == NotificationSort.NewestFirst)
                CurrentNotifications.Add(wrapper);
            else
                CurrentNotifications.Insert(0, wrapper);

            while (CurrentNotifications.Count > MaximumNotificationCount)
            {
                // Handling this like a stack, popping the oldest one off
                if (NotificationSort == NotificationSort.NewestFirst)
                    CurrentNotifications.RemoveAt(0);
                else
                    CurrentNotifications.RemoveAt(CurrentNotifications.Count - 1);
            }
            CurrentNotificationsCount = CurrentNotifications.Count;

            RaiseEvent(new RoutedEventArgs(NotificationChangedEvent));

            var timeout = overrideTimeout ?? NotificationTimeout;
            wrapper.InternalTimer = new Timer(state => Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    CurrentNotifications.Remove(wrapper);
                    CurrentNotificationsCount = CurrentNotifications.Count;
                })), null, timeout, new TimeSpan(-1));

            return true;
        }

        /// <summary>
        /// Clears all notifications
        /// </summary>
        public void ClearNotifications()
        {
            CurrentNotifications.Clear();
            CurrentNotificationsCount = CurrentNotifications.Count;
            RaiseEvent(new RoutedEventArgs(NotificationChangedEvent));
        }

        /// <summary>
        /// This method is invoked when a view is opened
        /// </summary>
        /// <param name="context">Request context (contains information about the view)</param>
        /// <returns>
        /// True if handled successfully
        /// </returns>
        public bool OpenView(RequestContext context)
        {
            if (context.Result is StatusMessageResult) return HandleStatusMessage(context);
            if (context.Result is NotificationMessageResult)
            {
                var result = context.Result as NotificationMessageResult;
                return HandleNotificationMessage(context, result.Model.OverrideTimeout);
            }

            // This is a special model that activates views that are already open
            var existingViewResult = context.Result as ExistingViewResult;
            if (existingViewResult != null)
                return ActivateViewForModel(existingViewResult.Model);

            var viewResult = context.Result as ViewResult;
            if (viewResult != null && viewResult.ForceNewShell)
                if ((viewResult.ViewLevel == ViewLevel.Normal && NormalViewCount > 0) || (viewResult.ViewLevel == ViewLevel.TopLevel && TopLevelViewCount > 0))
                {
                    // A new shell should be opened, so we try to create another shell and then hand off view opening duties to it, rather than handling it directly
                    var shellLauncher = Controller.GetShellLauncher() as WindowShellLauncher<Shell>;
                    if (shellLauncher != null)
                        if (shellLauncher.OpenAnotherShellInstance())
                            return Current.OpenView(context);
                }

            var messageBoxResult = context.Result as MessageBoxResult;
            if (messageBoxResult != null)
            {
                if (messageBoxResult.View == null)
                {
                    var messageBoxViewModel = messageBoxResult.Model as MessageBoxViewModel;
                    if (messageBoxViewModel != null)
                    {
                        var textBlock = new TextBlock {TextWrapping = TextWrapping.Wrap, Text = messageBoxViewModel.Text};
                        textBlock.SetResourceReference(StyleProperty, "CODE.Framework.Wpf.Mvvm.Shell-MessageBox-Text");

                        if (!string.IsNullOrEmpty(messageBoxViewModel.IconResourceKey))
                        {
                            var grid = new Grid();
                            grid.ColumnDefinitions.Clear();
                            grid.ColumnDefinitions.Add(new ColumnDefinition {Width = GridLength.Auto});
                            grid.ColumnDefinitions.Add(new ColumnDefinition {Width = new GridLength(1, GridUnitType.Star)});
                            var rect = new Rectangle {Fill = messageBoxViewModel.IconBrush};
                            rect.SetResourceReference(StyleProperty, "CODE.Framework.Wpf.Mvvm.Shell-MessageBox-Image");
                            grid.Children.Add(rect);
                            grid.Children.Add(textBlock);
                            Grid.SetColumn(textBlock, 1);
                            SimpleView.SetSizeStrategy(grid, ViewSizeStrategies.UseMinimumSizeRequired);
                            messageBoxResult.View = grid;
                        }
                        else messageBoxResult.View = textBlock;
                    }
                }

                if (messageBoxResult.View != null)
                {
                    var messageBoxModelAction = messageBoxResult.Model as IHaveActions;
                    if (messageBoxModelAction != null)
                    {
                        messageBoxResult.View.InputBindings.Clear();
                        foreach (var action in messageBoxModelAction.Actions)
                            if (action.ShortcutKey != Key.None)
                                messageBoxResult.View.InputBindings.Add(new KeyBinding(action, action.ShortcutKey, action.ShortcutModifiers));
                    }
                }
            }

            if (viewResult != null && viewResult.View != null && !viewResult.IsPartial)
            {
                if (viewResult.ViewLevel == ViewLevel.Popup || viewResult.ViewLevel == ViewLevel.StandAlone || viewResult.ViewLevel == ViewLevel.TopLevel)
                    return OpenTopLevelView(context, messageBoxResult, viewResult);

                // This is an normal (main) view

                // Need to make sure we do not open more than allowed
                if (MaximumNormalViewCount > -1)
                {
                    var inplaceNormalViews = NormalViews.Where(v => v.TopLevelWindow == null).ToList();
                    while (inplaceNormalViews.Count + 1 > MaximumNormalViewCount)
                    {
                        CloseViewForModel(inplaceNormalViews[0].Model);
                        inplaceNormalViews.RemoveAt(0);
                    }
                }

                NormalViews.Add(viewResult);
                if (viewResult.MakeViewVisibleOnLaunch)
                {
                    FocusManager.SetFocusedElement(this, viewResult.View);
                    SelectedNormalView = NormalViews.Count - 1;
                    SelectedNormalViewResult = SelectedNormalView > -1 ? NormalViews[SelectedNormalView] : null;
                }
                NormalViewCount = NormalViews.Count;

                if (NormalViewLaunchMode == ViewLaunchMode.Popup)
                    OpenNormalViewInWindow(context, viewResult);

                return true;
            }

            return false;
        }

        /// <summary>
        /// Opens a normal view in a separate window.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="viewResult">The view result.</param>
        private static void OpenNormalViewInWindow(RequestContext context, ViewResult viewResult)
        {
            var window = new Window
                {
                    Title = viewResult.ViewTitle,
                    SizeToContent = SizeToContent.WidthAndHeight,
                    Content = viewResult.View,
                    DataContext = viewResult.Model,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen
                };

            window.SetBinding(TitleProperty, new Binding("ViewTitle") {Source = viewResult});

            var simpleView = viewResult.View as SimpleView;
            if (simpleView != null)
                if (SimpleView.GetSizeStrategy(simpleView) == ViewSizeStrategies.UseMaximumSizeAvailable)
                    window.SizeToContent = SizeToContent.Manual;
            
            viewResult.TopLevelWindow = window;
            if (context.Result is MessageBoxResult) window.SetResourceReference(StyleProperty, "CODE.Framework.Wpf.Mvvm.Shell-TopLevelMessageBoxWindowStyle");
            else window.SetResourceReference(StyleProperty, "CODE.Framework.Wpf.Mvvm.Shell-NormalLevelWindowStyle");
            if (viewResult.IsModal) window.ShowDialog();
            else window.Show();
        }

        /// <summary>
        /// Opens the top level view in a separate window.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="messageBoxResult">The message box result.</param>
        /// <param name="viewResult">The view result.</param>
        /// <returns>True if successfully opened</returns>
        private bool OpenTopLevelView(RequestContext context, MessageBoxResult messageBoxResult, ViewResult viewResult)
        {
            if (messageBoxResult != null && string.IsNullOrEmpty(viewResult.ViewIconResourceKey))
                viewResult.ViewIconResourceKey = messageBoxResult.ModelMessageBox.IconResourceKey;

            // If we respect local views and the view is in fact a local view, and we have a normal view already open, then we open it in a local way only.
            if (viewResult.ViewScope == ViewScope.Local && HandleLocalViewsSpecial && SelectedNormalView > -1)
            {
                var selectedView = NormalViews[SelectedNormalView];
                if (selectedView == null) return false;
                selectedView.LocalViews.Add(viewResult);
                if (viewResult.MakeViewVisibleOnLaunch)
                    selectedView.SelectedLocalViewIndex = selectedView.LocalViews.Count - 1;
                return true;
            }

            //Need to make sure we do not open more than allowed - Popups should not close underlying views.
            if (viewResult.ViewLevel != ViewLevel.Popup && MaximumTopLevelViewCount > -1)
            {
                var inplaceTopLevelviews = TopLevelViews.Where(v => v.TopLevelWindow == null).ToList();
                while (inplaceTopLevelviews.Count + 1 > MaximumTopLevelViewCount)
                {
                    CloseViewForModel(inplaceTopLevelviews[0].Model);
                    inplaceTopLevelviews.RemoveAt(0);
                }
            }

            TopLevelViews.Add(viewResult);

            if (viewResult.MakeViewVisibleOnLaunch && !(TopLevelViewLaunchMode == ViewLaunchMode.Popup || (TopLevelViewLaunchMode == ViewLaunchMode.InPlaceExceptPopups && viewResult.ViewLevel == ViewLevel.Popup)))
            {
                SelectedTopLevelView = TopLevelViews.Count - 1;
                SelectedTopLevelViewResult = SelectedTopLevelView > -1 ? TopLevelViews[SelectedTopLevelView] : null;
                if (viewResult.View != null)
                    if (!FocusHelper.FocusFirstControlDelayed(viewResult.View))
                        FocusHelper.FocusDelayed(viewResult.View);
            }
            TopLevelViewCount = TopLevelViews.Count;

            if (TopLevelViewLaunchMode == ViewLaunchMode.Popup || (TopLevelViewLaunchMode == ViewLaunchMode.InPlaceExceptPopups && viewResult.ViewLevel == ViewLevel.Popup))
            {
                var window = new Window
                    {
                        Title = viewResult.ViewTitle,
                        //Content = viewResult.View,
                        DataContext = viewResult.Model
                    };

                if (viewResult.IsModal) window.Owner = this;

                window.SetBinding(ContentProperty, new Binding("View") { Source = viewResult });
                window.SetBinding(TitleProperty, new Binding("ViewTitle") { Source = viewResult });

                // Setting the size strategy
                var strategy = SimpleView.GetSizeStrategy(viewResult.View);
                switch (strategy)
                {
                    case ViewSizeStrategies.UseMinimumSizeRequired:
                        window.SizeToContent = SizeToContent.WidthAndHeight;
                        break;
                    case ViewSizeStrategies.UseMaximumSizeAvailable:
                        window.SizeToContent = SizeToContent.Manual;
                        window.Height = SystemParameters.WorkArea.Height;
                        window.Width = SystemParameters.WorkArea.Width;
                        break;
                    case ViewSizeStrategies.UseSuggestedSize:
                        window.SizeToContent = SizeToContent.Manual;
                        window.Height = SimpleView.GetSuggestedHeight(viewResult.View);
                        window.Width = SimpleView.GetSuggestedWidth(viewResult.View);
                        break;
                }

                viewResult.TopLevelWindow = window;

                if (context.Result is MessageBoxResult) window.SetResourceReference(StyleProperty, "CODE.Framework.Wpf.Mvvm.Shell-TopLevelMessageBoxWindowStyle");
                else
                {
                    var styleKey = "CODE.Framework.Wpf.Mvvm.Shell-TopLevelWindowStyle";
                    var handler = GetTopLevelWindowStyleName;
                    if (handler != null)
                    {
                        var getTopLevelWindowStyleNameEventArgs = new GetTopLevelWindowStyleNameEventArgs { ViewResult = viewResult, RequestContext = context, StyleKey = styleKey, Window = window };
                        handler(this, getTopLevelWindowStyleNameEventArgs);
                        styleKey = getTopLevelWindowStyleNameEventArgs.StyleKey;
                    }
                    if (string.IsNullOrEmpty(styleKey))
                        styleKey = "CODE.Framework.Wpf.Mvvm.Shell-TopLevelWindowStyle";
                    window.SetResourceReference(StyleProperty, styleKey);
                }

                if (viewResult.View != null)
                    foreach (InputBinding binding in viewResult.View.InputBindings)
                        window.InputBindings.Add(binding);

                if (!FocusHelper.FocusFirstControlDelayed(window))
                    FocusHelper.FocusDelayed(window);

                if (viewResult.IsModal) window.ShowDialog();
                else window.Show();
            }


            //if (iconBrush != null)
            //{
            //    try
            //    {
            //        // TODO: Implement the icon logic
            //        //var iconRect = new Canvas {Height = 96, Width = 96, Background = iconBrush};
            //        //window.Icon = iconRect.ToIconSource();
            //    }
            //    catch
            //    {
            //    }
            //}

            return true;
        }

        /// <summary>
        /// This event can be used to influence the name of the style used for top level windows
        /// </summary>
        public static event EventHandler<GetTopLevelWindowStyleNameEventArgs> GetTopLevelWindowStyleName;

        /// <summary>
        /// This method is invoked when a view that is associated with a certain model should be closed
        /// </summary>
        /// <param name="model">The model associated with the view that is to be closed</param>
        /// <returns>True, if the view was found and successfully closed</returns>
        public bool CloseViewForModel(object model)
        {
            // We check whether any of the models want to cancel the closing operation
            foreach (var view in TopLevelViews)
                if (view.Model != null && view.Model == model)
                    if (view.RaiseBeforeViewClosed()) // returns true if closing is canceled
                        return false;

            foreach (var view in NormalViews)
            {
                if (view.Model != null && view.Model == model)
                    if (view.RaiseBeforeViewClosed()) // returns true if closing is canceled
                        return false;
                foreach (var localView in view.LocalViews)
                    if (localView.Model != null && localView.Model == model)
                        if (view.RaiseBeforeViewClosed()) // returns true if closing is canceled
                            return false;
            }

            // Arrived at the regular close operation (and events)
            foreach (var view in TopLevelViews)
                if (view.Model != null && view.Model == model)
                {
                    TopLevelViews.Remove(view);
                    SelectedTopLevelView = TopLevelViews.Count - 1;
                    SelectedTopLevelViewResult = SelectedTopLevelView > -1 ? TopLevelViews[SelectedTopLevelView] : null;
                    TopLevelViewCount = TopLevelViews.Count;
                    if (view.TopLevelWindow != null && !IsWindowClosing(view.TopLevelWindow))
                        view.TopLevelWindow.Close();
                    view.RaiseViewClosed();
                    return true;
                }

            foreach (var view in NormalViews)
            {
                if (view.Model != null && view.Model == model)
                {
                    NormalViews.Remove(view);
                    SelectedNormalView = NormalViews.Count - 1;
                    SelectedNormalViewResult = SelectedNormalView > -1 ? NormalViews[SelectedNormalView] : null;
                    NormalViewCount = NormalViews.Count;
                    if (view.TopLevelWindow != null && !IsWindowClosing(view.TopLevelWindow))
                        view.TopLevelWindow.Close();
                    view.RaiseViewClosed();
                    return true;
                }
                foreach (var localView in view.LocalViews)
                    if (localView.Model != null && localView.Model == model)
                    {
                        view.LocalViews.Remove(localView);
                        if (view.SelectedLocalViewIndex >= view.LocalViews.Count)
                            view.SelectedLocalViewIndex = view.LocalViews.Count - 1;
                        localView.RaiseViewClosed();
                        return true;
                    }
            }

            return false;
        }

        /// <summary>
        /// This method is invoked when a view that is associated with a certain model should be activated/shown
        /// </summary>
        /// <param name="model">Model</param>
        /// <returns>
        /// True if successful
        /// </returns>
        public bool ActivateViewForModel(object model)
        {
            // We check all top level views
            var topLevelViewCounter = -1;
            foreach (var view in TopLevelViews)
            {
                topLevelViewCounter++;
                if (view.Model != null && view.Model == model)
                {
                    TopViewIndexToActivate = -1;
                    TopViewIndexToActivate = topLevelViewCounter;
                    SelectedTopLevelView = topLevelViewCounter;
                    return true;
                }
            }

            // We check all normal views
            var normalViewCounter = -1;
            foreach (var view in NormalViews)
            {
                normalViewCounter++;
                if (view.Model != null && view.Model == model)
                {
                    ViewIndexToActivate = -1; // Making sure the event triggers
                    ViewIndexToActivate = normalViewCounter;
                    SelectedNormalView = normalViewCounter;
                    return true;
                }
            }

            return false;
        }


        /// <summary>
        /// Returns true, if a model instance of the specified type and selector criteria is already open
        /// </summary>
        /// <typeparam name="TModel">The type of the model.</typeparam>
        /// <param name="selector">Selector used to pick an appropriate model instance</param>
        /// <returns>
        /// A reference to the open model instance
        /// </returns>
        public TModel GetOpenModel<TModel>(Func<TModel, bool> selector) where TModel : class
        {
            // We check all top level views
            foreach (var view in TopLevelViews)
                if (view.Model != null && view.Model.GetType() == typeof(TModel))
                {
                    var typedModel = view.Model as TModel;
                    if (typedModel == null) continue;
                    if (selector(typedModel)) return typedModel;
                }

            // We check all normal views
            foreach (var view in NormalViews)
                if (view.Model != null && view.Model.GetType() == typeof(TModel))
                {
                    var typedModel = view.Model as TModel;
                    if (typedModel == null) continue;
                    if (selector(typedModel)) return typedModel;
                }

            return default(TModel);
        }

        /// <summary>
        /// Returns true, if a model instance of the specified type is already open
        /// </summary>
        /// <typeparam name="TModel">The type of the model.</typeparam>
        /// <returns>
        /// A reference to the open model instance
        /// </returns>
        public TModel GetOpenModel<TModel>() where TModel : class
        {
            // We check all top level views
            foreach (var view in TopLevelViews)
                if (view.Model != null && view.Model.GetType() == typeof(TModel))
                    return view.Model as TModel;

            // We check all normal views
            foreach (var view in NormalViews)
                if (view.Model != null && view.Model.GetType() == typeof(TModel))
                    return view.Model as TModel;

            return default(TModel);
        }

        private static bool IsWindowClosing(Window window)
        {
            if (window == null) return false;
            var field = typeof (Window).GetField("_isClosing", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null) return false;
            var isClosing = (bool) field.GetValue(window);
            return isClosing;
        }

        /// <summary>
        /// This method closes all currently open views
        /// </summary>
        /// <returns>True if the handler successfully closed all views. False if it didn't close all views or generally does not handle view closing</returns>
        public bool CloseAllViews()
        {
            var topLevelViews = TopLevelViews.ToArray();
            foreach (var view in topLevelViews)
            {
                TopLevelViews.Remove(view);
                if (view.TopLevelWindow != null) view.TopLevelWindow.Close();
                view.RaiseViewClosed();
            }
            SelectedTopLevelView = -1;
            SelectedTopLevelViewResult = null;
            TopLevelViewCount = 0;

            var normalViews = NormalViews.ToArray();
            foreach (var view in normalViews)
            {
                NormalViews.Remove(view);
                if (view.TopLevelWindow != null) view.TopLevelWindow.Close();
                view.RaiseViewClosed();
            }
            SelectedNormalView = -1;
            SelectedNormalViewResult = null;
            NormalViewCount = 0;

            return true;
        }

        /// <summary>
        /// This method is used to retrieve a view associated with the specified model
        /// </summary>
        /// <param name="model">Model</param>
        /// <returns>
        /// Document if found (null otherwise)
        /// </returns>
        public object GetViewForModel(object model)
        {
            foreach (var view in TopLevelViews)
                if (view.Model != null && view.Model == model)
                    return view.View;

            foreach (var view in NormalViews)
            {
                if (view.Model != null && view.Model == model)
                    return view.View;
                foreach (var localView in view.LocalViews)
                    if (localView.Model != null && localView.Model == model)
                        return localView;
            }

            return null;
        }

        private string _originalTitle = string.Empty;

        /// <summary>
        /// Sets the original version of the title (which can be used later to reset the title)
        /// </summary>
        /// <param name="originalTitle">The original title.</param>
        public void SetOriginalTitle(string originalTitle)
        {
            _originalTitle = originalTitle;
        }

        /// <summary>
        /// Raises the <see cref="E:System.Windows.Window.Closing" /> event.
        /// </summary>
        /// <param name="e">A <see cref="T:System.ComponentModel.CancelEventArgs" /> that contains the event data.</param>
        protected override void OnClosing(CancelEventArgs e)
        {
            CloseAllViews();
            base.OnClosing(e);
        }
    }

    /// <summary>
    /// Defines how notifications are to be sorted
    /// </summary>
    public enum NotificationSort
    {
        /// <summary>
        /// Newest notification first (default)
        /// </summary>
        NewestFirst,
        /// <summary>
        /// Oldest notification first
        /// </summary>
        OldestFirst
    }

    /// <summary>
    /// Different view launch modes
    /// </summary>
    /// <remarks>Note that each theme is free to interpret these settings differently.</remarks>
    public enum ViewLaunchMode
    {
        /// <summary>
        /// Activates a view in-place (typically in a tab control)
        /// </summary>
        InPlace,
        /// <summary>
        /// Activates a view as a top level view (typically a new window)
        /// </summary>
        Popup,
        /// <summary>
        /// Activates a view in-place except when they are specifically flagged as pop ups
        /// </summary>
        InPlaceExceptPopups,
        /// <summary>
        /// Activates a view in-place in the main shell, but will create a new shell window for each view
        /// </summary>
        /// <remarks>Typically only applies for normal views</remarks>
        InPlaceStandAlone
    }

    /// <summary>Wraper used to encapsulate the status view model</summary>
    public class StatusViewResultWrapper : INotifyPropertyChanged
    {
        /// <summary>
        /// Status view model
        /// </summary>
        public StatusViewModel Model
        {
            get { return _model; }
            set
            {
                _model = value;
                NotifyChanged("Model");
            }
        }
        private StatusViewModel _model;

        /// <summary>Document</summary>
        public UIElement View
        {
            get { return _view; }
            set
            {
                _view = value;
                NotifyChanged("Document");
            }
        }
        private UIElement _view;

        private void NotifyChanged(string property = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(property));
        }
        /// <summary>Occurs when a property value changes.</summary>
        public event PropertyChangedEventHandler PropertyChanged;
    }

    /// <summary>Wraper used to encapsulate the notification view model</summary>
    public class NotificationViewResultWrapper : INotifyPropertyChanged
    {
        /// <summary>
        /// Notification view model
        /// </summary>
        public NotificationViewModel Model
        {
            get { return _model; }
            set
            {
                _model = value;
                NotifyChanged("Model");
            }
        }
        private NotificationViewModel _model;

        /// <summary>Document</summary>
        public FrameworkElement View
        {
            get { return _view; }
            set
            {
                _view = value;
                NotifyChanged("Document");
            }
        }
        private FrameworkElement _view;

        private void NotifyChanged(string property = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(property));
        }
        /// <summary>Occurs when a property value changes.</summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// For internal use only
        /// </summary>
        internal Timer InternalTimer { get; set; }
    }

    /// <summary>
    /// Defines the title mode for the shell window
    /// </summary>
    public enum ShellTitleMode
    {
        /// <summary>
        /// Shows the original title of the shell only (such as "Word")
        /// </summary>
        OriginalTitleOnly,
        /// <summary>
        /// Shows the current view title plus the original title (such as "ReadMe.doc - Word")
        /// </summary>
        ViewTitleDashOriginalTitle,
        /// <summary>
        /// Shows the original title plus the current view title (such as "Word - ReadMe.doc")
        /// </summary>
        OriginalTitleDashViewTitle,
        /// <summary>
        /// Shows only the title of the current view (except when there is no open view, then the original title is shown)
        /// </summary>
        ViewTitleOnly
    }

    /// <summary>
    /// Event args for GetTopLevelWindowStyleName event
    /// </summary>
    /// <seealso cref="System.EventArgs" />
    public class GetTopLevelWindowStyleNameEventArgs : EventArgs
    {
        /// <summary>
        /// The window the style will be attached to
        /// </summary>
        /// <value>The window.</value>
        public Window Window { get; set; }
        /// <summary>
        /// The view result
        /// </summary>
        /// <value>The view result.</value>
        public ViewResult ViewResult { get; set; }
        /// <summary>
        /// Key of the style that will be applied to the window. Set the property to load a different style
        /// </summary>
        /// <value>The style key.</value>
        public string StyleKey { get; set; }
        /// <summary>
        /// Request context
        /// </summary>
        /// <value>The request context.</value>
        public RequestContext RequestContext { get; set; }
    }
}

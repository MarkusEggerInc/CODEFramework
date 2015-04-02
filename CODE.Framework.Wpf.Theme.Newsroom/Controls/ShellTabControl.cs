using System;
using System.ComponentModel;
using System.Timers;
using System.Windows;
using System.Windows.Controls;

namespace CODE.Framework.Wpf.Theme.Newsroom.Controls
{
    /// <summary>
    /// Tab Control used specifically as a view host in a Metro Shell
    /// </summary>
    public class ShellTabControl : TabControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ShellTabControl"/> class.
        /// </summary>
        public ShellTabControl()
        {
            var descriptor = DependencyPropertyDescriptor.FromProperty(SelectedIndexProperty, typeof (TabControl));
            if (descriptor != null)
                descriptor.AddValueChanged(this, (s, e) =>
                {
                    var mustRaisePageSwitchEvent = !HomePageVisible;
                    if (SelectedIndex > -1 && HomePageVisible)
                    {
                        mustRaisePageSwitchEvent = false;
                        HomePageVisible = false;
                    }
                    if (SelectedIndex == -1 && Items.Count == 0 && !HomePageVisible)
                    {
                        mustRaisePageSwitchEvent = false;
                        HomePageVisible = true;
                    }

                    if (mustRaisePageSwitchEvent)
                    {
                        try
                        {
                            RaiseEvent(new RoutedEventArgs(PageSwitchedEvent, this));
                        }
                        catch
                        {
                        }
                    }
                });
        }

        /// <summary>This event fires whenever the user switches to a different view (but not to and from the start page)</summary>
        public static readonly RoutedEvent PageSwitchedEvent = EventManager.RegisterRoutedEvent("PageSwitched", RoutingStrategy.Bubble, typeof (RoutedEventHandler), typeof (ShellTabControl));

        /// <summary>This event fires whenever the user switches to a different view (but not to and from the start page)</summary>
        public event RoutedEventHandler PageSwitched
        {
            add { AddHandler(PageSwitchedEvent, value); }
            remove { RemoveHandler(PageSwitchedEvent, value); }
        }

        /// <summary>
        /// Called when <see cref="M:System.Windows.FrameworkElement.ApplyTemplate"/> is called.
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (Template == null) return;

            var headers = Template.FindName("HeaderPanel", this) as UIElement;
            if (headers != null)
                headers.MouseLeftButtonUp += (s, e) =>
                {
                    if (HomePageVisible)
                    {
                        HomePageVisible = false;
                        if (SelectedIndex < 0 && _lastSelectedPage > -1)
                            SelectedIndex = _lastSelectedPage;
                    }
                };

            var home = Template.FindName("HomePanel", this) as UIElement;
            if (home != null)
                home.MouseLeftButtonUp += (s, e) => MakeHomePageVisible();
        }

        /// <summary>
        /// Makes the home page visible.
        /// </summary>
        public void MakeHomePageVisible()
        {
            if (!HomePageVisible) HomePageVisible = true;
            _lastSelectedPage = SelectedIndex;
            var timer = new Timer(300);
            timer.Elapsed += ((s2, e2) =>
            {
                timer.Stop();
                Dispatcher.BeginInvoke(new Action(HidePages), null);
            });
            timer.Start();
        }

        private void HidePages()
        {
            SelectedIndex = -1;
        }

        private int _lastSelectedPage = -1;

        /// <summary>
        /// Title for the home item in the tabs
        /// </summary>
        /// <value>The home title.</value>
        public string HomeTitle
        {
            get { return (string) GetValue(HomeTitleProperty); }
            set { SetValue(HomeTitleProperty, value); }
        }

        /// <summary>
        /// Title for the home item in the tabs
        /// </summary>
        public static readonly DependencyProperty HomeTitleProperty = DependencyProperty.Register("HomeTitle", typeof (string), typeof (ShellTabControl), new UIPropertyMetadata("Home"));

        /// <summary>
        /// Defines whether the homepage is the currently visible element
        /// </summary>
        /// <value>
        ///   <c>true</c> if home page visible; otherwise, <c>false</c>.
        /// </value>
        public bool HomePageVisible
        {
            get { return (bool) GetValue(HomePageVisibleProperty); }
            set { SetValue(HomePageVisibleProperty, value); }
        }

        /// <summary>
        /// Defines whether the homepage is the currently visible element
        /// </summary>
        public static readonly DependencyProperty HomePageVisibleProperty = DependencyProperty.Register("HomePageVisible", typeof (bool), typeof (ShellTabControl), new UIPropertyMetadata(false));

        /// <summary>
        /// Visual for the homepage
        /// </summary>
        /// <value>
        /// The home page.
        /// </value>
        public UIElement HomePage
        {
            get { return (UIElement) GetValue(HomePageProperty); }
            set { SetValue(HomePageProperty, value); }
        }

        /// <summary>
        /// Visual for the homepage
        /// </summary>
        public static readonly DependencyProperty HomePageProperty = DependencyProperty.Register("HomePage", typeof (UIElement), typeof (ShellTabControl), new UIPropertyMetadata(null));
    }
}
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace CODE.Framework.Wpf.Mvvm
{
    /// <summary>
    /// Abstract toolbar class designed for automatic binding to view actions
    /// </summary>
    public class ViewActionToolbar : WrapPanel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ViewActionToolbar"/> class.
        /// </summary>
        public ViewActionToolbar()
        {
            Visibility = Visibility.Collapsed;
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
        public static readonly DependencyProperty ModelProperty = DependencyProperty.Register("Model", typeof(object), typeof(ViewActionToolbar), new UIPropertyMetadata(null, ModelChanged));
        /// <summary>
        /// Change handler for model property
        /// </summary>
        /// <param name="d">The dependency object that triggered this change.</param>
        /// <param name="e">The <see cref="System.Windows.DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        static void ModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var toolbar = d as ViewActionToolbar;
            if (toolbar == null) return;

            var actionsContainer = e.NewValue as IHaveActions;
            if (actionsContainer != null && actionsContainer.Actions != null)
            {
                actionsContainer.Actions.CollectionChanged += (s, e2) => toolbar.PopulateToolbar(actionsContainer);
                toolbar.Visibility = Visibility.Visible;
                toolbar.PopulateToolbar(actionsContainer);
            }
            else
                toolbar.Visibility = Visibility.Collapsed;
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
        public static readonly DependencyProperty SelectedViewProperty = DependencyProperty.Register("SelectedView", typeof(object), typeof(ViewActionToolbar), new UIPropertyMetadata(null, SelectedViewChanged));
        /// <summary>
        /// Change handler for selected view property
        /// </summary>
        /// <param name="d">The dependency object that triggered this change.</param>
        /// <param name="e">The <see cref="System.Windows.DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        static void SelectedViewChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d == null) return;
            var toolbar = d as ViewActionToolbar; 
            if (toolbar == null) return;

            var viewResult = e.NewValue as ViewResult;
            if (viewResult == null)
            {
                toolbar.PopulateToolbar(toolbar.Model as IHaveActions);
                return;
            }

            var actionsContainer = viewResult.Model as IHaveActions;
            if (actionsContainer != null)
            {
                actionsContainer.Actions.CollectionChanged += (s, e2) => toolbar.PopulateToolbar(toolbar.Model as IHaveActions, actionsContainer);
                toolbar.PopulateToolbar(toolbar.Model as IHaveActions, actionsContainer);
            }
            else
                toolbar.PopulateToolbar(toolbar.Model as IHaveActions);
        }

        /// <summary>
        /// Defines which root view actions should be displayed
        /// </summary>
        /// <value>The view action display mode</value>
        public ViewActionDisplayMode RootViewActionDisplayMode
        {
            get { return (ViewActionDisplayMode)GetValue(RootViewActionDisplayModeProperty); }
            set { SetValue(RootViewActionDisplayModeProperty, value); }
        }
        /// <summary>
        /// Defines which root view actions should be displayed
        /// </summary>
        /// <value>The view action display mode</value>
        public static readonly DependencyProperty RootViewActionDisplayModeProperty = DependencyProperty.Register("RootViewActionDisplayMode", typeof(ViewActionDisplayMode), typeof(ViewActionToolbar), new PropertyMetadata(ViewActionDisplayMode.HighestSignificance));

        /// <summary>
        /// Defines which local view view actions should be displayed
        /// </summary>
        /// <value>The view action display mode</value>
        public ViewActionDisplayMode LocalViewActionDisplayMode
        {
            get { return (ViewActionDisplayMode)GetValue(LocalViewActionDisplayModeProperty); }
            set { SetValue(LocalViewActionDisplayModeProperty, value); }
        }
        /// <summary>
        /// Defines which local view view actions should be displayed
        /// </summary>
        /// <value>The view action display mode</value>
        public static readonly DependencyProperty LocalViewActionDisplayModeProperty = DependencyProperty.Register("LocalViewActionDisplayMode", typeof(ViewActionDisplayMode), typeof(ViewActionToolbar), new PropertyMetadata(ViewActionDisplayMode.BelowNormalSignificanceAndHigher));

        /// <summary>
        /// Populates the current menu with items based on the actions collection
        /// </summary>
        /// <param name="actions">List of primary actions</param>
        /// <param name="actions2">List of view specific actions</param>
        protected virtual void PopulateToolbar(IHaveActions actions, IHaveActions actions2 = null)
        {
            Children.Clear();
            if (actions == null) return;

            var actionList = ViewActionHelper.GetConsolidatedActions(actions, actions2, 
                actionsDisplayFilter: RootViewActionDisplayMode, 
                actions2DisplayFilter: LocalViewActionDisplayMode,
                flagFirstSecondaryActionAsNewGroup: true);

            var actionCounter = 0;
            foreach (var action in actionList)
            {
                if (!IncludeAction(action)) continue;
                if (actionCounter > 0 && action.BeginGroup) Children.Add(new ViewActionToolbarSeparator());
                Children.Add(new ViewActionToolbarButton(action));
                actionCounter++;
            }

            Visibility = Children.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// This method is designed to be overridden in subclasses. It can be use to indicate 
        /// whether a certain action should be included in the display, or not.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <returns><c>true</c> if the action is to be included, <c>false</c> otherwise.</returns>
        protected virtual bool IncludeAction(IViewAction action)
        {
            return true;
        }
    }

    /// <summary>
    /// Button class used for view action toolbars
    /// </summary>
    public class ViewActionToolbarButton : Button
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ViewActionToolbarButton" /> class.
        /// </summary>
        /// <param name="action">The action associated with this toolbar button.</param>
        public ViewActionToolbarButton(IViewAction action)
        {
            Action = action;

            var viewAction = Action as ViewAction;
            if (viewAction != null)
            {
                ToolTip = viewAction.ToolTipText;
                HasIcon = viewAction.HasBrush;
                if (!HasIcon) Title = action.Caption;
                else

                {
                    switch (TitleDisplayFilter)
                    {
                        case ViewActionDisplayMode.All:
                            SetBinding(TitleProperty, new Binding("Caption") {Source = action});
                            break;
                        case ViewActionDisplayMode.AboveNormalSignificanceAndHigher:
                            if (action.Significance == ViewActionSignificance.AboveNormal || action.Significance == ViewActionSignificance.Highest)
                                SetBinding(TitleProperty, new Binding("Caption") {Source = action});
                            break;
                        case ViewActionDisplayMode.HighestSignificance:
                            if (action.Significance == ViewActionSignificance.Highest)
                                SetBinding(TitleProperty, new Binding("Caption") {Source = action});
                            break;
                        case ViewActionDisplayMode.NormalSignificanceAndHigher:
                            if (action.Significance == ViewActionSignificance.Normal || action.Significance == ViewActionSignificance.AboveNormal || action.Significance == ViewActionSignificance.Highest)
                                SetBinding(TitleProperty, new Binding("Caption") {Source = action});
                            break;
                        case ViewActionDisplayMode.BelowNormalSignificanceAndHigher:
                            if (action.Significance == ViewActionSignificance.BelowNormal || action.Significance == ViewActionSignificance.Normal || action.Significance == ViewActionSignificance.AboveNormal || action.Significance == ViewActionSignificance.Highest)
                                SetBinding(TitleProperty, new Binding("Caption") {Source = action});
                            break;
                    }
                }
            }
            else
                SetBinding(TitleProperty, new Binding("Caption") {Source = action});

            SetBinding(HasTitleProperty, new Binding("Title") {Source = this, Converter = new EmptyStringToBooleanConverter()});

            Command = action;

            SetBinding(VisibilityProperty, new Binding("Availability") {Source = Action, Converter = new AvailabilityToVisibleConverter()});
            SetBinding(IsCheckedProperty, new Binding("IsChecked") {Source = Action});
            SetBinding(SignificanceProperty, new Binding("Significance") {Source = Action});
        }

        /// <summary>
        /// View action associated with this button
        /// </summary>
        /// <value>The action.</value>
        public IViewAction Action
        {
            get { return (IViewAction)GetValue(ActionProperty); }
            set { SetValue(ActionProperty, value); }
        }
        /// <summary>
        /// View action associated with this button
        /// </summary>
        /// <value>The action.</value>
        public static readonly DependencyProperty ActionProperty = DependencyProperty.Register("Action", typeof(IViewAction), typeof(ViewActionToolbarButton), new PropertyMetadata(null));

        /// <summary>
        /// Indicates what level of view action titles (text) should be displayed for.
        /// Note that text is always displayed if no icon is available.
        /// </summary>
        /// <value>The title display filter.</value>
        public ViewActionDisplayMode TitleDisplayFilter
        {
            get { return (ViewActionDisplayMode)GetValue(TitleDisplayFilterProperty); }
            set { SetValue(TitleDisplayFilterProperty, value); }
        }
        /// <summary>
        /// Indicates what level of view action titles (text) should be displayed for.
        /// Note that text is always displayed if no icon is available.
        /// </summary>
        /// <value>The title display filter.</value>
        public static readonly DependencyProperty TitleDisplayFilterProperty = DependencyProperty.Register("TitleDisplayFilter", typeof(ViewActionDisplayMode), typeof(ViewActionToolbarButton), new PropertyMetadata(ViewActionDisplayMode.AboveNormalSignificanceAndHigher));
        
        /// <summary>
        /// Indicates whether this button has an icon
        /// </summary>
        /// <value><c>true</c> if this instance has icon; otherwise, <c>false</c>.</value>
        public bool HasIcon
        {
            get { return (bool)GetValue(HasIconProperty); }
            set { SetValue(HasIconProperty, value); }
        }
        /// <summary>
        /// Indicates whether this button has an icon
        /// </summary>
        /// <value><c>true</c> if this instance has icon; otherwise, <c>false</c>.</value>
        public static readonly DependencyProperty HasIconProperty = DependencyProperty.Register("HasIcon", typeof(bool), typeof(ViewActionToolbarButton), new PropertyMetadata(false));

        /// <summary>
        /// Indicates whether the button has a title that should be displayed
        /// </summary>
        /// <value><c>true</c> if this instance has title; otherwise, <c>false</c>.</value>
        public bool HasTitle
        {
            get { return (bool)GetValue(HasTitleProperty); }
            set { SetValue(HasTitleProperty, value); }
        }
        /// <summary>
        /// Indicates whether the button has a title that should be displayed
        /// </summary>
        /// <value><c>true</c> if this instance has title; otherwise, <c>false</c>.</value>
        public static readonly DependencyProperty HasTitleProperty = DependencyProperty.Register("HasTitle", typeof(bool), typeof(ViewActionToolbarButton), new PropertyMetadata(true));

        /// <summary>
        /// Title of the button
        /// </summary>
        /// <value>The title.</value>
        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }
        /// <summary>
        /// Title of the button
        /// </summary>
        /// <value>The title.</value>
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register("Title", typeof(string), typeof(ViewActionToolbarButton), new PropertyMetadata(""));

        /// <summary>
        /// Indicates whether the associated action has its IsChecked property set
        /// </summary>
        /// <value><c>true</c> if this instance is checked; otherwise, <c>false</c>.</value>
        public bool IsChecked
        {
            get { return (bool)GetValue(IsCheckedProperty); }
            set { SetValue(IsCheckedProperty, value); }
        }
        /// <summary>
        /// Indicates whether the associated action has its IsChecked property set
        /// </summary>
        public static readonly DependencyProperty IsCheckedProperty = DependencyProperty.Register("IsChecked", typeof(bool), typeof(ViewActionToolbarButton), new PropertyMetadata(false));

        /// <summary>
        /// Significance of the associated view-action
        /// </summary>
        public ViewActionSignificance Significance
        {
            get { return (ViewActionSignificance)GetValue(SignificanceProperty); }
            set { SetValue(SignificanceProperty, value); }
        }
        /// <summary>
        /// Significance of the associated view-action
        /// </summary>
        public static readonly DependencyProperty SignificanceProperty = DependencyProperty.Register("Significance", typeof(ViewActionSignificance), typeof(ViewActionToolbarButton), new PropertyMetadata(ViewActionSignificance.Normal));
    }

    /// <summary>
    /// For internal use only
    /// </summary>
    /// <seealso cref="System.Windows.Data.IValueConverter" />
    public class EmptyStringToBooleanConverter : IValueConverter
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
            return !string.IsNullOrEmpty(value.ToString());
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
            return value;
        }
    }

    /// <summary>
    /// Separator class used for view action separators
    /// </summary>
    public class ViewActionToolbarSeparator : Separator
    {
    }
}

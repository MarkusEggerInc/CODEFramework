using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Shapes;

namespace CODE.Framework.Wpf.Mvvm
{
    /// <summary>
    /// Stack panel populated with view actions
    /// </summary>
    public class ViewActionStackPanel : StackPanel
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
        public static readonly DependencyProperty ModelProperty = DependencyProperty.Register("Model", typeof(object), typeof(ViewActionStackPanel), new UIPropertyMetadata(null, ModelChanged));
        /// <summary>
        /// Change handler for model property
        /// </summary>
        /// <param name="d">The dependency object that triggered this change.</param>
        /// <param name="e">The <see cref="System.Windows.DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        static void ModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var panel = d as ViewActionStackPanel;
            if (panel == null) return;
            var actionsContainer = e.NewValue as IHaveActions;
            if (actionsContainer != null && actionsContainer.Actions != null)
            {
                actionsContainer.Actions.CollectionChanged += (s, e2) => panel.PopulateStack();
                panel.PopulateStack();
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
        public static readonly DependencyProperty SelectedViewProperty = DependencyProperty.Register("SelectedView", typeof(object), typeof(ViewActionStackPanel), new UIPropertyMetadata(null, SelectedViewChanged));
        /// <summary>
        /// Change handler for selected view property
        /// </summary>
        /// <param name="d">The dependency object that triggered this change.</param>
        /// <param name="e">The <see cref="System.Windows.DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        static void SelectedViewChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d == null) return;
            var stack = d as ViewActionStackPanel; 
            if (stack == null) return;
            var viewResult = e.NewValue as ViewResult;
            if (viewResult == null)
            {
                stack.PopulateStack();
                return;
            }

            var actionsContainer = viewResult.Model as IHaveActions;
            if (actionsContainer != null)
            {
                actionsContainer.Actions.CollectionChanged += (s, e2) => stack.PopulateStack();
                stack.PopulateStack();
            }
            else
                stack.PopulateStack();
        }

        /// <summary>
        /// Defines which view actions are to be displayed in this stack panel
        /// </summary>
        /// <value>The action filter.</value>
        public ViewActionStackPanelActionFilter ActionFilter
        {
            get { return (ViewActionStackPanelActionFilter)GetValue(ActionFilterProperty); }
            set { SetValue(ActionFilterProperty, value); }
        }

        /// <summary>
        /// Defines which view actions are to be displayed in this stack panel
        /// </summary>
        public static readonly DependencyProperty ActionFilterProperty = DependencyProperty.Register("ActionFilter", typeof(ViewActionStackPanelActionFilter), typeof(ViewActionStackPanel), new PropertyMetadata(ViewActionStackPanelActionFilter.ShowPinned, OnActionFilterChanged));

        /// <summary>
        /// Fires when the action filter changes
        /// </summary>
        /// <param name="d">The object the filter was changed on</param>
        /// <param name="args">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void OnActionFilterChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            if (d == null) return;
            var stack = d as ViewActionStackPanel;
            if (stack == null) return;
            stack.PopulateStack();
        }

        /// <summary>
        /// Populates the current ribbon with items based on the actions collection
        /// </summary>
        protected virtual void PopulateStack()
        {
            var actions = Model as IHaveActions;
            var viewResult = SelectedView as ViewResult;
            IHaveActions actions2 = null;
            if (viewResult != null) actions2 = viewResult.Model as IHaveActions;

            Children.Clear();
            if (actions == null)
            {
                Visibility = Visibility.Collapsed;
                return;
            }
            Visibility = Visibility.Visible;

            var actionList = ViewActionHelper.GetConsolidatedActions(actions, actions2);
            foreach (var action in actionList)
            {
                var button = new ViewActionStackPanelButton {Command = action, ToolTip = action.Caption, DataContext = action};
                if (ActionFilter == ViewActionStackPanelActionFilter.ShowPinned)
                    button.SetBinding(VisibilityProperty, new MultiBinding
                    {
                        Bindings =
                        {
                            new Binding("Visibility"),
                            new Binding("IsPinned")
                        },
                        Converter = new VisibilityAndPinnedConverter()
                    });
                else
                    button.SetBinding(VisibilityProperty, new Binding("Visibility"));
                var action2 = action as ViewAction;
                if (action2 != null)
                {
                    var rectangle = new Rectangle {Fill = action2.PopulatedBrush};
                    button.Content = rectangle;
                }
                Children.Add(button);
            }
        }

        private class VisibilityAndPinnedConverter : IMultiValueConverter
        {
            public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
            {
                if (values.Length != 2) return Visibility.Collapsed;
                if (!(values[0] is Visibility)) return Visibility.Collapsed;
                if (!(values[1] is bool)) return Visibility.Collapsed;

                var visibility = (Visibility)values[0];
                var pinned = (bool)values[1];

                if (visibility == Visibility.Visible && pinned) return Visibility.Visible;
                return Visibility.Collapsed;
            }

            public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }
    }

    /// <summary>
    /// Button class used by the ViewActionStackPanel class
    /// </summary>
    public class ViewActionStackPanelButton : Button
    {
    }

    /// <summary>
    /// Defines which view actions to show in a view action stack panel
    /// </summary>
    public enum ViewActionStackPanelActionFilter
    {
        /// <summary>
        /// Shows all available and visible view actions
        /// </summary>
        ShowAll,
        /// <summary>
        /// Shows view actions that are available, visible, and pinned
        /// </summary>
        ShowPinned
    }
}

using System.Linq;
using System.Windows;
using System.Windows.Controls;
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
                actionsContainer.Actions.CollectionChanged += (s, e2) => panel.PopulateStack(actionsContainer);
                panel.Visibility = Visibility.Visible;
                panel.PopulateStack(actionsContainer);
            }
            else
                panel.Visibility = Visibility.Collapsed;
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
                stack.PopulateStack(stack.Model as IHaveActions);
                return;
            }

            var actionsContainer = viewResult.Model as IHaveActions;
            if (actionsContainer != null)
            {
                actionsContainer.Actions.CollectionChanged += (s, e2) => stack.PopulateStack(stack.Model as IHaveActions, actionsContainer);
                stack.PopulateStack(stack.Model as IHaveActions, actionsContainer);
            }
            else
                stack.PopulateStack(stack.Model as IHaveActions);
        }

        /// <summary>
        /// Populates the current ribbon with items based on the actions collection
        /// </summary>
        /// <param name="actions">List of primary actions</param>
        /// <param name="actions2">List of view specific actions</param>
        private void PopulateStack(IHaveActions actions, IHaveActions actions2 = null)
        {
            Children.Clear();
            if (actions == null) return;

            var actionList = ViewActionHelper.GetConsolidatedActions(actions, actions2);

            foreach (var action in actionList.Where(a => a.IsPinned))
            {
                var button = new ViewActionStackPanelButton {Command = action, ToolTip = action.Caption};
                var action2 = action as ViewAction;
                if (action2 != null)
                {
                    var rectangle = new Rectangle {Fill = action2.PopulatedBrush};
                    button.Content = rectangle;
                }
                Children.Add(button);
            }
        }
    }

    /// <summary>
    /// Button class used by the ViewActionStackPanel class
    /// </summary>
    public class ViewActionStackPanelButton : Button
    {
        
    }
}

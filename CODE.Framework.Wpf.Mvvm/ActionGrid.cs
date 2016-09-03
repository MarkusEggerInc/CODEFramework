using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace CODE.Framework.Wpf.Mvvm
{
    /// <summary>
    /// Grid UI element that is automatically made visible and invisible depending on whether the current model implements IHaveActions
    /// </summary>
    public class ActionGrid : Grid
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
        public static readonly DependencyProperty ModelProperty = DependencyProperty.Register("Model", typeof(object), typeof(ActionGrid), new UIPropertyMetadata(null, ModelChanged));
        /// <summary>
        /// Change handler for model property
        /// </summary>
        /// <param name="d">The dependency object that triggered this change.</param>
        /// <param name="e">The <see cref="System.Windows.DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        static void ModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var grid = d as ActionGrid;
            if (grid == null) return;
            var actions = e.NewValue as IHaveActions;
            if (actions == null)
            {
                grid.Visibility = Visibility.Collapsed;
                return;
            }

            grid.Visibility = Visibility.Visible;

            grid.InputBindings.Clear();
            foreach (var action in actions.Actions)
                if (action.ShortcutKey != Key.None)
                    grid.InputBindings.Add(new KeyBinding(action, action.ShortcutKey, action.ShortcutModifiers));
        }
    }

    /// <summary>
    /// Provides a control that can be bound to an arbitrary object and if that object implements IHaveActions, it uses the collection of actions as its data source.
    /// </summary>
    public class ActionItemsControl : ItemsControl
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
        public static readonly DependencyProperty ModelProperty = DependencyProperty.Register("Model", typeof(object), typeof(ActionItemsControl), new UIPropertyMetadata(null, ModelChanged));
        /// <summary>
        /// Change handler for model property
        /// </summary>
        /// <param name="d">The dependency object that triggered this change.</param>
        /// <param name="e">The <see cref="System.Windows.DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        static void ModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var itemsControl = d as ActionItemsControl;
            if (itemsControl != null)
            {
                var actionHost = e.NewValue as IHaveActions;
                if (actionHost != null)
                {
                    RepopulateItems(itemsControl, actionHost);
                    actionHost.ActionsChanged += (s, e2) => RepopulateItems(itemsControl, actionHost);
                }
                else
                    itemsControl.Visibility = Visibility.Collapsed;
            }
        }

        private static void RepopulateItems(ActionItemsControl itemsControl, IHaveActions actionHost)
        {
            itemsControl.Visibility = Visibility.Visible;

            // We also need to hook all actions' changed event so we can update this view whenever the availability changes
            foreach (var inpc in actionHost.Actions.OfType<INotifyPropertyChanged>())
                inpc.PropertyChanged += (s, a) =>
                                            {
                                                if (a.PropertyName == "Availability")
                                                    RepopulateItems(itemsControl, actionHost);
                                            };

            // Getting all currently available actions
            var actions = actionHost.Actions.Where(a => a.Availability == ViewActionAvailabilities.Available).OrderBy(a => a.CategoryOrder).ToList();
            if (itemsControl.ViewActionPolicy != null) actions = itemsControl.ViewActionPolicy.ProcessActions(actions).ToList();
            itemsControl.ItemsSource = itemsControl.OnRepopulateItems(actions);
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
        public static readonly DependencyProperty ViewActionPolicyProperty = DependencyProperty.Register("ViewActionPolicy", typeof(IViewActionPolicy), typeof(ActionItemsControl), new PropertyMetadata(null));

        /// <summary>
        /// This method is designed to be overridden in subclasses
        /// </summary>
        /// <param name="actions"></param>
        protected virtual IEnumerable OnRepopulateItems(List<IViewAction> actions)
        {
            return actions;
        }
    }

    /// <summary>
    /// Grid UI element that is automatically made visible and invisible depending on whether it's ViewResult property is in fact of type ViewResult and view result header information is attached to the view result
    /// </summary>
    public class ViewResultHeaderGrid : Grid
    {
        /// <summary>
        /// Icon brush
        /// </summary>
        public Brush TitleIconBrush
        {
            get { return (Brush)GetValue(TitleIconBrushProperty); }
            set { SetValue(TitleIconBrushProperty, value); }
        }
        /// <summary>
        /// Icon brush
        /// </summary>
        public static readonly DependencyProperty TitleIconBrushProperty = DependencyProperty.Register("TitleIconBrush", typeof(Brush), typeof(ViewResultHeaderGrid), new UIPropertyMetadata(null));
        
        /// <summary>
        /// Document result title
        /// </summary>
        public string ViewTitle
        {
            get { return (string)GetValue(ViewTitleProperty); }
            set { SetValue(ViewTitleProperty, value); }
        }
        /// <summary>
        /// Document result title
        /// </summary>
        public static readonly DependencyProperty ViewTitleProperty = DependencyProperty.Register("ViewTitle", typeof(string), typeof(ViewResultHeaderGrid), new UIPropertyMetadata(""));

        /// <summary>
        /// ViewResult used as the data context
        /// </summary>
        public object ViewResult
        {
            get { return GetValue(ViewResultProperty); }
            set { SetValue(ViewResultProperty, value); }
        }
        /// <summary>
        /// Model dependency property
        /// </summary>
        public static readonly DependencyProperty ViewResultProperty = DependencyProperty.Register("ViewResult", typeof(object), typeof(ViewResultHeaderGrid), new UIPropertyMetadata(null, ViewResultChanged));
        /// <summary>
        /// Change handler for model property
        /// </summary>
        /// <param name="d">The dependency object that triggered this change.</param>
        /// <param name="e">The <see cref="System.Windows.DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        static void ViewResultChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var grid = d as ViewResultHeaderGrid;
            if (grid != null)
            {
                var viewResult = e.NewValue as ViewResult;
                if (viewResult != null)
                {
                    bool isVisible = false;
                    if (!string.IsNullOrEmpty(viewResult.ViewTitle))
                    {
                        isVisible = true;
                        grid.ViewTitle = viewResult.ViewTitle;
                    }
                    if (!string.IsNullOrEmpty(viewResult.ViewIconResourceKey))
                    {
                        Brush brush = null;
                        try
                        {
                            var resource = Application.Current.FindResource(viewResult.ViewIconResourceKey);
                            if ((resource) != null) brush = resource as Brush;
                        }
                        catch
                        {
                            brush = null;
                        }
                        if (brush != null)
                        {
                            isVisible = true;
                            grid.TitleIconBrush = brush;
                        }
                    }
                    grid.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
                }
                else
                    grid.Visibility = Visibility.Collapsed;
            }
        }
    }
}

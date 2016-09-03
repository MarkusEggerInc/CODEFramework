using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CODE.Framework.Wpf.Layout;
using CODE.Framework.Wpf.Utilities;

namespace CODE.Framework.Wpf.Mvvm
{
    /// <summary>
    /// Items control that auto-populates from an Actions collection of model object that implements IHaveActions
    /// </summary>
    public class ViewActionItemsControl : ItemsControl
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
        public static readonly DependencyProperty ModelProperty = DependencyProperty.Register("Model", typeof(object), typeof(ViewActionItemsControl), new UIPropertyMetadata(null, ModelChanged));

        /// <summary>
        /// Change handler for model property
        /// </summary>
        /// <param name="d">The dependency object that triggered this change.</param>
        /// <param name="e">The <see cref="System.Windows.DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void ModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var itemsControl = d as ViewActionItemsControl;
            if (itemsControl == null) return;
            itemsControl.RepopulateItems(e.NewValue);
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
        public static readonly DependencyProperty ViewActionPolicyProperty = DependencyProperty.Register("ViewActionPolicy", typeof(IViewActionPolicy), typeof(ViewActionItemsControl), new PropertyMetadata(null));

        /// <summary>
        /// If set to true, actions are sorted by group title, before they are sorted by order
        /// </summary>
        /// <value><c>true</c> if [order by group title]; otherwise, <c>false</c>.</value>
        public bool OrderByGroupTitle
        {
            get { return (bool)GetValue(OrderByGroupTitleProperty); }
            set { SetValue(OrderByGroupTitleProperty, value); }
        }
        /// <summary>
        /// If set to true, actions are sorted by group title, before they are sorted by order
        /// </summary>
        public static readonly DependencyProperty OrderByGroupTitleProperty = DependencyProperty.Register("OrderByGroupTitle", typeof (bool), typeof (ViewActionItemsControl), new PropertyMetadata(false));
        
        /// <summary>
        /// Title for empty global category titles (default: File)
        /// </summary>
        public string EmptyGlobalCategoryTitle
        {
            get { return (string)GetValue(EmptyGlobalCategoryTitleProperty); }
            set { SetValue(EmptyGlobalCategoryTitleProperty, value); }
        }

        /// <summary>
        /// Title for empty global category titles (default: File)
        /// </summary>
        public static readonly DependencyProperty EmptyGlobalCategoryTitleProperty = DependencyProperty.Register("EmptyGlobalCategoryTitle", typeof(string), typeof(ViewActionItemsControl), new PropertyMetadata("File"));

        private void RepopulateItems(object model)
        {
            var actionsContainer = model as IHaveActions;
            if (actionsContainer != null && actionsContainer.Actions != null)
            {
                actionsContainer.Actions.CollectionChanged += (s, e2) => PopulateItems(actionsContainer);
                Visibility = Visibility.Visible;
                PopulateItems(actionsContainer);
            }
            else
                Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Populates the current items control with items based on the actions collection
        /// </summary>
        /// <param name="actions">List of actions</param>
        protected virtual void PopulateItems(IHaveActions actions)
        {
            RemoveAllMenuKeyBindings();
            Items.Clear();
            if (actions == null || actions.Actions == null) return;

            var actionList = actions.Actions.ToList();
            var rootCategories = ViewActionPolicy != null ? ViewActionPolicy.GetTopLevelActionCategories(actionList, EmptyGlobalCategoryTitle) : ViewActionHelper.GetTopLevelActionCategories(actionList, EmptyGlobalCategoryTitle);

            var viewActionCategories = rootCategories as ViewActionCategory[] ?? rootCategories.ToArray();
            foreach (var category in viewActionCategories)
            {
                var matchingActions = ViewActionPolicy != null ? ViewActionPolicy.GetAllActionsForCategory(actionList, category, orderByGroupTitle: OrderByGroupTitle) : ViewActionHelper.GetAllActionsForCategory(actionList, category, orderByGroupTitle: OrderByGroupTitle);
                foreach (var action in matchingActions)
                {
                    var wrapper = new DependencyViewActionWrapper(action);
                    if (action.Categories.Count > 0)
                    {
                        MetroTiles.SetGroupName(wrapper, action.Categories[0].Id);
                        MetroTiles.SetGroupTitle(wrapper, action.Categories[0].Caption);
                    }
                    else
                    {
                        MetroTiles.SetGroupName(wrapper, string.Empty);
                        MetroTiles.SetGroupTitle(wrapper, string.Empty);
                    }
                    if (action.Availability != ViewActionAvailabilities.Unavailable)
                        MetroTiles.SetTileVisibility(wrapper, action.Visibility);
                    else
                        MetroTiles.SetTileVisibility(wrapper, Visibility.Collapsed);
                    Items.Add(wrapper);
                    if (action.ShortcutKey == Key.None) continue;
                    MenuKeyBindings.Add(new ViewActionMenuKeyBinding(action));
                }
            }

            CreateAllMenuKeyBindings();
        }

        /// <summary>
        /// For internal use only
        /// </summary>
        protected readonly List<ViewActionMenuKeyBinding> MenuKeyBindings = new List<ViewActionMenuKeyBinding>();

        /// <summary>
        /// Removes all key bindings from the current window that were associated with a view category menu
        /// </summary>
        protected virtual void CreateAllMenuKeyBindings()
        {
            var window = ElementHelper.FindVisualTreeParent<Window>(this);
            if (window == null) return;

            foreach (var binding in MenuKeyBindings)
                window.InputBindings.Add(binding);
        }

        /// <summary>
        /// Removes all key bindings from the current window that were associated with a view category menu
        /// </summary>
        protected virtual void RemoveAllMenuKeyBindings()
        {
            MenuKeyBindings.Clear();

            var window = ElementHelper.FindVisualTreeParent<Window>(this);
            if (window == null) return;

            var bindingIndex = 0;
            while (true)
            {
                if (bindingIndex >= window.InputBindings.Count) break;
                var binding = window.InputBindings[bindingIndex];
                if (binding is ViewActionMenuKeyBinding)
                    window.InputBindings.RemoveAt(bindingIndex); // We remove the item from the collection and start over with the remove operation since now all indexes changed
                else
                    bindingIndex++;
            }
        }
    }
}

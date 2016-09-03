using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CODE.Framework.Wpf.Mvvm
{
    /// <summary>
    /// Interface IViewActionPolicy
    /// </summary>
    public interface IViewActionPolicy
    {
        /// <summary>
        /// Provides a way to impact a collection of view-actions before they are used by the UI.
        /// </summary>
        /// <param name="originalActions">The original list of actions.</param>
        /// <param name="viewModel">Optional view model.</param>
        /// <param name="tag">An additional (optional) tag that can be used to identify where the call comes from, or what it is used for. (For instance "MenuButton" indicates that the list of actions is used for actions in the menu of a menu button).</param>
        /// <returns>IEnumerable&lt;IViewAction&gt;.</returns>
        IEnumerable<IViewAction> ProcessActions(IEnumerable<IViewAction> originalActions, object viewModel = null, string tag = "");

        /// <summary>
        /// Returns a list of consolidated actions from two different action lists.
        /// </summary>
        /// <param name="actions">The first set of actions.</param>
        /// <param name="actions2">The second set of actions.</param>
        /// <param name="defaultEmptyCategory">The default empty category.</param>
        /// <param name="actionsDisplayFilter">The actions display filter.</param>
        /// <param name="flagFirstSecondaryActionAsNewGroup">Defines whether the first secondary action is to be flagged as a new group.</param>
        /// <param name="actions2DisplayFilter">Display filter for the secondary set of actions</param>
        /// <param name="viewModel">Optional view model object</param>
        /// <returns>ObservableCollection&lt;IViewAction&gt;.</returns>
        ObservableCollection<IViewAction> GetConsolidatedActions(IHaveActions actions, IHaveActions actions2 = null, string defaultEmptyCategory = "", ViewActionDisplayMode actionsDisplayFilter = ViewActionDisplayMode.All, bool flagFirstSecondaryActionAsNewGroup = false, ViewActionDisplayMode actions2DisplayFilter = ViewActionDisplayMode.All, object viewModel = null);

        /// <summary>
        /// Gets the top level action categories.
        /// </summary>
        /// <param name="actions">The actions.</param>
        /// <param name="emptyGlobalCategoryTitle">The empty global category title.</param>
        /// <param name="emptyLocalCategoryTitle">The empty local category title.</param>
        /// <param name="viewModel">Optional view model object</param>
        /// <returns>IEnumerable&lt;ViewActionCategory&gt;.</returns>
        IEnumerable<ViewActionCategory> GetTopLevelActionCategories(IEnumerable<IViewAction> actions, string emptyGlobalCategoryTitle = "", string emptyLocalCategoryTitle = "", object viewModel = null);

        /// <summary>
        /// Gets all actions for the specified category.
        /// </summary>
        /// <param name="actions">The actions.</param>
        /// <param name="category">The category.</param>
        /// <param name="indentLevel">The indent level.</param>
        /// <param name="emptyCategory">The empty category.</param>
        /// <param name="orderByGroupTitle">if set to <c>true</c> [order by group title].</param>
        /// <param name="viewModel">Optional view model object</param>
        /// <returns>IEnumerable&lt;IViewAction&gt;.</returns>
        IEnumerable<IViewAction> GetAllActionsForCategory(IEnumerable<IViewAction> actions, ViewActionCategory category, int indentLevel = 0, string emptyCategory = "File", bool orderByGroupTitle = true, object viewModel = null);
    }

    /// <summary>
    /// Default implementation for view-action policies
    /// </summary>
    /// <seealso cref="CODE.Framework.Wpf.Mvvm.IViewActionPolicy" />
    public class DefaultViewActionPolicy : IViewActionPolicy
    {
        /// <summary>
        /// Provides a way to impact a collection of view-actions before they are used by the UI.
        /// </summary>
        /// <param name="originalActions">The original list of actions.</param>
        /// <param name="viewModel">Optional view model.</param>
        /// <param name="tag">An additional (optional) tag that can be used to identify where the call comes from, or what it is used for. (For instance "MenuButton" indicates that the list of actions is used for actions in the menu of a menu button).</param>
        /// <returns>IEnumerable&lt;IViewAction&gt;.</returns>
        public virtual IEnumerable<IViewAction> ProcessActions(IEnumerable<IViewAction> originalActions, object viewModel = null, string tag = "")
        {
            return originalActions;
        }

        /// <summary>
        /// Returns a list of consolidated actions from two different action lists.
        /// </summary>
        /// <param name="actions">The first set of actions.</param>
        /// <param name="actions2">The second set of actions.</param>
        /// <param name="defaultEmptyCategory">The default empty category.</param>
        /// <param name="actionsDisplayFilter">The actions display filter.</param>
        /// <param name="flagFirstSecondaryActionAsNewGroup">Defines whether the first secondary action is to be flagged as a new group.</param>
        /// <param name="actions2DisplayFilter">Display filter for the secondary set of actions</param>
        /// <param name="viewModel">Optional view model object</param>
        /// <returns>ObservableCollection&lt;IViewAction&gt;.</returns>
        public virtual ObservableCollection<IViewAction> GetConsolidatedActions(IHaveActions actions, IHaveActions actions2 = null, string defaultEmptyCategory = "", ViewActionDisplayMode actionsDisplayFilter = ViewActionDisplayMode.All, bool flagFirstSecondaryActionAsNewGroup = false, ViewActionDisplayMode actions2DisplayFilter = ViewActionDisplayMode.All, object viewModel = null)
        {
            return ViewActionHelper.GetConsolidatedActions(actions, actions2, defaultEmptyCategory, actionsDisplayFilter, flagFirstSecondaryActionAsNewGroup, actions2DisplayFilter);
        }

        /// <summary>
        /// Gets the top level action categories.
        /// </summary>
        /// <param name="actions">The actions.</param>
        /// <param name="emptyGlobalCategoryTitle">The empty global category title.</param>
        /// <param name="emptyLocalCategoryTitle">The empty local category title.</param>
        /// <param name="viewModel">Optional view model object</param>
        /// <returns>IEnumerable&lt;ViewActionCategory&gt;.</returns>
        public virtual IEnumerable<ViewActionCategory> GetTopLevelActionCategories(IEnumerable<IViewAction> actions, string emptyGlobalCategoryTitle = "", string emptyLocalCategoryTitle = "", object viewModel = null)
        {
            return ViewActionHelper.GetTopLevelActionCategories(actions, emptyGlobalCategoryTitle, emptyLocalCategoryTitle);
        }

        /// <summary>
        /// Gets all actions for the specified category.
        /// </summary>
        /// <param name="actions">The actions.</param>
        /// <param name="category">The category.</param>
        /// <param name="indentLevel">The indent level.</param>
        /// <param name="emptyCategory">The empty category.</param>
        /// <param name="orderByGroupTitle">if set to <c>true</c> [order by group title].</param>
        /// <param name="viewModel">Optional view model object</param>
        /// <returns>IEnumerable&lt;IViewAction&gt;.</returns>
        public virtual IEnumerable<IViewAction> GetAllActionsForCategory(IEnumerable<IViewAction> actions, ViewActionCategory category, int indentLevel = 0, string emptyCategory = "File", bool orderByGroupTitle = true, object viewModel = null)
        {
            return ViewActionHelper.GetAllActionsForCategory(actions, category, indentLevel, emptyCategory, orderByGroupTitle);
        }
    }
}

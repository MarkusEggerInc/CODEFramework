using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CODE.Framework.Wpf.Mvvm
{
    /// <summary>
    /// This class is designed mostly for internal use and provides functions related to standard view action tasks
    /// </summary>
    public static class ViewActionHelper
    {
        /// <summary>
        /// Inspects up to 2 IHaveActions interfaces and returns a consolidated list of actions for both interfaces
        /// </summary>
        /// <param name="actions">The first interface containing actions</param>
        /// <param name="actions2">The second interface containing actions</param>
        /// <param name="defaultEmptyCategory">The default empty category.</param>
        /// <param name="actionsDisplayFilter">Indicates which actions out of the first actions collection shall be included.</param>
        /// <param name="flagFirstSecondaryActionAsNewGroup">Indicates whether the first action from the second actions collection should be flagged as a new group</param>
        /// <param name="actions2DisplayFilter">Indicates which actions out of the second actions collection shall be included.</param>
        /// <returns>ObservableCollection{IViewAction}.</returns>
        public static ObservableCollection<IViewAction> GetConsolidatedActions(IHaveActions actions, IHaveActions actions2 = null, string defaultEmptyCategory = "", ViewActionDisplayMode actionsDisplayFilter = ViewActionDisplayMode.All, bool flagFirstSecondaryActionAsNewGroup = false, ViewActionDisplayMode actions2DisplayFilter = ViewActionDisplayMode.All)
        {
            var actionList = new ObservableCollection<IViewAction>();

            if (actions == null || actions.Actions == null) return actionList;

            if (actionsDisplayFilter != ViewActionDisplayMode.None)
            {
                var list = actions.Actions.OrderBy(a => a.FirstCategoryId).ToList();
                switch (actionsDisplayFilter)
                {
                    case ViewActionDisplayMode.BelowNormalSignificanceAndHigher:
                        list = list.Where(a => a.Significance == ViewActionSignificance.BelowNormal || a.Significance == ViewActionSignificance.Normal || a.Significance == ViewActionSignificance.AboveNormal || a.Significance == ViewActionSignificance.Highest).ToList();
                        break;
                    case ViewActionDisplayMode.NormalSignificanceAndHigher:
                        list = list.Where(a => a.Significance == ViewActionSignificance.Normal || a.Significance == ViewActionSignificance.AboveNormal || a.Significance == ViewActionSignificance.Highest).ToList();
                        break;
                    case ViewActionDisplayMode.AboveNormalSignificanceAndHigher:
                        list = list.Where(a => a.Significance == ViewActionSignificance.AboveNormal || a.Significance == ViewActionSignificance.Highest).ToList();
                        break;
                    case ViewActionDisplayMode.HighestSignificance:
                        list = list.Where(a => a.Significance == ViewActionSignificance.Highest).ToList();
                        break;
                }
                foreach (var action in list) actionList.Add(action);
            }

            if (actions2 != null && actions2.Actions.Count > 0 && actions2DisplayFilter != ViewActionDisplayMode.None)
            {
                var actionsCount = 0;
                var list = actions2.Actions.ToList();
                switch (actions2DisplayFilter)
                {
                    case ViewActionDisplayMode.BelowNormalSignificanceAndHigher:
                        list = list.Where(a => a.Significance == ViewActionSignificance.BelowNormal || a.Significance == ViewActionSignificance.Normal || a.Significance == ViewActionSignificance.AboveNormal || a.Significance == ViewActionSignificance.Highest).ToList();
                        break;
                    case ViewActionDisplayMode.NormalSignificanceAndHigher:
                        list = list.Where(a => a.Significance == ViewActionSignificance.Normal || a.Significance == ViewActionSignificance.AboveNormal || a.Significance == ViewActionSignificance.Highest).ToList();
                        break;
                    case ViewActionDisplayMode.AboveNormalSignificanceAndHigher:
                        list = list.Where(a => a.Significance == ViewActionSignificance.AboveNormal || a.Significance == ViewActionSignificance.Highest).ToList();
                        break;
                    case ViewActionDisplayMode.HighestSignificance:
                        list = list.Where(a => a.Significance == ViewActionSignificance.Highest).ToList();
                        break;
                }
                foreach (var action in list)
                {
                    if (actionsCount == 0 && flagFirstSecondaryActionAsNewGroup) action.BeginGroup = true;
                    if (action.Categories.Count == 1 && string.IsNullOrEmpty(action.Categories[0].Caption))
                        action.Categories[0].Caption = defaultEmptyCategory;
                    if (action.Categories.Count == 1 && string.IsNullOrEmpty(action.Categories[0].Id))
                        action.Categories[0].Id = defaultEmptyCategory.Replace(" ", "");
                    if (action.Categories.Count == 0)
                        action.Categories.Add(new ViewActionCategory(defaultEmptyCategory.Replace(" ", ""), defaultEmptyCategory));
                    var viewAction = action as ViewAction;
                    if (viewAction != null)
                    {
                        viewAction.IsLocalAction = true;
                    }
                    actionList.Add(action);
                    actionsCount++;
                }
            }

            return actionList;
        }

        /// <summary>
        /// Retrieves a list of all categories at the root of each action
        /// </summary>
        /// <param name="actions">List of actions</param>
        /// <param name="emptyGlobalCategoryTitle">The empty global category title.</param>
        /// <param name="emptyLocalCategoryTitle">The empty local category title.</param>
        /// <returns>IEnumerable{ViewActionCategory}.</returns>
        public static IEnumerable<ViewActionCategory> GetTopLevelActionCategories(IEnumerable<IViewAction> actions, string emptyGlobalCategoryTitle = "", string emptyLocalCategoryTitle = "")
        {
            var result = new List<ViewActionCategory>();
            var globalFileCategoryAdded = false;
            var localFileCategoryAdded = false;

            foreach (var action in actions)
            {
                var viewAction = action as ViewAction;
                if (viewAction != null && viewAction.IsLocalAction)
                {
                    if (!localFileCategoryAdded && (action.Categories == null || action.Categories.Count == 0))
                    {
                        result.Add(new ViewActionCategory(emptyLocalCategoryTitle.Replace(" ", ""), emptyLocalCategoryTitle) {BrushResourceKey = viewAction.CategoryBrushResourceKey });
                        localFileCategoryAdded = true;
                    }
                    else if (action.Categories != null && action.Categories.Count > 0)
                    {
                        var alreadyAdded = false;
                        foreach (var category in result)
                        {
                            var id = action.Categories[0].Id;
                            if (string.IsNullOrEmpty(id)) id = emptyLocalCategoryTitle;
                            var caption = action.Categories[0].Caption;
                            if (string.IsNullOrEmpty(caption)) caption = emptyLocalCategoryTitle;
                            if (category.Caption == caption && category.Id == id)
                            {
                                alreadyAdded = true;
                                if (string.IsNullOrEmpty(category.BrushResourceKey)) category.BrushResourceKey = viewAction.CategoryBrushResourceKey;
                                break;
                            }
                        }
                        if (!alreadyAdded)
                        {
                            var order = action.CategoryOrder;
                            if (order == 0)
                                order += 10000; // Local categories are to be put at the end, unless they have a special setting
                            var id = action.Categories[0].Id;
                            if (string.IsNullOrEmpty(id)) id = emptyLocalCategoryTitle;
                            var caption = action.Categories[0].Caption;
                            if (string.IsNullOrEmpty(caption)) caption = emptyLocalCategoryTitle;
                            result.Add(new ViewActionCategory(id, caption, action.Categories[0].AccessKey)
                            {
                                IsLocalCategory = true,
                                Order = order,
                                BrushResourceKey = viewAction.CategoryBrushResourceKey
                            });
                        }
                        if (action.Categories[0].Id == emptyLocalCategoryTitle.Replace(" ", "")) globalFileCategoryAdded = true;
                    }
                }
                else
                {
                    if (!globalFileCategoryAdded && (action.Categories == null || action.Categories.Count == 0))
                    {
                        var newCategory = new ViewActionCategory(emptyGlobalCategoryTitle.Replace(" ", ""), emptyGlobalCategoryTitle);
                        if (viewAction != null) newCategory.BrushResourceKey = viewAction.CategoryBrushResourceKey;
                        result.Add(newCategory);
                        globalFileCategoryAdded = true;
                    }
                    else if (action.Categories != null && action.Categories.Count > 0)
                    {
                        var alreadyAdded = false;
                        foreach (var category in result)
                        {
                            var id = action.Categories[0].Id;
                            if (string.IsNullOrEmpty(id)) id = emptyGlobalCategoryTitle;
                            var caption = action.Categories[0].Caption;
                            if (string.IsNullOrEmpty(caption)) caption = emptyGlobalCategoryTitle;
                            if (category.Caption == caption && category.Id == id)
                            {
                                alreadyAdded = true;
                                if (viewAction != null && string.IsNullOrEmpty(category.BrushResourceKey)) category.BrushResourceKey = viewAction.CategoryBrushResourceKey;
                                break;
                            }
                        }
                        if (!alreadyAdded)
                        {
                            var id = action.Categories[0].Id;
                            if (string.IsNullOrEmpty(id)) id = emptyGlobalCategoryTitle;
                            var caption = action.Categories[0].Caption;
                            if (string.IsNullOrEmpty(caption)) caption = emptyGlobalCategoryTitle;
                            var newCategory = new ViewActionCategory(id, caption, action.Categories[0].AccessKey)
                            {
                                IsLocalCategory = false,
                                Order = action.CategoryOrder
                            };
                            if (viewAction != null) newCategory.BrushResourceKey = viewAction.CategoryBrushResourceKey;
                            result.Add(newCategory);
                        }
                        if (action.Categories[0].Id == emptyGlobalCategoryTitle.Replace(" ", "")) globalFileCategoryAdded = true;
                    }
                }
            }

            return result.OrderBy(c => c.Order);
        }

        /// <summary>
        /// Gets all actions that fall under the specified category
        /// </summary>
        /// <param name="actions">The actions.</param>
        /// <param name="category">The category.</param>
        /// <param name="indentLevel">The indent level.</param>
        /// <param name="emptyCategory">The empty category.</param>
        /// <param name="orderByGroupTitle">If true, then the result set is first ordered by group title.</param>
        /// <returns>IEnumerable{IViewAction}.</returns>
        public static IEnumerable<IViewAction> GetAllActionsForCategory(IEnumerable<IViewAction> actions, ViewActionCategory category, int indentLevel = 0, string emptyCategory = "File", bool orderByGroupTitle = true)
        {
            var result = new List<IViewAction>();

            foreach (var action in actions)
            {
                if (indentLevel == 0 && ((category.Id == emptyCategory || string.IsNullOrEmpty(category.Id)) && (action.Categories == null || action.Categories.Count == 0))) // If no other category is assigned, then we consider items to be on the file menu
                    result.Add(action);
                else if (action.Categories != null && action.Categories.Count > indentLevel && action.Categories[indentLevel].Id == category.Id)
                    result.Add(action);
                else if (action.Categories != null && action.Categories.Count > indentLevel && action.Categories[indentLevel].Id == emptyCategory && string.IsNullOrEmpty(category.Id))
                    result.Add(action);
            }

            if (orderByGroupTitle)
                return result.OrderBy(a => a.GroupTitle + ":::" + a.Order.ToString("0000000000")).ToList();
            return result.OrderBy(a => a.Order).ToList();
        }
    }

    /// <summary>
    /// Indicates which view actions should be displayed
    /// </summary>
    public enum ViewActionDisplayMode
    {
        /// <summary>
        /// None
        /// </summary>
        None,
        /// <summary>
        /// All
        /// </summary>
        All,
        /// <summary>
        /// View actions with above normal significance
        /// </summary>
        AboveNormalSignificanceAndHigher,
        /// <summary>
        /// View actions with highest significance only
        /// </summary>
        HighestSignificance,
        /// <summary>
        /// Anything that is at least low normal significance
        /// </summary>
        NormalSignificanceAndHigher,
        ///     <summary>
        /// Anything that is at least low significance
        /// </summary>
        BelowNormalSignificanceAndHigher
    }
}

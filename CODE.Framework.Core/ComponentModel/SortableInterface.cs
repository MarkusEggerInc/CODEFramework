namespace CODE.Framework.Core.ComponentModel
{
    /// <summary>
    /// Interface used for sortable collections
    /// </summary>
    public interface ISortable
    {
        /// <summary>
        /// Sort expression
        /// </summary>
        /// <example>FirstName, LastName</example>
        string SortBy { get; set; }

        /// <summary>
        /// Master sort expression
        /// </summary>
        /// <remarks>
        /// Sortable objects are first sorted by the master expression, 
        /// and then by the sort-by expression
        /// </remarks>
        /// <example>Company</example>
        string SortByMaster { get; set; }

        /// <summary>
        /// Complete sort expression
        /// </summary>
        /// <remarks>
        /// This is a combination of the master sort expression
        /// and the sort-by expression
        /// </remarks>
        /// <example>Company, FirstName, LastName</example>
        string CompleteSortExpression { get; }
    }
}

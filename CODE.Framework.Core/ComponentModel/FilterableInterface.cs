namespace CODE.Framework.Core.ComponentModel
{
    /// <summary>
    /// Interface used tor filterable collections
    /// </summary>
    public interface IFilterable
    {
        /// <summary>
        /// Filter expression
        /// </summary>
        /// <example>FirstName = 'John'</example>
        string Filter { get; set; }

        /// <summary>
        /// Filter expression
        /// </summary>
        /// <remarks>
        /// Filterable objects are always filtered by their master expression
        /// AND the individual filter expression.
        /// </remarks>
        /// <example>Status = 1</example>
        string FilterMaster { get; set; }

        /// <summary>
        /// Complete filter expression, including the master filter
        /// and the individual filter
        /// </summary>
        /// <example>(Status = 1) AND (FirstName = 'John')</example>
        string CompleteFilterExpression { get; }

        /// <summary>
        /// Clears out all filter expressions, except the master filter.
        /// </summary>
        void ClearFilter();
    }
}

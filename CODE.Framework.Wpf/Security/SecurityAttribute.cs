using System;

namespace CODE.Framework.Wpf.Security
{
    /// <summary>
    /// Security atttribute that can be applied to properties
    /// </summary>
    /// <seealso cref="System.Attribute" />
    [AttributeUsage(AttributeTargets.Property)]
    public class SecurityAttribute : Attribute
    {
        /// <summary>
        /// Comma-separated list of roles that have read-only access.
        /// </summary>
        /// <remarks>If empty, then everyone has read-only access</remarks>
        /// <value>The read only roles.</value>
        public string ReadOnlyRoles { get; set; }
        /// <summary>
        /// Comma-separated list of roles that have full access.
        /// </summary>
        /// <value>The full access roles.</value>
        /// <remarks>If empty, then everyone has full access</remarks>
        public string FullAccessRoles { get; set; }
    }
}

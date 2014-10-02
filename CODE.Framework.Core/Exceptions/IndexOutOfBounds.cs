using System;
using System.Runtime.Serialization;

namespace CODE.Framework.Core.Exceptions
{
    /// <summary>
    /// Exception class used for enumeration errors.
    /// The error is raised when an enumeration finds its enumeration source in disarray
    /// and thus overshoots the sources bounds
    /// </summary>
    [Serializable]
    public class IndexOutOfBoundsException : Exception
    {
        /// <summary>
        /// Default Constructor.
        /// </summary>
        public IndexOutOfBoundsException() : base(Properties.Resources.IndexOutOfBounds) { }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="message">Exception message</param>
        public IndexOutOfBoundsException(string message) : base(message) { }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="message">Exception message.</param>
        /// <param name="innerException">Inner exception.</param>
        public IndexOutOfBoundsException(string message, Exception innerException) : base(message, innerException) { }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="info">Serialization information.</param>
        /// <param name="context">Streaming context.</param>
        protected IndexOutOfBoundsException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}

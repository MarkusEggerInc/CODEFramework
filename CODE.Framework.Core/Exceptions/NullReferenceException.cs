using System;
using System.Runtime.Serialization;

namespace CODE.Framework.Core.Exceptions
{
    /// <summary>
    /// Exception class used for null reference exceptions thrown by Milos.
    /// </summary>
    [Serializable]
    public class NullReferenceException : Exception
    {
        /// <summary>
        /// Default Constructor.
        /// </summary>
        public NullReferenceException() { }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="message">Exception message</param>
        public NullReferenceException(string message) : base(message) { }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="message">Exception message.</param>
        /// <param name="innerException">Inner exception.</param>
        public NullReferenceException(string message, Exception innerException) : base(message, innerException) { }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="info">Serialization information.</param>
        /// <param name="context">Streaming context.</param>
        protected NullReferenceException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}

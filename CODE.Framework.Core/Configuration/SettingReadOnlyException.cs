using System;
using System.Runtime.Serialization;

namespace CODE.Framework.Core.Configuration
{
    /// <summary>
    /// Exception thrown when some code is trying to write to a read-only setting.
    /// </summary>
    [Serializable]
    public class SettingReadOnlyException : Exception
    {
        /// <summary>
        /// Default Constructor.
        /// </summary>
        public SettingReadOnlyException() { }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="message">Exception message</param>
        public SettingReadOnlyException(string message) : base(message) { }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="message">Exception message.</param>
        /// <param name="innerException">Inner exception.</param>
        public SettingReadOnlyException(string message, Exception innerException) : base(message, innerException) { }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="info">Serialization information.</param>
        /// <param name="context">Streaming context.</param>
        protected SettingReadOnlyException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
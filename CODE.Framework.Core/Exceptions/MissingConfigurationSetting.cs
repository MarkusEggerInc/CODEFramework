using System;
using System.Runtime.Serialization;

namespace CODE.Framework.Core.Exceptions
{
    /// <summary>
    /// This exception is thrown whenever part of Milos requires a configuration setting that is not present.
    /// </summary>
    [Serializable]
    public class MissingConfigurationSettingException : Exception
    {
        /// <summary>
        /// Default Constructor.
        /// </summary>
        public MissingConfigurationSettingException() : base(Properties.Resources.IndexOutOfBounds) { }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="setting">Name of the missing setting</param>
        public MissingConfigurationSettingException(string setting) : base(setting) { }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="message">Exception message.</param>
        /// <param name="innerException">Inner exception.</param>
        public MissingConfigurationSettingException(string message, Exception innerException) : base(message, innerException) { }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="info">Serialization information.</param>
        /// <param name="context">Streaming context.</param>
        protected MissingConfigurationSettingException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}

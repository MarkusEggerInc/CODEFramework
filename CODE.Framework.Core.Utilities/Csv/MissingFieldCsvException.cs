using System;
using System.Runtime.Serialization;

namespace CODE.Framework.Core.Utilities.Csv
{
    /// <summary>
    /// Represents the exception that is thrown when a there is a missing field in a record of the CSV file.
    /// </summary>
    /// <remarks>
    /// MissingFieldException would have been a better name, but there is already a <see cref="T:System.MissingFieldException"/>.
    /// </remarks>
    [Serializable()]
    public class MissingFieldCsvException : MalformedCsvException
    {
        /// <summary>
        /// Initializes a new instance of the MissingFieldCsvException class.
        /// </summary>
        public MissingFieldCsvException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the MissingFieldCsvException class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public MissingFieldCsvException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the MissingFieldCsvException class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public MissingFieldCsvException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the MissingFieldCsvException class.
        /// </summary>
        /// <param name="rawData">The raw data when the error occured.</param>
        /// <param name="currentPosition">The current position in the raw data.</param>
        /// <param name="currentRecordIndex">The current record index.</param>
        /// <param name="currentFieldIndex">The current field index.</param>
        public MissingFieldCsvException(string rawData, int currentPosition, long currentRecordIndex, int currentFieldIndex)
            : base(rawData, currentPosition, currentRecordIndex, currentFieldIndex)
        {
        }

        /// <summary>
        /// Initializes a new instance of the MissingFieldCsvException class.
        /// </summary>
        /// <param name="rawData">The raw data when the error occured.</param>
        /// <param name="currentPosition">The current position in the raw data.</param>
        /// <param name="currentRecordIndex">The current record index.</param>
        /// <param name="currentFieldIndex">The current field index.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public MissingFieldCsvException(string rawData, int currentPosition, long currentRecordIndex, int currentFieldIndex, Exception innerException)
            : base(rawData, currentPosition, currentRecordIndex, currentFieldIndex, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the MissingFieldCsvException class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="T:SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="T:StreamingContext"/> that contains contextual information about the source or destination.</param>
        protected MissingFieldCsvException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
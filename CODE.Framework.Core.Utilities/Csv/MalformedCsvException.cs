﻿using System;
using System.Globalization;
using System.Runtime.Serialization;

namespace CODE.Framework.Core.Utilities.Csv
{
    /// <summary>
    /// Represents the exception that is thrown when a CSV file is malformed.
    /// </summary>
    [Serializable()]
    public class MalformedCsvException : Exception
    {
        /// <summary>
        /// Contains the message that describes the error.
        /// </summary>
        private readonly string _message;

        /// <summary>
        /// Contains the raw data when the error occured.
        /// </summary>
        private readonly string _rawData;

        /// <summary>
        /// Contains the current field index.
        /// </summary>
        private readonly int _currentFieldIndex;

        /// <summary>
        /// Contains the current record index.
        /// </summary>
        private readonly long _currentRecordIndex;

        /// <summary>
        /// Contains the current position in the raw data.
        /// </summary>
        private readonly int _currentPosition;

        /// <summary>
        /// Initializes a new instance of the MalformedCsvException class.
        /// </summary>
        public MalformedCsvException() : this(null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the MalformedCsvException class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public MalformedCsvException(string message)
            : this(message, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the MalformedCsvException class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public MalformedCsvException(string message, Exception innerException)
            : base(String.Empty, innerException)
        {
            _message = (message == null ? string.Empty : message);

            _rawData = string.Empty;
            _currentPosition = -1;
            _currentRecordIndex = -1;
            _currentFieldIndex = -1;
        }

        /// <summary>
        /// Initializes a new instance of the MalformedCsvException class.
        /// </summary>
        /// <param name="rawData">The raw data when the error occured.</param>
        /// <param name="currentPosition">The current position in the raw data.</param>
        /// <param name="currentRecordIndex">The current record index.</param>
        /// <param name="currentFieldIndex">The current field index.</param>
        public MalformedCsvException(string rawData, int currentPosition, long currentRecordIndex, int currentFieldIndex)
            : this(rawData, currentPosition, currentRecordIndex, currentFieldIndex, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the MalformedCsvException class.
        /// </summary>
        /// <param name="rawData">The raw data when the error occured.</param>
        /// <param name="currentPosition">The current position in the raw data.</param>
        /// <param name="currentRecordIndex">The current record index.</param>
        /// <param name="currentFieldIndex">The current field index.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public MalformedCsvException(string rawData, int currentPosition, long currentRecordIndex, int currentFieldIndex, Exception innerException)
            : base(String.Empty, innerException)
        {
            _rawData = (rawData == null ? string.Empty : rawData);
            _currentPosition = currentPosition;
            _currentRecordIndex = currentRecordIndex;
            _currentFieldIndex = currentFieldIndex;

            _message = String.Format(CultureInfo.InvariantCulture, Properties.Resources.MalformedCsvException, _currentRecordIndex, _currentFieldIndex, _currentPosition, _rawData);
        }

        /// <summary>
        /// Initializes a new instance of the MalformedCsvException class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="T:SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="T:StreamingContext"/> that contains contextual information about the source or destination.</param>
        protected MalformedCsvException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            _message = info.GetString("MyMessage");

            _rawData = info.GetString("RawData");
            _currentPosition = info.GetInt32("CurrentPosition");
            _currentRecordIndex = info.GetInt64("CurrentRecordIndex");
            _currentFieldIndex = info.GetInt32("CurrentFieldIndex");
        }

        /// <summary>
        /// Gets the raw data when the error occured.
        /// </summary>
        /// <value>The raw data when the error occured.</value>
        public string RawData
        {
            get { return _rawData; }
        }

        /// <summary>
        /// Gets the current position in the raw data.
        /// </summary>
        /// <value>The current position in the raw data.</value>
        public int CurrentPosition
        {
            get { return _currentPosition; }
        }

        /// <summary>
        /// Gets the current record index.
        /// </summary>
        /// <value>The current record index.</value>
        public long CurrentRecordIndex
        {
            get { return _currentRecordIndex; }
        }

        /// <summary>
        /// Gets the current field index.
        /// </summary>
        /// <value>The current record index.</value>
        public int CurrentFieldIndex
        {
            get { return _currentFieldIndex; }
        }

        /// <summary>
        /// Gets a message that describes the current exception.
        /// </summary>
        /// <value>A message that describes the current exception.</value>
        public override string Message
        {
            get { return _message; }
        }

        /// <summary>
        /// When overridden in a derived class, sets the <see cref="T:SerializationInfo"/> with information about the exception.
        /// </summary>
        /// <param name="info">The <see cref="T:SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="T:StreamingContext"/> that contains contextual information about the source or destination.</param>
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
        {
            base.GetObjectData(info, context);

            info.AddValue("MyMessage", _message);

            info.AddValue("RawData", _rawData);
            info.AddValue("CurrentPosition", _currentPosition);
            info.AddValue("CurrentRecordIndex", _currentRecordIndex);
            info.AddValue("CurrentFieldIndex", _currentFieldIndex);
        }
    }
}
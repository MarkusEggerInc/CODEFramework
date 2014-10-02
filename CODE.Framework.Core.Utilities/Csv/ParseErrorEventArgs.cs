using System;

namespace CODE.Framework.Core.Utilities.Csv
{
    /// <summary>
    /// Provides data for the <see cref="M:CsvReader.ParseError"/> event.
    /// </summary>
    public class ParseErrorEventArgs
        : EventArgs
    {
        #region Fields

        /// <summary>
        /// Contains the error that occured.
        /// </summary>
        private MalformedCsvException _error;

        /// <summary>
        /// Contains the action to take.
        /// </summary>
        private ParseErrorAction _action;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the ParseErrorEventArgs class.
        /// </summary>
        /// <param name="error">The error that occured.</param>
        /// <param name="defaultAction">The default action to take.</param>
        public ParseErrorEventArgs(MalformedCsvException error, ParseErrorAction defaultAction)
            : base()
        {
            _error = error;
            _action = defaultAction;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the error that occured.
        /// </summary>
        /// <value>The error that occured.</value>
        public MalformedCsvException Error
        {
            get { return _error; }
        }

        /// <summary>
        /// Gets or sets the action to take.
        /// </summary>
        /// <value>The action to take.</value>
        public ParseErrorAction Action
        {
            get { return _action; }
            set { _action = value; }
        }

        #endregion
    }
}
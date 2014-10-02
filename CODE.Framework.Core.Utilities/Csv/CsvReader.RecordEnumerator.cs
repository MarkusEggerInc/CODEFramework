﻿using System;
using System.Collections;
using System.Collections.Generic;

namespace CODE.Framework.Core.Utilities.Csv
{
    public partial class CsvReader
    {
        /// <summary>
        /// Supports a simple iteration over the records of a <see cref="T:CsvReader"/>.
        /// </summary>
        public struct RecordEnumerator : IEnumerator<string[]>
        {
            /// <summary>
            /// Contains the enumerated <see cref="T:CsvReader"/>.
            /// </summary>
            private CsvReader _reader;

            /// <summary>
            /// Contains the current record.
            /// </summary>
            private string[] _current;

            /// <summary>
            /// Contains the current record index.
            /// </summary>
            private long _currentRecordIndex;

            /// <summary>
            /// Initializes a new instance of the <see cref="T:RecordEnumerator"/> class.
            /// </summary>
            /// <param name="reader">The <see cref="T:CsvReader"/> to iterate over.</param>
            /// <exception cref="T:ArgumentNullException">
            ///		<paramref name="reader"/> is a <see langword="null"/>.
            /// </exception>
            public RecordEnumerator(CsvReader reader)
            {
                if (reader == null)
                    throw new ArgumentNullException("reader");

                _reader = reader;
                _current = null;

                _currentRecordIndex = reader._currentRecordIndex;
            }

            /// <summary>
            /// Gets the current record.
            /// </summary>
            public string[] Current
            {
                get { return _current; }
            }

            /// <summary>
            /// Advances the enumerator to the next record of the CSV.
            /// </summary>
            /// <returns><see langword="true"/> if the enumerator was successfully advanced to the next record, <see langword="false"/> if the enumerator has passed the end of the CSV.</returns>
            public bool MoveNext()
            {
                if (_reader._currentRecordIndex != _currentRecordIndex)
                    throw new InvalidOperationException(Properties.Resources.EnumerationVersionCheckFailed);

                if (_reader.ReadNextRecord())
                {
                    _current = new string[_reader._fieldCount];

                    _reader.CopyCurrentRecordTo(_current);
                    _currentRecordIndex = _reader._currentRecordIndex;

                    return true;
                }
                _current = null;
                _currentRecordIndex = _reader._currentRecordIndex;

                return false;
            }

            /// <summary>
            /// Sets the enumerator to its initial position, which is before the first record in the CSV.
            /// </summary>
            public void Reset()
            {
                if (_reader._currentRecordIndex != _currentRecordIndex)
                    throw new InvalidOperationException(Properties.Resources.EnumerationVersionCheckFailed);

                _reader.MoveTo(-1);

                _current = null;
                _currentRecordIndex = _reader._currentRecordIndex;
            }

            /// <summary>
            /// Gets the current record.
            /// </summary>
            object IEnumerator.Current
            {
                get
                {
                    if (_reader._currentRecordIndex != _currentRecordIndex)
                        throw new InvalidOperationException(Properties.Resources.EnumerationVersionCheckFailed);

                    return Current;
                }
            }

            /// <summary>
            /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
            /// </summary>
            public void Dispose()
            {
                _reader = null;
                _current = null;
            }
        }
    }
}
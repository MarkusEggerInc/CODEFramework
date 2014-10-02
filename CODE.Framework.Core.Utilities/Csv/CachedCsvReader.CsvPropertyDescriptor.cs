using System;
using System.ComponentModel;

namespace CODE.Framework.Core.Utilities.Csv
{
    public partial class CachedCsvReader
    {
        /// <summary>
        /// Represents a CSV field property descriptor.
        /// </summary>
        private class CsvPropertyDescriptor : PropertyDescriptor
        {
            /// <summary>
            /// Contains the field index.
            /// </summary>
            private readonly int _index;

            /// <summary>
            /// Initializes a new instance of the CsvPropertyDescriptor class.
            /// </summary>
            /// <param name="fieldName">The field name.</param>
            /// <param name="index">The field index.</param>
            public CsvPropertyDescriptor(string fieldName, int index) : base(fieldName, null)
            {
                _index = index;
            }

            /// <summary>
            /// Gets the field index.
            /// </summary>
            /// <value>The field index.</value>
            public int Index
            {
                get { return _index; }
            }

            public override bool CanResetValue(object component)
            {
                return false;
            }

            public override object GetValue(object component)
            {
                return ((string[])component)[_index];
            }

            public override void ResetValue(object component)
            {
            }

            public override void SetValue(object component, object value)
            {
            }

            public override bool ShouldSerializeValue(object component)
            {
                return false;
            }

            public override Type ComponentType
            {
                get { return typeof (CachedCsvReader); }
            }

            public override bool IsReadOnly
            {
                get { return true; }
            }

            public override Type PropertyType
            {
                get { return typeof (string); }
            }
        }
    }
}

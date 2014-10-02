using System;
using System.Data;
using System.Text;
using CODE.Framework.Core.Utilities.Csv;
using System.IO;

namespace CODE.Framework.Core.Utilities
{
    /// <summary>
    /// This class provides a number of methods that help with a number of standard data tasks.
    /// </summary>
    public static class DataHelper
    {
        /// <summary>
        /// This method takes a csv string (comma separated) and turns into a DataTable.
        /// </summary>
        /// <param name="csvString">The CSV string (comma separated).</param>
        /// <param name="tableName">Name of the table.</param>
        /// <returns></returns>
        public static DataTable CsvToTable(string csvString, string tableName)
        {
            var dtResult = new DataTable();
            dtResult.TableName = tableName;

            using (var csv = new CachedCsvReader(new StringReader(csvString), true))
            {
                string[] headers = csv.GetFieldHeaders();

                foreach (string header in headers)
                    dtResult.Columns.Add(header, typeof (string));

                while (csv.ReadNextRecord())
                {
                    var newRow = dtResult.NewRow();
                    for (int columnNumber = 0; columnNumber < headers.GetLongLength(0); columnNumber++)
                        newRow[columnNumber] = csv[columnNumber];
                    dtResult.Rows.Add(newRow);
                }
            }

            return dtResult;
        }

        /// <summary>
        /// This method takes a data table and turns all its contents into a CSV formatted string.
        /// </summary>
        /// <param name="table">Data Table</param>
        /// <returns>CSV String</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1705:LongAcronymsShouldBePascalCased", MessageId = "Member", Justification = "This is a correct 3la")]
        public static string TableToCsv(DataTable table)
        {
            var sb = new StringBuilder();

            // We create the header record
            for (int counter = 0; counter < table.Columns.Count; counter++)
            {
                // We separate all but the first field by a comma
                if (counter > 0) { sb.Append(","); }
                // We add the field name
                sb.Append(table.Columns[counter].ColumnName.Trim());
            }
            sb.Append("\r\n");

            // We iterate over all rows, and all fields/columns
            for (int counter = 0; counter < table.Rows.Count; counter++)
            {
                var row = table.Rows[counter];
                for (int counter2 = 0; counter2 < table.Columns.Count; counter2++)
                {
                    string field;
                    field = row[counter2].ToString().Trim().Replace("\"", "\"\"");
                    if (field.IndexOf(",") > 0 || field.IndexOf("\"") > 0 || field.IndexOf("\r") > 0 || field.IndexOf("\n") > 0)
                        field = "\"" + field + "\"";

                    // For all fields but the first, we need to add a comma-separator
                    if (counter2 > 0) { sb.Append(","); }

                    // We add the field value to the output stream.
                    sb.Append(field);
                }
                // End of record. We add a line feed
                sb.Append("\r\n");
            }

            return sb.ToString();
        }

        /// <summary>
        /// This method takes a data view and turns all its contents into a CSV formatted string.
        /// </summary>
        /// <param name="view">Data View</param>
        /// <returns>CSV String</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1705:LongAcronymsShouldBePascalCased", MessageId = "Member", Justification = "This is a correct 3la")]
        public static string TableToCsv(DataView view)
        {
            return TableToCsv(view.Table);
        }

        /// <summary>
        /// Safely converts a value into a Guid or returns Guid.Empty if the value is invalid.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>Guid</returns>
        /// <remarks>
        /// This method is an extension method
        /// </remarks>
        /// <example>
        /// using EPS.Utilities;
        /// 
        /// // more code here
        /// 
        /// Guid myGuid = dataSet.Tables[0].Rows[0]["id"].ToGuidSave();
        /// </example>
        public static Guid ToGuidSafe(this object value)
        {
            try
            {
                if (value != DBNull.Value)
                    return (Guid)value;
                return Guid.Empty;
            }
            catch
            {
                return Guid.Empty;
            }
        }

        /// <summary>
        /// Safely converts a value into a string or returns string.Empty if the value is invalid.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        /// <remarks>
        /// This method is an extension method
        /// </remarks>
        /// <example>
        /// using EPS.Utilities;
        /// 
        /// // more code here
        /// 
        /// string myString = dataSet.Tables[0].Rows[0]["name"].ToStringSave();
        /// </example>
        public static string ToStringSafe(this object value)
        {
            try
            {
                if (value != DBNull.Value)
                    return value.ToString();
                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Safely converts a value into a boolean or returns false if the value is invalid.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        /// <remarks>
        /// This method is an extension method
        /// </remarks>
        /// <example>
        /// using EPS.Utilities;
        /// 
        /// // more code here
        /// 
        /// bool myBool = dataSet.Tables[0].Rows[0]["active"].ToBooleanSave();
        /// </example>
        public static bool ToBooleanSafe(this object value)
        {
            try
            {
                if (value != DBNull.Value)
                    return (bool)value;
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Safely converts a value into a DateTime or returns DateTime.MinValue if the value is invalid.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        /// <remarks>
        /// This method is an extension method
        /// </remarks>
        /// <example>
        /// using EPS.Utilities;
        /// 
        /// // more code here
        /// 
        /// DateTime myDate = dataSet.Tables[0].Rows[0]["timeStamp"].ToDateTimeSave();
        /// </example>
        public static DateTime ToDateTimeSafe(this object value)
        {
            try
            {
                if (value != DBNull.Value)
                    return (DateTime)value;
                return DateTime.MinValue;
            }
            catch
            {
                return DateTime.MinValue;
            }
        }

        /// <summary>
        /// Safely converts a value into an integer or returns 0 if the value is invalid.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        /// <remarks>
        /// This method is an extension method
        /// </remarks>
        /// <example>
        /// using EPS.Utilities;
        /// 
        /// // more code here
        /// 
        /// int myInt = dataSet.Tables[0].Rows[0]["number"].ToIntegerSave();
        /// </example>
        public static int ToIntegerSafe(this object value)
        {
            try
            {
                if (value != DBNull.Value)
                    return (int)value;
                return 0;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Safely converts a value into a double or returns 0.0 if the value is invalid.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        /// <remarks>
        /// This method is an extension method
        /// </remarks>
        /// <example>
        /// using EPS.Utilities;
        /// 
        /// // more code here
        /// 
        /// int myDouble = dataSet.Tables[0].Rows[0]["number"].ToDoubleSave();
        /// </example>
        public static double ToDoubleSafe(this object value)
        {
            try
            {
                if (value != DBNull.Value)
                    return (double)value;
                return 0.0;
            }
            catch
            {
                return 0.0;
            }
        }

        /// <summary>
        /// Safely converts a value into a decimal or returns 0.0 if the value is invalid.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        /// <remarks>
        /// This method is an extension method
        /// </remarks>
        /// <example>
        /// using EPS.Utilities;
        /// 
        /// // more code here
        /// 
        /// decimal myDec = dataSet.Tables[0].Rows[0]["price"].ToDecimalSave();
        /// </example>
        public static decimal ToDecimalSafe(this object value)
        {
            try
            {
                if (value != DBNull.Value)
                    return (decimal)value;
                return 0m;
            }
            catch
            {
                return 0m;
            }
        }

        /// <summary>
        /// Safely converts a value into a char or returns ' ' if the value is invalid.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        /// <remarks>
        /// This method is an extension method
        /// </remarks>
        /// <example>
        /// using EPS.Utilities;
        /// 
        /// // more code here
        /// 
        /// char myChar = dataSet.Tables[0].Rows[0]["character"].ToCharSave();
        /// </example>
        public static char ToCharSafe(this object value)
        {
            try
            {
                if (value != DBNull.Value)
                    return (char)value;
                return ' ';
            }
            catch
            {
                return ' ';
            }
        }

        /// <summary>
        /// Safely converts a value into a byte array or returns an empty byte array if the value is invalid.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        /// <remarks>
        /// This method is an extension method
        /// </remarks>
        /// <example>
        /// using EPS.Utilities;
        /// 
        /// // more code here
        /// 
        /// byte[] myBytes = dataSet.Tables[0].Rows[0]["image"].ToByteArraySave();
        /// </example>
        public static byte[] ToByteArraySafe(this object value)
        {
            try
            {
                if (value != DBNull.Value)
                    return (byte[])value;
                return new byte[0];
            }
            catch
            {
                return new byte[0];
            }
        }
    }
}

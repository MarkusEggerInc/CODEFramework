﻿#region License
// Copyright (c) 2007 James Newton-King
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
#endregion

using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using CODE.Framework.Core.Newtonsoft.Serialization;
using CODE.Framework.Core.Newtonsoft.Utilities;

namespace CODE.Framework.Core.Newtonsoft.Converters
{
    /// <summary>
    /// Converts a <see cref="DataTable"/> to and from JSON.
    /// </summary>
    public class DataTableConverter : JsonConverter
    {
        /// <summary>
        /// Writes the JSON representation of the object.
        /// </summary>
        /// <param name="writer">The <see cref="JsonWriter"/> to write to.</param>
        /// <param name="value">The value.</param>
        /// <param name="serializer">The calling serializer.</param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var table = (DataTable)value;
            var resolver = serializer.ContractResolver as DefaultContractResolver;

            writer.WriteStartArray();

            foreach (DataRow row in table.Rows)
            {
                writer.WriteStartObject();
                foreach (DataColumn column in row.Table.Columns)
                {
                    var columnValue = row[column];

                    if (serializer.NullValueHandling == NullValueHandling.Ignore && (columnValue == null || columnValue == DBNull.Value))
                        continue;

                    writer.WritePropertyName((resolver != null) ? resolver.GetResolvedPropertyName(column.ColumnName) : column.ColumnName);
                    serializer.Serialize(writer, columnValue);
                }
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        }

        /// <summary>
        /// Reads the JSON representation of the object.
        /// </summary>
        /// <param name="reader">The <see cref="JsonReader"/> to read from.</param>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="existingValue">The existing value of object being read.</param>
        /// <param name="serializer">The calling serializer.</param>
        /// <returns>The object value.</returns>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var dt = existingValue as DataTable ?? ((objectType == typeof(DataTable)) ? new DataTable() : (DataTable)Activator.CreateInstance(objectType));

            if (reader.TokenType == JsonToken.PropertyName)
            {
                dt.TableName = (string)reader.Value;
                CheckedRead(reader);
            }

            if (reader.TokenType != JsonToken.StartArray)
                throw JsonSerializationException.Create(reader, "Unexpected JSON token when reading DataTable. Expected StartArray, got {0}.".FormatWith(CultureInfo.InvariantCulture, reader.TokenType));

            CheckedRead(reader);

            while (reader.TokenType != JsonToken.EndArray)
            {
                CreateRow(reader, dt, serializer);
                CheckedRead(reader);
            }

            return dt;
        }

        private static void CreateRow(JsonReader reader, DataTable dt, JsonSerializer serializer)
        {
            var dr = dt.NewRow();
            CheckedRead(reader);

            while (reader.TokenType == JsonToken.PropertyName)
            {
                var columnName = (string)reader.Value;

                CheckedRead(reader);

                if (columnName != null)
                {
                    var column = dt.Columns[columnName];
                    if (column == null)
                    {
                        var columnType = GetColumnDataType(reader);
                        column = new DataColumn(columnName, columnType);
                        dt.Columns.Add(column);
                    }

                    if (column.DataType == typeof(DataTable))
                    {
                        if (reader.TokenType == JsonToken.StartArray)
                            CheckedRead(reader);

                        var nestedDt = new DataTable();

                        while (reader.TokenType != JsonToken.EndArray)
                        {
                            CreateRow(reader, nestedDt, serializer);

                            CheckedRead(reader);
                        }

                        dr[columnName] = nestedDt;
                    }
                    else if (column.DataType.IsArray && column.DataType != typeof(byte[]))
                    {
                        if (reader.TokenType == JsonToken.StartArray)
                            CheckedRead(reader);

                        var o = new List<object>();

                        while (reader.TokenType != JsonToken.EndArray)
                        {
                            o.Add(reader.Value);
                            CheckedRead(reader);
                        }

                        var destinationArray = Array.CreateInstance(column.DataType.GetElementType(), o.Count);
                        Array.Copy(o.ToArray(), destinationArray, o.Count);

                        dr[columnName] = destinationArray;
                    }
                    else
                    {
                        dr[columnName] = (reader.Value != null) ? serializer.Deserialize(reader, column.DataType) : DBNull.Value;
                    }
                }

                CheckedRead(reader);
            }

            dr.EndEdit();
            dt.Rows.Add(dr);
        }

        private static Type GetColumnDataType(JsonReader reader)
        {
            var tokenType = reader.TokenType;

            switch (tokenType)
            {
                case JsonToken.Integer:
                case JsonToken.Boolean:
                case JsonToken.Float:
                case JsonToken.String:
                case JsonToken.Date:
                case JsonToken.Bytes:
                    return reader.ValueType;
                case JsonToken.Null:
                case JsonToken.Undefined:
                    return typeof(string);
                case JsonToken.StartArray:
                    CheckedRead(reader);
                    if (reader.TokenType == JsonToken.StartObject)
                        return typeof(DataTable); // nested datatable

                    var arrayType = GetColumnDataType(reader);
                    return arrayType.MakeArrayType();
                default:
                    throw JsonSerializationException.Create(reader, "Unexpected JSON token when reading DataTable: {0}".FormatWith(CultureInfo.InvariantCulture, tokenType));
            }
        }

        private static void CheckedRead(JsonReader reader)
        {
            if (!reader.Read())
                throw JsonSerializationException.Create(reader, "Unexpected end when reading DataTable.");
        }

        /// <summary>
        /// Determines whether this instance can convert the specified value type.
        /// </summary>
        /// <param name="valueType">Type of the value.</param>
        /// <returns>
        /// 	<c>true</c> if this instance can convert the specified value type; otherwise, <c>false</c>.
        /// </returns>
        public override bool CanConvert(Type valueType)
        {
            return typeof(DataTable).IsAssignableFrom(valueType);
        }
    }
}
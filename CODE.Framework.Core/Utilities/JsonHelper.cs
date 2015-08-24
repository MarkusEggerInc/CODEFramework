using System;
using System.Text;
using CODE.Framework.Core.Newtonsoft;
using CODE.Framework.Core.Utilities.Extensions;

namespace CODE.Framework.Core.Utilities
{
    /// <summary>
    /// This class provides useful helper functionality to deal with JSON strings
    /// </summary>
    public static class JsonHelper
    {
        /// <summary>
        /// Serializes to REST JSON.
        /// </summary>
        /// <param name="objectToSerialize">The object to serialize.</param>
        /// <returns>JSON string</returns>
        public static string SerializeToRestJson(object objectToSerialize)
        {
            try
            {
                return JsonConvert.SerializeObject(objectToSerialize);
            }
            catch (Exception ex)
            {
                return ExceptionHelper.GetExceptionText(ex);
            }
        }

        /// <summary>
        /// Deserializes from REST JSON.
        /// </summary>
        /// <typeparam name="T">Type to return</typeparam>
        /// <param name="json">The json.</param>
        /// <returns>Deserialized object</returns>
        public static T DeserializeFromRestJson<T>(string json) where T: class, new()
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Deserializes from REST JSON.
        /// </summary>
        /// <param name="json">The json.</param>
        /// <param name="returnType">Type of the return.</param>
        /// <returns>Deserialized object</returns>
        public static object DeserializeFromRestJson(string json, Type returnType)
        {
            try
            {
                return JsonConvert.DeserializeObject(json, returnType);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Formats the provided JSON string (adds line feeds, indents, ...)
        /// </summary>
        /// <param name="json">The original JSON string.</param>
        /// <returns>Formatted JSON string</returns>
        public static string Format(string json)
        {
            var jsonBytes = Encoding.UTF8.GetBytes(json);
            var sb = new StringBuilder();

            var currentIndentLevel = 0;
            var inQuotes = false;
            var lastQuoteChar = ' ';
            foreach (var jsonByte in jsonBytes)
            {
                switch (jsonByte)
                {
                    case (byte)',':
                        sb.Append(',');
                        if (!inQuotes)
                        {
                            sb.Append("\r\n");
                            sb.Append(" ".Replicate(currentIndentLevel * 4));
                        }
                        break;
                    case (byte)'"':
                        if (!inQuotes)
                        {
                            lastQuoteChar = '"';
                            inQuotes = true;
                        }
                        else if (lastQuoteChar == '"')
                            inQuotes = false;
                        sb.Append('"');
                        break;
                    case (byte)'\'':
                        if (!inQuotes)
                        {
                            lastQuoteChar = '\'';
                            inQuotes = true;
                        }
                        else if (lastQuoteChar == '\'')
                            inQuotes = false;
                        sb.Append('\'');
                        break;
                    case (byte)'{':
                        sb.Append("{\r\n");
                        currentIndentLevel++;
                        sb.Append(" ".Replicate(currentIndentLevel * 4));
                        break;
                    case (byte)'}':
                        sb.Append("\r\n");
                        currentIndentLevel--;
                        sb.Append(" ".Replicate(currentIndentLevel * 4));
                        sb.Append('}');
                        break;
                    default:
                        sb.Append((char)jsonByte);
                        break;
                }
            }

            return sb.ToString();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using CODE.Framework.Core.Newtonsoft;
using CODE.Framework.Core.Newtonsoft.Serialization;
using CODE.Framework.Core.Utilities.Extensions;

namespace CODE.Framework.Core.Utilities
{
    /// <summary>
    /// This class provides useful helper functionality to deal with JSON strings
    /// </summary>
    public static class JsonHelper
    {
        /// <summary>
        /// Returns a name/value pair in JSON format
        /// </summary>
        /// <param name="elementName">Name of the element.</param>
        /// <param name="value">The value.</param>
        /// <returns>JSON snippet</returns>
        public static string GetJsonNameValuePair(string elementName, object value)
        {
            var json = JsonConvert.SerializeObject(new Dictionary<string, object> {{elementName, value}});
            if (json.StartsWith("{")) json = json.Substring(1);
            if (json.EndsWith("}")) json = json.Substring(0, json.Length - 1);
            return json;
        }

        /// <summary>
        /// Serializes to REST JSON.
        /// </summary>
        /// <param name="objectToSerialize">The object to serialize.</param>
        /// <param name="forceCamelCase">If set to true, the result will be forced to camelCase property names, regardless of the actual property names.</param>
        /// <returns>
        /// JSON string
        /// </returns>
        public static string SerializeToRestJson(object objectToSerialize, bool forceCamelCase = false)
        {
            try
            {
                if (!forceCamelCase)
                    return JsonConvert.SerializeObject(objectToSerialize);

                return JsonConvert.SerializeObject(objectToSerialize,
                    new JsonSerializerSettings
                    {
                        ContractResolver = new CamelCasePropertyNamesContractResolver()
                    });
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
        public static T DeserializeFromRestJson<T>(string json) where T : class, new()
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
        /// <param name="indentSpaces">Defines how many spaces are to be used for indentation.</param>
        /// <returns>Formatted JSON string</returns>
        public static string Format(string json, int indentSpaces = 2)
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
                    case (byte) ',':
                        sb.Append(',');
                        if (!inQuotes)
                        {
                            sb.Append("\r\n");
                            sb.Append(" ".Replicate(currentIndentLevel*indentSpaces));
                        }
                        break;
                    case (byte) '"':
                        if (!inQuotes)
                        {
                            lastQuoteChar = '"';
                            inQuotes = true;
                        }
                        else if (lastQuoteChar == '"')
                            inQuotes = false;
                        sb.Append('"');
                        break;
                    case (byte) '\'':
                        if (!inQuotes)
                        {
                            lastQuoteChar = '\'';
                            inQuotes = true;
                        }
                        else if (lastQuoteChar == '\'')
                            inQuotes = false;
                        sb.Append('\'');
                        break;
                    case (byte) '{':
                        sb.Append("{\r\n");
                        currentIndentLevel++;
                        sb.Append(" ".Replicate(currentIndentLevel*indentSpaces));
                        break;
                    case (byte) '}':
                        sb.Append("\r\n");
                        currentIndentLevel--;
                        sb.Append(" ".Replicate(currentIndentLevel*indentSpaces));
                        sb.Append('}');
                        break;
                    default:
                        sb.Append((char) jsonByte);
                        break;
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Performs a quick element by element parse of a flat JSON string 
        /// and invokes the callback for each value found.
        /// </summary>
        /// <param name="json">The JSON string that is to be parsed.</param>
        /// <param name="callback">The callback that gets called for each name/value pair found.</param>
        /// <example>
        /// var json = "{ 'value': 'x', 'value2': 'y' }";
        /// JsonHelper.QuicklParse(json, (n, v) => {
        ///     Console.WriteLine("Name: " + n);
        ///     Console.WriteLine("Value: " + v);
        /// });
        /// </example>
        public static void QuickParse(string json, Action<string, string> callback)
        {
            if (string.IsNullOrEmpty(json)) return;
            var dictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
            foreach (var key in dictionary.Keys)
                callback(key, dictionary[key]);
        }
    }

    /// <summary>
    /// This class helps in manually building simple JSON
    /// </summary>
    public class JsonBuilder
    {
        private readonly StringBuilder _sb;
        private bool _firstElement = true;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonBuilder"/> class.
        /// </summary>
        public JsonBuilder()
        {
            _sb = new StringBuilder();
        }

        /// <summary>
        /// Appends the specified name/value pair.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        public void Append(string name, object value)
        {
            if (!_firstElement) _sb.Append(",");
            _sb.Append(JsonHelper.GetJsonNameValuePair(name, value));
            _firstElement = false;
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        public override string ToString()
        {
            var json = "{" + _sb + "}";
            json = JsonHelper.Format(json);
            return json;
        }
    }
}

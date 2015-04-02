using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using CODE.Framework.Core.Utilities;
using CODE.Framework.Services.Contracts;

namespace CODE.Framework.Services.Client
{
    /// <summary>
    /// Helper functionality needed for REST operations
    /// </summary>
    public static class RestHelper
    {
        /// <summary>
        /// Inspects the specified method in the contract for special configuration to see what the REST-exposed method name is supposed to be
        /// </summary>
        /// <param name="actualMethodName">Actual name of the method.</param>
        /// <param name="httpMethod">The HTTP method.</param>
        /// <param name="contractType">Service contract type.</param>
        /// <returns>REST-exposed name of the method</returns>
        public static string GetExposedMethodNameFromContract(string actualMethodName, string httpMethod, Type contractType)
        {
            var methods = ObjectHelper.GetAllMethodsForInterface(contractType).Where(m => m.Name == actualMethodName).ToList();
            foreach (var method in methods)
            {
                var restAttribute = GetRestAttribute(method);
                if (string.Equals(restAttribute.Method.ToString(), httpMethod, StringComparison.OrdinalIgnoreCase))
                {
                    if (restAttribute.Name == null) return method.Name;
                    if (restAttribute.Name == string.Empty) return string.Empty;
                    return restAttribute.Name;
                }
            }
            return actualMethodName;
        }

        /// <summary>
        /// Returns the exposed HTTP-method/verb for the provided method
        /// </summary>
        /// <param name="methodName">Name of the method.</param>
        /// <param name="contractType">Service contract type.</param>
        /// <returns>HTTP Method/Verb</returns>
        public static string GetHttpMethodFromContract(string methodName, Type contractType)
        {
            var method = ObjectHelper.GetAllMethodsForInterface(contractType).FirstOrDefault(m => m.Name == methodName);
            return method == null ? "POST" : GetRestAttribute(method).Method.ToString().ToUpper();
        }

        /// <summary>
        /// Extracts the RestAttribute from a method's attributes
        /// </summary>
        /// <param name="method">The method to be inspected</param>
        /// <returns>The applied RestAttribute or a default RestAttribute.</returns>
        public static RestAttribute GetRestAttribute(MethodInfo method)
        {
            var customAttributes = method.GetCustomAttributes(typeof(RestAttribute), true);
            if (customAttributes.Length <= 0) return new RestAttribute();
            var restAttribute = customAttributes[0] as RestAttribute;
            return restAttribute ?? new RestAttribute();
        }

        /// <summary>
        /// Extracts the RestUrlParameterAttribute from a property's attributes
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>The applied RestUrlParameterAttribute or a default RestUrlParameterAttribute</returns>
        public static RestUrlParameterAttribute GetRestUrlParameterAttribute(PropertyInfo property)
        {
            var customAttributes = property.GetCustomAttributes(typeof(RestUrlParameterAttribute), true);
            if (customAttributes.Length <= 0) return new RestUrlParameterAttribute();
            var restAttribute = customAttributes[0] as RestUrlParameterAttribute;
            return restAttribute ?? new RestUrlParameterAttribute();
        }

        /// <summary>
        /// Gets a list of all properties that are to be used as inline parameters, sorted by their sequence
        /// </summary>
        /// <param name="contractType">Contract type</param>
        /// <returns>List of properties to be used as inline URL parameters</returns>
        public static List<PropertyInfo> GetOrderedInlinePropertyList(Type contractType)
        {
            var propertiesToSerialize = contractType.GetProperties();
            var inlineParameterProperties = new List<PropertySorter>();
            if (propertiesToSerialize.Length == 1) // If we only have one parameter, we always allow passing it as an inline parameter, unless it is specifically flagged as a named parameter
            {
                var parameterAttribute = GetRestUrlParameterAttribute(propertiesToSerialize[0]);
                if (parameterAttribute == null || parameterAttribute.Mode == UrlParameterMode.Inline)
                    inlineParameterProperties.Add(new PropertySorter { Property = propertiesToSerialize[0] });
            }
            else
                foreach (var property in propertiesToSerialize)
                {
                    var parameterAttribute = GetRestUrlParameterAttribute(property);
                    if (parameterAttribute != null && parameterAttribute.Mode == UrlParameterMode.Inline)
                        inlineParameterProperties.Add(new PropertySorter { Sequence = parameterAttribute.Sequence, Property = property });
                }
            return inlineParameterProperties.OrderBy(inline => inline.Sequence).Select(inline => inline.Property).ToList();
        }

        /// <summary>
        /// Returns a list of all properties of the provided object that are NOT flagged to be used as inline URL parameters
        /// </summary>
        /// <param name="contractType">Contract type</param>
        /// <returns>List of named properties</returns>
        public static List<PropertyInfo> GetNamedPropertyList(Type contractType)
        {
            var propertiesToSerialize = contractType.GetProperties();
            var properties = new List<PropertyInfo>();
            if (propertiesToSerialize.Length == 1) // If there is only one property, we allow passing it as a named parameter, unless it is specifically flagged with a mode
            {
                var parameterAttribute = GetRestUrlParameterAttribute(propertiesToSerialize[0]);
                if (parameterAttribute == null || parameterAttribute.Mode == UrlParameterMode.Named)
                    properties.Add(propertiesToSerialize[0]);
            }
            else
                foreach (var property in propertiesToSerialize)
                {
                    var parameterAttribute = GetRestUrlParameterAttribute(property);
                    if (parameterAttribute == null || parameterAttribute.Mode != UrlParameterMode.Named) continue;
                    properties.Add(property);
                }
            return properties;
        }

        /// <summary>
        /// Serializes an object to URL parameters
        /// </summary>
        /// <param name="objectToSerialize">The object to serialize.</param>
        /// <param name="httpMethod">The HTTP method.</param>
        /// <returns>System.String.</returns>
        /// <remarks>This is used for REST GET operatoins</remarks>
        public static string SerializeToUrlParameters(object objectToSerialize, string httpMethod = "GET")
        {
            var typeToSerialize = objectToSerialize.GetType();
            var inlineParameterProperties = GetOrderedInlinePropertyList(typeToSerialize);
            var namedParameterProperties = GetNamedPropertyList(typeToSerialize);

            var sb = new StringBuilder();
            foreach (var inlineProperty in inlineParameterProperties)
            {
                var propertyValue = inlineProperty.GetValue(objectToSerialize, null);
                sb.Append("/");
                if (propertyValue != null)
                    sb.Append(HttpHelper.UrlEncode(propertyValue.ToString())); // TODO: We need to make sure we are doing well for specific property types
            }
            if (httpMethod == "GET" && namedParameterProperties.Count > 0)
            {
                var isFirst = true;
                foreach (var namedProperty in namedParameterProperties)
                {
                    var propertyValue = namedProperty.GetValue(objectToSerialize, null);
                    if (propertyValue == null) continue;
                    if (isFirst) sb.Append("?");
                    if (!isFirst) sb.Append("&");
                    sb.Append(namedProperty.Name + "=" + HttpHelper.UrlEncode(propertyValue.ToString())); // TODO: We need to make sure we are doing well for specific property types
                    isFirst = false;
                }
            }

            return sb.ToString();
        }

        private class PropertySorter
        {
            public int Sequence { get; set; }
            public PropertyInfo Property { get; set; }
        }

        private static object ConvertValue(string value, Type propertyType)
        {
            if (propertyType == typeof(string)) return value; // Very likely case, so we handle this right away, even though we are also handling it below
            if (propertyType.IsEnum) return Enum.Parse(propertyType, value);
            if (propertyType == typeof(Guid)) return Guid.Parse(value);
            if (propertyType == typeof(bool)) return Convert.ToBoolean(value);
            if (propertyType == typeof(byte)) return Convert.ToByte(value);
            if (propertyType == typeof(char)) return Convert.ToChar(value);
            if (propertyType == typeof(DateTime)) return Convert.ToDateTime(value);
            if (propertyType == typeof(decimal)) return Convert.ToDecimal(value);
            if (propertyType == typeof(double)) return Convert.ToDouble(value);
            if (propertyType == typeof(Int16)) return Convert.ToInt16(value);
            if (propertyType == typeof(Int32)) return Convert.ToInt32(value);
            if (propertyType == typeof(Int64)) return Convert.ToInt64(value);
            if (propertyType == typeof(sbyte)) return Convert.ToSByte(value);
            if (propertyType == typeof(float)) return Convert.ToSingle(value);
            if (propertyType == typeof(UInt16)) return Convert.ToUInt16(value);
            if (propertyType == typeof(UInt32)) return Convert.ToUInt32(value);
            if (propertyType == typeof(UInt64)) return Convert.ToUInt64(value);
            return value;
        }
    }
}

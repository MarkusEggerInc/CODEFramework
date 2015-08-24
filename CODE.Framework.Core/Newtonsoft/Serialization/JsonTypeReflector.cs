#region License
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
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security;
using System.Security.Permissions;
using CODE.Framework.Core.Newtonsoft.Utilities;

namespace CODE.Framework.Core.Newtonsoft.Serialization
{
    internal static class JsonTypeReflector
    {
        private static bool? _dynamicCodeGeneration;
        private static bool? _fullyTrusted;

        public const string IdPropertyName = "$id";
        public const string RefPropertyName = "$ref";
        public const string TypePropertyName = "$type";
        public const string ValuePropertyName = "$value";
        public const string ArrayValuesPropertyName = "$values";

        public const string ShouldSerializePrefix = "ShouldSerialize";
        public const string SpecifiedPostfix = "Specified";

        private static readonly ThreadSafeStore<Type, Func<object[], JsonConverter>> JsonConverterCreatorCache = new ThreadSafeStore<Type, Func<object[], JsonConverter>>(GetJsonConverterCreator);

        private static readonly ThreadSafeStore<Type, Type> AssociatedMetadataTypesCache = new ThreadSafeStore<Type, Type>(GetAssociateMetadataTypeFromAttribute);
        private static ReflectionObject _metadataTypeAttributeReflectionObject;

        public static T GetCachedAttribute<T>(object attributeProvider) where T : Attribute
        {
            return CachedAttributeGetter<T>.GetAttribute(attributeProvider);
        }

        public static DataContractAttribute GetDataContractAttribute(Type type)
        {
            // DataContractAttribute does not have inheritance
            var currentType = type;

            while (currentType != null)
            {
                var result = CachedAttributeGetter<DataContractAttribute>.GetAttribute(currentType);
                if (result != null) return result;
                currentType = currentType.BaseType();
            }

            return null;
        }

        public static DataMemberAttribute GetDataMemberAttribute(MemberInfo memberInfo)
        {
            // DataMemberAttribute does not have inheritance

            // can't override a field
            if (memberInfo.MemberType() == MemberTypes.Field) return CachedAttributeGetter<DataMemberAttribute>.GetAttribute(memberInfo);

            // search property and then search base properties if nothing is returned and the property is virtual
            var propertyInfo = (PropertyInfo)memberInfo;
            var result = CachedAttributeGetter<DataMemberAttribute>.GetAttribute(propertyInfo);
            if (result != null) return result;
            if (!propertyInfo.IsVirtual()) return null;
            var currentType = propertyInfo.DeclaringType;

            while (result == null && currentType != null)
            {
                var baseProperty = (PropertyInfo)ReflectionUtils.GetMemberInfoFromType(currentType, propertyInfo);
                if (baseProperty != null && baseProperty.IsVirtual())
                    result = CachedAttributeGetter<DataMemberAttribute>.GetAttribute(baseProperty);
                currentType = currentType.BaseType();
            }

            return result;
        }

        public static MemberSerialization GetObjectMemberSerialization(Type objectType, bool ignoreSerializableAttribute)
        {
            var objectAttribute = GetCachedAttribute<JsonObjectAttribute>(objectType);
            if (objectAttribute != null) return objectAttribute.MemberSerialization;

            var dataContractAttribute = GetDataContractAttribute(objectType);
            if (dataContractAttribute != null) return MemberSerialization.OptIn;

            if (ignoreSerializableAttribute) return MemberSerialization.OptOut;
            var serializableAttribute = GetCachedAttribute<SerializableAttribute>(objectType);
            return serializableAttribute != null ? MemberSerialization.Fields : MemberSerialization.OptOut;
        }

        public static JsonConverter GetJsonConverter(object attributeProvider)
        {
            var converterAttribute = GetCachedAttribute<JsonConverterAttribute>(attributeProvider);

            if (converterAttribute == null) return null;
            var creator = JsonConverterCreatorCache.Get(converterAttribute.ConverterType);
            return creator != null ? creator(converterAttribute.ConverterParameters) : null;
        }

        /// <summary>
        /// Lookup and create an instance of the JsonConverter type described by the argument.
        /// </summary>
        /// <param name="converterType">The JsonConverter type to create.</param>
        /// <param name="converterArgs">Optional arguments to pass to an initializing constructor of the JsonConverter.
        /// If null, the default constructor is used.</param>
        public static JsonConverter CreateJsonConverterInstance(Type converterType, object[] converterArgs)
        {
            var converterCreator = JsonConverterCreatorCache.Get(converterType);
            return converterCreator(converterArgs);
        }

        /// <summary>
        /// Create a factory function that can be used to create instances of a JsonConverter described by the 
        /// argument type.  The returned function can then be used to either invoke the converter's default ctor, or any 
        /// parameterized constructors by way of an object array.
        /// </summary>
        private static Func<object[], JsonConverter> GetJsonConverterCreator(Type converterType)
        {
            var defaultConstructor = (ReflectionUtils.HasDefaultConstructor(converterType, false)) ? ReflectionDelegateFactory.CreateDefaultConstructor<object>(converterType) : null;

            return parameters =>
            {
                try
                {
                    if (parameters != null)
                    {
                        var paramTypes = parameters.Select(param => param.GetType()).ToArray();
                        var parameterizedConstructorInfo = converterType.GetConstructor(paramTypes);

                        if (null != parameterizedConstructorInfo)
                        {
                            var parameterizedConstructor = ReflectionDelegateFactory.CreateParametrizedConstructor(parameterizedConstructorInfo);
                            return (JsonConverter)parameterizedConstructor(parameters);
                        }
                        throw new JsonException("No matching parameterized constructor found for '{0}'.".FormatWith(CultureInfo.InvariantCulture, converterType));
                    }

                    if (defaultConstructor == null) throw new JsonException("No parameterless constructor defined for '{0}'.".FormatWith(CultureInfo.InvariantCulture, converterType));

                    return (JsonConverter)defaultConstructor();
                }
                catch (Exception ex)
                {
                    throw new JsonException("Error creating '{0}'.".FormatWith(CultureInfo.InvariantCulture, converterType), ex);
                }
            };
        }

        public static TypeConverter GetTypeConverter(Type type)
        {
            return TypeDescriptor.GetConverter(type);
        }

        private static Type GetAssociatedMetadataType(Type type)
        {
            return AssociatedMetadataTypesCache.Get(type);
        }

        private static Type GetAssociateMetadataTypeFromAttribute(Type type)
        {
            var customAttributes = ReflectionUtils.GetAttributes(type, null, true);

            foreach (var attribute in customAttributes)
            {
                var attributeType = attribute.GetType();

                // only test on attribute type name
                // attribute assembly could change because of type forwarding, etc
                if (!string.Equals(attributeType.FullName, "System.ComponentModel.DataAnnotations.MetadataTypeAttribute", StringComparison.Ordinal)) continue;
                const string metadataClassTypeName = "MetadataClassType";

                if (_metadataTypeAttributeReflectionObject == null)
                    _metadataTypeAttributeReflectionObject = ReflectionObject.Create(attributeType, metadataClassTypeName);

                return (Type)_metadataTypeAttributeReflectionObject.GetValue(attribute, metadataClassTypeName);
            }

            return null;
        }

        private static T GetAttribute<T>(Type type) where T : Attribute
        {
            T attribute;

            var metadataType = GetAssociatedMetadataType(type);
            if (metadataType != null)
            {
                attribute = ReflectionUtils.GetAttribute<T>(metadataType, true);
                if (attribute != null) return attribute;
            }

            attribute = ReflectionUtils.GetAttribute<T>(type, true);
            if (attribute != null) return attribute;

            foreach (var typeInterface in type.GetInterfaces())
            {
                attribute = ReflectionUtils.GetAttribute<T>(typeInterface, true);
                if (attribute != null) return attribute;
            }

            return null;
        }

        private static T GetAttribute<T>(MemberInfo memberInfo) where T : Attribute
        {
            T attribute;

            var metadataType = GetAssociatedMetadataType(memberInfo.DeclaringType);
            if (metadataType != null)
            {
                var metadataTypeMemberInfo = ReflectionUtils.GetMemberInfoFromType(metadataType, memberInfo);

                if (metadataTypeMemberInfo != null)
                {
                    attribute = ReflectionUtils.GetAttribute<T>(metadataTypeMemberInfo, true);
                    if (attribute != null) return attribute;
                }
            }

            attribute = ReflectionUtils.GetAttribute<T>(memberInfo, true);
            if (attribute != null) return attribute;

            if (memberInfo.DeclaringType == null) return null;
            foreach (var typeInterface in memberInfo.DeclaringType.GetInterfaces())
            {
                var interfaceTypeMemberInfo = ReflectionUtils.GetMemberInfoFromType(typeInterface, memberInfo);

                if (interfaceTypeMemberInfo == null) continue;
                attribute = ReflectionUtils.GetAttribute<T>(interfaceTypeMemberInfo, true);
                if (attribute != null) return attribute;
            }

            return null;
        }

        public static T GetAttribute<T>(object provider) where T : Attribute
        {
            var type = provider as Type;
            if (type != null) return GetAttribute<T>(type);

            var memberInfo = provider as MemberInfo;
            return memberInfo != null ? GetAttribute<T>(memberInfo) : ReflectionUtils.GetAttribute<T>(provider, true);
        }

        public static bool DynamicCodeGeneration
        {
            [SecuritySafeCritical]
            get
            {
                if (_dynamicCodeGeneration != null) return _dynamicCodeGeneration.Value;
                try
                {
                    new ReflectionPermission(ReflectionPermissionFlag.MemberAccess).Demand();
                    new ReflectionPermission(ReflectionPermissionFlag.RestrictedMemberAccess).Demand();
                    new SecurityPermission(SecurityPermissionFlag.SkipVerification).Demand();
                    new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Demand();
                    new SecurityPermission(PermissionState.Unrestricted).Demand();
                    _dynamicCodeGeneration = true;
                }
                catch (Exception)
                {
                    _dynamicCodeGeneration = false;
                }

                return _dynamicCodeGeneration.Value;
            }
        }

        public static bool FullyTrusted
        {
            get
            {
                if (_fullyTrusted != null) return _fullyTrusted.Value;
                var appDomain = AppDomain.CurrentDomain;
                _fullyTrusted = appDomain.IsHomogenous && appDomain.IsFullyTrusted;
                return _fullyTrusted.Value;
            }
        }

        public static ReflectionDelegateFactory ReflectionDelegateFactory
        {
            get
            {
                return DynamicCodeGeneration ? DynamicReflectionDelegateFactory.Instance : LateBoundReflectionDelegateFactory.Instance;
            }
        }
    }
}
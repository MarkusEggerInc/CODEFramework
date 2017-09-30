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
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using CODE.Framework.Core.Newtonsoft.Serialization;

namespace CODE.Framework.Core.Newtonsoft.Utilities
{
    internal static class ReflectionUtils
    {
        public static readonly Type[] EmptyTypes;

        static ReflectionUtils()
        {
            EmptyTypes = Type.EmptyTypes;
        }

        public static bool IsVirtual(this PropertyInfo propertyInfo)
        {
            ValidationUtils.ArgumentNotNull(propertyInfo, nameof(propertyInfo));

            var m = propertyInfo.GetGetMethod(true);
            if (m != null && m.IsVirtual)
                return true;

            m = propertyInfo.GetSetMethod(true);
            if (m != null && m.IsVirtual)
                return true;

            return false;
        }

        public static MethodInfo GetBaseDefinition(this PropertyInfo propertyInfo)
        {
            ValidationUtils.ArgumentNotNull(propertyInfo, nameof(propertyInfo));

            var m = propertyInfo.GetGetMethod(true);
            if (m != null)
                return m.GetBaseDefinition();

            return propertyInfo.GetSetMethod(true)?.GetBaseDefinition();
        }

        public static bool IsPublic(PropertyInfo property)
        {
            if (property.GetGetMethod() != null && property.GetGetMethod().IsPublic)
                return true;
            if (property.GetSetMethod() != null && property.GetSetMethod().IsPublic)
                return true;

            return false;
        }

        public static Type GetObjectType(object v)
        {
            return v?.GetType();
        }

        public static string GetTypeName(Type t, TypeNameAssemblyFormatHandling assemblyFormat, ISerializationBinder binder)
        {
            var fullyQualifiedTypeName = GetFullyQualifiedTypeName(t, binder);

            switch (assemblyFormat)
            {
                case TypeNameAssemblyFormatHandling.Simple:
                    return RemoveAssemblyDetails(fullyQualifiedTypeName);
                case TypeNameAssemblyFormatHandling.Full:
                    return fullyQualifiedTypeName;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static string GetFullyQualifiedTypeName(Type t, ISerializationBinder binder)
        {
            if (binder != null)
            {
                string assemblyName;
                string typeName;
                binder.BindToName(t, out assemblyName, out typeName);
                return typeName + (assemblyName == null ? "" : ", " + assemblyName);
            }

            return t.AssemblyQualifiedName;
        }

        private static string RemoveAssemblyDetails(string fullyQualifiedTypeName)
        {
            var builder = new StringBuilder();

            // loop through the type name and filter out qualified assembly details from nested type names
            var writingAssemblyName = false;
            var skippingAssemblyDetails = false;
            for (var i = 0; i < fullyQualifiedTypeName.Length; i++)
            {
                var current = fullyQualifiedTypeName[i];
                switch (current)
                {
                    case '[':
                        writingAssemblyName = false;
                        skippingAssemblyDetails = false;
                        builder.Append(current);
                        break;
                    case ']':
                        writingAssemblyName = false;
                        skippingAssemblyDetails = false;
                        builder.Append(current);
                        break;
                    case ',':
                        if (!writingAssemblyName)
                        {
                            writingAssemblyName = true;
                            builder.Append(current);
                        }
                        else
                        {
                            skippingAssemblyDetails = true;
                        }
                        break;
                    default:
                        if (!skippingAssemblyDetails)
                            builder.Append(current);
                        break;
                }
            }

            return builder.ToString();
        }

        public static bool HasDefaultConstructor(Type t, bool nonPublic)
        {
            ValidationUtils.ArgumentNotNull(t, nameof(t));

            if (t.IsValueType())
                return true;

            return GetDefaultConstructor(t, nonPublic) != null;
        }

        public static ConstructorInfo GetDefaultConstructor(Type t)
        {
            return GetDefaultConstructor(t, false);
        }

        public static ConstructorInfo GetDefaultConstructor(Type t, bool nonPublic)
        {
            BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public;
            if (nonPublic)
            {
                bindingFlags = bindingFlags | BindingFlags.NonPublic;
            }

            return t.GetConstructors(bindingFlags).SingleOrDefault(c => !c.GetParameters().Any());
        }

        public static bool IsNullable(Type t)
        {
            ValidationUtils.ArgumentNotNull(t, nameof(t));

            if (t.IsValueType())
                return IsNullableType(t);

            return true;
        }

        public static bool IsNullableType(Type t)
        {
            ValidationUtils.ArgumentNotNull(t, nameof(t));

            return t.IsGenericType() && t.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        public static Type EnsureNotNullableType(Type t)
        {
            return IsNullableType(t)
                ? Nullable.GetUnderlyingType(t)
                : t;
        }

        public static bool IsGenericDefinition(Type type, Type genericInterfaceDefinition)
        {
            if (!type.IsGenericType())
                return false;

            var t = type.GetGenericTypeDefinition();
            return t == genericInterfaceDefinition;
        }

        public static bool ImplementsGenericDefinition(Type type, Type genericInterfaceDefinition)
        {
            Type implementingType;
            return ImplementsGenericDefinition(type, genericInterfaceDefinition, out implementingType);
        }

        public static bool ImplementsGenericDefinition(Type type, Type genericInterfaceDefinition, out Type implementingType)
        {
            ValidationUtils.ArgumentNotNull(type, nameof(type));
            ValidationUtils.ArgumentNotNull(genericInterfaceDefinition, nameof(genericInterfaceDefinition));

            if (!genericInterfaceDefinition.IsInterface() || !genericInterfaceDefinition.IsGenericTypeDefinition())
                throw new ArgumentNullException("'{0}' is not a generic interface definition.".FormatWith(CultureInfo.InvariantCulture, genericInterfaceDefinition));

            if (type.IsInterface())
                if (type.IsGenericType())
                {
                    var interfaceDefinition = type.GetGenericTypeDefinition();

                    if (genericInterfaceDefinition == interfaceDefinition)
                    {
                        implementingType = type;
                        return true;
                    }
                }

            foreach (var i in type.GetInterfaces())
                if (i.IsGenericType())
                {
                    var interfaceDefinition = i.GetGenericTypeDefinition();

                    if (genericInterfaceDefinition == interfaceDefinition)
                    {
                        implementingType = i;
                        return true;
                    }
                }

            implementingType = null;
            return false;
        }

        public static bool InheritsGenericDefinition(Type type, Type genericClassDefinition)
        {
            Type implementingType;
            return InheritsGenericDefinition(type, genericClassDefinition, out implementingType);
        }

        public static bool InheritsGenericDefinition(Type type, Type genericClassDefinition, out Type implementingType)
        {
            ValidationUtils.ArgumentNotNull(type, nameof(type));
            ValidationUtils.ArgumentNotNull(genericClassDefinition, nameof(genericClassDefinition));

            if (!genericClassDefinition.IsClass() || !genericClassDefinition.IsGenericTypeDefinition())
                throw new ArgumentNullException("'{0}' is not a generic class definition.".FormatWith(CultureInfo.InvariantCulture, genericClassDefinition));

            return InheritsGenericDefinitionInternal(type, genericClassDefinition, out implementingType);
        }

        private static bool InheritsGenericDefinitionInternal(Type currentType, Type genericClassDefinition, out Type implementingType)
        {
            if (currentType.IsGenericType())
            {
                var currentGenericClassDefinition = currentType.GetGenericTypeDefinition();

                if (genericClassDefinition == currentGenericClassDefinition)
                {
                    implementingType = currentType;
                    return true;
                }
            }

            if (currentType.BaseType() == null)
            {
                implementingType = null;
                return false;
            }

            return InheritsGenericDefinitionInternal(currentType.BaseType(), genericClassDefinition, out implementingType);
        }

        /// <summary>
        ///     Gets the type of the typed collection's items.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The type of the typed collection's items.</returns>
        public static Type GetCollectionItemType(Type type)
        {
            ValidationUtils.ArgumentNotNull(type, nameof(type));
            Type genericListType;

            if (type.IsArray)
                return type.GetElementType();
            if (ImplementsGenericDefinition(type, typeof(IEnumerable<>), out genericListType))
            {
                if (genericListType.IsGenericTypeDefinition())
                    throw new Exception("Type {0} is not a collection.".FormatWith(CultureInfo.InvariantCulture, type));

                return genericListType.GetGenericArguments()[0];
            }
            if (typeof(IEnumerable).IsAssignableFrom(type))
                return null;

            throw new Exception("Type {0} is not a collection.".FormatWith(CultureInfo.InvariantCulture, type));
        }

        public static void GetDictionaryKeyValueTypes(Type dictionaryType, out Type keyType, out Type valueType)
        {
            ValidationUtils.ArgumentNotNull(dictionaryType, nameof(dictionaryType));

            Type genericDictionaryType;
            if (ImplementsGenericDefinition(dictionaryType, typeof(IDictionary<,>), out genericDictionaryType))
            {
                if (genericDictionaryType.IsGenericTypeDefinition())
                    throw new Exception("Type {0} is not a dictionary.".FormatWith(CultureInfo.InvariantCulture, dictionaryType));

                var dictionaryGenericArguments = genericDictionaryType.GetGenericArguments();

                keyType = dictionaryGenericArguments[0];
                valueType = dictionaryGenericArguments[1];
                return;
            }
            if (typeof(IDictionary).IsAssignableFrom(dictionaryType))
            {
                keyType = null;
                valueType = null;
                return;
            }

            throw new Exception("Type {0} is not a dictionary.".FormatWith(CultureInfo.InvariantCulture, dictionaryType));
        }

        /// <summary>
        ///     Gets the member's underlying type.
        /// </summary>
        /// <param name="member">The member.</param>
        /// <returns>The underlying type of the member.</returns>
        public static Type GetMemberUnderlyingType(MemberInfo member)
        {
            ValidationUtils.ArgumentNotNull(member, nameof(member));

            switch (member.MemberType())
            {
                case MemberTypes.Field:
                    return ((FieldInfo) member).FieldType;
                case MemberTypes.Property:
                    return ((PropertyInfo) member).PropertyType;
                case MemberTypes.Event:
                    return ((EventInfo) member).EventHandlerType;
                case MemberTypes.Method:
                    return ((MethodInfo) member).ReturnType;
                default:
                    throw new ArgumentException("MemberInfo must be of type FieldInfo, PropertyInfo, EventInfo or MethodInfo", nameof(member));
            }
        }

        /// <summary>
        ///     Determines whether the member is an indexed property.
        /// </summary>
        /// <param name="member">The member.</param>
        /// <returns>
        ///     <c>true</c> if the member is an indexed property; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsIndexedProperty(MemberInfo member)
        {
            ValidationUtils.ArgumentNotNull(member, nameof(member));

            var propertyInfo = member as PropertyInfo;

            if (propertyInfo != null)
                return IsIndexedProperty(propertyInfo);
            return false;
        }

        /// <summary>
        ///     Determines whether the property is an indexed property.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>
        ///     <c>true</c> if the property is an indexed property; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsIndexedProperty(PropertyInfo property)
        {
            ValidationUtils.ArgumentNotNull(property, nameof(property));

            return property.GetIndexParameters().Length > 0;
        }

        /// <summary>
        ///     Gets the member's value on the object.
        /// </summary>
        /// <param name="member">The member.</param>
        /// <param name="target">The target object.</param>
        /// <returns>The member's value on the object.</returns>
        public static object GetMemberValue(MemberInfo member, object target)
        {
            ValidationUtils.ArgumentNotNull(member, nameof(member));
            ValidationUtils.ArgumentNotNull(target, nameof(target));

            switch (member.MemberType())
            {
                case MemberTypes.Field:
                    return ((FieldInfo) member).GetValue(target);
                case MemberTypes.Property:
                    try
                    {
                        return ((PropertyInfo) member).GetValue(target, null);
                    }
                    catch (TargetParameterCountException e)
                    {
                        throw new ArgumentException("MemberInfo '{0}' has index parameters".FormatWith(CultureInfo.InvariantCulture, member.Name), e);
                    }
                default:
                    throw new ArgumentException("MemberInfo '{0}' is not of type FieldInfo or PropertyInfo".FormatWith(CultureInfo.InvariantCulture, member.Name), nameof(member));
            }
        }

        /// <summary>
        ///     Sets the member's value on the target object.
        /// </summary>
        /// <param name="member">The member.</param>
        /// <param name="target">The target.</param>
        /// <param name="value">The value.</param>
        public static void SetMemberValue(MemberInfo member, object target, object value)
        {
            ValidationUtils.ArgumentNotNull(member, nameof(member));
            ValidationUtils.ArgumentNotNull(target, nameof(target));

            switch (member.MemberType())
            {
                case MemberTypes.Field:
                    ((FieldInfo) member).SetValue(target, value);
                    break;
                case MemberTypes.Property:
                    ((PropertyInfo) member).SetValue(target, value, null);
                    break;
                default:
                    throw new ArgumentException("MemberInfo '{0}' must be of type FieldInfo or PropertyInfo".FormatWith(CultureInfo.InvariantCulture, member.Name), nameof(member));
            }
        }

        /// <summary>
        ///     Determines whether the specified MemberInfo can be read.
        /// </summary>
        /// <param name="member">The MemberInfo to determine whether can be read.</param>
        /// ///
        /// <param name="nonPublic">if set to <c>true</c> then allow the member to be gotten non-publicly.</param>
        /// <returns>
        ///     <c>true</c> if the specified MemberInfo can be read; otherwise, <c>false</c>.
        /// </returns>
        public static bool CanReadMemberValue(MemberInfo member, bool nonPublic)
        {
            switch (member.MemberType())
            {
                case MemberTypes.Field:
                    var fieldInfo = (FieldInfo) member;

                    if (nonPublic)
                        return true;
                    if (fieldInfo.IsPublic)
                        return true;
                    return false;
                case MemberTypes.Property:
                    var propertyInfo = (PropertyInfo) member;

                    if (!propertyInfo.CanRead)
                        return false;
                    if (nonPublic)
                        return true;
                    return propertyInfo.GetGetMethod(nonPublic) != null;
                default:
                    return false;
            }
        }

        /// <summary>
        ///     Determines whether the specified MemberInfo can be set.
        /// </summary>
        /// <param name="member">The MemberInfo to determine whether can be set.</param>
        /// <param name="nonPublic">if set to <c>true</c> then allow the member to be set non-publicly.</param>
        /// <param name="canSetReadOnly">if set to <c>true</c> then allow the member to be set if read-only.</param>
        /// <returns>
        ///     <c>true</c> if the specified MemberInfo can be set; otherwise, <c>false</c>.
        /// </returns>
        public static bool CanSetMemberValue(MemberInfo member, bool nonPublic, bool canSetReadOnly)
        {
            switch (member.MemberType())
            {
                case MemberTypes.Field:
                    var fieldInfo = (FieldInfo) member;

                    if (fieldInfo.IsLiteral)
                        return false;
                    if (fieldInfo.IsInitOnly && !canSetReadOnly)
                        return false;
                    if (nonPublic)
                        return true;
                    if (fieldInfo.IsPublic)
                        return true;
                    return false;
                case MemberTypes.Property:
                    var propertyInfo = (PropertyInfo) member;

                    if (!propertyInfo.CanWrite)
                        return false;
                    if (nonPublic)
                        return true;
                    return propertyInfo.GetSetMethod(nonPublic) != null;
                default:
                    return false;
            }
        }

        public static List<MemberInfo> GetFieldsAndProperties(Type type, BindingFlags bindingAttr)
        {
            var targetMembers = new List<MemberInfo>();

            targetMembers.AddRange(GetFields(type, bindingAttr));
            targetMembers.AddRange(GetProperties(type, bindingAttr));

            // for some reason .NET returns multiple members when overriding a generic member on a base class
            // http://social.msdn.microsoft.com/Forums/en-US/b5abbfee-e292-4a64-8907-4e3f0fb90cd9/reflection-overriden-abstract-generic-properties?forum=netfxbcl
            // filter members to only return the override on the topmost class
            // update: I think this is fixed in .NET 3.5 SP1 - leave this in for now...
            var distinctMembers = new List<MemberInfo>(targetMembers.Count);

            foreach (var groupedMember in targetMembers.GroupBy(m => m.Name))
            {
                var count = groupedMember.Count();

                if (count == 1)
                {
                    distinctMembers.Add(groupedMember.First());
                }
                else
                {
                    IList<MemberInfo> resolvedMembers = new List<MemberInfo>();
                    foreach (var memberInfo in groupedMember)
                        if (resolvedMembers.Count == 0)
                            resolvedMembers.Add(memberInfo);
                        else if (!IsOverridenGenericMember(memberInfo, bindingAttr) || memberInfo.Name == "Item")
                            resolvedMembers.Add(memberInfo);

                    distinctMembers.AddRange(resolvedMembers);
                }
            }

            return distinctMembers;
        }

        private static bool IsOverridenGenericMember(MemberInfo memberInfo, BindingFlags bindingAttr)
        {
            if (memberInfo.MemberType() != MemberTypes.Property)
                return false;

            var propertyInfo = (PropertyInfo) memberInfo;
            if (!IsVirtual(propertyInfo))
                return false;

            var declaringType = propertyInfo.DeclaringType;
            if (!declaringType.IsGenericType())
                return false;
            var genericTypeDefinition = declaringType.GetGenericTypeDefinition();
            if (genericTypeDefinition == null)
                return false;
            var members = genericTypeDefinition.GetMember(propertyInfo.Name, bindingAttr);
            if (members.Length == 0)
                return false;
            var memberUnderlyingType = GetMemberUnderlyingType(members[0]);
            if (!memberUnderlyingType.IsGenericParameter)
                return false;

            return true;
        }

        public static T GetAttribute<T>(object attributeProvider) where T : Attribute
        {
            return GetAttribute<T>(attributeProvider, true);
        }

        public static T GetAttribute<T>(object attributeProvider, bool inherit) where T : Attribute
        {
            var attributes = GetAttributes<T>(attributeProvider, inherit);

            return attributes?.FirstOrDefault();
        }

        public static T[] GetAttributes<T>(object attributeProvider, bool inherit) where T : Attribute
        {
            var a = GetAttributes(attributeProvider, typeof(T), inherit);

            var attributes = a as T[];
            if (attributes != null)
                return attributes;

            return a.Cast<T>().ToArray();
        }

        public static Attribute[] GetAttributes(object attributeProvider, Type attributeType, bool inherit)
        {
            ValidationUtils.ArgumentNotNull(attributeProvider, nameof(attributeProvider));

            var provider = attributeProvider;

            // http://hyperthink.net/blog/getcustomattributes-gotcha/
            // ICustomAttributeProvider doesn't do inheritance

            var t = provider as Type;
            if (t != null)
            {
                var array = attributeType != null ? t.GetCustomAttributes(attributeType, inherit) : t.GetCustomAttributes(inherit);
                var attributes = array.Cast<Attribute>().ToArray();
                return attributes;
            }

            var a = provider as Assembly;
            if (a != null)
                return attributeType != null ? Attribute.GetCustomAttributes(a, attributeType) : Attribute.GetCustomAttributes(a);

            var mi = provider as MemberInfo;
            if (mi != null)
                return attributeType != null ? Attribute.GetCustomAttributes(mi, attributeType, inherit) : Attribute.GetCustomAttributes(mi, inherit);

            var m = provider as Module;
            if (m != null)
                return attributeType != null ? Attribute.GetCustomAttributes(m, attributeType, inherit) : Attribute.GetCustomAttributes(m, inherit);

            var p = provider as ParameterInfo;
            if (p != null)
                return attributeType != null ? Attribute.GetCustomAttributes(p, attributeType, inherit) : Attribute.GetCustomAttributes(p, inherit);

            var customAttributeProvider = (ICustomAttributeProvider) attributeProvider;
            var result = attributeType != null ? customAttributeProvider.GetCustomAttributes(attributeType, inherit) : customAttributeProvider.GetCustomAttributes(inherit);

            return (Attribute[]) result;
        }

        public static TypeNameKey SplitFullyQualifiedTypeName(string fullyQualifiedTypeName)
        {
            var assemblyDelimiterIndex = GetAssemblyDelimiterIndex(fullyQualifiedTypeName);

            string typeName;
            string assemblyName;

            if (assemblyDelimiterIndex != null)
            {
                typeName = fullyQualifiedTypeName.Trim(0, assemblyDelimiterIndex.GetValueOrDefault());
                assemblyName = fullyQualifiedTypeName.Trim(assemblyDelimiterIndex.GetValueOrDefault() + 1, fullyQualifiedTypeName.Length - assemblyDelimiterIndex.GetValueOrDefault() - 1);
            }
            else
            {
                typeName = fullyQualifiedTypeName;
                assemblyName = null;
            }

            return new TypeNameKey(assemblyName, typeName);
        }

        private static int? GetAssemblyDelimiterIndex(string fullyQualifiedTypeName)
        {
            // we need to get the first comma following all surrounded in brackets because of generic types
            // e.g. System.Collections.Generic.Dictionary`2[[System.String, mscorlib,Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[System.String, mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089
            var scope = 0;
            for (var i = 0; i < fullyQualifiedTypeName.Length; i++)
            {
                var current = fullyQualifiedTypeName[i];
                switch (current)
                {
                    case '[':
                        scope++;
                        break;
                    case ']':
                        scope--;
                        break;
                    case ',':
                        if (scope == 0)
                            return i;
                        break;
                }
            }

            return null;
        }

        public static MemberInfo GetMemberInfoFromType(Type targetType, MemberInfo memberInfo)
        {
            const BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

            switch (memberInfo.MemberType())
            {
                case MemberTypes.Property:
                    var propertyInfo = (PropertyInfo) memberInfo;

                    var types = propertyInfo.GetIndexParameters().Select(p => p.ParameterType).ToArray();

                    return targetType.GetProperty(propertyInfo.Name, bindingAttr, null, propertyInfo.PropertyType, types, null);
                default:
                    return targetType.GetMember(memberInfo.Name, memberInfo.MemberType(), bindingAttr).SingleOrDefault();
            }
        }

        public static IEnumerable<FieldInfo> GetFields(Type targetType, BindingFlags bindingAttr)
        {
            ValidationUtils.ArgumentNotNull(targetType, nameof(targetType));

            var fieldInfos = new List<MemberInfo>(targetType.GetFields(bindingAttr));
            // Type.GetFields doesn't return inherited private fields
            // manually find private fields from base class
            GetChildPrivateFields(fieldInfos, targetType, bindingAttr);

            return fieldInfos.Cast<FieldInfo>();
        }

        private static void GetChildPrivateFields(IList<MemberInfo> initialFields, Type targetType, BindingFlags bindingAttr)
        {
            // fix weirdness with private FieldInfos only being returned for the current Type
            // find base type fields and add them to result
            if ((bindingAttr & BindingFlags.NonPublic) != 0)
            {
                // modify flags to not search for public fields
                var nonPublicBindingAttr = bindingAttr.RemoveFlag(BindingFlags.Public);

                while ((targetType = targetType.BaseType()) != null)
                {
                    // filter out protected fields
                    var childPrivateFields =
                        targetType.GetFields(nonPublicBindingAttr).Where(f => f.IsPrivate);

                    initialFields.AddRange(childPrivateFields);
                }
            }
        }

        public static IEnumerable<PropertyInfo> GetProperties(Type targetType, BindingFlags bindingAttr)
        {
            ValidationUtils.ArgumentNotNull(targetType, nameof(targetType));

            var propertyInfos = new List<PropertyInfo>(targetType.GetProperties(bindingAttr));

            // GetProperties on an interface doesn't return properties from its interfaces
            if (targetType.IsInterface())
                foreach (var i in targetType.GetInterfaces())
                    propertyInfos.AddRange(i.GetProperties(bindingAttr));

            GetChildPrivateProperties(propertyInfos, targetType, bindingAttr);

            // a base class private getter/setter will be inaccessible unless the property was gotten from the base class
            for (var i = 0; i < propertyInfos.Count; i++)
            {
                var member = propertyInfos[i];
                if (member.DeclaringType != targetType)
                {
                    var declaredMember = (PropertyInfo) GetMemberInfoFromType(member.DeclaringType, member);
                    propertyInfos[i] = declaredMember;
                }
            }

            return propertyInfos;
        }

        public static BindingFlags RemoveFlag(this BindingFlags bindingAttr, BindingFlags flag)
        {
            return (bindingAttr & flag) == flag
                ? bindingAttr ^ flag
                : bindingAttr;
        }

        private static void GetChildPrivateProperties(IList<PropertyInfo> initialProperties, Type targetType, BindingFlags bindingAttr)
        {
            // fix weirdness with private PropertyInfos only being returned for the current Type
            // find base type properties and add them to result

            // also find base properties that have been hidden by subtype properties with the same name

            while ((targetType = targetType.BaseType()) != null)
                foreach (var propertyInfo in targetType.GetProperties(bindingAttr))
                {
                    var subTypeProperty = propertyInfo;

                    if (!subTypeProperty.IsVirtual())
                    {
                        if (!IsPublic(subTypeProperty))
                        {
                            // have to test on name rather than reference because instances are different
                            // depending on the type that GetProperties was called on
                            var index = initialProperties.IndexOf(p => p.Name == subTypeProperty.Name);
                            if (index == -1)
                            {
                                initialProperties.Add(subTypeProperty);
                            }
                            else
                            {
                                var childProperty = initialProperties[index];
                                // don't replace public child with private base
                                if (!IsPublic(childProperty))
                                    initialProperties[index] = subTypeProperty;
                            }
                        }
                        else
                        {
                            var index = initialProperties.IndexOf(p => p.Name == subTypeProperty.Name
                                                                       && p.DeclaringType == subTypeProperty.DeclaringType);

                            if (index == -1)
                                initialProperties.Add(subTypeProperty);
                        }
                    }
                    else
                    {
                        var subTypePropertyDeclaringType = subTypeProperty.GetBaseDefinition()?.DeclaringType ?? subTypeProperty.DeclaringType;

                        var index = initialProperties.IndexOf(p => p.Name == subTypeProperty.Name
                                                                   && p.IsVirtual()
                                                                   && (p.GetBaseDefinition()?.DeclaringType ?? p.DeclaringType).IsAssignableFrom(subTypePropertyDeclaringType));

                        // don't add a virtual property that has an override
                        if (index == -1)
                            initialProperties.Add(subTypeProperty);
                    }
                }
        }

        public static bool IsMethodOverridden(Type currentType, Type methodDeclaringType, string method)
        {
            var isMethodOverriden = currentType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Any(info =>
                    info.Name == method &&
                    // check that the method overrides the original on DynamicObjectProxy
                    info.DeclaringType != methodDeclaringType
                    && info.GetBaseDefinition().DeclaringType == methodDeclaringType
                );

            return isMethodOverriden;
        }

        public static object GetDefaultValue(Type type)
        {
            if (!type.IsValueType())
                return null;

            switch (ConvertUtils.GetTypeCode(type))
            {
                case PrimitiveTypeCode.Boolean:
                    return false;
                case PrimitiveTypeCode.Char:
                case PrimitiveTypeCode.SByte:
                case PrimitiveTypeCode.Byte:
                case PrimitiveTypeCode.Int16:
                case PrimitiveTypeCode.UInt16:
                case PrimitiveTypeCode.Int32:
                case PrimitiveTypeCode.UInt32:
                    return 0;
                case PrimitiveTypeCode.Int64:
                case PrimitiveTypeCode.UInt64:
                    return 0L;
                case PrimitiveTypeCode.Single:
                    return 0f;
                case PrimitiveTypeCode.Double:
                    return 0.0;
                case PrimitiveTypeCode.Decimal:
                    return 0m;
                case PrimitiveTypeCode.DateTime:
                    return new DateTime();
                case PrimitiveTypeCode.BigInteger:
                    return new BigInteger();
                case PrimitiveTypeCode.Guid:
                    return new Guid();
                case PrimitiveTypeCode.DateTimeOffset:
                    return new DateTimeOffset();
            }

            if (IsNullable(type))
                return null;

            // possibly use IL initobj for perf here?
            return Activator.CreateInstance(type);
        }
    }

    internal struct TypeNameKey : IEquatable<TypeNameKey>
    {
        internal readonly string AssemblyName;
        internal readonly string TypeName;

        public TypeNameKey(string assemblyName, string typeName)
        {
            AssemblyName = assemblyName;
            TypeName = typeName;
        }

        public override int GetHashCode()
        {
            return (AssemblyName?.GetHashCode() ?? 0) ^ (TypeName?.GetHashCode() ?? 0);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is TypeNameKey))
                return false;

            return Equals((TypeNameKey) obj);
        }

        public bool Equals(TypeNameKey other)
        {
            return AssemblyName == other.AssemblyName && TypeName == other.TypeName;
        }
    }
}
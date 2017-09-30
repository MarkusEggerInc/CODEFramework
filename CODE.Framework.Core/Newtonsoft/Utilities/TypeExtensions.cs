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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CODE.Framework.Core.Newtonsoft.Utilities
{
    internal static class TypeExtensions
    {
        public static MethodInfo Method(this Delegate d)
        {
            return d.Method;
        }

        public static MemberTypes MemberType(this MemberInfo memberInfo)
        {
            if (memberInfo is PropertyInfo)
                return MemberTypes.Property;
            if (memberInfo is FieldInfo)
                return MemberTypes.Field;
            if (memberInfo is EventInfo)
                return MemberTypes.Event;
            if (memberInfo is MethodInfo)
                return MemberTypes.Method;
            return default(MemberTypes);
        }

        public static bool ContainsGenericParameters(this Type type)
        {
            return type.ContainsGenericParameters;
        }

        public static bool IsInterface(this Type type)
        {
            return type.IsInterface;
        }

        public static bool IsGenericType(this Type type)
        {
            return type.IsGenericType;
        }

        public static bool IsGenericTypeDefinition(this Type type)
        {
            return type.IsGenericTypeDefinition;
        }

        public static Type BaseType(this Type type)
        {
            return type.BaseType;
        }

        public static Assembly Assembly(this Type type)
        {
            return type.Assembly;
        }

        public static bool IsEnum(this Type type)
        {
            return type.IsEnum;
        }

        public static bool IsClass(this Type type)
        {
            return type.IsClass;
        }

        public static bool IsSealed(this Type type)
        {
            return type.IsSealed;
        }

        public static PropertyInfo GetProperty(this Type type, string name, BindingFlags bindingFlags, object placeholder1, Type propertyType, IList<Type> indexParameters, object placeholder2)
        {
            IEnumerable<PropertyInfo> propertyInfos = type.GetProperties(bindingFlags);

            return propertyInfos.Where(p =>
            {
                if (name != null && name != p.Name)
                    return false;
                if (propertyType != null && propertyType != p.PropertyType)
                    return false;
                if (indexParameters != null)
                    if (!p.GetIndexParameters().Select(ip => ip.ParameterType).SequenceEqual(indexParameters))
                        return false;

                return true;
            }).SingleOrDefault();
        }

        public static IEnumerable<MemberInfo> GetMember(this Type type, string name, MemberTypes memberType, BindingFlags bindingFlags)
        {
            return type.GetMember(name, bindingFlags).Where(m =>
            {
                if ((m.MemberType() | memberType) != memberType)
                    return false;

                return true;
            });
        }

        public static bool IsAbstract(this Type type)
        {
            return type.IsAbstract;
        }

        public static bool IsVisible(this Type type)
        {
            return type.IsVisible;
        }

        public static bool IsValueType(this Type type)
        {
            return type.IsValueType;
        }

        public static bool IsPrimitive(this Type type)
        {
            return type.IsPrimitive;
        }

        public static bool AssignableToTypeName(this Type type, string fullTypeName, bool searchInterfaces, out Type match)
        {
            var current = type;

            while (current != null)
            {
                if (string.Equals(current.FullName, fullTypeName, StringComparison.Ordinal))
                {
                    match = current;
                    return true;
                }

                current = current.BaseType();
            }

            if (searchInterfaces)
                foreach (var i in type.GetInterfaces())
                    if (string.Equals(i.Name, fullTypeName, StringComparison.Ordinal))
                    {
                        match = type;
                        return true;
                    }

            match = null;
            return false;
        }

        public static bool AssignableToTypeName(this Type type, string fullTypeName, bool searchInterfaces)
        {
            Type match;
            return type.AssignableToTypeName(fullTypeName, searchInterfaces, out match);
        }

        public static bool ImplementInterface(this Type type, Type interfaceType)
        {
            for (var currentType = type; currentType != null; currentType = currentType.BaseType())
            {
                IEnumerable<Type> interfaces = currentType.GetInterfaces();
                foreach (var i in interfaces)
                    if (i == interfaceType || i != null && i.ImplementInterface(interfaceType))
                        return true;
            }

            return false;
        }
    }
}
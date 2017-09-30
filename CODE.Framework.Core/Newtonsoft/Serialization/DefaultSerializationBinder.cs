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
using System.Globalization;
using System.Reflection;
using System.Runtime.Serialization;
using CODE.Framework.Core.Newtonsoft.Utilities;

namespace CODE.Framework.Core.Newtonsoft.Serialization
{
    /// <summary>
    ///     The default serialization binder used when resolving and loading classes from type names.
    /// </summary>
    public class DefaultSerializationBinder :
#pragma warning disable 618
        SerializationBinder,
#pragma warning restore 618
        ISerializationBinder
    {
        internal static readonly DefaultSerializationBinder Instance = new DefaultSerializationBinder();

        private readonly ThreadSafeStore<TypeNameKey, Type> _typeCache;

        /// <summary>
        ///     Initializes a new instance of the <see cref="DefaultSerializationBinder" /> class.
        /// </summary>
        public DefaultSerializationBinder()
        {
            _typeCache = new ThreadSafeStore<TypeNameKey, Type>(GetTypeFromTypeNameKey);
        }

        /// <summary>
        ///     When overridden in a derived class, controls the binding of a serialized object to a type.
        /// </summary>
        /// <param name="assemblyName">Specifies the <see cref="Assembly" /> name of the serialized object.</param>
        /// <param name="typeName">Specifies the <see cref="System.Type" /> name of the serialized object.</param>
        /// <returns>
        ///     The type of the object the formatter creates a new instance of.
        /// </returns>
        public override Type BindToType(string assemblyName, string typeName)
        {
            return GetTypeByName(new TypeNameKey(assemblyName, typeName));
        }

        /// <summary>
        ///     When overridden in a derived class, controls the binding of a serialized object to a type.
        /// </summary>
        /// <param name="serializedType">The type of the object the formatter creates a new instance of.</param>
        /// <param name="assemblyName">Specifies the <see cref="Assembly" /> name of the serialized object.</param>
        /// <param name="typeName">Specifies the <see cref="System.Type" /> name of the serialized object.</param>
        public override void BindToName(Type serializedType, out string assemblyName, out string typeName)
        {
            assemblyName = serializedType.Assembly.FullName;
            typeName = serializedType.FullName;
        }

        private Type GetTypeFromTypeNameKey(TypeNameKey typeNameKey)
        {
            var assemblyName = typeNameKey.AssemblyName;
            var typeName = typeNameKey.TypeName;

            if (assemblyName != null)
            {
                Assembly assembly;

                assembly = Assembly.Load(new AssemblyName(assemblyName));

                if (assembly == null)
                {
                    // will find assemblies loaded with Assembly.LoadFile outside of the main directory
                    var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
                    foreach (var a in loadedAssemblies)
                        if (a.FullName == assemblyName || a.GetName().Name == assemblyName)
                        {
                            assembly = a;
                            break;
                        }
                }

                if (assembly == null)
                    throw new JsonSerializationException("Could not load assembly '{0}'.".FormatWith(CultureInfo.InvariantCulture, assemblyName));

                var type = assembly.GetType(typeName);
                if (type == null)
                {
                    // if generic type, try manually parsing the type arguments for the case of dynamically loaded assemblies
                    // example generic typeName format: System.Collections.Generic.Dictionary`2[[System.String, mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[System.String, mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]
                    if (typeName.IndexOf('`') >= 0)
                        try
                        {
                            type = GetGenericTypeFromTypeName(typeName, assembly);
                        }
                        catch (Exception ex)
                        {
                            throw new JsonSerializationException("Could not find type '{0}' in assembly '{1}'.".FormatWith(CultureInfo.InvariantCulture, typeName, assembly.FullName), ex);
                        }

                    if (type == null)
                        throw new JsonSerializationException("Could not find type '{0}' in assembly '{1}'.".FormatWith(CultureInfo.InvariantCulture, typeName, assembly.FullName));
                }

                return type;
            }
            return Type.GetType(typeName);
        }

        private Type GetGenericTypeFromTypeName(string typeName, Assembly assembly)
        {
            Type type = null;
            var openBracketIndex = typeName.IndexOf('[');
            if (openBracketIndex >= 0)
            {
                var genericTypeDefName = typeName.Substring(0, openBracketIndex);
                var genericTypeDef = assembly.GetType(genericTypeDefName);
                if (genericTypeDef != null)
                {
                    var genericTypeArguments = new List<Type>();
                    var scope = 0;
                    var typeArgStartIndex = 0;
                    var endIndex = typeName.Length - 1;
                    for (var i = openBracketIndex + 1; i < endIndex; ++i)
                    {
                        var current = typeName[i];
                        switch (current)
                        {
                            case '[':
                                if (scope == 0)
                                    typeArgStartIndex = i + 1;
                                ++scope;
                                break;
                            case ']':
                                --scope;
                                if (scope == 0)
                                {
                                    var typeArgAssemblyQualifiedName = typeName.Substring(typeArgStartIndex, i - typeArgStartIndex);

                                    var typeNameKey = ReflectionUtils.SplitFullyQualifiedTypeName(typeArgAssemblyQualifiedName);
                                    genericTypeArguments.Add(GetTypeByName(typeNameKey));
                                }
                                break;
                        }
                    }

                    type = genericTypeDef.MakeGenericType(genericTypeArguments.ToArray());
                }
            }

            return type;
        }

        private Type GetTypeByName(TypeNameKey typeNameKey)
        {
            return _typeCache.Get(typeNameKey);
        }
    }
}
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
using System.Reflection;
using CODE.Framework.Core.Newtonsoft.Utilities;

namespace CODE.Framework.Core.Newtonsoft.Serialization
{
    /// <summary>
    /// Contract details for a <see cref="Type"/> used by the <see cref="JsonSerializer"/>.
    /// </summary>
    public class JsonDictionaryContract : JsonContainerContract
    {
        /// <summary>
        /// Gets or sets the property name resolver.
        /// </summary>
        /// <value>The property name resolver.</value>
        public Func<string, string> PropertyNameResolver { get; set; }

        /// <summary>
        /// Gets the <see cref="Type"/> of the dictionary keys.
        /// </summary>
        /// <value>The <see cref="Type"/> of the dictionary keys.</value>
        public Type DictionaryKeyType { get; private set; }

        /// <summary>
        /// Gets the <see cref="Type"/> of the dictionary values.
        /// </summary>
        /// <value>The <see cref="Type"/> of the dictionary values.</value>
        public Type DictionaryValueType { get; private set; }

        internal JsonContract KeyContract { get; set; }

        private readonly Type _genericCollectionDefinitionType;

        private Type _genericWrapperType;
        private ObjectConstructor<object> _genericWrapperCreator;

        private Func<object> _genericTemporaryDictionaryCreator;

        internal bool ShouldCreateWrapper { get; private set; }

        private readonly ConstructorInfo _parametrizedConstructor;

        private ObjectConstructor<object> _parametrizedCreator;
        internal ObjectConstructor<object> ParametrizedCreator
        {
            get { return _parametrizedCreator ?? (_parametrizedCreator = JsonTypeReflector.ReflectionDelegateFactory.CreateParametrizedConstructor(_parametrizedConstructor)); }
        }

        internal bool HasParametrizedCreator
        {
            get { return _parametrizedCreator != null || _parametrizedConstructor != null; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonDictionaryContract"/> class.
        /// </summary>
        /// <param name="underlyingType">The underlying type for the contract.</param>
        public JsonDictionaryContract(Type underlyingType)
            : base(underlyingType)
        {
            ContractType = JsonContractType.Dictionary;

            Type keyType;
            Type valueType;

            if (ReflectionUtils.ImplementsGenericDefinition(underlyingType, typeof(IDictionary<,>), out _genericCollectionDefinitionType))
            {
                keyType = _genericCollectionDefinitionType.GetGenericArguments()[0];
                valueType = _genericCollectionDefinitionType.GetGenericArguments()[1];
                if (ReflectionUtils.IsGenericDefinition(UnderlyingType, typeof(IDictionary<,>)))
                    CreatedType = typeof(Dictionary<,>).MakeGenericType(keyType, valueType);
            }
            else
            {
                ReflectionUtils.GetDictionaryKeyValueTypes(UnderlyingType, out keyType, out valueType);
                if (UnderlyingType == typeof(IDictionary))
                    CreatedType = typeof(Dictionary<object, object>);
            }

            if (keyType != null && valueType != null)
            {
                _parametrizedConstructor = CollectionUtils.ResolveEnumerableCollectionConstructor(CreatedType, typeof(KeyValuePair<,>).MakeGenericType(keyType, valueType));
                if (!HasParametrizedCreator && underlyingType.Name == FSharpUtils.FSharpMapTypeName)
                {
                    FSharpUtils.EnsureInitialized(underlyingType.Assembly());
                    _parametrizedCreator = FSharpUtils.CreateMap(keyType, valueType);
                }
            }

            ShouldCreateWrapper = !typeof(IDictionary).IsAssignableFrom(CreatedType);

            DictionaryKeyType = keyType;
            DictionaryValueType = valueType;
        }

        internal IWrappedDictionary CreateWrapper(object dictionary)
        {
            if (_genericWrapperCreator != null) return (IWrappedDictionary) _genericWrapperCreator(dictionary);
            _genericWrapperType = typeof(DictionaryWrapper<,>).MakeGenericType(DictionaryKeyType, DictionaryValueType);
            var genericWrapperConstructor = _genericWrapperType.GetConstructor(new[] { _genericCollectionDefinitionType });
            _genericWrapperCreator = JsonTypeReflector.ReflectionDelegateFactory.CreateParametrizedConstructor(genericWrapperConstructor);

            return (IWrappedDictionary)_genericWrapperCreator(dictionary);
        }

        internal IDictionary CreateTemporaryDictionary()
        {
            if (_genericTemporaryDictionaryCreator != null) return (IDictionary) _genericTemporaryDictionaryCreator();
            var temporaryDictionaryType = typeof(Dictionary<,>).MakeGenericType(DictionaryKeyType, DictionaryValueType);
            _genericTemporaryDictionaryCreator = JsonTypeReflector.ReflectionDelegateFactory.CreateDefaultConstructor<object>(temporaryDictionaryType);
            return (IDictionary)_genericTemporaryDictionaryCreator();
        }
    }
}
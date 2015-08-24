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
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using CODE.Framework.Core.Newtonsoft.Utilities;

namespace CODE.Framework.Core.Newtonsoft.Serialization
{
    /// <summary>
    /// Contract details for a <see cref="Type"/> used by the <see cref="JsonSerializer"/>.
    /// </summary>
    public class JsonArrayContract : JsonContainerContract
    {
        /// <summary>
        /// Gets the <see cref="Type"/> of the collection items.
        /// </summary>
        /// <value>The <see cref="Type"/> of the collection items.</value>
        public Type CollectionItemType { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the collection type is a multidimensional array.
        /// </summary>
        /// <value><c>true</c> if the collection type is a multidimensional array; otherwise, <c>false</c>.</value>
        public bool IsMultidimensionalArray { get; private set; }

        private readonly Type _genericCollectionDefinitionType;

        private Type _genericWrapperType;
        private ObjectConstructor<object> _genericWrapperCreator;
        private Func<object> _genericTemporaryCollectionCreator;

        internal bool IsArray { get; private set; }
        internal bool ShouldCreateWrapper { get; private set; }
        internal bool CanDeserialize { get; private set; }

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
        /// Initializes a new instance of the <see cref="JsonArrayContract"/> class.
        /// </summary>
        /// <param name="underlyingType">The underlying type for the contract.</param>
        public JsonArrayContract(Type underlyingType)
            : base(underlyingType)
        {
            ContractType = JsonContractType.Array;
            IsArray = CreatedType.IsArray;

            bool canDeserialize;

            Type tempCollectionType;
            if (IsArray)
            {
                CollectionItemType = ReflectionUtils.GetCollectionItemType(UnderlyingType);
                IsReadOnlyOrFixedSize = true;
                _genericCollectionDefinitionType = typeof(List<>).MakeGenericType(CollectionItemType);

                canDeserialize = true;
                IsMultidimensionalArray = (IsArray && UnderlyingType.GetArrayRank() > 1);
            }
            else if (typeof(IList).IsAssignableFrom(underlyingType))
            {
                if (ReflectionUtils.ImplementsGenericDefinition(underlyingType, typeof(ICollection<>), out _genericCollectionDefinitionType))
                    CollectionItemType = _genericCollectionDefinitionType.GetGenericArguments()[0];
                else
                    CollectionItemType = ReflectionUtils.GetCollectionItemType(underlyingType);

                if (underlyingType == typeof(IList))
                    CreatedType = typeof(List<object>);

                if (CollectionItemType != null)
                    _parametrizedConstructor = CollectionUtils.ResolveEnumerableCollectionConstructor(underlyingType, CollectionItemType);

                IsReadOnlyOrFixedSize = ReflectionUtils.InheritsGenericDefinition(underlyingType, typeof(ReadOnlyCollection<>));
                canDeserialize = true;
            }
            else if (ReflectionUtils.ImplementsGenericDefinition(underlyingType, typeof(ICollection<>), out _genericCollectionDefinitionType))
            {
                CollectionItemType = _genericCollectionDefinitionType.GetGenericArguments()[0];

                if (ReflectionUtils.IsGenericDefinition(underlyingType, typeof(ICollection<>))
                    || ReflectionUtils.IsGenericDefinition(underlyingType, typeof(IList<>)))
                    CreatedType = typeof(List<>).MakeGenericType(CollectionItemType);

                if (ReflectionUtils.IsGenericDefinition(underlyingType, typeof(ISet<>)))
                    CreatedType = typeof(HashSet<>).MakeGenericType(CollectionItemType);

                _parametrizedConstructor = CollectionUtils.ResolveEnumerableCollectionConstructor(underlyingType, CollectionItemType);
                canDeserialize = true;
                ShouldCreateWrapper = true;
            }
            else if (ReflectionUtils.ImplementsGenericDefinition(underlyingType, typeof(IEnumerable<>), out tempCollectionType))
            {
                CollectionItemType = tempCollectionType.GetGenericArguments()[0];

                if (ReflectionUtils.IsGenericDefinition(UnderlyingType, typeof(IEnumerable<>)))
                    CreatedType = typeof(List<>).MakeGenericType(CollectionItemType);

                _parametrizedConstructor = CollectionUtils.ResolveEnumerableCollectionConstructor(underlyingType, CollectionItemType);

                if (!HasParametrizedCreator && underlyingType.Name == FSharpUtils.FSharpListTypeName)
                {
                    FSharpUtils.EnsureInitialized(underlyingType.Assembly());
                    _parametrizedCreator = FSharpUtils.CreateSeq(CollectionItemType);
                }

                if (underlyingType.IsGenericType() && underlyingType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    _genericCollectionDefinitionType = tempCollectionType;

                    IsReadOnlyOrFixedSize = false;
                    ShouldCreateWrapper = false;
                    canDeserialize = true;
                }
                else
                {
                    _genericCollectionDefinitionType = typeof(List<>).MakeGenericType(CollectionItemType);

                    IsReadOnlyOrFixedSize = true;
                    ShouldCreateWrapper = true;
                    canDeserialize = HasParametrizedCreator;
                }
            }
            else
            {
                // types that implement IEnumerable and nothing else
                canDeserialize = false;
                ShouldCreateWrapper = true;
            }

            CanDeserialize = canDeserialize;
        }

        internal IWrappedCollection CreateWrapper(object list)
        {
            if (_genericWrapperCreator != null) return (IWrappedCollection) _genericWrapperCreator(list);
            _genericWrapperType = typeof(CollectionWrapper<>).MakeGenericType(CollectionItemType);

            Type constructorArgument;

            if (ReflectionUtils.InheritsGenericDefinition(_genericCollectionDefinitionType, typeof(List<>))
                || _genericCollectionDefinitionType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                constructorArgument = typeof(ICollection<>).MakeGenericType(CollectionItemType);
            else
                constructorArgument = _genericCollectionDefinitionType;

            var genericWrapperConstructor = _genericWrapperType.GetConstructor(new[] { constructorArgument });
            _genericWrapperCreator = JsonTypeReflector.ReflectionDelegateFactory.CreateParametrizedConstructor(genericWrapperConstructor);

            return (IWrappedCollection)_genericWrapperCreator(list);
        }

        internal IList CreateTemporaryCollection()
        {
            if (_genericTemporaryCollectionCreator != null) return (IList) _genericTemporaryCollectionCreator();
            // multidimensional array will also have array instances in it
            var collectionItemType = (IsMultidimensionalArray) ? typeof(object) : CollectionItemType;
            var temporaryListType = typeof(List<>).MakeGenericType(collectionItemType);
            _genericTemporaryCollectionCreator = JsonTypeReflector.ReflectionDelegateFactory.CreateDefaultConstructor<object>(temporaryListType);
            return (IList)_genericTemporaryCollectionCreator();
        }
    }
}
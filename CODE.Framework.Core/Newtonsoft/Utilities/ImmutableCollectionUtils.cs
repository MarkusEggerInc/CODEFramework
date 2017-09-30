using System;
using System.Collections.Generic;
using System.Linq;
using CODE.Framework.Core.Newtonsoft.Serialization;

namespace CODE.Framework.Core.Newtonsoft.Utilities
{
    /// <summary>
    ///     Helper class for serializing immutable collections.
    ///     Note that this is used by all builds, even those that don't support immutable collections, in case the DLL is GACed
    ///     https://github.com/JamesNK/Newtonsoft.Json/issues/652
    /// </summary>
    internal static class ImmutableCollectionsUtils
    {
        private const string ImmutableListGenericInterfaceTypeName = "System.Collections.Immutable.IImmutableList`1";
        private const string ImmutableQueueGenericInterfaceTypeName = "System.Collections.Immutable.IImmutableQueue`1";
        private const string ImmutableStackGenericInterfaceTypeName = "System.Collections.Immutable.IImmutableStack`1";
        private const string ImmutableSetGenericInterfaceTypeName = "System.Collections.Immutable.IImmutableSet`1";

        private const string ImmutableArrayTypeName = "System.Collections.Immutable.ImmutableArray";
        private const string ImmutableArrayGenericTypeName = "System.Collections.Immutable.ImmutableArray`1";

        private const string ImmutableListTypeName = "System.Collections.Immutable.ImmutableList";
        private const string ImmutableListGenericTypeName = "System.Collections.Immutable.ImmutableList`1";

        private const string ImmutableQueueTypeName = "System.Collections.Immutable.ImmutableQueue";
        private const string ImmutableQueueGenericTypeName = "System.Collections.Immutable.ImmutableQueue`1";

        private const string ImmutableStackTypeName = "System.Collections.Immutable.ImmutableStack";
        private const string ImmutableStackGenericTypeName = "System.Collections.Immutable.ImmutableStack`1";

        private const string ImmutableSortedSetTypeName = "System.Collections.Immutable.ImmutableSortedSet";
        private const string ImmutableSortedSetGenericTypeName = "System.Collections.Immutable.ImmutableSortedSet`1";

        private const string ImmutableHashSetTypeName = "System.Collections.Immutable.ImmutableHashSet";
        private const string ImmutableHashSetGenericTypeName = "System.Collections.Immutable.ImmutableHashSet`1";

        private const string ImmutableDictionaryGenericInterfaceTypeName = "System.Collections.Immutable.IImmutableDictionary`2";

        private const string ImmutableDictionaryTypeName = "System.Collections.Immutable.ImmutableDictionary";
        private const string ImmutableDictionaryGenericTypeName = "System.Collections.Immutable.ImmutableDictionary`2";

        private const string ImmutableSortedDictionaryTypeName = "System.Collections.Immutable.ImmutableSortedDictionary";
        private const string ImmutableSortedDictionaryGenericTypeName = "System.Collections.Immutable.ImmutableSortedDictionary`2";

        private static readonly IList<ImmutableCollectionTypeInfo> ArrayContractImmutableCollectionDefinitions = new List<ImmutableCollectionTypeInfo>
        {
            new ImmutableCollectionTypeInfo(ImmutableListGenericInterfaceTypeName, ImmutableListGenericTypeName, ImmutableListTypeName),
            new ImmutableCollectionTypeInfo(ImmutableListGenericTypeName, ImmutableListGenericTypeName, ImmutableListTypeName),
            new ImmutableCollectionTypeInfo(ImmutableQueueGenericInterfaceTypeName, ImmutableQueueGenericTypeName, ImmutableQueueTypeName),
            new ImmutableCollectionTypeInfo(ImmutableQueueGenericTypeName, ImmutableQueueGenericTypeName, ImmutableQueueTypeName),
            new ImmutableCollectionTypeInfo(ImmutableStackGenericInterfaceTypeName, ImmutableStackGenericTypeName, ImmutableStackTypeName),
            new ImmutableCollectionTypeInfo(ImmutableStackGenericTypeName, ImmutableStackGenericTypeName, ImmutableStackTypeName),
            new ImmutableCollectionTypeInfo(ImmutableSetGenericInterfaceTypeName, ImmutableSortedSetGenericTypeName, ImmutableSortedSetTypeName),
            new ImmutableCollectionTypeInfo(ImmutableSortedSetGenericTypeName, ImmutableSortedSetGenericTypeName, ImmutableSortedSetTypeName),
            new ImmutableCollectionTypeInfo(ImmutableHashSetGenericTypeName, ImmutableHashSetGenericTypeName, ImmutableHashSetTypeName),
            new ImmutableCollectionTypeInfo(ImmutableArrayGenericTypeName, ImmutableArrayGenericTypeName, ImmutableArrayTypeName)
        };

        private static readonly IList<ImmutableCollectionTypeInfo> DictionaryContractImmutableCollectionDefinitions = new List<ImmutableCollectionTypeInfo>
        {
            new ImmutableCollectionTypeInfo(ImmutableDictionaryGenericInterfaceTypeName, ImmutableSortedDictionaryGenericTypeName, ImmutableSortedDictionaryTypeName),
            new ImmutableCollectionTypeInfo(ImmutableSortedDictionaryGenericTypeName, ImmutableSortedDictionaryGenericTypeName, ImmutableSortedDictionaryTypeName),
            new ImmutableCollectionTypeInfo(ImmutableDictionaryGenericTypeName, ImmutableDictionaryGenericTypeName, ImmutableDictionaryTypeName)
        };

        internal static bool TryBuildImmutableForArrayContract(Type underlyingType, Type collectionItemType, out Type createdType, out ObjectConstructor<object> parameterizedCreator)
        {
            if (underlyingType.IsGenericType())
            {
                var underlyingTypeDefinition = underlyingType.GetGenericTypeDefinition();
                var name = underlyingTypeDefinition.FullName;

                var definition = ArrayContractImmutableCollectionDefinitions.FirstOrDefault(d => d.ContractTypeName == name);
                if (definition != null)
                {
                    var createdTypeDefinition = underlyingTypeDefinition.Assembly().GetType(definition.CreatedTypeName);
                    var builderTypeDefinition = underlyingTypeDefinition.Assembly().GetType(definition.BuilderTypeName);

                    if (createdTypeDefinition != null && builderTypeDefinition != null)
                    {
                        var mb = builderTypeDefinition.GetMethods().FirstOrDefault(m => m.Name == "CreateRange" && m.GetParameters().Length == 1);
                        if (mb != null)
                        {
                            createdType = createdTypeDefinition.MakeGenericType(collectionItemType);
                            var method = mb.MakeGenericMethod(collectionItemType);
                            parameterizedCreator = JsonTypeReflector.ReflectionDelegateFactory.CreateParameterizedConstructor(method);
                            return true;
                        }
                    }
                }
            }

            createdType = null;
            parameterizedCreator = null;
            return false;
        }

        internal static bool TryBuildImmutableForDictionaryContract(Type underlyingType, Type keyItemType, Type valueItemType, out Type createdType, out ObjectConstructor<object> parameterizedCreator)
        {
            if (underlyingType.IsGenericType())
            {
                var underlyingTypeDefinition = underlyingType.GetGenericTypeDefinition();
                var name = underlyingTypeDefinition.FullName;

                var definition = DictionaryContractImmutableCollectionDefinitions.FirstOrDefault(d => d.ContractTypeName == name);
                if (definition != null)
                {
                    var createdTypeDefinition = underlyingTypeDefinition.Assembly().GetType(definition.CreatedTypeName);
                    var builderTypeDefinition = underlyingTypeDefinition.Assembly().GetType(definition.BuilderTypeName);

                    if (createdTypeDefinition != null && builderTypeDefinition != null)
                    {
                        var mb = builderTypeDefinition.GetMethods().FirstOrDefault(m =>
                        {
                            var parameters = m.GetParameters();

                            return m.Name == "CreateRange" && parameters.Length == 1 && parameters[0].ParameterType.IsGenericType() && parameters[0].ParameterType.GetGenericTypeDefinition() == typeof(IEnumerable<>);
                        });
                        if (mb != null)
                        {
                            createdType = createdTypeDefinition.MakeGenericType(keyItemType, valueItemType);
                            var method = mb.MakeGenericMethod(keyItemType, valueItemType);
                            parameterizedCreator = JsonTypeReflector.ReflectionDelegateFactory.CreateParameterizedConstructor(method);
                            return true;
                        }
                    }
                }
            }

            createdType = null;
            parameterizedCreator = null;
            return false;
        }

        internal class ImmutableCollectionTypeInfo
        {
            public ImmutableCollectionTypeInfo(string contractTypeName, string createdTypeName, string builderTypeName)
            {
                ContractTypeName = contractTypeName;
                CreatedTypeName = createdTypeName;
                BuilderTypeName = builderTypeName;
            }

            public string ContractTypeName { get; set; }
            public string CreatedTypeName { get; set; }
            public string BuilderTypeName { get; set; }
        }
    }
}
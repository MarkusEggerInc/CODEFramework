using System;
using System.Collections.Generic;
using CODE.Framework.Core.Newtonsoft.Utilities;

namespace CODE.Framework.Core.Newtonsoft.Serialization
{
    /// <summary>
    ///     Resolves member mappings for a type, camel casing property names.
    /// </summary>
    public class CamelCasePropertyNamesContractResolver : DefaultContractResolver
    {
        private static readonly object TypeContractCacheLock = new object();
        private static readonly PropertyNameTable NameTable = new PropertyNameTable();
        private static Dictionary<ResolverContractKey, JsonContract> _contractCache;

        /// <summary>
        ///     Initializes a new instance of the <see cref="CamelCasePropertyNamesContractResolver" /> class.
        /// </summary>
        public CamelCasePropertyNamesContractResolver()
        {
            NamingStrategy = new CamelCaseNamingStrategy
            {
                ProcessDictionaryKeys = true,
                OverrideSpecifiedNames = true
            };
        }

        /// <summary>
        ///     Resolves the contract for a given type.
        /// </summary>
        /// <param name="type">The type to resolve a contract for.</param>
        /// <returns>The contract for a given type.</returns>
        public override JsonContract ResolveContract(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            // for backwards compadibility the CamelCasePropertyNamesContractResolver shares contracts between instances
            JsonContract contract;
            var key = new ResolverContractKey(GetType(), type);
            var cache = _contractCache;
            if (cache == null || !cache.TryGetValue(key, out contract))
            {
                contract = CreateContract(type);

                // avoid the possibility of modifying the cache dictionary while another thread is accessing it
                lock (TypeContractCacheLock)
                {
                    cache = _contractCache;
                    var updatedCache = cache != null
                        ? new Dictionary<ResolverContractKey, JsonContract>(cache)
                        : new Dictionary<ResolverContractKey, JsonContract>();
                    updatedCache[key] = contract;

                    _contractCache = updatedCache;
                }
            }

            return contract;
        }

        internal override PropertyNameTable GetNameTable()
        {
            return NameTable;
        }
    }
}
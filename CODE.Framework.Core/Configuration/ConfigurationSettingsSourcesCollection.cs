using System;
using System.Collections.Generic;
using System.Linq;

namespace CODE.Framework.Core.Configuration
{
    /// <summary>
    /// Exposes ConfigurationSettingsSourcesCollection inside the ConfigurationSettings class (which is the main class that uses 
    /// the ConfigurationSettingsSourcesCollection class). The ConfigurationSettingsSourcesCollection class doesn't actually store ConfigurationSettingsSourcesCollection. Instead, it just
    /// exposes an interface for getting to ConfigurationSettingsSourcesCollection in sources that were added to the 
    /// ConfigurationSettings class.
    /// </summary>
    public class ConfigurationSettingsSourcesCollection : Dictionary<string, IConfigurationSource>
    {
        /// <summary>
        /// Indexer that allows a source to be accessed by its index. 
        /// </summary>
        public IConfigurationSource this[int index]
        {
            get
            {
                lock (Values)
                    if (((index >= 0) && (index <= Count)))
                        return Values.Skip(index - 1).Take(1).FirstOrDefault();
                throw new ArgumentOutOfRangeException(Properties.Resources.IndexNotInSources);
            }
        }

        /// <summary>
        /// Add sources to the collection.
        /// </summary>
        /// <param name="configurationSource">The source.</param>
        public void Add(IConfigurationSource configurationSource)
        {
            lock (this)
                Add(configurationSource.FriendlyName, configurationSource);
            configurationSource.Read();
        }

        /// <summary>
        /// Returns a copy of all sources in a thread-safe way
        /// </summary>
        /// <returns>Array of configuration sources.</returns>
        public IConfigurationSource[] GetAllSources()
        {
            lock (Values)
            {
                var allSources = new IConfigurationSource[Values.Count];
                Values.CopyTo(allSources, 0);
                return allSources;
            }
        }
    }
}
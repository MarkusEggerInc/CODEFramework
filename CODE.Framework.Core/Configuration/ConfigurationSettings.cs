using System.Collections.Generic;
using System.Linq;

namespace CODE.Framework.Core.Configuration
{
    /// <summary>
    /// The ConfigurationSettings class is the main point of access to an application settings
    /// </summary>
    public static class ConfigurationSettings
    {
        static ConfigurationSettings()
        {
            Settings = new Settings();
            Sources = new ConfigurationSettingsSourcesCollection
            {
                new MemorySettings(),
                new SecureConfigurationFile(),
                new DotNetConfigurationFile()
            };
        }

        /// <summary>
        /// Exposes access to the Settings.
        /// </summary>
        public static Settings Settings { get; private set; }

        /// <summary>
        /// Exposes access to the ConfigurationSettingsSourcesCollection.
        /// </summary>
        public static ConfigurationSettingsSourcesCollection Sources { get; private set; }

        /// <summary>
        /// Returns a complete collection of all keys across all sources
        /// </summary>
        /// <returns>List&lt;System.String&gt;.</returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public static List<string> GetAllKeys()
        {
            var keys = new List<string>();
            var allSources = Sources.GetAllSources();
            foreach (var source in allSources)
            {
                var allKeysInSource = source.Settings.GetAllKeys();
                foreach (var key in allKeysInSource.Where(key => !keys.Contains(key)))
                    keys.Add(key);
            }
            return keys.OrderBy(k => k).ToList();
        }
    }
}
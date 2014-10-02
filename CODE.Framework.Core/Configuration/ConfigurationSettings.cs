namespace CODE.Framework.Core.Configuration
{
    /// <summary>
    /// The ConfigurationSettings class is the main point of access to an application settings
    /// </summary>
    public static class ConfigurationSettings
    {
        static ConfigurationSettings()
        {
            Sources = new ConfigurationSettingsSourcesCollection {new MemorySettings(), new SecureConfigurationFile(), new DotNetConfigurationFile()};
            Settings = new Settings();
        }

        /// <summary>
        /// Exposes access to the Settings.
        /// </summary>
        public static Settings Settings { get; set; }

        /// <summary>
        /// Exposes access to the ConfigurationSettingsSourcesCollection.
        /// </summary>
        public static ConfigurationSettingsSourcesCollection Sources { get; private set; }
    }
}

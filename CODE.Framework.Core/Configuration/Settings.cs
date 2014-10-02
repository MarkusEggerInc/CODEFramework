using System.Linq;

namespace CODE.Framework.Core.Configuration
{
    /// <summary>
    /// Exposes settings inside the ConfigurationSettings class (which is the main class that uses 
    /// the Settings class). The Settings class doesn't actually store settings. Instead, it just
    /// exposes an interface for getting to settings in sources that were added to the 
    /// ConfigurationSettings class.
    /// </summary>
    public class Settings
    {
        /// <summary>
        /// Indexer that allows a setting to be accessed by its name. 
        /// </summary>
        public string this[string setting]
        {
            set
            {
                // Indicates if setting is supported.

                // Look for the setting through all sources.
                foreach (IConfigurationSource source in ConfigurationSettings.Sources)
                    if (source.IsActive)
                        // Check if setting is supported.
                        if (source.IsSettingSupported(setting))
                        {
                            // Check if source is readonly.
                            if (source.IsReadOnly) throw new SettingReadOnlyException();
                            // If setting is supported and not readonly, write new value to it.
                            source.Settings[setting] = value;

                            // We've found what we want, so just stop iterating through sources.
                            // By the specs, we go for the order sources were added.
                            break;
                        }

                // If the system is configured to have an in-memory source, we dynamically set the value in memory
                foreach (IConfigurationSource source in ConfigurationSettings.Sources)
                    if (source is MemorySettings)
                        source.Settings.Add(setting, value);
            }

            get
            {
                // Look for the setting through all sources.
                foreach (IConfigurationSource source in ConfigurationSettings.Sources)
                    if (source.IsActive)
                        if (source.IsSettingSupported(setting))
                            return source.Settings[setting].ToString();
                return string.Empty;
            }
        }

        /// <summary>
        /// Checks whether a given setting is supported by any source inside the ConfigurationSettings object.
        /// </summary>
        /// <param name="setting">Name of the setting.</param>
        /// <returns>True/False, indicating whether the setting is supported or not.</returns>
        public bool IsSettingSupported(string setting)
        {
            // Look for the setting through all sources.
            return ConfigurationSettings.Sources.Cast<IConfigurationSource>().Where(source => source.IsActive).Any(source => source.IsSettingSupported(setting));
        }
    }

    /// <summary>
    /// Enum with possible Configuration Source Types.
    /// </summary>
    public enum ConfigurationSourceTypes
    {
        /// <summary>User</summary>
        User,
        /// <summary>Machine</summary>
        Machine,
        /// <summary>System</summary>
        System,
        /// <summary>Network</summary>
        Network,
        /// <summary>Security</summary>
        Security,
        /// <summary>Other</summary>
        Other
    }

    /// <summary>
    /// Enum with possible Security Types.
    /// </summary>
    public enum SecurityType
    {
        /// <summary>Secure</summary>
        Secure,
        /// <summary>Non-Secure</summary>
        NonSecure
    }
}

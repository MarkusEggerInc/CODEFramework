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
                // Look for the setting through all sources.
                var source = ConfigurationSettings.Sources.Values.FirstOrDefault(s => s.IsActive && !s.IsReadOnly && s.IsSettingSupported(setting));
                if (source != null)
                {
                    source.Settings[setting] = value;
                    return;
                }

                // If the system is configured to have an in-memory source, we dynamically set the value in memory
                var inMemorySetting = ConfigurationSettings.Sources.Values.OfType<MemorySettings>().FirstOrDefault();
                if (inMemorySetting != null && !inMemorySetting.Settings.ContainsKey(setting))
                    inMemorySetting.Settings.Add(setting, value);
            }

            get
            {
                // Look for the setting through all sources.
                var source = ConfigurationSettings.Sources.Values.FirstOrDefault(s => s.IsActive && s.IsSettingSupported(setting));
                return source != null ? source.Settings[setting].ToString() : string.Empty;
            }
        }

        /// <summary>
        /// Checks whether a given setting is supported by any source inside the ConfigurationSettings object.
        /// </summary>
        /// <param name="setting">Name of the setting.</param>
        /// <returns>True/False, indicating whether the setting is supported or not.</returns>
        public bool IsSettingSupported(string setting)
        {
            return ConfigurationSettings.Sources.Values.Any(s => s.IsActive && s.IsSettingSupported(setting));
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

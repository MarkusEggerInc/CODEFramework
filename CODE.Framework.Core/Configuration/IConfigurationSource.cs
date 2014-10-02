namespace CODE.Framework.Core.Configuration
{
    /// <summary>
    /// Interface that determines everything a configuration source must expose. 
    /// There is a ConfigurationSource abstract class that implements this interface, giving 
    /// a starting point in case we need such class.
    /// </summary>
    public interface IConfigurationSource
    {
        /// <summary>
        /// Determines a friendly name for the source (such as "UserConfiguration", or "MachineConfiguration").
        /// </summary>
        string FriendlyName { get; }

        /// <summary>
        /// Checks whether a given setting is supported or not.
        /// </summary>
        /// <param name="settingName">The setting.</param>
        /// <returns>True/False for supported or not.</returns>
        bool IsSettingSupported(string settingName);

        /// <summary>
        /// Read settings from storage and put them in memory.
        /// </summary>
        void Read();

        /// <summary>
        /// Checks whether a given source type is supported.
        /// </summary>
        /// <param name="sourceType">The source type, according to enum ConfigurationSourceTypes.</param>
        /// <returns>True/False for supported or not.</returns>
        bool SupportsType(ConfigurationSourceTypes sourceType);

        /// <summary>
        /// Persists settings into storage.
        /// </summary>
        void Write();

        /// <summary>
        /// Indicates whether source is secure or not.
        /// </summary>
        bool IsSecure { get; }

        /// <summary>
        /// Indicates whether the source is ReadOnly, meaning that settings can be read, but cannot be written to.
        /// </summary>
        bool IsReadOnly { get; }

        /// <summary>
        /// Indicates whether the source is active (enabled)
        /// </summary>
        bool IsActive { get; set; }

        /// <summary>
        /// Keeps list of settings and its values.
        /// </summary>
        ConfigurationSourceSettings Settings { get; }

        /// <summary>
        /// Gets a value indicating whether the source has changed since 
        /// the last time it's been populated.
        /// </summary>
        bool IsDirty { get; }
    }
}

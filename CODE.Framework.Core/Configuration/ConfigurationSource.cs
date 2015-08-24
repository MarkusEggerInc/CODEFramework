namespace CODE.Framework.Core.Configuration
{
    /// <summary>
    /// This class implements the IConfigurationSource interface and it serves as the baseclass
    /// for the concrete "config source" classes.
    /// </summary>
    public abstract class ConfigurationSource : IConfigurationSource
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationSource"/> class.
        /// </summary>
        protected ConfigurationSource()
        {
            InternalSettings = new ConfigurationSourceSettings(this);
        }

        /// <summary>
        /// Used to mark the source as dirty.
        /// </summary>
        internal void MarkDirty()
        {
            IsDirty = true;
        }

        /// <summary>
        /// Reloads original settings for the configuration source and marks the source as not dirty.
        /// </summary>
        /// <param name="reloadSettings">Indicates whether the settings should be reloaded.</param>
        protected void SetNotDirty(bool reloadSettings)
        {
            IsDirty = false;
            if (reloadSettings) Read();
        }

        /// <summary>
        /// Determines a friendly name for the source (such as "UserConfiguration", or "MachineConfiguration").
        /// </summary>
        public abstract string FriendlyName
        {
            get;
        }

        /// <summary>
        /// Determines whether a given setting is supported by the class. The default behavior
        /// is provided, but the method is marked as virtual so that subclasses can provide their own
        /// implementation.
        /// </summary>
        /// <param name="settingName">The name of the setting.</param>
        /// <returns>True if the setting is supported, False if it is not.</returns>
        /// <example>
        /// // Look for setting in any source.
        /// bool supported = ConfigurationSettings.IsSettingsSupported("MySetting")
        /// // Look for setting only in a specific source.
        /// bool supported = ConfigurationSettings["Registry"].IsSettingsSupported("MySetting")
        /// </example>
        public virtual bool IsSettingSupported(string settingName)
        {
            lock (Settings)
                return Settings.ContainsKey(settingName);
        }

        /// <summary>
        /// Read persisted settings and place them in memory.
        /// </summary>
        public abstract void Read();

        /// <summary>
        /// Checks whether a given Source Type is supported.
        /// </summary>
        /// <param name="sourceType">The type of source being checked, according to enum EPS.Configuration.ConfigurationSourceTypes.</param>
        /// <returns>True or False, indicating whether or not the source type is supported.</returns>
        public abstract bool SupportsType(ConfigurationSourceTypes sourceType);

        /// <summary>
        /// Persists settings, taking it from memory to the storage being used.
        /// </summary>
        public abstract void Write();

        /// <summary>
        /// Indicates whether the source is considered secure or not.
        /// </summary>
        public abstract bool IsSecure { get; }

        /// <summary>
        /// Indicates whether the source is considered ReadOnly.
        /// </summary>
        public abstract bool IsReadOnly { get; }

        /// <summary>Exposes the Settings member.</summary>
        /// <example>
        /// var setting = ConfigurationSettings.Settings["MySetting"]
        /// </example>
        public virtual ConfigurationSourceSettings Settings
        {
            get { return InternalSettings; }
        }

        /// <summary>
        /// Indicates whether the source is active (enabled)
        /// </summary>
        public bool IsActive
        {
            get { lock (this) return _isActive; }
            set { lock (this) _isActive = value; }
        }

        /// <summary>
        /// Gets a value indicating whether the source has changed since
        /// the last time it's been populated.
        /// </summary>
        public bool IsDirty
        {
            get { lock (this) return _isDirty; }
            private set { lock (this) _isDirty = value; }
        }

        /// <summary>
        /// The internal settings
        /// </summary>
        protected ConfigurationSourceSettings InternalSettings;
        private bool _isActive = true;
        private bool _isDirty;
    }
}
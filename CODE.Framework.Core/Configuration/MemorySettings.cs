namespace CODE.Framework.Core.Configuration
{
    /// <summary>
    /// summary description for specialconfiguration.
    /// </summary>
    internal class MemorySettings : ConfigurationSource
    {
        /// <summary>
        /// Source's Friendly Name.
        /// </summary>
        public override string FriendlyName
        {
            get { return "Memory"; }
        }

        /// <summary>
        /// Determines whether source is secure.
        /// </summary>
        public override bool IsSecure
        {
            get { return true; }
        }

        /// <summary>
        /// Indicates if this source is read-only.
        /// </summary>
        public override bool IsReadOnly
        {
            get { return false; }
        }


        /// <summary>
        /// Read settings from file.
        /// </summary>
        public override void Read()
        {
            // No need to go to some other source here since we maintain all data internally
        }

        /// <summary>
        /// Write settings to file.
        /// </summary>
        public override void Write()
        {
            // Nowhere to write to
        }

        /// <summary>
        /// Checks whether a given type is supported.
        /// </summary>
        /// <param name="sourceType"></param>
        /// <returns></returns>
        public override bool SupportsType(ConfigurationSourceTypes sourceType)
        {
            return false;
        }

        /// <summary>
        /// Exposes the Settings member. We're shadowing that member here mostly because
        /// the Memory object is very specialized, designed to override temporarily whatever
        /// other sources might have the same setting. In order to give that special behavior,
        /// a new collection class has been created for it. 
        /// Notice that we still type the member as a ConfigurationSourceSettings class, 
        /// and the only difference is that we instantiate the ConfigurationSourceSettings class instead.
        /// </summary>
        public override ConfigurationSourceSettings Settings
        {
            get { return _settings ?? (_settings = new ConfigurationSourceMemorySettings(this)); }
        }

        /// <summary>
        /// Keeps an internal instance of the ConfigurationSourceSettings class.
        /// </summary>
        private ConfigurationSourceSettings _settings;
    }
}

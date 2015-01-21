namespace CODE.Framework.Core.Configuration
{
    /// <summary>
    /// summary description for specialconfiguration.
    /// </summary>
    internal class MemorySettings : ConfigurationSource
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MemorySettings"/> class.
        /// </summary>
        public MemorySettings()
        {
            InternalSettings.RemoveSettingWhenSetToNull = true;
        }

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
    }
}
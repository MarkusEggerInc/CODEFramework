namespace CODE.Framework.Core.Configuration
{
    /// <summary>
    /// Hashtable that keeps a Name-Value list of settings. This class is mainly used by the MemorySettings class.
    /// </summary>
    public class ConfigurationSourceMemorySettings : ConfigurationSourceSettings
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="parent">Collection this source belongs to</param>
        public ConfigurationSourceMemorySettings(IConfigurationSource parent) : base(parent) { }

        /// <summary>
        /// Indexer.
        /// </summary>
        public override object this[object key]
        {
            get { return base[key]; }
            set
            {
                // If a null value is being assigned to the setting,
                // we remove it (the setting) from the Memory source.
                if (value == null)
                    Remove(key);
                else
                    base[key] = value;
            }
        }
    }
}
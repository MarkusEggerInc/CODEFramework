namespace CODE.Framework.Core.Configuration
{
    /// <summary>
    /// Hashtable that keeps a Name-Value list of settings. This class is mainly used by the ConfigurationSource class.
    /// </summary>
    public class ConfigurationSourceSettings : System.Collections.Hashtable
    {
        /// <summary>
        /// Keeps a reference to the config source that hosts the settings collection.
        /// </summary>
        private readonly IConfigurationSource _parentConfigSource;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="parent">The parent configuration source that hosts this settings collection.</param>
        public ConfigurationSourceSettings(IConfigurationSource parent)
        {
            _parentConfigSource = parent;
        }

        /// <summary>
        /// Indexer.
        /// </summary>
        /// <remarks>
        /// The main reason this indexer is being overridden is so that we can
        /// flag as "dirty" the config source that hosts the settings.
        /// </remarks>
        public override object this[object key]
        {
            get { return base[key]; }
            set
            {
                base[key] = value;

                // We want to mark the source as "dirty".
                // We make sure the current config source inherits from our
                // concrete ConfigurationSource class, which has a MarkDirty method.
                // Other classes that just implements the IConfigurationSource interface
                // should be responsible for marking a source as dirty, since we 
                // probably don't want to make MarkDirty a public method on that interface.
                var concreteSource = _parentConfigSource as ConfigurationSource;
                if (concreteSource != null) concreteSource.MarkDirty();
            }
        }
    }
}

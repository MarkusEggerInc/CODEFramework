using System;
using System.Collections.Generic;

namespace CODE.Framework.Core.Configuration
{
    /// <summary>
    /// Hashtable that keeps a Name-Value list of settings. This class is mainly used by the ConfigurationSource class.
    /// </summary>
    public class ConfigurationSourceSettings
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="parent">The parent configuration source that hosts this settings collection.</param>
        /// <param name="maxSettingsCacheDuration">Maximum duration of the settings cache.</param>
        public ConfigurationSourceSettings(IConfigurationSource parent, TimeSpan maxSettingsCacheDuration)
        {
            _parentConfigSource = parent;
            _maxSettingsCacheDuration = maxSettingsCacheDuration;
        }
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="parent">The parent configuration source that hosts this settings collection.</param>
        public ConfigurationSourceSettings(IConfigurationSource parent)
        {
            _parentConfigSource = parent;
            _maxSettingsCacheDuration = TimeSpan.MaxValue;
        }

        private readonly IConfigurationSource _parentConfigSource;
        private readonly Dictionary<string, object> _settings = new Dictionary<string, object>();
        private readonly TimeSpan _maxSettingsCacheDuration;
        private DateTime _lastReadTimestamp = DateTime.MinValue;

        /// <summary>
        /// Applies the current date/time as the last read timestamp
        /// </summary>
        public void ApplyCurrentReadTimestamp()
        {
            _lastReadTimestamp = DateTime.Now;
        }

        /// <summary>
        /// Defines whether null values are automatically removed from the settings collection
        /// </summary>
        public bool RemoveSettingWhenSetToNull { get; set; }

        /// <summary>
        /// Indexer.
        /// </summary>
        /// <remarks>
        /// The main reason this indexer is being overridden is so that we can
        /// flag as "dirty" the config source that hosts the settings.
        /// </remarks>
        public virtual object this[string key]
        {
            get
            {
                if (_maxSettingsCacheDuration != TimeSpan.MaxValue && DateTime.Now > _lastReadTimestamp + _maxSettingsCacheDuration)
                    _parentConfigSource.Read();
                lock (this)
                    return _settings.ContainsKey(key) ? _settings[key] : null;
            }
            set
            {
                var valueChanged = false;
                lock (this)
                {
                    if (value == null && RemoveSettingWhenSetToNull)
                    {
                        if (_settings.ContainsKey(key))
                        {
                            _settings.Remove(key);
                            valueChanged = true;
                        }
                    }
                    else
                    {
                        if (_settings.ContainsKey(key))
                        {
                            if (_settings[key] != value)
                            {
                                _settings[key] = value;
                                valueChanged = true;
                            }
                        }
                        else
                        {
                            _settings.Add(key, value);
                            valueChanged = true;
                        }
                    }
                }

                // We want to mark the source as "dirty".
                // We make sure the current config source inherits from our
                // concrete ConfigurationSource class, which has a MarkDirty method.
                // Other classes that just implements the IConfigurationSource interface
                // should be responsible for marking a source as dirty, since we 
                // probably don't want to make MarkDirty a public method on that interface.
                if (valueChanged)
                {
                    var concreteSource = _parentConfigSource as ConfigurationSource;
                    if (concreteSource != null) concreteSource.MarkDirty();
                }
            }
        }

        /// <summary>
        /// Returns a copied list of all keys in a thread-safe way.
        /// </summary>
        /// <returns>List of all keys</returns>
        public string[] GetAllKeys()
        {
            lock (this)
            {
                var allKeys = new string[_settings.Keys.Count];
                _settings.Keys.CopyTo(allKeys, 0);
                return allKeys;
            }
        }

        /// <summary>
        /// Returns a copied list of all keys and values in a thread-safe way
        /// </summary>
        /// <returns>List of all keys and values</returns>
        public Dictionary<string, object> GetAllKeysAndValues()
        {
            var result = new Dictionary<string, object>();
            lock (this)
            {
                var allKeys = new string[_settings.Keys.Count];
                _settings.Keys.CopyTo(allKeys, 0);
                foreach (var key in allKeys)
                    result.Add(key, this[key]);
            }
            return result;
        }

        /// <summary>
        /// Determines whether the specified key is contained in the settings collection.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns><c>true</c> if the specified key is contained in the collection; otherwise, <c>false</c>.</returns>
        public bool ContainsKey(string key)
        {
            lock (this)
                return _settings.ContainsKey(key);
        }

        /// <summary>
        /// Adds the specified setting
        /// </summary>
        /// <param name="key">The key/setting name.</param>
        /// <param name="value">The value.</param>
        public void Add(string key, string value)
        {
            this[key] = value; // No lock needed here, since there already is a lock in the indexer
        }

        /// <summary>
        /// Clears the settings.
        /// </summary>
        public void Clear()
        {
            lock (this)
                _settings.Clear();
        }
    }
}
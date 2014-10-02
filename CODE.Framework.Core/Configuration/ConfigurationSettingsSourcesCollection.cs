using System;
using System.Collections;
using System.Linq;

namespace CODE.Framework.Core.Configuration
{
    /// <summary>
    /// Exposes ConfigurationSettingsSourcesCollection inside the ConfigurationSettings class (which is the main class that uses 
    /// the ConfigurationSettingsSourcesCollection class). The ConfigurationSettingsSourcesCollection class doesn't actually store ConfigurationSettingsSourcesCollection. Instead, it just
    /// exposes an interface for getting to ConfigurationSettingsSourcesCollection in sources that were added to the 
    /// ConfigurationSettings class.
    /// </summary>
    public class ConfigurationSettingsSourcesCollection : IEnumerable
    {
        /// <summary>
        /// For internal use only
        /// </summary>
        private readonly SourcesCollection _sources = new SourcesCollection();

        /// <summary>
        /// Indexer that allows a source to be accessed by its name. 
        /// </summary>
        public IConfigurationSource this[string sourceName]
        {
            get
            {
                return _sources.Cast<IConfigurationSource>().FirstOrDefault(source => source.FriendlyName == sourceName);
            }
        }

        /// <summary>
        /// Indexer that allows a source to be accessed by its index. 
        /// </summary>
        public IConfigurationSource this[int index]
        {
            get
            {
                if (((index >= 0) && (index <= _sources.Count))) return _sources.Item(index);
                throw new ArgumentOutOfRangeException(Properties.Resources.IndexNotInSources);
            }
        }

        /// <summary>
        /// Indicates how many sources exist in the collection.
        /// </summary>
        public int Count
        {
            get { return _sources.Count; }
        }


        /// <summary>
        /// Add sources to the collection.
        /// </summary>
        /// <param name="configurationSource">The source.</param>
        public void Add(IConfigurationSource configurationSource)
        {
            _sources.Add(configurationSource);
        }

        /// <summary>
        /// Implements IEnumerator.GetEnumerator.
        /// </summary>
        /// <returns></returns>
        public IEnumerator GetEnumerator()
        {
            return new SourceEnumerator(_sources);
        }

        /// <summary>
        /// Source enumerator class (for internal use only)
        /// </summary>
        private class SourceEnumerator : IEnumerator
        {
            /// <summary>
            /// For internal use only
            /// </summary>
            private int _intPosition = -1;

            /// <summary>
            /// For internal use only
            /// </summary>
            private readonly int _intInitialCount;

            /// <summary>
            /// For internal use only
            /// </summary>
            private readonly SourcesCollection _sources;

            public SourceEnumerator(SourcesCollection sourcesCollection)
            {
                _sources = sourcesCollection;
                _intInitialCount = sourcesCollection.Count;
            }

            /// <summary>
            /// Reset the enumeration
            /// </summary>
            public void Reset()
            {
                _intPosition = -1;
            }

            /// <summary>
            /// Current item
            /// </summary>
            public object Current
            {
                get
                {
                    if (_intInitialCount != _sources.Count)
                        throw new InvalidOperationException(Properties.Resources.EnumSourceChanged);
                    if (_intPosition >= _sources.Count)
                        throw new InvalidOperationException(Properties.Resources.EnumerationValueInvalid);
                    return _sources.Item(_intPosition);
                }
            }

            /// <summary>
            /// Move to next item
            /// </summary>
            /// <returns></returns>
            public bool MoveNext()
            {
                if (_intInitialCount == _sources.Count)
                {
                    _intPosition++;
                    return _intPosition < _sources.Count;
                }
                throw new InvalidOperationException(Properties.Resources.EnumSourceChanged);
            }
        }
    }
}
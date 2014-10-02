using System.Collections;

namespace CODE.Framework.Core.Configuration
{
    /// <summary>
    /// The SourcesCollection class is a typed-collection that contains objects that implement
    /// the IConfigurationSource interface. 
    /// </summary>
    public class SourcesCollection : CollectionBase
    {
        /// <summary>
        /// The Add method only accepts objects that implement the IConfigurationSource interface.
        /// </summary>
        /// <param name="configurationSource">A Configuration Source.</param>
        public void Add(IConfigurationSource configurationSource)
        {
            List.Add(configurationSource);
        }

        /// <summary>
        /// Removes a Configuration Source from the collection.
        /// </summary>
        /// <param name="index">Index of the source that must be removed.</param>
        public void Remove(int index)
        {
            // Check to see if there is a config source at the supplied index.
            if (index > Count - 1 || index < 0)
                // Throw an exception if index doesn't exist within the collection.
                throw new Exceptions.IndexOutOfBoundsException();
            List.RemoveAt(index);
        }

        /// <summary>
        /// Item returns a reference to a specific source.
        /// The item will always be retrieved as IConfigurationSource.
        /// </summary>
        /// <param name="index">Index of the item that's being accessed.</param>
        /// <returns>Reference to the item needed.</returns>
        public IConfigurationSource Item(int index)
        {
            // The appropriate item is retrieved from the List object and
            // explicitly cast to the IConfigurationSource type, then returned to the 
            // caller.
            return (IConfigurationSource)List[index];
        }
    }

}

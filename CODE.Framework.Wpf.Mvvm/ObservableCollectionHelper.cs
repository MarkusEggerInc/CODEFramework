using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;

namespace CODE.Framework.Wpf.Mvvm
{
    /// <summary>
    /// Helper functions for observable collections
    /// </summary>
    public static class ObservableCollectionHelper
    {
        /// <summary>
        /// Adds a range of object. Only fires the collection changed event once after all objects have been added
        /// </summary>
        /// <typeparam name="T">Item type</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="itemsToAdd">The items to add.</param>
        /// <exception cref="System.NullReferenceException">Unable to find Add method for observable collection.
        /// or
        /// Unable to find items collection for observable collection.</exception>
        public static void AddRange<T>(this ObservableCollection<T> collection, IEnumerable<T> itemsToAdd)
        {
            collection.AddRange(itemsToAdd.ToList());
        }

        /// <summary>
        /// Adds a range of object. Only fires the collection changed event once after all objects have been added
        /// </summary>
        /// <typeparam name="T">Item type</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="itemsToAdd">The items to add.</param>
        /// <exception cref="System.NullReferenceException">Unable to find Add method for observable collection.
        /// or
        /// Unable to find items collection for observable collection.</exception>
        public static void AddRange<T>(this ObservableCollection<T> collection, IQueryable<T> itemsToAdd)
        {
            collection.AddRange(itemsToAdd.ToList());
        }

        /// <summary>
        /// Adds a range of object. Only fires the collection changed event once after all objects have been added
        /// </summary>
        /// <typeparam name="T">Item type</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="itemsToAdd">The items to add.</param>
        /// <exception cref="System.NullReferenceException">
        /// Unable to find Add method for observable collection.
        /// or
        /// Unable to find items collection for observable collection.
        /// </exception>
        public static void AddRange<T>(this ObservableCollection<T> collection, IList<T> itemsToAdd)
        {
            if (itemsToAdd.Count < 1) return; // Nothing to do, so no need to take any overhead hit

            var itemsCollection = typeof(ObservableCollection<T>).GetProperty("Items", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(collection, null) as ICollection<T>;

            if (itemsCollection != null)
                foreach (var item in itemsToAdd)
                    itemsCollection.Add(item);

            var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
            GetCollectionChangedMethod<T>().Invoke(collection, new object[] {args});
        }

        private static readonly Dictionary<Type, MethodInfo> OnCollectionChangedMethodCollections = new Dictionary<Type, MethodInfo>();
        /// <summary>Returns collection changed method for a specific item in a cached fashion</summary>
        /// <typeparam name="T"></typeparam>
        /// <returns>MethodInfo.</returns>
        /// <exception cref="System.ArgumentNullException">OnCollectionChanged method not found on observable collection!</exception>
        private static MethodInfo GetCollectionChangedMethod<T>()
        {
            if (OnCollectionChangedMethodCollections.ContainsKey(typeof (T)))
                return OnCollectionChangedMethodCollections[typeof (T)];

            var methods = typeof(ObservableCollection<T>).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic).Where(m => (m.Name == "OnCollectionChanged") && (m.Attributes & MethodAttributes.Virtual) == MethodAttributes.Virtual);
            var method = methods.FirstOrDefault();
            if (method != null)
            {
                OnCollectionChangedMethodCollections.Add(typeof (T), method);
                return method;
            }
            throw new ArgumentNullException("OnCollectionChanged method not found on observable collection!");
        }
    }
}

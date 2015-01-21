using System;
using System.Collections;
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

            var itemsCollection = typeof (ObservableCollection<T>).GetProperty("Items", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(collection, null) as ICollection<T>;

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

            var methods = typeof (ObservableCollection<T>).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic).Where(m => (m.Name == "OnCollectionChanged") && (m.Attributes & MethodAttributes.Virtual) == MethodAttributes.Virtual);
            var method = methods.FirstOrDefault();
            if (method != null)
            {
                OnCollectionChangedMethodCollections.Add(typeof (T), method);
                return method;
            }
            throw new ArgumentNullException("OnCollectionChanged method not found on observable collection!");
        }

        /// <summary>
        /// Synchronizes the items of two different observable collections of different item types
        /// </summary>
        /// <remarks>The types of both collections must be castable (TSource must be a TTarget)</remarks>
        /// <typeparam name="TSource">The type of the t source.</typeparam>
        /// <typeparam name="TTarget">The type of the t target.</typeparam>
        /// <param name="sourceCollection">The source collection.</param>
        /// <param name="targetCollection">The target collection.</param>
        public static void Sync<TSource, TTarget>(this ObservableCollection<TSource> sourceCollection, ObservableCollection<TTarget> targetCollection) where TSource : TTarget
        {
            if (sourceCollection == null || targetCollection == null) return;

            if (_synchedCollections == null) _synchedCollections = new Dictionary<object, List<object>>();

            targetCollection.Clear();
            foreach (var item in sourceCollection)
                if (!targetCollection.Contains(item))
                    targetCollection.Add(item);

            if (!_synchedCollections.ContainsKey(sourceCollection))
            {
                sourceCollection.CollectionChanged += SyncCollectionEventHandler;
                _synchedCollections.Add(sourceCollection, new List<object> {targetCollection});
            }
            else if (!_synchedCollections[sourceCollection].Contains(targetCollection))
                _synchedCollections[sourceCollection].Add(targetCollection);
        }

        /// <summary>
        /// Removes a previously configured sync between collections
        /// </summary>
        /// <typeparam name="TSource">The type of the t source.</typeparam>
        /// <typeparam name="TTarget">The type of the t target.</typeparam>
        /// <param name="sourceCollection">The source collection.</param>
        /// <param name="targetCollection">The target collection.</param>
        public static void RemoveSync<TSource, TTarget>(this ObservableCollection<TSource> sourceCollection, ObservableCollection<TTarget> targetCollection) where TSource : TTarget
        {
            if (_synchedCollections == null) return;
            if (!_synchedCollections.ContainsKey(sourceCollection)) return;
            if (_synchedCollections[sourceCollection].Contains(targetCollection)) _synchedCollections[sourceCollection].Remove(targetCollection);
            if (_synchedCollections[sourceCollection].Count == 0)
            {
                sourceCollection.CollectionChanged -= SyncCollectionEventHandler;
                _synchedCollections.Remove(sourceCollection);
            }
        }

        private static Dictionary<object, List<object>> _synchedCollections;

        private static void SyncCollectionEventHandler(object source, NotifyCollectionChangedEventArgs e)
        {
            if (_synchedCollections == null) return;
            var sourceCollection = source as IList;
            if (sourceCollection == null) return;
            if (!_synchedCollections.ContainsKey(sourceCollection)) return; // We are apparently not interested in this collection anymore

            foreach (var target in _synchedCollections[sourceCollection])
            {
                var targetCollection = target as IList;
                if (targetCollection == null) continue;

                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        foreach (var item in e.NewItems)
                            targetCollection.Add(item);
                        break;
                    case NotifyCollectionChangedAction.Remove:
                        foreach (var item in e.OldItems)
                            if (targetCollection.Contains(item))
                                targetCollection.Remove(item);
                        break;
                    case NotifyCollectionChangedAction.Reset:
                        targetCollection.Clear();
                            foreach (var item in sourceCollection)
                                targetCollection.Add(item);
                        break;
                }
            }
        }
    }
}
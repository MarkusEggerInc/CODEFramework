#region License

// Copyright (c) 2007 James Newton-King
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.

#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Threading;
using CODE.Framework.Core.Newtonsoft.Utilities;

namespace CODE.Framework.Core.Newtonsoft.Linq
{
    /// <summary>
    ///     Represents a token that can contain other tokens.
    /// </summary>
    public abstract class JContainer : JToken, IList<JToken>, ITypedList, IBindingList, IList, INotifyCollectionChanged
    {
        internal AddingNewEventHandler _addingNew;
        private bool _busy;

        internal NotifyCollectionChangedEventHandler _collectionChanged;
        internal ListChangedEventHandler _listChanged;

        private object _syncRoot;

        internal JContainer()
        {
        }

        internal JContainer(JContainer other)
            : this()
        {
            ValidationUtils.ArgumentNotNull(other, nameof(other));

            var i = 0;
            foreach (var child in other)
            {
                AddInternal(i, child, false);
                i++;
            }
        }

        /// <summary>
        ///     Gets the container's children tokens.
        /// </summary>
        /// <value>The container's children tokens.</value>
        protected abstract IList<JToken> ChildrenTokens { get; }

        /// <summary>
        ///     Gets a value indicating whether this token has child tokens.
        /// </summary>
        /// <value>
        ///     <c>true</c> if this token has child values; otherwise, <c>false</c>.
        /// </value>
        public override bool HasValues
        {
            get { return ChildrenTokens.Count > 0; }
        }

        /// <summary>
        ///     Get the first child token of this token.
        /// </summary>
        /// <value>
        ///     A <see cref="JToken" /> containing the first child token of the <see cref="JToken" />.
        /// </value>
        public override JToken First
        {
            get
            {
                var children = ChildrenTokens;
                return children.Count > 0 ? children[0] : null;
            }
        }

        /// <summary>
        ///     Get the last child token of this token.
        /// </summary>
        /// <value>
        ///     A <see cref="JToken" /> containing the last child token of the <see cref="JToken" />.
        /// </value>
        public override JToken Last
        {
            get
            {
                var children = ChildrenTokens;
                var count = children.Count;
                return count > 0 ? children[count - 1] : null;
            }
        }

        /// <summary>
        ///     Occurs when the list changes or an item in the list changes.
        /// </summary>
        public event ListChangedEventHandler ListChanged
        {
            add { _listChanged += value; }
            remove { _listChanged -= value; }
        }

        /// <summary>
        ///     Occurs when the items list of the collection has changed, or the collection is reset.
        /// </summary>
        public event NotifyCollectionChangedEventHandler CollectionChanged
        {
            add { _collectionChanged += value; }
            remove { _collectionChanged -= value; }
        }

        string ITypedList.GetListName(PropertyDescriptor[] listAccessors)
        {
            return string.Empty;
        }

        PropertyDescriptorCollection ITypedList.GetItemProperties(PropertyDescriptor[] listAccessors)
        {
            var d = First as ICustomTypeDescriptor;
            return d?.GetProperties();
        }

        /// <summary>
        ///     Occurs before an item is added to the collection.
        /// </summary>
        public event AddingNewEventHandler AddingNew
        {
            add { _addingNew += value; }
            remove { _addingNew -= value; }
        }

        internal void CheckReentrancy()
        {
            if (_busy)
                throw new InvalidOperationException("Cannot change {0} during a collection change event.".FormatWith(CultureInfo.InvariantCulture, GetType()));
        }

        internal virtual IList<JToken> CreateChildrenCollection()
        {
            return new List<JToken>();
        }

        /// <summary>
        ///     Raises the <see cref="AddingNew" /> event.
        /// </summary>
        /// <param name="e">The <see cref="AddingNewEventArgs" /> instance containing the event data.</param>
        protected virtual void OnAddingNew(AddingNewEventArgs e)
        {
            _addingNew?.Invoke(this, e);
        }

        /// <summary>
        ///     Raises the <see cref="ListChanged" /> event.
        /// </summary>
        /// <param name="e">The <see cref="ListChangedEventArgs" /> instance containing the event data.</param>
        protected virtual void OnListChanged(ListChangedEventArgs e)
        {
            var handler = _listChanged;

            if (handler != null)
            {
                _busy = true;
                try
                {
                    handler(this, e);
                }
                finally
                {
                    _busy = false;
                }
            }
        }

        /// <summary>
        ///     Raises the <see cref="CollectionChanged" /> event.
        /// </summary>
        /// <param name="e">The <see cref="NotifyCollectionChangedEventArgs" /> instance containing the event data.</param>
        protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            var handler = _collectionChanged;

            if (handler != null)
            {
                _busy = true;
                try
                {
                    handler(this, e);
                }
                finally
                {
                    _busy = false;
                }
            }
        }

        internal bool ContentsEqual(JContainer container)
        {
            if (container == this)
                return true;

            var t1 = ChildrenTokens;
            var t2 = container.ChildrenTokens;

            if (t1.Count != t2.Count)
                return false;

            for (var i = 0; i < t1.Count; i++)
                if (!t1[i].DeepEquals(t2[i]))
                    return false;

            return true;
        }

        /// <summary>
        ///     Returns a collection of the child tokens of this token, in document order.
        /// </summary>
        /// <returns>
        ///     An <see cref="IEnumerable{T}" /> of <see cref="JToken" /> containing the child tokens of this <see cref="JToken" />
        ///     , in document order.
        /// </returns>
        public override JEnumerable<JToken> Children()
        {
            return new JEnumerable<JToken>(ChildrenTokens);
        }

        /// <summary>
        ///     Returns a collection of the child values of this token, in document order.
        /// </summary>
        /// <typeparam name="T">The type to convert the values to.</typeparam>
        /// <returns>
        ///     A <see cref="IEnumerable{T}" /> containing the child values of this <see cref="JToken" />, in document order.
        /// </returns>
        public override IEnumerable<T> Values<T>()
        {
            return ChildrenTokens.Convert<JToken, T>();
        }

        /// <summary>
        ///     Returns a collection of the descendant tokens for this token in document order.
        /// </summary>
        /// <returns>
        ///     An <see cref="IEnumerable{T}" /> of <see cref="JToken" /> containing the descendant tokens of the
        ///     <see cref="JToken" />.
        /// </returns>
        public IEnumerable<JToken> Descendants()
        {
            return GetDescendants(false);
        }

        /// <summary>
        ///     Returns a collection of the tokens that contain this token, and all descendant tokens of this token, in document
        ///     order.
        /// </summary>
        /// <returns>
        ///     An <see cref="IEnumerable{T}" /> of <see cref="JToken" /> containing this token, and all the descendant tokens
        ///     of the <see cref="JToken" />.
        /// </returns>
        public IEnumerable<JToken> DescendantsAndSelf()
        {
            return GetDescendants(true);
        }

        internal IEnumerable<JToken> GetDescendants(bool self)
        {
            if (self)
                yield return this;

            foreach (var o in ChildrenTokens)
            {
                yield return o;
                var c = o as JContainer;
                if (c != null)
                    foreach (var d in c.Descendants())
                        yield return d;
            }
        }

        internal bool IsMultiContent(object content)
        {
            return content is IEnumerable && !(content is string) && !(content is JToken) && !(content is byte[]);
        }

        internal JToken EnsureParentToken(JToken item, bool skipParentCheck)
        {
            if (item == null)
                return JValue.CreateNull();

            if (skipParentCheck)
                return item;

            // to avoid a token having multiple parents or creating a recursive loop, create a copy if...
            // the item already has a parent
            // the item is being added to itself
            // the item is being added to the root parent of itself
            if (item.Parent != null || item == this || item.HasValues && Root == item)
                item = item.CloneToken();

            return item;
        }

        internal abstract int IndexOfItem(JToken item);

        internal virtual void InsertItem(int index, JToken item, bool skipParentCheck)
        {
            var children = ChildrenTokens;

            if (index > children.Count)
                throw new ArgumentOutOfRangeException(nameof(index), "Index must be within the bounds of the List.");

            CheckReentrancy();

            item = EnsureParentToken(item, skipParentCheck);

            var previous = index == 0 ? null : children[index - 1];
            // haven't inserted new token yet so next token is still at the inserting index
            var next = index == children.Count ? null : children[index];

            ValidateToken(item, null);

            item.Parent = this;

            item.Previous = previous;
            if (previous != null)
                previous.Next = item;

            item.Next = next;
            if (next != null)
                next.Previous = item;

            children.Insert(index, item);

            if (_listChanged != null)
                OnListChanged(new ListChangedEventArgs(ListChangedType.ItemAdded, index));
            if (_collectionChanged != null)
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
        }

        internal virtual void RemoveItemAt(int index)
        {
            var children = ChildrenTokens;

            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index), "Index is less than 0.");
            if (index >= children.Count)
                throw new ArgumentOutOfRangeException(nameof(index), "Index is equal to or greater than Count.");

            CheckReentrancy();

            var item = children[index];
            var previous = index == 0 ? null : children[index - 1];
            var next = index == children.Count - 1 ? null : children[index + 1];

            if (previous != null)
                previous.Next = next;
            if (next != null)
                next.Previous = previous;

            item.Parent = null;
            item.Previous = null;
            item.Next = null;

            children.RemoveAt(index);

            if (_listChanged != null)
                OnListChanged(new ListChangedEventArgs(ListChangedType.ItemDeleted, index));
            if (_collectionChanged != null)
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index));
        }

        internal virtual bool RemoveItem(JToken item)
        {
            var index = IndexOfItem(item);
            if (index >= 0)
            {
                RemoveItemAt(index);
                return true;
            }

            return false;
        }

        internal virtual JToken GetItem(int index)
        {
            return ChildrenTokens[index];
        }

        internal virtual void SetItem(int index, JToken item)
        {
            var children = ChildrenTokens;

            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index), "Index is less than 0.");
            if (index >= children.Count)
                throw new ArgumentOutOfRangeException(nameof(index), "Index is equal to or greater than Count.");

            var existing = children[index];

            if (IsTokenUnchanged(existing, item))
                return;

            CheckReentrancy();

            item = EnsureParentToken(item, false);

            ValidateToken(item, existing);

            var previous = index == 0 ? null : children[index - 1];
            var next = index == children.Count - 1 ? null : children[index + 1];

            item.Parent = this;

            item.Previous = previous;
            if (previous != null)
                previous.Next = item;

            item.Next = next;
            if (next != null)
                next.Previous = item;

            children[index] = item;

            existing.Parent = null;
            existing.Previous = null;
            existing.Next = null;

            if (_listChanged != null)
                OnListChanged(new ListChangedEventArgs(ListChangedType.ItemChanged, index));
            if (_collectionChanged != null)
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, item, existing, index));
        }

        internal virtual void ClearItems()
        {
            CheckReentrancy();

            var children = ChildrenTokens;

            foreach (var item in children)
            {
                item.Parent = null;
                item.Previous = null;
                item.Next = null;
            }

            children.Clear();

            if (_listChanged != null)
                OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
            if (_collectionChanged != null)
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        internal virtual void ReplaceItem(JToken existing, JToken replacement)
        {
            if (existing == null || existing.Parent != this)
                return;

            var index = IndexOfItem(existing);
            SetItem(index, replacement);
        }

        internal virtual bool ContainsItem(JToken item)
        {
            return IndexOfItem(item) != -1;
        }

        internal virtual void CopyItemsTo(Array array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (arrayIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex), "arrayIndex is less than 0.");
            if (arrayIndex >= array.Length && arrayIndex != 0)
                throw new ArgumentException("arrayIndex is equal to or greater than the length of array.");
            if (Count > array.Length - arrayIndex)
                throw new ArgumentException("The number of elements in the source JObject is greater than the available space from arrayIndex to the end of the destination array.");

            var index = 0;
            foreach (var token in ChildrenTokens)
            {
                array.SetValue(token, arrayIndex + index);
                index++;
            }
        }

        internal static bool IsTokenUnchanged(JToken currentValue, JToken newValue)
        {
            var v1 = currentValue as JValue;
            if (v1 != null)
            {
                // null will get turned into a JValue of type null
                if (v1.Type == JTokenType.Null && newValue == null)
                    return true;

                return v1.Equals(newValue);
            }

            return false;
        }

        internal virtual void ValidateToken(JToken o, JToken existing)
        {
            ValidationUtils.ArgumentNotNull(o, nameof(o));

            if (o.Type == JTokenType.Property)
                throw new ArgumentException("Can not add {0} to {1}.".FormatWith(CultureInfo.InvariantCulture, o.GetType(), GetType()));
        }

        /// <summary>
        ///     Adds the specified content as children of this <see cref="JToken" />.
        /// </summary>
        /// <param name="content">The content to be added.</param>
        public virtual void Add(object content)
        {
            AddInternal(ChildrenTokens.Count, content, false);
        }

        internal void AddAndSkipParentCheck(JToken token)
        {
            AddInternal(ChildrenTokens.Count, token, true);
        }

        /// <summary>
        ///     Adds the specified content as the first children of this <see cref="JToken" />.
        /// </summary>
        /// <param name="content">The content to be added.</param>
        public void AddFirst(object content)
        {
            AddInternal(0, content, false);
        }

        internal void AddInternal(int index, object content, bool skipParentCheck)
        {
            if (IsMultiContent(content))
            {
                var enumerable = (IEnumerable) content;

                var multiIndex = index;
                foreach (var c in enumerable)
                {
                    AddInternal(multiIndex, c, skipParentCheck);
                    multiIndex++;
                }
            }
            else
            {
                var item = CreateFromContent(content);

                InsertItem(index, item, skipParentCheck);
            }
        }

        internal static JToken CreateFromContent(object content)
        {
            var token = content as JToken;
            if (token != null)
                return token;

            return new JValue(content);
        }

        /// <summary>
        ///     Creates a <see cref="JsonWriter" /> that can be used to add tokens to the <see cref="JToken" />.
        /// </summary>
        /// <returns>A <see cref="JsonWriter" /> that is ready to have content written to it.</returns>
        public JsonWriter CreateWriter()
        {
            return new JTokenWriter(this);
        }

        /// <summary>
        ///     Replaces the child nodes of this token with the specified content.
        /// </summary>
        /// <param name="content">The content.</param>
        public void ReplaceAll(object content)
        {
            ClearItems();
            Add(content);
        }

        /// <summary>
        ///     Removes the child nodes from this token.
        /// </summary>
        public void RemoveAll()
        {
            ClearItems();
        }

        internal abstract void MergeItem(object content, JsonMergeSettings settings);

        /// <summary>
        ///     Merge the specified content into this <see cref="JToken" />.
        /// </summary>
        /// <param name="content">The content to be merged.</param>
        public void Merge(object content)
        {
            MergeItem(content, new JsonMergeSettings());
        }

        /// <summary>
        ///     Merge the specified content into this <see cref="JToken" /> using <see cref="JsonMergeSettings" />.
        /// </summary>
        /// <param name="content">The content to be merged.</param>
        /// <param name="settings">The <see cref="JsonMergeSettings" /> used to merge the content.</param>
        public void Merge(object content, JsonMergeSettings settings)
        {
            MergeItem(content, settings);
        }

        internal void ReadTokenFrom(JsonReader reader, JsonLoadSettings options)
        {
            var startDepth = reader.Depth;

            if (!reader.Read())
                throw JsonReaderException.Create(reader, "Error reading {0} from JsonReader.".FormatWith(CultureInfo.InvariantCulture, GetType().Name));

            ReadContentFrom(reader, options);

            var endDepth = reader.Depth;

            if (endDepth > startDepth)
                throw JsonReaderException.Create(reader, "Unexpected end of content while loading {0}.".FormatWith(CultureInfo.InvariantCulture, GetType().Name));
        }

        internal void ReadContentFrom(JsonReader r, JsonLoadSettings settings)
        {
            ValidationUtils.ArgumentNotNull(r, nameof(r));
            var lineInfo = r as IJsonLineInfo;

            var parent = this;

            do
            {
                if ((parent as JProperty)?.Value != null)
                {
                    if (parent == this)
                        return;

                    parent = parent.Parent;
                }

                switch (r.TokenType)
                {
                    case JsonToken.None:
                        // new reader. move to actual content
                        break;
                    case JsonToken.StartArray:
                        var a = new JArray();
                        a.SetLineInfo(lineInfo, settings);
                        parent.Add(a);
                        parent = a;
                        break;

                    case JsonToken.EndArray:
                        if (parent == this)
                            return;

                        parent = parent.Parent;
                        break;
                    case JsonToken.StartObject:
                        var o = new JObject();
                        o.SetLineInfo(lineInfo, settings);
                        parent.Add(o);
                        parent = o;
                        break;
                    case JsonToken.EndObject:
                        if (parent == this)
                            return;

                        parent = parent.Parent;
                        break;
                    case JsonToken.StartConstructor:
                        var constructor = new JConstructor(r.Value.ToString());
                        constructor.SetLineInfo(lineInfo, settings);
                        parent.Add(constructor);
                        parent = constructor;
                        break;
                    case JsonToken.EndConstructor:
                        if (parent == this)
                            return;

                        parent = parent.Parent;
                        break;
                    case JsonToken.String:
                    case JsonToken.Integer:
                    case JsonToken.Float:
                    case JsonToken.Date:
                    case JsonToken.Boolean:
                    case JsonToken.Bytes:
                        var v = new JValue(r.Value);
                        v.SetLineInfo(lineInfo, settings);
                        parent.Add(v);
                        break;
                    case JsonToken.Comment:
                        if (settings != null && settings.CommentHandling == CommentHandling.Load)
                        {
                            v = JValue.CreateComment(r.Value.ToString());
                            v.SetLineInfo(lineInfo, settings);
                            parent.Add(v);
                        }
                        break;
                    case JsonToken.Null:
                        v = JValue.CreateNull();
                        v.SetLineInfo(lineInfo, settings);
                        parent.Add(v);
                        break;
                    case JsonToken.Undefined:
                        v = JValue.CreateUndefined();
                        v.SetLineInfo(lineInfo, settings);
                        parent.Add(v);
                        break;
                    case JsonToken.PropertyName:
                        var propertyName = r.Value.ToString();
                        var property = new JProperty(propertyName);
                        property.SetLineInfo(lineInfo, settings);
                        var parentObject = (JObject) parent;
                        // handle multiple properties with the same name in JSON
                        var existingPropertyWithName = parentObject.Property(propertyName);
                        if (existingPropertyWithName == null)
                            parent.Add(property);
                        else
                            existingPropertyWithName.Replace(property);
                        parent = property;
                        break;
                    default:
                        throw new InvalidOperationException("The JsonReader should not be on a token of type {0}.".FormatWith(CultureInfo.InvariantCulture, r.TokenType));
                }
            } while (r.Read());
        }

        internal int ContentsHashCode()
        {
            var hashCode = 0;
            foreach (var item in ChildrenTokens)
                hashCode ^= item.GetDeepHashCode();
            return hashCode;
        }

        private JToken EnsureValue(object value)
        {
            if (value == null)
                return null;

            var token = value as JToken;
            if (token != null)
                return token;

            throw new ArgumentException("Argument is not a JToken.");
        }

        internal static void MergeEnumerableContent(JContainer target, IEnumerable content, JsonMergeSettings settings)
        {
            switch (settings.MergeArrayHandling)
            {
                case MergeArrayHandling.Concat:
                    foreach (JToken item in content)
                        target.Add(item);
                    break;
                case MergeArrayHandling.Union:
                    var items = new HashSet<JToken>(target, EqualityComparer);

                    foreach (JToken item in content)
                        if (items.Add(item))
                            target.Add(item);
                    break;
                case MergeArrayHandling.Replace:
                    target.ClearItems();
                    foreach (JToken item in content)
                        target.Add(item);
                    break;
                case MergeArrayHandling.Merge:
                    var i = 0;
                    foreach (var targetItem in content)
                    {
                        if (i < target.Count)
                        {
                            var sourceItem = target[i];

                            var existingContainer = sourceItem as JContainer;
                            if (existingContainer != null)
                            {
                                existingContainer.Merge(targetItem, settings);
                            }
                            else
                            {
                                if (targetItem != null)
                                {
                                    var contentValue = CreateFromContent(targetItem);
                                    if (contentValue.Type != JTokenType.Null)
                                        target[i] = contentValue;
                                }
                            }
                        }
                        else
                        {
                            target.Add(targetItem);
                        }

                        i++;
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(settings), "Unexpected merge array handling when merging JSON.");
            }
        }

        #region IList<JToken> Members

        int IList<JToken>.IndexOf(JToken item)
        {
            return IndexOfItem(item);
        }

        void IList<JToken>.Insert(int index, JToken item)
        {
            InsertItem(index, item, false);
        }

        void IList<JToken>.RemoveAt(int index)
        {
            RemoveItemAt(index);
        }

        JToken IList<JToken>.this[int index]
        {
            get { return GetItem(index); }
            set { SetItem(index, value); }
        }

        #endregion

        #region ICollection<JToken> Members

        void ICollection<JToken>.Add(JToken item)
        {
            Add(item);
        }

        void ICollection<JToken>.Clear()
        {
            ClearItems();
        }

        bool ICollection<JToken>.Contains(JToken item)
        {
            return ContainsItem(item);
        }

        void ICollection<JToken>.CopyTo(JToken[] array, int arrayIndex)
        {
            CopyItemsTo(array, arrayIndex);
        }

        bool ICollection<JToken>.IsReadOnly
        {
            get { return false; }
        }

        bool ICollection<JToken>.Remove(JToken item)
        {
            return RemoveItem(item);
        }

        #endregion

        #region IList Members

        int IList.Add(object value)
        {
            Add(EnsureValue(value));
            return Count - 1;
        }

        void IList.Clear()
        {
            ClearItems();
        }

        bool IList.Contains(object value)
        {
            return ContainsItem(EnsureValue(value));
        }

        int IList.IndexOf(object value)
        {
            return IndexOfItem(EnsureValue(value));
        }

        void IList.Insert(int index, object value)
        {
            InsertItem(index, EnsureValue(value), false);
        }

        bool IList.IsFixedSize
        {
            get { return false; }
        }

        bool IList.IsReadOnly
        {
            get { return false; }
        }

        void IList.Remove(object value)
        {
            RemoveItem(EnsureValue(value));
        }

        void IList.RemoveAt(int index)
        {
            RemoveItemAt(index);
        }

        object IList.this[int index]
        {
            get { return GetItem(index); }
            set { SetItem(index, EnsureValue(value)); }
        }

        #endregion

        #region ICollection Members

        void ICollection.CopyTo(Array array, int index)
        {
            CopyItemsTo(array, index);
        }

        /// <summary>
        ///     Gets the count of child JSON tokens.
        /// </summary>
        /// <value>The count of child JSON tokens.</value>
        public int Count
        {
            get { return ChildrenTokens.Count; }
        }

        bool ICollection.IsSynchronized
        {
            get { return false; }
        }

        object ICollection.SyncRoot
        {
            get
            {
                if (_syncRoot == null)
                    Interlocked.CompareExchange(ref _syncRoot, new object(), null);

                return _syncRoot;
            }
        }

        #endregion

        #region IBindingList Members

        void IBindingList.AddIndex(PropertyDescriptor property)
        {
        }

        object IBindingList.AddNew()
        {
            var args = new AddingNewEventArgs();
            OnAddingNew(args);

            if (args.NewObject == null)
                throw new JsonException("Could not determine new value to add to '{0}'.".FormatWith(CultureInfo.InvariantCulture, GetType()));

            if (!(args.NewObject is JToken))
                throw new JsonException("New item to be added to collection must be compatible with {0}.".FormatWith(CultureInfo.InvariantCulture, typeof(JToken)));

            var newItem = (JToken) args.NewObject;
            Add(newItem);

            return newItem;
        }

        bool IBindingList.AllowEdit
        {
            get { return true; }
        }

        bool IBindingList.AllowNew
        {
            get { return true; }
        }

        bool IBindingList.AllowRemove
        {
            get { return true; }
        }

        void IBindingList.ApplySort(PropertyDescriptor property, ListSortDirection direction)
        {
            throw new NotSupportedException();
        }

        int IBindingList.Find(PropertyDescriptor property, object key)
        {
            throw new NotSupportedException();
        }

        bool IBindingList.IsSorted
        {
            get { return false; }
        }

        void IBindingList.RemoveIndex(PropertyDescriptor property)
        {
        }

        void IBindingList.RemoveSort()
        {
            throw new NotSupportedException();
        }

        ListSortDirection IBindingList.SortDirection
        {
            get { return ListSortDirection.Ascending; }
        }

        PropertyDescriptor IBindingList.SortProperty
        {
            get { return null; }
        }

        bool IBindingList.SupportsChangeNotification
        {
            get { return true; }
        }

        bool IBindingList.SupportsSearching
        {
            get { return false; }
        }

        bool IBindingList.SupportsSorting
        {
            get { return false; }
        }

        #endregion
    }
}
﻿#region License
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
using System.Diagnostics;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Numerics;
using CODE.Framework.Core.Newtonsoft.Linq.JsonPath;
using CODE.Framework.Core.Newtonsoft.Utilities;

namespace CODE.Framework.Core.Newtonsoft.Linq
{
    /// <summary>
    /// Represents an abstract JSON token.
    /// </summary>
    public abstract class JToken : IJEnumerable<JToken>, IJsonLineInfo, ICloneable, IDynamicMetaObjectProvider
    {
        private static JTokenEqualityComparer _equalityComparer;

        private JContainer _parent;
        private object _annotations;

        private static readonly JTokenType[] BooleanTypes = {JTokenType.Integer, JTokenType.Float, JTokenType.String, JTokenType.Comment, JTokenType.Raw, JTokenType.Boolean};
        private static readonly JTokenType[] NumberTypes = {JTokenType.Integer, JTokenType.Float, JTokenType.String, JTokenType.Comment, JTokenType.Raw, JTokenType.Boolean};
        private static readonly JTokenType[] BigIntegerTypes = {JTokenType.Integer, JTokenType.Float, JTokenType.String, JTokenType.Comment, JTokenType.Raw, JTokenType.Boolean, JTokenType.Bytes};
        private static readonly JTokenType[] StringTypes = {JTokenType.Date, JTokenType.Integer, JTokenType.Float, JTokenType.String, JTokenType.Comment, JTokenType.Raw, JTokenType.Boolean, JTokenType.Bytes, JTokenType.Guid, JTokenType.TimeSpan, JTokenType.Uri};
        private static readonly JTokenType[] GuidTypes = {JTokenType.String, JTokenType.Comment, JTokenType.Raw, JTokenType.Guid, JTokenType.Bytes};
        private static readonly JTokenType[] TimeSpanTypes = {JTokenType.String, JTokenType.Comment, JTokenType.Raw, JTokenType.TimeSpan};
        private static readonly JTokenType[] UriTypes = {JTokenType.String, JTokenType.Comment, JTokenType.Raw, JTokenType.Uri};
        private static readonly JTokenType[] CharTypes = {JTokenType.Integer, JTokenType.Float, JTokenType.String, JTokenType.Comment, JTokenType.Raw};
        private static readonly JTokenType[] DateTimeTypes = {JTokenType.Date, JTokenType.String, JTokenType.Comment, JTokenType.Raw};
        private static readonly JTokenType[] BytesTypes = {JTokenType.Bytes, JTokenType.String, JTokenType.Comment, JTokenType.Raw, JTokenType.Integer};

        /// <summary>
        /// Gets a comparer that can compare two tokens for value equality.
        /// </summary>
        /// <value>A <see cref="JTokenEqualityComparer"/> that can compare two nodes for value equality.</value>
        public static JTokenEqualityComparer EqualityComparer
        {
            get { return _equalityComparer ?? (_equalityComparer = new JTokenEqualityComparer()); }
        }

        /// <summary>
        /// Gets or sets the parent.
        /// </summary>
        /// <value>The parent.</value>
        public JContainer Parent
        {
            [DebuggerStepThrough] get { return _parent; }
            internal set { _parent = value; }
        }

        /// <summary>
        /// Gets the root <see cref="JToken"/> of this <see cref="JToken"/>.
        /// </summary>
        /// <value>The root <see cref="JToken"/> of this <see cref="JToken"/>.</value>
        public JToken Root
        {
            get
            {
                var parent = Parent;
                if (parent == null) return this;
                while (parent.Parent != null)
                    parent = parent.Parent;
                return parent;
            }
        }

        internal abstract JToken CloneToken();
        internal abstract bool DeepEquals(JToken node);

        /// <summary>
        /// Gets the node type for this <see cref="JToken"/>.
        /// </summary>
        /// <value>The type.</value>
        public abstract JTokenType Type { get; }

        /// <summary>
        /// Gets a value indicating whether this token has child tokens.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this token has child values; otherwise, <c>false</c>.
        /// </value>
        public abstract bool HasValues { get; }

        /// <summary>
        /// Compares the values of two tokens, including the values of all descendant tokens.
        /// </summary>
        /// <param name="t1">The first <see cref="JToken"/> to compare.</param>
        /// <param name="t2">The second <see cref="JToken"/> to compare.</param>
        /// <returns>true if the tokens are equal; otherwise false.</returns>
        public static bool DeepEquals(JToken t1, JToken t2)
        {
            return (t1 == t2 || (t1 != null && t2 != null && t1.DeepEquals(t2)));
        }

        /// <summary>
        /// Gets the next sibling token of this node.
        /// </summary>
        /// <value>The <see cref="JToken"/> that contains the next sibling token.</value>
        public JToken Next { get; internal set; }

        /// <summary>
        /// Gets the previous sibling token of this node.
        /// </summary>
        /// <value>The <see cref="JToken"/> that contains the previous sibling token.</value>
        public JToken Previous { get; internal set; }

        /// <summary>
        /// Gets the path of the JSON token. 
        /// </summary>
        public string Path
        {
            get
            {
                if (Parent == null) return string.Empty;

                IList<JToken> ancestors = AncestorsAndSelf().Reverse().ToList();

                IList<JsonPosition> positions = new List<JsonPosition>();
                for (var i = 0; i < ancestors.Count; i++)
                {
                    var current = ancestors[i];
                    JToken next = null;
                    if (i + 1 < ancestors.Count)
                        next = ancestors[i + 1];
                    else if (ancestors[i].Type == JTokenType.Property)
                        next = ancestors[i];

                    if (next != null)
                    {
                        switch (current.Type)
                        {
                            case JTokenType.Property:
                                var property = (JProperty) current;
                                positions.Add(new JsonPosition(JsonContainerType.Object) {PropertyName = property.Name});
                                break;
                            case JTokenType.Array:
                            case JTokenType.Constructor:
                                var index = ((IList<JToken>) current).IndexOf(next);
                                positions.Add(new JsonPosition(JsonContainerType.Array) {Position = index});
                                break;
                        }
                    }
                }

                return JsonPosition.BuildPath(positions);
            }
        }

        internal JToken()
        {
        }

        /// <summary>
        /// Adds the specified content immediately after this token.
        /// </summary>
        /// <param name="content">A content object that contains simple content or a collection of content objects to be added after this token.</param>
        public void AddAfterSelf(object content)
        {
            if (_parent == null) throw new InvalidOperationException("The parent is missing.");
            var index = _parent.IndexOfItem(this);
            _parent.AddInternal(index + 1, content, false);
        }

        /// <summary>
        /// Adds the specified content immediately before this token.
        /// </summary>
        /// <param name="content">A content object that contains simple content or a collection of content objects to be added before this token.</param>
        public void AddBeforeSelf(object content)
        {
            if (_parent == null) throw new InvalidOperationException("The parent is missing.");
            var index = _parent.IndexOfItem(this);
            _parent.AddInternal(index, content, false);
        }

        /// <summary>
        /// Returns a collection of the ancestor tokens of this token.
        /// </summary>
        /// <returns>A collection of the ancestor tokens of this token.</returns>
        public IEnumerable<JToken> Ancestors()
        {
            return GetAncestors(false);
        }

        /// <summary>
        /// Returns a collection of tokens that contain this token, and the ancestors of this token.
        /// </summary>
        /// <returns>A collection of tokens that contain this token, and the ancestors of this token.</returns>
        public IEnumerable<JToken> AncestorsAndSelf()
        {
            return GetAncestors(true);
        }

        internal IEnumerable<JToken> GetAncestors(bool self)
        {
            for (JToken current = self ? this : Parent; current != null; current = current.Parent)
                yield return current;
        }

        /// <summary>
        /// Returns a collection of the sibling tokens after this token, in document order.
        /// </summary>
        /// <returns>A collection of the sibling tokens after this tokens, in document order.</returns>
        public IEnumerable<JToken> AfterSelf()
        {
            if (Parent == null)
                yield break;

            for (var o = Next; o != null; o = o.Next)
                yield return o;
        }

        /// <summary>
        /// Returns a collection of the sibling tokens before this token, in document order.
        /// </summary>
        /// <returns>A collection of the sibling tokens before this token, in document order.</returns>
        public IEnumerable<JToken> BeforeSelf()
        {
            for (var o = Parent.First; o != this; o = o.Next)
                yield return o;
        }

        /// <summary>
        /// Gets the <see cref="JToken"/> with the specified key.
        /// </summary>
        /// <value>The <see cref="JToken"/> with the specified key.</value>
        public virtual JToken this[object key]
        {
            get { throw new InvalidOperationException("Cannot access child value on {0}.".FormatWith(CultureInfo.InvariantCulture, GetType())); }
            set { throw new InvalidOperationException("Cannot set child value on {0}.".FormatWith(CultureInfo.InvariantCulture, GetType())); }
        }

        /// <summary>
        /// Gets the <see cref="JToken"/> with the specified key converted to the specified type.
        /// </summary>
        /// <typeparam name="T">The type to convert the token to.</typeparam>
        /// <param name="key">The token key.</param>
        /// <returns>The converted token value.</returns>
        public virtual T Value<T>(object key)
        {
            var token = this[key];
            // null check to fix MonoTouch issue - https://github.com/dolbz/Newtonsoft.Json/commit/a24e3062846b30ee505f3271ac08862bb471b822
            return token == null ? default(T) : token.Convert<JToken, T>();
        }

        /// <summary>
        /// Get the first child token of this token.
        /// </summary>
        /// <value>A <see cref="JToken"/> containing the first child token of the <see cref="JToken"/>.</value>
        public virtual JToken First
        {
            get { throw new InvalidOperationException("Cannot access child value on {0}.".FormatWith(CultureInfo.InvariantCulture, GetType())); }
        }

        /// <summary>
        /// Get the last child token of this token.
        /// </summary>
        /// <value>A <see cref="JToken"/> containing the last child token of the <see cref="JToken"/>.</value>
        public virtual JToken Last
        {
            get { throw new InvalidOperationException("Cannot access child value on {0}.".FormatWith(CultureInfo.InvariantCulture, GetType())); }
        }

        /// <summary>
        /// Returns a collection of the child tokens of this token, in document order.
        /// </summary>
        /// <returns>An <see cref="IEnumerable{T}"/> of <see cref="JToken"/> containing the child tokens of this <see cref="JToken"/>, in document order.</returns>
        public virtual JEnumerable<JToken> Children()
        {
            return JEnumerable<JToken>.Empty;
        }

        /// <summary>
        /// Returns a collection of the child tokens of this token, in document order, filtered by the specified type.
        /// </summary>
        /// <typeparam name="T">The type to filter the child tokens on.</typeparam>
        /// <returns>A <see cref="JEnumerable{T}"/> containing the child tokens of this <see cref="JToken"/>, in document order.</returns>
        public JEnumerable<T> Children<T>() where T : JToken
        {
            return new JEnumerable<T>(Children().OfType<T>());
        }

        /// <summary>
        /// Returns a collection of the child values of this token, in document order.
        /// </summary>
        /// <typeparam name="T">The type to convert the values to.</typeparam>
        /// <returns>A <see cref="IEnumerable{T}"/> containing the child values of this <see cref="JToken"/>, in document order.</returns>
        public virtual IEnumerable<T> Values<T>()
        {
            throw new InvalidOperationException("Cannot access child value on {0}.".FormatWith(CultureInfo.InvariantCulture, GetType()));
        }

        /// <summary>
        /// Removes this token from its parent.
        /// </summary>
        public void Remove()
        {
            if (_parent == null) throw new InvalidOperationException("The parent is missing.");
            _parent.RemoveItem(this);
        }

        /// <summary>
        /// Replaces this token with the specified token.
        /// </summary>
        /// <param name="value">The value.</param>
        public void Replace(JToken value)
        {
            if (_parent == null) throw new InvalidOperationException("The parent is missing.");
            _parent.ReplaceItem(this, value);
        }

        /// <summary>
        /// Writes this token to a <see cref="JsonWriter"/>.
        /// </summary>
        /// <param name="writer">A <see cref="JsonWriter"/> into which this method will write.</param>
        /// <param name="converters">A collection of <see cref="JsonConverter"/> which will be used when writing the token.</param>
        public abstract void WriteTo(JsonWriter writer, params JsonConverter[] converters);

        /// <summary>
        /// Returns the indented JSON for this token.
        /// </summary>
        /// <returns>
        /// The indented JSON for this token.
        /// </returns>
        public override string ToString()
        {
            return ToString(Formatting.Indented);
        }

        /// <summary>
        /// Returns the JSON for this token using the given formatting and converters.
        /// </summary>
        /// <param name="formatting">Indicates how the output is formatted.</param>
        /// <param name="converters">A collection of <see cref="JsonConverter"/> which will be used when writing the token.</param>
        /// <returns>The JSON for this token using the given formatting and converters.</returns>
        public string ToString(Formatting formatting, params JsonConverter[] converters)
        {
            using (var sw = new StringWriter(CultureInfo.InvariantCulture))
            {
                var jw = new JsonTextWriter(sw) {Formatting = formatting};
                WriteTo(jw, converters);
                return sw.ToString();
            }
        }

        private static JValue EnsureValue(JToken value)
        {
            if (value == null) throw new ArgumentNullException("value");
            if (value is JProperty)
                value = ((JProperty) value).Value;
            var v = value as JValue;
            return v;
        }

        private static string GetType(JToken token)
        {
            ValidationUtils.ArgumentNotNull(token, "token");
            if (token is JProperty)
                token = ((JProperty) token).Value;
            return token.Type.ToString();
        }

        private static bool ValidateToken(JToken o, JTokenType[] validTypes, bool nullable)
        {
            return (Array.IndexOf(validTypes, o.Type) != -1) || (nullable && (o.Type == JTokenType.Null || o.Type == JTokenType.Undefined));
        }

        /// <summary>
        /// Performs an explicit conversion from <see cref="CODE.Framework.Core.Newtonsoft.Linq.JToken"/> to <see cref="System.Boolean"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static explicit operator bool(JToken value)
        {
            var v = EnsureValue(value);
            if (v == null || !ValidateToken(v, BooleanTypes, false)) throw new ArgumentException("Can not convert {0} to Boolean.".FormatWith(CultureInfo.InvariantCulture, GetType(value)));
            if (v.Value is BigInteger) return Convert.ToBoolean((int) (BigInteger) v.Value);
            return Convert.ToBoolean(v.Value, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Performs an explicit conversion from <see cref="CODE.Framework.Core.Newtonsoft.Linq.JToken"/> to <see cref="System.DateTimeOffset"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static explicit operator DateTimeOffset(JToken value)
        {
            var v = EnsureValue(value);
            if (v == null || !ValidateToken(v, DateTimeTypes, false)) throw new ArgumentException("Can not convert {0} to DateTimeOffset.".FormatWith(CultureInfo.InvariantCulture, GetType(value)));
            if (v.Value is DateTimeOffset) return (DateTimeOffset) v.Value;
            var stringValue = v.Value as string;
            if (stringValue != null) return DateTimeOffset.Parse(stringValue, CultureInfo.InvariantCulture);
            return new DateTimeOffset(Convert.ToDateTime(v.Value, CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Performs an explicit conversion from <see cref="CODE.Framework.Core.Newtonsoft.Linq.JToken"/> to <see cref="Nullable{Boolean}"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static explicit operator bool?(JToken value)
        {
            if (value == null) return null;
            var v = EnsureValue(value);
            if (v == null || !ValidateToken(v, BooleanTypes, true)) throw new ArgumentException("Can not convert {0} to Boolean.".FormatWith(CultureInfo.InvariantCulture, GetType(value)));
            if (v.Value is BigInteger) return Convert.ToBoolean((int) (BigInteger) v.Value);
            return (v.Value != null) ? (bool?) Convert.ToBoolean(v.Value, CultureInfo.InvariantCulture) : null;
        }

        /// <summary>
        /// Performs an explicit conversion from <see cref="CODE.Framework.Core.Newtonsoft.Linq.JToken"/> to <see cref="System.Int64"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static explicit operator long(JToken value)
        {
            var v = EnsureValue(value);
            if (v == null || !ValidateToken(v, NumberTypes, false)) throw new ArgumentException("Can not convert {0} to Int64.".FormatWith(CultureInfo.InvariantCulture, GetType(value)));
            if (v.Value is BigInteger) return (long) (BigInteger) v.Value;
            return Convert.ToInt64(v.Value, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Performs an explicit conversion from <see cref="CODE.Framework.Core.Newtonsoft.Linq.JToken"/> to <see cref="Nullable{DateTime}"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static explicit operator DateTime?(JToken value)
        {
            if (value == null) return null;
            var v = EnsureValue(value);
            if (v == null || !ValidateToken(v, DateTimeTypes, true)) throw new ArgumentException("Can not convert {0} to DateTime.".FormatWith(CultureInfo.InvariantCulture, GetType(value)));
            if (v.Value is DateTimeOffset) return ((DateTimeOffset) v.Value).DateTime;
            return (v.Value != null) ? (DateTime?) Convert.ToDateTime(v.Value, CultureInfo.InvariantCulture) : null;
        }

        /// <summary>
        /// Performs an explicit conversion from <see cref="CODE.Framework.Core.Newtonsoft.Linq.JToken"/> to <see cref="Nullable{DateTimeOffset}"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static explicit operator DateTimeOffset?(JToken value)
        {
            if (value == null) return null;
            var v = EnsureValue(value);
            if (v == null || !ValidateToken(v, DateTimeTypes, true)) throw new ArgumentException("Can not convert {0} to DateTimeOffset.".FormatWith(CultureInfo.InvariantCulture, GetType(value)));
            if (v.Value == null) return null;
            if (v.Value is DateTimeOffset) return (DateTimeOffset?) v.Value;
            var stringValue = v.Value as string;
            if (stringValue != null) return DateTimeOffset.Parse(stringValue, CultureInfo.InvariantCulture);
            return new DateTimeOffset(Convert.ToDateTime(v.Value, CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Performs an explicit conversion from <see cref="CODE.Framework.Core.Newtonsoft.Linq.JToken"/> to <see cref="Nullable{Decimal}"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static explicit operator decimal?(JToken value)
        {
            if (value == null) return null;
            var v = EnsureValue(value);
            if (v == null || !ValidateToken(v, NumberTypes, true)) throw new ArgumentException("Can not convert {0} to Decimal.".FormatWith(CultureInfo.InvariantCulture, GetType(value)));
            if (v.Value is BigInteger) return (decimal?) (BigInteger) v.Value;
            return (v.Value != null) ? (decimal?) Convert.ToDecimal(v.Value, CultureInfo.InvariantCulture) : null;
        }

        /// <summary>
        /// Performs an explicit conversion from <see cref="CODE.Framework.Core.Newtonsoft.Linq.JToken"/> to <see cref="Nullable{Double}"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static explicit operator double?(JToken value)
        {
            if (value == null) return null;
            var v = EnsureValue(value);
            if (v == null || !ValidateToken(v, NumberTypes, true)) throw new ArgumentException("Can not convert {0} to Double.".FormatWith(CultureInfo.InvariantCulture, GetType(value)));
            if (v.Value is BigInteger) return (double?) (BigInteger) v.Value;
            return (v.Value != null) ? (double?) Convert.ToDouble(v.Value, CultureInfo.InvariantCulture) : null;
        }

        /// <summary>
        /// Performs an explicit conversion from <see cref="CODE.Framework.Core.Newtonsoft.Linq.JToken"/> to <see cref="Nullable{Char}"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static explicit operator char?(JToken value)
        {
            if (value == null) return null;
            var v = EnsureValue(value);
            if (v == null || !ValidateToken(v, CharTypes, true)) throw new ArgumentException("Can not convert {0} to Char.".FormatWith(CultureInfo.InvariantCulture, GetType(value)));
            if (v.Value is BigInteger) return (char?) (BigInteger) v.Value;
            return (v.Value != null) ? (char?) Convert.ToChar(v.Value, CultureInfo.InvariantCulture) : null;
        }

        /// <summary>
        /// Performs an explicit conversion from <see cref="CODE.Framework.Core.Newtonsoft.Linq.JToken"/> to <see cref="System.Int32"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static explicit operator int(JToken value)
        {
            var v = EnsureValue(value);
            if (v == null || !ValidateToken(v, NumberTypes, false)) throw new ArgumentException("Can not convert {0} to Int32.".FormatWith(CultureInfo.InvariantCulture, GetType(value)));
            if (v.Value is BigInteger) return (int) (BigInteger) v.Value;
            return Convert.ToInt32(v.Value, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Performs an explicit conversion from <see cref="CODE.Framework.Core.Newtonsoft.Linq.JToken"/> to <see cref="System.Int16"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static explicit operator short(JToken value)
        {
            var v = EnsureValue(value);
            if (v == null || !ValidateToken(v, NumberTypes, false)) throw new ArgumentException("Can not convert {0} to Int16.".FormatWith(CultureInfo.InvariantCulture, GetType(value)));
            if (v.Value is BigInteger) return (short) (BigInteger) v.Value;
            return Convert.ToInt16(v.Value, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Performs an explicit conversion from <see cref="CODE.Framework.Core.Newtonsoft.Linq.JToken"/> to <see cref="System.UInt16"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        [CLSCompliant(false)]
        public static explicit operator ushort(JToken value)
        {
            var v = EnsureValue(value);
            if (v == null || !ValidateToken(v, NumberTypes, false)) throw new ArgumentException("Can not convert {0} to UInt16.".FormatWith(CultureInfo.InvariantCulture, GetType(value)));
            if (v.Value is BigInteger) return (ushort) (BigInteger) v.Value;
            return Convert.ToUInt16(v.Value, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Performs an explicit conversion from <see cref="CODE.Framework.Core.Newtonsoft.Linq.JToken"/> to <see cref="System.Char"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        [CLSCompliant(false)]
        public static explicit operator char(JToken value)
        {
            var v = EnsureValue(value);
            if (v == null || !ValidateToken(v, CharTypes, false)) throw new ArgumentException("Can not convert {0} to Char.".FormatWith(CultureInfo.InvariantCulture, GetType(value)));
            if (v.Value is BigInteger) return (char) (BigInteger) v.Value;
            return Convert.ToChar(v.Value, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Performs an explicit conversion from <see cref="CODE.Framework.Core.Newtonsoft.Linq.JToken"/> to <see cref="System.Byte"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static explicit operator byte(JToken value)
        {
            var v = EnsureValue(value);
            if (v == null || !ValidateToken(v, NumberTypes, false)) throw new ArgumentException("Can not convert {0} to Byte.".FormatWith(CultureInfo.InvariantCulture, GetType(value)));
            if (v.Value is BigInteger) return (byte) (BigInteger) v.Value;
            return Convert.ToByte(v.Value, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Performs an explicit conversion from <see cref="CODE.Framework.Core.Newtonsoft.Linq.JToken"/> to <see cref="System.SByte"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        [CLSCompliant(false)]
        public static explicit operator sbyte(JToken value)
        {
            var v = EnsureValue(value);
            if (v == null || !ValidateToken(v, NumberTypes, false)) throw new ArgumentException("Can not convert {0} to SByte.".FormatWith(CultureInfo.InvariantCulture, GetType(value)));
            if (v.Value is BigInteger) return (sbyte) (BigInteger) v.Value;
            return Convert.ToSByte(v.Value, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Performs an explicit conversion from <see cref="CODE.Framework.Core.Newtonsoft.Linq.JToken"/> to <see cref="Nullable{Int32}"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static explicit operator int?(JToken value)
        {
            if (value == null) return null;
            var v = EnsureValue(value);
            if (v == null || !ValidateToken(v, NumberTypes, true)) throw new ArgumentException("Can not convert {0} to Int32.".FormatWith(CultureInfo.InvariantCulture, GetType(value)));
            if (v.Value is BigInteger) return (int?) (BigInteger) v.Value;
            return (v.Value != null) ? (int?) Convert.ToInt32(v.Value, CultureInfo.InvariantCulture) : null;
        }

        /// <summary>
        /// Performs an explicit conversion from <see cref="CODE.Framework.Core.Newtonsoft.Linq.JToken"/> to <see cref="Nullable{Int16}"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static explicit operator short?(JToken value)
        {
            if (value == null) return null;
            var v = EnsureValue(value);
            if (v == null || !ValidateToken(v, NumberTypes, true)) throw new ArgumentException("Can not convert {0} to Int16.".FormatWith(CultureInfo.InvariantCulture, GetType(value)));
            if (v.Value is BigInteger) return (short?) (BigInteger) v.Value;
            return (v.Value != null) ? (short?) Convert.ToInt16(v.Value, CultureInfo.InvariantCulture) : null;
        }

        /// <summary>
        /// Performs an explicit conversion from <see cref="CODE.Framework.Core.Newtonsoft.Linq.JToken"/> to <see cref="Nullable{UInt16}"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        [CLSCompliant(false)]
        public static explicit operator ushort?(JToken value)
        {
            if (value == null) return null;
            var v = EnsureValue(value);
            if (v == null || !ValidateToken(v, NumberTypes, true)) throw new ArgumentException("Can not convert {0} to UInt16.".FormatWith(CultureInfo.InvariantCulture, GetType(value)));
            if (v.Value is BigInteger) return (ushort?) (BigInteger) v.Value;
            return (v.Value != null) ? (ushort?) Convert.ToUInt16(v.Value, CultureInfo.InvariantCulture) : null;
        }

        /// <summary>
        /// Performs an explicit conversion from <see cref="CODE.Framework.Core.Newtonsoft.Linq.JToken"/> to <see cref="Nullable{Byte}"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static explicit operator byte?(JToken value)
        {
            if (value == null) return null;
            var v = EnsureValue(value);
            if (v == null || !ValidateToken(v, NumberTypes, true)) throw new ArgumentException("Can not convert {0} to Byte.".FormatWith(CultureInfo.InvariantCulture, GetType(value)));
            if (v.Value is BigInteger) return (byte?) (BigInteger) v.Value;
            return (v.Value != null) ? (byte?) Convert.ToByte(v.Value, CultureInfo.InvariantCulture) : null;
        }

        /// <summary>
        /// Performs an explicit conversion from <see cref="CODE.Framework.Core.Newtonsoft.Linq.JToken"/> to <see cref="Nullable{SByte}"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        [CLSCompliant(false)]
        public static explicit operator sbyte?(JToken value)
        {
            if (value == null) return null;
            var v = EnsureValue(value);
            if (v == null || !ValidateToken(v, NumberTypes, true)) throw new ArgumentException("Can not convert {0} to SByte.".FormatWith(CultureInfo.InvariantCulture, GetType(value)));
            if (v.Value is BigInteger) return (sbyte?) (BigInteger) v.Value;
            return (v.Value != null) ? (sbyte?) Convert.ToSByte(v.Value, CultureInfo.InvariantCulture) : null;
        }

        /// <summary>
        /// Performs an explicit conversion from <see cref="CODE.Framework.Core.Newtonsoft.Linq.JToken"/> to <see cref="System.DateTime"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static explicit operator DateTime(JToken value)
        {
            var v = EnsureValue(value);
            if (v == null || !ValidateToken(v, DateTimeTypes, false)) throw new ArgumentException("Can not convert {0} to DateTime.".FormatWith(CultureInfo.InvariantCulture, GetType(value)));
            if (v.Value is DateTimeOffset) return ((DateTimeOffset) v.Value).DateTime;
            return Convert.ToDateTime(v.Value, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Performs an explicit conversion from <see cref="CODE.Framework.Core.Newtonsoft.Linq.JToken"/> to <see cref="Nullable{Int64}"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static explicit operator long?(JToken value)
        {
            if (value == null) return null;
            var v = EnsureValue(value);
            if (v == null || !ValidateToken(v, NumberTypes, true)) throw new ArgumentException("Can not convert {0} to Int64.".FormatWith(CultureInfo.InvariantCulture, GetType(value)));
            if (v.Value is BigInteger) return (long?) (BigInteger) v.Value;
            return (v.Value != null) ? (long?) Convert.ToInt64(v.Value, CultureInfo.InvariantCulture) : null;
        }

        /// <summary>
        /// Performs an explicit conversion from <see cref="CODE.Framework.Core.Newtonsoft.Linq.JToken"/> to <see cref="Nullable{Single}"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static explicit operator float?(JToken value)
        {
            if (value == null) return null;
            var v = EnsureValue(value);
            if (v == null || !ValidateToken(v, NumberTypes, true)) throw new ArgumentException("Can not convert {0} to Single.".FormatWith(CultureInfo.InvariantCulture, GetType(value)));
            if (v.Value is BigInteger) return (float?) (BigInteger) v.Value;
            return (v.Value != null) ? (float?) Convert.ToSingle(v.Value, CultureInfo.InvariantCulture) : null;
        }

        /// <summary>
        /// Performs an explicit conversion from <see cref="CODE.Framework.Core.Newtonsoft.Linq.JToken"/> to <see cref="System.Decimal"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static explicit operator decimal(JToken value)
        {
            var v = EnsureValue(value);
            if (v == null || !ValidateToken(v, NumberTypes, false)) throw new ArgumentException("Can not convert {0} to Decimal.".FormatWith(CultureInfo.InvariantCulture, GetType(value)));
            if (v.Value is BigInteger) return (decimal) (BigInteger) v.Value;
            return Convert.ToDecimal(v.Value, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Performs an explicit conversion from <see cref="CODE.Framework.Core.Newtonsoft.Linq.JToken"/> to <see cref="Nullable{UInt32}"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        [CLSCompliant(false)]
        public static explicit operator uint?(JToken value)
        {
            if (value == null) return null;
            var v = EnsureValue(value);
            if (v == null || !ValidateToken(v, NumberTypes, true)) throw new ArgumentException("Can not convert {0} to UInt32.".FormatWith(CultureInfo.InvariantCulture, GetType(value)));
            if (v.Value is BigInteger) return (uint?) (BigInteger) v.Value;
            return (v.Value != null) ? (uint?) Convert.ToUInt32(v.Value, CultureInfo.InvariantCulture) : null;
        }

        /// <summary>
        /// Performs an explicit conversion from <see cref="CODE.Framework.Core.Newtonsoft.Linq.JToken"/> to <see cref="Nullable{UInt64}"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        [CLSCompliant(false)]
        public static explicit operator ulong?(JToken value)
        {
            if (value == null) return null;
            var v = EnsureValue(value);
            if (v == null || !ValidateToken(v, NumberTypes, true)) throw new ArgumentException("Can not convert {0} to UInt64.".FormatWith(CultureInfo.InvariantCulture, GetType(value)));
            if (v.Value is BigInteger) return (ulong?) (BigInteger) v.Value;
            return (v.Value != null) ? (ulong?) Convert.ToUInt64(v.Value, CultureInfo.InvariantCulture) : null;
        }

        /// <summary>
        /// Performs an explicit conversion from <see cref="CODE.Framework.Core.Newtonsoft.Linq.JToken"/> to <see cref="System.Double"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static explicit operator double(JToken value)
        {
            var v = EnsureValue(value);
            if (v == null || !ValidateToken(v, NumberTypes, false)) throw new ArgumentException("Can not convert {0} to Double.".FormatWith(CultureInfo.InvariantCulture, GetType(value)));
            if (v.Value is BigInteger) return (double) (BigInteger) v.Value;
            return Convert.ToDouble(v.Value, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Performs an explicit conversion from <see cref="CODE.Framework.Core.Newtonsoft.Linq.JToken"/> to <see cref="System.Single"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static explicit operator float(JToken value)
        {
            var v = EnsureValue(value);
            if (v == null || !ValidateToken(v, NumberTypes, false)) throw new ArgumentException("Can not convert {0} to Single.".FormatWith(CultureInfo.InvariantCulture, GetType(value)));
            if (v.Value is BigInteger) return (float) (BigInteger) v.Value;
            return Convert.ToSingle(v.Value, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Performs an explicit conversion from <see cref="CODE.Framework.Core.Newtonsoft.Linq.JToken"/> to <see cref="System.String"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static explicit operator string(JToken value)
        {
            if (value == null) return null;
            var v = EnsureValue(value);
            if (v == null || !ValidateToken(v, StringTypes, true)) throw new ArgumentException("Can not convert {0} to String.".FormatWith(CultureInfo.InvariantCulture, GetType(value)));
            if (v.Value == null) return null;
            var bytes = v.Value as byte[];
            if (bytes != null) return Convert.ToBase64String(bytes);
            if (v.Value is BigInteger) return ((BigInteger) v.Value).ToString(CultureInfo.InvariantCulture);
            return Convert.ToString(v.Value, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Performs an explicit conversion from <see cref="CODE.Framework.Core.Newtonsoft.Linq.JToken"/> to <see cref="System.UInt32"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        [CLSCompliant(false)]
        public static explicit operator uint(JToken value)
        {
            var v = EnsureValue(value);
            if (v == null || !ValidateToken(v, NumberTypes, false)) throw new ArgumentException("Can not convert {0} to UInt32.".FormatWith(CultureInfo.InvariantCulture, GetType(value)));
            if (v.Value is BigInteger) return (uint) (BigInteger) v.Value;
            return Convert.ToUInt32(v.Value, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Performs an explicit conversion from <see cref="CODE.Framework.Core.Newtonsoft.Linq.JToken"/> to <see cref="System.UInt64"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        [CLSCompliant(false)]
        public static explicit operator ulong(JToken value)
        {
            var v = EnsureValue(value);
            if (v == null || !ValidateToken(v, NumberTypes, false)) throw new ArgumentException("Can not convert {0} to UInt64.".FormatWith(CultureInfo.InvariantCulture, GetType(value)));
            if (v.Value is BigInteger) return (ulong) (BigInteger) v.Value;
            return Convert.ToUInt64(v.Value, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Performs an explicit conversion from <see cref="CODE.Framework.Core.Newtonsoft.Linq.JToken"/> to <see cref="Byte"/>[].
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static explicit operator byte[](JToken value)
        {
            if (value == null) return null;
            var v = EnsureValue(value);
            if (v == null || !ValidateToken(v, BytesTypes, false)) throw new ArgumentException("Can not convert {0} to byte array.".FormatWith(CultureInfo.InvariantCulture, GetType(value)));
            if (v.Value is string) return Convert.FromBase64String(Convert.ToString(v.Value, CultureInfo.InvariantCulture));
            if (v.Value is BigInteger) return ((BigInteger) v.Value).ToByteArray();
            var bytes = v.Value as byte[];
            if (bytes != null) return bytes;
            throw new ArgumentException("Can not convert {0} to byte array.".FormatWith(CultureInfo.InvariantCulture, GetType(value)));
        }

        /// <summary>
        /// Performs an explicit conversion from <see cref="CODE.Framework.Core.Newtonsoft.Linq.JToken"/> to <see cref="System.Guid"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static explicit operator Guid(JToken value)
        {
            var v = EnsureValue(value);
            if (v == null || !ValidateToken(v, GuidTypes, false)) throw new ArgumentException("Can not convert {0} to Guid.".FormatWith(CultureInfo.InvariantCulture, GetType(value)));
            var bytes = v.Value as byte[];
            if (bytes != null) return new Guid(bytes);
            return (v.Value is Guid) ? (Guid) v.Value : new Guid(Convert.ToString(v.Value, CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Performs an explicit conversion from <see cref="CODE.Framework.Core.Newtonsoft.Linq.JToken"/> to <see cref="System.Guid"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static explicit operator Guid?(JToken value)
        {
            if (value == null) return null;
            var v = EnsureValue(value);
            if (v == null || !ValidateToken(v, GuidTypes, true)) throw new ArgumentException("Can not convert {0} to Guid.".FormatWith(CultureInfo.InvariantCulture, GetType(value)));
            if (v.Value == null) return null;
            var bytes = v.Value as byte[];
            if (bytes != null) return new Guid(bytes);
            return (v.Value is Guid) ? (Guid) v.Value : new Guid(Convert.ToString(v.Value, CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Performs an explicit conversion from <see cref="CODE.Framework.Core.Newtonsoft.Linq.JToken"/> to <see cref="System.TimeSpan"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static explicit operator TimeSpan(JToken value)
        {
            var v = EnsureValue(value);
            if (v == null || !ValidateToken(v, TimeSpanTypes, false)) throw new ArgumentException("Can not convert {0} to TimeSpan.".FormatWith(CultureInfo.InvariantCulture, GetType(value)));
            return (v.Value is TimeSpan) ? (TimeSpan) v.Value : ConvertUtils.ParseTimeSpan(Convert.ToString(v.Value, CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Performs an explicit conversion from <see cref="CODE.Framework.Core.Newtonsoft.Linq.JToken"/> to <see cref="System.TimeSpan"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static explicit operator TimeSpan?(JToken value)
        {
            if (value == null) return null;
            var v = EnsureValue(value);
            if (v == null || !ValidateToken(v, TimeSpanTypes, true)) throw new ArgumentException("Can not convert {0} to TimeSpan.".FormatWith(CultureInfo.InvariantCulture, GetType(value)));
            if (v.Value == null) return null;
            return (v.Value is TimeSpan) ? (TimeSpan) v.Value : ConvertUtils.ParseTimeSpan(Convert.ToString(v.Value, CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Performs an explicit conversion from <see cref="CODE.Framework.Core.Newtonsoft.Linq.JToken"/> to <see cref="System.Uri"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static explicit operator Uri(JToken value)
        {
            if (value == null) return null;
            var v = EnsureValue(value);
            if (v == null || !ValidateToken(v, UriTypes, true)) throw new ArgumentException("Can not convert {0} to Uri.".FormatWith(CultureInfo.InvariantCulture, GetType(value)));
            if (v.Value == null) return null;
            return (v.Value is Uri) ? (Uri) v.Value : new Uri(Convert.ToString(v.Value, CultureInfo.InvariantCulture));
        }

        private static BigInteger ToBigInteger(JToken value)
        {
            var v = EnsureValue(value);
            if (v == null || !ValidateToken(v, BigIntegerTypes, false)) throw new ArgumentException("Can not convert {0} to BigInteger.".FormatWith(CultureInfo.InvariantCulture, GetType(value)));
            return ConvertUtils.ToBigInteger(v.Value);
        }

        private static BigInteger? ToBigIntegerNullable(JToken value)
        {
            var v = EnsureValue(value);
            if (v == null || !ValidateToken(v, BigIntegerTypes, true)) throw new ArgumentException("Can not convert {0} to BigInteger.".FormatWith(CultureInfo.InvariantCulture, GetType(value)));
            if (v.Value == null) return null;
            return ConvertUtils.ToBigInteger(v.Value);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="Boolean"/> to <see cref="JToken"/>.
        /// </summary>
        /// <param name="value">The value to create a <see cref="JValue"/> from.</param>
        /// <returns>The <see cref="JValue"/> initialized with the specified value.</returns>
        public static implicit operator JToken(bool value)
        {
            return new JValue(value);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="DateTimeOffset"/> to <see cref="JToken"/>.
        /// </summary>
        /// <param name="value">The value to create a <see cref="JValue"/> from.</param>
        /// <returns>The <see cref="JValue"/> initialized with the specified value.</returns>
        public static implicit operator JToken(DateTimeOffset value)
        {
            return new JValue(value);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="Byte"/> to <see cref="JToken"/>.
        /// </summary>
        /// <param name="value">The value to create a <see cref="JValue"/> from.</param>
        /// <returns>The <see cref="JValue"/> initialized with the specified value.</returns>
        public static implicit operator JToken(byte value)
        {
            return new JValue(value);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="Nullable{Byte}"/> to <see cref="JToken"/>.
        /// </summary>
        /// <param name="value">The value to create a <see cref="JValue"/> from.</param>
        /// <returns>The <see cref="JValue"/> initialized with the specified value.</returns>
        public static implicit operator JToken(byte? value)
        {
            return new JValue(value);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="SByte"/> to <see cref="JToken"/>.
        /// </summary>
        /// <param name="value">The value to create a <see cref="JValue"/> from.</param>
        /// <returns>The <see cref="JValue"/> initialized with the specified value.</returns>
        [CLSCompliant(false)]
        public static implicit operator JToken(sbyte value)
        {
            return new JValue(value);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="Nullable{SByte}"/> to <see cref="JToken"/>.
        /// </summary>
        /// <param name="value">The value to create a <see cref="JValue"/> from.</param>
        /// <returns>The <see cref="JValue"/> initialized with the specified value.</returns>
        [CLSCompliant(false)]
        public static implicit operator JToken(sbyte? value)
        {
            return new JValue(value);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="Nullable{Boolean}"/> to <see cref="JToken"/>.
        /// </summary>
        /// <param name="value">The value to create a <see cref="JValue"/> from.</param>
        /// <returns>The <see cref="JValue"/> initialized with the specified value.</returns>
        public static implicit operator JToken(bool? value)
        {
            return new JValue(value);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="Nullable{Int64}"/> to <see cref="JToken"/>.
        /// </summary>
        /// <param name="value">The value to create a <see cref="JValue"/> from.</param>
        /// <returns>The <see cref="JValue"/> initialized with the specified value.</returns>
        public static implicit operator JToken(long value)
        {
            return new JValue(value);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="Nullable{DateTime}"/> to <see cref="JToken"/>.
        /// </summary>
        /// <param name="value">The value to create a <see cref="JValue"/> from.</param>
        /// <returns>The <see cref="JValue"/> initialized with the specified value.</returns>
        public static implicit operator JToken(DateTime? value)
        {
            return new JValue(value);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="Nullable{DateTimeOffset}"/> to <see cref="JToken"/>.
        /// </summary>
        /// <param name="value">The value to create a <see cref="JValue"/> from.</param>
        /// <returns>The <see cref="JValue"/> initialized with the specified value.</returns>
        public static implicit operator JToken(DateTimeOffset? value)
        {
            return new JValue(value);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="Nullable{Decimal}"/> to <see cref="JToken"/>.
        /// </summary>
        /// <param name="value">The value to create a <see cref="JValue"/> from.</param>
        /// <returns>The <see cref="JValue"/> initialized with the specified value.</returns>
        public static implicit operator JToken(decimal? value)
        {
            return new JValue(value);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="Nullable{Double}"/> to <see cref="JToken"/>.
        /// </summary>
        /// <param name="value">The value to create a <see cref="JValue"/> from.</param>
        /// <returns>The <see cref="JValue"/> initialized with the specified value.</returns>
        public static implicit operator JToken(double? value)
        {
            return new JValue(value);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="Int16"/> to <see cref="JToken"/>.
        /// </summary>
        /// <param name="value">The value to create a <see cref="JValue"/> from.</param>
        /// <returns>The <see cref="JValue"/> initialized with the specified value.</returns>
        [CLSCompliant(false)]
        public static implicit operator JToken(short value)
        {
            return new JValue(value);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="UInt16"/> to <see cref="JToken"/>.
        /// </summary>
        /// <param name="value">The value to create a <see cref="JValue"/> from.</param>
        /// <returns>The <see cref="JValue"/> initialized with the specified value.</returns>
        [CLSCompliant(false)]
        public static implicit operator JToken(ushort value)
        {
            return new JValue(value);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="Int32"/> to <see cref="JToken"/>.
        /// </summary>
        /// <param name="value">The value to create a <see cref="JValue"/> from.</param>
        /// <returns>The <see cref="JValue"/> initialized with the specified value.</returns>
        public static implicit operator JToken(int value)
        {
            return new JValue(value);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="Nullable{Int32}"/> to <see cref="JToken"/>.
        /// </summary>
        /// <param name="value">The value to create a <see cref="JValue"/> from.</param>
        /// <returns>The <see cref="JValue"/> initialized with the specified value.</returns>
        public static implicit operator JToken(int? value)
        {
            return new JValue(value);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="DateTime"/> to <see cref="JToken"/>.
        /// </summary>
        /// <param name="value">The value to create a <see cref="JValue"/> from.</param>
        /// <returns>The <see cref="JValue"/> initialized with the specified value.</returns>
        public static implicit operator JToken(DateTime value)
        {
            return new JValue(value);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="Nullable{Int64}"/> to <see cref="JToken"/>.
        /// </summary>
        /// <param name="value">The value to create a <see cref="JValue"/> from.</param>
        /// <returns>The <see cref="JValue"/> initialized with the specified value.</returns>
        public static implicit operator JToken(long? value)
        {
            return new JValue(value);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="Nullable{Single}"/> to <see cref="JToken"/>.
        /// </summary>
        /// <param name="value">The value to create a <see cref="JValue"/> from.</param>
        /// <returns>The <see cref="JValue"/> initialized with the specified value.</returns>
        public static implicit operator JToken(float? value)
        {
            return new JValue(value);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="Decimal"/> to <see cref="JToken"/>.
        /// </summary>
        /// <param name="value">The value to create a <see cref="JValue"/> from.</param>
        /// <returns>The <see cref="JValue"/> initialized with the specified value.</returns>
        public static implicit operator JToken(decimal value)
        {
            return new JValue(value);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="Nullable{Int16}"/> to <see cref="JToken"/>.
        /// </summary>
        /// <param name="value">The value to create a <see cref="JValue"/> from.</param>
        /// <returns>The <see cref="JValue"/> initialized with the specified value.</returns>
        [CLSCompliant(false)]
        public static implicit operator JToken(short? value)
        {
            return new JValue(value);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="Nullable{UInt16}"/> to <see cref="JToken"/>.
        /// </summary>
        /// <param name="value">The value to create a <see cref="JValue"/> from.</param>
        /// <returns>The <see cref="JValue"/> initialized with the specified value.</returns>
        [CLSCompliant(false)]
        public static implicit operator JToken(ushort? value)
        {
            return new JValue(value);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="Nullable{UInt32}"/> to <see cref="JToken"/>.
        /// </summary>
        /// <param name="value">The value to create a <see cref="JValue"/> from.</param>
        /// <returns>The <see cref="JValue"/> initialized with the specified value.</returns>
        [CLSCompliant(false)]
        public static implicit operator JToken(uint? value)
        {
            return new JValue(value);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="Nullable{UInt64}"/> to <see cref="JToken"/>.
        /// </summary>
        /// <param name="value">The value to create a <see cref="JValue"/> from.</param>
        /// <returns>The <see cref="JValue"/> initialized with the specified value.</returns>
        [CLSCompliant(false)]
        public static implicit operator JToken(ulong? value)
        {
            return new JValue(value);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="Double"/> to <see cref="JToken"/>.
        /// </summary>
        /// <param name="value">The value to create a <see cref="JValue"/> from.</param>
        /// <returns>The <see cref="JValue"/> initialized with the specified value.</returns>
        public static implicit operator JToken(double value)
        {
            return new JValue(value);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="Single"/> to <see cref="JToken"/>.
        /// </summary>
        /// <param name="value">The value to create a <see cref="JValue"/> from.</param>
        /// <returns>The <see cref="JValue"/> initialized with the specified value.</returns>
        public static implicit operator JToken(float value)
        {
            return new JValue(value);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="String"/> to <see cref="JToken"/>.
        /// </summary>
        /// <param name="value">The value to create a <see cref="JValue"/> from.</param>
        /// <returns>The <see cref="JValue"/> initialized with the specified value.</returns>
        public static implicit operator JToken(string value)
        {
            return new JValue(value);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="UInt32"/> to <see cref="JToken"/>.
        /// </summary>
        /// <param name="value">The value to create a <see cref="JValue"/> from.</param>
        /// <returns>The <see cref="JValue"/> initialized with the specified value.</returns>
        [CLSCompliant(false)]
        public static implicit operator JToken(uint value)
        {
            return new JValue(value);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="UInt64"/> to <see cref="JToken"/>.
        /// </summary>
        /// <param name="value">The value to create a <see cref="JValue"/> from.</param>
        /// <returns>The <see cref="JValue"/> initialized with the specified value.</returns>
        [CLSCompliant(false)]
        public static implicit operator JToken(ulong value)
        {
            return new JValue(value);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="Byte"/>[] to <see cref="CODE.Framework.Core.Newtonsoft.Linq.JToken"/>.
        /// </summary>
        /// <param name="value">The value to create a <see cref="JValue"/> from.</param>
        /// <returns>The <see cref="JValue"/> initialized with the specified value.</returns>
        public static implicit operator JToken(byte[] value)
        {
            return new JValue(value);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="T:System.Uri"/> to <see cref="CODE.Framework.Core.Newtonsoft.Linq.JToken"/>.
        /// </summary>
        /// <param name="value">The value to create a <see cref="JValue"/> from.</param>
        /// <returns>The <see cref="JValue"/> initialized with the specified value.</returns>
        public static implicit operator JToken(Uri value)
        {
            return new JValue(value);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="T:System.TimeSpan"/> to <see cref="CODE.Framework.Core.Newtonsoft.Linq.JToken"/>.
        /// </summary>
        /// <param name="value">The value to create a <see cref="JValue"/> from.</param>
        /// <returns>The <see cref="JValue"/> initialized with the specified value.</returns>
        public static implicit operator JToken(TimeSpan value)
        {
            return new JValue(value);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="Nullable{TimeSpan}"/> to <see cref="CODE.Framework.Core.Newtonsoft.Linq.JToken"/>.
        /// </summary>
        /// <param name="value">The value to create a <see cref="JValue"/> from.</param>
        /// <returns>The <see cref="JValue"/> initialized with the specified value.</returns>
        public static implicit operator JToken(TimeSpan? value)
        {
            return new JValue(value);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="T:System.Guid"/> to <see cref="CODE.Framework.Core.Newtonsoft.Linq.JToken"/>.
        /// </summary>
        /// <param name="value">The value to create a <see cref="JValue"/> from.</param>
        /// <returns>The <see cref="JValue"/> initialized with the specified value.</returns>
        public static implicit operator JToken(Guid value)
        {
            return new JValue(value);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="Nullable{Guid}"/> to <see cref="CODE.Framework.Core.Newtonsoft.Linq.JToken"/>.
        /// </summary>
        /// <param name="value">The value to create a <see cref="JValue"/> from.</param>
        /// <returns>The <see cref="JValue"/> initialized with the specified value.</returns>
        public static implicit operator JToken(Guid? value)
        {
            return new JValue(value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<JToken>) this).GetEnumerator();
        }

        IEnumerator<JToken> IEnumerable<JToken>.GetEnumerator()
        {
            return Children().GetEnumerator();
        }

        internal abstract int GetDeepHashCode();

        IJEnumerable<JToken> IJEnumerable<JToken>.this[object key]
        {
            get { return this[key]; }
        }

        /// <summary>
        /// Creates an <see cref="JsonReader"/> for this token.
        /// </summary>
        /// <returns>An <see cref="JsonReader"/> that can be used to read this token and its descendants.</returns>
        public JsonReader CreateReader()
        {
            return new JTokenReader(this, Path);
        }

        internal static JToken FromObjectInternal(object o, JsonSerializer jsonSerializer)
        {
            ValidationUtils.ArgumentNotNull(o, "o");
            ValidationUtils.ArgumentNotNull(jsonSerializer, "jsonSerializer");

            JToken token;
            using (var jsonWriter = new JTokenWriter())
            {
                jsonSerializer.Serialize(jsonWriter, o);
                token = jsonWriter.Token;
            }

            return token;
        }

        /// <summary>
        /// Creates a <see cref="JToken"/> from an object.
        /// </summary>
        /// <param name="o">The object that will be used to create <see cref="JToken"/>.</param>
        /// <returns>A <see cref="JToken"/> with the value of the specified object</returns>
        public static JToken FromObject(object o)
        {
            return FromObjectInternal(o, JsonSerializer.CreateDefault());
        }

        /// <summary>
        /// Creates a <see cref="JToken"/> from an object using the specified <see cref="JsonSerializer"/>.
        /// </summary>
        /// <param name="o">The object that will be used to create <see cref="JToken"/>.</param>
        /// <param name="jsonSerializer">The <see cref="JsonSerializer"/> that will be used when reading the object.</param>
        /// <returns>A <see cref="JToken"/> with the value of the specified object</returns>
        public static JToken FromObject(object o, JsonSerializer jsonSerializer)
        {
            return FromObjectInternal(o, jsonSerializer);
        }

        /// <summary>
        /// Creates the specified .NET type from the <see cref="JToken"/>.
        /// </summary>
        /// <typeparam name="T">The object type that the token will be deserialized to.</typeparam>
        /// <returns>The new object created from the JSON value.</returns>
        public T ToObject<T>()
        {
            return (T) ToObject(typeof (T));
        }

        /// <summary>
        /// Creates the specified .NET type from the <see cref="JToken"/>.
        /// </summary>
        /// <param name="objectType">The object type that the token will be deserialized to.</param>
        /// <returns>The new object created from the JSON value.</returns>
        public object ToObject(Type objectType)
        {
            if (JsonConvert.DefaultSettings != null) return ToObject(objectType, JsonSerializer.CreateDefault());
            bool isEnum;
            PrimitiveTypeCode typeCode = ConvertUtils.GetTypeCode(objectType, out isEnum);

            if (isEnum && Type == JTokenType.String)
            {
                var enumType = objectType.IsEnum() ? objectType : Nullable.GetUnderlyingType(objectType);
                try
                {
                    return Enum.Parse(enumType, (string) this, true);
                }
                catch (Exception ex)
                {
                    throw new ArgumentException("Could not convert '{0}' to {1}.".FormatWith(CultureInfo.InvariantCulture, (string) this, enumType.Name), ex);
                }
            }

            switch (typeCode)
            {
                case PrimitiveTypeCode.BooleanNullable:
                    return (bool?) this;
                case PrimitiveTypeCode.Boolean:
                    return (bool) this;
                case PrimitiveTypeCode.CharNullable:
                    return (char?) this;
                case PrimitiveTypeCode.Char:
                    return (char) this;
                case PrimitiveTypeCode.SByte:
                    return (sbyte?) this;
                case PrimitiveTypeCode.SByteNullable:
                    return (sbyte) this;
                case PrimitiveTypeCode.ByteNullable:
                    return (byte?) this;
                case PrimitiveTypeCode.Byte:
                    return (byte) this;
                case PrimitiveTypeCode.Int16Nullable:
                    return (short?) this;
                case PrimitiveTypeCode.Int16:
                    return (short) this;
                case PrimitiveTypeCode.UInt16Nullable:
                    return (ushort?) this;
                case PrimitiveTypeCode.UInt16:
                    return (ushort) this;
                case PrimitiveTypeCode.Int32Nullable:
                    return (int?) this;
                case PrimitiveTypeCode.Int32:
                    return (int) this;
                case PrimitiveTypeCode.UInt32Nullable:
                    return (uint?) this;
                case PrimitiveTypeCode.UInt32:
                    return (uint) this;
                case PrimitiveTypeCode.Int64Nullable:
                    return (long?) this;
                case PrimitiveTypeCode.Int64:
                    return (long) this;
                case PrimitiveTypeCode.UInt64Nullable:
                    return (ulong?) this;
                case PrimitiveTypeCode.UInt64:
                    return (ulong) this;
                case PrimitiveTypeCode.SingleNullable:
                    return (float?) this;
                case PrimitiveTypeCode.Single:
                    return (float) this;
                case PrimitiveTypeCode.DoubleNullable:
                    return (double?) this;
                case PrimitiveTypeCode.Double:
                    return (double) this;
                case PrimitiveTypeCode.DecimalNullable:
                    return (decimal?) this;
                case PrimitiveTypeCode.Decimal:
                    return (decimal) this;
                case PrimitiveTypeCode.DateTimeNullable:
                    return (DateTime?) this;
                case PrimitiveTypeCode.DateTime:
                    return (DateTime) this;
                case PrimitiveTypeCode.DateTimeOffsetNullable:
                    return (DateTimeOffset?) this;
                case PrimitiveTypeCode.DateTimeOffset:
                    return (DateTimeOffset) this;
                case PrimitiveTypeCode.String:
                    return (string) this;
                case PrimitiveTypeCode.GuidNullable:
                    return (Guid?) this;
                case PrimitiveTypeCode.Guid:
                    return (Guid) this;
                case PrimitiveTypeCode.Uri:
                    return (Uri) this;
                case PrimitiveTypeCode.TimeSpanNullable:
                    return (TimeSpan?) this;
                case PrimitiveTypeCode.TimeSpan:
                    return (TimeSpan) this;
                case PrimitiveTypeCode.BigIntegerNullable:
                    return ToBigIntegerNullable(this);
                case PrimitiveTypeCode.BigInteger:
                    return ToBigInteger(this);
            }

            return ToObject(objectType, JsonSerializer.CreateDefault());
        }

        /// <summary>
        /// Creates the specified .NET type from the <see cref="JToken"/> using the specified <see cref="JsonSerializer"/>.
        /// </summary>
        /// <typeparam name="T">The object type that the token will be deserialized to.</typeparam>
        /// <param name="jsonSerializer">The <see cref="JsonSerializer"/> that will be used when creating the object.</param>
        /// <returns>The new object created from the JSON value.</returns>
        public T ToObject<T>(JsonSerializer jsonSerializer)
        {
            return (T) ToObject(typeof (T), jsonSerializer);
        }

        /// <summary>
        /// Creates the specified .NET type from the <see cref="JToken"/> using the specified <see cref="JsonSerializer"/>.
        /// </summary>
        /// <param name="objectType">The object type that the token will be deserialized to.</param>
        /// <param name="jsonSerializer">The <see cref="JsonSerializer"/> that will be used when creating the object.</param>
        /// <returns>The new object created from the JSON value.</returns>
        public object ToObject(Type objectType, JsonSerializer jsonSerializer)
        {
            ValidationUtils.ArgumentNotNull(jsonSerializer, "jsonSerializer");
            using (var jsonReader = new JTokenReader(this))
                return jsonSerializer.Deserialize(jsonReader, objectType);
        }

        /// <summary>
        /// Creates a <see cref="JToken"/> from a <see cref="JsonReader"/>.
        /// </summary>
        /// <param name="reader">An <see cref="JsonReader"/> positioned at the token to read into this <see cref="JToken"/>.</param>
        /// <returns>
        /// An <see cref="JToken"/> that contains the token and its descendant tokens
        /// that were read from the reader. The runtime type of the token is determined
        /// by the token type of the first token encountered in the reader.
        /// </returns>
        public static JToken ReadFrom(JsonReader reader)
        {
            ValidationUtils.ArgumentNotNull(reader, "reader");

            if (reader.TokenType == JsonToken.None)
                if (!reader.Read()) throw JsonReaderException.Create(reader, "Error reading JToken from JsonReader.");

            var lineInfo = reader as IJsonLineInfo;

            switch (reader.TokenType)
            {
                case JsonToken.StartObject:
                    return JObject.Load(reader);
                case JsonToken.StartArray:
                    return JArray.Load(reader);
                case JsonToken.StartConstructor:
                    return JConstructor.Load(reader);
                case JsonToken.PropertyName:
                    return JProperty.Load(reader);
                case JsonToken.String:
                case JsonToken.Integer:
                case JsonToken.Float:
                case JsonToken.Date:
                case JsonToken.Boolean:
                case JsonToken.Bytes:
                    var v = new JValue(reader.Value);
                    v.SetLineInfo(lineInfo);
                    return v;
                case JsonToken.Comment:
                    v = JValue.CreateComment(reader.Value.ToString());
                    v.SetLineInfo(lineInfo);
                    return v;
                case JsonToken.Null:
                    v = JValue.CreateNull();
                    v.SetLineInfo(lineInfo);
                    return v;
                case JsonToken.Undefined:
                    v = JValue.CreateUndefined();
                    v.SetLineInfo(lineInfo);
                    return v;
                default:
                    throw JsonReaderException.Create(reader, "Error reading JToken from JsonReader. Unexpected token: {0}".FormatWith(CultureInfo.InvariantCulture, reader.TokenType));
            }
        }

        /// <summary>
        /// Load a <see cref="JToken"/> from a string that contains JSON.
        /// </summary>
        /// <param name="json">A <see cref="String"/> that contains JSON.</param>
        /// <returns>A <see cref="JToken"/> populated from the string that contains JSON.</returns>
        public static JToken Parse(string json)
        {
            using (JsonReader reader = new JsonTextReader(new StringReader(json)))
            {
                var t = Load(reader);
                if (reader.Read() && reader.TokenType != JsonToken.Comment) throw JsonReaderException.Create(reader, "Additional text found in JSON string after parsing content.");
                return t;
            }
        }

        /// <summary>
        /// Creates a <see cref="JToken"/> from a <see cref="JsonReader"/>.
        /// </summary>
        /// <param name="reader">An <see cref="JsonReader"/> positioned at the token to read into this <see cref="JToken"/>.</param>
        /// <returns>
        /// An <see cref="JToken"/> that contains the token and its descendant tokens
        /// that were read from the reader. The runtime type of the token is determined
        /// by the token type of the first token encountered in the reader.
        /// </returns>
        public static JToken Load(JsonReader reader)
        {
            return ReadFrom(reader);
        }

        internal void SetLineInfo(IJsonLineInfo lineInfo)
        {
            if (lineInfo == null || !lineInfo.HasLineInfo()) return;
            SetLineInfo(lineInfo.LineNumber, lineInfo.LinePosition);
        }

        private class LineInfoAnnotation
        {
            internal readonly int LineNumber;
            internal readonly int LinePosition;

            public LineInfoAnnotation(int lineNumber, int linePosition)
            {
                LineNumber = lineNumber;
                LinePosition = linePosition;
            }
        }

        internal void SetLineInfo(int lineNumber, int linePosition)
        {
            AddAnnotation(new LineInfoAnnotation(lineNumber, linePosition));
        }

        bool IJsonLineInfo.HasLineInfo()
        {
            return (Annotation<LineInfoAnnotation>() != null);
        }

        int IJsonLineInfo.LineNumber
        {
            get
            {
                var annotation = Annotation<LineInfoAnnotation>();
                return annotation != null ? annotation.LineNumber : 0;
            }
        }

        int IJsonLineInfo.LinePosition
        {
            get
            {
                var annotation = Annotation<LineInfoAnnotation>();
                return annotation != null ? annotation.LinePosition : 0;
            }
        }

        /// <summary>
        /// Selects a <see cref="JToken"/> using a JPath expression. Selects the token that matches the object path.
        /// </summary>
        /// <param name="path">
        /// A <see cref="String"/> that contains a JPath expression.
        /// </param>
        /// <returns>A <see cref="JToken"/>, or null.</returns>
        public JToken SelectToken(string path)
        {
            return SelectToken(path, false);
        }

        /// <summary>
        /// Selects a <see cref="JToken"/> using a JPath expression. Selects the token that matches the object path.
        /// </summary>
        /// <param name="path">
        /// A <see cref="String"/> that contains a JPath expression.
        /// </param>
        /// <param name="errorWhenNoMatch">A flag to indicate whether an error should be thrown if no tokens are found when evaluating part of the expression.</param>
        /// <returns>A <see cref="JToken"/>.</returns>
        public JToken SelectToken(string path, bool errorWhenNoMatch)
        {
            var p = new JPath(path);

            JToken token = null;
            foreach (var t in p.Evaluate(this, errorWhenNoMatch))
            {
                if (token != null) throw new JsonException("Path returned multiple tokens.");
                token = t;
            }

            return token;
        }

        /// <summary>
        /// Selects a collection of elements using a JPath expression.
        /// </summary>
        /// <param name="path">
        /// A <see cref="String"/> that contains a JPath expression.
        /// </param>
        /// <returns>An <see cref="IEnumerable{JToken}"/> that contains the selected elements.</returns>
        public IEnumerable<JToken> SelectTokens(string path)
        {
            return SelectTokens(path, false);
        }

        /// <summary>
        /// Selects a collection of elements using a JPath expression.
        /// </summary>
        /// <param name="path">
        /// A <see cref="String"/> that contains a JPath expression.
        /// </param>
        /// <param name="errorWhenNoMatch">A flag to indicate whether an error should be thrown if no tokens are found when evaluating part of the expression.</param>
        /// <returns>An <see cref="IEnumerable{JToken}"/> that contains the selected elements.</returns>
        public IEnumerable<JToken> SelectTokens(string path, bool errorWhenNoMatch)
        {
            var p = new JPath(path);
            return p.Evaluate(this, errorWhenNoMatch);
        }

        /// <summary>
        /// Returns the <see cref="T:System.Dynamic.DynamicMetaObject"/> responsible for binding operations performed on this object.
        /// </summary>
        /// <param name="parameter">The expression tree representation of the runtime value.</param>
        /// <returns>
        /// The <see cref="T:System.Dynamic.DynamicMetaObject"/> to bind this object.
        /// </returns>
        protected virtual DynamicMetaObject GetMetaObject(Expression parameter)
        {
            return new DynamicProxyMetaObject<JToken>(parameter, this, new DynamicProxy<JToken>(), true);
        }

        /// <summary>
        /// Returns the <see cref="T:System.Dynamic.DynamicMetaObject"/> responsible for binding operations performed on this object.
        /// </summary>
        /// <param name="parameter">The expression tree representation of the runtime value.</param>
        /// <returns>
        /// The <see cref="T:System.Dynamic.DynamicMetaObject"/> to bind this object.
        /// </returns>
        DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter)
        {
            return GetMetaObject(parameter);
        }

        object ICloneable.Clone()
        {
            return DeepClone();
        }

        /// <summary>
        /// Creates a new instance of the <see cref="JToken"/>. All child tokens are recursively cloned.
        /// </summary>
        /// <returns>A new instance of the <see cref="JToken"/>.</returns>
        public JToken DeepClone()
        {
            return CloneToken();
        }

        /// <summary>
        /// Adds an object to the annotation list of this <see cref="JToken"/>.
        /// </summary>
        /// <param name="annotation">The annotation to add.</param>
        public void AddAnnotation(object annotation)
        {
            if (annotation == null) throw new ArgumentNullException("annotation");

            if (_annotations == null)
                _annotations = (annotation is object[]) ? new[] {annotation} : annotation;
            else
            {
                var annotations = _annotations as object[];
                if (annotations == null)
                    _annotations = new[] {_annotations, annotation};
                else
                {
                    var index = 0;
                    while (index < annotations.Length && annotations[index] != null)
                        index++;
                    if (index == annotations.Length)
                    {
                        Array.Resize(ref annotations, index*2);
                        _annotations = annotations;
                    }
                    annotations[index] = annotation;
                }
            }
        }

        /// <summary>
        /// Get the first annotation object of the specified type from this <see cref="JToken"/>.
        /// </summary>
        /// <typeparam name="T">The type of the annotation to retrieve.</typeparam>
        /// <returns>The first annotation object that matches the specified type, or <c>null</c> if no annotation is of the specified type.</returns>
        public T Annotation<T>() where T : class
        {
            if (_annotations == null) return default(T);
            var annotations = _annotations as object[];
            if (annotations == null)
                return (_annotations as T);
            for (var i = 0; i < annotations.Length; i++)
            {
                var annotation = annotations[i];
                if (annotation == null) break;
                var local = annotation as T;
                if (local != null)
                    return local;
            }

            return default(T);
        }

        /// <summary>
        /// Gets the first annotation object of the specified type from this <see cref="JToken"/>.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> of the annotation to retrieve.</param>
        /// <returns>The first annotation object that matches the specified type, or <c>null</c> if no annotation is of the specified type.</returns>
        public object Annotation(Type type)
        {
            if (type == null) throw new ArgumentNullException("type");

            if (_annotations == null) return null;
            var annotations = _annotations as object[];
            if (annotations == null)
            {
                if (type.IsInstanceOfType(_annotations)) return _annotations;
            }
            else
            {
                for (var i = 0; i < annotations.Length; i++)
                {
                    var o = annotations[i];
                    if (o == null) break;
                    if (type.IsInstanceOfType(o)) return o;
                }
            }

            return null;
        }

        /// <summary>
        /// Gets a collection of annotations of the specified type for this <see cref="JToken"/>.
        /// </summary>
        /// <typeparam name="T">The type of the annotations to retrieve.</typeparam>
        /// <returns>An <see cref="IEnumerable{T}"/>  that contains the annotations for this <see cref="JToken"/>.</returns>
        public IEnumerable<T> Annotations<T>() where T : class
        {
            if (_annotations == null)
                yield break;

            var annotations = _annotations as object[];
            if (annotations != null)
            {
                for (var i = 0; i < annotations.Length; i++)
                {
                    var o = annotations[i];
                    if (o == null) break;
                    var casted = o as T;
                    if (casted != null)
                        yield return casted;
                }
                yield break;
            }

            var annotation = _annotations as T;
            if (annotation == null)
                yield break;

            yield return annotation;
        }

        /// <summary>
        /// Gets a collection of annotations of the specified type for this <see cref="JToken"/>.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> of the annotations to retrieve.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> of <see cref="Object"/> that contains the annotations that match the specified type for this <see cref="JToken"/>.</returns>
        public IEnumerable<object> Annotations(Type type)
        {
            if (type == null) throw new ArgumentNullException("type");

            if (_annotations == null)
                yield break;

            var annotations = _annotations as object[];
            if (annotations != null)
            {
                for (var i = 0; i < annotations.Length; i++)
                {
                    var o = annotations[i];
                    if (o == null) break;
                    if (type.IsInstanceOfType(o))
                        yield return o;
                }
                yield break;
            }

            if (!type.IsInstanceOfType(_annotations))
                yield break;

            yield return _annotations;
        }

        /// <summary>
        /// Removes the annotations of the specified type from this <see cref="JToken"/>.
        /// </summary>
        /// <typeparam name="T">The type of annotations to remove.</typeparam>
        public void RemoveAnnotations<T>() where T : class
        {
            if (_annotations == null) return;
            var annotations = _annotations as object[];
            if (annotations == null)
            {
                if (_annotations is T)
                    _annotations = null;
            }
            else
            {
                var index = 0;
                var keepCount = 0;
                while (index < annotations.Length)
                {
                    var obj2 = annotations[index];
                    if (obj2 == null) break;
                    if (!(obj2 is T))
                        annotations[keepCount++] = obj2;
                    index++;
                }

                if (keepCount != 0)
                    while (keepCount < index)
                        annotations[keepCount++] = null;
                else
                    _annotations = null;
            }
        }

        /// <summary>
        /// Removes the annotations of the specified type from this <see cref="JToken"/>.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> of annotations to remove.</param>
        public void RemoveAnnotations(Type type)
        {
            if (type == null) throw new ArgumentNullException("type");

            if (_annotations == null) return;
            var annotations = _annotations as object[];
            if (annotations == null)
            {
                if (type.IsInstanceOfType(_annotations))
                    _annotations = null;
            }
            else
            {
                var index = 0;
                var keepCount = 0;
                while (index < annotations.Length)
                {
                    var o = annotations[index];
                    if (o == null) break;
                    if (!type.IsInstanceOfType(o))
                        annotations[keepCount++] = o;
                    index++;
                }

                if (keepCount != 0)
                    while (keepCount < index)
                        annotations[keepCount++] = null;
                else
                    _annotations = null;
            }
        }
    }
}
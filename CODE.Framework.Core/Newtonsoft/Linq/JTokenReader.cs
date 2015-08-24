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
using CODE.Framework.Core.Newtonsoft.Utilities;

namespace CODE.Framework.Core.Newtonsoft.Linq
{
    /// <summary>
    /// Represents a reader that provides fast, non-cached, forward-only access to serialized JSON data.
    /// </summary>
    public class JTokenReader : JsonReader, IJsonLineInfo
    {
        private readonly string _initialPath;
        private readonly JToken _root;
        private JToken _parent;
        private JToken _current;

        /// <summary>
        /// Gets the <see cref="JToken"/> at the reader's current position.
        /// </summary>
        public JToken CurrentToken
        {
            get { return _current; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JTokenReader"/> class.
        /// </summary>
        /// <param name="token">The token to read from.</param>
        public JTokenReader(JToken token)
        {
            ValidationUtils.ArgumentNotNull(token, "token");
            _root = token;
        }

        internal JTokenReader(JToken token, string initialPath)
            : this(token)
        {
            _initialPath = initialPath;
        }

        /// <summary>
        /// Reads the next JSON token from the stream as a <see cref="byte"/>[].
        /// </summary>
        /// <returns>
        /// A <see cref="byte"/>[] or a null reference if the next JSON token is null. This method will return <c>null</c> at the end of an array.
        /// </returns>
        public override byte[] ReadAsBytes()
        {
            return ReadAsBytesInternal();
        }

        /// <summary>
        /// Reads the next JSON token from the stream as a <see cref="Nullable{T}"/>.
        /// </summary>
        /// <returns>A <see cref="Nullable{Decimal}"/>. This method will return <c>null</c> at the end of an array.</returns>
        public override decimal? ReadAsDecimal()
        {
            return ReadAsDecimalInternal();
        }

        /// <summary>
        /// Reads the next JSON token from the stream as a <see cref="Nullable{Int32}"/>.
        /// </summary>
        /// <returns>A <see cref="Nullable{Int32}"/>. This method will return <c>null</c> at the end of an array.</returns>
        public override int? ReadAsInt32()
        {
            return ReadAsInt32Internal();
        }

        /// <summary>
        /// Reads the next JSON token from the stream as a <see cref="String"/>.
        /// </summary>
        /// <returns>A <see cref="String"/>. This method will return <c>null</c> at the end of an array.</returns>
        public override string ReadAsString()
        {
            return ReadAsStringInternal();
        }

        /// <summary>
        /// Reads the next JSON token from the stream as a <see cref="Nullable{DateTime}"/>.
        /// </summary>
        /// <returns>A <see cref="String"/>. This method will return <c>null</c> at the end of an array.</returns>
        public override DateTime? ReadAsDateTime()
        {
            return ReadAsDateTimeInternal();
        }

        /// <summary>
        /// Reads the next JSON token from the stream as a <see cref="Nullable{DateTimeOffset}"/>.
        /// </summary>
        /// <returns>A <see cref="Nullable{DateTimeOffset}"/>. This method will return <c>null</c> at the end of an array.</returns>
        public override DateTimeOffset? ReadAsDateTimeOffset()
        {
            return ReadAsDateTimeOffsetInternal();
        }

        internal override bool ReadInternal()
        {
            if (CurrentState != State.Start)
            {
                if (_current == null) return false;
                var container = _current as JContainer;
                return container != null && _parent != container ? ReadInto(container) : ReadOver(_current);
            }
            _current = _root;
            SetToken(_current);
            return true;
        }

        /// <summary>
        /// Reads the next JSON token from the stream.
        /// </summary>
        /// <returns>
        /// true if the next token was read successfully; false if there are no more tokens to read.
        /// </returns>
        public override bool Read()
        {
            _readType = ReadType.Read;
            return ReadInternal();
        }

        private bool ReadOver(JToken t)
        {
            if (t == _root) return ReadToEnd();
            var next = t.Next;
            if ((next == null || next == t) || t == t.Parent.Last)
                return t.Parent == null ? ReadToEnd() : SetEnd(t.Parent);
            _current = next;
            SetToken(_current);
            return true;
        }

        private bool ReadToEnd()
        {
            _current = null;
            SetToken(JsonToken.None);
            return false;
        }

        private JsonToken? GetEndToken(JContainer c)
        {
            switch (c.Type)
            {
                case JTokenType.Object:
                    return JsonToken.EndObject;
                case JTokenType.Array:
                    return JsonToken.EndArray;
                case JTokenType.Constructor:
                    return JsonToken.EndConstructor;
                case JTokenType.Property:
                    return null;
                default:
                    throw MiscellaneousUtils.CreateArgumentOutOfRangeException("Type", c.Type, "Unexpected JContainer type.");
            }
        }

        private bool ReadInto(JContainer c)
        {
            var firstChild = c.First;
            if (firstChild == null)
                return SetEnd(c);
            SetToken(firstChild);
            _current = firstChild;
            _parent = c;
            return true;
        }

        private bool SetEnd(JContainer c)
        {
            var endToken = GetEndToken(c);
            if (endToken != null)
            {
                SetToken(endToken.Value);
                _current = c;
                _parent = c;
                return true;
            }
            return ReadOver(c);
        }

        private void SetToken(JToken token)
        {
            switch (token.Type)
            {
                case JTokenType.Object:
                    SetToken(JsonToken.StartObject);
                    break;
                case JTokenType.Array:
                    SetToken(JsonToken.StartArray);
                    break;
                case JTokenType.Constructor:
                    SetToken(JsonToken.StartConstructor, ((JConstructor)token).Name);
                    break;
                case JTokenType.Property:
                    SetToken(JsonToken.PropertyName, ((JProperty)token).Name);
                    break;
                case JTokenType.Comment:
                    SetToken(JsonToken.Comment, ((JValue)token).Value);
                    break;
                case JTokenType.Integer:
                    SetToken(JsonToken.Integer, ((JValue)token).Value);
                    break;
                case JTokenType.Float:
                    SetToken(JsonToken.Float, ((JValue)token).Value);
                    break;
                case JTokenType.String:
                    SetToken(JsonToken.String, ((JValue)token).Value);
                    break;
                case JTokenType.Boolean:
                    SetToken(JsonToken.Boolean, ((JValue)token).Value);
                    break;
                case JTokenType.Null:
                    SetToken(JsonToken.Null, ((JValue)token).Value);
                    break;
                case JTokenType.Undefined:
                    SetToken(JsonToken.Undefined, ((JValue)token).Value);
                    break;
                case JTokenType.Date:
                    SetToken(JsonToken.Date, ((JValue)token).Value);
                    break;
                case JTokenType.Raw:
                    SetToken(JsonToken.Raw, ((JValue)token).Value);
                    break;
                case JTokenType.Bytes:
                    SetToken(JsonToken.Bytes, ((JValue)token).Value);
                    break;
                case JTokenType.Guid:
                    SetToken(JsonToken.String, SafeToString(((JValue)token).Value));
                    break;
                case JTokenType.Uri:
                    SetToken(JsonToken.String, SafeToString(((JValue)token).Value));
                    break;
                case JTokenType.TimeSpan:
                    SetToken(JsonToken.String, SafeToString(((JValue)token).Value));
                    break;
                default:
                    throw MiscellaneousUtils.CreateArgumentOutOfRangeException("Type", token.Type, "Unexpected JTokenType.");
            }
        }

        private string SafeToString(object value)
        {
            return (value != null) ? value.ToString() : null;
        }

        bool IJsonLineInfo.HasLineInfo()
        {
            if (CurrentState == State.Start) return false;
            IJsonLineInfo info = _current;
            return (info != null && info.HasLineInfo());
        }

        int IJsonLineInfo.LineNumber
        {
            get
            {
                if (CurrentState == State.Start) return 0;
                IJsonLineInfo info = _current;
                return info != null ? info.LineNumber : 0;
            }
        }

        int IJsonLineInfo.LinePosition
        {
            get
            {
                if (CurrentState == State.Start) return 0;
                IJsonLineInfo info = _current;
                return info != null ? info.LinePosition : 0;
            }
        }

        /// <summary>
        /// Gets the path of the current JSON token. 
        /// </summary>
        public override string Path
        {
            get
            {
                var path = base.Path;

                if (string.IsNullOrEmpty(_initialPath)) return path;
                if (string.IsNullOrEmpty(path)) return _initialPath;
                if (path.StartsWith('['))
                    path = _initialPath + path;
                else
                    path = _initialPath + "." + path;
                return path;
            }
        }
    }
}

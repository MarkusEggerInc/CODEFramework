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

namespace CODE.Framework.Core.Newtonsoft.Utilities
{
    internal class PropertyNameTable
    {
        // used to defeat hashtable DoS attack where someone passes in lots of strings that hash to the same hash code
        private static readonly int HashCodeRandomizer;

        private int _count;
        private Entry[] _entries;
        private int _mask = 31;

        /// <summary>
        /// Initializes static members of the <see cref="PropertyNameTable"/> class.
        /// </summary>
        static PropertyNameTable()
        {
            HashCodeRandomizer = Environment.TickCount;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyNameTable"/> class.
        /// </summary>
        public PropertyNameTable()
        {
            _entries = new Entry[_mask + 1];
        }

        /// <summary>
        /// Gets the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="start">The start.</param>
        /// <param name="length">The length.</param>
        /// <returns>System.String.</returns>
        public string Get(char[] key, int start, int length)
        {
            if (length == 0)
                return string.Empty;

            var hashCode = length + HashCodeRandomizer;
            hashCode += (hashCode << 7) ^ key[start];
            var end = start + length;
            for (var i = start + 1; i < end; i++)
                hashCode += (hashCode << 7) ^ key[i];
            hashCode -= hashCode >> 17;
            hashCode -= hashCode >> 11;
            hashCode -= hashCode >> 5;
            for (Entry entry = _entries[hashCode & _mask]; entry != null; entry = entry.Next)
                if (entry.HashCode == hashCode && TextEquals(entry.Value, key, start, length))
                    return entry.Value;

            return null;
        }

        /// <summary>
        /// Adds the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.ArgumentNullException">key</exception>
        public string Add(string key)
        {
            if (key == null)
                throw new ArgumentNullException("key");

            var length = key.Length;
            if (length == 0)
                return string.Empty;

            var hashCode = length + HashCodeRandomizer;
            for (var i = 0; i < key.Length; i++)
                hashCode += (hashCode << 7) ^ key[i];
            hashCode -= hashCode >> 17;
            hashCode -= hashCode >> 11;
            hashCode -= hashCode >> 5;
            for (var entry = _entries[hashCode & _mask]; entry != null; entry = entry.Next)
                if (entry.HashCode == hashCode && entry.Value.Equals(key))
                    return entry.Value;

            return AddEntry(key, hashCode);
        }

        /// <summary>
        /// Adds the entry.
        /// </summary>
        /// <param name="str">The string.</param>
        /// <param name="hashCode">The hash code.</param>
        /// <returns>System.String.</returns>
        private string AddEntry(string str, int hashCode)
        {
            var index = hashCode & _mask;
            var entry = new Entry(str, hashCode, _entries[index]);
            _entries[index] = entry;
            if (_count++ == _mask)
                Grow();
            return entry.Value;
        }

        /// <summary>
        /// Grows this instance.
        /// </summary>
        private void Grow()
        {
            var entries = _entries;
            var newMask = (_mask * 2) + 1;
            var newEntries = new Entry[newMask + 1];

            for (var i = 0; i < entries.Length; i++)
            {
                Entry next;
                for (var entry = entries[i]; entry != null; entry = next)
                {
                    var index = entry.HashCode & newMask;
                    next = entry.Next;
                    entry.Next = newEntries[index];
                    newEntries[index] = entry;
                }
            }
            _entries = newEntries;
            _mask = newMask;
        }

        /// <summary>
        /// Texts the equals.
        /// </summary>
        /// <param name="str1">The STR1.</param>
        /// <param name="str2">The STR2.</param>
        /// <param name="str2Start">The STR2 start.</param>
        /// <param name="str2Length">Length of the STR2.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        private static bool TextEquals(string str1, char[] str2, int str2Start, int str2Length)
        {
            if (str1.Length != str2Length)
                return false;

            for (var i = 0; i < str1.Length; i++)
                if (str1[i] != str2[str2Start + i])
                    return false;
            return true;
        }

        /// <summary>
        /// For internal use only
        /// </summary>
        private class Entry
        {
            internal readonly string Value;
            internal readonly int HashCode;
            internal Entry Next;

            /// <summary>
            /// Initializes a new instance of the <see cref="Entry"/> class.
            /// </summary>
            /// <param name="value">The value.</param>
            /// <param name="hashCode">The hash code.</param>
            /// <param name="next">The next.</param>
            internal Entry(string value, int hashCode, Entry next)
            {
                Value = value;
                HashCode = hashCode;
                Next = next;
            }
        }
    }
}
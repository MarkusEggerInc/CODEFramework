using System;
using System.Text;

namespace CODE.Framework.Core.Utilities
{
    /// <summary>
    /// This class provides useful methods for dealing with HTTP
    /// </summary>
    public static class HttpHelper
    {
        /// <summary>
        /// Decodes a URL string/value into clear text
        /// </summary>
        /// <param name="encodedValue">The encoded value.</param>
        /// <returns>Clear text version of the string</returns>
        /// <remarks>This implementation is identical to the one provided by the .NET Framework in the WebUtility class, but it is provided here without dependencies on any server components.</remarks>
        public static string UrlDecode(string encodedValue)
        {
            if (encodedValue == null) return null;
            var count = encodedValue.Length;
            var helper = new UrlDecoder(count, Encoding.UTF8);

            // go through the string's chars collapsing %XX and
            // appending each char as char, with exception of %XX constructs
            // that are appended as bytes

            for (var position = 0; position < count; position++)
            {
                var ch = encodedValue[position];

                if (ch == '+') ch = ' ';
                else if (ch == '%' && position < count - 2)
                {
                    var h1 = HexToInt(encodedValue[position + 1]);
                    var h2 = HexToInt(encodedValue[position + 2]);

                    if (h1 < 0 || h2 < 0) continue;
                    // valid 2 hex chars
                    var b = (byte) ((h1 << 4) | h2);
                    position += 2;

                    // don't add as char
                    helper.AddByte(b);
                    continue;
                }

                if ((ch & 0xFF80) == 0)
                    helper.AddByte((byte)ch); // 7 bit have to go as bytes because of Unicode
                else
                    helper.AddChar(ch);
            }

            return helper.GetString();
        }

        private class UrlDecoder
        {
            private readonly int _bufferSize;

            // Accumulate characters in a special array
            private int _numChars;
            private readonly char[] _charBuffer;

            // Accumulate bytes for decoding into characters in a special array
            private int _numBytes;
            private byte[] _byteBuffer;

            // Encoding to convert chars to bytes
            private readonly Encoding _encoding;

            private void FlushBytes()
            {
                if (_numBytes > 0)
                {
                    _numChars += _encoding.GetChars(_byteBuffer, 0, _numBytes, _charBuffer, _numChars);
                    _numBytes = 0;
                }
            }

            internal UrlDecoder(int bufferSize, Encoding encoding)
            {
                _bufferSize = bufferSize;
                _encoding = encoding;

                _charBuffer = new char[bufferSize];
                // byte buffer created on demand
            }

            internal void AddChar(char ch)
            {
                if (_numBytes > 0)
                    FlushBytes();

                _charBuffer[_numChars++] = ch;
            }

            internal void AddByte(byte b)
            {
                if (_byteBuffer == null)
                    _byteBuffer = new byte[_bufferSize];

                _byteBuffer[_numBytes++] = b;
            }

            internal String GetString()
            {
                if (_numBytes > 0)
                    FlushBytes();

                if (_numChars > 0)
                    return new String(_charBuffer, 0, _numChars);
                else
                    return String.Empty;
            }
        }
        
        /// <summary>
        /// Encodes the string for use in a URL
        /// </summary>
        /// <param name="value">The clear-text encodedValue</param>
        /// <returns>URL encoded string</returns>
        /// <remarks>This implementation is identical to the one provided by the .NET Framework in the WebUtility class, but it is provided here without dependencies on any server components.</remarks>
        public static string UrlEncode(string value)
        {
            if (value == null) return null;
            var bytes = Encoding.UTF8.GetBytes(value);
            return Encoding.UTF8.GetString(UrlEncode(bytes, 0, bytes.Length, false /* alwaysCreateNewReturnValue */));
        }

        private static byte[] UrlEncode(byte[] bytes, int offset, int count, bool alwaysCreateNewReturnValue)
        {
            byte[] encoded = UrlEncode(bytes, offset, count);
            return (alwaysCreateNewReturnValue && (encoded != null) && (encoded == bytes)) ? (byte[])encoded.Clone() : encoded;
        }

        private static byte[] UrlEncode(byte[] bytes, int offset, int count)
        {
            if (!ValidateUrlEncodingParameters(bytes, offset, count))
                return null;

            var cSpaces = 0;
            var cUnsafe = 0;

            // count them first
            for (var i = 0; i < count; i++)
            {
                var ch = (char)bytes[offset + i];

                if (ch == ' ') cSpaces++;
                else if (!IsUrlSafeChar(ch)) cUnsafe++;
            }

            // nothing to expand?
            if (cSpaces == 0 && cUnsafe == 0) return bytes;

            // expand not 'safe' characters into %XX, spaces to +s
            var expandedBytes = new byte[count + cUnsafe * 2];
            var pos = 0;

            for (var i = 0; i < count; i++)
            {
                var b = bytes[offset + i];
                var ch = (char)b;

                if (IsUrlSafeChar(ch)) expandedBytes[pos++] = b;
                else if (ch == ' ') expandedBytes[pos++] = (byte) '+';
                else
                {
                    expandedBytes[pos++] = (byte) '%';
                    expandedBytes[pos++] = (byte) IntToHex((b >> 4) & 0xf);
                    expandedBytes[pos++] = (byte) IntToHex(b & 0x0f);
                }
            }

            return expandedBytes;
        }

        private static bool ValidateUrlEncodingParameters(byte[] bytes, int offset, int count)
        {
            if (bytes == null && count == 0) return false;
            if (bytes == null) throw new ArgumentNullException("bytes");
            if (offset < 0 || offset > bytes.Length) throw new ArgumentOutOfRangeException("offset");
            if (count < 0 || offset + count > bytes.Length) throw new ArgumentOutOfRangeException("count");
            return true;
        }

        private static int HexToInt(char h)
        {
            return (h >= '0' && h <= '9') ? h - '0' : (h >= 'a' && h <= 'f') ? h - 'a' + 10 : (h >= 'A' && h <= 'F') ? h - 'A' + 10 : -1;
        }

        private static char IntToHex(int n)
        {
            if (n <= 9) return (char)(n + '0');
            return (char)(n - 10 + 'A');
        }

        // Set of safe chars, from RFC 1738.4 minus '+'
        private static bool IsUrlSafeChar(char ch)
        {
            if (ch >= 'a' && ch <= 'z' || ch >= 'A' && ch <= 'Z' || ch >= '0' && ch <= '9') return true;

            switch (ch)
            {
                case '-':
                case '_':
                case '.':
                case '!':
                case '*':
                case '(':
                case ')':
                    return true;
            }

            return false;
        }
    }
}

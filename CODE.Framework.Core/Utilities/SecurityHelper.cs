using System;
using System.Text;
using System.Security.Cryptography;

namespace CODE.Framework.Core.Utilities
{
    /// <summary>
    /// A simple encryption class that can be used to two-encode/decode strings and byte buffers
    /// with single method calls.
    /// </summary>
    public static class SecurityHelper
    {
        /// <summary>
        /// Encodes a stream of bytes using DES encryption with a pass key. 
        /// Lowest level method that handles all work.
        /// </summary>
        /// <param name="inputString">Byte array that represents the input string</param>
        /// <param name="encryptionKey">Encryption key used for the encryption</param>
        /// <returns>Encrypted bytes</returns>
        public static byte[] EncryptBytes(byte[] inputString, byte[] encryptionKey)
        {
            var des = new TripleDESCryptoServiceProvider {Key = encryptionKey, Mode = CipherMode.ECB};
            var transform = des.CreateEncryptor();
            var buffer = inputString;
            return transform.TransformFinalBlock(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Encrypts a string into bytes using DES encryption with a Passkey. 
        /// </summary>
        /// <param name="inputString">Input String</param>
        /// <param name="encryptionKey">Encryption Key</param>
        /// <returns>Encrypted bytes</returns>
        public static byte[] EncryptBytes(string inputString, byte[] encryptionKey)
        {
            return EncryptBytes(Encoding.ASCII.GetBytes(inputString), encryptionKey);
        }

        /// <summary>
        /// Encrypts a string using Triple DES encryption with a two way encryption key.
        /// String is returned as Base64 encoded value rather than binary.
        /// </summary>
        /// <param name="inputString">Input string</param>
        /// <param name="encryptionKey">Encryption Key</param>
        /// <returns>Base64 encoded encrypted string</returns>
        /// <remarks>
        /// The key is expected to have a length of 24 bytes.
        /// This method can be used with an arbitrary key, but make sure
        /// you use the same key for encryption and decryption.
        /// </remarks>
        public static string EncryptString(string inputString, byte[] encryptionKey)
        {
            return Convert.ToBase64String(EncryptBytes(Encoding.ASCII.GetBytes(inputString), encryptionKey));
        }

        /// <summary>
        /// Decrypts a Byte array from DES with an Encryption Key.
        /// </summary>
        /// <param name="decryptBuffer">Bytes to decrypt</param>
        /// <param name="encryptionKey">Encryption Key</param>
        /// <returns>Decrypted bytes</returns>
        public static byte[] DecryptBytes(byte[] decryptBuffer, byte[] encryptionKey)
        {
            var des = new TripleDESCryptoServiceProvider {Key = encryptionKey, Mode = CipherMode.ECB};
            var transform = des.CreateDecryptor();
            return transform.TransformFinalBlock(decryptBuffer, 0, decryptBuffer.Length);
        }

        /// <summary>
        /// Decrypts a string
        /// </summary>
        /// <param name="decryptString">String to decrypt</param>
        /// <param name="encryptionKey">Encryption Key</param>
        /// <returns>Decrypted bytes</returns>
        public static byte[] DecryptBytes(string decryptString, byte[] encryptionKey)
        {
            return DecryptBytes(Encoding.ASCII.GetBytes(decryptString), encryptionKey);
        }

        /// <summary>
        /// Decrypts a Base64 encoded string using DES encryption and a pass key that was used for 
        /// encryption.
        /// </summary>
        /// <param name="stringToDecrypt">String to decrypt</param>
        /// <param name="encryptionKey">Key</param>
        /// <returns>Decrypted string</returns>
        /// <remarks>
        /// The key is expected to have a length of 24 bytes.
        /// This method can be used with an arbitrary key, but make sure
        /// you use the same key for encryption and decryption.
        /// </remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1720:AvoidTypeNamesInParameters", MessageId = "0#", Justification = "This is a special case and the 'string' part is not identifying the type.")]
        public static string DecryptString(string stringToDecrypt, byte[] encryptionKey)
        {
            return Encoding.ASCII.GetString(DecryptBytes(Convert.FromBase64String(stringToDecrypt), encryptionKey));
        }
    }
}

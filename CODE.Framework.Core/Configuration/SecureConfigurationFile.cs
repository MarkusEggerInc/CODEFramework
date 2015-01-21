using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using CODE.Framework.Core.Utilities;

namespace CODE.Framework.Core.Configuration
{
    /// <summary>
    /// This class wraps up the functionality available natively in .NET for reading 
    /// the default settings (AppSettings) available in the config files.
    /// </summary>
    public class SecureConfigurationFile : ConfigurationSource
    {
        /// <summary>
        /// Key used to encrypt and decrypt configuration settings
        /// </summary>
        public byte[] EncryptionKey { get; set; }

        /// <summary>
        /// File name for the secure configuration file (if left empty, 'App.sconfig' will be used in the same directory as core.dll)
        /// </summary>
        public string ConfigurationFileName { get; set; }

        /// <summary>
        /// Indicates source's Friendly Name.
        /// </summary>
        public override string FriendlyName
        {
            get { return "SecureConfigurationFile"; }
        }

        /// <summary>
        /// Indicates whether the source is read-only. .NET's native AppSettings is read-only,
        /// therefore we mark this class as read-only too.
        /// </summary>
        public override bool IsReadOnly
        {
            get { return false; }
        }

        /// <summary>
        /// Determines whether the source is secure or not.
        /// </summary>
        public override bool IsSecure
        {
            get { return true; }
        }

        /// <summary>
        /// Generates a standard file name to be used for the configuration file
        /// </summary>
        /// <returns></returns>
        private string GetConfigurationFileName()
        {
            if (string.IsNullOrEmpty(ConfigurationFileName))
                return StringHelper.AddBS(StringHelper.JustPath(System.Reflection.Assembly.GetExecutingAssembly().Location)) + "App.sconfig";
            return ConfigurationFileName;
        }

        /// <summary>
        /// Read settings from native .NET object and feed settings into our own object.
        /// </summary>
        public override void Read()
        {
            var fileName = GetConfigurationFileName();
            if (!File.Exists(fileName))
            {
                lock (this)
                    Settings.Clear();
                return;
            }

            var encryptedFileContents = StringHelper.FromFile(fileName);
            if (EncryptionKey == null) throw new Exception("SecureConfigurationFile.EncryptionKey must be set!");
            var fileContents = SecurityHelper.DecryptString(encryptedFileContents, EncryptionKey);
            var xml = new XmlDocument();
            xml.LoadXml(fileContents);

            var pairs = xml.SelectNodes("*/*");
            if (pairs == null) return;
            var dictionary = new Dictionary<string, string>();
            foreach (XmlNode pair in pairs)
            {
                var name = pair.SelectSingleNode("@name");
                var value = pair.SelectSingleNode("@value");
                if (name != null && !string.IsNullOrEmpty(name.Value) && value != null && !string.IsNullOrEmpty(value.Value))
                    dictionary.Add(name.Value, value.Value);
            }

            lock (Settings)
            {
                Settings.Clear();
                foreach (var key in dictionary.Keys)
                    Settings.Add(key, dictionary[key]);
            }
        }

        /// <summary>
        /// Checks whether a given source type is supported, according to Enum ConfigurationSourceTypes.
        /// </summary>
        /// <param name="sourceType">The source type.</param>
        /// <returns>True/False for supported or not.</returns>
        public override bool SupportsType(ConfigurationSourceTypes sourceType)
        {
            return false;
        }

        /// <summary>
        /// Persists settings from memory into storage.
        /// </summary>
        public override void Write()
        {
            var fileName = GetConfigurationFileName();

            var xml = new XmlDocument();
            xml.LoadXml("<?xml version=\"1.0\"?><settings />");
            var root = xml.SelectSingleNode("*");
            if (root == null) return;

            var allSettings = Settings.GetAllKeysAndValues();

            foreach (var key in allSettings.Keys)
            {
                var newNode = xml.CreateElement("setting");

                var nameAttribute = xml.CreateAttribute("name");
                nameAttribute.Value = key;
                newNode.Attributes.Append(nameAttribute);

                var valueAttribute = xml.CreateAttribute("value");
                valueAttribute.Value = Settings[key].ToString();
                newNode.Attributes.Append(valueAttribute);

                root.AppendChild(newNode);
            }

            using (var stream = new MemoryStream())
            {
                xml.Save(stream);
                if (EncryptionKey == null) throw new Exception("SecureConfigurationFile.EncryptionKey must be set!");
                var encryptedfileContents = SecurityHelper.EncryptString(StreamHelper.ToString(stream), EncryptionKey);
                StringHelper.ToFile(encryptedfileContents, fileName);
            }
        }
    }
}
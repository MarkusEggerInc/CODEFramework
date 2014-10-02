using System;
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
        /// Default Constructor.
        /// </summary>
        public SecureConfigurationFile()
        {
            Read();
        }

        /// <summary>
        /// Key used to encrypt and decrypt configuration settings
        /// </summary>
        public static byte[] EncryptionKey { get; set; }

        static SecureConfigurationFile()
        {
            ConfigurationFileName = string.Empty;
        }

        /// <summary>
        /// File name for the secure configuration file (if left empty, 'App.sconfig' will be used in the same directory as core.dll)
        /// </summary>
        public static string ConfigurationFileName { get; set; }

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
        private static string GetConfigurationFileName()
        {
            if (string.IsNullOrEmpty(ConfigurationFileName))
                return StringHelper.AddBS(StringHelper.JustPath(System.Reflection.Assembly.GetExecutingAssembly().Location)) + "App.sconfig";
            return ConfigurationFileName;
        }

        /// <summary>
        /// Read settings from native .NET object and feed settings into our own object.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("EPS.MilosBusinessObjects", "EPS0016:OnlyMilosConfigurationSystemShouldBeUsed", Justification = "Not here, as this is the class that implements the Milos configuration system.")]
        public override void Read()
        {
            string fileName = GetConfigurationFileName();
            if (!File.Exists(fileName))
            {
                Settings.Clear();
                return;
            }

            string encryptedFileContents = StringHelper.FromFile(fileName);
            if (EncryptionKey == null) throw new Exception("SecureConfigurationFile.EncryptionKey must be set!");
            string fileContents = SecurityHelper.DecryptString(encryptedFileContents, EncryptionKey);
            var xml = new XmlDocument();
            xml.LoadXml(fileContents);

            var pairs = xml.SelectNodes("*/*");
            if (pairs != null)
                foreach (XmlNode pair in pairs)
                {
                    var name = pair.SelectSingleNode("@name");
                    var value = pair.SelectSingleNode("@value");
                    if (name != null && value != null)
                        if (Settings.ContainsKey(name.Value))
                            Settings[name.Value] = value.Value;
                        else
                            Settings.Add(name.Value, value.Value);
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
            string fileName = GetConfigurationFileName();

            var xml = new XmlDocument();
            xml.LoadXml("<?xml version=\"1.0\"?><settings />");
            var root = xml.SelectSingleNode("*");

            if (root != null)
                foreach (var key in Settings.Keys)
                {
                    var newNode = xml.CreateElement("setting");

                    var nameAttribute = xml.CreateAttribute("name");
                    nameAttribute.Value = key.ToString();
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

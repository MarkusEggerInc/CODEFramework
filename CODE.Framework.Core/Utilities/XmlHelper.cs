using System;
using System.Text;
using System.Xml;

namespace CODE.Framework.Core.Utilities
{
    /// <summary>
    /// Provides useful XML helper functionality
    /// </summary>
    public static class XmlHelper
    {
        /// <summary>
        /// Returns a formatted version of the XML string (line breaks, indentations, ...)
        /// </summary>
        /// <param name="xml">The raw XML.</param>
        /// <returns>Formatted XML string</returns>
        public static string Format(string xml)
        {
            try
            {
                var xmlDocument = new XmlDocument();
                xmlDocument.LoadXml(xml);
                return Format(xmlDocument);
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Returns a formatted version of the XML string (line breaks, indentations, ...)
        /// </summary>
        /// <param name="xml">The original XML Document.</param>
        /// <returns>Formatted XML string</returns>
        public static string Format(XmlDocument xml)
        {
            try
            {
                var output = new StringBuilder();
                var xmlWriter = XmlWriter.Create(output, new XmlWriterSettings { Indent = true, NewLineHandling = NewLineHandling.Entitize });
                xml.WriteTo(xmlWriter);
                xmlWriter.Flush();
                return output.ToString();
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}

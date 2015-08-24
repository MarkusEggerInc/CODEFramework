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

using System.Numerics;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using CODE.Framework.Core.Newtonsoft.Utilities;
using System.Linq;

namespace CODE.Framework.Core.Newtonsoft.Converters
{
    internal class XmlDocumentWrapper : XmlNodeWrapper, IXmlDocument
    {
        private readonly XmlDocument _document;

        public XmlDocumentWrapper(XmlDocument document)
            : base(document)
        {
            _document = document;
        }

        public IXmlNode CreateComment(string data)
        {
            return new XmlNodeWrapper(_document.CreateComment(data));
        }

        public IXmlNode CreateTextNode(string text)
        {
            return new XmlNodeWrapper(_document.CreateTextNode(text));
        }

        public IXmlNode CreateCDataSection(string data)
        {
            return new XmlNodeWrapper(_document.CreateCDataSection(data));
        }

        public IXmlNode CreateWhitespace(string text)
        {
            return new XmlNodeWrapper(_document.CreateWhitespace(text));
        }

        public IXmlNode CreateSignificantWhitespace(string text)
        {
            return new XmlNodeWrapper(_document.CreateSignificantWhitespace(text));
        }

        public IXmlNode CreateXmlDeclaration(string version, string encoding, string standalone)
        {
            return new XmlDeclarationWrapper(_document.CreateXmlDeclaration(version, encoding, standalone));
        }

        public IXmlNode CreateXmlDocumentType(string name, string publicId, string systemId, string internalSubset)
        {
            return new XmlDocumentTypeWrapper(_document.CreateDocumentType(name, publicId, systemId, null));
        }

        public IXmlNode CreateProcessingInstruction(string target, string data)
        {
            return new XmlNodeWrapper(_document.CreateProcessingInstruction(target, data));
        }

        public IXmlElement CreateElement(string elementName)
        {
            return new XmlElementWrapper(_document.CreateElement(elementName));
        }

        public IXmlElement CreateElement(string qualifiedName, string namespaceUri)
        {
            return new XmlElementWrapper(_document.CreateElement(qualifiedName, namespaceUri));
        }

        public IXmlNode CreateAttribute(string name, string value)
        {
            return new XmlNodeWrapper(_document.CreateAttribute(name)) {Value = value};
        }

        public IXmlNode CreateAttribute(string qualifiedName, string namespaceUri, string value)
        {
            return new XmlNodeWrapper(_document.CreateAttribute(qualifiedName, namespaceUri)) {Value = value};
        }

        public IXmlElement DocumentElement
        {
            get
            {
                return _document.DocumentElement == null ? null : new XmlElementWrapper(_document.DocumentElement);
            }
        }
    }

    internal class XmlElementWrapper : XmlNodeWrapper, IXmlElement
    {
        private readonly XmlElement _element;

        public XmlElementWrapper(XmlElement element)
            : base(element)
        {
            _element = element;
        }

        public void SetAttributeNode(IXmlNode attribute)
        {
            var xmlAttributeWrapper = (XmlNodeWrapper) attribute;
            _element.SetAttributeNode((XmlAttribute) xmlAttributeWrapper.WrappedNode);
        }

        public string GetPrefixOfNamespace(string namespaceUri)
        {
            return _element.GetPrefixOfNamespace(namespaceUri);
        }

        public bool IsEmpty
        {
            get { return _element.IsEmpty; }
        }
    }

    internal class XmlDeclarationWrapper : XmlNodeWrapper, IXmlDeclaration
    {
        private readonly XmlDeclaration _declaration;

        public XmlDeclarationWrapper(XmlDeclaration declaration)
            : base(declaration)
        {
            _declaration = declaration;
        }

        public string Version
        {
            get { return _declaration.Version; }
        }

        public string Encoding
        {
            get { return _declaration.Encoding; }
            set { _declaration.Encoding = value; }
        }

        public string Standalone
        {
            get { return _declaration.Standalone; }
            set { _declaration.Standalone = value; }
        }
    }

    internal class XmlDocumentTypeWrapper : XmlNodeWrapper, IXmlDocumentType
    {
        private readonly XmlDocumentType _documentType;

        public XmlDocumentTypeWrapper(XmlDocumentType documentType)
            : base(documentType)
        {
            _documentType = documentType;
        }

        public string Name
        {
            get { return _documentType.Name; }
        }

        public string System
        {
            get { return _documentType.SystemId; }
        }

        public string Public
        {
            get { return _documentType.PublicId; }
        }

        public string InternalSubset
        {
            get { return _documentType.InternalSubset; }
        }

        public override string LocalName
        {
            get { return "DOCTYPE"; }
        }
    }

    internal class XmlNodeWrapper : IXmlNode
    {
        private readonly XmlNode _node;
        private IList<IXmlNode> _childNodes;

        public XmlNodeWrapper(XmlNode node)
        {
            _node = node;
        }

        public object WrappedNode
        {
            get { return _node; }
        }

        public XmlNodeType NodeType
        {
            get { return _node.NodeType; }
        }

        public virtual string LocalName
        {
            get { return _node.LocalName; }
        }

        public IList<IXmlNode> ChildNodes
        {
            get
            {
                // childnodes is read multiple times
                // cache results to prevent multiple reads which kills perf in large documents
                return _childNodes ?? (_childNodes = _node.ChildNodes.Cast<XmlNode>().Select(WrapNode).ToList());
            }
        }

        internal static IXmlNode WrapNode(XmlNode node)
        {
            switch (node.NodeType)
            {
                case XmlNodeType.Element:
                    return new XmlElementWrapper((XmlElement) node);
                case XmlNodeType.XmlDeclaration:
                    return new XmlDeclarationWrapper((XmlDeclaration) node);
                case XmlNodeType.DocumentType:
                    return new XmlDocumentTypeWrapper((XmlDocumentType) node);
                default:
                    return new XmlNodeWrapper(node);
            }
        }

        public IList<IXmlNode> Attributes
        {
            get
            {
                return _node.Attributes == null ? null : _node.Attributes.Cast<XmlAttribute>().Select(WrapNode).ToList();
            }
        }

        public IXmlNode ParentNode
        {
            get
            {
                var node = (_node is XmlAttribute) ? ((XmlAttribute) _node).OwnerElement : _node.ParentNode;
                return node == null ? null : WrapNode(node);
            }
        }

        public string Value
        {
            get { return _node.Value; }
            set { _node.Value = value; }
        }

        public IXmlNode AppendChild(IXmlNode newChild)
        {
            var xmlNodeWrapper = (XmlNodeWrapper) newChild;
            _node.AppendChild(xmlNodeWrapper._node);
            _childNodes = null;
            return newChild;
        }

        public string NamespaceUri
        {
            get { return _node.NamespaceURI; }
        }
    }

    internal interface IXmlDocument : IXmlNode
    {
        IXmlNode CreateComment(string text);
        IXmlNode CreateTextNode(string text);
        IXmlNode CreateCDataSection(string data);
        IXmlNode CreateWhitespace(string text);
        IXmlNode CreateSignificantWhitespace(string text);
        IXmlNode CreateXmlDeclaration(string version, string encoding, string standalone);
        IXmlNode CreateXmlDocumentType(string name, string publicId, string systemId, string internalSubset);
        IXmlNode CreateProcessingInstruction(string target, string data);
        IXmlElement CreateElement(string elementName);
        IXmlElement CreateElement(string qualifiedName, string namespaceUri);
        IXmlNode CreateAttribute(string name, string value);
        IXmlNode CreateAttribute(string qualifiedName, string namespaceUri, string value);

        IXmlElement DocumentElement { get; }
    }

    internal interface IXmlDeclaration : IXmlNode
    {
        string Version { get; }
        string Encoding { get; set; }
        string Standalone { get; set; }
    }

    internal interface IXmlDocumentType : IXmlNode
    {
        string Name { get; }
        string System { get; }
        string Public { get; }
        string InternalSubset { get; }
    }

    internal interface IXmlElement : IXmlNode
    {
        void SetAttributeNode(IXmlNode attribute);
        string GetPrefixOfNamespace(string namespaceUri);
        bool IsEmpty { get; }
    }

    internal interface IXmlNode
    {
        XmlNodeType NodeType { get; }
        string LocalName { get; }
        IList<IXmlNode> ChildNodes { get; }
        IList<IXmlNode> Attributes { get; }
        IXmlNode ParentNode { get; }
        string Value { get; set; }
        IXmlNode AppendChild(IXmlNode newChild);
        string NamespaceUri { get; }
        object WrappedNode { get; }
    }

    internal class XDeclarationWrapper : XObjectWrapper, IXmlDeclaration
    {
        internal XDeclaration Declaration { get; private set; }

        public XDeclarationWrapper(XDeclaration declaration)
            : base(null)
        {
            Declaration = declaration;
        }

        public override XmlNodeType NodeType
        {
            get { return XmlNodeType.XmlDeclaration; }
        }

        public string Version
        {
            get { return Declaration.Version; }
        }

        public string Encoding
        {
            get { return Declaration.Encoding; }
            set { Declaration.Encoding = value; }
        }

        public string Standalone
        {
            get { return Declaration.Standalone; }
            set { Declaration.Standalone = value; }
        }
    }

    internal class XDocumentTypeWrapper : XObjectWrapper, IXmlDocumentType
    {
        private readonly XDocumentType _documentType;

        public XDocumentTypeWrapper(XDocumentType documentType)
            : base(documentType)
        {
            _documentType = documentType;
        }

        public string Name
        {
            get { return _documentType.Name; }
        }

        public string System
        {
            get { return _documentType.SystemId; }
        }

        public string Public
        {
            get { return _documentType.PublicId; }
        }

        public string InternalSubset
        {
            get { return _documentType.InternalSubset; }
        }

        public override string LocalName
        {
            get { return "DOCTYPE"; }
        }
    }

    internal class XDocumentWrapper : XContainerWrapper, IXmlDocument
    {
        private XDocument Document
        {
            get { return (XDocument) WrappedNode; }
        }

        public XDocumentWrapper(XDocument document)
            : base(document)
        {
        }

        public override IList<IXmlNode> ChildNodes
        {
            get
            {
                var childNodes = base.ChildNodes;
                if (Document.Declaration != null && childNodes[0].NodeType != XmlNodeType.XmlDeclaration)
                    childNodes.Insert(0, new XDeclarationWrapper(Document.Declaration));
                return childNodes;
            }
        }

        public IXmlNode CreateComment(string text)
        {
            return new XObjectWrapper(new XComment(text));
        }

        public IXmlNode CreateTextNode(string text)
        {
            return new XObjectWrapper(new XText(text));
        }

        public IXmlNode CreateCDataSection(string data)
        {
            return new XObjectWrapper(new XCData(data));
        }

        public IXmlNode CreateWhitespace(string text)
        {
            return new XObjectWrapper(new XText(text));
        }

        public IXmlNode CreateSignificantWhitespace(string text)
        {
            return new XObjectWrapper(new XText(text));
        }

        public IXmlNode CreateXmlDeclaration(string version, string encoding, string standalone)
        {
            return new XDeclarationWrapper(new XDeclaration(version, encoding, standalone));
        }

        public IXmlNode CreateXmlDocumentType(string name, string publicId, string systemId, string internalSubset)
        {
            return new XDocumentTypeWrapper(new XDocumentType(name, publicId, systemId, internalSubset));
        }

        public IXmlNode CreateProcessingInstruction(string target, string data)
        {
            return new XProcessingInstructionWrapper(new XProcessingInstruction(target, data));
        }

        public IXmlElement CreateElement(string elementName)
        {
            return new XElementWrapper(new XElement(elementName));
        }

        public IXmlElement CreateElement(string qualifiedName, string namespaceUri)
        {
            string localName = MiscellaneousUtils.GetLocalName(qualifiedName);
            return new XElementWrapper(new XElement(XName.Get(localName, namespaceUri)));
        }

        public IXmlNode CreateAttribute(string name, string value)
        {
            return new XAttributeWrapper(new XAttribute(name, value));
        }

        public IXmlNode CreateAttribute(string qualifiedName, string namespaceUri, string value)
        {
            var localName = MiscellaneousUtils.GetLocalName(qualifiedName);
            return new XAttributeWrapper(new XAttribute(XName.Get(localName, namespaceUri), value));
        }

        public IXmlElement DocumentElement
        {
            get
            {
                return Document.Root == null ? null : new XElementWrapper(Document.Root);
            }
        }

        public override IXmlNode AppendChild(IXmlNode newChild)
        {
            var declarationWrapper = newChild as XDeclarationWrapper;
            if (declarationWrapper != null)
            {
                Document.Declaration = declarationWrapper.Declaration;
                return declarationWrapper;
            }
            return base.AppendChild(newChild);
        }
    }

    internal class XTextWrapper : XObjectWrapper
    {
        private XText Text
        {
            get { return (XText) WrappedNode; }
        }

        public XTextWrapper(XText text)
            : base(text)
        {
        }

        public override string Value
        {
            get { return Text.Value; }
            set { Text.Value = value; }
        }

        public override IXmlNode ParentNode
        {
            get
            {
                return Text.Parent == null ? null : XContainerWrapper.WrapNode(Text.Parent);
            }
        }
    }

    internal class XCommentWrapper : XObjectWrapper
    {
        private XComment Text
        {
            get { return (XComment) WrappedNode; }
        }

        public XCommentWrapper(XComment text)
            : base(text)
        {
        }

        public override string Value
        {
            get { return Text.Value; }
            set { Text.Value = value; }
        }

        public override IXmlNode ParentNode
        {
            get
            {
                return Text.Parent == null ? null : XContainerWrapper.WrapNode(Text.Parent);
            }
        }
    }

    internal class XProcessingInstructionWrapper : XObjectWrapper
    {
        private XProcessingInstruction ProcessingInstruction
        {
            get { return (XProcessingInstruction) WrappedNode; }
        }

        public XProcessingInstructionWrapper(XProcessingInstruction processingInstruction)
            : base(processingInstruction)
        {
        }

        public override string LocalName
        {
            get { return ProcessingInstruction.Target; }
        }

        public override string Value
        {
            get { return ProcessingInstruction.Data; }
            set { ProcessingInstruction.Data = value; }
        }
    }

    internal class XContainerWrapper : XObjectWrapper
    {
        private IList<IXmlNode> _childNodes;

        private XContainer Container
        {
            get { return (XContainer) WrappedNode; }
        }

        public XContainerWrapper(XContainer container)
            : base(container)
        {
        }

        public override IList<IXmlNode> ChildNodes
        {
            get
            {
                // childnodes is read multiple times
                // cache results to prevent multiple reads which kills perf in large documents
                return _childNodes ?? (_childNodes = Container.Nodes().Select(WrapNode).ToList());
            }
        }

        public override IXmlNode ParentNode
        {
            get
            {
                return Container.Parent == null ? null : WrapNode(Container.Parent);
            }
        }

        internal static IXmlNode WrapNode(XObject node)
        {
            var document = node as XDocument;
            if (document != null) return new XDocumentWrapper(document);
            var element = node as XElement;
            if (element != null) return new XElementWrapper(element);
            var container = node as XContainer;
            if (container != null) return new XContainerWrapper(container);
            var processingInstruction = node as XProcessingInstruction;
            if (processingInstruction != null) return new XProcessingInstructionWrapper(processingInstruction);
            var text = node as XText;
            if (text != null) return new XTextWrapper(text);
            var comment = node as XComment;
            if (comment != null) return new XCommentWrapper(comment);
            var attribute = node as XAttribute;
            if (attribute != null) return new XAttributeWrapper(attribute);
            var type = node as XDocumentType;
            return type != null ? new XDocumentTypeWrapper(type) : new XObjectWrapper(node);
        }

        public override IXmlNode AppendChild(IXmlNode newChild)
        {
            Container.Add(newChild.WrappedNode);
            _childNodes = null;
            return newChild;
        }
    }

    internal class XObjectWrapper : IXmlNode
    {
        private readonly XObject _xmlObject;

        public XObjectWrapper(XObject xmlObject)
        {
            _xmlObject = xmlObject;
        }

        public object WrappedNode
        {
            get { return _xmlObject; }
        }

        public virtual XmlNodeType NodeType
        {
            get { return _xmlObject.NodeType; }
        }

        public virtual string LocalName
        {
            get { return null; }
        }

        public virtual IList<IXmlNode> ChildNodes
        {
            get { return new List<IXmlNode>(); }
        }

        public virtual IList<IXmlNode> Attributes
        {
            get { return null; }
        }

        public virtual IXmlNode ParentNode
        {
            get { return null; }
        }

        public virtual string Value
        {
            get { return null; }
            set { throw new InvalidOperationException(); }
        }

        public virtual IXmlNode AppendChild(IXmlNode newChild)
        {
            throw new InvalidOperationException();
        }

        public virtual string NamespaceUri
        {
            get { return null; }
        }
    }

    internal class XAttributeWrapper : XObjectWrapper
    {
        private XAttribute Attribute
        {
            get { return (XAttribute) WrappedNode; }
        }

        public XAttributeWrapper(XAttribute attribute)
            : base(attribute)
        {
        }

        public override string Value
        {
            get { return Attribute.Value; }
            set { Attribute.Value = value; }
        }

        public override string LocalName
        {
            get { return Attribute.Name.LocalName; }
        }

        public override string NamespaceUri
        {
            get { return Attribute.Name.NamespaceName; }
        }

        public override IXmlNode ParentNode
        {
            get
            {
                return Attribute.Parent == null ? null : XContainerWrapper.WrapNode(Attribute.Parent);
            }
        }
    }

    internal class XElementWrapper : XContainerWrapper, IXmlElement
    {
        private XElement Element
        {
            get { return (XElement) WrappedNode; }
        }

        public XElementWrapper(XElement element)
            : base(element)
        {
        }

        public void SetAttributeNode(IXmlNode attribute)
        {
            var wrapper = (XObjectWrapper) attribute;
            Element.Add(wrapper.WrappedNode);
        }

        public override IList<IXmlNode> Attributes
        {
            get { return Element.Attributes().Select(a => new XAttributeWrapper(a)).Cast<IXmlNode>().ToList(); }
        }

        public override string Value
        {
            get { return Element.Value; }
            set { Element.Value = value; }
        }

        public override string LocalName
        {
            get { return Element.Name.LocalName; }
        }

        public override string NamespaceUri
        {
            get { return Element.Name.NamespaceName; }
        }

        public string GetPrefixOfNamespace(string namespaceUri)
        {
            return Element.GetPrefixOfNamespace(namespaceUri);
        }

        public bool IsEmpty
        {
            get { return Element.IsEmpty; }
        }
    }

    /// <summary>
    /// Converts XML to and from JSON.
    /// </summary>
    public class XmlNodeConverter : JsonConverter
    {
        private const string TextName = "#text";
        private const string CommentName = "#comment";
        private const string CDataName = "#cdata-section";
        private const string WhitespaceName = "#whitespace";
        private const string SignificantWhitespaceName = "#significant-whitespace";
        private const string DeclarationName = "?xml";
        private const string JsonNamespaceUri = "http://james.newtonking.com/projects/json";

        /// <summary>
        /// Gets or sets the name of the root element to insert when deserializing to XML if the JSON structure has produces multiple root elements.
        /// </summary>
        /// <value>The name of the deserialize root element.</value>
        public string DeserializeRootElementName { get; set; }

        /// <summary>
        /// Gets or sets a flag to indicate whether to write the Json.NET array attribute.
        /// This attribute helps preserve arrays when converting the written XML back to JSON.
        /// </summary>
        /// <value><c>true</c> if the array attibute is written to the XML; otherwise, <c>false</c>.</value>
        public bool WriteArrayAttribute { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to write the root JSON object.
        /// </summary>
        /// <value><c>true</c> if the JSON root object is omitted; otherwise, <c>false</c>.</value>
        public bool OmitRootObject { get; set; }

        /// <summary>
        /// Writes the JSON representation of the object.
        /// </summary>
        /// <param name="writer">The <see cref="JsonWriter"/> to write to.</param>
        /// <param name="serializer">The calling serializer.</param>
        /// <param name="value">The value.</param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var node = WrapXml(value);

            var manager = new XmlNamespaceManager(new NameTable());
            PushParentNamespaces(node, manager);

            if (!OmitRootObject)
                writer.WriteStartObject();

            SerializeNode(writer, node, manager, !OmitRootObject);

            if (!OmitRootObject)
                writer.WriteEndObject();
        }

        private IXmlNode WrapXml(object value)
        {
            var xObject = value as XObject;
            if (xObject != null) return XContainerWrapper.WrapNode(xObject);
            var node = value as XmlNode;
            if (node != null) return XmlNodeWrapper.WrapNode(node);
            throw new ArgumentException("Value must be an XML object.", "value");
        }

        private void PushParentNamespaces(IXmlNode node, XmlNamespaceManager manager)
        {
            List<IXmlNode> parentElements = null;

            var parent = node;
            while ((parent = parent.ParentNode) != null)
            {
                if (parent.NodeType != XmlNodeType.Element) continue;
                if (parentElements == null)
                    parentElements = new List<IXmlNode>();
                parentElements.Add(parent);
            }

            if (parentElements == null) return;
            parentElements.Reverse();

            foreach (var parentElement in parentElements)
            {
                manager.PushScope();
                foreach (var attribute in parentElement.Attributes)
                    if (attribute.NamespaceUri == "http://www.w3.org/2000/xmlns/" && attribute.LocalName != "xmlns")
                        manager.AddNamespace(attribute.LocalName, attribute.Value);
            }
        }

        private string ResolveFullName(IXmlNode node, XmlNamespaceManager manager)
        {
            var prefix = (node.NamespaceUri == null || (node.LocalName == "xmlns" && node.NamespaceUri == "http://www.w3.org/2000/xmlns/")) ? null : manager.LookupPrefix(node.NamespaceUri);

            if (!string.IsNullOrEmpty(prefix))
                return prefix + ":" + node.LocalName;
            return node.LocalName;
        }

        private string GetPropertyName(IXmlNode node, XmlNamespaceManager manager)
        {
            switch (node.NodeType)
            {
                case XmlNodeType.Attribute:
                    if (node.NamespaceUri == JsonNamespaceUri)
                        return "$" + node.LocalName;
                    return "@" + ResolveFullName(node, manager);
                case XmlNodeType.CDATA:
                    return CDataName;
                case XmlNodeType.Comment:
                    return CommentName;
                case XmlNodeType.Element:
                    return ResolveFullName(node, manager);
                case XmlNodeType.ProcessingInstruction:
                    return "?" + ResolveFullName(node, manager);
                case XmlNodeType.DocumentType:
                    return "!" + ResolveFullName(node, manager);
                case XmlNodeType.XmlDeclaration:
                    return DeclarationName;
                case XmlNodeType.SignificantWhitespace:
                    return SignificantWhitespaceName;
                case XmlNodeType.Text:
                    return TextName;
                case XmlNodeType.Whitespace:
                    return WhitespaceName;
                default:
                    throw new JsonSerializationException("Unexpected XmlNodeType when getting node name: " + node.NodeType);
            }
        }

        private bool IsArray(IXmlNode node)
        {
            var jsonArrayAttribute = (node.Attributes != null) ? node.Attributes.SingleOrDefault(a => a.LocalName == "Array" && a.NamespaceUri == JsonNamespaceUri) : null;
            return (jsonArrayAttribute != null && XmlConvert.ToBoolean(jsonArrayAttribute.Value));
        }

        private void SerializeGroupedNodes(JsonWriter writer, IXmlNode node, XmlNamespaceManager manager, bool writePropertyName)
        {
            // group nodes together by name
            var nodesGroupedByName = new Dictionary<string, List<IXmlNode>>();

            for (var i = 0; i < node.ChildNodes.Count; i++)
            {
                var childNode = node.ChildNodes[i];
                var nodeName = GetPropertyName(childNode, manager);

                List<IXmlNode> nodes;
                if (!nodesGroupedByName.TryGetValue(nodeName, out nodes))
                {
                    nodes = new List<IXmlNode>();
                    nodesGroupedByName.Add(nodeName, nodes);
                }

                nodes.Add(childNode);
            }

            // loop through grouped nodes. write single name instances as normal,
            // write multiple names together in an array
            foreach (var nodeNameGroup in nodesGroupedByName)
            {
                var groupedNodes = nodeNameGroup.Value;
                bool writeArray;

                writeArray = groupedNodes.Count != 1 || IsArray(groupedNodes[0]);

                if (!writeArray)
                    SerializeNode(writer, groupedNodes[0], manager, writePropertyName);
                else
                {
                    var elementNames = nodeNameGroup.Key;
                    if (writePropertyName)
                        writer.WritePropertyName(elementNames);
                    writer.WriteStartArray();
                    for (var i = 0; i < groupedNodes.Count; i++)
                        SerializeNode(writer, groupedNodes[i], manager, false);
                    writer.WriteEndArray();
                }
            }
        }

        private void SerializeNode(JsonWriter writer, IXmlNode node, XmlNamespaceManager manager, bool writePropertyName)
        {
            switch (node.NodeType)
            {
                case XmlNodeType.Document:
                case XmlNodeType.DocumentFragment:
                    SerializeGroupedNodes(writer, node, manager, writePropertyName);
                    break;
                case XmlNodeType.Element:
                    if (IsArray(node) && node.ChildNodes.All(n => n.LocalName == node.LocalName) && node.ChildNodes.Count > 0)
                        SerializeGroupedNodes(writer, node, manager, false);
                    else
                    {
                        manager.PushScope();

                        foreach (var attribute in node.Attributes)
                        {
                            if (attribute.NamespaceUri != "http://www.w3.org/2000/xmlns/") continue;
                            var namespacePrefix = (attribute.LocalName != "xmlns") ? attribute.LocalName : string.Empty;
                            var namespaceUri = attribute.Value;
                            manager.AddNamespace(namespacePrefix, namespaceUri);
                        }

                        if (writePropertyName)
                            writer.WritePropertyName(GetPropertyName(node, manager));

                        if (!ValueAttributes(node.Attributes).Any() && node.ChildNodes.Count == 1 && node.ChildNodes[0].NodeType == XmlNodeType.Text)
                            // write elements with a single text child as a name value pair
                            writer.WriteValue(node.ChildNodes[0].Value);
                        else if (node.ChildNodes.Count == 0 && CollectionUtils.IsNullOrEmpty(node.Attributes))
                        {
                            var element = (IXmlElement) node;

                            // empty element
                            if (element.IsEmpty)
                                writer.WriteNull();
                            else
                                writer.WriteValue(string.Empty);
                        }
                        else
                        {
                            writer.WriteStartObject();

                            for (var i = 0; i < node.Attributes.Count; i++)
                                SerializeNode(writer, node.Attributes[i], manager, true);

                            SerializeGroupedNodes(writer, node, manager, true);

                            writer.WriteEndObject();
                        }

                        manager.PopScope();
                    }

                    break;
                case XmlNodeType.Comment:
                    if (writePropertyName)
                        writer.WriteComment(node.Value);
                    break;
                case XmlNodeType.Attribute:
                case XmlNodeType.Text:
                case XmlNodeType.CDATA:
                case XmlNodeType.ProcessingInstruction:
                case XmlNodeType.Whitespace:
                case XmlNodeType.SignificantWhitespace:
                    if (node.NamespaceUri == "http://www.w3.org/2000/xmlns/" && node.Value == JsonNamespaceUri)
                        return;

                    if (node.NamespaceUri == JsonNamespaceUri)
                        if (node.LocalName == "Array")
                            return;

                    if (writePropertyName)
                        writer.WritePropertyName(GetPropertyName(node, manager));
                    writer.WriteValue(node.Value);
                    break;
                case XmlNodeType.XmlDeclaration:
                    var declaration = (IXmlDeclaration) node;
                    writer.WritePropertyName(GetPropertyName(node, manager));
                    writer.WriteStartObject();

                    if (!string.IsNullOrEmpty(declaration.Version))
                    {
                        writer.WritePropertyName("@version");
                        writer.WriteValue(declaration.Version);
                    }
                    if (!string.IsNullOrEmpty(declaration.Encoding))
                    {
                        writer.WritePropertyName("@encoding");
                        writer.WriteValue(declaration.Encoding);
                    }
                    if (!string.IsNullOrEmpty(declaration.Standalone))
                    {
                        writer.WritePropertyName("@standalone");
                        writer.WriteValue(declaration.Standalone);
                    }

                    writer.WriteEndObject();
                    break;
                case XmlNodeType.DocumentType:
                    var documentType = (IXmlDocumentType) node;
                    writer.WritePropertyName(GetPropertyName(node, manager));
                    writer.WriteStartObject();

                    if (!string.IsNullOrEmpty(documentType.Name))
                    {
                        writer.WritePropertyName("@name");
                        writer.WriteValue(documentType.Name);
                    }
                    if (!string.IsNullOrEmpty(documentType.Public))
                    {
                        writer.WritePropertyName("@public");
                        writer.WriteValue(documentType.Public);
                    }
                    if (!string.IsNullOrEmpty(documentType.System))
                    {
                        writer.WritePropertyName("@system");
                        writer.WriteValue(documentType.System);
                    }
                    if (!string.IsNullOrEmpty(documentType.InternalSubset))
                    {
                        writer.WritePropertyName("@internalSubset");
                        writer.WriteValue(documentType.InternalSubset);
                    }

                    writer.WriteEndObject();
                    break;
                default:
                    throw new JsonSerializationException("Unexpected XmlNodeType when serializing nodes: " + node.NodeType);
            }
        }

        /// <summary>
        /// Reads the JSON representation of the object.
        /// </summary>
        /// <param name="reader">The <see cref="JsonReader"/> to read from.</param>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="existingValue">The existing value of object being read.</param>
        /// <param name="serializer">The calling serializer.</param>
        /// <returns>The object value.</returns>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;

            var manager = new XmlNamespaceManager(new NameTable());
            IXmlDocument document = null;
            IXmlNode rootNode = null;

            if (typeof (XObject).IsAssignableFrom(objectType))
            {
                if (objectType != typeof (XDocument) && objectType != typeof (XElement)) throw new JsonSerializationException("XmlNodeConverter only supports deserializing XDocument or XElement.");
                var d = new XDocument();
                document = new XDocumentWrapper(d);
                rootNode = document;
            }

            if (typeof (XmlNode).IsAssignableFrom(objectType))
            {
                if (objectType != typeof (XmlDocument)) throw new JsonSerializationException("XmlNodeConverter only supports deserializing XmlDocuments");
                var d = new XmlDocument {XmlResolver = null};
                // prevent http request when resolving any DTD references
                document = new XmlDocumentWrapper(d);
                rootNode = document;
            }

            if (document == null || rootNode == null) throw new JsonSerializationException("Unexpected type when converting XML: " + objectType);
            if (reader.TokenType != JsonToken.StartObject) throw new JsonSerializationException("XmlNodeConverter can only convert JSON that begins with an object.");

            if (!string.IsNullOrEmpty(DeserializeRootElementName))
                ReadElement(reader, document, rootNode, DeserializeRootElementName, manager);
            else
            {
                reader.Read();
                DeserializeNode(reader, document, manager, rootNode);
            }

            if (objectType != typeof (XElement)) return document.WrappedNode;
            var element = (XElement) document.DocumentElement.WrappedNode;
            element.Remove();
            return element;
        }

        private void DeserializeValue(JsonReader reader, IXmlDocument document, XmlNamespaceManager manager, string propertyName, IXmlNode currentNode)
        {
            switch (propertyName)
            {
                case TextName:
                    currentNode.AppendChild(document.CreateTextNode(reader.Value.ToString()));
                    break;
                case CDataName:
                    currentNode.AppendChild(document.CreateCDataSection(reader.Value.ToString()));
                    break;
                case WhitespaceName:
                    currentNode.AppendChild(document.CreateWhitespace(reader.Value.ToString()));
                    break;
                case SignificantWhitespaceName:
                    currentNode.AppendChild(document.CreateSignificantWhitespace(reader.Value.ToString()));
                    break;
                default:
                    // processing instructions and the xml declaration start with ?
                    if (!string.IsNullOrEmpty(propertyName) && propertyName[0] == '?')
                        CreateInstruction(reader, document, currentNode, propertyName);
                    else if (string.Equals(propertyName, "!DOCTYPE", StringComparison.OrdinalIgnoreCase))
                        CreateDocumentType(reader, document, currentNode);
                    else
                    {
                        if (reader.TokenType == JsonToken.StartArray)
                        {
                            // handle nested arrays
                            ReadArrayElements(reader, document, propertyName, currentNode, manager);
                            return;
                        }

                        // have to wait until attributes have been parsed before creating element
                        // attributes may contain namespace info used by the element
                        ReadElement(reader, document, currentNode, propertyName, manager);
                    }
                    break;
            }
        }

        private void ReadElement(JsonReader reader, IXmlDocument document, IXmlNode currentNode, string propertyName, XmlNamespaceManager manager)
        {
            if (string.IsNullOrEmpty(propertyName)) throw new JsonSerializationException("XmlNodeConverter cannot convert JSON with an empty property name to XML.");

            var attributeNameValues = ReadAttributeElements(reader, manager);
            var elementPrefix = MiscellaneousUtils.GetPrefix(propertyName);

            if (propertyName.StartsWith('@'))
            {
                var attributeName = propertyName.Substring(1);
                var attributeValue = reader.Value.ToString();
                var attributePrefix = MiscellaneousUtils.GetPrefix(attributeName);
                var attribute = (!string.IsNullOrEmpty(attributePrefix)) ? document.CreateAttribute(attributeName, manager.LookupNamespace(attributePrefix), attributeValue) : document.CreateAttribute(attributeName, attributeValue);
                ((IXmlElement) currentNode).SetAttributeNode(attribute);
            }
            else
            {
                var element = CreateElement(propertyName, document, elementPrefix, manager);

                currentNode.AppendChild(element);

                // add attributes to newly created element
                foreach (var nameValue in attributeNameValues)
                {
                    var attributePrefix = MiscellaneousUtils.GetPrefix(nameValue.Key);
                    var attribute = (!string.IsNullOrEmpty(attributePrefix)) ? document.CreateAttribute(nameValue.Key, manager.LookupNamespace(attributePrefix) ?? string.Empty, nameValue.Value) : document.CreateAttribute(nameValue.Key, nameValue.Value);
                    element.SetAttributeNode(attribute);
                }

                switch (reader.TokenType)
                {
                    case JsonToken.Date:
                    case JsonToken.Boolean:
                    case JsonToken.Float:
                    case JsonToken.Integer:
                    case JsonToken.String:
                        element.AppendChild(document.CreateTextNode(ConvertTokenToXmlValue(reader)));
                        break;
                    case JsonToken.Null:
                        break;
                    default:
                        if (reader.TokenType != JsonToken.EndObject)
                        {
                            manager.PushScope();
                            DeserializeNode(reader, document, manager, element);
                            manager.PopScope();
                        }
                        manager.RemoveNamespace(string.Empty, manager.DefaultNamespace);
                        break;
                }
            }
        }

        private string ConvertTokenToXmlValue(JsonReader reader)
        {
            switch (reader.TokenType)
            {
                case JsonToken.String:
                    return reader.Value.ToString();
                case JsonToken.Integer:
                    if (reader.Value is BigInteger) return ((BigInteger) reader.Value).ToString(CultureInfo.InvariantCulture);
                    return XmlConvert.ToString(Convert.ToInt64(reader.Value, CultureInfo.InvariantCulture));
                case JsonToken.Float:
                    if (reader.Value is decimal) return XmlConvert.ToString((decimal) reader.Value);
                    if (reader.Value is float) return XmlConvert.ToString((float) reader.Value);
                    return XmlConvert.ToString(Convert.ToDouble(reader.Value, CultureInfo.InvariantCulture));
                case JsonToken.Boolean:
                    return XmlConvert.ToString(Convert.ToBoolean(reader.Value, CultureInfo.InvariantCulture));
                case JsonToken.Date:
                {
                    if (reader.Value is DateTimeOffset) return XmlConvert.ToString((DateTimeOffset) reader.Value);
                    var d = Convert.ToDateTime(reader.Value, CultureInfo.InvariantCulture);
                    return XmlConvert.ToString(d, DateTimeUtils.ToSerializationMode(d.Kind));
                }
                case JsonToken.Null:
                    return null;
                default:
                    throw JsonSerializationException.Create(reader, "Cannot get an XML string value from token type '{0}'.".FormatWith(CultureInfo.InvariantCulture, reader.TokenType));
            }
        }

        private void ReadArrayElements(JsonReader reader, IXmlDocument document, string propertyName, IXmlNode currentNode, XmlNamespaceManager manager)
        {
            var elementPrefix = MiscellaneousUtils.GetPrefix(propertyName);
            var nestedArrayElement = CreateElement(propertyName, document, elementPrefix, manager);

            currentNode.AppendChild(nestedArrayElement);

            var count = 0;
            while (reader.Read() && reader.TokenType != JsonToken.EndArray)
            {
                DeserializeValue(reader, document, manager, propertyName, nestedArrayElement);
                count++;
            }

            if (WriteArrayAttribute)
                AddJsonArrayAttribute(nestedArrayElement, document);

            if (count != 1 || !WriteArrayAttribute) return;
            var arrayElement = nestedArrayElement.ChildNodes.OfType<IXmlElement>().Single(n => n.LocalName == propertyName);
            AddJsonArrayAttribute(arrayElement, document);
        }

        private void AddJsonArrayAttribute(IXmlElement element, IXmlDocument document)
        {
            element.SetAttributeNode(document.CreateAttribute("json:Array", JsonNamespaceUri, "true"));

            // linq to xml doesn't automatically include prefixes via the namespace manager
            if (!(element is XElementWrapper)) return;
            if (element.GetPrefixOfNamespace(JsonNamespaceUri) == null)
                element.SetAttributeNode(document.CreateAttribute("xmlns:json", "http://www.w3.org/2000/xmlns/", JsonNamespaceUri));
        }

        private Dictionary<string, string> ReadAttributeElements(JsonReader reader, XmlNamespaceManager manager)
        {
            var attributeNameValues = new Dictionary<string, string>();
            var finishedAttributes = false;
            var finishedElement = false;

            // a string token means the element only has a single text child
            if (reader.TokenType == JsonToken.String || reader.TokenType == JsonToken.Null || reader.TokenType == JsonToken.Boolean || reader.TokenType == JsonToken.Integer || reader.TokenType == JsonToken.Float || reader.TokenType == JsonToken.Date || reader.TokenType == JsonToken.StartConstructor) return attributeNameValues;
            // read properties until first non-attribute is encountered
            while (!finishedAttributes && !finishedElement && reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonToken.PropertyName:
                        var attributeName = reader.Value.ToString();

                        if (!string.IsNullOrEmpty(attributeName))
                        {
                            var firstChar = attributeName[0];
                            string attributeValue;

                            switch (firstChar)
                            {
                                case '@':
                                    attributeName = attributeName.Substring(1);
                                    reader.Read();
                                    attributeValue = ConvertTokenToXmlValue(reader);
                                    attributeNameValues.Add(attributeName, attributeValue);

                                    string namespacePrefix;
                                    if (IsNamespaceAttribute(attributeName, out namespacePrefix))
                                        manager.AddNamespace(namespacePrefix, attributeValue);
                                    break;
                                case '$':
                                    attributeName = attributeName.Substring(1);
                                    reader.Read();
                                    attributeValue = reader.Value.ToString();

                                    // check that JsonNamespaceUri is in scope
                                    // if it isn't then add it to document and namespace manager
                                    var jsonPrefix = manager.LookupPrefix(JsonNamespaceUri);
                                    if (jsonPrefix == null)
                                    {
                                        // ensure that the prefix used is free
                                        int? i = null;
                                        while (manager.LookupNamespace("json" + i) != null)
                                            i = i.GetValueOrDefault() + 1;
                                        jsonPrefix = "json" + i;

                                        attributeNameValues.Add("xmlns:" + jsonPrefix, JsonNamespaceUri);
                                        manager.AddNamespace(jsonPrefix, JsonNamespaceUri);
                                    }

                                    attributeNameValues.Add(jsonPrefix + ":" + attributeName, attributeValue);
                                    break;
                                default:
                                    finishedAttributes = true;
                                    break;
                            }
                        }
                        else
                            finishedAttributes = true;

                        break;
                    case JsonToken.EndObject:
                        finishedElement = true;
                        break;
                    case JsonToken.Comment:
                        finishedElement = true;
                        break;
                    default:
                        throw new JsonSerializationException("Unexpected JsonToken: " + reader.TokenType);
                }
            }

            return attributeNameValues;
        }

        private void CreateInstruction(JsonReader reader, IXmlDocument document, IXmlNode currentNode, string propertyName)
        {
            if (propertyName == DeclarationName)
            {
                string version = null;
                string encoding = null;
                string standalone = null;
                while (reader.Read() && reader.TokenType != JsonToken.EndObject)
                {
                    switch (reader.Value.ToString())
                    {
                        case "@version":
                            reader.Read();
                            version = reader.Value.ToString();
                            break;
                        case "@encoding":
                            reader.Read();
                            encoding = reader.Value.ToString();
                            break;
                        case "@standalone":
                            reader.Read();
                            standalone = reader.Value.ToString();
                            break;
                        default:
                            throw new JsonSerializationException("Unexpected property name encountered while deserializing XmlDeclaration: " + reader.Value);
                    }
                }

                var declaration = document.CreateXmlDeclaration(version, encoding, standalone);
                currentNode.AppendChild(declaration);
            }
            else
            {
                var instruction = document.CreateProcessingInstruction(propertyName.Substring(1), reader.Value.ToString());
                currentNode.AppendChild(instruction);
            }
        }

        private void CreateDocumentType(JsonReader reader, IXmlDocument document, IXmlNode currentNode)
        {
            string name = null;
            string publicId = null;
            string systemId = null;
            string internalSubset = null;
            while (reader.Read() && reader.TokenType != JsonToken.EndObject)
            {
                switch (reader.Value.ToString())
                {
                    case "@name":
                        reader.Read();
                        name = reader.Value.ToString();
                        break;
                    case "@public":
                        reader.Read();
                        publicId = reader.Value.ToString();
                        break;
                    case "@system":
                        reader.Read();
                        systemId = reader.Value.ToString();
                        break;
                    case "@internalSubset":
                        reader.Read();
                        internalSubset = reader.Value.ToString();
                        break;
                    default:
                        throw new JsonSerializationException("Unexpected property name encountered while deserializing XmlDeclaration: " + reader.Value);
                }
            }

            var documentType = document.CreateXmlDocumentType(name, publicId, systemId, internalSubset);
            currentNode.AppendChild(documentType);
        }

        private IXmlElement CreateElement(string elementName, IXmlDocument document, string elementPrefix, XmlNamespaceManager manager)
        {
            var ns = string.IsNullOrEmpty(elementPrefix) ? manager.DefaultNamespace : manager.LookupNamespace(elementPrefix);
            var element = (!string.IsNullOrEmpty(ns)) ? document.CreateElement(elementName, ns) : document.CreateElement(elementName);
            return element;
        }

        private void DeserializeNode(JsonReader reader, IXmlDocument document, XmlNamespaceManager manager, IXmlNode currentNode)
        {
            do
            {
                switch (reader.TokenType)
                {
                    case JsonToken.PropertyName:
                        if (currentNode.NodeType == XmlNodeType.Document && document.DocumentElement != null) throw new JsonSerializationException("JSON root object has multiple properties. The root object must have a single property in order to create a valid XML document. Consider specifing a DeserializeRootElementName.");
                        var propertyName = reader.Value.ToString();
                        reader.Read();

                        if (reader.TokenType == JsonToken.StartArray)
                        {
                            var count = 0;
                            while (reader.Read() && reader.TokenType != JsonToken.EndArray)
                            {
                                DeserializeValue(reader, document, manager, propertyName, currentNode);
                                count++;
                            }

                            if (count != 1 || !WriteArrayAttribute) continue;
                            var arrayElement = currentNode.ChildNodes.OfType<IXmlElement>().Single(n => n.LocalName == propertyName);
                            AddJsonArrayAttribute(arrayElement, document);
                        }
                        else
                            DeserializeValue(reader, document, manager, propertyName, currentNode);
                        break;
                    case JsonToken.StartConstructor:
                        var constructorName = reader.Value.ToString();
                        while (reader.Read() && reader.TokenType != JsonToken.EndConstructor)
                            DeserializeValue(reader, document, manager, constructorName, currentNode);
                        break;
                    case JsonToken.Comment:
                        currentNode.AppendChild(document.CreateComment((string) reader.Value));
                        break;
                    case JsonToken.EndObject:
                    case JsonToken.EndArray:
                        return;
                    default:
                        throw new JsonSerializationException("Unexpected JsonToken when deserializing node: " + reader.TokenType);
                }
            } while (reader.TokenType == JsonToken.PropertyName || reader.Read());
            // don't read if current token is a property. token was already read when parsing element attributes
        }

        /// <summary>
        /// Checks if the attributeName is a namespace attribute.
        /// </summary>
        /// <param name="attributeName">Attribute name to test.</param>
        /// <param name="prefix">The attribute name prefix if it has one, otherwise an empty string.</param>
        /// <returns>True if attribute name is for a namespace attribute, otherwise false.</returns>
        private bool IsNamespaceAttribute(string attributeName, out string prefix)
        {
            if (attributeName.StartsWith("xmlns", StringComparison.Ordinal))
            {
                if (attributeName.Length == 5)
                {
                    prefix = string.Empty;
                    return true;
                }
                if (attributeName[5] == ':')
                {
                    prefix = attributeName.Substring(6, attributeName.Length - 6);
                    return true;
                }
            }
            prefix = null;
            return false;
        }

        private IEnumerable<IXmlNode> ValueAttributes(IEnumerable<IXmlNode> c)
        {
            return c.Where(a => a.NamespaceUri != JsonNamespaceUri);
        }

        /// <summary>
        /// Determines whether this instance can convert the specified value type.
        /// </summary>
        /// <param name="valueType">Type of the value.</param>
        /// <returns>
        /// 	<c>true</c> if this instance can convert the specified value type; otherwise, <c>false</c>.
        /// </returns>
        public override bool CanConvert(Type valueType)
        {
            return typeof (XObject).IsAssignableFrom(valueType) || typeof (XmlNode).IsAssignableFrom(valueType);
        }
    }
}
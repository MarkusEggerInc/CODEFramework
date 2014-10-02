using System.Collections.Generic;
using System.Windows;
using System.Windows.Markup;

namespace CODE.Framework.Wpf.Documents
{
    /// <summary>
    /// Document Fragment
    /// </summary>
    [ContentProperty("Items")]
    public class DocumentMultiFragment : FrameworkElement, IAddChild
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentMultiFragment" /> class.
        /// </summary>
        public DocumentMultiFragment()
        {
            Items = new List<FrameworkContentElement>();
        }

        /// <summary>Represents a generic fragment of multiple content items within a print document</summary>
        public List<FrameworkContentElement> Items { get; set; }

        /// <summary>
        /// Adds a child object.
        /// </summary>
        /// <param name="value">The child object to add.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public void AddChild(object value)
        {
            var element = value as FrameworkContentElement;
            if (element == null) return;
            Items.Add(element);
        }

        /// <summary>
        /// Adds the text content of a node to the object.
        /// </summary>
        /// <param name="text">The text to add to the object.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public void AddText(string text)
        {
            throw new System.NotImplementedException();
        }
    }
}

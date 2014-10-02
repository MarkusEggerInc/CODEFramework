using System.Windows;
using System.Windows.Markup;

namespace CODE.Framework.Wpf.Documents
{
    /// <summary>
    /// Document Fragment
    /// </summary>
    [ContentProperty("Content")]
    public class DocumentFragment : FrameworkElement
    {
        /// <summary>Represents a generic fragment of content within a print document</summary>
        public static readonly DependencyProperty ContentProperty = DependencyProperty.Register("Content", typeof(FrameworkContentElement), typeof(DocumentFragment));

        /// <summary>The content fragment</summary>
        /// <value>The content.</value>
        public FrameworkContentElement Content
        {
            get { return (FrameworkContentElement)GetValue(ContentProperty); }
            set { SetValue(ContentProperty, value); }
        }
    }
}

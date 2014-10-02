using System.Windows;
using System.Windows.Documents;
using CODE.Framework.Wpf.Interfaces;

namespace CODE.Framework.Wpf.Documents
{
    /// <summary>
    /// Extended version of the WPF FlowDocument class
    /// </summary>
    public class FlowDocumentEx : FlowDocument, ISourceInformation, ITitle
    {
        /// <summary>
        /// Location this document was originally loaded from
        /// </summary>
        /// <value>The original document load location.</value>
        public string OriginalLoadLocation { get; set; }

        /// <summary>
        /// Document title
        /// </summary>
        /// <value>The title.</value>
        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }
        /// <summary>
        /// Document title
        /// </summary>
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register("Title", typeof(string), typeof(FlowDocumentEx), new PropertyMetadata(""));

        /// <summary>
        /// Page header for printed documents
        /// </summary>
        public FrameworkElement PageHeader
        {
            get { return (FrameworkElement)GetValue(PageHeaderProperty); }
            set { SetValue(PageHeaderProperty, value); }
        }
        /// <summary>
        /// Page header for printed documents
        /// </summary>
        public static readonly DependencyProperty PageHeaderProperty = DependencyProperty.Register("PageHeader", typeof(FrameworkElement), typeof(FlowDocumentEx), new PropertyMetadata(null));

        /// <summary>
        /// Page footer for printed documents
        /// </summary>
        public FrameworkElement PageFooter
        {
            get { return (FrameworkElement)GetValue(PageFooterProperty); }
            set { SetValue(PageFooterProperty, value); }
        }
        /// <summary>
        /// Page footer for printed documents
        /// </summary>
        public static readonly DependencyProperty PageFooterProperty = DependencyProperty.Register("PageFooter", typeof(FrameworkElement), typeof(FlowDocumentEx), new PropertyMetadata(null));

        /// <summary>
        /// Watermark for print output
        /// </summary>
        public FrameworkElement PrintWatermark
        {
            get { return (FrameworkElement)GetValue(PrintWatermarkProperty); }
            set { SetValue(PrintWatermarkProperty, value); }
        }
        /// <summary>
        /// Watermark for print output
        /// </summary>
        public static readonly DependencyProperty PrintWatermarkProperty = DependencyProperty.Register("PrintWatermark", typeof(FrameworkElement), typeof(FlowDocumentEx), new PropertyMetadata(null));

        /// <summary>
        /// Margins applied to the document when printing
        /// </summary>
        /// <value>The print margin.</value>
        public Thickness PrintMargin
        {
            get { return (Thickness)GetValue(PrintMarginProperty); }
            set { SetValue(PrintMarginProperty, value); }
        }
        /// <summary>
        /// Margins applied to the document when printing
        /// </summary>
        public static readonly DependencyProperty PrintMarginProperty = DependencyProperty.Register("PrintMargin", typeof(Thickness), typeof(FlowDocumentEx), new PropertyMetadata(new Thickness(0)));

        /// <summary>
        /// Prints this instance.
        /// </summary>
        public void Print()
        {
            PrintHelper.Print(this);
        }
    }
}

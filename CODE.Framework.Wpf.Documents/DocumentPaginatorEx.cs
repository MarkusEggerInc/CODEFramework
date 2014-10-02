using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Xml;

namespace CODE.Framework.Wpf.Documents
{
    /// <summary>
    /// Document paginator class (used for print output)
    /// </summary>
    public class DocumentPaginatorEx : DocumentPaginator
    {
        private readonly DocumentPaginator _flowDocumentPaginator;

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentPaginatorEx" /> class.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <param name="printableArea">The printable area size.</param>
        /// <param name="range">The print page range.</param>
        public DocumentPaginatorEx(FlowDocument document, Size printableArea, PageRange range)
        {
            // Clone the source document's content into a new FlowDocument.
            // This is because the pagination for the printer needs to be
            // done differently than the pagination for the displayed page.
            // We print the copy, rather that the original FlowDocument.
            var stream = new MemoryStream();
            var source = new TextRange(document.ContentStart, document.ContentEnd);
            source.Save(stream, DataFormats.Xaml);
            var documentCopy = new FlowDocument();
            var dest = new TextRange(documentCopy.ContentStart, documentCopy.ContentEnd);
            dest.Load(stream, DataFormats.Xaml);

            // Ready to go on the copy of the document
            var paginatorSource = documentCopy as IDocumentPaginatorSource;
            _flowDocumentPaginator = paginatorSource.DocumentPaginator;
            CurrentPage = 0;

            TotalPrintableArea = printableArea;
            var height = printableArea.Height;
            var width = printableArea.Width;

            var docEx = document as FlowDocumentEx;
            if (docEx != null)
            {
                width -= (docEx.PrintMargin.Left + docEx.PrintMargin.Right);
                height -= (docEx.PrintMargin.Top + docEx.PrintMargin.Bottom);
                DocumentPrintMargin = docEx.PrintMargin;
                OriginalPrintMargin = docEx.PrintMargin;

                Watermark = docEx.PrintWatermark;
                if (Watermark != null)
                {
                    Watermark.Resources = document.Resources;
                    Watermark.DataContext = document.DataContext;
                    Watermark.Measure(printableArea);
                    Watermark.Arrange(new Rect(0,0, Watermark.DesiredSize.Width, Watermark.DesiredSize.Height));
                }

                Header = docEx.PageHeader;
                if (Header != null)
                {
                    Header.Resources = document.Resources;
                    Header.DataContext = document.DataContext;
                    Header.Width = width;
                    Header.Measure(new Size(width, double.PositiveInfinity));
                    Header.Height = Header.DesiredSize.Height; // These two lines attempt to fix the size as desired and make sure it is properly measured at that
                    Header.Measure(new Size(width, Header.DesiredSize.Height));
                    Header.Arrange(new Rect(0,0, Header.DesiredSize.Width, Header.DesiredSize.Height));
                    height -= Header.DesiredSize.Height;
                    DocumentPrintMargin = new Thickness(DocumentPrintMargin.Left, DocumentPrintMargin.Top + Header.DesiredSize.Height, DocumentPrintMargin.Right, DocumentPrintMargin.Bottom);
                }

                Footer = docEx.PageFooter;
                if (Footer != null)
                {
                    Footer.Resources = document.Resources;
                    Footer.DataContext = document.DataContext;
                    Footer.Width = width;
                    Footer.Measure(new Size(width, double.PositiveInfinity));
                    Footer.Height = Footer.DesiredSize.Height; // These two lines attempt to fix the size as desired and make sure it is properly measured at that
                    Footer.Measure(new Size(width, Footer.DesiredSize.Height));
                    Footer.Arrange(new Rect(0, 0, Footer.DesiredSize.Width, Footer.DesiredSize.Height));
                    height -= Footer.DesiredSize.Height;
                    DocumentPrintMargin = new Thickness(DocumentPrintMargin.Left, DocumentPrintMargin.Top, DocumentPrintMargin.Right, DocumentPrintMargin.Bottom + Footer.DesiredSize.Height);
                }
            }
            else
                DocumentPrintMargin = new Thickness();

            _flowDocumentPaginator.PageSize = new Size(width, height);
            _flowDocumentPaginator.ComputePageCount();
            TotalPages = _flowDocumentPaginator.PageCount;

            Range = range;
        }

        /// <summary>Defines the area within the total printable area that can be used to print page content</summary>
        private Thickness DocumentPrintMargin { get; set; }
        /// <summary>Defines the area within the total printable area that can be used for any content (including headers and footers)</summary>
        private Thickness OriginalPrintMargin { get; set; }
        /// <summary>Printable page size (without margins)</summary>
        private Size TotalPrintableArea { get; set; }

        /// <summary>Watermark</summary>
        private FrameworkElement Watermark { get; set; }
        /// <summary>Header</summary>
        private FrameworkElement Header { get; set; }
        /// <summary>Footer</summary>
        private FrameworkElement Footer { get; set; }

        /// <summary>Page currently printing</summary>
        private int CurrentPage { get; set; }
        /// <summary>Total pages printing</summary>
        private int TotalPages { get; set; }

        /// <summary>Page range to print</summary>
        private PageRange Range { get; set; }

        /// <summary>
        /// When overridden in a derived class, gets a value indicating whether <see cref="P:System.Windows.Documents.DocumentPaginator.PageCount" /> is the total number of pages.
        /// </summary>
        /// <value><c>true</c> if this instance is page count valid; otherwise, <c>false</c>.</value>
        /// <returns>true if pagination is complete and <see cref="P:System.Windows.Documents.DocumentPaginator.PageCount" /> is the total number of pages; otherwise, false, if pagination is in process and <see cref="P:System.Windows.Documents.DocumentPaginator.PageCount" /> is the number of pages currently formatted (not the total).This value may revert to false, after being true, if <see cref="P:System.Windows.Documents.DocumentPaginator.PageSize" /> or content changes; because those events would force a repagination.</returns>
        public override bool IsPageCountValid
        {
            get { return _flowDocumentPaginator != null && _flowDocumentPaginator.IsPageCountValid; }
        }

        /// <summary>
        /// When overridden in a derived class, gets a count of the number of pages currently formatted
        /// </summary>
        /// <value>The page count.</value>
        /// <returns>A count of the number of pages that have been formatted.</returns>
        public override int PageCount
        {
            get
            {
                if (_flowDocumentPaginator == null) return 0;

                if (Range.PageFrom < 2 && Range.PageTo == 0)
                    return TotalPages;

                var from = Range.PageFrom;
                var to = Math.Min(Range.PageTo, TotalPages);
                var total = to - from;
                return total + 1;
            }
        }

        /// <summary>Translates the index of the printed page to the actual page</summary>
        /// <param name="nativePageNumber">Native page number</param>
        /// <returns>Translated page</returns>
        /// <remarks>
        /// Example: If only pages 3-5 are printed, then whenever the system requests 
        /// page number 0, that is really the 3rd page (first in the printed range)
        /// and thus page index 2 has to be returned
        /// </remarks>
        private int GetTranslatedPageNumber(int nativePageNumber)
        {
            if (Range.PageFrom < 2 && Range.PageTo == 0)
                return nativePageNumber;

            var translatedPageNumber = nativePageNumber + Range.PageFrom - 1;
            return translatedPageNumber;
        }

        /// <summary>
        /// When overridden in a derived class, gets or sets the suggested width and height of each page.
        /// </summary>
        /// <value>The size of the page.</value>
        /// <returns>A <see cref="T:System.Windows.Size" /> representing the width and height of each page.</returns>
        public override Size PageSize
        {
            get { return _flowDocumentPaginator.PageSize; }
            set { _flowDocumentPaginator.PageSize = value; }
        }

        /// <summary>
        /// When overridden in a derived class, returns the element being paginated.
        /// </summary>
        /// <value>The source.</value>
        /// <returns>An <see cref="T:System.Windows.Documents.IDocumentPaginatorSource" /> representing the element being paginated.</returns>
        public override IDocumentPaginatorSource Source
        {
            get { return _flowDocumentPaginator == null ? null : _flowDocumentPaginator.Source; }
        }

        /// <summary>
        /// When overridden in a derived class, gets the <see cref="T:System.Windows.Documents.DocumentPage" /> for the specified page number.
        /// </summary>
        /// <param name="pageNumber">The zero-based page number of the document page that is needed.</param>
        /// <returns>The <see cref="T:System.Windows.Documents.DocumentPage" /> for the specified <paramref name="pageNumber" />, or <see cref="F:System.Windows.Documents.DocumentPage.Missing" /> if the page does not exist.</returns>
        public override DocumentPage GetPage(int pageNumber)
        {
            var realPageNumber = GetTranslatedPageNumber(pageNumber); // Needed if only certain pages are to be printed

            CurrentPage = realPageNumber + 1;
            var printPage = new ContainerVisual();

            RenderElement(printPage, Watermark, PrintElementType.Watermark);
            RenderElement(printPage, Header, PrintElementType.Header);

            var page = _flowDocumentPaginator.GetPage(realPageNumber);

            if (DocumentPrintMargin.Top > 0 || DocumentPrintMargin.Left > 0)
                printPage.Transform = new TranslateTransform(DocumentPrintMargin.Left, DocumentPrintMargin.Top);
            printPage.Children.Add(page.Visual);

            RenderElement(printPage, Footer, PrintElementType.Footer);

            return new DocumentPage(printPage);
        }

        /// <summary>
        /// Renders the element to the provided page (container).
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="visual">The visual.</param>
        /// <param name="alignment">The alignment.</param>
        private void RenderElement(ContainerVisual container, FrameworkElement visual, PrintElementType alignment)
        {
            if (visual == null) return;

            var drawingVisual = new DrawingVisual();
            using (var dc = drawingVisual.RenderOpen())
            {
                var visualX = 0d;
                var visualY = 0d;
                var visualHeight = visual.DesiredSize.Height;
                var visualWidth = visual.DesiredSize.Width;

                switch (alignment)
                {
                    case PrintElementType.Header:
                        visualY = OriginalPrintMargin.Top;
                        visualX = OriginalPrintMargin.Left;
                        break;
                    case PrintElementType.Footer:
                        visualY = TotalPrintableArea.Height - OriginalPrintMargin.Bottom - visual.DesiredSize.Height;
                        visualX = OriginalPrintMargin.Left;
                        break;
                    case PrintElementType.Watermark:
                        var totalWidth = TotalPrintableArea.Width - OriginalPrintMargin.Left - OriginalPrintMargin.Right;
                        var totalHeight = TotalPrintableArea.Height - OriginalPrintMargin.Top - OriginalPrintMargin.Bottom;
                        visualX = ((totalWidth - visualWidth)/2) + OriginalPrintMargin.Left;
                        visualY = ((totalHeight - visualHeight)/2) + OriginalPrintMargin.Right;
                        break;
                }


                if (visual.Margin.Top > 0.0001 || visual.Margin.Left > 0.0001 || visual.Margin.Right > 0.0001 || visual.Margin.Bottom > 0.0001)
                {
                    visualX += visual.Margin.Left;
                    visualY += visual.Margin.Top;
                    visualHeight -= (visual.Margin.Top + visual.Margin.Bottom);
                    visualWidth -= (visual.Margin.Left + visual.Margin.Right);
                }

                // We need to clone the visual, so we can change the same visual individually on each page if need be
                var xaml = XamlWriter.Save(visual);
                var stringReader = new StringReader(xaml);
                var xmlReader = XmlReader.Create(stringReader);
                var newVisual = (FrameworkElement) XamlReader.Load(xmlReader);
                newVisual.DataContext = visual.DataContext;
                newVisual.Resources = visual.Resources;

                CheckVisualForSpecialRuns(newVisual);
                var brush = new VisualBrush(newVisual) {Stretch = Stretch.None};
                var renderRect = new Rect(visualX, visualY, visualWidth, visualHeight);

                if (DocumentPrintMargin.Top > 0 || DocumentPrintMargin.Left > 0)
                    dc.PushTransform(new TranslateTransform(DocumentPrintMargin.Left * -1, DocumentPrintMargin.Top * -1));
                dc.DrawRectangle(brush, new Pen(Brushes.Transparent, 0d), renderRect);
                if (DocumentPrintMargin.Top > 0 || DocumentPrintMargin.Left > 0)
                    dc.Pop();
            }
            container.Children.Add(drawingVisual);
        }


        private void CheckVisualForSpecialRuns(FrameworkElement root)
        {
            var currentPages = new List<CurrentPage>();
            var pageCounts = new List<PageCount>();
            var text = root as TextBlock;
            if (text != null)
            {
                foreach (var run in text.Inlines)
                {
                    var pageCount = run as PageCount;
                    if (pageCount != null) pageCounts.Add(pageCount);

                    var currentPage = run as CurrentPage;
                    if (currentPage != null) currentPages.Add(currentPage);
                }

                foreach (var pageCount in pageCounts)
                    pageCount.Text = TotalPages.ToString(CultureInfo.InvariantCulture);
                foreach (var currentPage in currentPages)
                    currentPage.Text = CurrentPage.ToString(CultureInfo.InvariantCulture);
                return;
            }

            var items = root as ItemsControl;
            if (items != null)
                foreach (var item in items.Items)
                {
                    var childElement = item as FrameworkElement;
                    if (childElement != null)
                        CheckVisualForSpecialRuns(childElement);
                }
            else
            {
                var panel = root as Panel;
                if (panel != null)
                    foreach (var item in panel.Children)
                    {
                        var childElement = item as FrameworkElement;
                        if (childElement != null)
                            CheckVisualForSpecialRuns(childElement);
                    }
            }
        }

        private enum PrintElementType
        {
            Header,
            Footer,
            Watermark
        }
    }
}

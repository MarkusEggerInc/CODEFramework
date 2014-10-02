using System.IO;
using System.IO.Packaging;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Xps.Packaging;
using System.Windows.Xps.Serialization;

namespace CODE.Framework.Wpf.Documents
{
    /// <summary>This class provides methods that make it easier to print Flow Documents</summary>
    public static class PrintHelper
    {
        /// <summary>Prints the specified document.</summary>
        /// <param name="document">The document to print.</param>
        public static void Print(this FlowDocument document)
        {
            var title = "Document";
            var docEx = document as FlowDocumentEx;
            if (docEx != null)
                title = docEx.Title;
            if (string.IsNullOrEmpty(title)) title = "Document";

            var printDialog = new PrintDialog { PageRangeSelection = PageRangeSelection.AllPages, UserPageRangeEnabled = true };
            if (printDialog.ShowDialog() != true) return;
            if (printDialog.PrintTicket.PageMediaSize.Width == null || printDialog.PrintTicket.PageMediaSize.Height == null) return;

            var pageSize = new Size((double) printDialog.PrintTicket.PageMediaSize.Width, (double) printDialog.PrintTicket.PageMediaSize.Height);
            printDialog.PrintDocument(new DocumentPaginatorEx(document, pageSize, printDialog.PageRange), title);
        }

        /// <summary>
        /// Saves a flow document as an XPS file
        /// </summary>
        /// <param name="document">The document to be sabed.</param>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="pageWidth">Width of the page.</param>
        /// <param name="pageHeight">Height of the page.</param>
        public static void SaveAsXps(this FlowDocument document, string fileName, double pageWidth = 816, double pageHeight = 1056)
        {
            if (!fileName.ToLower().EndsWith(".xps"))
                fileName += ".xps";

            using (var container = Package.Open(fileName, FileMode.Create))
            using (var xpsDoc = new XpsDocument(container, CompressionOption.Maximum))
            {
                var rsm = new XpsSerializationManager(new XpsPackagingPolicy(xpsDoc), false);
                var paginator = new DocumentPaginatorEx(document, new Size(pageWidth, pageHeight), new PageRange());
                rsm.SaveAsXaml(paginator);
            }
        }
    }
}

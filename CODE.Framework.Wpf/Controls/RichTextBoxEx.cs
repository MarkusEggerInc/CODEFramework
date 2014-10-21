using CODE.Framework.Core.Utilities;
using CODE.Framework.Wpf.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Threading;
using core = CODE.Framework.Core.Configuration;

namespace CODE.Framework.Wpf.Controls
{
    /// <summary>
    /// RichtextBox with a bindable property "Xaml" which is a FlowDocument serialized as a string.
    /// </summary>
    public class RichTextBoxEx : RichTextBox
    {
        private static bool _inSerialization;
        private static bool _inSet;
        private static readonly Dictionary<RichTextBox, bool> EventsHooked = new Dictionary<RichTextBox, bool>();

        /// <summary>
        /// XAML version of the document
        /// </summary>
        public static readonly DependencyProperty XamlProperty = DependencyProperty.RegisterAttached("Xaml", typeof(string), typeof(RichTextBoxEx), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, ChangeXaml));

        /// <summary>
        /// Changes the xaml.
        /// </summary>
        /// <param name="dependencyObject">The dependency object.</param>
        /// <param name="a">The <see cref="DependencyPropertyChangedEventArgs" /> instance containing the event data.</param>
        private static void ChangeXaml(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs a)
        {
            var richTextBox = dependencyObject as RichTextBox;
            if (richTextBox == null) return;
            if (_inSerialization) return;
            if (a.NewValue == null) return;

            var xaml = a.NewValue.ToString();

            if (!EventsHooked.ContainsKey(richTextBox))
            {
                richTextBox.TextChanged += (s, e) => Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action<RichTextBox>(UpdateXaml), richTextBox);
                EventsHooked.Add(richTextBox, true);
            }

            FlowDocument doc;

            var serializer = GetDocumentSerializer(richTextBox);
            if (serializer == null)
                if (string.IsNullOrEmpty(xaml))
                    doc = new FlowDocument();
                else
                    try
                    {
                        doc = XamlReader.Parse(xaml) as FlowDocument;
                    }
                    catch
                    {
                        doc = new FlowDocument();
                    }
            else
                doc = serializer.Deserialize(xaml);

            richTextBox.Document = doc ?? new FlowDocument();
        }

        /// <summary>
        /// Updates the XAML property from the textbox's document
        /// </summary>
        /// <param name="richTextBox">The rich text box.</param>
        private static void UpdateXaml(RichTextBox richTextBox)
        {
            if (richTextBox.Document == null) return;
            if (_inSet) return;

            var newXaml = string.Empty;
            var serializer = GetDocumentSerializer(richTextBox);
            if (serializer == null)
            {
                newXaml = XamlWriter.Save(richTextBox.Document);
                if (GetXamlEditMode(richTextBox) == XamlEditMode.XamlSnippet && !string.IsNullOrEmpty(newXaml))
                {
                    // We need to get rid of the flow document overhead
                    if (newXaml.StartsWith("<FlowDocument"))
                    {
                        var firstTagClosePosition = newXaml.IndexOf(">", StringComparison.Ordinal);
                        if (firstTagClosePosition > -1)
                            if (newXaml.Length > firstTagClosePosition + 1)
                                newXaml = newXaml.Substring(firstTagClosePosition + 1);
                            else
                                newXaml = string.Empty;
                        newXaml = newXaml.Replace("</FlowDocument>", string.Empty);
                    }
                }
            }
            else
                newXaml = serializer.Serialize(richTextBox.Document);

            _inSerialization = true;
            SetXaml(richTextBox, newXaml);
            _inSerialization = false;
        }

        /// <summary>
        /// Returns the XAML version of the document
        /// </summary>
        /// <param name="obj">The textbox object</param>
        /// <returns>XAML flow document string</returns>
        public static string GetXaml(DependencyObject obj)
        {
            return (string)obj.GetValue(XamlProperty);
        }

        /// <summary>
        /// Sets the document based on a XAML string
        /// </summary>
        /// <param name="obj">The textbox object</param>
        /// <param name="value">The XAML document string to set</param>
        public static void SetXaml(DependencyObject obj, string value)
        {
            _inSet = true;
            obj.SetValue(XamlProperty, value);
            _inSet = false;
        }

        /// <summary>
        /// Defines whether the serialized XAML property shall be a fully qualified FlowDocument or just a snippet of XAML text
        /// </summary>
        public static readonly DependencyProperty XamlEditModeProperty = DependencyProperty.RegisterAttached("XamlEditMode", typeof(XamlEditMode), typeof(RichTextBoxEx), new PropertyMetadata(XamlEditMode.FlowDocument));

        /// <summary>
        /// Returns the XAML version of the document
        /// </summary>
        /// <param name="obj">The textbox object</param>
        /// <returns>XAML flow document string</returns>
        public static XamlEditMode GetXamlEditMode(DependencyObject obj)
        {
            return (XamlEditMode)obj.GetValue(XamlEditModeProperty);
        }

        /// <summary>
        /// Sets the document based on a XAML string
        /// </summary>
        /// <param name="obj">The textbox object</param>
        /// <param name="value">The XAML document string to set</param>
        public static void SetXamlEditMode(DependencyObject obj, XamlEditMode value)
        {
            obj.SetValue(XamlEditModeProperty, value);
        }

        /// <summary>
        /// Defines a custom serializer object for the flow document
        /// </summary>
        public static readonly DependencyProperty DocumentSerializerProperty = DependencyProperty.RegisterAttached("DocumentSerializer", typeof(IFlowDocumentSerializer), typeof(RichTextBoxEx), new PropertyMetadata(null));

        /// <summary>
        /// Defines a custom serializer object for the flow document
        /// </summary>
        /// <param name="obj">The textbox object</param>
        /// <returns>Flow document serializer</returns>
        public static IFlowDocumentSerializer GetDocumentSerializer(DependencyObject obj)
        {
            return (IFlowDocumentSerializer)obj.GetValue(DocumentSerializerProperty);
        }

        /// <summary>
        /// Defines a custom serializer object for the flow document
        /// </summary>
        /// <param name="obj">The textbox object</param>
        /// <param name="value">The flow document serializer</param>
        public static void SetDocumentSerializer(DependencyObject obj, IFlowDocumentSerializer value)
        {
            obj.SetValue(DocumentSerializerProperty, value);
        }
    }

    /// <summary>
    /// Defines different edit modes for XAML base text
    /// </summary>
    public enum XamlEditMode
    {
        /// <summary>
        /// Text is serialized as a fully qualified flow document
        /// </summary>
        FlowDocument,
        /// <summary>
        /// Text is serialized as a XAML snippet
        /// </summary>
        XamlSnippet
    }

    /// <summary>
    /// Flow document serializer interface
    /// </summary>
    /// <remarks>
    /// This interface has been created to enable custom serialization of flow documents in rich text boxes
    /// </remarks>
    public interface IFlowDocumentSerializer
    {
        /// <summary>
        /// Returns the text/xaml for a given flow document
        /// </summary>
        /// <param name="document">The document to serialize.</param>
        /// <returns>The serialized version of the document</returns>
        string Serialize(FlowDocument document);

        /// <summary>
        /// Returns a flow document representing the provided text
        /// </summary>
        /// <param name="text">The serialized version of the flow document.</param>
        /// <returns>Flow document</returns>
        FlowDocument Deserialize(string text);
    }
}

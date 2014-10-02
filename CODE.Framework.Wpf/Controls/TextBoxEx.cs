using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace CODE.Framework.Wpf.Controls
{
    /// <summary>
    /// This class provides extensions to the text box class
    /// </summary>
    public class TextBoxEx : TextBox
    {
        /// <summary>Watermark text property (can be used to set text for empty textboxes)</summary>
        public static readonly DependencyProperty WatermarkTextProperty = DependencyProperty.RegisterAttached("WatermarkText", typeof(string), typeof(TextBoxEx), new PropertyMetadata(""));
        /// <summary>Watermark text property (can be used to set text for empty textboxes)</summary>
        /// <param name="o">The object to set the value on.</param>
        /// <param name="value">The value.</param>
        public static void SetWatermarkTextProperty(DependencyObject o, string value)
        {
            o.SetValue(WatermarkTextProperty, value);
        }
        /// <summary>Watermark text property (can be used to set text for empty textboxes)</summary>
        /// <param name="o">The object to get the value for.</param>
        /// <returns>System.String.</returns>
        public static string GetWatermarkTextProperty(DependencyObject o)
        {
            return (string)o.GetValue(WatermarkTextProperty);
        }

        /// <summary>
        /// Attached property can be used to define a RegEx based input mask
        /// </summary>
        public static readonly DependencyProperty RegexInputMaskProperty = DependencyProperty.RegisterAttached("RegexInputMask", typeof(Regex), typeof(TextBoxEx), new PropertyMetadata(null));

        /// <summary>
        /// Attached property can be used to define a RegEx based input mask
        /// </summary>
        public static readonly DependencyProperty InputMaskRegExProperty = DependencyProperty.RegisterAttached("InputMaskRegEx", typeof(string), typeof(TextBoxEx), new PropertyMetadata(null, OnInputMaskRegExChanged));

        /// <summary>
        /// Fires when the input mask regular expression changes
        /// </summary>
        /// <param name="o">The object the mask is set on.</param>
        /// <param name="args">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void OnInputMaskRegExChanged(DependencyObject o, DependencyPropertyChangedEventArgs args)
        {
            var textBox = o as TextBoxBase;
            if (textBox == null) return;

            var stringExpression = args.NewValue as string;
            if (string.IsNullOrEmpty(stringExpression))
            {
                textBox.SetValue(RegexInputMaskProperty, null);
                textBox.PreviewKeyDown -= PreviewKeyDownHandler;
                textBox.PreviewTextInput -= PreviewTextInputHandler;
                DataObject.RemovePastingHandler(textBox, PastingHandler);
            }
            else
            {
                var regex = new Regex(stringExpression, RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace);
                textBox.SetValue(RegexInputMaskProperty, regex);
                textBox.PreviewKeyDown += PreviewKeyDownHandler;
                textBox.PreviewTextInput += PreviewTextInputHandler;
                DataObject.AddPastingHandler(textBox, PastingHandler);
            }
        }

        /// <summary>
        /// Text input handler
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="TextCompositionEventArgs"/> instance containing the event data.</param>
        private static void PreviewTextInputHandler(object sender, TextCompositionEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox == null) return;

            var regex = textBox.GetValue(RegexInputMaskProperty) as Regex;
            if (regex == null) return;

            var proposedText = GetProposedText(textBox, e.Text);

            if (!regex.IsMatch(proposedText)) e.Handled = true; // The input doesn't match the pattern, so we throw it out.
        }

        private static void PreviewKeyDownHandler(object sender, KeyEventArgs e)
        {
            //pressing space doesn't raise PreviewTextInput, reasons here http://social.msdn.microsoft.com/Forums/en-US/wpf/thread/446ec083-04c8-43f2-89dc-1e2521a31f6b?prof=required
            if (e.Key != Key.Space) return;

            var textBox = sender as TextBox;
            if (textBox == null) return;

            var regex = textBox.GetValue(RegexInputMaskProperty) as Regex;
            if (regex == null) return;

            var proposedText = e.Key == Key.Back ? GetProposedTextBackspace(textBox) : GetProposedText(textBox, " ");
            if (!regex.IsMatch(proposedText)) e.Handled = true;
        }

        /// <summary>
        /// Fires when text is pasted into the control
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="DataObjectPastingEventArgs"/> instance containing the event data.</param>
        private static void PastingHandler(object sender, DataObjectPastingEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox == null) return;

            var regex = textBox.GetValue(RegexInputMaskProperty) as Regex;
            if (regex == null) return;


            if (e.DataObject.GetDataPresent(typeof (string)))
            {
                var pastedText = e.DataObject.GetData(typeof (string)) as string;
                var proposedText = GetProposedText(textBox, pastedText);
                if (!regex.IsMatch(proposedText)) e.CancelCommand();
            }
            else
                e.CancelCommand();
        }

        /// <summary>
        /// Gets the text a new input event would produce if we let things go forward
        /// </summary>
        /// <param name="textBox">The text box.</param>
        /// <param name="newText">The new text.</param>
        /// <returns>System.String.</returns>
        private static string GetProposedText(TextBox textBox, string newText)
        {
            var text = textBox.Text;
            if (textBox.SelectionStart != -1) text = text.Remove(textBox.SelectionStart, textBox.SelectionLength);
            text = text.Insert(textBox.CaretIndex, newText);
            return text;
        }

        /// <summary>
        /// Gets the proposed text assuming a backspace.
        /// </summary>
        /// <param name="textBox">The text box.</param>
        /// <returns>System.String.</returns>
        private static string GetProposedTextBackspace(TextBox textBox)
        {
            var text = GetTextWithSelectionRemoved(textBox);
            if (textBox.SelectionStart > 0 && textBox.SelectionLength == 0) text = text.Remove(textBox.SelectionStart - 1, 1);
            return text;
        }

        /// <summary>
        /// Returns what the textbox text would be with the current selection removed
        /// </summary>
        /// <param name="textBox">TextBox</param>
        /// <returns>Text</returns>
        private static string GetTextWithSelectionRemoved(TextBox textBox)
        {
            var text = textBox.Text;
            if (textBox.SelectionStart != -1)text = text.Remove(textBox.SelectionStart, textBox.SelectionLength);
            return text;
        }

        /// <summary>
        /// Sets the input mask.
        /// </summary>
        public static void SetInputMaskRegEx(DependencyObject o, string value)
        {
            o.SetValue(InputMaskRegExProperty, value);
        }

        /// <summary>
        /// Gets the input mask.
        /// </summary>
        public static string GetInputMaskRegEx(DependencyObject o)
        {
            return (string)o.GetValue(InputMaskRegExProperty);
        }
    }
}

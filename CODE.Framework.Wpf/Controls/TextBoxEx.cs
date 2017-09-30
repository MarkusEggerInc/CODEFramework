using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using CODE.Framework.Core.Utilities;
using CODE.Framework.Wpf.Utilities;

namespace CODE.Framework.Wpf.Controls
{
    /// <summary>
    /// This class provides extensions to the text box class
    /// </summary>
    public class TextBoxEx : TextBox
    {
        /// <summary>Watermark text property (can be used to set text for empty textboxes)</summary>
        public static readonly DependencyProperty WatermarkTextProperty = DependencyProperty.RegisterAttached("WatermarkText", typeof (string), typeof (TextBoxEx), new PropertyMetadata(""));

        /// <summary>Watermark text property (can be used to set text for empty textboxes)</summary>
        /// <param name="o">The object to set the value on.</param>
        /// <param name="value">The value.</param>
        public static void SetWatermarkText(DependencyObject o, string value)
        {
            o.SetValue(WatermarkTextProperty, value);
        }

        /// <summary>Watermark text property (can be used to set text for empty textboxes)</summary>
        /// <param name="o">The object to get the value for.</param>
        /// <returns>System.String.</returns>
        public static string GetWatermarkText(DependencyObject o)
        {
            return (string) o.GetValue(WatermarkTextProperty);
        }

        /// <summary>
        /// Attached property can be used to define a RegEx based input mask
        /// </summary>
        public static readonly DependencyProperty RegexInputMaskProperty = DependencyProperty.RegisterAttached("RegexInputMask", typeof (Regex), typeof (TextBoxEx), new PropertyMetadata(null));

        /// <summary>
        /// Attached property can be used to define a RegEx based input mask
        /// </summary>
        public static readonly DependencyProperty InputMaskRegExProperty = DependencyProperty.RegisterAttached("InputMaskRegEx", typeof (string), typeof (TextBoxEx), new PropertyMetadata(null, OnInputMaskRegExChanged));

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
            if (textBox.SelectionStart != -1) text = text.Remove(textBox.SelectionStart, textBox.SelectionLength);
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
            return (string) o.GetValue(InputMaskRegExProperty);
        }


        //CUSTOM SPELLCHECK DICTIONARIES
        private static string _customDictionaryFile;
        private static string _ignoreAllDictionaryFile;
        private static List<string> _customDictionaries;

        /// <summary>
        /// Fires when the spell check flag is toggled
        /// </summary>
        public static event EventHandler ToggleSpellCheck;

        ///<summary>When set to True, assumes a custom user dictionary named UserDictionary.lex in the application folder.
        ///Spell check context menu allows user to add words to the UserDictionary.lex custom dictionary.
        ///If UserDictionary.lex does not exist, it will be created.
        ///IgnoreAllDictionary.lex will also be created. Unlike UserDictionary.lex it will be overwritten each time the app is restarted.
        ///Other custom dictionaries found in the same folder will be used, but only UserDictionary.lex will get "Added" words.
        ///Custom dictionary path can be overridden in the app.config by adding a CustomDictionaryPath key and value to appSettings.
        ///Custom dictionary path setting is applicaiton wide.</summary>
        public static readonly DependencyProperty UseCustomDictionariesProperty = DependencyProperty.RegisterAttached("UseCustomDictionaries", typeof (bool), typeof (TextBoxEx), new PropertyMetadata(false, UseCustomDictionariesChanged));

        /// <summary>
        /// Gets UseCustomDictionaries
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public static bool GetUseCustomDictionaries(DependencyObject obj)
        {
            return (bool) obj.GetValue(UseCustomDictionariesProperty);
        }

        /// <summary>
        /// Sets UseCustomDictionaries
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <param name="value">if set to <c>true</c> [value].</param>
        public static void SetUseCustomDictionaries(DependencyObject obj, bool value)
        {
            obj.SetValue(UseCustomDictionariesProperty, value);
        }

        /// <summary>
        /// Fires when UseCustomDictionaries changes
        /// </summary>
        /// <param name="dependencyObject">The dependency object.</param>
        /// <param name="a">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void UseCustomDictionariesChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs a)
        {
            var textBoxBase = dependencyObject as TextBoxBase;
            if (textBoxBase == null) return;
            var isEnabled = (bool) a.NewValue;

            if (isEnabled)
            {
                var handler = new EventHandler((s, e) =>
                {
                    SpellCheck.SetIsEnabled(textBoxBase, false);
                    SpellCheck.SetIsEnabled(textBoxBase, true);
                });

                ToggleSpellCheck += handler;
                textBoxBase.Unloaded += (s, e) => ToggleSpellCheck -= handler;

                SpellCheck.SetIsEnabled(textBoxBase, true);

                lock (textBoxBase)
                {
                    try
                    {
                        if (_customDictionaries == null) //load custom dictionaries
                        {
                            _customDictionaries = new List<string>();
                            var customDictionaryPath = SpellCheckHelper.GetCustomDictionaryPath();
                            if (!Directory.Exists(customDictionaryPath)) Directory.CreateDirectory(customDictionaryPath);
                            _customDictionaryFile = SpellCheckHelper.GetCustomDictionaryFile(customDictionaryPath);
                            _ignoreAllDictionaryFile = SpellCheckHelper.GetIgnoreAllDictionaryFile(customDictionaryPath);
                            var fs = File.Create(_ignoreAllDictionaryFile);
                            fs.Close();
                            if (!File.Exists(_customDictionaryFile))
                            {
                                fs = File.Create(_customDictionaryFile);
                                fs.Close();
                            }

                            var dir = new DirectoryInfo(customDictionaryPath);
                            foreach (var fileInfo in dir.GetFiles("*.lex")) _customDictionaries.Add(fileInfo.FullName);
                        }
                        var dictionaries = SpellCheck.GetCustomDictionaries(textBoxBase);
                        dictionaries.Clear();
                        foreach (var fileName in _customDictionaries) dictionaries.Add(new Uri(fileName));
                    }
                    catch (Exception ex)
                    {
                        LoggingMediator.Log(ex);
                    }
                }

                if (textBoxBase.ContextMenu != null) return;
                textBoxBase.ContextMenu = new ContextMenu();
                textBoxBase.ContextMenuOpening += textBoxEx_ContextMenuOpening;
            }
        }

        private static void textBoxEx_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var textBoxBase = sender as TextBoxBase;
            if (textBoxBase == null) return;

            textBoxBase.ContextMenu.Items.Clear();
            SpellingError spellingError = null;
            if (textBoxBase is TextBox)
            {
                var textBox = textBoxBase as TextBox;
                spellingError = textBox.GetSpellingError(textBox.CaretIndex);
            }
            else if (textBoxBase is RichTextBox)
            {
                var richTextBox = textBoxBase as RichTextBox;
                spellingError = richTextBox.GetSpellingError(richTextBox.CaretPosition);
            }
            if (spellingError == null)
            {
                e.Handled = true;
                return;
            }

            var menuItemIndex = 0;
            foreach (var str in spellingError.Suggestions)
            {
                var mi = new MenuItem
                {
                    Header = str,
                    FontWeight = FontWeights.Bold,
                    Command = EditingCommands.CorrectSpellingError,
                    CommandParameter = str,
                    CommandTarget = textBoxBase,
                };
                textBoxBase.ContextMenu.Items.Insert(menuItemIndex, mi);
                menuItemIndex++;
            }
            if (menuItemIndex == 0)
            {
                var mi = new MenuItem
                {
                    Header = "(no suggestions)",
                    IsEnabled = false,
                };
                textBoxBase.ContextMenu.Items.Insert(menuItemIndex, mi);
                menuItemIndex++;
            }

            var separator = new Separator();
            textBoxBase.ContextMenu.Items.Insert(menuItemIndex, separator);
            menuItemIndex++;

            var ignoreAll = new MenuItem
            {
                Header = "Ignore All",
                CommandTarget = textBoxBase,
            };
            ignoreAll.Click += ignoreAll_Click;
            textBoxBase.ContextMenu.Items.Insert(menuItemIndex, ignoreAll);
            menuItemIndex++;

            var addToDictionary = new MenuItem
            {
                Header = "Add to Dictionary",
                CommandTarget = textBoxBase,
            };

            addToDictionary.Click += addToDictionary_Click;
            textBoxBase.ContextMenu.Items.Insert(menuItemIndex, addToDictionary);
        }

        private static void addToDictionary_Click(object sender, RoutedEventArgs e)
        {
            WriteToDictionary(sender, _customDictionaryFile);
        }

        private static void ignoreAll_Click(object sender, RoutedEventArgs e)
        {
            WriteToDictionary(sender, _ignoreAllDictionaryFile);
        }

        private static void WriteToDictionary(object sender, string dictionaryFile)
        {
            var item = sender as MenuItem;
            if (item == null) return;
            var textBoxBase = item.CommandTarget as TextBoxBase;
            ;
            if (textBoxBase == null) return;

            string misspelledWord = string.Empty;
            if (textBoxBase is TextBox)
            {
                var textBox = textBoxBase as TextBox;
                misspelledWord = textBox.Text.Substring(textBox.GetSpellingErrorStart(textBox.CaretIndex), textBox.GetSpellingErrorLength(textBox.CaretIndex));
            }
            else if (textBoxBase is RichTextBox)
            {
                var richTextBox = textBoxBase as RichTextBox;
                var range = richTextBox.GetSpellingErrorRange(richTextBox.CaretPosition);
                if (range != null) misspelledWord = range.Text;
            }
            if (string.IsNullOrWhiteSpace(misspelledWord)) return;

            try
            {
                var writer = File.AppendText(dictionaryFile);
                writer.WriteLine(misspelledWord);
                writer.Close();
                if (ToggleSpellCheck != null) ToggleSpellCheck(textBoxBase, null);
            }
            catch (Exception ex)
            {
                LoggingMediator.Log(ex);
            }
        }

        /// <summary>
        /// If set to true, triggers a source update on textboxes whenever the ENTER key is pressed, even if the 
        /// text element is otherwise only updated on LostFocus or Explicit
        /// </summary>
        public static readonly DependencyProperty UpdateSourceOnEnterKeyProperty = DependencyProperty.RegisterAttached("UpdateSourceOnEnterKey", typeof (bool), typeof (TextBoxEx), new PropertyMetadata(false, OnUpdateSourceOnEnterKeyChanged));

        /// <summary>
        /// Fires when the UpdateSourceOnEnterKey property changes
        /// </summary>
        /// <param name="d">The object the property is set on</param>
        /// <param name="e">Event arguments</param>
        private static void OnUpdateSourceOnEnterKeyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(bool) e.NewValue) return;

            var text = d as TextBox;
            if (text == null) return;

            text.PreviewKeyDown += (s2, e2) =>
            {
                if (e2.Key == Key.Enter)
                {
                    var binding = BindingOperations.GetBindingExpression(text, TextProperty);
                    if (binding != null)
                        binding.UpdateSource();
                }
            };
        }

        /// <summary>
        /// If set to true, triggers a source update on textboxes whenever the ENTER key is pressed, even if the 
        /// text element is otherwise only updated on LostFocus or Explicit
        /// </summary>
        /// <param name="d">The object the property is set on</param>
        public static bool GetUpdateSourceOnEnterKey(DependencyObject d)
        {
            return (bool) d.GetValue(UpdateSourceOnEnterKeyProperty);
        }

        /// <summary>
        /// If set to true, triggers a source update on textboxes whenever the ENTER key is pressed, even if the 
        /// text element is otherwise only updated on LostFocus or Explicit
        /// </summary>
        /// <param name="d">The object the property is set on</param>
        /// <param name="value">The new value that is to be set</param>
        public static void SetUpdateSourceOnEnterKey(DependencyObject d, bool value)
        {
            d.SetValue(UpdateSourceOnEnterKeyProperty, value);
        }

        /// <summary>
        /// When an InputMask is applied, this attached property contains the unmasked value of the textbox.
        /// For instance, if the mask is (999) 999-999 for a phone number entry, then the the user can type
        /// something like (555) 123-4567. The ValueUnmasked property will contain just 5551234567.
        /// </summary>
        public static readonly DependencyProperty TextUnmaskedProperty = DependencyProperty.RegisterAttached("TextUnmasked", typeof (string), typeof (TextBoxEx), new FrameworkPropertyMetadata("", OnTextUnmaskedChanged) {BindsTwoWayByDefault = true});

        private static void OnTextUnmaskedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var textbox = d as TextBox;
            if (textbox == null) return;
            if ((bool)textbox.GetValue(InputMaskIsValidatingProperty)) return;
            var mask = GetInputMask(textbox);
            if (string.IsNullOrEmpty(mask)) return;
            var newText = string.Empty;
            if (e.NewValue != null) newText = e.NewValue.ToString();
            ValidateInputMaskText(textbox, mask, newText);
        }

        /// <summary>
        /// When an InputMask is applied, this attached property contains the unmasked value of the textbox.
        /// For instance, if the mask is (999) 999-999 for a phone number entry, then the the user can type
        /// something like (555) 123-4567. The ValueUnmasked property will contain just 5551234567.
        /// </summary>
        /// <param name="d">The textbox the mask is set on</param>
        /// <returns>Unmasked value</returns>
        public static string GetTextUnmasked(DependencyObject d)
        {
            return (string) d.GetValue(TextUnmaskedProperty);
        }

        /// <summary>
        /// When an InputMask is applied, this attached property contains the unmasked value of the textbox.
        /// For instance, if the mask is (999) 999-999 for a phone number entry, then the the user can type
        /// something like (555) 123-4567. The ValueUnmasked property will contain just 5551234567.
        /// </summary>
        /// <param name="d">The textbox the mask is set on</param>
        /// <param name="value">The value.</param>
        /// <remarks>This value should never be set manually.</remarks>
        public static void SetTextUnmasked(DependencyObject d, string value)
        {
            d.SetValue(TextUnmaskedProperty, value);
        }

        /// <summary>
        /// Defines the input mask for the characteristic part (the part to the left of the .) of decimal (d, %) formatting.
        /// The default is '###,###,###,##0', which allows entering up to 12 digits
        /// </summary>
        public static readonly DependencyProperty InputMaskDecimalCharacteristicMaskProperty = DependencyProperty.RegisterAttached("InputMaskDecimalCharacteristicMask", typeof(string), typeof(TextBoxEx), new PropertyMetadata("###,###,###,##0"));

        /// <summary>
        /// Defines the input mask for the characteristic part (the part to the left of the .) of decimal (d, %) formatting.
        /// The default is '###,###,###,##0', which allows entering up to 12 digits
        /// </summary>
        /// <param name="d">The element the value is set on</param>
        /// <returns>Mask for the characteristic part of a decimal input mask (default is '###,###,###,##0')</returns>
        public static string GetInputMaskDecimalCharacteristicMask(DependencyObject d)
        {
            return d.GetValue(InputMaskDecimalCharacteristicMaskProperty).ToString();
        }

        /// <summary>
        /// Defines the input mask for the characteristic part (the part to the left of the .) of decimal (d, %) formatting.
        /// The default is '###,###,###,##0', which allows entering up to 12 digits
        /// </summary>
        /// <param name="d">The element the value is retrieved from</param>
        /// <param name="value">Mask for the characteristic, such as '###,###,###,##0'</param>
        public static void SetInputMaskDecimalCharacteristicMask(DependencyObject d, string value)
        {
            d.SetValue(InputMaskDecimalCharacteristicMaskProperty, value);
        }

        /// <summary>
        /// Defines the input mask for the fractional part (the 'mantissa') of decimal (d, %) formatting.
        /// The default is '00', which means that there can be two digits in the fraction. If you wanted 
        /// 4 digits, set this to '0000'
        /// </summary>
        public static readonly DependencyProperty InputMaskDecimalFractionalMaskProperty = DependencyProperty.RegisterAttached("InputMaskDecimalFractionalMask", typeof(string), typeof(TextBoxEx), new PropertyMetadata("00"));

        /// <summary>
        /// Defines the input mask for the fractional part (the 'mantissa') of decimal (d, %) formatting.
        /// The default is '00', which means that there can be two digits in the fraction. If you wanted 
        /// 4 digits, set this to '0000'
        /// </summary>
        /// <param name="d">The element the value is set on</param>
        /// <returns>Mask for the mantissa part of a decimal input mask</returns>
        public static string GetInputMaskDecimalFractionalMask(DependencyObject d)
        {
            return d.GetValue(InputMaskDecimalFractionalMaskProperty).ToString();
        }

        /// <summary>
        /// Defines the input mask for the fractional part (the 'mantissa') of decimal (d, %) formatting.
        /// The default is '00', which means that there can be two digits in the fraction. If you wanted 
        /// 4 digits, set this to '0000'
        /// </summary>
        /// <param name="d">The element the value is retrieved from</param>
        /// <param name="value">Mask for the mantissa, such as '00' or '0000'</param>
        public static void SetInputMaskDecimalFractionalMask(DependencyObject d, string value)
        {
            d.SetValue(InputMaskDecimalFractionalMaskProperty, value);
        }

        /// <summary>
        /// Currency symbol (such as $, €, or £ - can be multiple characters)
        /// </summary>
        public static readonly DependencyProperty InputMaskCurrencySymbolProperty = DependencyProperty.Register("InputMaskCurrencySymbol", typeof(string), typeof(TextBoxEx), new PropertyMetadata(""));

        /// <summary>
        /// Currency symbol (such as $, €, or £ - can be multiple characters)
        /// </summary>
        /// <param name="d">The object the symbol is set on</param>
        /// <returns>Currency symbol</returns>
        public static string GetInputMaskCurrencySymbol(DependencyObject d)
        {
            return (string) d.GetValue(InputMaskCurrencySymbolProperty);
        }

        /// <summary>
        /// Currency symbol (such as $, €, or £ - can be multiple characters)
        /// </summary>
        /// <param name="d">The object the symbol is set on</param>
        /// <param name="value">Currency symbol to set.</param>
        public static void SetInputMaskCurrencySymbol(DependencyObject d, string value)
        {
            d.SetValue(InputMaskCurrencySymbolProperty, value);
        }

        /// <summary>
        /// If set to true, the decimal or currency input mask supports negative values as well as positive ones
        /// </summary>
        public static readonly DependencyProperty InputMaskSupportsNegativeProperty = DependencyProperty.RegisterAttached("InputMaskSupportsNegative", typeof(bool), typeof(TextBoxEx), new PropertyMetadata(true));

        /// <summary>
        /// If set to true, the decimal or currency input mask supports negative values as well as positive ones
        /// </summary>
        /// <param name="d">The element to get the value on</param>
        /// <returns>True (default) if negative values are supported</returns>
        public static bool GetInputMaskSupportsNegative(DependencyObject d)
        {
            return (bool) d.GetValue(InputMaskSupportsNegativeProperty);
        }

        /// <summary>
        /// If set to true, the decimal or currency input mask supports negative values as well as positive ones
        /// </summary>
        /// <param name="d">The element to set the value on</param>
        /// <param name="value">True (default) if negative values are supported</param>
        public static void SetInputMaskSupportsNegative(DependencyObject d, bool value)
        {
            d.SetValue(InputMaskSupportsNegativeProperty, value);
        }

        /// <summary>
        /// Provides the ability to define input masks on textboxes
        /// </summary>
        public static readonly DependencyProperty InputMaskProperty = DependencyProperty.RegisterAttached("InputMask", typeof (string), typeof (TextBoxEx), new PropertyMetadata("", OnInputMaskChanged));

        /// <summary>
        /// Provides the ability to define input masks on textboxes
        /// </summary>
        /// <param name="d">The object the input mask is defined on</param>
        /// <returns>Input mask</returns>
        public static string GetInputMask(DependencyObject d)
        {
            return (string) d.GetValue(InputMaskProperty);
        }

        /// <summary>
        /// Provides the ability to define input masks on textboxes
        /// </summary>
        /// <param name="d">The object the mask is to be set on</param>
        /// <param name="value">The input mask.</param>
        public static void SetInputMask(DependencyObject d, string value)
        {
            d.SetValue(InputMaskProperty, value);
        }

        /// <summary>
        /// Fires whenever an input mask changes on a textbox
        /// </summary>
        /// <param name="d">The object the input mask was set on</param>
        /// <param name="e">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void OnInputMaskChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var textbox = d as TextBox;
            if (textbox == null) return;
            var mask = e.NewValue.ToString();

            // Detaching the event handler. This makes sure we are not attached when the mask is empty,
            // and if the mask isn't empty, we reset the handler below, resulting in it being wired up once.
            textbox.PreviewKeyDown -= HandleInputMaskKeyDown;
            if (string.IsNullOrEmpty(mask)) return;

            // We have a mask, so we wire everything up we need to handle text input
            textbox.PreviewKeyDown += HandleInputMaskKeyDown;

            // Triggering the first validation to start out with proper state
            ValidateInputMaskText(textbox, mask, textbox.Text);
        }

        private static void HandleInputMaskKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) return;
            if (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt)) return;

            var ignoreKeys = new List<Key> { Key.LeftShift, Key.RightShift, Key.Tab, Key.Capital, Key.CapsLock, Key.Left, Key.Right, Key.Home, Key.End, Key.LeftAlt, Key.RightAlt, Key.LeftCtrl, Key.RightCtrl };
            if (ignoreKeys.Contains(e.Key)) return;

            var textbox = sender as TextBox;
            if (textbox == null) return;

            var mask = GetInputMask(textbox);

            if (mask == "d" || mask == "$" || mask == "%")
            {
                HandleInputMaskDecimalKeyDown(e.Key, textbox, mask);
                e.Handled = true;
            }
            else
            {
                var startPosition = textbox.SelectionStart;

                // We are using the default .NET MaskedTextProvider to provide this functionality
                var provider = GetMaskedTextProvider(textbox, mask);

                // Need to handle selected text properly by replacing it with the new input

                if (e.Key == Key.Delete)
                {
                    if (textbox.SelectionLength > 0)
                        provider.RemoveAt(startPosition, startPosition + textbox.SelectionLength - 1);
                    else
                        provider.RemoveAt(startPosition);
                }
                else if (e.Key == Key.Back)
                {
                    if (startPosition > 0 || textbox.SelectionLength > 0)
                        if (textbox.SelectionLength > 0)
                            provider.RemoveAt(startPosition, startPosition + textbox.SelectionLength - 1);
                        else
                            provider.RemoveAt(startPosition - 1);
                }
                else
                {
                    if (textbox.SelectionLength > 0)
                        provider.RemoveAt(startPosition, startPosition + textbox.SelectionLength - 1);
                    var newChar = KeyboardHelper.GetCharFromKey(e.Key);
                    if (provider.MaskFull) provider.RemoveAt(provider.Length - 1);
                    provider.InsertAt(newChar, startPosition);
                    textbox.SetValue(InputMaskIsValidatingProperty, true);
                    textbox.Text = provider.ToDisplayString();
                    textbox.SetValue(InputMaskIsValidatingProperty, false);
                    var newCarotPosition = provider.FindAssignedEditPositionFrom(startPosition, true);
                    newCarotPosition++;
                    if (newCarotPosition > textbox.Text.Length - 1) newCarotPosition = textbox.Text.Length - 1;
                    if (newCarotPosition < 0) newCarotPosition = textbox.CaretIndex > 0 ? textbox.Text.Length : 0;
                    textbox.CaretIndex = newCarotPosition;
                    e.Handled = true;
                }
                textbox.SetValue(InputMaskIsValidatingProperty, true);
                SetTextUnmasked(textbox, provider.ToString(false, false));
                textbox.SetValue(InputMaskIsValidatingProperty, false);
            }
        }

        private static void HandleInputMaskDecimalKeyDown(Key key, TextBox textbox, string mask)
        {
            var startPosition = textbox.SelectionStart;
            var isCurrency = mask == "$";
            var isDecimal = mask == "%";
            var numberFormat = CultureInfo.CurrentUICulture.NumberFormat;
            var decimalSeparator = isCurrency ? numberFormat.CurrencyDecimalSeparator : numberFormat.NumberDecimalSeparator;
            var lastInsertWasDecimalSeparator = false;
            var valueIsNegative = false;
            var valueWasNegativeOnEnter = false;

            // Need to memorize how many digits there were to the left before this keystroke
            var allChars = textbox.Text.ToCharArray().ToList();
            var numberOfDigitsToTheLeft = 0;
            for (var charCounter = 0; charCounter < startPosition; charCounter++)
                if (char.IsDigit(allChars[charCounter]) || allChars[charCounter] == '-')
                    numberOfDigitsToTheLeft++;

            // We check if the value is negative
            if (allChars.Count > 0 && allChars[0] == '-')
            {
                valueIsNegative = true;
                valueWasNegativeOnEnter = true;
            }

            // Special custom decimal handling
            var currencySymbol = GetInputMaskCurrencySymbol(textbox);
            if (string.IsNullOrEmpty(currencySymbol))
            {
                currencySymbol = numberFormat.CurrencySymbol;
                SetInputMaskCurrencySymbol(textbox, currencySymbol); // So we have it for later
            }
            var newChar = KeyboardHelper.GetCharFromKey(key);
            var sb = new StringBuilder(textbox.Text);

            if (key == Key.Delete)
            {
                if (textbox.SelectionLength > 0)
                {
                    sb.Remove(startPosition, textbox.SelectionLength);
                    for (var counter = 0; counter < textbox.SelectionLength; counter++)
                        allChars.RemoveAt(startPosition);
                }
                else
                {
                    if (startPosition < sb.Length)
                    {
                        sb.Remove(startPosition, 1);
                        allChars.RemoveAt(startPosition);
                    }
                }
                if (allChars.Count == 0)
                    sb.Append("0");
            }
            else if (key == Key.Back)
            {
                if (textbox.SelectionLength > 0)
                {
                    sb.Remove(startPosition, textbox.SelectionLength);
                    for (var counter = 0; counter < textbox.SelectionLength; counter++)
                        allChars.RemoveAt(startPosition);
                }
                else if (startPosition > 0)
                {
                    if (startPosition - 1 < sb.Length)
                    {
                        sb.Remove(startPosition - 1, 1);
                        allChars.RemoveAt(startPosition - 1);
                        numberOfDigitsToTheLeft--;
                    }
                }
                if (allChars.Count == 0)
                    sb.Append("0");
            }
            else
            {
                // Need to handle selected text properly by replacing it with the new input
                if (textbox.SelectionLength > 0 && !(newChar == '-' || newChar == '+'))
                {
                    sb.Remove(startPosition, textbox.SelectionLength);
                    for (var counter = 0; counter < textbox.SelectionLength; counter++)
                        allChars.RemoveAt(startPosition);
                    if (allChars.Count == 0 && valueWasNegativeOnEnter)
                    {
                        valueWasNegativeOnEnter = false;
                        valueIsNegative = false;
                    }
                }

                if (char.IsDigit(newChar))
                {
                    sb.Insert(startPosition, newChar);
                    numberOfDigitsToTheLeft++;
                }
                else if (GetInputMaskSupportsNegative(textbox) && newChar == '-')
                {
                    // We need to make sure that the value is negative
                    valueIsNegative = true;
                    if (!valueWasNegativeOnEnter) numberOfDigitsToTheLeft++;
                }
                else if (GetInputMaskSupportsNegative(textbox) && newChar == '+')
                {
                    // We need to make sure that the value is positive
                    valueIsNegative = false;
                    if (valueWasNegativeOnEnter)
                    {
                        numberOfDigitsToTheLeft--;
                        if (numberOfDigitsToTheLeft < 0) numberOfDigitsToTheLeft = 0;
                    }
                }
                else if (newChar.ToString() == decimalSeparator)
                {
                    // If there is a decimal indicator to the left, we ignore the input
                    for (var counter = 0; counter < startPosition; counter++)
                        if (allChars[counter].ToString() == decimalSeparator)
                        {
                            SystemSounds.Beep.Play();
                            return;
                        }

                    // If there already is another decimal in the string (to the right), we need to get rid of it
                    var existingDecimalPosition = -1;
                    for (var counter = startPosition; counter < allChars.Count; counter++)
                        if (allChars[counter].ToString() == decimalSeparator)
                            existingDecimalPosition = counter;
                    if (existingDecimalPosition > -1)
                        sb.Remove(startPosition, existingDecimalPosition - startPosition + 1);
                    sb.Insert(startPosition, newChar);
                    lastInsertWasDecimalSeparator = true;
                }
                else
                    SystemSounds.Beep.Play();
            }

            var newText = sb.ToString();
            if (isCurrency && newText.StartsWith(currencySymbol))
                newText = newText.Substring(currencySymbol.Length).Trim();

            if (valueIsNegative && !newText.StartsWith("-"))
                newText = "-" + newText;
            else if (!valueIsNegative && newText.StartsWith("-"))
                newText = newText.Substring(1);

            if (isDecimal && newText.EndsWith("%"))
                newText = newText.Substring(0, newText.Length - 1);

            decimal parsedValue;
            if (decimal.TryParse(newText, out parsedValue))
            {
                var rightFormat = isCurrency ? StringHelper.Replicate("0", numberFormat.CurrencyDecimalDigits) : GetInputMaskDecimalFractionalMask(textbox);
                newText = parsedValue.ToString(GetInputMaskDecimalCharacteristicMask(textbox) + "." + rightFormat);
                if (isCurrency)
                    newText = currencySymbol + " " + newText;
                if (isDecimal)
                    newText += "%";

                textbox.SetValue(InputMaskIsValidatingProperty, true);
                textbox.Text = newText;
                SetTextUnmasked(textbox, parsedValue.ToString("###########0." + rightFormat));
                textbox.SetValue(InputMaskIsValidatingProperty, false);

                var newCaretIndex = 0;
                var digitsFound = 0;
                var newAllChars = newText.ToCharArray();
                foreach (var newAllChar in newAllChars)
                {
                    if (digitsFound >= numberOfDigitsToTheLeft) break;
                    if (isCurrency)
                    {
                        if (newCaretIndex > currencySymbol.Length && char.IsDigit(newAllChar)) digitsFound++;
                    }
                    else if (char.IsDigit(newAllChar) || newAllChar == '-') digitsFound++;
                    newCaretIndex++;
                }
                if (lastInsertWasDecimalSeparator) newCaretIndex++;
                textbox.CaretIndex = newCaretIndex;
            }
            else
                SystemSounds.Beep.Play();
        }

        private static void ValidateInputMaskText(TextBox textbox, string mask, string text)
        {
            if (string.IsNullOrEmpty(mask)) return;

            if (mask.ToLowerInvariant() == "d" || mask == "$" || mask == "%")
            {
                decimal parsedValue;
                if (decimal.TryParse(text, out parsedValue))
                {
                    var currencySymbol = GetInputMaskCurrencySymbol(textbox);
                    if (string.IsNullOrEmpty(currencySymbol))
                    {
                        currencySymbol = CultureInfo.CurrentUICulture.NumberFormat.CurrencySymbol;
                        SetInputMaskCurrencySymbol(textbox, currencySymbol); // So we have it for later
                    }
                    var rightFormat = mask == "$" ? StringHelper.Replicate("0", CultureInfo.CurrentUICulture.NumberFormat.CurrencyDecimalDigits) : GetInputMaskDecimalFractionalMask(textbox);
                    text = parsedValue.ToString("###,###,###,##0." + rightFormat);
                    if (mask == "$") text = currencySymbol + " " + text;
                    if (mask == "%") text = text + "%";
                    textbox.SetValue(InputMaskIsValidatingProperty, true);
                    textbox.Text = text;
                    SetTextUnmasked(textbox, parsedValue.ToString("###########0." + rightFormat));
                    textbox.SetValue(InputMaskIsValidatingProperty, false);
                }
            }
            else
            {
                var provider = GetMaskedTextProvider(textbox, mask);
                if (text == null) text = string.Empty;
                provider.Set(text);
                text = provider.ToString(false, false, true, 0, provider.Length);
                textbox.SetValue(InputMaskIsValidatingProperty, true);
                textbox.Text = text;
                SetTextUnmasked(textbox, provider.ToString(false, false));
                textbox.SetValue(InputMaskIsValidatingProperty, false);
            }
        }

        private static MaskedTextProvider GetMaskedTextProvider(DependencyObject d, string mask, bool allowPromptAsInput = true, char promptChar = ' ', char passwordChar = '*', bool restrictToAscii = false)
        {
            var currentProvider = d.GetValue(InputMaskMaskedTextProviderProperty) as MaskedTextProvider;
            if (currentProvider != null && currentProvider.Mask == mask) return currentProvider;

            var newProvider = new MaskedTextProvider(mask, CultureInfo.CurrentUICulture, allowPromptAsInput, promptChar, passwordChar, restrictToAscii)
            {
                ResetOnPrompt = true,
                ResetOnSpace = true,
                SkipLiterals = true,
                IncludeLiterals = true,
                IncludePrompt = true,
                IsPassword = false
            };

            d.SetValue(InputMaskMaskedTextProviderProperty, newProvider);

            return newProvider;
        }

        private static readonly DependencyProperty InputMaskMaskedTextProviderProperty = DependencyProperty.RegisterAttached("InputMaskMaskedTextProvider", typeof (MaskedTextProvider), typeof (TextBoxEx), new PropertyMetadata(null));
        private static readonly DependencyProperty InputMaskIsValidatingProperty = DependencyProperty.RegisterAttached("InputMaskIsValidating", typeof(bool), typeof(TextBoxEx), new PropertyMetadata(false));
    }
}
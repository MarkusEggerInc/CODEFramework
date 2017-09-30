using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace CODE.Framework.Wpf.Validation
{
    /// <summary>
    /// This class provides features around UI data validation
    /// </summary>
    /// <seealso cref="System.Windows.DependencyObject" />
    public class InputValidation : DependencyObject
    {
        /// <summary>
        /// Defines whether an element is valid (true) or not (false)
        /// </summary>
        public static bool GetIsValid(DependencyObject d)
        {
            return (bool)d.GetValue(IsValidProperty);
        }
        /// <summary>
        /// Defines whether an element is valid (true) or not (false)
        /// </summary>
        public static void SetIsValid(DependencyObject d, bool value)
        {
            d.SetValue(IsValidProperty, value);
        }
        /// <summary>
        /// Defines whether an element is valid (true) or not (false)
        /// </summary>
        public static readonly DependencyProperty IsValidProperty = DependencyProperty.RegisterAttached("IsValid", typeof(bool), typeof(InputValidation), new PropertyMetadata(true));

        /// <summary>
        /// List of error messages
        /// </summary>
        public static IEnumerable<string> GetErrorMessages(DependencyObject d)
        {
            return (IEnumerable<string>)d.GetValue(ErrorMessagesProperty);
        }
        /// <summary>
        /// List of error messages
        /// </summary>
        public static void SetErrorMessages(DependencyObject d, IEnumerable<string> errorMessages)
        {
            d.SetValue(ErrorMessagesProperty, errorMessages);
        }
        /// <summary>
        /// List of error messages
        /// </summary>
        public static readonly DependencyProperty ErrorMessagesProperty = DependencyProperty.RegisterAttached("ErrorMessages", typeof(IEnumerable<string>), typeof(InputValidation), new PropertyMetadata(null, OnErrorMessagesChanged));

        private static void OnErrorMessagesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue == null)
                d.SetValue(ErrorMessageProperty, string.Empty);
            else
            {
                var messages = e.NewValue as IEnumerable<string>;
                if (messages == null)
                    d.SetValue(ErrorMessageProperty, string.Empty);
                else
                {
                    var sb = new StringBuilder();
                    foreach (var message in messages)
                        sb.AppendLine(message.Trim());
                    d.SetValue(ErrorMessageProperty, sb.ToString().Trim());
                }
            }
        }

        /// <summary>
        /// Consolidated list of error messages
        /// </summary>
        public static string GetErrorMessage(DependencyObject d)
        {
            return (string)d.GetValue(ErrorMessageProperty);
        }
        /// <summary>
        /// Consolidated list of error messages
        /// </summary>
        public static void SetErrorMessage(DependencyObject d, string errorMessages)
        {
            d.SetValue(ErrorMessageProperty, errorMessages);
        }
        /// <summary>
        /// Consolidated list of error messages
        /// </summary>
        public static readonly DependencyProperty ErrorMessageProperty = DependencyProperty.RegisterAttached("ErrorMessage", typeof(string), typeof(InputValidation), new PropertyMetadata(""));

        /// <summary>
        /// Enumerable list of validation attributes, which are then automatically used to set up appropriate validation.
        /// </summary>
        public static IEnumerable<Attribute> GetValidationAttributes(DependencyObject d)
        {
            return (IEnumerable<Attribute>)d.GetValue(ValidationAttributesProperty);
        }

        /// <summary>
        /// Enumerable list of validation attributes, which are then automatically used to set up appropriate validation.
        /// </summary>
        public static void SetValidationAttributes(DependencyObject d, IEnumerable<Attribute> value)
        {
            d.SetValue(ValidationAttributesProperty, value);
        }

        /// <summary>
        /// Enumerable list of validation attributes, which are then automatically used to set up appropriate validation.
        /// </summary>
        public static readonly DependencyProperty ValidationAttributesProperty = DependencyProperty.RegisterAttached("ValidationAttributes", typeof(IEnumerable<Attribute>), typeof(InputValidation), new PropertyMetadata(null, OnValidationAttributesChanged));

        /// <summary>
        /// Fires when validation attributes change
        /// </summary>
        /// <param name="d">The d.</param>
        /// <param name="e">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void OnValidationAttributesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // TODO: Support controls other than textboxes
            var validationAttributes = GetValidationAttributes(d);
            if (validationAttributes == null) return;

            var text = d as TextBox;
            if (text != null)
            {
                OnTextBoxValidationAttributesChanged(text, validationAttributes);
                return;
            }
            var passwordBox = d as PasswordBox;
            if (passwordBox != null)
            {
                OnPasswordBoxValidationAttributesChanged(passwordBox, validationAttributes);
                return;
            }

            var selector = d as Selector;
            if (selector != null)
            {
                OnSelectorValidationAttributesChanged(selector, validationAttributes);
                return;
            }

            var toggleButton = d as ToggleButton;
            if (toggleButton != null)
            {
                OnToggleButtonValidationAttributesChanged(toggleButton, validationAttributes);
                return;
            }

            var datePicker = d as DatePicker;
            if (datePicker != null)
            {
                OnDatePickerValidationAttributesChanged(datePicker, validationAttributes);
                return;
            }
        }

        /// <summary>
        /// Handles validation logic on textboxes
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="validationAttributes">The validation attributes.</param>
        private static void OnTextBoxValidationAttributesChanged(TextBox text, IEnumerable<Attribute> validationAttributes)
        {
            if (text == null || validationAttributes == null) return;

            text.TextChanged += (s2, e2) => ApplyAttributeValidation(text, text.Text);

            var maxLengthAttribute = validationAttributes.FirstOrDefault(a => a.GetType().Name == "MaxLengthAttribute");
            if (maxLengthAttribute != null)
            {
                try
                {
                    dynamic dynamicMaxLength = maxLengthAttribute;
                    int length = dynamicMaxLength.Length;
                    if (length > 0)
                        text.MaxLength = length;
                }
                catch (Exception)
                {
                    // Oh well...
                }
            }

            ApplyAttributeValidation(text, text.Text);
        }

        /// <summary>
        /// Handles validation logic on passwordboxes
        /// </summary>
        /// <param name="passwordBox">The text.</param>
        /// <param name="validationAttributes">The validation attributes.</param>
        private static void OnPasswordBoxValidationAttributesChanged(PasswordBox passwordBox, IEnumerable<Attribute> validationAttributes)
        {
            if (passwordBox == null || validationAttributes == null) return;

            passwordBox.PasswordChanged += (s2, e2) => ApplyAttributeValidation(passwordBox, passwordBox.Password);

            var maxLengthAttribute = validationAttributes.FirstOrDefault(a => a.GetType().Name == "MaxLengthAttribute");
            if (maxLengthAttribute != null)
            {
                try
                {
                    dynamic dynamicMaxLength = maxLengthAttribute;
                    int length = dynamicMaxLength.Length;
                    if (length > 0)
                        passwordBox.MaxLength = length;
                }
                catch (Exception)
                {
                    // Oh well...
                }
            }

            ApplyAttributeValidation(passwordBox, passwordBox.Password);
        }

        /// <summary>
        /// Handles validation logic on selectors (such as listboxes)
        /// </summary>
        /// <param name="selector">The text.</param>
        /// <param name="validationAttributes">The validation attributes.</param>
        private static void OnSelectorValidationAttributesChanged(Selector selector, IEnumerable<Attribute> validationAttributes)
        {
            if (selector == null || validationAttributes == null) return;
            selector.SelectionChanged += (s2, e2) => ApplyAttributeValidation(selector, selector.SelectedItem);
            ApplyAttributeValidation(selector, selector.SelectedItem);
        }

        /// <summary>
        /// Handles validation logic on toggle buttons (such as checkboxes)
        /// </summary>
        /// <param name="toggleButton">The text.</param>
        /// <param name="validationAttributes">The validation attributes.</param>
        private static void OnToggleButtonValidationAttributesChanged(ToggleButton toggleButton, IEnumerable<Attribute> validationAttributes)
        {
            if (toggleButton == null || validationAttributes == null) return;
            toggleButton.Checked += (s2, e2) => ApplyAttributeValidation(toggleButton, toggleButton.IsChecked);
            ApplyAttributeValidation(toggleButton, toggleButton.IsChecked);
        }

        /// <summary>
        /// Handles validation logic on toggle buttons (such as checkboxes)
        /// </summary>
        /// <param name="datePicker">The text.</param>
        /// <param name="validationAttributes">The validation attributes.</param>
        private static void OnDatePickerValidationAttributesChanged(DatePicker datePicker, IEnumerable<Attribute> validationAttributes)
        {
            if (datePicker == null || validationAttributes == null) return;
            datePicker.SelectedDateChanged += (s2, e2) => ApplyAttributeValidation(datePicker, datePicker.SelectedDate);
            ApplyAttributeValidation(datePicker, datePicker.SelectedDate);
        }

        /// <summary>
        /// Performs the actual validation based on the provided attributes
        /// </summary>
        /// <param name="d">The source object to be validated</param>
        /// <param name="value">The value to be validated.</param>
        private static void ApplyAttributeValidation(DependencyObject d, object value)
        {
            var validationAttributes = GetValidationAttributes(d);
            if (validationAttributes == null) return;

            var isValid = true;

            var errorMessages = new List<string>();

            foreach (var validationAttribute in validationAttributes.OfType<ValidationAttribute>())
                if (!validationAttribute.IsValid(value))
                {
                    isValid = false;
                    errorMessages.Add(validationAttribute.FormatErrorMessage("Text"));
                }

            SetErrorMessages(d, errorMessages);
            SetIsValid(d, isValid);
        }

        /// <summary>
        /// Performs data validation on all properties of the provided object.
        /// </summary>
        /// <param name="objectToValidate">The object to validate.</param>
        /// <param name="results">An existing results object (can be useful if you want to add up multiple validations for multiple objects)</param>
        /// <returns>ValidationResults.</returns>
        public static ValidationResults ValidateObject(object objectToValidate, ValidationResults results = null)
        {
            if (results == null) results = new ValidationResults { IsValid = true };
            if (objectToValidate == null) return results;

            var objectType = objectToValidate.GetType();
            var properties = objectType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var property in properties.Where(p => p.CanRead))
            {
                var propertyError = new PropertyValidationResult(objectToValidate, property.Name);
                var validationAttributes = property.GetCustomAttributes(true).OfType<ValidationAttribute>().ToList();
                if (validationAttributes.Count > 0)
                {
                    var propertyValue = property.GetValue(objectToValidate, null);
                    foreach (var validationAttribute in validationAttributes)
                    {
                        var contextObjectAttribute = validationAttribute as IValidationWithContextObject;
                        if (contextObjectAttribute != null) contextObjectAttribute.ContextObject = objectToValidate;

                        var realValue = propertyValue != null ? propertyValue.ToString() : string.Empty;
                        if (!validationAttribute.IsValid(realValue))
                        {
                            results.IsValid = false;
                            propertyError.ErrorMessages.Add(validationAttribute.FormatErrorMessage(property.Name));
                        }
                    }
                    if (propertyError.ErrorMessages.Count > 0)
                        results.InvalidProperties.Add(propertyError);
                }
            }

            return results;
        }
    }

    /// <summary>
    /// This interface can be implemented by a custom validation attribute. 
    /// If this interface is implemented, then the current main object will be set before validation is executed.
    /// Example: A CustomerEditViewModel class has a property called ValueMayBeRequired with a custom validation attribute.
    /// Let's say the custom attribute wants to check other properties within the view-model before deciding whether the value is
    /// indeed required. For this, the attribute needs access to the view-model. The ContextObject is the view-model (it's always the object
    /// that defines the property that has the attribute).
    /// </summary>
    public interface IValidationWithContextObject
    {
        /// <summary>
        /// Gets or sets the context object.
        /// </summary>
        /// <value>The context object.</value>
        object ContextObject { get; set; }
    }

    /// <summary>
    /// Validation results for input validation on multiple objects
    /// </summary>
    public class ValidationResults
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationResults"/> class.
        /// </summary>
        public ValidationResults()
        {
            InvalidProperties = new List<PropertyValidationResult>();
        }
        /// <summary>
        /// Indicates whether all validations returned true (the entire validated set is valid)
        /// </summary>
        /// <value><c>true</c> if this instance is valid; otherwise, <c>false</c>.</value>
        public bool IsValid { get; set; }

        /// <summary>
        /// If IsValid=false, this collection contains information about all the properties that indicated an invalid state
        /// </summary>
        /// <value>The invalid properties.</value>
        public List<PropertyValidationResult> InvalidProperties { get; private set; }
    }

    /// <summary>
    /// Validation result for an individual property
    /// </summary>
    public class PropertyValidationResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyValidationResult"/> class.
        /// </summary>
        /// <param name="parentObject">The parent object.</param>
        /// <param name="propertyName">Name of the property.</param>
        public PropertyValidationResult(object parentObject, string propertyName)
        {
            ParentObject = parentObject;
            PropertyName = propertyName;
            ErrorMessages = new List<string>();
        }
        /// <summary>
        /// Object the property belongs to.
        /// </summary>
        /// <value>The parent object.</value>
        public object ParentObject { get; private set; }
        /// <summary>
        /// Name of the property that caused the validation error.
        /// </summary>
        /// <value>The name of the property.</value>
        public string PropertyName { get; private set; }
        /// <summary>
        /// List of error messages
        /// </summary>
        /// <value>The error messages.</value>
        public List<string> ErrorMessages { get; private set; }
    }
}
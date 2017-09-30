using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using CODE.Framework.Wpf.Validation;

namespace CODE.Framework.Wpf.MarkupExtensions
{
    /// <summary>
    /// An extension to the standard Binding markup extension provided by WPF.
    /// This extension provides all features of the standard WPF Binding class, as well as additional features,
    /// such as binding security setup, binding validation, and more
    /// </summary>
    /// <seealso cref="System.Windows.Markup.MarkupExtension" />
    public class Bind : MarkupExtension
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Bind"/> class.
        /// </summary>
        public Bind()
        {
            _binding = new Binding();
            BindSecurityAttribute = true;
            BindValidationAttributes = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Bind"/> class.
        /// </summary>
        /// <param name="path">The path.</param>
        public Bind(string path)
        {
            _binding = new Binding(path);
            BindSecurityAttribute = true;
            BindValidationAttributes = true;
        }

        /// <summary>
        /// The decorated binding class.
        /// </summary>
        private readonly Binding _binding;

        /// <summary>
        /// The decorated binding class.
        /// </summary>
        [Browsable(false)]
        public Binding Binding
        {
            get { return _binding; }
        }

        /// <summary> Opaque data passed to the asynchronous data dispatcher </summary>
        [DefaultValue(null)]
        public object AsyncState
        {
            get { return _binding.AsyncState; }
            set { _binding.AsyncState = value; }
        }

        /// <summary> True if Binding should interpret its path relative to
        /// the data item itself.
        /// </summary>
        /// <remarks>
        /// The normal behavior (when this property is false)
        /// includes special treatment for a data item that implements IDataSource.
        /// In this case, the path is treated relative to the object obtained
        /// from the IDataSource.Data property.  In addition, the binding listens
        /// for the IDataSource.DataChanged event and reacts accordingly.
        /// Setting this property to true overrides this behavior and gives
        /// the binding access to properties on the data source object itself.
        /// </remarks>
        [DefaultValue(false)]
        public bool BindsDirectlyToSource
        {
            get { return _binding.BindsDirectlyToSource; }
            set { _binding.BindsDirectlyToSource = value; }
        }

        /// <summary>
        /// The converter to apply
        /// </summary>
        /// <value>The converter.</value>
        [DefaultValue(null)]
        public IValueConverter Converter
        {
            get { return _binding.Converter; }
            set { _binding.Converter = value; }
        }

        /// <summary>
        /// Value to be used for the target when the bound value is null
        /// </summary>
        /// <value>The target null value.</value>
        [DefaultValue(null)]
        public object TargetNullValue
        {
            get { return _binding.TargetNullValue; }
            set { _binding.TargetNullValue = value; }
        }

        /// <summary>
        /// The converter culture to apply
        /// </summary>
        /// <value>The converter culture.</value>
        [TypeConverter(typeof (CultureInfoIetfLanguageTagConverter)), DefaultValue(null)]
        public CultureInfo ConverterCulture
        {
            get { return _binding.ConverterCulture; }
            set { _binding.ConverterCulture = value; }
        }

        /// <summary>
        /// Gets or sets the converter parameter.
        /// </summary>
        /// <value>The converter parameter.</value>
        [DefaultValue(null)]
        public object ConverterParameter
        {
            get { return _binding.ConverterParameter; }
            set { _binding.ConverterParameter = value; }
        }

        /// <summary>
        /// Name of the element to use as the source
        /// </summary>
        /// <value>The name of the element.</value>
        [DefaultValue(null)]
        public string ElementName
        {
            get { return _binding.ElementName; }
            set { _binding.ElementName = value; }
        }

        /// <summary>
        /// Fallback value to apply when no binding value can be determined
        /// </summary>
        /// <value>The fallback value.</value>
        [DefaultValue(null)]
        public object FallbackValue
        {
            get { return _binding.FallbackValue; }
            set { _binding.FallbackValue = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is asynchronous.
        /// </summary>
        /// <value><c>true</c> if this instance is asynchronous; otherwise, <c>false</c>.</value>
        [DefaultValue(false)]
        public bool IsAsync
        {
            get { return _binding.IsAsync; }
            set { _binding.IsAsync = value; }
        }

        /// <summary>
        /// Binding Mode
        /// </summary>
        /// <value>The mode.</value>
        [DefaultValue(BindingMode.Default)]
        public BindingMode Mode
        {
            get { return _binding.Mode; }
            set { _binding.Mode = value; }
        }

        /// <summary>
        /// Raise SourceUpdated event whenever a value flows from target to source
        /// </summary>
        /// <value><c>true</c> if [notify on source updated]; otherwise, <c>false</c>.</value>
        [DefaultValue(false)]
        public bool NotifyOnSourceUpdated
        {
            get { return _binding.NotifyOnSourceUpdated; }
            set { _binding.NotifyOnSourceUpdated = value; }
        }

        /// <summary>
        /// Raise TargetUpdated event whenever a value flows from source to target
        /// </summary>
        /// <value><c>true</c> if [notify on target updated]; otherwise, <c>false</c>.</value>
        [DefaultValue(false)]
        public bool NotifyOnTargetUpdated
        {
            get { return _binding.NotifyOnTargetUpdated; }
            set { _binding.NotifyOnTargetUpdated = value; }
        }

        /// <summary>
        /// Raise ValidationError event whenever there is a ValidationError on Update
        /// </summary>
        /// <value><c>true</c> if [notify on validation error]; otherwise, <c>false</c>.</value>
        [DefaultValue(false)]
        public bool NotifyOnValidationError
        {
            get { return _binding.NotifyOnValidationError; }
            set { _binding.NotifyOnValidationError = value; }
        }

        /// <summary>
        /// The source path (for CLR bindings).
        /// </summary>
        /// <value>The path.</value>
        [DefaultValue(null)]
        public PropertyPath Path
        {
            get { return _binding.Path; }
            set { _binding.Path = value; }
        }

        /// <summary>
        /// Gets or sets the relative source.
        /// </summary>
        /// <value>The relative source.</value>
        [DefaultValue(null)]
        public RelativeSource RelativeSource
        {
            get { return _binding.RelativeSource; }
            set { _binding.RelativeSource = value; }
        }

        /// <summary> object to use as the source </summary>
        /// <remarks> To clear this property, set it to DependencyProperty.UnsetValue. </remarks>
        [DefaultValue(null)]
        public object Source
        {
            get { return _binding.Source; }
            set { _binding.Source = value; }
        }

        /// <summary>
        /// called whenever any exception is encountered when trying to update
        /// the value to the source. The application author can provide its own
        /// handler for handling exceptions here. If the delegate returns
        ///     null - don't throw an error or provide a ValidationError.
        ///     Exception - returns the exception itself, we will fire the exception using Async exception model.
        ///     ValidationError - it will set itself as the BindingInError and add it to the element's Validation errors.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public UpdateSourceExceptionFilterCallback UpdateSourceExceptionFilter
        {
            get { return _binding.UpdateSourceExceptionFilter; }
            set { _binding.UpdateSourceExceptionFilter = value; }
        }

        /// <summary>
        /// Update type
        /// </summary>
        /// <value>The update source trigger.</value>
        [DefaultValue(UpdateSourceTrigger.Default)]
        public UpdateSourceTrigger UpdateSourceTrigger
        {
            get { return _binding.UpdateSourceTrigger; }
            set { _binding.UpdateSourceTrigger = value; }
        }

        /// <summary>
        /// True if a data error in the source item should be considered a validation error.
        /// </summary>
        /// <value><c>true</c> if [validates on data errors]; otherwise, <c>false</c>.</value>
        [DefaultValue(false)]
        public bool ValidatesOnDataErrors
        {
            get { return _binding.ValidatesOnDataErrors; }
            set { _binding.ValidatesOnDataErrors = value; }
        }

        /// <summary>
        /// True if an exception during source updates should be considered a validation error.
        /// </summary>
        /// <value><c>true</c> if [validates on exceptions]; otherwise, <c>false</c>.</value>
        [DefaultValue(false)]
        public bool ValidatesOnExceptions
        {
            get { return _binding.ValidatesOnExceptions; }
            set { _binding.ValidatesOnExceptions = value; }
        }

        /// <summary>
        /// The XPath path (for XML bindings).
        /// </summary>
        /// <value>The x path.</value>
        [DefaultValue(null)]
        public string XPath
        {
            get { return _binding.XPath; }
            set { _binding.XPath = value; }
        }

        /// <summary>
        ///     Collection&lt;ValidationRule&gt; is a collection of ValidationRule
        ///     implementations on either a Binding or a MultiBinding.  Each of the rules
        ///     is run by the binding engine when validation on update to source
        /// </summary>
        [DefaultValue(null)]
        public Collection<ValidationRule> ValidationRules
        {
            get { return _binding.ValidationRules; }
        }

        /// <summary>
        /// Gets or sets the string format.
        /// </summary>
        /// <value>The string format.</value>
        [DefaultValue(null)]
        public string StringFormat
        {
            get { return _binding.StringFormat; }
            set { _binding.StringFormat = value; }
        }

        /// <summary>
        /// Gets or sets the name of the binding group.
        /// </summary>
        /// <value>The name of the binding group.</value>
        [DefaultValue("")]
        public string BindingGroupName
        {
            get { return _binding.BindingGroupName; }
            set { _binding.BindingGroupName = value; }
        }

        /// <summary>
        /// Defines whether the security attribute on the binding source should automatically be bound 
        /// to the CODE Framework security system for UI security.
        /// </summary>
        /// <value><c>true</c> if the security system is to automatically kick in (default)</value>
        [DefaultValue(true)]
        public bool BindSecurityAttribute { get; set; }

        /// <summary>
        /// Defines whether validation attributes should be automatically bound to the CODE Framework validation system
        /// </summary>
        /// <value><c>true</c> if the validation system is to automatically kick in (default)</value>
        [DefaultValue(true)]
        public bool BindValidationAttributes { get; set; }

        /// <summary>
        /// This basic implementation just sets a binding on the targeted
        /// <see cref="DependencyObject"/> and returns the appropriate
        /// <see cref="BindingExpressionBase"/> instance.<br/>
        /// All this work is delegated to the decorated <see cref="Binding"/>
        /// instance.
        /// </summary>
        /// <returns>
        /// The object value to set on the property where the extension is applied. 
        /// In case of a valid binding expression, this is a <see cref="BindingExpressionBase"/>
        /// instance.
        /// </returns>
        /// <param name="provider">Object that can provide services for the markup
        /// extension.</param>
        public override object ProvideValue(IServiceProvider provider)
        {
            var valueProvider = provider as IProvideValueTarget;
            if (valueProvider != null)
            {
                var element = valueProvider.TargetObject as FrameworkElement;
                if (element != null)
                {
                    // Automatically hook up CODE Framework security if applicable
                    if (BindSecurityAttribute)
                        if (element.DataContext == null)
                            // We do not have a source yet, but if the data context ever changes, we can trigger another update
                            element.DataContextChanged += (s, e) => { UpdateSecurity(element); };
                        else
                            UpdateSecurity(element);

                    // Automatically hook up CODE Framework validation if applicable
                    if (BindValidationAttributes)
                        if (element.DataContext == null)
                            // We do not have a source yet, but if the data context ever changes, we can trigger another update
                            element.DataContextChanged += (s, e) => { UpdateValidation(element); };
                        else
                            UpdateValidation(element);
                }
            }

            // Invoke standard binding behavior
            return _binding.ProvideValue(provider);
        }

        private void UpdateValidation(DependencyObject targetObject)
        {
            var validationBinding = new AttributeValidationBinding(_binding.Path.Path);
            var attributes = validationBinding.GetAttributes(targetObject, InputValidation.ValidationAttributesProperty);
            if (attributes != null)
                InputValidation.SetValidationAttributes(targetObject, attributes);
        }

        private void UpdateSecurity(DependencyObject targetObject)
        {
            var readOnlyRolesBinding = new AttributePropertyBinding(_binding.Path.Path + "[Security.ReadOnlyRoles]");
            var readOnlyRolesValue = readOnlyRolesBinding.GetAttributeProperty(targetObject, Security.Security.ReadOnlyRolesProperty);
            if (readOnlyRolesValue != null && readOnlyRolesValue is string)
                Security.Security.SetReadOnlyRoles(targetObject, (string)readOnlyRolesValue);

            var fullAccessBinding = new AttributePropertyBinding(_binding.Path.Path + "[Security.FullAccessRoles]");
            var fullAccessRolesValue = fullAccessBinding.GetAttributeProperty(targetObject, Security.Security.FullAccessRolesProperty);
            if (fullAccessRolesValue != null && fullAccessRolesValue is string)
                Security.Security.SetFullAccessRoles(targetObject, (string)fullAccessRolesValue);
        }

        /// <summary>
        /// Validates a service provider that was submitted to the <see cref="ProvideValue"/>
        /// method. This method checks whether the provider is null (happens at design time),
        /// whether it provides an <see cref="IProvideValueTarget"/> service, and whether
        /// the service's <see cref="IProvideValueTarget.TargetObject"/> and
        /// <see cref="IProvideValueTarget.TargetProperty"/> properties are valid
        /// <see cref="DependencyObject"/> and <see cref="DependencyProperty"/>
        /// instances.
        /// </summary>
        /// <param name="provider">The provider to be validated.</param>
        /// <param name="target">The binding target of the binding.</param>
        /// <param name="dp">The target property of the binding.</param>
        /// <returns>True if the provider supports all that's needed.</returns>
        protected virtual bool TryGetTargetItems(IServiceProvider provider, out DependencyObject target, out DependencyProperty dp)
        {
            target = null;
            dp = null;
            if (provider == null) return false;

            //create a binding and assign it to the target
            var service = (IProvideValueTarget) provider.GetService(typeof (IProvideValueTarget));
            if (service == null) return false;

            //we need dependency objects / properties
            target = service.TargetObject as DependencyObject;
            dp = service.TargetProperty as DependencyProperty;
            return target != null && dp != null;
        }
    }
}
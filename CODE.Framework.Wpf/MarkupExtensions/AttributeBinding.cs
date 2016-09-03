using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using CODE.Framework.Core.Utilities;

namespace CODE.Framework.Wpf.MarkupExtensions
{
    /// <summary>
    /// This class allows binding members of XAML elements to attributes set on members
    /// </summary>
    /// <seealso cref="System.Windows.Markup.MarkupExtension" />
    public class AttributeBinding : MarkupExtension
    {
        /// <summary>
        /// The path to the member the attribute is to be found on. The path is relative to the DataContext of the target element.
        /// </summary>
        /// <value>The path.</value>
        /// <example>
        /// The following example sets the IsEnabled property to true, if the FirstName property on the current
        /// DataContext has an assigned 'Enabled' attribute.
        /// 
        /// &lt;TextBox IsEnabled=&quot;{a:AttributeBinding Path=FirstName, AttributeName=Enabled, Mode=IsAttributeSet}&quot; /&gt;
        /// </example>
        public string Path { get; set; }

        /// <summary>
        /// The binding source. Defines the source object the binding targets relative to the current DataContext.
        /// If not set, the current DataContext is assumed.
        /// </summary>
        /// <value>The binding source.</value>
        /// <remarks>
        /// When set in XAML, the source usually is set in the form of resources and a complex binding expression. 
        /// In most cases, a source does not have to be explicitly specified.
        /// </remarks>
        public object Source { get; set; }

        /// <summary>
        /// Name of the attribute on the bound member (Path) that is to be bound to
        /// </summary>
        /// <value>The name of the attribute.</value>
        /// <example>
        /// The following example sets the IsEnabled property to true, if the FirstName property on the current
        /// DataContext has an assigned 'Enabled' attribute.
        /// 
        /// &lt;TextBox IsEnabled=&quot;{a:AttributeBinding Path=FirstName, Name=Enabled, Mode=IsAttributeSet}&quot; /&gt;
        /// </example>
        public string Name { get; set; }

        /// <summary>
        /// Name of the attribute's property to bind to when Mode=AttributeProperty
        /// </summary>
        /// <value>The name of the property.</value>
        public string PropertyName { get; set; }

        /// <summary>
        /// Defines whether attributes that are set due to being inherited should be considered (true, default) or ignored (false)
        /// </summary>
        /// <value>True if inherited attributes are included</value>
        [DefaultValue(true)]
        public bool IncludeInheritedAttributes { get; set; }

        /// <summary>
        /// The binding mode. This specifies what the binding does and how it behaves:
        /// Attribute: Returns the actual attribute
        /// Attributes: Returns all attributes of that type, or attributes that are derived from that type
        /// AttributeSet: Returns a true as the bound value, if the attribute does indeed exist.
        /// </summary>
        /// <value>The attribute binding mode.</value>
        [DefaultValue(AttributeBindingMode.Attribute)]
        public AttributeBindingMode Mode { get; set; }

        /// <summary>
        /// The converter to apply
        /// </summary>
        /// <value>The converter.</value>
        public IValueConverter Converter { get; set; }

        /// <summary>
        /// The parameter to pass to the converter
        /// </summary>
        /// <value>The converter parameter.</value>
        public object ConverterParameter { get; set; }

        /// <summary>
        /// Fall-back value to be used when the binding doesn't succeed
        /// </summary>
        /// <value>The fallback value.</value>
        public object FallbackValue { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AttributeBinding"/> class.
        /// </summary>
        public AttributeBinding()
        {
            IncludeInheritedAttributes = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AttributeBinding" /> class.
        /// </summary>
        /// <param name="path">The property path (and optionally the attribute name in brackets)</param>
        /// <example>
        /// &lt;TextBox IsEnabled=&quot;{a:AttributeBinding FirstName, Name=Enabled, Mode=IsAttributeSet}&quot; /&gt;
        /// &lt;TextBox IsEnabled=&quot;{a:AttributeBinding FirstName[Enabled], Mode=IsAttributeSet}&quot; /&gt;
        /// </example>
        public AttributeBinding(string path) : this()
        {
            var indexOfOpenBracket = path.IndexOf('[');
            if (indexOfOpenBracket > -1)
            {
                // We support passing in the attribute name in square brackets
                var name = path.Substring(indexOfOpenBracket + 1);
                var indexOfCloseBracket = name.IndexOf(']');
                if (indexOfCloseBracket > -1)
                    name = name.Substring(0, indexOfCloseBracket);
                if (name.IndexOf('.') > -1)
                {
                    // This also defines the property on the attribute
                    Mode = AttributeBindingMode.AttributeProperty;
                    var nameParts = name.Split('.');
                    Name = nameParts[0];
                    PropertyName = nameParts[1];
                }
                else
                    Name = name;
                Path = path.Substring(0, indexOfOpenBracket);
            }
            else
                Path = path;
        }

        /// <summary>
        /// Provides the value.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        /// <returns>System.Object.</returns>
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            var valueProvider = serviceProvider as IProvideValueTarget;
            if (valueProvider == null) return null;

            var returnValue = FallbackValue;

            switch (Mode)
            {
                case AttributeBindingMode.Attribute:
                    returnValue = GetAttribute(valueProvider.TargetObject, valueProvider.TargetProperty);
                    break;
                case AttributeBindingMode.Attributes:
                    returnValue = GetAttributes(valueProvider.TargetObject, valueProvider.TargetProperty);
                    break;
                case AttributeBindingMode.IsAttributeSet:
                    returnValue = IsAttributeSet(valueProvider.TargetObject, valueProvider.TargetProperty);
                    break;
                case AttributeBindingMode.AttributeProperty:
                    returnValue = GetAttributeProperty(valueProvider.TargetObject, valueProvider.TargetProperty);
                    break;
            }

            if (Converter != null)
                returnValue = Converter.Convert(returnValue, valueProvider.TargetProperty.GetType(), ConverterParameter, CultureInfo.CurrentUICulture);

            return returnValue;
        }

        /// <summary>
        /// Executes the binding for Mode=Attribute
        /// </summary>
        /// <param name="targetObject">The target object.</param>
        /// <param name="targetProperty">The target property.</param>
        /// <returns>The actual attribute or null if not found</returns>
        public Attribute GetAttribute(object targetObject, object targetProperty)
        {
            var source = Source;

            if (source == null)
            {
                var element = targetObject as FrameworkElement;
                if (element != null)
                {
                    // We do not have a source yet, but if the data context ever changes, we can trigger another update
                    var targetObject2 = (DependencyObject) targetObject;
                    var targetProperty2 = targetProperty as DependencyProperty;
                    if (element.DataContext == null)
                    {
                        element.DataContextChanged += (s, e) =>
                        {
                            if (element.DataContext == null || targetProperty2 == null) return;
                            targetObject2.SetValue(targetProperty2, GetAttribute(targetObject2, targetProperty2));
                        };
                        return FallbackValue as Attribute; // The binding can not yet work, so we abandon for now. Once the data context changes, the binding will be re-done
                    }
                    source = element.DataContext;
                }
            }

            if (source == null)
                throw new ArgumentNullException("Unable to find binding source. Source must not be null. Source: '" + Source + "'. Path: '" + Path + "'");

            object parentObject;
            var propertyInfo = ObjectHelper.GetPropertyByPath(source, Path, out parentObject);
            if (propertyInfo == null) return FallbackValue as Attribute;

            var attributes = propertyInfo.GetCustomAttributes(IncludeInheritedAttributes);
            var attribute = attributes.FirstOrDefault(a =>
            {
                var attributeType = a.GetType();
                return attributeType.Name == Name || attributeType.Name == Name + "Attribute";
            });
            return attribute as Attribute;
        }

        /// <summary>
        /// Executes the binding for Mode=Attribute
        /// </summary>
        /// <param name="targetObject">The target object.</param>
        /// <param name="targetProperty">The target property.</param>
        /// <returns>The actual attribute or null if not found</returns>
        public object GetAttributeProperty(object targetObject, object targetProperty)
        {
            var source = Source;

            if (source == null)
            {
                var element = targetObject as FrameworkElement;
                if (element != null)
                {
                    // We do not have a source yet, but if the data context ever changes, we can trigger another update
                    var targetObject2 = (DependencyObject) targetObject;
                    var targetProperty2 = targetProperty as DependencyProperty;
                    if (element.DataContext == null)
                    {
                        element.DataContextChanged += (s, e) =>
                        {
                            if (element.DataContext == null || targetProperty2 == null) return;
                            targetObject2.SetValue(targetProperty2, GetAttributeProperty(targetObject2, targetProperty2));
                        };
                        return GetFallbackValueForTargetProperty(targetProperty); // The binding can not yet work, so we abandon for now. Once the data context changes, the binding will be re-done
                    }
                    source = element.DataContext;
                }
            }

            if (source == null)
                throw new ArgumentNullException("Unable to find binding source. Source must not be null. Source: '" + Source + "'. Path: '" + Path + "'");

            object parentObject;
            var propertyInfo = ObjectHelper.GetPropertyByPath(source, Path, out parentObject);
            if (propertyInfo == null) return GetFallbackValueForTargetProperty(targetProperty);

            var attributes = propertyInfo.GetCustomAttributes(IncludeInheritedAttributes);
            var attribute = attributes.FirstOrDefault(a =>
            {
                var attributeType = a.GetType();
                return attributeType.Name == Name || attributeType.Name == Name + "Attribute";
            });

            if (attribute == null) return GetFallbackValueForTargetProperty(targetProperty);
            var propertyValue = attribute.GetPropertyValue<object>(PropertyName);
            return propertyValue;
        }

        private object GetFallbackValueForTargetProperty(object targetProperty)
        {
            var property = targetProperty as DependencyProperty;
            if (property == null) return FallbackValue;

            if (property.PropertyType == typeof (int))
            {
                if (FallbackValue is int) return (int) FallbackValue;
                var outInt = 0;
                if (int.TryParse(FallbackValue.ToString(), out outInt)) return outInt;
            }
            if (property.PropertyType == typeof(double))
            {
                if (FallbackValue is double) return (double)FallbackValue;
                var outDouble = 0d;
                if (double.TryParse(FallbackValue.ToString(), out outDouble)) return outDouble;
            }
            if (property.PropertyType == typeof(decimal))
            {
                if (FallbackValue is decimal) return (decimal)FallbackValue;
                var outDecimal = 0m;
                if (decimal.TryParse(FallbackValue.ToString(), out outDecimal)) return outDecimal;
            }
            if (property.PropertyType == typeof(bool))
            {
                if (FallbackValue is bool) return (bool)FallbackValue;
                return (FallbackValue.ToString().ToLower().Trim() == "true");
            }

            return FallbackValue;
        }

        /// <summary>
        /// Executes the binding for Mode=Attributes
        /// </summary>
        /// <param name="targetObject">The target object.</param>
        /// <param name="targetProperty">The target property.</param>
        /// <returns>An enumerable collection containing all the attributes that match the criteria (collection is empty if no matches are found)</returns>
        public List<Attribute> GetAttributes(object targetObject, object targetProperty)
        {
            var source = Source;

            if (source == null)
            {
                var element = targetObject as FrameworkElement;
                if (element != null)
                {
                    // We do not have a source yet, but if the data context ever changes, we can trigger another update
                    var targetObject2 = (DependencyObject)targetObject;
                    var targetProperty2 = targetProperty as DependencyProperty;
                    element.DataContextChanged += (s, e) =>
                    {
                        if (element.DataContext == null || targetProperty2 == null) return;
                        targetObject2.SetValue(targetProperty2, GetAttributes(targetObject2, targetProperty2));
                    };
                    if (element.DataContext == null) return new List<Attribute>(); // The binding can not yet work, so we abandon for now. Once the data context changes, the binding will be re-done
                    source = element.DataContext;
                }
            }

            if (source == null)
                throw new ArgumentNullException("Unable to find binding source. Source must not be null. Source: '" + Source + "'. Path: '" + Path + "'");

            object parentObject;
            var propertyInfo = ObjectHelper.GetPropertyByPath(source, Path, out parentObject);
            if (propertyInfo == null) return new List<Attribute>();

            var attributes = propertyInfo.GetCustomAttributes(IncludeInheritedAttributes);
            var matchingAttributes = attributes.OfType<Attribute>().Where(a =>
            {
                var attributeType = a.GetType();
                var directMatch = (attributeType.Name == Name || attributeType.Name == Name + "Attribute");
                if (directMatch) return true;

                if (attributeType.BaseType != null)
                {
                    if (attributeType.BaseType.Name == Name || attributeType.BaseType.Name == Name + "Attribute")
                        return true;
                    if (attributeType.BaseType.BaseType != null)
                    {
                        if (attributeType.BaseType.BaseType.Name == Name || attributeType.BaseType.BaseType.Name == Name + "Attribute")
                            return true;
                        if (attributeType.BaseType.BaseType.BaseType != null)
                            if (attributeType.BaseType.BaseType.BaseType.Name == Name || attributeType.BaseType.BaseType.BaseType.Name == Name + "Attribute")
                                return true;
                    }
                }
                return false;
            });
            return matchingAttributes.ToList();
        }

        /// <summary>
        /// Executes the binding for Mode=IsAttributeSet
        /// </summary>
        /// <param name="targetObject">The target object.</param>
        /// <param name="targetProperty">The target property.</param>
        /// <returns>True if the attribute has indeed been set on the target</returns>
        public bool IsAttributeSet(object targetObject, object targetProperty)
        {
            var source = Source;

            if (source == null)
            {
                var element = targetObject as FrameworkElement;
                if (element != null)
                {
                    // We do not have a source yet, but if the data context ever changes, we can trigger another update
                    var targetObject2 = (DependencyObject) targetObject;
                    var targetProperty2 = targetProperty as DependencyProperty;
                    element.DataContextChanged += (s, e) =>
                    {
                        if (element.DataContext == null || targetProperty2 == null) return;
                        targetObject2.SetValue(targetProperty2, IsAttributeSet(targetObject2, targetProperty2));
                    };
                    if (element.DataContext == null) return false; // The binding can not yet work, so we abandon for now. Once the data context changes, the binding will be re-done
                    source = element.DataContext;
                }
            }

            if (source == null)
                throw new ArgumentNullException("Unable to find binding source. Source must not be null. Source: '" + Source + "'. Path: '" + Path + "'");

            object parentObject;
            var propertyInfo = ObjectHelper.GetPropertyByPath(source, Path, out parentObject);
            if (propertyInfo == null) return false;

            var attributes = propertyInfo.GetCustomAttributes(IncludeInheritedAttributes);
            var attribute = attributes.FirstOrDefault(a =>
            {
                var attributeType = a.GetType();
                return attributeType.Name == Name || attributeType.Name == Name + "Attribute";
            });
            return (attribute != null);
        }
    }

    /// <summary>
    /// Special variation on the attribute binding, with Mode=IsAttributeSet
    /// </summary>
    /// <seealso cref="CODE.Framework.Wpf.MarkupExtensions.AttributeBinding" />
    public class AttributeSet : AttributeBinding
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AttributeSet"/> class.
        /// </summary>
        public AttributeSet() : base()
        {
            Mode = AttributeBindingMode.IsAttributeSet;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="AttributeSet" /> class.
        /// </summary>
        /// <param name="path">The property path (and optionally the attribute name in brackets)</param>
        /// <example>
        /// &lt;TextBox IsEnabled="{a:AttributeBinding FirstName, Name=Enabled, Mode=IsAttributeSet}" /&gt;
        /// &lt;TextBox IsEnabled="{a:AttributeBinding FirstName[Enabled], Mode=IsAttributeSet}" /&gt;
        /// </example>
        public AttributeSet(string path) : base(path)
        {
            Mode = AttributeBindingMode.IsAttributeSet;
        }
    }

    /// <summary>
    /// Special version of an attribute binding that defaults to list mode
    /// </summary>
    /// <seealso cref="CODE.Framework.Wpf.MarkupExtensions.AttributeBinding" />
    public class AttributeListBinding : AttributeBinding
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AttributeListBinding"/> class.
        /// </summary>
        public AttributeListBinding() : base()
        {
            Mode = AttributeBindingMode.Attributes;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="AttributeListBinding"/> class.
        /// </summary>
        /// <param name="path">The path.</param>
        public AttributeListBinding(string path) : base(path)
        {
            Mode = AttributeBindingMode.Attributes;
        }
    }

    /// <summary>
    /// Special attribute binding that binds to all validation attributes
    /// </summary>
    /// <seealso cref="CODE.Framework.Wpf.MarkupExtensions.AttributeListBinding" />
    public class AttributeValidationBinding : AttributeListBinding
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AttributeValidationBinding"/> class.
        /// </summary>
        public AttributeValidationBinding() : base()
        {
            Name = "Validation";
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="AttributeValidationBinding"/> class.
        /// </summary>
        /// <param name="path">The path.</param>
        public AttributeValidationBinding(string path) : base(path)
        {
            Name = "Validation";
        }
    }

    /// <summary>
    /// Special attribute binding class that binds to a property on an attribute by default
    /// </summary>
    /// <seealso cref="CODE.Framework.Wpf.MarkupExtensions.AttributeBinding" />
    public class AttributePropertyBinding : AttributeBinding
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AttributePropertyBinding"/> class.
        /// </summary>
        public AttributePropertyBinding() : base()
        {
            Mode = AttributeBindingMode.AttributeProperty;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="AttributePropertyBinding"/> class.
        /// </summary>
        /// <param name="path">The property path (and optionally the attribute name in brackets)</param>
        public AttributePropertyBinding(string path) : base(path)
        {
            Mode = AttributeBindingMode.AttributeProperty;
        }
    }

    /// <summary>
    /// Attribute binding mode enumeration
    /// </summary>
    public enum AttributeBindingMode
    {
        /// <summary>
        /// Returns the actual attribute as the binding value
        /// </summary>
        Attribute,
        /// <summary>
        /// Returns an IEnumerable of Attribute containing all the attributes that match the name, or derive from it.
        /// </summary>
        Attributes,
        /// <summary>
        /// This mode checks if an attribute has been set and returns true if it has been found
        /// </summary>
        IsAttributeSet,
        /// <summary>
        /// This mode binds to the value of a specific property of an attribute
        /// </summary>
        AttributeProperty
    }
}
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Markup;

namespace CODE.Framework.Wpf.Documents
{
    /// <summary>
    /// Data template created for easy use in data bound documents
    /// </summary>
    [ContentProperty("DocumentVisualTree")]
    public class DocumentDataTemplate : DataTemplate
    {
        /// <summary>
        /// Gets or sets the document visual tree.
        /// </summary>
        /// <value>The document visual tree.</value>
        public FrameworkContentElement DocumentVisualTree { get; set; }

        /// <summary>
        /// Loads the document template.
        /// </summary>
        /// <returns>DependencyObject.</returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public DependencyObject LoadDocumentTemplate()
        {
            if (DocumentVisualTree == null) return null;

            var root = DocumentVisualTree;
            var expressions = new List<BindingToSet>();
            var newInstance = CloneObject(root, null, expressions);
            newInstance.SetValue(DocEx.ForceRefreshExpressionProperty, expressions);

            var fragment = new DocumentFragment {Content = newInstance as FrameworkContentElement};
            return fragment;
        }

        private static DependencyObject CloneObject(DependencyObject objectToClone, object container, List<BindingToSet> expressions)
        {
            var cloneType = objectToClone.GetType();
            var newInstance = Activator.CreateInstance(cloneType) as FrameworkContentElement;
            if (newInstance == null) return null;

            if (container != null) AddElementToContainer(container, newInstance);

            // Cloning all regular properties
            var properties = cloneType.GetProperties();
            foreach (var property in properties)
                if (property.CanWrite && property.CanRead)
                {
                    var originalValue = property.GetValue(objectToClone, null);
                    property.SetValue(newInstance, originalValue, null);
                }

            // Cloning all dependency properties
            var dependencyPropertyFields = cloneType.GetFields(BindingFlags.Static | BindingFlags.Public);
            foreach (var dependencyPropertyField in dependencyPropertyFields)
                if (dependencyPropertyField.FieldType == typeof (DependencyProperty))
                {
                    var originalValue = dependencyPropertyField.GetValue(objectToClone) as DependencyProperty;
                    if (originalValue == null) continue;

                    BindingExpression bindingExpression = null;
                    var frameworkContentElement = objectToClone as FrameworkContentElement;
                    if (frameworkContentElement != null) bindingExpression = frameworkContentElement.GetBindingExpression(originalValue);
                    else
                    {
                        var frameworkElement = objectToClone as FrameworkElement;
                        if (frameworkElement != null) bindingExpression = frameworkElement.GetBindingExpression(originalValue);
                    }
                    if (bindingExpression != null)
                    {
                        var newBinding = new Binding(bindingExpression.ParentBinding.Path.Path) {Source = bindingExpression.ParentBinding.Source};
                        if (newBinding.Source == null && bindingExpression.ParentBinding.RelativeSource != null)
                            newBinding.RelativeSource = new RelativeSource
                                {
                                    Mode = bindingExpression.ParentBinding.RelativeSource.Mode,
                                    AncestorType = bindingExpression.ParentBinding.RelativeSource.AncestorType,
                                    AncestorLevel = bindingExpression.ParentBinding.RelativeSource.AncestorLevel
                                };
                        newBinding.Converter = bindingExpression.ParentBinding.Converter;
                        newBinding.ConverterParameter = bindingExpression.ParentBinding.ConverterParameter;
                        if (newBinding.Source == null && !string.IsNullOrEmpty(bindingExpression.ParentBinding.ElementName))
                            newBinding.ElementName = bindingExpression.ParentBinding.ElementName;
                        newBinding.Mode = bindingExpression.ParentBinding.Mode;

                        var newDependencyPropertyField = cloneType.GetField(dependencyPropertyField.Name, BindingFlags.Static | BindingFlags.Public);
                        if (newDependencyPropertyField != null)
                        {
                            var newDependencyProperty = newDependencyPropertyField.GetValue(newInstance) as DependencyProperty;
                            if (newDependencyProperty != null)
                                expressions.Add(new BindingToSet {Binding = newBinding, Property = newDependencyProperty, DependencyObject = newInstance});
                        }
                    }
                    else
                        newInstance.SetValue(originalValue, objectToClone.GetValue(originalValue));
                }

            var children = GetChildElements(objectToClone);
            foreach (var child in children)
                CloneObject(child, newInstance, expressions);

            return newInstance;
        }

        /// <summary>
        /// Gets the child elements.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <returns>IEnumerable{DependencyObject}.</returns>
        private static IEnumerable<DependencyObject> GetChildElements(DependencyObject parent)
        {
            var children = new List<DependencyObject>();

            var section = parent as Section;
            if (section != null)
            {
                children.AddRange(section.Blocks);
                return children;
            }

            var paragraph = parent as Paragraph;
            if (paragraph != null)
            {
                children.AddRange(paragraph.Inlines);
                return children;
            }

            var items  = parent as ItemsControl;
            if (items != null)
            {
                children.AddRange(from object item in items.Items select item as DependencyObject);
                return children;
            }

            var panel = parent as Panel;
            if (panel != null)
            {
                children.AddRange(from object child in panel.Children select child as DependencyObject);
                return children;
            }

            return children;
        }

        private static void AddElementToContainer(object container, object newInstance)
        {
            var section = container as Section;
            var newInstanceBlock = newInstance as Block;
            if (section != null && newInstanceBlock != null) section.Blocks.Add(newInstanceBlock);
            else
            {
                var paragraph = container as Paragraph;
                var newInstanceInline = newInstance as Inline;
                if (paragraph != null && newInstanceInline != null) paragraph.Inlines.Add(newInstanceInline);
                else
                {
                    var items = container as ItemsControl;
                    if (items != null) items.Items.Add(newInstance);
                    else
                    {
                        var panel = container as Panel;
                        var uiElement = newInstance as UIElement;
                        if (panel != null && uiElement != null) panel.Children.Add(uiElement);
                        else
                        {
                            var content = container as ContentControl;
                            if (content != null) content.Content = newInstance;
                        }
                    }
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Data;
using CODE.Framework.Core.Utilities;

namespace CODE.Framework.Wpf.Documents
{
    /// <summary>
    /// Various document extension features made available as attached properties
    /// </summary>
    public class DocEx : DependencyObject
    {
        /// <summary>
        /// Indicates whether the object is an items host
        /// </summary>
        private static readonly DependencyProperty IsItemsHostProperty = DependencyProperty.RegisterAttached("IsItemsHost", typeof (bool), typeof (DocEx), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.NotDataBindable, OnIsItemsHostChanged));

        /// <summary>
        /// Gets the is items host.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <returns><c>true</c> or <c>false</c></returns>
        public static bool GetIsItemsHost(DependencyObject target)
        {
            return (bool) target.GetValue(IsItemsHostProperty);
        }

        /// <summary>
        /// Sets the is items host.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="value">if set to <c>true</c> [value].</param>
        public static void SetIsItemsHost(DependencyObject target, bool value)
        {
            target.SetValue(IsItemsHostProperty, value);
        }

        /// <summary>
        /// Called when is items host changes.
        /// </summary>
        /// <param name="d">The dependency object the value is set on.</param>
        /// <param name="e">The <see cref="DependencyPropertyChangedEventArgs" /> instance containing the event data.</param>
        private static void OnIsItemsHostChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!((bool) e.NewValue)) return;
            If.Real<FrameworkContentElement>(d, element =>
                {
                    if (element.IsInitialized)
                        SetItemsHost(element);
                    else
                        element.Initialized += ItemsHostInitialized;
                });
        }

        /// <summary>
        /// The items host property
        /// </summary>
        private static readonly DependencyProperty ItemsHostProperty = DependencyProperty.RegisterAttached("ItemsHost", typeof (FrameworkContentElement), typeof (DocEx), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.NotDataBindable));

        /// <summary>
        /// Sets the items host.
        /// </summary>
        /// <param name="element">The element.</param>
        private static void SetItemsHost(FrameworkContentElement element)
        {
            var parent = element;
            while (parent.Parent != null)
                parent = (FrameworkContentElement) parent.Parent;
            parent.SetValue(ItemsHostProperty, element);
        }

        /// <summary>
        /// Gets the items host.
        /// </summary>
        /// <param name="dp">The dependency property to set.</param>
        /// <returns>FrameworkContentElement.</returns>
        public static FrameworkContentElement GetItemsHost(DependencyObject dp)
        {
            FrameworkContentElement host = null;
            If.Real<FrameworkContentElement>(dp, element => { host = element.GetValue(ItemsHostProperty) as FrameworkContentElement ?? element; });
            return host;
        }

        /// <summary>
        /// Fires when ItemsHost is changed
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private static void ItemsHostInitialized(object sender, EventArgs e)
        {
            var element = (FrameworkContentElement) sender;
            element.Initialized -= ItemsHostInitialized;
            SetItemsHost(element);
        }

        /// <summary>For internal use only</summary>
        public static readonly DependencyProperty ForceRefreshExpressionProperty = DependencyProperty.Register("ForceRefreshExpression", typeof(List<BindingToSet>), typeof(DocEx), new PropertyMetadata(null));
    }

    /// <summary>
    /// For internal use only
    /// </summary>
    public class BindingToSet
    {
        /// <summary>
        /// Gets or sets the binding.
        /// </summary>
        /// <value>The binding.</value>
        public Binding Binding { get; set; }
        /// <summary>
        /// Gets or sets the property.
        /// </summary>
        /// <value>The property.</value>
        public DependencyProperty Property { get; set; }
        /// <summary>
        /// Gets or sets the dependency object.
        /// </summary>
        /// <value>The dependency object.</value>
        public DependencyObject DependencyObject { get; set; }
    }
}
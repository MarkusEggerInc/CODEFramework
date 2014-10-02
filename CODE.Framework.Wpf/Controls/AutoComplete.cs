using System.Collections;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace CODE.Framework.Wpf.Controls
{
    /// <summary>Attachable auto-complete behavior</summary>
    public class AutoComplete : ListBox
    {
        /// <summary>Constructor</summary>
        public AutoComplete()
        {
            MinHeight = 50;
            MinWidth = 100;
            Visibility = Visibility.Collapsed;
            BorderBrush = Brushes.Black;
        }

        /// <summary>Attached property to set the items source for auto complete</summary>
        /// <remarks>This attached property can be attached to any UI Element to define auto-complete behavior</remarks>
        public static readonly DependencyProperty AutoCompleteItemsSourceProperty = DependencyProperty.RegisterAttached("AutoCompleteItemsSource", typeof (IEnumerable), typeof (AutoComplete), new PropertyMetadata(null, AutoCompleteItemsSourceChanged));

        /// <summary>Handler for auto complete item source changes</summary>
        /// <param name="d">Source object</param>
        /// <param name="e">Event arguments</param>
        private static void AutoCompleteItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var element = d as FrameworkElement;
            if (element == null) return;

            var existingUI = GetAutoCompleteUIElement(element);
            existingUI.ItemsSource = e.NewValue as IEnumerable;
        }

        /// <summary>Auto complete items source</summary>
        /// <param name="obj">Object to set the auto complete items source on</param>
        /// <returns>Auto complete items source</returns>
        public static IEnumerable GetAutoCompleteItemsSource(DependencyObject obj)
        {
            return (IEnumerable) obj.GetValue(AutoCompleteItemsSourceProperty);
        }

        /// <summary>Auto complete items source</summary>
        /// <param name="obj">Object to set the auto complete items source on</param>
        /// <param name="value">Value to set</param>
        public static void SetAutoCompleteItemsSource(DependencyObject obj, IEnumerable value)
        {
            obj.SetValue(AutoCompleteItemsSourceProperty, value);
        }

        /// <summary>Attached property to set the item that was picked by auto-complete</summary>
        /// <remarks>This attached property can be attached to any UI Element to define auto-complete behavior</remarks>
        public static readonly DependencyProperty AutoCompleteSelectedItemProperty = DependencyProperty.RegisterAttached("AutoCompleteSelectedItem", typeof (object), typeof (AutoComplete), new PropertyMetadata(null));

        /// <summary>Auto complete selected item</summary>
        /// <param name="obj">Object to set the auto complete selected itemon</param>
        /// <returns>Auto complete selected item</returns>
        public static object GetAutoCompleteSelectedItem(DependencyObject obj)
        {
            return obj.GetValue(AutoCompleteSelectedItemProperty);
        }

        /// <summary>Auto complete selected item</summary>
        /// <param name="obj">Object to set the auto complete selected item on</param>
        /// <param name="value">Value to set</param>
        public static void SetAutoCompleteSelectedItem(DependencyObject obj, object value)
        {
            obj.SetValue(AutoCompleteSelectedItemProperty, value);
        }

        private bool _adornerLoaded;

        /// <summary>
        /// Called when the source of an item in a selector changes.
        /// </summary>
        /// <param name="oldValue">Old value of the source.</param>
        /// <param name="newValue">New value of the source.</param>
        protected override void OnItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue)
        {
            base.OnItemsSourceChanged(oldValue, newValue);

            var observable = newValue as INotifyCollectionChanged;
            if (observable == null) return;
            observable.CollectionChanged += (s, e) =>
                {
                    if (_uiEligibleToBeVisible && Items.Count > 0 && Visibility == Visibility.Collapsed)
                        Visibility = Visibility.Visible;
                };
        }

        /// <summary>Returns an existing reference to the UI used for the drop-down (or creates a new one)</summary>
        /// <param name="attachedTo">Object the UI is to be attached to</param>
        /// <returns>UI element</returns>
        private static AutoComplete GetAutoCompleteUIElement(UIElement attachedTo)
        {
            var dropDownUI = GetAutoCompleteUI(attachedTo);
            if (dropDownUI == null)
            {
                dropDownUI = new AutoComplete();

                attachedTo.GotFocus += (sender, e) =>
                    {
                        var adornerLayer = AdornerLayer.GetAdornerLayer(attachedTo);
                        if (adornerLayer == null) return;
                        var currentAdorners = adornerLayer.GetAdorners(attachedTo);
                        var adornerFound = false;
                        if (currentAdorners != null)
                            if (currentAdorners.OfType<AutoCompleteDropDownAdorner>().Any())
                                adornerFound = true;

                        if (dropDownUI._adornerLoaded && adornerFound) return;
                        var parent = VisualTreeHelper.GetParent(dropDownUI);
                        if (parent != null)
                        {
                            var oldAdorner = parent as AutoCompleteDropDownAdorner;
                            if (oldAdorner != null)
                                oldAdorner.DisconnectVisualChild(dropDownUI);
                        }
                        var adorner = new AutoCompleteDropDownAdorner(attachedTo, dropDownUI);
                        var localAdorner = adorner;
                        adornerLayer.Add(adorner);
                        dropDownUI._adornerLoaded = true;

                        if (dropDownUI.Items.Count > 0) dropDownUI.Visibility = Visibility.Visible;
                        localAdorner.InvalidateMeasure();
                        localAdorner.InvalidateArrange();
                    };
                attachedTo.PreviewKeyDown += (sender, e) =>
                    {
                        switch (e.Key)
                        {
                            case Key.Up:
                            case Key.Down:
                                {
                                    var index = dropDownUI.SelectedIndex;
                                    switch (e.Key)
                                    {
                                        case Key.Down:
                                            index++;
                                            if (index >= dropDownUI.Items.Count)
                                                index = dropDownUI.Items.Count - 1;
                                            break;
                                        case Key.Up:
                                            index--;
                                            if (index < 0)
                                                index = 0;
                                            break;
                                    }
                                    dropDownUI.SelectedIndex = index;
                                    dropDownUI.ScrollIntoView(dropDownUI.SelectedItem);
                                    e.Handled = true;
                                }
                                break;
                            case Key.Enter:
                                {
                                    if (dropDownUI.SelectedIndex > -1)
                                        SetAutoCompleteSelectedItem(attachedTo, dropDownUI.SelectedItem);
                                    dropDownUI.Visibility = Visibility.Collapsed;
                                    dropDownUI._uiEligibleToBeVisible = false;
                                    attachedTo.Focus();
                                    var text = attachedTo as TextBox;
                                    if (text != null)
                                        text.SelectionStart = text.Text.Length;
                                }
                                break;
                        }
                    };
                attachedTo.KeyUp += (sender, e) =>
                    {
                        if (e.Key != Key.Enter && dropDownUI.Visibility == Visibility.Collapsed)
                            if (dropDownUI.Items.Count > 0)
                                dropDownUI.Visibility = Visibility.Visible;
                            else
                                dropDownUI._uiEligibleToBeVisible = true;
                        if (dropDownUI.Visibility == Visibility.Visible && dropDownUI.Items.Count == 0) dropDownUI.Visibility = Visibility.Collapsed;
                    };
                dropDownUI.PreviewKeyDown += (sender, e) =>
                    {
                        if (e.Key != Key.Enter) return;
                        if (dropDownUI.SelectedIndex > -1)
                            SetAutoCompleteSelectedItem(attachedTo, dropDownUI.SelectedItem);
                        dropDownUI.Visibility = Visibility.Collapsed;
                        dropDownUI._uiEligibleToBeVisible = false;
                    };
                dropDownUI.MouseLeftButtonUp += (sender, e) =>
                    {
                        if (dropDownUI.SelectedIndex <= -1) return;
                        SetAutoCompleteSelectedItem(attachedTo, dropDownUI.SelectedItem);
                        dropDownUI.Visibility = Visibility.Collapsed;
                        dropDownUI._uiEligibleToBeVisible = false;
                        e.Handled = true;
                        attachedTo.Focus();
                        var text = attachedTo as TextBox;
                        if (text != null)
                            text.SelectionStart = text.Text.Length;
                    };
                attachedTo.LostFocus += (sender, e) =>
                    {
                        if (!dropDownUI.IsKeyboardFocusWithin && !attachedTo.IsKeyboardFocusWithin)
                        {
                            dropDownUI.Visibility = Visibility.Collapsed;
                            dropDownUI._uiEligibleToBeVisible = false;
                        }
                    };
                dropDownUI.LostFocus += (sender, e) =>
                    {
                        if (!dropDownUI.IsKeyboardFocusWithin && !attachedTo.IsKeyboardFocusWithin)
                        {
                            dropDownUI.Visibility = Visibility.Collapsed;
                            dropDownUI._uiEligibleToBeVisible = false;
                        }
                    };

                SetAutoCompleteUI(attachedTo, dropDownUI);
            }
            return dropDownUI;
        }

        /// <summary>Attached property to set the items source for auto complete</summary>
        /// <remarks>This attached property can be attached to any UI Element to define auto-complete behavior</remarks>
        public static readonly DependencyProperty AutoCompleteDisplayMemberPathProperty = DependencyProperty.RegisterAttached("AutoCompleteDisplayMemberPath", typeof (string), typeof (AutoComplete), new PropertyMetadata("", AutoCompleteDisplayMemberPathChanged));

        /// <summary>Handler for auto complete item source changes</summary>
        /// <param name="d">Source object</param>
        /// <param name="e">Event arguments</param>
        private static void AutoCompleteDisplayMemberPathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var element = d as FrameworkElement;
            if (element == null) return;

            var existingUI = GetAutoCompleteUIElement(element);
            existingUI.DisplayMemberPath = (string) e.NewValue;
        }

        /// <summary>Auto complete items source</summary>
        /// <param name="obj">Object to set the auto complete items source on</param>
        /// <returns>Auto complete items source</returns>
        public static string GetAutoCompleteDisplayMemberPathSource(DependencyObject obj)
        {
            return (string) obj.GetValue(AutoCompleteDisplayMemberPathProperty);
        }

        /// <summary>Auto complete items source</summary>
        /// <param name="obj">Object to set the auto complete items source on</param>
        /// <param name="value">Value to set</param>
        public static void SetAutoCompleteDisplayMemberPath(DependencyObject obj, string value)
        {
            obj.SetValue(AutoCompleteDisplayMemberPathProperty, value);
        }

        /// <summary>Actual UI used by the auto-complete drop down</summary>
        public static readonly DependencyProperty AutoCompleteUIProperty = DependencyProperty.RegisterAttached("AutoCompleteUI", typeof (AutoComplete), typeof (AutoComplete), new PropertyMetadata(null));

        /// <summary>Auto complete UI</summary>
        /// <param name="obj">Object to get the auto complete UI on</param>
        /// <returns>Auto complete UI</returns>
        public static AutoComplete GetAutoCompleteUI(DependencyObject obj)
        {
            return (AutoComplete) obj.GetValue(AutoCompleteUIProperty);
        }

        /// <summary>Auto complete UI</summary>
        /// <param name="obj">Object to set the auto complete UI on</param>
        /// <param name="value">Value to set</param>
        public static void SetAutoCompleteUI(DependencyObject obj, AutoComplete value)
        {
            obj.SetValue(AutoCompleteUIProperty, value);
        }

        /// <summary>Attached property to set the item template for each item in the auto-complete drop down</summary>
        /// <remarks>This attached property can be attached to any UI Element to define auto-complete behavior</remarks>
        public static readonly DependencyProperty AutoCompleteItemTemplateProperty = DependencyProperty.RegisterAttached("AutoCompleteItemTemplate", typeof (DataTemplate), typeof (AutoComplete), new PropertyMetadata(null, AutoCompleteItemTemplateChanged));

        /// <summary>Handler for auto complete item template changes</summary>
        /// <param name="d">Source object</param>
        /// <param name="e">Event arguments</param>
        private static void AutoCompleteItemTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var element = d as FrameworkElement;
            if (element == null) return;

            var existingUI = GetAutoCompleteUIElement(element);
            existingUI.ItemTemplate = e.NewValue as DataTemplate;
        }

        /// <summary>Auto complete item template</summary>
        /// <param name="obj">Object to set the auto complete item template on</param>
        /// <returns>Auto complete item template</returns>
        public static DataTemplate GetAutoCompleteItemTemplate(DependencyObject obj)
        {
            return (DataTemplate) obj.GetValue(AutoCompleteItemTemplateProperty);
        }

        /// <summary>Auto complete item template</summary>
        /// <param name="obj">Object to set the auto complete item template on</param>
        /// <param name="value">Value to set</param>
        public static void SetAutoCompleteItemTemplate(DependencyObject obj, DataTemplate value)
        {
            obj.SetValue(AutoCompleteItemTemplateProperty, value);
        }

        /// <summary>Attached property to set the style for the auto-complete listbox</summary>
        /// <remarks>This attached property can be attached to any UI Element to define auto-complete behavior</remarks>
        public static readonly DependencyProperty AutoCompleteStyleProperty = DependencyProperty.RegisterAttached("AutoCompleteStyle", typeof (Style), typeof (AutoComplete), new PropertyMetadata(null, AutoCompleteStyleChanged));

        /// <summary>Handler for auto complete style changes</summary>
        /// <param name="d">Source object</param>
        /// <param name="e">Event arguments</param>
        private static void AutoCompleteStyleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var element = d as FrameworkElement;
            if (element == null) return;

            var existingUI = GetAutoCompleteUIElement(element);
            existingUI.Style = e.NewValue as Style;
        }

        /// <summary>Auto complete style</summary>
        /// <param name="obj">Object to set the auto complete style on</param>
        /// <returns>Auto complete style</returns>
        public static Style GetAutoCompleteStyle(DependencyObject obj)
        {
            return (Style) obj.GetValue(AutoCompleteStyleProperty);
        }

        /// <summary>Auto complete style</summary>
        /// <param name="obj">Object to set the auto complete style on</param>
        /// <param name="value">Value to set</param>
        public static void SetAutoCompleteStyle(DependencyObject obj, Style value)
        {
            obj.SetValue(AutoCompleteStyleProperty, value);
        }

        /// <summary>
        /// Internal flag indicating whether the UI would theoretically be visible if it had items
        /// </summary>
        private bool _uiEligibleToBeVisible = false;
    }

    /// <summary>Adorner UI for the drop-down part of the auto-complete implementation</summary>
    public class AutoCompleteDropDownAdorner : Adorner
    {
        private readonly AutoComplete _ui;

        /// <summary>Constructor</summary>
        /// <param name="adornedElement">Adorned element (typically a textbox)</param>
        /// <param name="ui">The UI that is to be used in the drop-down</param>
        public AutoCompleteDropDownAdorner(UIElement adornedElement, AutoComplete ui)
            : base(adornedElement)
        {
            _ui = ui;
            AddVisualChild(ui);
        }

        /// <summary>
        /// Disconnects a visual child element
        /// </summary>
        /// <param name="ui"></param>
        public void DisconnectVisualChild(AutoComplete ui)
        {
            RemoveVisualChild(ui);
        }

        /// <summary>
        /// Implements any custom measuring behavior for the adorner.
        /// </summary>
        /// <param name="constraint">A size to constrain the adorner to.</param>
        /// <returns>
        /// A <see cref="T:System.Windows.Size"/> object representing the amount of layout space needed by the adorner.
        /// </returns>
        protected override Size MeasureOverride(Size constraint)
        {
            _ui.Measure(constraint);
            return _ui.DesiredSize;
        }

        /// <summary>
        /// When overridden in a derived class, positions child elements and determines a size for a <see cref="T:System.Windows.FrameworkElement"/> derived class.
        /// </summary>
        /// <param name="finalSize">The final area within the parent that this element should use to arrange itself and its children.</param>
        /// <returns>
        /// The actual size used.
        /// </returns>
        protected override Size ArrangeOverride(Size finalSize)
        {

            var location = new Point(0, 28);
            var size = new Size(_ui.DesiredSize.Width, _ui.DesiredSize.Height);

            var element = AdornedElement as FrameworkElement;
            if (element != null)
            {
                location.Y = element.ActualHeight;
                size.Width = element.ActualWidth;
            }

            //_ui.Arrange(new Rect(location, _ui.DesiredSize));
            _ui.Arrange(new Rect(location, size));
            return finalSize;
        }

        /// <summary>
        /// Gets the number of visual child elements within this element.
        /// </summary>
        /// <returns>The number of visual child elements for this element.</returns>
        protected override int VisualChildrenCount
        {
            get { return 1; }
        }

        /// <summary>
        /// Overrides <see cref="M:System.Windows.Media.Visual.GetVisualChild(System.Int32)"/>, and returns a child at the specified index from a collection of child elements.
        /// </summary>
        /// <param name="index">The zero-based index of the requested child element in the collection.</param>
        /// <returns>
        /// The requested child element. This should not return null; if the provided index is out of range, an exception is thrown.
        /// </returns>
        protected override Visual GetVisualChild(int index)
        {
            return _ui;
        }
    }
}

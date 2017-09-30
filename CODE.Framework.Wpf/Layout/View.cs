using System;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using CODE.Framework.Wpf.Interfaces;

namespace CODE.Framework.Wpf.Layout
{
    /// <summary>
    ///     Base class for views (can be used instead of UserControl)
    /// </summary>
    public class SimpleView : ItemsControl, IUserInterfaceActivation
    {
        private static bool _templateSet;

        /// <summary>
        ///     Attached property to set an element's logical width ("1" usually represents the width of about 1 character,
        ///     but exact dimensions are up to the current style)
        /// </summary>
        /// <remarks>
        ///     This attached property can be attached to any UI Element to define an element's logical width. The width is
        ///     typically calculated on the current font's width based on the letter 'N'. So a width of 10 can typically accomodate
        ///     10 'N' letters. Note that this may vary with different skins.
        /// </remarks>
        public static readonly DependencyProperty WidthExProperty = DependencyProperty.RegisterAttached("WidthEx", typeof (double), typeof (SimpleView), new PropertyMetadata(double.NaN, OnWidthExChanged));

        /// <summary>
        ///     Attached property to set an element's logical minimum width ("1" usually represents the width of about 1
        ///     character, but exact dimensions are up to the current style)
        /// </summary>
        /// <remarks>
        ///     This attached property can be attached to any UI Element to define an element's logical minimum width. The
        ///     width is typically calculated on the current font's width based on the letter 'N'. So a width of 10 can typically
        ///     accommodate 10 'N' letters. Note that this may vary with different skins.
        /// </remarks>
        public static readonly DependencyProperty MinWidthExProperty = DependencyProperty.RegisterAttached("MinWidthEx", typeof (double), typeof (SimpleView), new PropertyMetadata(double.NaN, OnMinWidthExChanged));

        /// <summary>
        ///     Attached property to set an element's logical maximum width ("1" usually represents the width of about 1
        ///     character, but exact dimensions are up to the current style)
        /// </summary>
        /// <remarks>
        ///     This attached property can be attached to any UI Element to define an element's logical maximum width. The
        ///     width is typically calculated on the current font's width based on the letter 'N'. So a width of 10 can typically
        ///     accomodate 10 'N' letters. Note that this may vary with different skins.
        /// </remarks>
        public static readonly DependencyProperty MaxWidthExProperty = DependencyProperty.RegisterAttached("MaxWidthEx", typeof (double), typeof (SimpleView), new PropertyMetadata(double.NaN, OnMaxWidthExChanged));

        /// <summary>
        ///     Attached property to set an element's logical height ("1" usually represents the height of about 1 character,
        ///     but exact dimensions are up to the current style)
        /// </summary>
        /// <remarks>This attached property can be attached to any UI Element to define an element's logical height.</remarks>
        public static readonly DependencyProperty HeightExProperty = DependencyProperty.RegisterAttached("HeightEx", typeof (double), typeof (SimpleView), new PropertyMetadata(double.NaN, OnHeightExChanged));

        /// <summary>
        ///     Attached property to set an element's logical minimum height ("1" usually represents the height of about 1
        ///     character, but exact dimensions are up to the current style)
        /// </summary>
        /// <remarks>This attached property can be attached to any UI Element to define an element's logical minimum height.</remarks>
        public static readonly DependencyProperty MinHeightExProperty = DependencyProperty.RegisterAttached("MinHeightEx", typeof (double), typeof (SimpleView), new PropertyMetadata(double.NaN, OnMinHeightExChanged));

        /// <summary>
        ///     Attached property to set an element's logical maximum height ("1" usually represents the height of about 1
        ///     character, but exact dimensions are up to the current style)
        /// </summary>
        /// <remarks>This attached property can be attached to any UI Element to define an element's logical minimum height.</remarks>
        public static readonly DependencyProperty MaxHeightExProperty = DependencyProperty.RegisterAttached("MaxHeightEx", typeof (double), typeof (SimpleView), new PropertyMetadata(double.NaN, OnMaxHeightExChanged));

        /// <summary>An abstract label property that can be used to assign labels to arbitrary controls</summary>
        public static readonly DependencyProperty LabelProperty = DependencyProperty.RegisterAttached("Label", typeof (string), typeof (SimpleView), new PropertyMetadata(null));

        /// <summary>Font family for the abstract label</summary>
        public static readonly DependencyProperty LabelFontFamilyProperty = DependencyProperty.RegisterAttached("LabelFontFamily", typeof (FontFamily), typeof (SimpleView), new PropertyMetadata(null));

        /// <summary>Font style for the abstract label</summary>
        public static readonly DependencyProperty LabelFontStyleProperty = DependencyProperty.RegisterAttached("LabelFontStyle", typeof (FontStyle), typeof (SimpleView), new PropertyMetadata(FontStyles.Normal));

        /// <summary>Font weight for the abstract label</summary>
        public static readonly DependencyProperty LabelFontWeightProperty = DependencyProperty.RegisterAttached("LabelFontWeight", typeof (FontWeight), typeof (SimpleView), new PropertyMetadata(FontWeights.Normal));

        /// <summary>Font size for the abstract label</summary>
        public static readonly DependencyProperty LabelFontSizeProperty = DependencyProperty.RegisterAttached("LabelFontSize", typeof (double), typeof (SimpleView), new PropertyMetadata(0d));

        /// <summary>Font color for the abstract label</summary>
        public static readonly DependencyProperty LabelForegroundBrushProperty = DependencyProperty.RegisterAttached("LabelForegroundBrush", typeof (Brush), typeof (SimpleView), new PropertyMetadata(null));

        /// <summary>Attached property to set any view's icon resource key</summary>
        /// <remarks>This attached property can be attached to any UI Element to define a view icon resource key</remarks>
        public static readonly DependencyProperty IconResourceKeyProperty = DependencyProperty.RegisterAttached("IconResourceKey", typeof (string), typeof (SimpleView), new PropertyMetadata(""));

        /// <summary>
        ///     Theme color used by the view (utilized by some themes to create color schemes)
        /// </summary>
        public static readonly DependencyProperty ViewThemeColorProperty = DependencyProperty.Register("ViewThemeColor", typeof (Color), typeof (SimpleView), new PropertyMetadata(Colors.Transparent));

        /// <summary>Attached property to set any view's title</summary>
        /// <remarks>This attached property can be attached to any UI Element to define a view title</remarks>
        public static readonly DependencyProperty TitleProperty = DependencyProperty.RegisterAttached("Title", typeof (string), typeof (SimpleView), new PropertyMetadata("", (s, e) =>
        {
            var view = s as SimpleView;
            if (view == null) return;
            if (view.TitleChanged != null)
                view.TitleChanged(s, new EventArgs());
        }));

        /// <summary>
        ///     Color associated with the Title. (Note: Not all elements that respect the Title also respect the color. It's an
        ///     optional setting)
        /// </summary>
        public static readonly DependencyProperty TitleColorProperty = DependencyProperty.RegisterAttached("TitleColor", typeof (Brush), typeof (SimpleView), new PropertyMetadata(null));

        /// <summary>
        ///     Second color (such as background color) associated with the Title. (Note: Not all elements that respect the Title
        ///     also respect the color. It's an optional setting)
        /// </summary>
        public static readonly DependencyProperty TitleColor2Property = DependencyProperty.RegisterAttached("TitleColor2", typeof (Brush), typeof (SimpleView), new PropertyMetadata(null));

        /// <summary>Attached property to set any view's group</summary>
        /// <remarks>This attached property can be attached to any UI Element to define a view group</remarks>
        public static readonly DependencyProperty GroupProperty = DependencyProperty.RegisterAttached("Group", typeof (string), typeof (SimpleView), new PropertyMetadata(""));

        /// <summary>Attached property to set the view's desired sizing strategy</summary>
        /// <remarks>This attached property can be attached to any UI Element to define a view size strategy</remarks>
        public static readonly DependencyProperty SizeStrategyProperty = DependencyProperty.RegisterAttached("SizeStrategy", typeof (ViewSizeStrategies), typeof (SimpleView), new PropertyMetadata(ViewSizeStrategies.UseMinimumSizeRequired));

        /// <summary>Suggested view height</summary>
        /// <remarks>
        ///     Only applicable if SizeStrategy = UseSuggestedSize and if the skin and type of view supports explicit sizing.
        ///     Otherwise, the view is handled as if the strategy is set to use maximum size.
        /// </remarks>
        public static readonly DependencyProperty SuggestedHeightProperty = DependencyProperty.RegisterAttached("SuggestedHeight", typeof (double), typeof (SimpleView), new PropertyMetadata(500d));

        /// <summary>Suggested view width</summary>
        /// <remarks>
        ///     Only applicable if SizeStrategy = UseSuggestedSize and if the skin and type of view supports explicit sizing.
        ///     Otherwise, the view is handled as if the strategy is set to use maximum size.
        /// </remarks>
        public static readonly DependencyProperty SuggestedWidthProperty = DependencyProperty.RegisterAttached("SuggestedWidth", typeof (double), typeof (SimpleView), new PropertyMetadata(800d));

        /// <summary>Attached property used to define column breaks</summary>
        public static readonly DependencyProperty ColumnBreakProperty = DependencyProperty.RegisterAttached("ColumnBreak", typeof (bool), typeof (SimpleView), new PropertyMetadata(false, (s, e) => InvalidateAllVisuals(s)));

        /// <summary>Property used to determine group break (adds a space between elements)</summary>
        public static readonly DependencyProperty GroupBreakProperty = DependencyProperty.RegisterAttached("GroupBreak", typeof (bool), typeof (SimpleView), new PropertyMetadata(false, (s, e) => InvalidateAllVisuals(s)));

        /// <summary>Group caption/title</summary>
        public static readonly DependencyProperty GroupTitleProperty = DependencyProperty.RegisterAttached("GroupTitle", typeof (string), typeof (SimpleView), new PropertyMetadata("", (s, e) => InvalidateAllVisuals(s)));

        /// <summary>
        ///     Property used to determine whether the control is to be seen as a standard flow element, or whether it is to
        ///     be put together with the previous element
        /// </summary>
        public static readonly DependencyProperty FlowsWithPreviousProperty = DependencyProperty.RegisterAttached("FlowsWithPrevious", typeof (bool), typeof (SimpleView), new PropertyMetadata(false, (s, e) => InvalidateAllVisuals(s)));

        /// <summary>Property used to determine whether a line break shall be forced before the control</summary>
        public static readonly DependencyProperty LineBreakProperty = DependencyProperty.RegisterAttached("LineBreak", typeof (bool), typeof (SimpleView), new PropertyMetadata(false, (s, e) => InvalidateAllVisuals(s)));

        /// <summary>Property used to determine whether the control shall span the complete available width</summary>
        public static readonly DependencyProperty SpanFullWidthProperty = DependencyProperty.RegisterAttached("SpanFullWidth", typeof (bool), typeof (SimpleView), new PropertyMetadata(false, (s, e) => InvalidateAllVisuals(s)));

        /// <summary>Property used to determine group break (adds a space between elements)</summary>
        public static readonly DependencyProperty UIElementTypeProperty = DependencyProperty.RegisterAttached("UIElementType", typeof (UIElementTypes), typeof (SimpleView), new PropertyMetadata(UIElementTypes.Primary, (s, e) => InvalidateAllVisuals(s)));

        /// <summary>Defines the title of the UI element</summary>
        public static readonly DependencyProperty UIElementTitleProperty = DependencyProperty.RegisterAttached("UIElementTitle", typeof (string), typeof (SimpleView), new PropertyMetadata("", (s, e) => InvalidateAllVisuals(s)));

        /// <summary>
        ///     Defines whether a control is a stand-alone label in an automatic layout (next control is NOT considered the
        ///     edit control but a new group of controls altogether)
        /// </summary>
        public static readonly DependencyProperty IsStandAloneLabelProperty = DependencyProperty.RegisterAttached("IsStandAloneLabel", typeof (bool), typeof (SimpleView), new PropertyMetadata(false, (s, e) => InvalidateAllVisuals(s)));

        /// <summary>
        ///     Defines whether a control is a stand-alone edit control in an automatic layout (this control is NOT considered
        ///     to be a label but the edit control instead and the next control is NOT considered the edit control but a new group
        ///     of controls altogether)
        /// </summary>
        public static readonly DependencyProperty IsStandAloneEditControlProperty = DependencyProperty.RegisterAttached("IsStandAloneEditControl", typeof (bool), typeof (SimpleView), new PropertyMetadata(false, (s, e) => InvalidateAllVisuals(s)));

        /// <summary>Attached behavior</summary>
        private static readonly DependencyProperty BehaviorProperty = DependencyProperty.RegisterAttached("Behavior", typeof (string), typeof (SimpleView), new FrameworkPropertyMetadata(OnBehaviorChanged));

        /// <summary>
        ///     This property can be bound to a property in a view model (or other data context) to update the property with a
        ///     reference to whatever object currently has focus.
        /// </summary>
        /// <remarks>
        ///     This feature is meant for very specialized uses only. If you are unsure of what this does exactly, this is
        ///     probably not a feature you want to use.
        /// </remarks>
        public static readonly DependencyProperty FocusForwardingTargetProperty = DependencyProperty.RegisterAttached("FocusForwardingTarget", typeof (UIElement), typeof (SimpleView), new FrameworkPropertyMetadata(null) {BindsTwoWayByDefault = true});

        /// <summary>Indicates whether focus forwarding is enabled</summary>
        public static readonly DependencyProperty FocusForwardingIsEnabledProperty = DependencyProperty.RegisterAttached("FocusForwardingIsEnabled", typeof (bool), typeof (SimpleView), new UIPropertyMetadata(false, OnFocusForwardingIsEnabledChanged));

        /// <summary>Indicates whether a view element is "docked" into the main object or not.</summary>
        public static readonly DependencyProperty DockedProperty = DependencyProperty.RegisterAttached("Docked", typeof (bool), typeof (SimpleView), new PropertyMetadata(true));

        /// <summary>Indicates whether an element can be docked and undocked</summary>
        public static readonly DependencyProperty SupportsDockingProperty = DependencyProperty.RegisterAttached("SupportsDocking", typeof (bool), typeof (SimpleView), new PropertyMetadata(false));

        /// <summary>Indicates whether an element can be independenty closed</summary>
        public static readonly DependencyProperty ClosableProperty = DependencyProperty.RegisterAttached("Closable", typeof (bool), typeof (SimpleView), new PropertyMetadata(false));

        /// <summary>Command to be triggered when a view (element) is closed</summary>
        /// <remarks>
        ///     Only applies to objects that have the Closable attached property set to true.
        ///     In most cases, this command/action is optional. When no special action is provided, the element's visibility is
        ///     simply set to collapsed.
        /// </remarks>
        public static readonly DependencyProperty CloseActionProperty = DependencyProperty.RegisterAttached("CloseAction", typeof (ICommand), typeof (SimpleView), new PropertyMetadata(null));

        /// <summary>
        ///     <summary>Indicates the relative width of an alement compared to other elements</summary>
        /// </summary>
        public static readonly DependencyProperty RelativeWidthProperty = DependencyProperty.RegisterAttached("RelativeWidth", typeof (GridLength), typeof (SimpleView), new PropertyMetadata(new GridLength(1, GridUnitType.Star), OnRelativeWidthChanged));

        /// <summary>
        ///     <summary>Indicates the relative height of an alement compared to other elements</summary>
        /// </summary>
        public static readonly DependencyProperty RelativeHeightProperty = DependencyProperty.RegisterAttached("RelativeHeight", typeof (GridLength), typeof (SimpleView), new PropertyMetadata(new GridLength(1, GridUnitType.Star), OnRelativeHeightChanged));

        /// <summary>
        ///     Constructor
        /// </summary>
        public SimpleView()
        {
            if (!_templateSet)
            {
                try
                {
                    var defaultTemplate = new ItemsPanelTemplate(new FrameworkElementFactory(typeof (Grid)));
                    defaultTemplate.Seal();
                    ItemsPanelProperty.OverrideMetadata(typeof (SimpleView), new FrameworkPropertyMetadata(defaultTemplate));
                    _templateSet = true;
                }
                catch
                {
                }
            }

            OriginalViewLoadLocation = string.Empty;
        }

        /// <summary>
        ///     Location this view was originally loaded from
        /// </summary>
        /// <value></value>
        public string OriginalViewLoadLocation { get; set; }

        /// <summary>
        ///     Occurs when the user interface got activated
        /// </summary>
        public event EventHandler Activated;

        /// <summary>
        ///     Raises the activated events.
        /// </summary>
        /// <exception cref="System.NotImplementedException"></exception>
        public void RaiseActivated()
        {
            if (Activated != null)
                Activated(this, new EventArgs());
        }

        /// <summary>
        ///     Attached property to set an element's logical width ("1" usually represents the width of about 1 character,
        ///     but exact dimensions are up to the current style)
        /// </summary>
        /// <param name="obj">Object to set the size on</param>
        /// <returns>Width</returns>
        /// <remarks>This attached property can be attached to any UI Element to define a width</remarks>
        public static double GetWidthEx(DependencyObject obj)
        {
            return (double) obj.GetValue(WidthExProperty);
        }

        /// <summary>
        ///     Attached property to set an element's logical width ("1" usually represents the width of about 1 character,
        ///     but exact dimensions are up to the current style)
        /// </summary>
        /// <param name="obj">Object to set the size on</param>
        /// <param name="value">Value to set</param>
        public static void SetWidthEx(DependencyObject obj, double value)
        {
            obj.SetValue(WidthExProperty, value);
        }

        /// <summary>Attached behavior changed handler</summary>
        /// <param name="obj">Object to attach to</param>
        /// <param name="args">Event arguments</param>
        private static void OnWidthExChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            var newValue = (double) args.NewValue;
            if (double.IsNaN(newValue)) return;

            var element = obj as FrameworkElement;
            if (element == null) return;

            var totalWidth = GetAbstractWidth(element, newValue);
            element.Width = totalWidth;
            var control = element as Control;
            if (control != null) element.Width += control.Padding.Left + control.Padding.Right;
            InvalidateAllVisuals(element);
        }

        /// <summary>
        ///     Attached property to set an element's logical minimum width ("1" usually represents the width of about 1
        ///     character, but exact dimensions are up to the current style)
        /// </summary>
        /// <param name="obj">Object to set the size on</param>
        /// <returns>Minimum Width</returns>
        /// <remarks>This attached property can be attached to any UI Element to define a minimum width</remarks>
        public static double GetMinWidthEx(DependencyObject obj)
        {
            return (double) obj.GetValue(MinWidthExProperty);
        }

        /// <summary>
        ///     Attached property to set an element's logical minimum width ("1" usually represents the width of about 1
        ///     character, but exact dimensions are up to the current style)
        /// </summary>
        /// <param name="obj">Object to set the size on</param>
        /// <param name="value">Value to set</param>
        public static void SetMinWidthEx(DependencyObject obj, double value)
        {
            obj.SetValue(MinWidthExProperty, value);
        }

        /// <summary>Fires when the logical minimum width changes</summary>
        /// <param name="obj">Object to attach to</param>
        /// <param name="args">Event arguments</param>
        private static void OnMinWidthExChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            var newValue = (double) args.NewValue;
            if (double.IsNaN(newValue)) return;

            var element = obj as FrameworkElement;
            if (element == null) return;

            element.MinWidth = GetAbstractWidth(element, newValue);
            InvalidateAllVisuals(element);
        }

        /// <summary>
        ///     Attached property to set an element's logical maximum width ("1" usually represents the width of about 1
        ///     character, but exact dimensions are up to the current style)
        /// </summary>
        /// <param name="obj">Object to set the size on</param>
        /// <returns>Minimum Width</returns>
        /// <remarks>This attached property can be attached to any UI Element to define a maximum width</remarks>
        public static double GetMaxWidthEx(DependencyObject obj)
        {
            return (double) obj.GetValue(MaxWidthExProperty);
        }

        /// <summary>
        ///     Attached property to set an element's logical maximum width ("1" usually represents the width of about 1
        ///     character, but exact dimensions are up to the current style)
        /// </summary>
        /// <param name="obj">Object to set the size on</param>
        /// <param name="value">Value to set</param>
        public static void SetMaxWidthEx(DependencyObject obj, double value)
        {
            obj.SetValue(MaxWidthExProperty, value);
        }

        /// <summary>Fires when the logical maximum width changes</summary>
        /// <param name="obj">Object to attach to</param>
        /// <param name="args">Event arguments</param>
        private static void OnMaxWidthExChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            var newValue = (double) args.NewValue;
            if (double.IsNaN(newValue)) return;

            var element = obj as FrameworkElement;
            if (element == null) return;

            element.MaxWidth = GetAbstractWidth(element, newValue);
            InvalidateAllVisuals(element);
        }

        /// <summary>
        ///     Calculates the concrete width for an abstract width value based on font metrics
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="width">The abstract width.</param>
        /// <returns>Concrete width</returns>
        private static double GetAbstractWidth(DependencyObject element, double width)
        {
            if (element == null) return double.NaN;
            var fontFamily = TextBlock.GetFontFamily(element);
            var fontSize = TextBlock.GetFontSize(element);
            var face = new Typeface(fontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
            GlyphTypeface glyph;
            if (face.TryGetGlyphTypeface(out glyph))
            {
                var map = glyph.CharacterToGlyphMap['N'];
                var glyphWidth = glyph.AdvanceWidths[map];
                var singleWidth = fontSize*glyphWidth;
                return Math.Round(width*singleWidth);
            }

            return double.NaN;
        }

        /// <summary>
        ///     Calculates the concrete height for an abstract height value based on font metrics
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="height">The abstract height.</param>
        /// <returns>Concrete height</returns>
        private static double GetAbstractHeight(DependencyObject element, double height)
        {
            if (element == null) return double.NaN;
            var fontFamily = TextBlock.GetFontFamily(element);
            var fontSize = TextBlock.GetFontSize(element);
            var singleHeight = fontSize*fontFamily.LineSpacing;
            return Math.Round(height*singleHeight);
        }

        /// <summary>
        ///     Attached property to set an element's logical height ("1" usually represents the height of about 1 character,
        ///     but exact dimensions are up to the current style)
        /// </summary>
        /// <param name="obj">Object to set the size on</param>
        /// <returns>Width</returns>
        /// <remarks>This attached property can be attached to any UI Element to define a height</remarks>
        public static double GetHeightEx(DependencyObject obj)
        {
            return (double) obj.GetValue(HeightExProperty);
        }

        /// <summary>
        ///     Attached property to set an element's logical height ("1" usually represents the height of about 1 character,
        ///     but exact dimensions are up to the current style)
        /// </summary>
        /// <param name="obj">Object to set the size on</param>
        /// <param name="value">Value to set</param>
        public static void SetHeightEx(DependencyObject obj, double value)
        {
            obj.SetValue(HeightExProperty, value);
        }

        /// <summary>Fires when HeightEx changes</summary>
        /// <param name="obj">Object to attach to</param>
        /// <param name="args">Event arguments</param>
        private static void OnHeightExChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            var newValue = (double) args.NewValue;
            if (double.IsNaN(newValue)) return;
            var element = obj as FrameworkElement;
            if (element == null) return;
            var totalHeight = GetAbstractHeight(element, newValue);
            element.Height = totalHeight;
            var control = element as Control;
            if (control != null) element.Height += control.Padding.Top + control.Padding.Bottom;
            InvalidateAllVisuals(element);
        }

        /// <summary>
        ///     Attached property to set an element's logical minimum height ("1" usually represents the height of about 1
        ///     character, but exact dimensions are up to the current style)
        /// </summary>
        /// <param name="obj">Object to set the size on</param>
        /// <returns>Width</returns>
        /// <remarks>This attached property can be attached to any UI Element to define a minimum height</remarks>
        public static double GetMinHeightEx(DependencyObject obj)
        {
            return (double) obj.GetValue(MinHeightExProperty);
        }

        /// <summary>
        ///     Attached property to set an element's logical minimum height ("1" usually represents the width of about 1
        ///     character, but exact dimensions are up to the current style)
        /// </summary>
        /// <param name="obj">Object to set the size on</param>
        /// <param name="value">Value to set</param>
        public static void SetMinHeightEx(DependencyObject obj, double value)
        {
            obj.SetValue(MinHeightExProperty, value);
        }

        /// <summary>Fires when MinHeightEx changes</summary>
        /// <param name="obj">Object to attach to</param>
        /// <param name="args">Event arguments</param>
        private static void OnMinHeightExChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            var newValue = (double) args.NewValue;
            if (double.IsNaN(newValue)) return;
            var element = obj as FrameworkElement;
            if (element == null) return;
            element.MinHeight = GetAbstractHeight(element, newValue);
            InvalidateAllVisuals(element);
        }

        /// <summary>
        ///     Attached property to set an element's logical maximum height ("1" usually represents the height of about 1
        ///     character, but exact dimensions are up to the current style)
        /// </summary>
        /// <param name="obj">Object to set the size on</param>
        /// <returns>Width</returns>
        /// <remarks>This attached property can be attached to any UI Element to define a maximum height</remarks>
        public static double GetMaxHeightEx(DependencyObject obj)
        {
            return (double) obj.GetValue(MaxHeightExProperty);
        }

        /// <summary>
        ///     Attached property to set an element's logical maximum height ("1" usually represents the height of about 1
        ///     character, but exact dimensions are up to the current style)
        /// </summary>
        /// <param name="obj">Object to set the size on</param>
        /// <param name="value">Value to set</param>
        public static void SetMaxHeightEx(DependencyObject obj, double value)
        {
            obj.SetValue(MaxHeightExProperty, value);
        }

        /// <summary>Fires when MaxHeightEx changes</summary>
        /// <param name="obj">Object to attach to</param>
        /// <param name="args">Event arguments</param>
        private static void OnMaxHeightExChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            var newValue = (double) args.NewValue;
            if (double.IsNaN(newValue)) return;
            var element = obj as FrameworkElement;
            if (element == null) return;
            element.MaxHeight = GetAbstractHeight(element, newValue);
            InvalidateAllVisuals(element);
        }

        /// <summary>Returns the value of the attached label property</summary>
        /// <param name="obj">Dependency object</param>
        /// <returns>Label</returns>
        public static string GetLabel(DependencyObject obj)
        {
            return (string) obj.GetValue(LabelProperty);
        }

        /// <summary>Sets the value of the attached label property</summary>
        /// <param name="obj">Dependency object</param>
        /// <param name="value">The value.</param>
        public static void SetLabel(DependencyObject obj, string value)
        {
            obj.SetValue(LabelProperty, value);
        }

        /// <summary>Font family for the abstract label</summary>
        /// <param name="obj">Dependency object</param>
        /// <returns>Font family</returns>
        public static FontFamily GetLabelFontFamily(DependencyObject obj)
        {
            return (FontFamily) obj.GetValue(LabelFontFamilyProperty);
        }

        /// <summary>Font family for the abstract label</summary>
        /// <param name="obj">Dependency object</param>
        /// <param name="value">The value.</param>
        public static void SetLabelFontFamily(DependencyObject obj, FontFamily value)
        {
            obj.SetValue(LabelFontFamilyProperty, value);
        }

        /// <summary>Font style for the abstract label</summary>
        /// <param name="obj">Dependency object</param>
        /// <returns>Font size</returns>
        public static FontStyle GetLabelFontStyle(DependencyObject obj)
        {
            return (FontStyle) obj.GetValue(LabelFontStyleProperty);
        }

        /// <summary>Font style for the abstract label</summary>
        /// <param name="obj">Dependency object</param>
        /// <param name="value">The value.</param>
        public static void SetLabelFontStyle(DependencyObject obj, FontStyle value)
        {
            obj.SetValue(LabelFontStyleProperty, value);
        }

        /// <summary>Font weight for the abstract label</summary>
        /// <param name="obj">Dependency object</param>
        /// <returns>Font size</returns>
        public static FontWeight GetLabelFontWeight(DependencyObject obj)
        {
            return (FontWeight) obj.GetValue(LabelFontWeightProperty);
        }

        /// <summary>Font weight for the abstract label</summary>
        /// <param name="obj">Dependency object</param>
        /// <param name="value">The value.</param>
        public static void SetLabelFontWeight(DependencyObject obj, FontWeight value)
        {
            obj.SetValue(LabelFontWeightProperty, value);
        }

        /// <summary>Font size for the abstract label</summary>
        /// <param name="obj">Dependency object</param>
        /// <returns>Font size</returns>
        public static double GetLabelFontSize(DependencyObject obj)
        {
            return (double) obj.GetValue(LabelFontSizeProperty);
        }

        /// <summary>Font size for the abstract label</summary>
        /// <param name="obj">Dependency object</param>
        /// <param name="value">The value.</param>
        public static void SetLabelFontSize(DependencyObject obj, double value)
        {
            obj.SetValue(LabelFontSizeProperty, value);
        }

        /// <summary>Font color for the abstract label</summary>
        /// <param name="obj">Dependency object</param>
        /// <returns>Font size</returns>
        public static Brush GetLabelForegroundBrush(DependencyObject obj)
        {
            return (Brush) obj.GetValue(LabelForegroundBrushProperty);
        }

        /// <summary>Font color for the abstract label</summary>
        /// <param name="obj">Dependency object</param>
        /// <param name="value">The value.</param>
        public static void SetLabelForegroundBrush(DependencyObject obj, Brush value)
        {
            obj.SetValue(LabelForegroundBrushProperty, value);
        }

        /// <summary>View icon resource key</summary>
        /// <param name="obj">Object to set the icon resource key on</param>
        /// <returns>Icon resource key</returns>
        /// <remarks>This attached property can be attached to any UI Element to define an icon resource key</remarks>
        public static string GetIconResourceKey(DependencyObject obj)
        {
            return (string) obj.GetValue(IconResourceKeyProperty);
        }

        /// <summary>View icon resource key</summary>
        /// <param name="obj">Object to set the icon resource key on</param>
        /// <param name="value">Value to set</param>
        public static void SetIconResourceKey(DependencyObject obj, string value)
        {
            obj.SetValue(IconResourceKeyProperty, value);
        }

        /// <summary>
        ///     Theme color used by the view (utilized by some themes to create color schemes)
        /// </summary>
        public static void SetViewThemeColor(DependencyObject d, Color value)
        {
            d.SetValue(ViewThemeColorProperty, value);
        }

        /// <summary>
        ///     Theme color used by the view (utilized by some themes to create color schemes)
        /// </summary>
        public static Color GetViewThemeColor(DependencyObject d)
        {
            return (Color) d.GetValue(ViewThemeColorProperty);
        }

        /// <summary>View title</summary>
        /// <param name="obj">Object to set the title on</param>
        /// <returns>Title</returns>
        /// <remarks>This attached property can be attached to any UI Element to define a view title</remarks>
        public static string GetTitle(DependencyObject obj)
        {
            return (string) obj.GetValue(TitleProperty);
        }

        /// <summary>View title</summary>
        /// <param name="obj">Object to set the title on</param>
        /// <param name="value">Value to set</param>
        public static void SetTitle(DependencyObject obj, string value)
        {
            obj.SetValue(TitleProperty, value);
        }

        /// <summary>
        ///     Fires when the view title changes
        /// </summary>
        public event EventHandler TitleChanged;

        /// <summary>
        ///     Gets the color of the title.
        /// </summary>
        /// <param name="obj">The object to get the color for.</param>
        /// <returns>Title color (brush)</returns>
        public static Brush GetTitleColor(DependencyObject obj)
        {
            return (Brush) obj.GetValue(TitleColorProperty);
        }

        /// <summary>
        ///     Sets the color of the title.
        /// </summary>
        /// <param name="obj">The object to set the value on.</param>
        /// <param name="value">The color (Brush).</param>
        public static void SetTitleColor(DependencyObject obj, Brush value)
        {
            obj.SetValue(TitleColorProperty, value);
        }

        /// <summary>
        ///     Gets the second color of the title.
        /// </summary>
        /// <param name="obj">The object to get the color for.</param>
        /// <returns>Title color (brush)</returns>
        public static Brush GetTitleColor2(DependencyObject obj)
        {
            return (Brush) obj.GetValue(TitleColor2Property);
        }

        /// <summary>
        ///     Sets the second color of the title.
        /// </summary>
        /// <param name="obj">The object to set the value on.</param>
        /// <param name="value">The color (Brush).</param>
        public static void SetTitleColor2(DependencyObject obj, Brush value)
        {
            obj.SetValue(TitleColor2Property, value);
        }

        /// <summary>View group</summary>
        /// <param name="obj">Object to set the group on</param>
        /// <returns>Group</returns>
        /// <remarks>This attached property can be attached to any UI Element to define a view group</remarks>
        public static string GetGroup(DependencyObject obj)
        {
            return (string) obj.GetValue(GroupProperty);
        }

        /// <summary>View group</summary>
        /// <param name="obj">Object to set the group on</param>
        /// <param name="value">Value to set</param>
        public static void SetGroup(DependencyObject obj, string value)
        {
            obj.SetValue(GroupProperty, value);
        }

        /// <summary>View sizing strategy</summary>
        /// <param name="obj">Object to set the sizing strategy on</param>
        /// <returns>Sizing strategy</returns>
        /// <remarks>This attached property can be attached to any UI Element to define a sizing strategy</remarks>
        public static ViewSizeStrategies GetSizeStrategy(DependencyObject obj)
        {
            return (ViewSizeStrategies) obj.GetValue(SizeStrategyProperty);
        }

        /// <summary>View size strategy</summary>
        /// <param name="obj">Object to set the size strategy on</param>
        /// <param name="value">Value to set</param>
        public static void SetSizeStrategy(DependencyObject obj, ViewSizeStrategies value)
        {
            obj.SetValue(SizeStrategyProperty, value);
        }

        /// <summary>Suggested view height</summary>
        /// <remarks>
        ///     Only applicable if SizeStrategy = UseSuggestedSize and if the skin and type of view supports explicit sizing.
        ///     Otherwise, the view is handled as if the strategy is set to use maximum size.
        /// </remarks>
        public static double GetSuggestedHeight(DependencyObject obj)
        {
            return (double) obj.GetValue(SuggestedHeightProperty);
        }

        /// <summary>Suggested view height</summary>
        /// <remarks>
        ///     Only applicable if SizeStrategy = UseSuggestedSize and if the skin and type of view supports explicit sizing.
        ///     Otherwise, the view is handled as if the strategy is set to use maximum size.
        /// </remarks>
        public static void SetSuggestedHeight(DependencyObject obj, double value)
        {
            obj.SetValue(SuggestedHeightProperty, value);
        }

        /// <summary>Suggested view width</summary>
        /// <remarks>
        ///     Only applicable if SizeStrategy = UseSuggestedSize and if the skin and type of view supports explicit sizing.
        ///     Otherwise, the view is handled as if the strategy is set to use maximum size.
        /// </remarks>
        public static double GetSuggestedWidth(DependencyObject obj)
        {
            return (double) obj.GetValue(SuggestedWidthProperty);
        }

        /// <summary>Suggested view width</summary>
        /// <remarks>
        ///     Only applicable if SizeStrategy = UseSuggestedSize and if the skin and type of view supports explicit sizing.
        ///     Otherwise, the view is handled as if the strategy is set to use maximum size.
        /// </remarks>
        public static void SetSuggestedWidth(DependencyObject obj, double value)
        {
            obj.SetValue(SuggestedWidthProperty, value);
        }

        /// <summary>Column break indicator</summary>
        /// <param name="obj">Object to set the value on</param>
        /// <returns>Value</returns>
        public static bool GetColumnBreak(DependencyObject obj)
        {
            return (bool) obj.GetValue(ColumnBreakProperty);
        }

        /// <summary>Column break indicator</summary>
        /// <param name="obj">Object to get the value for</param>
        /// <param name="value">Value</param>
        public static void SetColumnBreak(DependencyObject obj, bool value)
        {
            obj.SetValue(ColumnBreakProperty, value);
        }

        /// <summary>Group break indicator</summary>
        /// <param name="obj">Object to set the value on</param>
        /// <returns>Value</returns>
        public static bool GetGroupBreak(DependencyObject obj)
        {
            return (bool) obj.GetValue(GroupBreakProperty);
        }

        /// <summary>Group break indicator</summary>
        /// <param name="obj">Object to get the value for</param>
        /// <param name="value">Value</param>
        public static void SetGroupBreak(DependencyObject obj, bool value)
        {
            obj.SetValue(GroupBreakProperty, value);
        }

        /// <summary>Group caption/title</summary>
        public static string GetGroupTitle(DependencyObject obj)
        {
            return (string) obj.GetValue(GroupTitleProperty);
        }

        /// <summary>Group caption/title</summary>
        public static void SetGroupTitle(DependencyObject obj, string value)
        {
            obj.SetValue(GroupTitleProperty, value);
        }

        /// <summary>
        ///     Property used to determine whether the control is to be seen as a standard flow element, or whether it is to
        ///     be put together with the previous element
        /// </summary>
        public static bool GetFlowsWithPrevious(DependencyObject obj)
        {
            return (bool) obj.GetValue(FlowsWithPreviousProperty);
        }

        /// <summary>
        ///     Property used to determine whether the control is to be seen as a standard flow element, or whether it is to
        ///     be put together with the previous element
        /// </summary>
        public static void SetFlowsWithPrevious(DependencyObject obj, bool value)
        {
            obj.SetValue(FlowsWithPreviousProperty, value);
        }

        /// <summary>Property used to determine whether a line break shall be forced before the control</summary>
        public static bool GetLineBreak(DependencyObject obj)
        {
            return (bool) obj.GetValue(LineBreakProperty);
        }

        /// <summary>Property used to determine whether a line break shall be forced before the control</summary>
        public static void SetLineBreak(DependencyObject obj, bool value)
        {
            obj.SetValue(LineBreakProperty, value);
        }

        /// <summary>Property used to determine whether the control shall span the complete available width</summary>
        public static bool GetSpanFullWidth(DependencyObject obj)
        {
            return (bool) obj.GetValue(SpanFullWidthProperty);
        }

        /// <summary>Property used to determine whether the control shall span the complete available width</summary>
        public static void SetSpanFullWidth(DependencyObject obj, bool value)
        {
            obj.SetValue(SpanFullWidthProperty, value);
        }

        /// <summary>Group break indicator</summary>
        /// <param name="obj">Object to set the value on</param>
        /// <returns>Value</returns>
        public static UIElementTypes GetUIElementType(DependencyObject obj)
        {
            return (UIElementTypes) obj.GetValue(UIElementTypeProperty);
        }

        /// <summary>Group break indicator</summary>
        /// <param name="obj">Object to get the value for</param>
        /// <param name="value">Value</param>
        public static void SetUIElementType(DependencyObject obj, UIElementTypes value)
        {
            obj.SetValue(UIElementTypeProperty, value);
        }

        /// <summary>Defines the title of the UI element</summary>
        /// <param name="obj">Object to set the value on</param>
        /// <returns>Value</returns>
        public static string GetUIElementTitle(DependencyObject obj)
        {
            return (string) obj.GetValue(UIElementTitleProperty);
        }

        /// <summary>Defines the title of the UI element</summary>
        /// <param name="obj">Object to get the value for</param>
        /// <param name="value">Value</param>
        public static void SetUIElementTitle(DependencyObject obj, string value)
        {
            obj.SetValue(UIElementTitleProperty, value);
        }

        /// <summary>Stand alone label indicator</summary>
        /// <param name="obj">Object to set the value on</param>
        /// <returns>Value</returns>
        public static bool GetIsStandAloneLabel(DependencyObject obj)
        {
            return (bool) obj.GetValue(IsStandAloneLabelProperty);
        }

        /// <summary>Stand alone label indicator</summary>
        /// <param name="obj">Object to get the value for</param>
        /// <param name="value">Value</param>
        public static void SetIsStandAloneLabel(DependencyObject obj, bool value)
        {
            obj.SetValue(IsStandAloneLabelProperty, value);
        }

        /// <summary>Stand alone edit control indicator</summary>
        /// <param name="obj">Object to set the value on</param>
        /// <returns>Value</returns>
        public static bool GetIsStandAloneEditControl(DependencyObject obj)
        {
            return (bool) obj.GetValue(IsStandAloneEditControlProperty);
        }

        /// <summary>Stand alone edit control indicator</summary>
        /// <param name="obj">Object to get the value for</param>
        /// <param name="value">Value</param>
        public static void SetIsStandAloneEditControl(DependencyObject obj, bool value)
        {
            obj.SetValue(IsStandAloneEditControlProperty, value);
        }

        /// <summary>Attached behavior changed handler</summary>
        /// <param name="obj">Object to attach to</param>
        /// <param name="args">Event arguments</param>
        private static void OnBehaviorChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            var oldValue = (string) args.OldValue;
            var newValue = (string) args.NewValue;
            if (oldValue != newValue)
            {
                var behaviorTypes = newValue.Split(',');
                foreach (var behaviorType in behaviorTypes)
                {
                    var instance = CreateObject(behaviorType);
                    if (instance != null)
                    {
                        var behavior = instance as Behavior.Behavior;
                        if (behavior != null)
                            behavior.Attach(obj);
                    }
                }
            }
        }

        /// <summary>
        ///     Sets the attached behavior class name
        /// </summary>
        /// <param name="element">Element the property is to be set for.</param>
        /// <param name="value">Value of the property</param>
        public static void SetBehavior(UIElement element, string value)
        {
            element.SetValue(BehaviorProperty, value);
        }

        /// <summary>
        ///     Gets the value of the Behavior property
        /// </summary>
        /// <param name="element">Element to get the property value for.</param>
        /// <returns>Name of the associated behavior class</returns>
        public static string GetBehavior(UIElement element)
        {
            return (string) element.GetValue(BehaviorProperty);
        }

        /// <summary>Focus forwarding getter</summary>
        /// <param name="obj">The object the property is set on.</param>
        /// <returns>UIElement or null</returns>
        public static UIElement GetFocusForwardingTarget(DependencyObject obj)
        {
            return (UIElement) obj.GetValue(FocusForwardingTargetProperty);
        }

        /// <summary>Focus forwarding setter</summary>
        /// <param name="obj">The object the property is set on.</param>
        /// <param name="value">UIElement or null</param>
        public static void SetFocusForwardingTarget(DependencyObject obj, UIElement value)
        {
            obj.SetValue(FocusForwardingTargetProperty, value);
        }

        /// <summary>Focus forwarding is enabled getter</summary>
        /// <param name="obj">The object the property is set on.</param>
        /// <returns>UIElement or null</returns>
        public static bool GetFocusForwardingIsEnabled(DependencyObject obj)
        {
            return (bool) obj.GetValue(FocusForwardingIsEnabledProperty);
        }

        /// <summary>Focus forwarding is enabled setter</summary>
        /// <param name="obj">The object the property is set on.</param>
        /// <param name="value">UIElement or null</param>
        public static void SetFocusForwardingIsEnabled(DependencyObject obj, bool value)
        {
            obj.SetValue(FocusForwardingIsEnabledProperty, value);
        }

        /// <summary>Callback for changes in the focus element forwarding is enabled property</summary>
        /// <param name="o">The object the property is set on.</param>
        /// <param name="args">
        ///     The <see cref="System.Windows.DependencyPropertyChangedEventArgs" /> instance containing the event
        ///     data.
        /// </param>
        private static void OnFocusForwardingIsEnabledChanged(DependencyObject o, DependencyPropertyChangedEventArgs args)
        {
            var element = o as UIElement;
            if (element == null) return;

            element.GotFocus += (sender, e) => SetFocusForwardingTarget(element, element);
            //element.LostFocus += (sender, e) => SetFocusForwardingTarget(element, null);
        }

        /// <summary>Indicates whether a view element is "docked" into the main object or not.</summary>
        /// <param name="obj">The object the property is set on.</param>
        /// <returns>True or false</returns>
        public static bool GetDocked(DependencyObject obj)
        {
            return (bool) obj.GetValue(DockedProperty);
        }

        /// <summary>Indicates whether a view element is "docked" into the main object or not.</summary>
        /// <param name="obj">The object the property is set on.</param>
        /// <param name="value">True or false</param>
        public static void SetDocked(DependencyObject obj, bool value)
        {
            obj.SetValue(DockedProperty, value);
        }

        /// <summary>Indicates whether an element can be docked and undocked</summary>
        /// <param name="obj">The object the property is set on.</param>
        /// <returns>True or false</returns>
        public static bool GetSupportsDocking(DependencyObject obj)
        {
            return (bool) obj.GetValue(SupportsDockingProperty);
        }

        /// <summary>Indicates whether an element can be docked and undocked</summary>
        /// <param name="obj">The object the property is set on.</param>
        /// <param name="value">True or false</param>
        public static void SetSupportsDocking(DependencyObject obj, bool value)
        {
            obj.SetValue(SupportsDockingProperty, value);
        }

        /// <summary>Indicates whether an element can be independenty closed</summary>
        /// <param name="obj">The object the property is set on.</param>
        /// <returns>True or false</returns>
        public static bool GetClosable(DependencyObject obj)
        {
            return (bool) obj.GetValue(ClosableProperty);
        }

        /// <summary>Indicates whether an element can be independenty closed</summary>
        /// <param name="obj">The object the property is set on.</param>
        /// <param name="value">True or false</param>
        public static void SetClosable(DependencyObject obj, bool value)
        {
            obj.SetValue(ClosableProperty, value);
        }

        /// <summary>Command to be triggered when a view (element) is closed</summary>
        /// <param name="obj">The object the property is set on.</param>
        /// <returns>Command or view action</returns>
        /// <remarks>
        ///     Only applies to objects that have the Closable attached property set to true.
        ///     In most cases, this command/action is optional. When no special action is provided, the element's visibility is
        ///     simply set to collapsed.
        /// </remarks>
        public static ICommand GetCloseAction(DependencyObject obj)
        {
            return (ICommand) obj.GetValue(CloseActionProperty);
        }

        /// <summary>Command to be triggered when a view (element) is closed</summary>
        /// <param name="obj">The object the property is set on.</param>
        /// <param name="value">Command or view action</param>
        /// <remarks>
        ///     Only applies to objects that have the Closable attached property set to true.
        ///     In most cases, this command/action is optional. When no special action is provided, the element's visibility is
        ///     simply set to collapsed.
        /// </remarks>
        public static void SetCloseAction(DependencyObject obj, ICommand value)
        {
            obj.SetValue(CloseActionProperty, value);
        }

        /// <summary>
        ///     Handles the <see cref="E:RelativeWidthChanged" /> event.
        /// </summary>
        /// <param name="d">The d.</param>
        /// <param name="e">The <see cref="DependencyPropertyChangedEventArgs" /> instance containing the event data.</param>
        private static void OnRelativeWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var element = d as FrameworkElement;
            if (element == null) return;
            if (element.Parent != null)
            {
                var parentElement = element.Parent as UIElement;
                if (parentElement != null)
                {
                    parentElement.InvalidateMeasure();
                    parentElement.InvalidateArrange();
                }
            }
        }

        /// <summary>Indicates the relative width of an alement compared to other elements</summary>
        /// <param name="obj">The object the property is set on.</param>
        /// <returns>True or false</returns>
        public static GridLength GetRelativeWidth(DependencyObject obj)
        {
            return (GridLength) obj.GetValue(RelativeWidthProperty);
        }

        /// <summary>Indicates the relative width of an alement compared to other elements</summary>
        /// <param name="obj">The object the property is set on.</param>
        /// <param name="value">True or false</param>
        public static void SetRelativeWidth(DependencyObject obj, GridLength value)
        {
            obj.SetValue(RelativeWidthProperty, value);
        }

        /// <summary>
        ///     Handles the <see cref="E:RelativeHeightChanged" /> event.
        /// </summary>
        /// <param name="d">The d.</param>
        /// <param name="e">The <see cref="DependencyPropertyChangedEventArgs" /> instance containing the event data.</param>
        private static void OnRelativeHeightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var element = d as FrameworkElement;
            if (element == null) return;
            if (element.Parent != null)
            {
                var parentElement = element.Parent as UIElement;
                if (parentElement != null)
                {
                    parentElement.InvalidateMeasure();
                    parentElement.InvalidateArrange();
                }
            }
        }

        /// <summary>Indicates the relative height of an alement compared to other elements</summary>
        /// <param name="obj">The object the property is set on.</param>
        /// <returns>True or false</returns>
        public static GridLength GetRelativeHeight(DependencyObject obj)
        {
            return (GridLength) obj.GetValue(RelativeHeightProperty);
        }

        /// <summary>Indicates the relative height of an alement compared to other elements</summary>
        /// <param name="obj">The object the property is set on.</param>
        /// <param name="value">True or false</param>
        public static void SetRelativeHeight(DependencyObject obj, GridLength value)
        {
            obj.SetValue(RelativeHeightProperty, value);
        }

        /// <summary>Invalidates everything in the UI and forces a refresh</summary>
        /// <param name="source">Reference to an instance of the form itself</param>
        private static void InvalidateAllVisuals(DependencyObject source)
        {
            var element = source as UIElement;
            if (element == null) return;

            element.InvalidateArrange();
            element.InvalidateMeasure();
            element.InvalidateVisual();

            var element2 = element as FrameworkElement;
            if (element2 == null || element2.Parent == null) return;
            var element3 = element2.Parent as FrameworkElement;
            if (element3 == null) return;

            element3.InvalidateArrange();
            element3.InvalidateMeasure();
            element3.InvalidateVisual();
        }

        /// <summary>
        ///     Instantiates the specified class defined in the passed assembly, assuming that assembly has that class.
        ///     Otherwise, null is returned.
        /// </summary>
        /// <param name="className">Name of the class (type) to instantiate.</param>
        /// <param name="assembly">Assembly containing the class.</param>
        /// <returns>Object instance or null</returns>
        private static object CreateObject(string className, Assembly assembly)
        {
            try
            {
                var type = assembly.GetType(className, true);
                return Activator.CreateInstance(type);
            }
            catch
            {
            }

            return null;
        }

        /// <summary>
        ///     Loads a named type from an assembly
        /// </summary>
        /// <param name="className">Fully qualified name of the class</param>
        /// <returns>Newly instantiated object</returns>
        private static object CreateObject(string className)
        {
            var entryAssembly = Assembly.GetEntryAssembly();
            var instance = CreateObject(className, entryAssembly);
            if (instance != null) return instance;

            var referencedAssemblyNames = entryAssembly.GetReferencedAssemblies().ToList();
            foreach (var referencedAssemblyName in referencedAssemblyNames)
            {
                var referencedAssembly = Assembly.Load(referencedAssemblyName);
                instance = CreateObject(className, referencedAssembly);
                if (instance != null) return instance;
            }

            return null;
        }
    }

    /// <summary>Identifies various types of UI elements and their significance/type</summary>
    /// <remarks>This is used in different ways by different UI styles</remarks>
    public enum UIElementTypes
    {
        /// <summary>Primary UI element (often used as the element that uses up the majority of space</summary>
        Primary,

        /// <summary>Secondary UI element (often positioned at the side of the primary content)</summary>
        Secondary,

        /// <summary>Auxiliary UI element (often hidden by default)</summary>
        Auxiliary
    }

    /// <summary>Defines the overall sizing trategies for views</summary>
    public enum ViewSizeStrategies
    {
        /// <summary>View uses the minimum size required (auto-sizes to content)</summary>
        UseMinimumSizeRequired,

        /// <summary>Uses all the space the view host will give it</summary>
        UseMaximumSizeAvailable,

        /// <summary>Use the suggested size of possible, otherwise, use maximum size</summary>
        UseSuggestedSize
    }
}
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using CODE.Framework.Wpf.Utilities;

namespace CODE.Framework.Wpf.Layout
{
    /// <summary>
    /// This class produces an outline like a property sheet in Visual Studio
    /// </summary>
    public class PropertySheet : Panel
    {
        private List<ControlPair> _lastControls;
        private double _labelWidth;

        private readonly ScrollBar _scrollVertical = new ScrollBar {Visibility = Visibility.Collapsed, Orientation = Orientation.Vertical, SmallChange = 25, LargeChange = 250};
        private AdornerLayer _adorner;
        private double _lastTotalHeight = -1;

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertySheet"/> class.
        /// </summary>
        public PropertySheet()
        {
            IsHitTestVisible = true;
            ClipToBounds = true;
            Background = Brushes.Transparent;

            if (GroupHeaderRenderer == null) GroupHeaderRenderer = new PropertySheetHeaderRenderer();

            Loaded += (s, e) => CreateScrollbars();
            IsVisibleChanged += (s, e) => HandleScrollBarVisibility(_lastTotalHeight);
            MouseWheel += (s, e) =>
            {
                if (_scrollVertical.Visibility == Visibility.Visible)
                    _scrollVertical.Value -= e.Delta;
            };
        }

        /// <summary>Padding for child elements</summary>
        public Thickness Padding
        {
            get { return (Thickness)GetValue(PaddingProperty); }
            set { SetValue(PaddingProperty, value); }
        }
        /// <summary>Padding for child elements</summary>
        public static readonly DependencyProperty PaddingProperty = DependencyProperty.Register("Padding", typeof(Thickness), typeof(PropertySheet), new PropertyMetadata(new Thickness(0), InvalidateOnChange));

        private static void InvalidateOnChange(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            var sheet = d as PropertySheet;
            if (sheet == null) return;
            sheet.InvalidateMeasure();
            sheet.InvalidateArrange();
            sheet.InvalidateVisual();
        }

        /// <summary>Vertical spacing between rows of elements in the property sheet</summary>
        public double VerticalElementSpacing
        {
            get { return (double)GetValue(VerticalElementSpacingProperty); }
            set { SetValue(VerticalElementSpacingProperty, value); }
        }
        /// <summary>Vertical spacing between rows of elements in the property sheet</summary>
        public static readonly DependencyProperty VerticalElementSpacingProperty = DependencyProperty.Register("VerticalElementSpacing", typeof(double), typeof(PropertySheet), new PropertyMetadata(3d, InvalidateOnChange));

        /// <summary>Horizontal spacing between label and edit elements</summary>
        public double HorizontalElementSpacing
        {
            get { return (double)GetValue(HorizontalElementSpacingProperty); }
            set { SetValue(HorizontalElementSpacingProperty, value); }
        }
        /// <summary>Horizontal spacing between label and edit elements</summary>
        public static readonly DependencyProperty HorizontalElementSpacingProperty = DependencyProperty.Register("HorizontalElementSpacing", typeof(double), typeof(PropertySheet), new PropertyMetadata(3d, InvalidateOnChange));

        /// <summary>Defines the horizontal space between the main edit control and subsequent flow controls</summary>
        /// <value>The additional flow element spacing.</value>
        public double AdditionalFlowElementSpacing
        {
            get { return (double)GetValue(AdditionalFlowElementSpacingProperty); }
            set { SetValue(AdditionalFlowElementSpacingProperty, value); }
        }
        /// <summary>Defines the horizontal space between the main edit control and subsequent flow controls</summary>
        /// <value>The additional flow element spacing.</value>
        public static readonly DependencyProperty AdditionalFlowElementSpacingProperty = DependencyProperty.Register("AdditionalFlowElementSpacing", typeof(double), typeof(PropertySheet), new PropertyMetadata(3d, InvalidateOnChange));

        /// <summary>Object used to render group headers</summary>
        public IPropertySheetHeaderRenderer GroupHeaderRenderer
        {
            get { return (IPropertySheetHeaderRenderer)GetValue(GroupHeaderRendererProperty); }
            set { SetValue(GroupHeaderRendererProperty, value); }
        }
        /// <summary>Object used to render group headers</summary>
        public static readonly DependencyProperty GroupHeaderRendererProperty = DependencyProperty.Register("GroupHeaderRenderer", typeof (IPropertySheetHeaderRenderer), typeof (PropertySheet), new PropertyMetadata(null, InvalidateOnChange));

        /// <summary>If set to true, renders group headers, if group information is set on child elements</summary>
        /// <remarks>Group information should be set on the label elements within the child collection (the odd numbered elements)</remarks>
        public bool ShowGroupHeaders
        {
            get { return (bool)GetValue(ShowGroupHeadersProperty); }
            set { SetValue(ShowGroupHeadersProperty, value); }
        }
        /// <summary>If set to true, renders group headers, if group information is set on child elements</summary>
        /// <remarks>Group information should be set on the label elements within the child collection (the odd numbered elements)</remarks>
        public static readonly DependencyProperty ShowGroupHeadersProperty = DependencyProperty.Register("ShowGroupHeaders", typeof(bool), typeof(PropertySheet), new PropertyMetadata(true, InvalidateOnChange));
        
        /// <summary>
        /// When overridden in a derived class, measures the size in layout required for child elements and determines a size for the <see cref="T:System.Windows.FrameworkElement" />-derived class.
        /// </summary>
        /// <param name="availableSize">The available size that this element can give to child elements. Infinity can be specified as a value to indicate that the element will size to whatever content is available.</param>
        /// <returns>The size that this element determines it needs during layout, based on its calculations of child element sizes.</returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            var availableSizeInternal = availableSize;
            if (Padding.Top > 0 || Padding.Bottom > 0 || Padding.Right > 0 || Padding.Left > 0)
            {
                var availableHeight = availableSize.Height - Padding.Top - Padding.Bottom;
                if (availableHeight < 0d) availableHeight = 0d;
                var availableWidth = availableSize.Width - Padding.Left - Padding.Right;
                if (availableWidth < 0d) availableWidth = 0d;
                availableSizeInternal = GeometryHelper.NewSize(availableWidth, availableHeight);
            }

            _lastControls = GetControls();
            InvalidateVisual();

            var widestLabel = 0d;
            var totalHeight = 0d;

            var foundGroupHeader = false;
            var groupPadding = new Thickness();
            if (ShowGroupHeaders && GroupHeaderRenderer != null) foundGroupHeader = _lastControls.Where(c => c.Label != null).Any(control => !string.IsNullOrEmpty(SimpleView.GetGroupTitle(control.Label)));
            if (foundGroupHeader)
            {
                groupPadding = GroupHeaderRenderer.GetHeaderPaddingUsedForRendering("X");
                availableSizeInternal = GeometryHelper.NewSize(Math.Max(availableSizeInternal.Width - groupPadding.Left, 0), Math.Max(availableSizeInternal.Height, 0));
            }

            foreach (var control in _lastControls.Where(c => c.Label != null))
            {
                control.Label.Measure(availableSizeInternal);
                if (!control.LabelSpansFullWidth) // For widest-label measurements, we do not consider span labels, since they always use the full width
                    widestLabel = Math.Max(control.Label.DesiredSize.Width, widestLabel);
            }
            foreach (var control in _lastControls.Where(c => !c.LabelSpansFullWidth && c.Edit != null))
            {
                var currentMaxWidth = SnapToPixel(availableSizeInternal.Width - widestLabel - HorizontalElementSpacing - groupPadding.Left);
                for (var flowControlCounter = control.AdditionalEditControls.Count - 1; flowControlCounter >= 0; flowControlCounter--)
                {
                    var flowControl = control.AdditionalEditControls[flowControlCounter];
                    var availableEditSize = GeometryHelper.NewSize(currentMaxWidth, availableSize.Height);
                    flowControl.Measure(availableEditSize);
                    currentMaxWidth = Math.Max(SnapToPixel(currentMaxWidth - flowControl.DesiredSize.Width), 0);
                }
                control.Edit.Measure(GeometryHelper.NewSize(currentMaxWidth, availableSize.Height));
            }

            var currentGroupIsExpanded = true;
            foreach (var control in _lastControls)
            {
                var itemHeight = 0d;
                if (control.Label != null) itemHeight = control.Label.DesiredSize.Height;
                if (control.Edit != null && !control.LabelSpansFullWidth)
                {
                    itemHeight = Math.Max(itemHeight, control.Edit.DesiredSize.Height);
                    foreach (var flowControl in control.AdditionalEditControls)
                        itemHeight = Math.Max(itemHeight, flowControl.DesiredSize.Height);
                }

                itemHeight = SnapToPixel(itemHeight);

                if (ShowGroupHeaders && control.Label != null && GroupHeaderRenderer != null && SimpleView.GetGroupBreak(control.Label))
                {
                    var groupHeader = SimpleView.GetGroupTitle(control.Label) ?? string.Empty;
                    if (!_renderedGroups.ContainsKey(groupHeader)) _renderedGroups.Add(groupHeader, new PropertySheetGroupHeaderInfo {IsExpanded = true});
                    currentGroupIsExpanded = _renderedGroups[groupHeader].IsExpanded;
                    var currentPadding = GroupHeaderRenderer.GetHeaderPaddingUsedForRendering(string.IsNullOrEmpty(groupHeader) ? "X" : groupHeader);
                    _renderedGroups[groupHeader].Height = currentPadding.Top;
                    totalHeight += SnapToPixel(currentPadding.Top);
                }

                if (currentGroupIsExpanded) totalHeight += itemHeight + VerticalElementSpacing;
            }

            _labelWidth = widestLabel;
            totalHeight += Padding.Top + Padding.Bottom;
            _lastTotalHeight = totalHeight;

            HandleScrollBarVisibility(totalHeight, availableSize);

            if (_scrollVertical.Visibility == Visibility.Visible)
            {
                foreach (var control in _lastControls.Where(c => !c.LabelSpansFullWidth && c.Edit != null))
                {
                    var currentMaxWidth = SnapToPixel(SnapToPixel(availableSizeInternal.Width - widestLabel - HorizontalElementSpacing - SystemParameters.VerticalScrollBarWidth - 1));
                    for (var flowControlCounter = control.AdditionalEditControls.Count - 1; flowControlCounter >= 0; flowControlCounter--)
                    {
                        var flowControl = control.AdditionalEditControls[flowControlCounter];
                        var availableEditSize = GeometryHelper.NewSize(currentMaxWidth, availableSize.Height);
                        flowControl.Measure(availableEditSize);
                        currentMaxWidth = Math.Max(SnapToPixel(currentMaxWidth - flowControl.DesiredSize.Width), 0);
                    }
                    control.Edit.Measure(GeometryHelper.NewSize(currentMaxWidth, availableSize.Height));
                }
            }

            return GeometryHelper.NewSize(availableSize.Width, Math.Min(totalHeight, availableSize.Height));
        }

        private readonly Dictionary<string, PropertySheetGroupHeaderInfo> _renderedGroups = new Dictionary<string, PropertySheetGroupHeaderInfo>();

        private class PropertySheetGroupHeaderInfo
        {
            public bool IsExpanded { get; set; }
            public double LastRenderTopPosition { get; set; }
            public double Height { get; set; }
        }

        private void HandleScrollBarVisibility(double totalHeight, Size availableSize = new Size())
        {
            if (availableSize.Height < .1d && availableSize.Width < .1d)
                availableSize = GeometryHelper.NewSize(ActualWidth, ActualHeight);

            if (!IsVisible)
            {
                if (_scrollVertical.Visibility == Visibility.Visible)
                {
                    _scrollVertical.Visibility = Visibility.Collapsed;
                    InvalidateMeasure();
                    InvalidateArrange();
                    InvalidateVisual();
                }
                return;
            }

            if (double.IsInfinity(availableSize.Height)) _scrollVertical.Visibility = Visibility.Collapsed;
            else if (totalHeight > availableSize.Height)
            {
                if (_scrollVertical.Visibility != Visibility.Visible)
                {
                    _scrollVertical.Visibility = Visibility.Visible;
                    InvalidateMeasure();
                    InvalidateArrange();
                    InvalidateVisual();
                }
                _scrollVertical.Maximum = totalHeight - availableSize.Height;
                _scrollVertical.ViewportSize = availableSize.Height;
            }
            else
                _scrollVertical.Visibility = Visibility.Collapsed;
        }

        private void CreateScrollbars()
        {
            _adorner = AdornerLayer.GetAdornerLayer(this);
            if (_adorner == null) return;
            _adorner.Add(new PropertySheetScrollAdorner(this, _scrollVertical) { Visibility = Visibility.Visible });
            _scrollVertical.ValueChanged += (s, e) => DispatchInvalidateScroll();
        }

        private void DispatchInvalidateScroll()
        {
            InvalidateMeasure();
            InvalidateArrange();
            InvalidateVisual();
        }

        /// <summary>
        /// When overridden in a derived class, positions child elements and determines a size for a <see cref="T:System.Windows.FrameworkElement" /> derived class.
        /// </summary>
        /// <param name="finalSize">The final area within the parent that this element should use to arrange itself and its children.</param>
        /// <returns>The actual size used.</returns>
        protected override Size ArrangeOverride(Size finalSize)
        {
            var finalSizeInternal = finalSize;
            if (Padding.Top > 0 || Padding.Bottom > 0 || Padding.Right > 0 || Padding.Left > 0)
            {
                var availableHeight = finalSize.Height - Padding.Top - Padding.Bottom;
                if (availableHeight < 0d) availableHeight = 0d;
                var availableWidth = finalSize.Width - Padding.Left - Padding.Right;
                if (availableWidth < 0d) availableWidth = 0d;
                finalSizeInternal = GeometryHelper.NewSize(availableWidth, availableHeight);
            }

            if (_lastControls == null) return base.ArrangeOverride(finalSize);

            var finalWidth = finalSizeInternal.Width;
            if (_scrollVertical.Visibility == Visibility.Visible) finalWidth = Math.Max(finalWidth - SystemParameters.VerticalScrollBarWidth, 1);

            var offsetTop = 0d;
            if (_scrollVertical.Visibility == Visibility.Visible) offsetTop = _scrollVertical.Value*-1;

            var currentTop = offsetTop + Padding.Top;
            if (!ShowGroupHeaders || GroupHeaderRenderer == null)
                foreach (var control in _lastControls)
                {
                    var lineHeight = 0d;
                    if (control.Label != null) lineHeight = control.Label.DesiredSize.Height;
                    if (control.Edit != null) lineHeight = Math.Max(lineHeight, control.Edit.DesiredSize.Height);
                    lineHeight = SnapToPixel(lineHeight);

                    if (control.Label != null)
                    {
                        if (!control.LabelSpansFullWidth)
                            control.Label.Arrange(GeometryHelper.NewRect(Padding.Left, currentTop, _labelWidth, lineHeight));
                        else
                            control.Label.Arrange(GeometryHelper.NewRect(Padding.Left, currentTop, SnapToPixel(finalWidth), lineHeight));
                    }
                    if (control.Edit != null && !control.LabelSpansFullWidth)
                    {
                        var editLeft = SnapToPixel(_labelWidth + HorizontalElementSpacing + Padding.Left);
                        var editWidth = Math.Max(SnapToPixel(finalWidth - editLeft + Padding.Left), 0);

                        for (var flowControlCounter = control.AdditionalEditControls.Count - 1; flowControlCounter >= 0; flowControlCounter--)
                        {
                            var flowControl = control.AdditionalEditControls[flowControlCounter];
                            var flowWidth = Math.Min(flowControl.DesiredSize.Width, editWidth);
                            flowControl.Arrange(GeometryHelper.NewRect(editLeft + editWidth - flowWidth, currentTop, flowWidth, lineHeight));
                            editWidth -= (flowWidth + AdditionalFlowElementSpacing);
                            if (editWidth < 0.1) editWidth = 0d;
                        }

                        control.Edit.Arrange(GeometryHelper.NewRect(editLeft, currentTop, editWidth, lineHeight));
                    }
                    currentTop += lineHeight + VerticalElementSpacing;
                }
            else
            {
                var itemIndent = GroupHeaderRenderer.GetHeaderPaddingUsedForRendering("X").Left;
                var currentGroupIsExpanded = true;
                foreach (var control in _lastControls)
                {
                    var lineHeight = 0d;
                    if (control.Label != null) lineHeight = control.Label.DesiredSize.Height;
                    if (control.Edit != null) lineHeight = Math.Max(lineHeight, control.Edit.DesiredSize.Height);
                    lineHeight = SnapToPixel(lineHeight);

                    if (control.Label != null && SimpleView.GetGroupBreak(control.Label))
                    {
                        var groupTitle = SimpleView.GetGroupTitle(control.Label);
                        if (_renderedGroups.ContainsKey(groupTitle))
                        {
                            currentGroupIsExpanded = _renderedGroups[groupTitle].IsExpanded;
                            currentTop += _renderedGroups[groupTitle].Height;
                        }
                    }

                    if (currentGroupIsExpanded)
                    {
                        if (control.Label != null)
                        {
                            control.Label.Visibility = Visibility.Visible;
                            if (!control.LabelSpansFullWidth)
                                control.Label.Arrange(GeometryHelper.NewRect(Padding.Left + itemIndent, currentTop, _labelWidth, lineHeight));
                            else
                                control.Label.Arrange(GeometryHelper.NewRect(Padding.Left + itemIndent, currentTop, SnapToPixel(finalWidth - itemIndent), lineHeight));
                        }
                        if (control.Edit != null)
                        {
                            control.Edit.Visibility = Visibility.Visible;
                            var editLeft = SnapToPixel(_labelWidth + HorizontalElementSpacing + Padding.Left + itemIndent);
                            var editWidth = Math.Max(SnapToPixel(finalWidth - editLeft + Padding.Left), 0);

                            for (var flowControlCounter = control.AdditionalEditControls.Count - 1; flowControlCounter >= 0; flowControlCounter--)
                            {
                                var flowControl = control.AdditionalEditControls[flowControlCounter];
                                var flowWidth = Math.Min(flowControl.DesiredSize.Width, editWidth);
                                flowControl.Visibility = Visibility.Visible;
                                flowControl.Arrange(GeometryHelper.NewRect(editLeft + editWidth - flowWidth, currentTop, flowWidth, lineHeight));
                                editWidth -= (flowWidth + AdditionalFlowElementSpacing);
                                if (editWidth < 0.1) editWidth = 0d;
                            }

                            control.Edit.Arrange(GeometryHelper.NewRect(editLeft, currentTop, editWidth, lineHeight));
                        }
                        currentTop += lineHeight + VerticalElementSpacing;
                    }
                    else
                    {
                        if (control.Label != null) control.Label.Visibility = Visibility.Collapsed;
                        if (control.Edit != null) control.Edit.Visibility = Visibility.Collapsed;
                        foreach (var flowControl in control.AdditionalEditControls)
                            flowControl.Visibility = Visibility.Collapsed;
                    }
                }
            }

            return base.ArrangeOverride(finalSize);
        }

        private static double SnapToPixel(double number)
        {
            if ((int) number < number) number = (int) number + 1d;
            if (number <= 0.1d) number = 0d;
            return number;
        }

        /// <summary>
        /// Gets the controls in pairs.
        /// </summary>
        /// <returns>List&lt;ControlPair&gt;.</returns>
        private List<ControlPair> GetControls()
        {
            var controls = new List<ControlPair>();
            for (var controlCounter = 0; controlCounter < Children.Count; controlCounter++)
            {
                var child = Children[controlCounter];
                if (child.Visibility == Visibility.Collapsed) continue;

                var controlPair = new ControlPair(0d);

                if (SimpleView.GetIsStandAloneEditControl(child))
                    controlPair.Edit = child;
                else
                {
                    controlPair.Label = child;
                    if (SimpleView.GetSpanFullWidth(child))
                        controlPair.LabelSpansFullWidth = true;
                    else if (!SimpleView.GetIsStandAloneLabel(child))
                    {
                        var editControlIndex = controlCounter + 1;
                        if (Children.Count > editControlIndex)
                            controlPair.Edit = Children[editControlIndex];
                        controlCounter++; // We are skipping the next control since we already accounted for it

                        while (true) // We check if the next control might flow with the current edit control
                        {
                            if (Children.Count <= controlCounter + 1) break;
                            if (!SimpleView.GetFlowsWithPrevious(Children[controlCounter + 1])) break;
                            controlPair.AdditionalEditControls.Add(Children[controlCounter + 1]);
                            controlCounter++;
                        }
                    }
                }
                controls.Add(controlPair);
            }
            return controls;
        }

        /// <summary>
        /// Invoked when an unhandled MouseDown attached event reaches an element in its route that is derived from this class. Implement this method to add class handling for this event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.Windows.Input.MouseButtonEventArgs" /> that contains the event data. This event data reports details about the mouse button that was pressed and the handled state.</param>
        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && e.ClickCount == 1)
                foreach (var control in _lastControls)
                    if (control.Label != null && SimpleView.GetGroupBreak(control.Label))
                    {
                        var groupTitle = SimpleView.GetGroupTitle(control.Label);
                        if (_renderedGroups.ContainsKey(groupTitle))
                        {
                            var group = _renderedGroups[groupTitle];
                            var y = group.LastRenderTopPosition;
                            var positionY = e.GetPosition(this).Y;
                            if (positionY >= y && positionY <= y + group.Height)
                            {
                                e.Handled = true;
                                group.IsExpanded = !group.IsExpanded;
                                InvalidateMeasure();
                                InvalidateArrange();
                                InvalidateVisual();
                                return;
                            }
                        }
                    }

            base.OnMouseDown(e);
        }

        /// <summary>
        /// Draws the content of a <see cref="T:System.Windows.Media.DrawingContext" /> object during the render pass of a <see cref="T:System.Windows.Controls.Panel" /> element.
        /// </summary>
        /// <param name="dc">The <see cref="T:System.Windows.Media.DrawingContext" /> object to draw.</param>
        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            if (!ShowGroupHeaders || GroupHeaderRenderer == null) return;

            var offsetTop = 0d;
            if (_scrollVertical.Visibility == Visibility.Visible) offsetTop = _scrollVertical.Value * -1;

            var currentTop = offsetTop + Padding.Top;
            var currentGroupIsExpanded = true;
            foreach (var control in _lastControls)
            {
                var lineHeight = 0d;
                if (control.Label != null) lineHeight = control.Label.DesiredSize.Height;
                if (control.Edit != null) lineHeight = Math.Max(lineHeight, control.Edit.DesiredSize.Height);
                lineHeight = SnapToPixel(lineHeight);

                if (control.Label != null && SimpleView.GetGroupBreak(control.Label))
                {
                    var groupTitle = SimpleView.GetGroupTitle(control.Label);
                    if (_renderedGroups.ContainsKey(groupTitle))
                    {
                        currentGroupIsExpanded = _renderedGroups[groupTitle].IsExpanded;

                        var group = _renderedGroups[groupTitle];
                        var width = ActualWidth - Padding.Left - Padding.Right;
                        if (_scrollVertical.Visibility == Visibility.Visible) width -= SystemParameters.VerticalScrollBarWidth;
                        dc.PushTransform(new TranslateTransform(Padding.Left, currentTop));
                        GroupHeaderRenderer.RenderHeader(dc, currentTop, width, groupTitle, group.IsExpanded);
                        dc.Pop();
                        group.LastRenderTopPosition = currentTop;

                        currentTop += _renderedGroups[groupTitle].Height;
                    }
                }

                if (currentGroupIsExpanded) currentTop += lineHeight + VerticalElementSpacing;
            }
        }
    }

    /// <summary>Adorner UI for scrollbars of the edit form control</summary>
    public class PropertySheetScrollAdorner : Adorner
    {
        private readonly ScrollBar _vertical;

        /// <summary>Constructor</summary>
        /// <param name="adornedElement">Adorned element PropertySheet</param>
        /// <param name="vertical">The vertical scrollbar.</param>
        public PropertySheetScrollAdorner(PropertySheet adornedElement, ScrollBar vertical) : base(adornedElement)
        {
            _vertical = vertical;
            CheckControlForExistingParent(_vertical);
            AddVisualChild(_vertical);
        }

        /// <summary>Implements any custom measuring behavior for the adorner.</summary>
        /// <param name="constraint">A size to constrain the adorner to.</param>
        /// <returns>A <see cref="T:System.Windows.Size"/> object representing the amount of layout space needed by the adorner.</returns>
        protected override Size MeasureOverride(Size constraint)
        {
            _vertical.Measure(constraint);

            if (!double.IsInfinity(constraint.Height) && !double.IsInfinity(constraint.Width))
                return constraint;
            return new Size(100000, 100000);
        }

        /// <summary>When overridden in a derived class, positions child elements and determines a size for a <see cref="T:System.Windows.FrameworkElement"/> derived class.</summary>
        /// <param name="finalSize">The final area within the parent that this element should use to arrange itself and its children.</param>
        /// <returns>The actual size used.</returns>
        protected override Size ArrangeOverride(Size finalSize)
        {
            var surfaceSize = AdornedElement.RenderSize;

            if (_vertical.Visibility == Visibility.Visible)
                _vertical.Arrange(GeometryHelper.NewRect(surfaceSize.Width - SystemParameters.VerticalScrollBarWidth, 0, SystemParameters.VerticalScrollBarWidth, surfaceSize.Height));

            return finalSize;
        }

        private void CheckControlForExistingParent(FrameworkElement element)
        {
            DependencyObject parent = null;
            if (element.Parent != null) parent = element.Parent;
            if (parent == null)
                parent = VisualTreeHelper.GetParent(element);
            if (parent != null)
            {
                var parentContent = parent as ContentControl;
                if (parentContent != null)
                {
                    parentContent.Content = null;
                    return;
                }
                var panelContent = parent as Panel;
                if (panelContent != null)
                {
                    panelContent.Children.Remove(element);
                    return;
                }
                var adorner = parent as PropertySheetScrollAdorner;
                if (adorner != null)
                    adorner.RemoveVisualChild(element);
            }
        }

        /// <summary>Gets the number of visual child elements within this element.</summary>
        /// <returns>The number of visual child elements for this element.</returns>
        protected override int VisualChildrenCount
        {
            get { return 1; }
        }

        /// <summary>Overrides <see cref="M:System.Windows.Media.Visual.GetVisualChild(System.Int32)"/>, and returns a child at the specified index from a collection of child elements.</summary>
        /// <param name="index">The zero-based index of the requested child element in the collection.</param>
        /// <returns>The requested child element. This should not return null; if the provided index is out of range, an exception is thrown.</returns>
        protected override Visual GetVisualChild(int index)
        {
            return _vertical;
        }
    }

    /// <summary>Fundamental interface for a header renderer for the property sheet</summary>
    public interface IPropertySheetHeaderRenderer
    {
        /// <summary>Returns the spacing used by header rendering</summary>
        /// <param name="headerText">Header text to be rendered</param>
        /// <returns>Margin used (note: only top and left are respected)</returns>
        Thickness GetHeaderPaddingUsedForRendering(string headerText);

        /// <summary>Performs the actual header rendering</summary>
        /// <param name="dc">DrawingContext</param>
        /// <param name="top">Indicates the current top render position</param>
        /// <param name="actualWidth">Maximum actual width of the render area</param>
        /// <param name="headerText">Header text that is to be rendered</param>
        /// <param name="isExpanded">Indicates whether the header is expanded (or collapsed)</param>
        void RenderHeader(DrawingContext dc, double top, double actualWidth, string headerText, bool isExpanded);
    }

    /// <summary>Renderer object for headers on the property sheet renderer</summary>
    public class PropertySheetHeaderRenderer : FrameworkElement, IPropertySheetHeaderRenderer
    {
        /// <summary>Font family</summary>
        public FontFamily FontFamily
        {
            get { return (FontFamily)GetValue(FontFamilyProperty); }
            set { SetValue(FontFamilyProperty, value); }
        }
        /// <summary>Font family</summary>
        public static readonly DependencyProperty FontFamilyProperty = DependencyProperty.Register("FontFamily", typeof(FontFamily), typeof(PropertySheetHeaderRenderer), new PropertyMetadata(new FontFamily("Segoe UI")));

        /// <summary>Font size</summary>
        public double FontSize
        {
            get { return (double)GetValue(FontSizeProperty); }
            set { SetValue(FontSizeProperty, value); }
        }
        /// <summary>Font size</summary>
        public static readonly DependencyProperty FontSizeProperty = DependencyProperty.Register("FontSize", typeof(double), typeof(PropertySheetHeaderRenderer), new PropertyMetadata(16d));

        /// <summary>Font style</summary>
        public FontStyle FontStyle
        {
            get { return (FontStyle)GetValue(FontStyleProperty); }
            set { SetValue(FontStyleProperty, value); }
        }
        /// <summary>Font style</summary>
        public static readonly DependencyProperty FontStyleProperty = DependencyProperty.Register("FontStyle", typeof(FontStyle), typeof(PropertySheetHeaderRenderer), new PropertyMetadata(FontStyles.Normal));

        /// <summary>Font weight</summary>
        public FontWeight FontWeight
        {
            get { return (FontWeight)GetValue(FontWeightProperty); }
            set { SetValue(FontWeightProperty, value); }
        }
        /// <summary>Font weight</summary>
        public static readonly DependencyProperty FontWeightProperty = DependencyProperty.Register("FontWeight", typeof(FontWeight), typeof(PropertySheetHeaderRenderer), new PropertyMetadata(FontWeights.Normal));

        /// <summary>Foreground brush</summary>
        public Brush Foreground
        {
            get { return (Brush)GetValue(ForegroundProperty); }
            set { SetValue(ForegroundProperty, value); }
        }
        /// <summary>Foreground brush</summary>
        public static readonly DependencyProperty ForegroundProperty = DependencyProperty.Register("Foreground", typeof(Brush), typeof(PropertySheetHeaderRenderer), new PropertyMetadata(Brushes.Black));

        /// <summary>Background brush</summary>
        public Brush Background
        {
            get { return (Brush)GetValue(BackgroundProperty); }
            set { SetValue(BackgroundProperty, value); }
        }
        /// <summary>Background brush</summary>
        public static readonly DependencyProperty BackgroundProperty = DependencyProperty.Register("Background", typeof(Brush), typeof(PropertySheetHeaderRenderer), new PropertyMetadata(null));

        /// <summary>Indentation for all the items when the property sheet shows groups</summary>
        public double ItemIndentation
        {
            get { return (double)GetValue(ItemIndentationProperty); }
            set { SetValue(ItemIndentationProperty, value); }
        }
        /// <summary>Indentation for all the items when the property sheet shows groups</summary>
        public static readonly DependencyProperty ItemIndentationProperty = DependencyProperty.Register("ItemIndentation", typeof(double), typeof(PropertySheetHeaderRenderer), new PropertyMetadata(20d));

        /// <summary>Brush to render an expanded icon</summary>
        public Brush ExpandedIcon
        {
            get { return (Brush)GetValue(ExpandedIconProperty); }
            set { SetValue(ExpandedIconProperty, value); }
        }
        /// <summary>Brush to render an expanded icon</summary>
        public static readonly DependencyProperty ExpandedIconProperty = DependencyProperty.Register("ExpandedIcon", typeof(Brush), typeof(PropertySheetHeaderRenderer), new PropertyMetadata(null));

        /// <summary>Brush to render an collapsed icon</summary>
        public Brush CollapsedIcon
        {
            get { return (Brush)GetValue(CollapsedIconProperty); }
            set { SetValue(CollapsedIconProperty, value); }
        }
        /// <summary>Brush to render an collapsed icon</summary>
        public static readonly DependencyProperty CollapsedIconProperty = DependencyProperty.Register("CollapsedIcon", typeof(Brush), typeof(PropertySheetHeaderRenderer), new PropertyMetadata(null));

        /// <summary>Returns the spacing used by header rendering</summary>
        /// <param name="headerText">Header text to be rendered</param>
        /// <returns>Margin used (note: only top and left are respected)</returns>
        public Thickness GetHeaderPaddingUsedForRendering(string headerText)
        {
            var ft = new FormattedText(headerText, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface(FontFamily, FontStyle, FontWeight, FontStretches.Normal), FontSize, Foreground) {MaxLineCount = 1, MaxTextWidth = 100000};
            return new Thickness(ItemIndentation, ft.Height + 5, 0d, 0d);
        }

        /// <summary>Performs the actual header rendering</summary>
        /// <param name="dc">DrawingContext</param>
        /// <param name="top">Indicates the current top render position</param>
        /// <param name="actualWidth">Maximum actual width of the render area</param>
        /// <param name="headerText">Header text that is to be rendered</param>
        /// <param name="isExpanded">Indicates whether the header is expanded (or collapsed)</param>
        public void RenderHeader(DrawingContext dc, double top, double actualWidth, string headerText, bool isExpanded)
        {
            var internalText = headerText;
            if (string.IsNullOrEmpty(internalText)) internalText = "X";
            var areaHeight = GetHeaderPaddingUsedForRendering(internalText).Top;
            if (areaHeight < 1 || actualWidth < 1) return;
            if (Background != null) dc.DrawRectangle(Background, null, GeometryHelper.NewRect(0, 2, actualWidth, areaHeight - 5));
            var ft = new FormattedText(headerText, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface(FontFamily, FontStyle, FontWeight, FontStretches.Normal), FontSize, Foreground) {MaxLineCount = 1, MaxTextWidth = Math.Max(actualWidth - ItemIndentation - 5, 0), Trimming = TextTrimming.CharacterEllipsis};

            var heightWidth = ItemIndentation - 10;
            if (heightWidth > areaHeight - 2) heightWidth = areaHeight - 2;
            var iconRect = GeometryHelper.NewRect(2, (int)((areaHeight - heightWidth) / 2), heightWidth, heightWidth);

            var interactionBrush = isExpanded ? GetExpandedIconBrush() : GetCollapsedIconBrush();
            dc.DrawRectangle(interactionBrush, null, iconRect);

            dc.DrawText(ft, new Point(ItemIndentation, 0d));
        }

        private Brush GetCollapsedIconBrush()
        {
            if (CollapsedIcon == null)
            {
                var resource = Application.Current.TryFindResource("CODE.Framework-Icon-Collapsed");
                if (resource != null) CollapsedIcon = resource as Brush;
            }
            return CollapsedIcon ?? (CollapsedIcon = Brushes.Red);
        }

        private Brush GetExpandedIconBrush()
        {
            if (ExpandedIcon == null)
            {
                var resource = Application.Current.TryFindResource("CODE.Framework-Icon-Expanded");
                if (resource != null) ExpandedIcon = resource as Brush;
            }
            return ExpandedIcon ?? (ExpandedIcon = Brushes.Black);
        }
    }
}

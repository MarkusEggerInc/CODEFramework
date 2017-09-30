using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using CODE.Framework.Wpf.Utilities;

namespace CODE.Framework.Wpf.Layout
{
    /// <summary>
    /// This control behaves much like a stack panel but arranges items not just top-to-bottom or left-to-right,
    /// but it can at the same time arrange items at the opposite end of the control based on horizontal and
    /// vertical alignment settings
    /// </summary>
    public class BidirectionalStackPanel : StackPanel
    {
        /// <summary>Additional margin added to each child item in this stack</summary>
        public static readonly DependencyProperty ChildItemMarginProperty = DependencyProperty.Register("ChildItemMargin", typeof (Thickness), typeof (BidirectionalStackPanel), new FrameworkPropertyMetadata(new Thickness(0)) {AffectsArrange = true, AffectsMeasure = true});

        /// <summary>Defines whether the extra bottom child item margin for text elements (labels and textblocks) should be ignored</summary>
        public static readonly DependencyProperty IgnoreChildItemBottomMarginForTextElementsProperty = DependencyProperty.Register("IgnoreChildItemBottomMarginForTextElements", typeof (bool), typeof (BidirectionalStackPanel), new FrameworkPropertyMetadata(true) {AffectsArrange = true, AffectsMeasure = true});

        /// <summary>Indicates whether the last top (or left) item fills the remaining space</summary>
        public static readonly DependencyProperty LastTopItemFillsSpaceProperty = DependencyProperty.Register("LastTopItemFillsSpace", typeof (bool), typeof (BidirectionalStackPanel), new FrameworkPropertyMetadata(false) {AffectsArrange = true, AffectsMeasure = true});

        /// <summary>Defines how much of a margin is to be left below the last top item in case the last top item is set to fill the available space</summary>
        public static readonly DependencyProperty LastTopItemFillMarginProperty = DependencyProperty.Register("LastTopItemFillMargin", typeof (double), typeof (BidirectionalStackPanel), new FrameworkPropertyMetadata(10d) {AffectsArrange = true, AffectsMeasure = true});

        /// <summary>Defines whether the stack panel automatically handles scrollbars</summary>
        public static readonly DependencyProperty ScrollBarModeProperty = DependencyProperty.Register("ScrollBarMode", typeof (BidirectionalStackPanelScrollBarModes), typeof (BidirectionalStackPanel), new FrameworkPropertyMetadata(BidirectionalStackPanelScrollBarModes.Automatic) {AffectsArrange = true, AffectsMeasure = true});

        /// <summary>Style used by the scroll bars</summary>
        public Style ScrollBarStyle
        {
            get { return (Style) GetValue(ScrollBarStyleProperty); }
            set { SetValue(ScrollBarStyleProperty, value); }
        }

        /// <summary>Style used by the scroll bars</summary>
        public static readonly DependencyProperty ScrollBarStyleProperty = DependencyProperty.Register("ScrollBarStyle", typeof (Style), typeof (BidirectionalStackPanel), new PropertyMetadata(null, ScrollBarStyleChanged));

        /// <summary>
        /// This method gets called whenever the ScrollBarStyle property is updated
        /// </summary>
        /// <param name="d">Dependency object</param>
        /// <param name="args">Arguments</param>
        private static void ScrollBarStyleChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            var panel = d as BidirectionalStackPanel;
            if (panel == null) return;
            var style = args.NewValue as Style;
            panel._scrollHorizontal.Style = style;
            panel._scrollVertical.Style = style;
        }

        /// <summary>Vertical additional spacing before new groups</summary>
        public double GroupSpacing
        {
            get { return (double)GetValue(GroupSpacingProperty); }
            set { SetValue(GroupSpacingProperty, value); }
        }
        /// <summary>Vertical additional spacing before new groups</summary>
        public static readonly DependencyProperty GroupSpacingProperty = DependencyProperty.Register("GroupSpacing", typeof(double), typeof(BidirectionalStackPanel), new UIPropertyMetadata(15d, (s, e) => InvalidateAllVisuals(s)));

        private readonly ScrollBar _scrollHorizontal = new ScrollBar {Visibility = Visibility.Collapsed, Orientation = Orientation.Horizontal, SmallChange = 10};
        private readonly ScrollBar _scrollVertical = new ScrollBar {Visibility = Visibility.Collapsed, Orientation = Orientation.Vertical, SmallChange = 10};
        private AdornerLayer _adorner;
        private bool _scrollbarsCreated;

        /// <summary>Constructor</summary>
        public BidirectionalStackPanel()
        {
            VerticalAlignment = VerticalAlignment.Stretch;
            HorizontalAlignment = HorizontalAlignment.Stretch;
            ClipToBounds = true;
            IsHitTestVisible = true;
            Background = Brushes.Transparent;

            Loaded += (s, e) => CreateScrollbars();
        }

        /// <summary>
        /// Invoked when an unhandled <see cref="E:System.Windows.Input.Mouse.PreviewMouseWheel" /> attached event reaches an element in its route that is derived from this class. Implement this method to add class handling for this event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.Windows.Input.MouseWheelEventArgs" /> that contains the event data.</param>
        protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
        {
            if (_scrollVertical.Visibility == Visibility.Visible)
                if (e.Delta < 0)
                    _scrollVertical.Value += _scrollVertical.SmallChange;
                else
                    _scrollVertical.Value -= _scrollVertical.SmallChange;

            base.OnPreviewMouseWheel(e);
        }

        private void CreateScrollbars()
        {
            if (Parent != null && !(Parent is AdornerDecorator) && (Parent is Grid || Parent is SimpleView || Parent is ContentControl))
            {
                // We need to make sure this control is directly within an adorner decorator
                // so elements in the adorner layer (scroll bars) behave correctly in the Z-Order
                var decorator = new AdornerDecorator
                {
                    VerticalAlignment = VerticalAlignment,
                    HorizontalAlignment = HorizontalAlignment,
                    Margin = new Thickness(Margin.Left, Margin.Top, Margin.Right, Margin.Bottom),
                    Width = Width,
                    Height = Height
                };

                Grid.SetColumn(decorator, Grid.GetColumn(this));
                Grid.SetRow(decorator, Grid.GetRow(this));
                Grid.SetColumnSpan(decorator, Grid.GetColumnSpan(this));
                Grid.SetRowSpan(decorator, Grid.GetRowSpan(this));

                SimpleView.SetUIElementTitle(decorator, SimpleView.GetUIElementTitle(this));
                SimpleView.SetUIElementType(decorator, SimpleView.GetUIElementType(this));

                Margin = new Thickness(0d);
                VerticalAlignment = VerticalAlignment.Stretch;
                HorizontalAlignment = HorizontalAlignment.Stretch;

                var parentPanel = Parent as Panel;
                if (parentPanel != null)
                {
                    var childItemIndex = -1;
                    var found = false;
                    foreach (var child in parentPanel.Children)
                    {
                        childItemIndex++;
                        if (child == this)
                        {
                            found = true;
                            break;
                        }
                    }
                    if (found)
                    {
                        parentPanel.Children.Remove(this);
                        parentPanel.Children.Insert(childItemIndex, decorator);
                        decorator.Child = this;
                    }
                }
                else
                {
                    var parentContent = Parent as ContentControl;
                    if (parentContent != null)
                    {
                        parentContent.Content = null;
                        parentContent.Content = decorator;
                        decorator.Child = this;
                    }
                }
            }

            if (_scrollbarsCreated) return;
            _adorner = AdornerLayer.GetAdornerLayer(this);
            if (_adorner == null) return;
            _adorner.Add(new BidirectionalStackPanelScrollAdorner(this, _scrollHorizontal, _scrollVertical) {Visibility = Visibility.Visible});
            _scrollHorizontal.ValueChanged += (s, e) => Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(DispatchInvalidateScroll));
            _scrollVertical.ValueChanged += (s, e) => Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(DispatchInvalidateScroll));
            _scrollbarsCreated = true;
        }

        private void DispatchInvalidateScroll()
        {
            InvalidateArrange();
            InvalidateVisual();
        }

        /// <summary>Additional margin added to each child item in this stack</summary>
        /// <remarks>Note that this margin is added to whatever native margin the control may already have</remarks>
        /// <value>The child item margin.</value>
        public Thickness ChildItemMargin
        {
            get { return (Thickness) GetValue(ChildItemMarginProperty); }
            set { SetValue(ChildItemMarginProperty, value); }
        }

        /// <summary>Defines whether the extra bottom child item margin for text elements (labels and textblocks) should be ignored</summary>
        public bool IgnoreChildItemBottomMarginForTextElements
        {
            get { return (bool) GetValue(IgnoreChildItemBottomMarginForTextElementsProperty); }
            set { SetValue(IgnoreChildItemBottomMarginForTextElementsProperty, value); }
        }

        /// <summary>Indicates whether the last top (or left) item fills the remaining space</summary>
        public bool LastTopItemFillsSpace
        {
            get { return (bool) GetValue(LastTopItemFillsSpaceProperty); }
            set { SetValue(LastTopItemFillsSpaceProperty, value); }
        }

        /// <summary>Defines how much of a margin is to be left below the last top item in case the last top item is set to fill the available space</summary>
        public double LastTopItemFillMargin
        {
            get { return (double) GetValue(LastTopItemFillMarginProperty); }
            set { SetValue(LastTopItemFillMarginProperty, value); }
        }

        /// <summary>Defines whether the stack panel automatically handles scrollbars</summary>
        public BidirectionalStackPanelScrollBarModes ScrollBarMode
        {
            get { return (BidirectionalStackPanelScrollBarModes) GetValue(ScrollBarModeProperty); }
            set { SetValue(ScrollBarModeProperty, value); }
        }

        /// <summary>Invalidates everything in the UI and forces a refresh</summary>
        /// <param name="source">Reference to an instance of the form itself</param>
        private static void InvalidateAllVisuals(DependencyObject source)
        {
            var panel = source as BidirectionalStackPanel;
            if (panel == null) return;

            panel.InvalidateArrange();
            panel.InvalidateMeasure();
            panel.InvalidateVisual();
        }

        private double _calculatedContentHeight;
        private double _calculatedContentWidth;

        /// <summary>
        /// Measures the child elements of a <see cref="T:System.Windows.Controls.StackPanel"/> in anticipation of arranging them during the <see cref="M:System.Windows.Controls.StackPanel.ArrangeOverride(System.Windows.Size)"/> pass.
        /// </summary>
        /// <param name="constraint">An upper limit <see cref="T:System.Windows.Size"/> that should not be exceeded.</param>
        /// <returns>
        /// The <see cref="T:System.Windows.Size"/> that represents the desired size of the element.
        /// </returns>
        protected override Size MeasureOverride(Size constraint)
        {
            var topUsed = 0d;
            var bottomUsed = 0d;
            var leftUsed = 0d;
            var rightUsed = 0d;

            var extraMargin = ChildItemMargin;

            var topElements = new List<FrameworkElement>();
            var bottomElements = new List<FrameworkElement>();

            if (Orientation == Orientation.Vertical && _scrollVertical.Visibility == Visibility.Visible)
                constraint.Width = Math.Max(constraint.Width - _scrollVertical.ActualWidth, 0d);
            else if (Orientation == Orientation.Horizontal && _scrollHorizontal.Visibility == Visibility.Visible)
                constraint.Height = Math.Max(constraint.Height - _scrollHorizontal.ActualHeight, 0d);

            foreach (var element2 in Children.OfType<FrameworkElement>().Where(e => e.Visibility != Visibility.Collapsed))
            {
                if (Orientation == Orientation.Vertical)
                    if (element2.VerticalAlignment != VerticalAlignment.Bottom) topElements.Add(element2);
                    else bottomElements.Add(element2);
                else if (element2.HorizontalAlignment != HorizontalAlignment.Right) topElements.Add(element2);
                else bottomElements.Add(element2);
            }

            // starting with the bottom elements
            foreach (var element2 in bottomElements)
            {
                if (Orientation == Orientation.Vertical)
                    element2.Measure(new Size(constraint.Width, double.PositiveInfinity));
                else
                    element2.Measure(new Size(double.PositiveInfinity, constraint.Height));
                var extraBottom = extraMargin.Bottom;
                if (element2 is TextBlock || element2 is Label)
                    if (IgnoreChildItemBottomMarginForTextElements) extraBottom = 0;
                var desiredSize = GeometryHelper.NewSize(element2.DesiredSize.Width + extraMargin.Left + extraMargin.Right, element2.DesiredSize.Height + extraMargin.Top + extraBottom);
                if (Orientation == Orientation.Vertical)
                {
                    bottomUsed += desiredSize.Height;
                    leftUsed = Math.Max(leftUsed, desiredSize.Width);
                }
                else
                {
                    rightUsed += desiredSize.Width;
                    topUsed = Math.Max(topUsed, desiredSize.Height);
                }
            }

            // now for the top elements
            var topCounter = 0;
            foreach (var element2 in topElements)
            {
                topCounter++;
                var constraint2 = constraint;
                var extraBottom = extraMargin.Bottom;
                if (topCounter == topElements.Count && LastTopItemFillsSpace)
                {
                    if (Orientation == Orientation.Vertical && !double.IsInfinity(constraint.Height) && constraint.Height > 0)
                    {
                        constraint2 = GeometryHelper.NewSize(constraint.Width, Math.Max(element2.MinHeight, constraint.Height - topUsed - bottomUsed - LastTopItemFillMargin - extraMargin.Top - extraBottom));
                        extraBottom = LastTopItemFillMargin;
                    }
                    else if (Orientation == Orientation.Horizontal && !double.IsInfinity(constraint.Width) && constraint.Width > 0)
                    {
                        constraint2 = GeometryHelper.NewSize(Math.Max(element2.MinWidth, constraint.Width - leftUsed - rightUsed - LastTopItemFillMargin), constraint.Height);
                        extraBottom = LastTopItemFillMargin;
                    }
                }
                else
                {
                    if (Orientation == Orientation.Vertical)
                        constraint2 = new Size(constraint2.Width, double.PositiveInfinity);
                    else
                        constraint2 = new Size(double.PositiveInfinity, constraint2.Height);
                }
                element2.Measure(constraint2);
                if (element2 is TextBlock || element2 is Label)
                    if (IgnoreChildItemBottomMarginForTextElements) extraBottom = 0;
                var desiredSize = GeometryHelper.NewSize(Math.Max(0, element2.DesiredSize.Width + extraMargin.Left + extraMargin.Right), Math.Max(0, element2.DesiredSize.Height + extraMargin.Top + extraBottom));
                if (Orientation == Orientation.Vertical)
                {
                    topUsed += desiredSize.Height;
                    leftUsed = Math.Max(leftUsed, desiredSize.Width);
                    if (SimpleView.GetGroupBreak(element2)) leftUsed += GroupSpacing;
                }
                else
                {
                    leftUsed += desiredSize.Width;
                    topUsed = Math.Max(topUsed, desiredSize.Height);
                    if (SimpleView.GetGroupBreak(element2)) topUsed += GroupSpacing;
                }
            }

            var widthUsed = leftUsed + rightUsed;
            var heightUsed = topUsed + bottomUsed;

            if (Orientation == Orientation.Vertical && !double.IsInfinity(constraint.Width) && constraint.Width > 0)
                widthUsed = Math.Min(constraint.Width, widthUsed); // In vertical orientation, we use no more than the width available to us
            if (Orientation == Orientation.Horizontal && !double.IsInfinity(constraint.Width) && constraint.Width > 0 && widthUsed < constraint.Width)
                widthUsed = Math.Min(constraint.Width, widthUsed); // In horizontal orientation, if we haven't used up the entire width yet, we occupy the entire available space

            if (Orientation == Orientation.Horizontal && !double.IsInfinity(constraint.Height) && constraint.Height > 0)
                heightUsed = Math.Min(constraint.Height, heightUsed); // In horizontal orientation, we use no more than the height available to us
            if (Orientation == Orientation.Vertical && !double.IsInfinity(constraint.Height) && constraint.Height > 0 && heightUsed < constraint.Height)
                heightUsed = Math.Min(constraint.Height, heightUsed); // In vertical orientation, if we haven't used up the entire height yet, we occupy the entire available space

            if (Orientation == Orientation.Vertical)
            {
                _calculatedContentHeight = double.IsInfinity(constraint.Height) ? heightUsed : Math.Max(constraint.Height, heightUsed);
                _calculatedContentWidth = widthUsed;
            }
            else
            {
                _calculatedContentHeight = heightUsed;
                _calculatedContentWidth = double.IsInfinity(constraint.Width) ? widthUsed : Math.Max(constraint.Width, widthUsed);
            }

            var finalSize = GeometryHelper.NewSize(widthUsed, heightUsed);

            // Checking if this forces us to change scroll bar visibility and thus invalidate everything
            if (Orientation == Orientation.Vertical)
            {
                if (heightUsed > constraint.Height)
                {
                    if (_scrollVertical.Visibility != Visibility.Visible && ScrollBarMode == BidirectionalStackPanelScrollBarModes.Automatic)
                    {
                        _scrollVertical.Visibility = Visibility.Visible;
                        InvalidateAllVisuals(this); // Triggers recalculation of everything with a visible scroll bar
                    }
                    _scrollVertical.Maximum = _calculatedContentHeight - constraint.Height;
                    _scrollVertical.ViewportSize = constraint.Height;
                    _scrollVertical.LargeChange = constraint.Height;
                    finalSize.Height = constraint.Height;
                }
                else if (heightUsed <= constraint.Height && _scrollVertical.Visibility != Visibility.Collapsed)
                {
                    _scrollVertical.Visibility = Visibility.Collapsed;
                    InvalidateAllVisuals(this);
                }
            }
            else
            {
                if (widthUsed > constraint.Width)
                {
                    if (_scrollHorizontal.Visibility != Visibility.Visible && ScrollBarMode == BidirectionalStackPanelScrollBarModes.Automatic)
                    {
                        _scrollHorizontal.Visibility = Visibility.Visible;
                        InvalidateAllVisuals(this); // Triggers recalculation of everything with a visible scroll bar
                    }
                    _scrollHorizontal.Maximum = _calculatedContentWidth - constraint.Width;
                    _scrollHorizontal.LargeChange = constraint.Width;
                    _scrollHorizontal.ViewportSize = constraint.Width;
                    finalSize.Width = constraint.Width;
                }
                else if (widthUsed <= constraint.Width && _scrollHorizontal.Visibility != Visibility.Collapsed)
                {
                    _scrollHorizontal.Visibility = Visibility.Collapsed;
                    InvalidateAllVisuals(this);
                }
            }

            if (Orientation == Orientation.Vertical && _scrollVertical.Visibility == Visibility.Visible)
                finalSize.Width += _scrollVertical.ActualWidth;
            else if (Orientation == Orientation.Horizontal && _scrollHorizontal.Visibility == Visibility.Visible)
                finalSize.Height += _scrollHorizontal.ActualHeight;

            return finalSize;
        }

        /// <summary>
        /// Arranges the content of a <see cref="T:System.Windows.Controls.StackPanel"/> element.
        /// </summary>
        /// <param name="arrangeSize">The <see cref="T:System.Windows.Size"/> that this element should use to arrange its child elements.</param>
        /// <returns>
        /// The <see cref="T:System.Windows.Size"/> that represents the arranged size of this <see cref="T:System.Windows.Controls.StackPanel"/> element and its child elements.
        /// </returns>
        protected override Size ArrangeOverride(Size arrangeSize)
        {
            var top = 0d;
            var left = 0d;
            var bottom = 0d;
            var right = 0d;

            var usableWidth = _calculatedContentWidth;
            var usableHeight = _calculatedContentHeight;
            if (Orientation == Orientation.Vertical) usableWidth = Math.Max(usableWidth, arrangeSize.Width);
            else usableHeight = Math.Max(usableHeight, arrangeSize.Height);

            var extraMargin = ChildItemMargin;

            var topElements = new List<FrameworkElement>();
            var bottomElements = new List<FrameworkElement>();

            if (Orientation == Orientation.Vertical && _scrollVertical.Visibility == Visibility.Visible)
            {
                top = _scrollVertical.Value*-1;
                usableWidth -= _scrollVertical.ActualWidth + 2;
            }
            else if (Orientation == Orientation.Horizontal && _scrollHorizontal.Visibility == Visibility.Visible)
            {
                left = _scrollHorizontal.Value*-1;
                usableHeight -= _scrollHorizontal.ActualHeight + 2;
            }

            var contentSize = GeometryHelper.NewSize(usableWidth, usableHeight);

            var topOrigin = top;
            var leftOrigin = left;

            foreach (var element2 in Children.OfType<FrameworkElement>().Where(e => e.Visibility != Visibility.Collapsed))
            {
                if (Orientation == Orientation.Vertical)
                    if (element2.VerticalAlignment != VerticalAlignment.Bottom) topElements.Add(element2);
                    else bottomElements.Add(element2);
                else if (element2.HorizontalAlignment != HorizontalAlignment.Right) topElements.Add(element2);
                else bottomElements.Add(element2);
            }

            // Calculating the height of the bottom items
            foreach (var element in bottomElements)
            {
                if (Orientation == Orientation.Vertical)
                {
                    var extraBottom = extraMargin.Bottom;
                    if (element is TextBlock || element is Label)
                        if (IgnoreChildItemBottomMarginForTextElements) extraBottom = 0;
                    bottom += element.DesiredSize.Height + extraMargin.Top + extraBottom;
                    if (SimpleView.GetGroupBreak(element)) bottom += GroupSpacing;
                }
                else
                {
                    right += element.DesiredSize.Width;
                    if (SimpleView.GetGroupBreak(element)) right += GroupSpacing;
                }
            }

            var topCounter = 0;
            foreach (var element2 in topElements)
            {
                topCounter++;
                var extraBottom = extraMargin.Bottom;
                if (element2 is TextBlock || element2 is Label)
                    if (IgnoreChildItemBottomMarginForTextElements) extraBottom = 0;
                var desiredSize = GeometryHelper.NewSize(element2.DesiredSize.Width + extraMargin.Left + extraMargin.Right, element2.DesiredSize.Height + extraMargin.Top + extraBottom);
                if (Orientation == Orientation.Vertical)
                    // Top Alignment
                    if (topCounter < topElements.Count || !LastTopItemFillsSpace)
                    {
                        if (SimpleView.GetGroupBreak(element2)) top += GroupSpacing;
                        element2.Arrange(GeometryHelper.NewRect(extraMargin.Left, top + extraMargin.Top, contentSize.Width - extraMargin.Left - extraMargin.Right, element2.DesiredSize.Height));
                        top += desiredSize.Height;
                    }
                    else
                    {
                        var elementAutoSizeHeight = Math.Max(0, usableHeight + topOrigin - top - bottom - extraMargin.Top - extraMargin.Bottom - LastTopItemFillMargin);
                        element2.Arrange(GeometryHelper.NewRect(extraMargin.Left, top + extraMargin.Top, contentSize.Width - extraMargin.Left - extraMargin.Right - extraMargin.Left, elementAutoSizeHeight));
                        top += elementAutoSizeHeight;
                    }
                else
                {
                    // Left Alignment
                    if (topCounter < topElements.Count || !LastTopItemFillsSpace)
                    {
                        if (SimpleView.GetGroupBreak(element2)) left += GroupSpacing;
                        element2.Arrange(GeometryHelper.NewRect(left + extraMargin.Left, extraMargin.Top, element2.DesiredSize.Width, contentSize.Height - extraBottom));
                        left += desiredSize.Width;
                    }
                    else
                    {
                        var elementAutoSizeWidth = Math.Max(0, usableWidth + leftOrigin - left - right - extraMargin.Left - extraMargin.Right - LastTopItemFillMargin);
                        element2.Arrange(GeometryHelper.NewRect(left + extraMargin.Left, extraMargin.Top, elementAutoSizeWidth, contentSize.Height - extraBottom - extraMargin.Top));
                        left += elementAutoSizeWidth;
                    }
                }
            }

            // Second pass to arrange the elements on the "other side"
            var availableHeight = contentSize.Height;
            var availableWidth = contentSize.Width;
            var top2 = topOrigin + availableHeight - bottom - extraMargin.Top;
            if (top2 < top) top2 = top; // Can't overlap the items that are already there
            var left2 = leftOrigin + availableWidth - right;
            if (left2 < left) left2 = left; // Can't overlap the items that are already there
            foreach (var element2 in bottomElements)
            {
                var extraBottom = extraMargin.Bottom;
                if (element2 is TextBlock || element2 is Label)
                    if (IgnoreChildItemBottomMarginForTextElements) extraBottom = 0;
                var desiredSize = GeometryHelper.NewSize(element2.DesiredSize.Width + extraMargin.Left + extraMargin.Right, element2.DesiredSize.Height + extraMargin.Top + extraBottom);
                if (Orientation == Orientation.Vertical)
                {
                    if (SimpleView.GetGroupBreak(element2)) top2 += GroupSpacing;
                    element2.Arrange(GeometryHelper.NewRect(extraMargin.Left, top2 + extraMargin.Top, contentSize.Width - extraMargin.Left - extraMargin.Right, element2.DesiredSize.Height));
                    top2 += desiredSize.Height;
                }
                else
                {
                    if (SimpleView.GetGroupBreak(element2)) left2 += GroupSpacing;
                    element2.Arrange(GeometryHelper.NewRect(left2 + extraMargin.Left, extraMargin.Top, element2.DesiredSize.Width, contentSize.Height - extraMargin.Top - extraBottom));
                    left2 += desiredSize.Width;
                }
            }

            return arrangeSize;
        }
    }

    /// <summary>
    /// Defines scroll bar behavior (visibility) for the bidirectional stack panel
    /// </summary>
    public enum BidirectionalStackPanelScrollBarModes
    {
        /// <summary>
        /// No scroll bars shall be shown
        /// </summary>
        None,

        /// <summary>
        /// Scroll bar handling is automatic and scroll bars will show up as needed
        /// </summary>
        Automatic
    }

    /// <summary>Adorner UI for scrollbars of the bidirectional stack panel control</summary>
    public class BidirectionalStackPanelScrollAdorner : Adorner
    {
        private readonly ScrollBar _horizontal;
        private readonly ScrollBar _vertical;

        /// <summary>Constructor</summary>
        /// <param name="adornedElement">Adorned element</param>
        /// <param name="horizontal">The horizontal scrollbar.</param>
        /// <param name="vertical">The vertical scrollbar.</param>
        public BidirectionalStackPanelScrollAdorner(BidirectionalStackPanel adornedElement, ScrollBar horizontal, ScrollBar vertical)
            : base(adornedElement)
        {
            _horizontal = horizontal;
            _vertical = vertical;
            CheckControlForExistingParent(_horizontal);
            AddVisualChild(_horizontal);
            CheckControlForExistingParent(_vertical);
            AddVisualChild(_vertical);
        }

        /// <summary>Implements any custom measuring behavior for the adorner.</summary>
        /// <param name="constraint">A size to constrain the adorner to.</param>
        /// <returns>A <see cref="T:System.Windows.Size"/> object representing the amount of layout space needed by the adorner.</returns>
        protected override Size MeasureOverride(Size constraint)
        {
            _horizontal.Measure(constraint);
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

            if (_horizontal.Visibility == Visibility.Visible)
            {
                var scrollTop = surfaceSize.Height - SystemParameters.HorizontalScrollBarHeight;
                var scrollWidth = _vertical.Visibility == Visibility.Visible ? surfaceSize.Width - SystemParameters.VerticalScrollBarWidth : surfaceSize.Width;
                if (scrollTop < 0d) scrollTop = 0d;
                var scrollHeight = SystemParameters.HorizontalScrollBarHeight;
                if (scrollHeight > finalSize.Height) scrollHeight = finalSize.Height;
                _horizontal.Arrange(GeometryHelper.NewRect(0, scrollTop, scrollWidth, scrollHeight, true));
            }
            if (_vertical.Visibility == Visibility.Visible)
            {
                var scrollLeft = Math.Max(surfaceSize.Width - SystemParameters.VerticalScrollBarWidth, 0d);
                var scrollWidth = SystemParameters.VerticalScrollBarWidth;
                if (scrollWidth > finalSize.Width) scrollWidth = finalSize.Width;
                var scrollHeight = _horizontal.Visibility == Visibility.Visible
                    ? surfaceSize.Height - SystemParameters.HorizontalScrollBarHeight
                    : surfaceSize.Height;
                _vertical.Arrange(GeometryHelper.NewRect(scrollLeft, 0, scrollWidth, scrollHeight, true));
            }

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
                var adorner = parent as BidirectionalStackPanelScrollAdorner;
                if (adorner != null)
                    adorner.RemoveVisualChild(element);
            }
        }

        /// <summary>Gets the number of visual child elements within this element.</summary>
        /// <returns>The number of visual child elements for this element.</returns>
        protected override int VisualChildrenCount
        {
            get { return 2; }
        }

        /// <summary>Overrides <see cref="M:System.Windows.Media.Visual.GetVisualChild(System.Int32)"/>, and returns a child at the specified index from a collection of child elements.</summary>
        /// <param name="index">The zero-based index of the requested child element in the collection.</param>
        /// <returns>The requested child element. This should not return null; if the provided index is out of range, an exception is thrown.</returns>
        protected override Visual GetVisualChild(int index)
        {
            return index == 0 ? _horizontal : _vertical;
        }
    }
}
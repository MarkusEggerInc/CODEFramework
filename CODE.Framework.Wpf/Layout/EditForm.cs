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
using System.Windows.Threading;
using CODE.Framework.Wpf.Utilities;

namespace CODE.Framework.Wpf.Layout
{
    /// <summary>Automatic edit form layout</summary>
    public class EditForm : Panel
    {
        private readonly ScaleTransform _scale = new ScaleTransform(1d, 1d);

        private readonly ScrollBar _scrollVertical = new ScrollBar {Visibility = Visibility.Collapsed, Orientation = Orientation.Vertical};
        private readonly ScrollBar _scrollHorizontal = new ScrollBar {Visibility = Visibility.Collapsed, Orientation = Orientation.Horizontal};
        private AdornerLayer _adorner;

        /// <summary>Constructor</summary>
        public EditForm()
        {
            VerticalAlignment = VerticalAlignment.Stretch;
            HorizontalAlignment = HorizontalAlignment.Stretch;
            ClipToBounds = true;
            Background = Brushes.Transparent;
            
            Loaded += (s, e) => CreateScrollbars();
        }

        private void CreateScrollbars()
        {
            _adorner = AdornerLayer.GetAdornerLayer(this);
            if (_adorner == null) return;
            _adorner.Add(new EditFormScrollAdorner(this, _scrollHorizontal, _scrollVertical) {Visibility = Visibility.Visible});
            _scrollHorizontal.ValueChanged += (s, e) => Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(DispatchInvalidateScroll));
            _scrollVertical.ValueChanged += (s, e) => Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(DispatchInvalidateScroll));
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

        private void DispatchInvalidateScroll()
        {
            InvalidateArrange();
            InvalidateVisual();
        }

        /// <summary>Defines whether the layout is elastic (smartly resizes automatically) or not</summary>
        public LayoutElasticity LayoutElasticity
        {
            get { return (LayoutElasticity)GetValue(LayoutElasticityProperty); }
            set { SetValue(LayoutElasticityProperty, value); }
        }
        /// <summary>Defines whether the layout is elastic (smartly resizes automatically) or not</summary>
        public static readonly DependencyProperty LayoutElasticityProperty = DependencyProperty.Register("LayoutElasticity", typeof(LayoutElasticity), typeof(EditForm), new UIPropertyMetadata(LayoutElasticity.LayoutAndScale, (s, e) => InvalidateAllVisuals(s)));

        /// <summary>Defines the position of the label relative to the edit control</summary>
        public EditFormLabelPositions LabelPosition
        {
            get { return (EditFormLabelPositions)GetValue(LabelPositionsProperty); }
            set { SetValue(LabelPositionsProperty, value); }
        }
        /// <summary>Defines the position of the label relative to the edit control</summary>
        public static readonly DependencyProperty LabelPositionsProperty = DependencyProperty.Register("LabelPositions", typeof(EditFormLabelPositions), typeof(EditForm), new UIPropertyMetadata(EditFormLabelPositions.Left, (s, e) => InvalidateAllVisuals(s)));

        /// <summary>Vertical spacing in between individual elements</summary>
        public double VerticalSpacing
        {
            get { return (double)GetValue(VerticalSpacingProperty); }
            set { SetValue(VerticalSpacingProperty, value); }
        }
        /// <summary>Vertical spacing in between individual elements</summary>
        public static readonly DependencyProperty VerticalSpacingProperty = DependencyProperty.Register("VerticalSpacing", typeof(double), typeof(EditForm), new UIPropertyMetadata(5d, (s, e) => InvalidateAllVisuals(s)));

        /// <summary>Minimum vertical spacing in between individual elements (only used if elastic layout is enabled)</summary>
        public double MinVerticalSpacing
        {
            get { return (double)GetValue(MinVerticalSpacingProperty); }
            set { SetValue(MinVerticalSpacingProperty, value); }
        }
        /// <summary>Minimum vertical spacing in between individual elements (only used if elastic layout is enabled)</summary>
        public static readonly DependencyProperty MinVerticalSpacingProperty = DependencyProperty.Register("MinVerticalSpacing", typeof(double), typeof(EditForm), new UIPropertyMetadata(1d, (s, e) => InvalidateAllVisuals(s)));

        /// <summary>Vertical additional spacing before new groups</summary>
        public double GroupSpacing
        {
            get { return (double)GetValue(GroupSpacingProperty); }
            set { SetValue(GroupSpacingProperty, value); }
        }
        /// <summary>Vertical additional spacing before new groups</summary>
        public static readonly DependencyProperty GroupSpacingProperty = DependencyProperty.Register("GroupSpacing", typeof(double), typeof(EditForm), new UIPropertyMetadata(15d, (s, e) => InvalidateAllVisuals(s)));

        /// <summary>Minimum vertical additional spacing before new groups (only used if elastic layout is enabled)</summary>
        public double MinGroupSpacing
        {
            get { return (double)GetValue(MinGroupSpacingProperty); }
            set { SetValue(MinGroupSpacingProperty, value); }
        }
        /// <summary>Minimum vertical additional spacing before new groups (only used if elastic layout is enabled)</summary>
        public static readonly DependencyProperty MinGroupSpacingProperty = DependencyProperty.Register("MinGroupSpacing", typeof(double), typeof(EditForm), new UIPropertyMetadata(4d, (s, e) => InvalidateAllVisuals(s)));

        /// <summary>Defines whether a group background should be rendered for groups</summary>
        public bool RenderGroupBackground
        {
            get { return (bool)GetValue(RenderGroupBackgroundProperty); }
            set { SetValue(RenderGroupBackgroundProperty, value); }
        }
        /// <summary>Defines whether a group background should be rendered for groups</summary>
        public static readonly DependencyProperty RenderGroupBackgroundProperty = DependencyProperty.Register("RenderGroupBackground", typeof(bool), typeof(EditForm), new UIPropertyMetadata(false));

        /// <summary>
        /// Indicates whether a horizontal separator line should be rendered after every control-pair
        /// </summary>
        /// <value><c>true</c> if [render horizontal control separator lines]; otherwise, <c>false</c>.</value>
        public bool RenderHorizontalControlSeparatorLines
        {
            get { return (bool)GetValue(RenderHorizontalControlSeparatorLinesProperty); }
            set { SetValue(RenderHorizontalControlSeparatorLinesProperty, value); }
        }
        /// <summary>
        /// Indicates whether a horizontal separator line should be rendered after every control-pair
        /// </summary>
        public static readonly DependencyProperty RenderHorizontalControlSeparatorLinesProperty = DependencyProperty.Register("RenderHorizontalControlSeparatorLines", typeof(bool), typeof(EditForm), new PropertyMetadata(false));

        /// <summary>
        /// Vertical offset for a potential horizontal control separator line (if shown)
        /// </summary>
        /// <value>The vertical control separator offset.</value>
        public double VerticalControlSeparatorOffset
        {
            get { return (double)GetValue(VerticalControlSeparatorOffsetProperty); }
            set { SetValue(VerticalControlSeparatorOffsetProperty, value); }
        }
        /// <summary>
        /// Vertical offset for a potential horizontal control separator line (if shown)
        /// </summary>
        public static readonly DependencyProperty VerticalControlSeparatorOffsetProperty = DependencyProperty.Register("VerticalControlSeparatorOffset", typeof(double), typeof(EditForm), new PropertyMetadata(0d));

        /// <summary>
        /// Brush used to render horizontal separators between control pairs
        /// </summary>
        /// <value>The horizontal line separator brush.</value>
        /// <remarks>Only used if RenderHorizontalControlSeparatorLines = true</remarks>
        public Brush HorizontalLineSeparatorBrush
        {
            get { return (Brush)GetValue(HorizontalLineSeparatorBrushProperty); }
            set { SetValue(HorizontalLineSeparatorBrushProperty, value); }
        }
        /// <summary>
        /// Brush used to render horizontal separators between control pairs
        /// </summary>
        /// <remarks>Only used if RenderHorizontalControlSeparatorLines = true</remarks>
        public static readonly DependencyProperty HorizontalLineSeparatorBrushProperty = DependencyProperty.Register("HorizontalLineSeparatorBrush", typeof(Brush), typeof(EditForm), new PropertyMetadata(null));

        /// <summary>Background brush for group backgrounds</summary>
        /// <remarks>Only used of RenderGroupBackground = true</remarks>
        public Brush GroupBackgroundBrush
        {
            get { return (Brush)GetValue(GroupBackgroundBrushProperty); }
            set { SetValue(GroupBackgroundBrushProperty, value); }
        }
        /// <summary>Background brush for group backgrounds</summary>
        /// <remarks>Only used of RenderGroupBackground = true</remarks>
        public static readonly DependencyProperty GroupBackgroundBrushProperty = DependencyProperty.Register("GroupBackgroundBrush", typeof(Brush), typeof(EditForm), new UIPropertyMetadata(null));

        /// <summary>Border brush for group backgrounds</summary>
        /// <remarks>Only used of RenderGroupBackground = true</remarks>
        public Brush GroupBorderBrush
        {
            get { return (Brush)GetValue(GroupBorderBrushProperty); }
            set { SetValue(GroupBorderBrushProperty, value); }
        }
        /// <summary>Border brush for group backgrounds</summary>
        /// <remarks>Only used of RenderGroupBackground = true</remarks>
        public static readonly DependencyProperty GroupBorderBrushProperty = DependencyProperty.Register("GroupBorderBrush", typeof(Brush), typeof(EditForm), new UIPropertyMetadata(null));

        /// <summary>Drawing width for the border of group backgrounds</summary>
        /// <remarks>Only used of RenderGroupBackground = true</remarks>
        public double GroupBorderWidth
        {
            get { return (double)GetValue(GroupBorderWidthProperty); }
            set { SetValue(GroupBorderWidthProperty, value); }
        }
        /// <summary>Drawing width for the border of group backgrounds</summary>
        /// <remarks>Only used of RenderGroupBackground = true</remarks>
        public static readonly DependencyProperty GroupBorderWidthProperty = DependencyProperty.Register("GroupBorderWidth", typeof(double), typeof(EditForm), new UIPropertyMetadata(1d));

        /// <summary>Margin to be added between a rendered group border and the actual group elements</summary>
        /// <remarks>Only used of RenderGroupBackground = true</remarks>
        public Thickness GroupBorderMargin
        {
            get { return (Thickness)GetValue(GroupBorderMarginProperty); }
            set { SetValue(GroupBorderMarginProperty, value); }
        }
        /// <summary>Margin to be added between a rendered group border and the actual group elements</summary>
        /// <remarks>Only used of RenderGroupBackground = true</remarks>
        public static readonly DependencyProperty GroupBorderMarginProperty = DependencyProperty.Register("GroupBorderMargin", typeof (Thickness), typeof (EditForm), new UIPropertyMetadata(new Thickness(20, 5, 20, 5)));

        /// <summary>Left padding/margin that separates the label from the edit control</summary>
        /// <remarks>Only applicable when the label is positioned to the left of the edit control</remarks>
        public double EditControlLeftSpacing
        {
            get { return (double)GetValue(EditControlLeftSpacingProperty); }
            set { SetValue(EditControlLeftSpacingProperty, value); }
        }
        /// <summary>Left padding/margin that separates the label from the edit control</summary>
        /// <remarks>Only applicable when the label is positioned to the left of the edit control</remarks>
        public static readonly DependencyProperty EditControlLeftSpacingProperty = DependencyProperty.Register("EditControlLeftSpacing", typeof(double), typeof(EditForm), new UIPropertyMetadata(20d, (s, e) => InvalidateAllVisuals(s)));

        /// <summary>Minimum left padding/margin that separates the label from the edit control (only applies for elastic layouts)</summary>
        /// <remarks>Only applicable when the label is positioned to the left of the edit control</remarks>
        public double MinEditControlLeftSpacing
        {
            get { return (double)GetValue(MinEditControlLeftSpacingProperty); }
            set { SetValue(MinEditControlLeftSpacingProperty, value); }
        }
        /// <summary>Minimum left padding/margin that separates the label from the edit control (only applies for elastic layouts)</summary>
        /// <remarks>Only applicable when the label is positioned to the left of the edit control</remarks>
        public static readonly DependencyProperty MinEditControlLeftSpacingProperty = DependencyProperty.Register("MinEditControlLeftSpacing", typeof(double), typeof(EditForm), new UIPropertyMetadata(2d, (s, e) => InvalidateAllVisuals(s)));

        /// <summary>Spacing (typically horizontal) for controls that are to flow with the previous element</summary>
        public double FlowWithPreviousSpacing
        {
            get { return (double)GetValue(FlowWithPreviousSpacingProperty); }
            set { SetValue(FlowWithPreviousSpacingProperty, value); }
        }
        /// <summary>Spacing (typically horizontal) for controls that are to flow with the previous element</summary>
        public static readonly DependencyProperty FlowWithPreviousSpacingProperty = DependencyProperty.Register("FlowWithPreviousSpacing", typeof(double), typeof(EditForm), new UIPropertyMetadata(9d, (s, e) => InvalidateAllVisuals(s)));

        /// <summary>Top padding/margin that separates the label from the edit control</summary>
        /// <remarks>Only applicable when the label is positioned to the top of the edit control</remarks>
        public double EditControlTopSpacing
        {
            get { return (double)GetValue(EditControlTopSpacingProperty); }
            set { SetValue(EditControlTopSpacingProperty, value); }
        }
        /// <summary>Top padding/margin that separates the label from the edit control</summary>
        /// <remarks>Only applicable when the label is positioned to the top of the edit control</remarks>
        public static readonly DependencyProperty EditControlTopSpacingProperty = DependencyProperty.Register("EditControlTopSpacing", typeof(double), typeof(EditForm), new UIPropertyMetadata(2d, (s, e) => InvalidateAllVisuals(s)));

        /// <summary>Spacing between columns</summary>
        public double ColumnSpacing
        {
            get { return (double)GetValue(ColumnSpacingProperty); }
            set { SetValue(ColumnSpacingProperty, value); }
        }
        /// <summary>Spacing between columns</summary>
        public static readonly DependencyProperty ColumnSpacingProperty = DependencyProperty.Register("ColumnSpacing", typeof(double), typeof(EditForm), new UIPropertyMetadata(30d, (s, e) => InvalidateAllVisuals(s)));

        /// <summary>Minimum spacing between columns (only applies for elastic layouts)</summary>
        public double MinColumnSpacing
        {
            get { return (double)GetValue(MinColumnSpacingProperty); }
            set { SetValue(MinColumnSpacingProperty, value); }
        }
        /// <summary>Minimum spacing between columns (only applies for elastic layouts)</summary>
        public static readonly DependencyProperty MinColumnSpacingProperty = DependencyProperty.Register("MinColumnSpacing", typeof(double), typeof(EditForm), new UIPropertyMetadata(5d, (s, e) => InvalidateAllVisuals(s)));

        /// <summary>Object used to render the group headers</summary>
        public AutoLayoutHeaderRenderer GroupHeaderRenderer
        {
            get { return (AutoLayoutHeaderRenderer)GetValue(GroupHeaderRendererProperty); }
            set { SetValue(GroupHeaderRendererProperty, value); }
        }
        /// <summary>Object used to render the group headers</summary>
        public static readonly DependencyProperty GroupHeaderRendererProperty = DependencyProperty.Register("GroupHeaderRenderer", typeof(AutoLayoutHeaderRenderer), typeof(EditForm), new UIPropertyMetadata(null));

        /// <summary>Vertical additional spacing before new groups</summary>
        public double GroupHeaderTopSpacing
        {
            get { return (double)GetValue(GroupHeaderTopSpacingProperty); }
            set { SetValue(GroupHeaderTopSpacingProperty, value); }
        }
        /// <summary>Vertical additional spacing before new groups</summary>
        public static readonly DependencyProperty GroupHeaderTopSpacingProperty = DependencyProperty.Register("GroupHeaderTopSpacing", typeof(double), typeof(EditForm), new UIPropertyMetadata(15d, (s, e) => InvalidateAllVisuals(s)));

        /// <summary>Vertical additional spacing before new groups</summary>
        public double GroupHeaderBottomSpacing
        {
            get { return (double)GetValue(GroupHeaderBottomSpacingProperty); }
            set { SetValue(GroupHeaderBottomSpacingProperty, value); }
        }
        /// <summary>Vertical additional spacing before new groups</summary>
        public static readonly DependencyProperty GroupHeaderBottomSpacingProperty = DependencyProperty.Register("GroupHeaderBottomSpacing", typeof(double), typeof(EditForm), new UIPropertyMetadata(7d, (s, e) => InvalidateAllVisuals(s)));

        /// <summary>Minimum vertical additional spacing after new group headers (only used if elastic layout is enabled)</summary>
        public double MinGroupHeaderBottomSpacing
        {
            get { return (double)GetValue(MinGroupHeaderBottomSpacingProperty); }
            set { SetValue(MinGroupHeaderBottomSpacingProperty, value); }
        }
        /// <summary>Minimum vertical additional spacing after new group headers (only used if elastic layout is enabled)</summary>
        public static readonly DependencyProperty MinGroupHeaderBottomSpacingProperty = DependencyProperty.Register("MinGroupHeaderBottomSpacing", typeof(double), typeof(EditForm), new UIPropertyMetadata(3d, (s, e) => InvalidateAllVisuals(s)));

        /// <summary>Minimum vertical additional spacing before new group headers (only used if elastic layout is enabled)</summary>
        public double MinGroupHeaderTopSpacing
        {
            get { return (double)GetValue(MinGroupHeaderTopSpacingProperty); }
            set { SetValue(MinGroupHeaderTopSpacingProperty, value); }
        }
        /// <summary>Minimum vertical additional spacing before new group headers (only used if elastic layout is enabled)</summary>
        public static readonly DependencyProperty MinGroupHeaderTopSpacingProperty = DependencyProperty.Register("MinGroupHeaderTopSpacing", typeof(double), typeof(EditForm), new UIPropertyMetadata(5d, (s, e) => InvalidateAllVisuals(s)));

        /// <summary>Font family used to render group headers</summary>
        public FontFamily GroupHeaderFontFamily
        {
            get { return (FontFamily)GetValue(GroupHeaderFontFamilyProperty); }
            set { SetValue(GroupHeaderFontFamilyProperty, value); }
        }
        /// <summary>Font family used to render group headers</summary>
        public static readonly DependencyProperty GroupHeaderFontFamilyProperty = DependencyProperty.Register("GroupHeaderFontFamily", typeof(FontFamily), typeof(EditForm), new UIPropertyMetadata(new FontFamily("Segoe UI"), (s, e) => InvalidateAllVisuals(s)));

        /// <summary>Font style used to render group headers</summary>
        public FontStyle GroupHeaderFontStyle
        {
            get { return (FontStyle)GetValue(GroupHeaderFontStyleProperty); }
            set { SetValue(GroupHeaderFontStyleProperty, value); }
        }
        /// <summary>Font style used to render group headers</summary>
        public static readonly DependencyProperty GroupHeaderFontStyleProperty = DependencyProperty.Register("GroupHeaderFontStyle", typeof(FontStyle), typeof(EditForm), new UIPropertyMetadata(FontStyles.Normal, (s, e) => InvalidateAllVisuals(s)));

        /// <summary>Font weight used to render group headers</summary>
        public FontWeight GroupHeaderFontWeight
        {
            get { return (FontWeight)GetValue(GroupHeaderFontWeightProperty); }
            set { SetValue(GroupHeaderFontWeightProperty, value); }
        }
        /// <summary>Font weight used to render group headers</summary>
        public static readonly DependencyProperty GroupHeaderFontWeightProperty = DependencyProperty.Register("GroupHeaderFontWeight", typeof(FontWeight), typeof(EditForm), new UIPropertyMetadata(FontWeights.Bold, (s, e) => InvalidateAllVisuals(s)));

        /// <summary>Font size used to render group headers</summary>
        public double GroupHeaderFontSize
        {
            get { return (double)GetValue(GroupHeaderFontSizeProperty); }
            set { SetValue(GroupHeaderFontSizeProperty, value); }
        }
        /// <summary>Font size used to render group headers</summary>
        public static readonly DependencyProperty GroupHeaderFontSizeProperty = DependencyProperty.Register("GroupHeaderFontSize", typeof(double), typeof(EditForm), new UIPropertyMetadata(12d, (s, e) => InvalidateAllVisuals(s)));

        /// <summary>Foreground brush used to render group headers</summary>
        public Brush GroupHeaderForegroundBrush
        {
            get { return (Brush)GetValue(GroupHeaderForegroundBrushProperty); }
            set { SetValue(GroupHeaderForegroundBrushProperty, value); }
        }
        /// <summary>Foreground brush used to render group headers</summary>
        public static readonly DependencyProperty GroupHeaderForegroundBrushProperty = DependencyProperty.Register("GroupHeaderForegroundBrush", typeof(Brush), typeof(EditForm), new UIPropertyMetadata(Brushes.Black, (s, e) => InvalidateAllVisuals(s)));

        /// <summary>Number of pixels the label is offset down when the layout uses labels to the left of the edit control</summary>
        public double VerticalLabelControlOffset
        {
            get { return (double)GetValue(VerticalLabelControlOffsetProperty); }
            set { SetValue(VerticalLabelControlOffsetProperty, value); }
        }
        /// <summary>Number of pixels the label is offset down when the layout uses labels to the left of the edit control</summary>
        public static readonly DependencyProperty VerticalLabelControlOffsetProperty = DependencyProperty.Register("VerticalLabelControlOffset", typeof(double), typeof(EditForm), new UIPropertyMetadata(5d, (s, e) => InvalidateAllVisuals(s)));

        /// <summary>Smallest factor the layout automatically scales to (only applicable when the elasticity of the layout includes the option to scale)</summary>
        public double MinElasticScaleFactor
        {
            get { return (double)GetValue(MinElasticScaleFactorProperty); }
            set { SetValue(MinElasticScaleFactorProperty, value); }
        }
        /// <summary>Smallest factor the layout automatically scales to (only applicable when the elasticity of the layout includes the option to scale)</summary>
        public static readonly DependencyProperty MinElasticScaleFactorProperty = DependencyProperty.Register("MinElasticScaleFactor", typeof(double), typeof(EditForm), new UIPropertyMetadata(.75d, (s, e) => InvalidateAllVisuals(s)));

        /// <summary>Defines whether the edit form automatically handles scrollbars</summary>
        public EditFormScrollBarModes ScrollBarMode
        {
            get { return (EditFormScrollBarModes)GetValue(ScrollBarModeProperty); }
            set { SetValue(ScrollBarModeProperty, value); }
        }
        /// <summary>Defines whether the edit form automatically handles scrollbars</summary>
        public static readonly DependencyProperty ScrollBarModeProperty = DependencyProperty.Register("ScrollBarMode", typeof(EditFormScrollBarModes), typeof(EditForm), new UIPropertyMetadata(EditFormScrollBarModes.Both, (s, e) => InvalidateAllVisuals(s)));
        
        /// <summary>Invalidates everything in the UI and forces a refresh</summary>
        /// <param name="source">Reference to an instance of the form itself</param>
        private static void InvalidateAllVisuals(DependencyObject source)
        {
            var form = source as EditForm;
            if (form == null) return;

            form.InvalidateArrange();
            form.InvalidateMeasure();
            form.InvalidateVisual();

            form._scrollHorizontal.Visibility = Visibility.Collapsed;
            form._scrollVertical.Visibility = Visibility.Collapsed;
        }

        private List<List<ControlPair>> _columnsUsed;
        private readonly List<ColumnRenderInformation> _columnRenderInformation = new List<ColumnRenderInformation>();
        private double _editControlLeftSpacingUsed = -1d;
        private double _columnSpacingUsed = -1d;

        private bool _mustScale;

        /// <summary>Provides the behavior for the Measure pass of Silverlight layout. Classes can override this method to define their own Measure pass behavior.</summary>
        /// <param name="availableSize">The available size that this object can give to child objects. Infinity can be specified as a value to indicate that the object will size to whatever content is available.</param>
        /// <returns>The size that this object determines it needs during layout, based on its calculations of child object allotted sizes.</returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            var newScaleX = 1d;
            var newScaleY = 1d;
            _mustScale = false;

            // First, we figure out how many columns we have and what the controls are in each column
            _columnsUsed = GetColumns();
            _columnRenderInformation.Clear();
            foreach (var column in _columnsUsed)
                _columnRenderInformation.Add(GetStandardColumnRenderInfo());

            _editControlLeftSpacingUsed = EditControlLeftSpacing;
            _columnSpacingUsed = ColumnSpacing;

            // Now we calculate the space needed in each column
            var requiredSize = PerformMeasurePass(_columnsUsed);
            if (LayoutElasticity != LayoutElasticity.None)
            {
                requiredSize = ShrinkHorizontalIfNeeded(availableSize, requiredSize);
                requiredSize = ShrinkVerticalIfNeeded(availableSize, requiredSize);
            }

            base.MeasureOverride(availableSize);

            // If it is still too large, we potentially have other strategies we can use
            if (requiredSize.Width - 1 > availableSize.Width || requiredSize.Height - 1 > availableSize.Height)
            {
                if (LayoutElasticity == LayoutElasticity.LayoutAndScale)
                {
                    // We can scale the overall UI
                    var factorX = availableSize.Width/requiredSize.Width;
                    var factorY = availableSize.Height/requiredSize.Height;
                    var factor = Math.Max(Math.Min(factorX, factorY), MinElasticScaleFactor);
                    newScaleX = factor;
                    newScaleY = factor;

                    foreach (var control in Children)
                    {
                        var childElement = control as FrameworkElement;
                        if (childElement == null) continue;
                        if (!Equals(childElement.LayoutTransform, _scale))
                            childElement.LayoutTransform = _scale;
                    }
                    _mustScale = true;
                }
                else if (LayoutElasticity == LayoutElasticity.LayoutAndReflow && requiredSize.Width - 1 > availableSize.Width)
                {
                    // We will re-flow the columns
                    var columnsFit = false;
                    while (!columnsFit)
                    {
                        MergeColumnIntoOtherColumn(_columnsUsed, 1, 0);
                        requiredSize = PerformMeasurePass(_columnsUsed);
                        columnsFit = requiredSize.Width <= availableSize.Width;
                        if (!columnsFit && _columnsUsed.Count == 1) columnsFit = true; // This is the best we can do
                    }
                }
            }

            _measuredSize = requiredSize;
            if (_mustScale) _measuredSize = GeometryHelper.NewSize(requiredSize.Width*_scale.ScaleX, requiredSize.Height*_scale.ScaleY);

            // If the new scale is within a very narrow margin of the scale already set, we leave it as is, otherwise we constantly re-trigger layout
            if (!(newScaleX < _scale.ScaleX + 0.01 && newScaleX > _scale.ScaleX - 0.01)) _scale.ScaleX = newScaleX;
            if (!(newScaleY < _scale.ScaleY + 0.01 && newScaleY > _scale.ScaleY - 0.01)) _scale.ScaleY = newScaleY;

            var finalMeasuredHeight = _measuredSize.Height;
            var finalMeasuredWidth = _measuredSize.Width;

            if (!double.IsInfinity(availableSize.Width) && availableSize.Width < 90000) finalMeasuredWidth = availableSize.Width; // 90000 is smaller than 100000, which is often uses instead of infiniti by parent elements
            if (!double.IsInfinity(availableSize.Height) && availableSize.Height < 90000) finalMeasuredHeight = availableSize.Height; // 90000 is smaller than 100000, which is often uses instead of infiniti by parent elements

            //// If we were given an actual size to fit into (rather than something like infinity), then we return that size
            //if (availableSize.Height > 0 && availableSize.Height < 50000 && availableSize.Width > 0 && availableSize.Width < 50000 && !double.IsInfinity(availableSize.Height) && !double.IsInfinity(availableSize.Width))
            //    return availableSize;

            return GeometryHelper.NewSize(finalMeasuredWidth, finalMeasuredHeight);
        }

        private void MergeColumnIntoOtherColumn(List<List<ControlPair>> columns, int sourceColumn, int destinationColumn)
        {
            var first = true;
            foreach (var pair in columns[sourceColumn])
            {
                if (first) pair.ForceGroupBreak();
                columns[destinationColumn].Add(pair);
                first = false;
            }
            columns.RemoveAt(sourceColumn);
        }

        private void HandleScrollBars(Size requiredSize, Size clientSize)
        {
            if (ScrollBarMode == EditFormScrollBarModes.None)
            {
                _scrollHorizontal.Visibility = Visibility.Collapsed;
                _scrollVertical.Visibility = Visibility.Collapsed;
                return;
            }

            if (ScrollBarMode == EditFormScrollBarModes.Vertical || ScrollBarMode == EditFormScrollBarModes.Both)
            {
                if (ScrollBarMode == EditFormScrollBarModes.Vertical) _scrollHorizontal.Visibility = Visibility.Collapsed;
                if (!double.IsInfinity(clientSize.Height) && !double.IsNaN(clientSize.Height))
                    if (clientSize.Height < requiredSize.Height - 1)
                    {
                        _scrollVertical.Visibility = Visibility.Visible;
                        _scrollVertical.Maximum = requiredSize.Height - clientSize.Height + SystemParameters.HorizontalScrollBarHeight;
                        _scrollVertical.ViewportSize = clientSize.Height;
                        _scrollVertical.LargeChange = _scrollVertical.ViewportSize;
                        _scrollVertical.SmallChange = (int) (_scrollVertical.LargeChange / 10);
                    }
                    else
                        _scrollVertical.Visibility = Visibility.Collapsed;
                else
                    _scrollVertical.Visibility = Visibility.Collapsed;
            }

            if (ScrollBarMode == EditFormScrollBarModes.Horizontal || ScrollBarMode == EditFormScrollBarModes.Both)
            {
                if (ScrollBarMode == EditFormScrollBarModes.Horizontal) _scrollVertical.Visibility = Visibility.Collapsed;
                if (!double.IsInfinity(clientSize.Width) && !double.IsNaN(clientSize.Width))
                    if (clientSize.Width < requiredSize.Width - 1)
                    {
                        _scrollHorizontal.Visibility = Visibility.Visible;
                        _scrollHorizontal.Maximum = requiredSize.Width - clientSize.Width + SystemParameters.VerticalScrollBarWidth;
                        _scrollHorizontal.ViewportSize = clientSize.Width;
                        _scrollHorizontal.LargeChange = _scrollHorizontal.ViewportSize;
                        _scrollHorizontal.SmallChange = (int)(_scrollHorizontal.LargeChange / 10);
                    }
                    else
                        _scrollHorizontal.Visibility = Visibility.Collapsed;
                else
                    _scrollHorizontal.Visibility = Visibility.Collapsed;
            }
        }

        private ColumnRenderInformation GetStandardColumnRenderInfo()
        {
            return new ColumnRenderInformation
            {
                EditControlTopSpacingUsed = EditControlTopSpacing,
                GroupHeaderBottomSpacingUsed = GroupHeaderBottomSpacing,
                GroupHeaderTopSpacingUsed = GroupHeaderTopSpacing,
                GroupSpacingUsed = GroupSpacing,
                VerticalSpacingUsed = VerticalSpacing
            };
        }

        /// <summary>Shrinks the layout vertically if possible and desired</summary>
        /// <param name="availableSize">Available parent size</param>
        /// <param name="requiredSize">Size currently required</param>
        /// <returns>Size required after a potential shrinking pass</returns>
        private Size ShrinkVerticalIfNeeded(Size availableSize, Size requiredSize)
        {
            // If things didn't fit on the current form, we will try to squeeze things a bit (assuming the layout is elastic).
            if (requiredSize.Height > availableSize.Height && LayoutElasticity != LayoutElasticity.None)
            {
                // We know the overall height is too large. As a result, we have to take a look at each column and see if we need to shrink it
                var columnCount = 0;
                foreach (var column in _columnsUsed)
                {
                    columnCount++;
                    var columnRenderInfo = _columnRenderInformation[columnCount - 1];
                    var columnSize = CalculateColumnSize(column, columnCount - 1);
                    if (columnSize.Height > availableSize.Height)
                    {
                        // This one is too tall, so we should try to shrink it
                        var verticalOverage = columnSize.Height - availableSize.Height;

                        // Perhaps we can squeeze potential group headers a bit
                        var nonLine1HeaderCount = CountGroupHeadersInColumns(column, true);
                        if (nonLine1HeaderCount > 0)
                        {
                            var groupHeaderTopSqueezePotential = (columnRenderInfo.GroupHeaderTopSpacingUsed - MinGroupHeaderTopSpacing) * nonLine1HeaderCount;
                            var groupHeaderTopOverage = verticalOverage;
                            if (groupHeaderTopOverage >= groupHeaderTopSqueezePotential)
                            {
                                // Can't entirely handle the squeeze using top header spacing, but we will do our best
                                groupHeaderTopOverage = groupHeaderTopSqueezePotential;
                                verticalOverage -= groupHeaderTopOverage; // This is what still remains afterwards
                            }
                            else verticalOverage = 0d;
                            columnRenderInfo.GroupHeaderTopSpacingUsed -= groupHeaderTopOverage / nonLine1HeaderCount;
                        }

                        if (verticalOverage > 0)
                        {
                            var totalHeaderCount = CountGroupHeadersInColumns(column);
                            if (totalHeaderCount > 0)
                            {
                                var groupHeaderBottomSqueezePotential = (columnRenderInfo.GroupHeaderBottomSpacingUsed - MinGroupHeaderBottomSpacing) * totalHeaderCount;
                                var groupHeaderBottomOverage = verticalOverage;
                                if (groupHeaderBottomOverage >= groupHeaderBottomSqueezePotential)
                                {
                                    // Can't entirely handle the squeeze using bottom header spacing, but we will do our best
                                    groupHeaderBottomOverage = groupHeaderBottomSqueezePotential;
                                    verticalOverage -= groupHeaderBottomOverage; // This is what still remains afterwards
                                }
                                else verticalOverage = 0d;
                                columnRenderInfo.GroupHeaderBottomSpacingUsed -= groupHeaderBottomOverage / totalHeaderCount;
                            }
                        }

                        if (verticalOverage > 0)
                        {
                            var controlCount = column.Count;
                            if (controlCount > 0)
                            {
                                var verticalSpacingPotential = (columnRenderInfo.VerticalSpacingUsed - MinVerticalSpacing) * controlCount;
                                var verticalSpacingOverage = verticalOverage;
                                if (verticalSpacingOverage >= verticalSpacingPotential)
                                {
                                    // Can't entirely handle the squeeze using group spacing, but we will do our best
                                    verticalSpacingOverage = verticalSpacingPotential;
                                    verticalOverage -= verticalSpacingOverage; // This is what still remains afterwards
                                }
                                else verticalOverage = 0d;
                                columnRenderInfo.VerticalSpacingUsed -= verticalSpacingOverage / controlCount;
                            }
                        }

                        if (verticalOverage > 0)
                        {
                            var remainingGroupCount = CountGroupBreaksInColumns(column, true, true);
                            if (remainingGroupCount > 0)
                            {
                                var groupSqueezePotential = (columnRenderInfo.GroupSpacingUsed - MinGroupSpacing) * remainingGroupCount;
                                var groupOverage = verticalOverage;
                                if (groupOverage >= groupSqueezePotential)
                                {
                                    // Can't entirely handle the squeeze using group spacing, but we will do our best
                                    groupOverage = groupSqueezePotential;
                                }
                                columnRenderInfo.GroupSpacingUsed -= groupOverage / remainingGroupCount;
                            }
                        }
                    }
                }

                // We perform a second measuring operation with the automatically adjusted settings
                requiredSize = PerformMeasurePass(_columnsUsed);
            }

            return requiredSize;
        }

        private int CountGroupHeadersInColumns(IEnumerable<ControlPair> column, bool dontCountFirstLineHeaders = false)
        {
            var headersFound = 0;

            var pairCount = 0;
            foreach (var pair in column)
            {
                pairCount++;
                if (!string.IsNullOrEmpty(pair.GroupHeader))
                    if (pairCount > 1 || !dontCountFirstLineHeaders)
                        headersFound++;
            }
            return headersFound;
        }

        private int CountGroupBreaksInColumns(IEnumerable<ControlPair> column, bool dontCountFirstLineHeaders = false, bool dontCountGroupsWithCaptions = false)
        {
            var groupsFound = 0;

            var pairCount = 0;
            foreach (var pair in column)
            {
                pairCount++;
                var headerText = pair.GroupHeader;
                if (pair.GroupBreak)
                    if (pairCount > 1 || !dontCountFirstLineHeaders)
                        if (string.IsNullOrEmpty(headerText) || !dontCountGroupsWithCaptions)
                            groupsFound++;
                        else if (string.IsNullOrEmpty(headerText))
                            groupsFound++;
            }

            return groupsFound;
        }

        /// <summary>Shrinks the layout horizontally if possible and desired</summary>
        /// <param name="availableSize">Available parent size</param>
        /// <param name="requiredSize">Size currently required</param>
        /// <returns>Size required after a potential shrinking pass</returns>
        private Size ShrinkHorizontalIfNeeded(Size availableSize, Size requiredSize)
        {
            // If things didn't fit on the current form, we will try to squeeze things a bit (assuming the layout is elastic).
            if (requiredSize.Width > availableSize.Width && LayoutElasticity != LayoutElasticity.None)
            {
                var horizontalOverage = requiredSize.Width - availableSize.Width;
                var columnSpaceCount = _columnsUsed.Count - 1;
                if (columnSpaceCount > 0)
                {
                    // We can probably squeeze the column spacing a bit
                    var columnSpaceSqueezePotential = (ColumnSpacing - MinColumnSpacing)*columnSpaceCount;
                    var horizontalColumnSpaceOverage = horizontalOverage;
                    if (horizontalColumnSpaceOverage >= columnSpaceSqueezePotential)
                    {
                        // Can't entirely handle the squeeze using columns, but we will do our best
                        horizontalColumnSpaceOverage = columnSpaceSqueezePotential;
                        horizontalOverage -= horizontalColumnSpaceOverage; // This is what still remains afterwards
                    }
                    else horizontalOverage = 0d;
                    _columnSpacingUsed -= horizontalColumnSpaceOverage/columnSpaceCount;
                }

                if (horizontalOverage > 0 && LabelPosition == EditFormLabelPositions.Left) // We can probably get a bit of extra squeezing done by reducing the space between labels and edit controls
                {
                    // We can probably squeeze the column spacing a bit
                    var editSqueezePotential = (EditControlLeftSpacing - MinEditControlLeftSpacing)*_columnsUsed.Count;
                    var editOverage = horizontalOverage;
                    if (editOverage >= editSqueezePotential)
                    {
                        // Can't entirely handle the squeeze using edit spaces, but we will do our best
                        editOverage = editSqueezePotential;
                    }
                    _editControlLeftSpacingUsed -= editOverage/_columnsUsed.Count;
                }

                // We perform a second measuring operation with the automatically adjusted settings
                requiredSize = PerformMeasurePass(_columnsUsed);
            }

            return requiredSize;
        }

        /// <summary>Performs a single measuring pass based on current spacing assumptions</summary>
        /// <param name="columns">Collection of columns to measure</param>
        /// <returns></returns>
        private Size PerformMeasurePass(IEnumerable<List<ControlPair>> columns)
        {
            var totalSize = new Size();

            var columnCount = 0;
            foreach (var column in columns)
            {
                columnCount++;
                var columnSize = CalculateColumnSize(column, columnCount - 1);

                totalSize.Width += columnSize.Width;
                if (columnCount > 1) totalSize.Width += _columnSpacingUsed;
                totalSize.Height = Math.Max(columnSize.Height, totalSize.Height);
            }

            return totalSize;
        }

        /// <summary>
        /// This method is used to measure the size of the label part of the pair
        /// </summary>
        /// <param name="controlPair">The control pair.</param>
        /// <returns>Size.</returns>
        protected virtual Size MeasureLabelSize(ControlPair controlPair)
        {
            if (controlPair.Label != null)
            {
                controlPair.Label.Measure(new Size(100000, 100000));
                return GeometryHelper.NewSize(controlPair.Label.DesiredSize.Width, controlPair.Label.DesiredSize.Height);
            }
            return Size.Empty;
        }

        /// <summary>
        /// Performs a measuring pass on a single column
        /// </summary>
        /// <param name="column">The column to measure.</param>
        /// <param name="columnIndex">Index of the column.</param>
        /// <returns>Size required.</returns>
        private Size CalculateColumnSize(IEnumerable<ControlPair> column, int columnIndex)
        {
            var columnRenderInfo = _columnRenderInformation[columnIndex];
            double widestLabel = 0d, widestControl = 0d;
            var columnSize = new Size();
            var columnItemCount = 0;
            var spanColumnWidthRequired = 0d;
            var large = new Size(100000, 100000);
            var hasOpenBorder = false;

            foreach (var pair in column)
            {
                columnItemCount++;

                if (pair.GroupBreak && columnItemCount != 1)
                {
                    if (RenderGroupBackground) columnSize.Height += GroupBorderMargin.Bottom;
                    columnSize.Height += columnRenderInfo.GroupSpacingUsed;
                }

                if (pair.GroupBreak || columnItemCount == 1)
                {
                    if (RenderGroupBackground)
                    {
                        hasOpenBorder = true;
                        columnSize.Height += GroupBorderMargin.Top;
                    }

                    var headerText = pair.GroupHeader;
                    if (!string.IsNullOrEmpty(headerText))
                    {
                        if (columnItemCount > 1)
                            // We are not using the group spacing after all, but are instead using the header spacing
                            columnSize.Height -= columnRenderInfo.GroupSpacingUsed;

                        var text = new FormattedText(headerText, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface(GroupHeaderFontFamily, GroupHeaderFontStyle, GroupHeaderFontWeight, FontStretches.Normal), GroupHeaderFontSize, GroupHeaderForegroundBrush);
                        if (columnItemCount > 1)
                            columnSize.Height += columnRenderInfo.GroupHeaderTopSpacingUsed;

                        var heightAdded2 = text.Height + columnRenderInfo.GroupHeaderBottomSpacingUsed;
                        columnSize.Height += heightAdded2;
                    }
                }

                if (pair.Span != null)
                {
                    pair.Span.Measure(large);
                    columnSize.Height = pair.DesiredSpanHeight + columnRenderInfo.VerticalSpacingUsed;
                    spanColumnWidthRequired = Math.Max(spanColumnWidthRequired, pair.DesiredSpanWidth);
                }
                else
                {
                    var labelHeight = 0d;
                    var editHeight = 0d;

                    var desiredLabelSize = MeasureLabelSize(pair);
                    if (desiredLabelSize != Size.Empty)
                    {
                        widestLabel = Math.Max(desiredLabelSize.Width, widestLabel);
                        labelHeight = desiredLabelSize.Height;
                    }
                    if (pair.Edit != null)
                    {
                        pair.Edit.Measure(large);
                        foreach (var secondaryFlowControl in pair.SecondaryControls)
                            secondaryFlowControl.Measure(large);
                        widestControl = Math.Max(pair.DesiredTotalEditWidth, widestControl);
                        editHeight = pair.DesiredEditHeight;
                    }

                    if (LabelPosition == EditFormLabelPositions.Left)
                        columnSize.Height += Math.Max(labelHeight, editHeight) + columnRenderInfo.VerticalSpacingUsed;
                    else
                        columnSize.Height += labelHeight + columnRenderInfo.EditControlTopSpacingUsed + editHeight + columnRenderInfo.VerticalSpacingUsed;
                }
            }

            if (LabelPosition == EditFormLabelPositions.Left)
            {
                if (widestLabel > 0 && widestControl > 0)
                    columnSize.Width += widestLabel + _editControlLeftSpacingUsed + widestControl;
                else if (widestControl > 0)
                    columnSize.Width += widestControl;
                else if (widestLabel > 0)
                    columnSize.Width += widestLabel;
            }
            else
                columnSize.Width += Math.Max(widestLabel, widestControl);

            if (hasOpenBorder && RenderGroupBackground)
                columnSize.Height += GroupBorderMargin.Bottom;

            if (RenderGroupBackground)
                columnSize.Width += GroupBorderMargin.Left + GroupBorderMargin.Right;

            if (columnSize.Width < spanColumnWidthRequired)
                columnSize.Width = spanColumnWidthRequired;

            return columnSize;
        }

        private readonly List<List<Rect>> _controlPairPositionedRectangles = new List<List<Rect>>();

        /// <summary>Provides the behavior for the Arrange pass of Silverlight layout. Classes can override this method to define their own Arrange pass behavior.</summary>
        /// <param name="finalSize">The final area within the parent that this object should use to arrange itself and its children.</param>
        /// <returns>The actual size used once the element is arranged.</returns>
        protected override Size ArrangeOverride(Size finalSize)
        {
            InvalidateVisual();

            _groupBackgrounds.Clear();
            _headers.Clear();
            _controlPairPositionedRectangles.Clear();

            double totalHeight = 0d, totalWidth = 0d, currentY = 0d, currentX = 0d, lastBorderX = 0d, lastBorderY = 0d, lastBorderHeight = 0d;
            var columnCount = 0;
            var hasOpenBorder = false;

            foreach (var column in _columnsUsed)
            {
                _controlPairPositionedRectangles.Add(new List<Rect>());
                columnCount++;
                var columnRenderInfo = _columnRenderInformation[columnCount - 1];

                double columnHeight = 0d, currentColumnWidth = 0d;

                if (RenderGroupBackground)
                {
                    currentX += GroupBorderMargin.Left;
                    totalWidth += GroupBorderMargin.Left;
                }

                var controlWidths = CalculateWidestElements(column);

                // Now we perform the layout for this column
                var columnItemCount = 0;
                foreach (var pair in column)
                {
                    columnItemCount++;

                    if (pair.GroupBreak && columnItemCount != 1)
                    {
                        if (RenderGroupBackground) currentY += GroupBorderMargin.Bottom;
                        currentY += columnRenderInfo.GroupSpacingUsed;
                    }

                    if (pair.GroupBreak || columnItemCount == 1)
                    {
                        if (hasOpenBorder && RenderGroupBackground) 
                            AddBackgroundRenderInformation(lastBorderX - GroupBorderMargin.Left, lastBorderY, lastBorderHeight, currentColumnWidth);

                        if (RenderGroupBackground)
                        {
                            lastBorderY = currentY;
                            lastBorderX = currentX;
                            lastBorderHeight = 0d; //GroupBorderMargin.Top + GroupBorderMargin.Bottom;
                            hasOpenBorder = true;
                            currentY += GroupBorderMargin.Top;
                        }

                        var headerText = pair.GroupHeader;
                        if (!string.IsNullOrEmpty(headerText))
                        {
                            if (columnItemCount > 1)
                            {
                                // We are not using the group spacing after all, but are instead using the header spacing
                                currentY -= columnRenderInfo.GroupSpacingUsed;
                                lastBorderHeight -= columnRenderInfo.GroupSpacingUsed; 
                            }

                            var text = new FormattedText(headerText, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface(GroupHeaderFontFamily, GroupHeaderFontStyle, GroupHeaderFontWeight, FontStretches.Normal), GroupHeaderFontSize, GroupHeaderForegroundBrush);
                            if (columnItemCount > 1)
                            {
                                currentY += columnRenderInfo.GroupHeaderTopSpacingUsed;
                                lastBorderHeight += columnRenderInfo.GroupHeaderTopSpacingUsed;
                            }
                            var headerRect = GeometryHelper.NewRect(currentX, currentY, controlWidths.Item1 + controlWidths.Item2 + _editControlLeftSpacingUsed + _columnSpacingUsed - 5, text.Height);
                            if (LabelPosition == EditFormLabelPositions.Top)
                                headerRect = GeometryHelper.NewRect(currentX, currentY, Math.Max(controlWidths.Item1, controlWidths.Item2) + _columnSpacingUsed - 5, text.Height);
                            _headers.Add(new AutoHeaderTextRenderInfo { RenderRect = headerRect, Text = headerText, FormattedText = text });

                            var heightAdded2 = text.Height + columnRenderInfo.GroupHeaderBottomSpacingUsed;
                            currentY += heightAdded2;
                            lastBorderHeight += heightAdded2;
                        }
                    }

                    Rect labelRect = Rect.Empty, editRect = Rect.Empty, spanRect = Rect.Empty;
                    if (pair.Span != null)
                        spanRect = GeometryHelper.NewRect(currentX, currentY, pair.DesiredSpanWidth, pair.DesiredSpanHeight);
                    else
                    {
                        if (LabelPosition == EditFormLabelPositions.Left)
                        {
                            if (pair.Label != null)
                                labelRect = GeometryHelper.NewRect(currentX, currentY, controlWidths.Item1, pair.DesiredLabelHeight);
                            else
                                CustomLabelHandlingOverride(pair, currentX, currentY, controlWidths.Item1, new Size());
                            if (pair.Edit != null)
                            {
                                if (controlWidths.Item1 > 0) // If there is a label in the entire column that is wider than 0, we always allow spacing for it
                                    editRect = GeometryHelper.NewRect(currentX + _editControlLeftSpacingUsed + controlWidths.Item1, currentY, pair.DesiredEditWidth, pair.DesiredEditHeight);
                                else
                                    editRect = GeometryHelper.NewRect(currentX + controlWidths.Item1, currentY, pair.DesiredEditWidth, pair.DesiredEditHeight);
                            }
                        }
                        else
                        {
                            var customLabelOffset = new Size();
                            if (!CustomLabelHandlingOverride(pair, currentX, currentY, controlWidths.Item1, customLabelOffset) || pair.Label != null)
                            {
                                labelRect = GeometryHelper.NewRect(currentX, currentY, controlWidths.Item1, pair.DesiredLabelHeight);
                                currentY += pair.DesiredLabelHeight + columnRenderInfo.EditControlTopSpacingUsed;
                                lastBorderHeight += pair.DesiredLabelHeight + columnRenderInfo.EditControlTopSpacingUsed;
                            }
                            else
                            {
                                var newWidth = controlWidths.Item1;
                                if (customLabelOffset.Width > 0d) newWidth = customLabelOffset.Width;
                                var newHeight = pair.DesiredLabelHeight;
                                if (customLabelOffset.Height > 0d) newHeight = customLabelOffset.Height;
                                labelRect = GeometryHelper.NewRect(currentX, currentY, newWidth, newHeight);
                                currentY += newHeight + columnRenderInfo.EditControlTopSpacingUsed;
                                lastBorderHeight += newHeight + columnRenderInfo.EditControlTopSpacingUsed;
                            }
                            if (pair.Edit != null)
                                editRect = GeometryHelper.NewRect(currentX, currentY, pair.DesiredEditWidth, pair.DesiredEditHeight);
                        }
                    }

                    if (pair.Label != null && pair.Edit != null && LabelPosition == EditFormLabelPositions.Left && VerticalLabelControlOffset > 0d && pair.Label.GetType() != pair.Edit.GetType())
                        labelRect.Y += VerticalLabelControlOffset;

                    if (_mustScale)
                    {
                        if (pair.Label != null) labelRect.Scale(_scale.ScaleX, _scale.ScaleY);
                        if (pair.Edit != null) editRect.Scale(_scale.ScaleX, _scale.ScaleY);
                    }

                    if (_scrollHorizontal.Visibility == Visibility.Visible)
                    {
                        if (pair.Label != null) labelRect.X += (_scrollHorizontal.Value * _scale.ScaleX) * -1;
                        if (pair.Edit != null) editRect.X += (_scrollHorizontal.Value * _scale.ScaleX) * -1;
                        if (pair.Span != null) spanRect.X += (_scrollHorizontal.Value * _scale.ScaleX) * -1;
                    }
                    if (_scrollVertical.Visibility == Visibility.Visible)
                    {
                        if (pair.Label != null) labelRect.Y += (_scrollVertical.Value * _scale.ScaleY) * -1;
                        if (pair.Edit != null) editRect.Y += (_scrollVertical.Value * _scale.ScaleY) * -1;
                        if (pair.Span != null) spanRect.Y += (_scrollVertical.Value * _scale.ScaleY) * -1;
                    }

                    if (pair.Label != null) pair.Label.Arrange(labelRect);
                    if (pair.Edit != null) pair.Edit.Arrange(editRect);
                    if (pair.Span != null) pair.Span.Arrange(spanRect);

                    var secondaryX = editRect.Right + (FlowWithPreviousSpacing * _scale.ScaleX);
                    var secondaryY = editRect.Y;
                    foreach (var secondaryControl in pair.SecondaryControls)
                    {
                        var secondaryWidth = secondaryControl.DesiredSize.Width*_scale.ScaleX;
                        var secondaryHeight = secondaryControl.DesiredSize.Height*_scale.ScaleY;
                        var secondaryRect = GeometryHelper.NewRect(secondaryX, secondaryY, secondaryWidth, secondaryHeight);
                        secondaryControl.Arrange(secondaryRect);
                        secondaryX = secondaryRect.Right + (FlowWithPreviousSpacing * _scale.ScaleX);
                    }

                    var resultingTop = Math.Min(Math.Min(labelRect.Top, editRect.Top), spanRect.Top);
                    var resultingLeft = Math.Min(Math.Min(labelRect.Left, editRect.Left), spanRect.Left);
                    var resultingHeight = Math.Max(Math.Max(labelRect.Bottom, editRect.Bottom), spanRect.Bottom) - resultingTop;
                    var resultingWidth = Math.Max(Math.Max(labelRect.Right, editRect.Right), spanRect.Right) - resultingLeft;
                    // TODO: Should this be supported with secondary flow controls as well?
                    _controlPairPositionedRectangles[_controlPairPositionedRectangles.Count - 1].Add(GeometryHelper.NewRect(resultingLeft, resultingTop, resultingWidth, resultingHeight));

                    double currentItemWidth;
                    if (LabelPosition == EditFormLabelPositions.Top)
                        currentItemWidth = Math.Max(Math.Max(editRect.Width, labelRect.Width), spanRect.Width);
                    else
                    {
                        if (pair.Span == null)
                            currentItemWidth = controlWidths.Item1 + _editControlLeftSpacingUsed + editRect.Width;
                        else
                            currentItemWidth = spanRect.Width;
                    }

                    foreach (var secondaryControl in pair.SecondaryControls)
                        currentItemWidth += FlowWithPreviousSpacing + secondaryControl.DesiredSize.Width;

                    currentColumnWidth = Math.Max(currentColumnWidth, currentItemWidth);

                    var heightAdded = Math.Max(Math.Max(pair.DesiredLabelHeight, pair.DesiredEditHeight), spanRect.Height) + columnRenderInfo.VerticalSpacingUsed;
                    currentY += heightAdded;
                    lastBorderHeight += heightAdded;
                    columnHeight = currentY;
                }

                if (hasOpenBorder && RenderGroupBackground)
                {
                    AddBackgroundRenderInformation(lastBorderX - GroupBorderMargin.Left, lastBorderY, lastBorderHeight, currentColumnWidth);
                    hasOpenBorder = false;
                    columnHeight += GroupBorderMargin.Bottom;
                }

                if (LabelPosition == EditFormLabelPositions.Left)
                    totalWidth += currentColumnWidth + (columnCount == 1 ? 0 : _columnSpacingUsed);
                else
                    totalWidth += currentColumnWidth + (columnCount == 1 ? 0 : _columnSpacingUsed);

                totalHeight = Math.Max(columnHeight, totalHeight);

                currentX = totalWidth + _columnSpacingUsed;
                if (RenderGroupBackground)
                {
                    currentX += GroupBorderMargin.Right;
                    totalWidth += GroupBorderMargin.Right;
                }
                currentY = 0;
            }

            var arrangedSize = GeometryHelper.NewSize(totalWidth, totalHeight);

            // We also handle optional scrollbars
            HandleScrollBars(arrangedSize, finalSize);

            base.ArrangeOverride(finalSize);

            if (finalSize.Height > 0 && finalSize.Width > 0 && !double.IsInfinity(finalSize.Height) && !double.IsInfinity(finalSize.Width))
                return finalSize;
            return arrangedSize;
        }

        /// <summary>
        /// This method can be used to create custom label handling code.
        /// Override this method if you want to handle the label logic in a subclass.
        /// </summary>
        /// <param name="controlPair">The control pair for which the label needs to be handled.</param>
        /// <param name="currentX">The current X position in the overall layout.</param>
        /// <param name="currentY">The current Y position in the overall layout.</param>
        /// <param name="width">The suggested width for the label.</param>
        /// <param name="customLabelOffset">Set this parameter to move the X and Y coordinates as a result of custom handling</param>
        /// <returns>Return true if custom handling code takes over and no default label handling is needed</returns>
        protected virtual bool CustomLabelHandlingOverride(ControlPair controlPair, double currentX, double currentY, double width, Size customLabelOffset)
        {
            return false;
        }

        private static Tuple<double, double> CalculateWidestElements(IEnumerable<ControlPair> column)
        {
            // We first need to know the widest elements, since all controls need to be aligned according to those controls
            var widestLabel = 0d;
            var widestControl = 0d;
            foreach (var pair in column)
            {
                widestLabel = Math.Max(pair.DesiredLabelWidth, widestLabel);
                widestControl = Math.Max(pair.DesiredTotalEditWidth, widestControl);
            }
            return new Tuple<double, double>(widestLabel, widestControl);
        }

        private void AddBackgroundRenderInformation(double left, double top, double height, double width)
        {
            _groupBackgrounds.Add(new GroupBackgroundRenderInfo
            {
                Background = GroupBackgroundBrush,
                Border = GroupBorderBrush,
                BorderWidth = GroupBorderWidth,
                RenderRect = GeometryHelper.NewRect(
                    left, 
                    top,
                    width/_scale.ScaleX + GroupBorderMargin.Left + GroupBorderMargin.Right,
                    height + GroupBorderMargin.Top + GroupBorderMargin.Bottom)
            });
        }

        private readonly List<AutoHeaderTextRenderInfo> _headers = new List<AutoHeaderTextRenderInfo>();
        private readonly List<GroupBackgroundRenderInfo> _groupBackgrounds = new List<GroupBackgroundRenderInfo>();
        private Size _measuredSize;

        /// <summary>Iterates over all the controls and returns them in columns and tuples</summary>
        /// <returns>Columns of control pairs</returns>
        protected virtual List<List<ControlPair>> GetColumns()
        {
            var columns = new List<List<ControlPair>> {new List<ControlPair>()};
            var currentColumn = columns[0];
            for (var controlCounter = 0; controlCounter < Children.Count; controlCounter++)
            {
                var child = Children[controlCounter];

                if (child.Visibility == Visibility.Collapsed) continue;

                if (SimpleView.GetColumnBreak(child))
                {
                    columns.Add(new List<ControlPair>());
                    currentColumn = columns[columns.Count - 1];
                }

                var controlPair = new ControlPair(FlowWithPreviousSpacing);

                if (child is HeaderedContentControl || child is TabControl || SimpleView.GetSpanFullWidth(child))
                    controlPair.Span = child;
                else
                {
                    if (IsControlStandAlone(child))
                        controlPair.Edit = child;
                    else
                    {
                        controlPair.Label = child;

                        if (!SimpleView.GetIsStandAloneLabel(child))
                        {
                            var editControlIndex = controlCounter + 1;
                            if (Children.Count > editControlIndex)
                                controlPair.Edit = Children[editControlIndex];
                            controlCounter++; // We are skipping the next control since we already accounted for it
                        }
                    }

                    while (true) // We check whether the next control(s) flow(s) with the previous as secondary controls
                    {
                        if (Children.Count <= controlCounter + 1) break;
                        var nextChild = Children[controlCounter + 1];
                        if (!DoesControlFlowWithPrevious(nextChild)) break;
                        controlPair.SecondaryControls.Add(nextChild);
                        controlCounter++;
                    }
                }
                currentColumn.Add(controlPair);
            }
            return columns;
        }

        /// <summary>
        /// Returns true if the object passed is a stand-alone control
        /// </summary>
        /// <param name="obj">The control object.</param>
        /// <returns><c>true</c> if [is control stand alone] [the specified obj]; otherwise, <c>false</c>.</returns>
        protected virtual bool IsControlStandAlone(DependencyObject obj)
        {
            return SimpleView.GetIsStandAloneEditControl(obj);
        }

        /// <summary>
        /// Returns true if the passed object is a control that is meant to flow with the previous control
        /// rather than forming a new pair.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        protected virtual bool DoesControlFlowWithPrevious(DependencyObject obj)
        {
            return SimpleView.GetFlowsWithPrevious(obj);
        }

        /// <summary>
        /// Draws the content of a <see cref="T:System.Windows.Media.DrawingContext"/> object during the render pass of a <see cref="T:System.Windows.Controls.Panel"/> element.
        /// </summary>
        /// <param name="dc">The <see cref="T:System.Windows.Media.DrawingContext"/> object to draw.</param>
        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            var scale = 1d;
            if (_mustScale) scale = _scale.ScaleX;

            var offset = new Point();
            if (_scrollHorizontal.Visibility == Visibility.Visible) offset.X += (_scrollHorizontal.Value * _scale.ScaleX) * -1;
            if (_scrollVertical.Visibility == Visibility.Visible) offset.Y += (_scrollVertical.Value * _scale.ScaleY) * -1;
            
            if (_headers.Count != 0)
            {
                if (GroupHeaderRenderer == null) GroupHeaderRenderer = new AutoLayoutHeaderRenderer();

                foreach (var bg in _groupBackgrounds)
                    GroupHeaderRenderer.RenderBackground(dc, bg, scale, offset);

                foreach (var header in _headers)
                    GroupHeaderRenderer.RenderHeader(dc, header, scale, offset);
            }

            if (RenderHorizontalControlSeparatorLines && HorizontalLineSeparatorBrush != null)
            {
                var pen = new Pen(HorizontalLineSeparatorBrush, 1d);
                foreach (var column in _controlPairPositionedRectangles)
                {
                    var right = column.Max(c => c.Right);
                    right = (double)(int) right;
                    foreach (var rect in column)
                    {
                        var y = rect.Bottom + VerticalControlSeparatorOffset;
                        y = (int) y;
                        y += .5d;
                        var x = (double) (int) rect.X;
                        dc.DrawLine(pen, new Point(x, y), new Point(right, y));
                    }
                }
            }

            OnRenderCustom(dc, scale, offset);
        }

        /// <summary>
        /// This method is designed to be overridden in subclasses to provide additional render functionality
        /// </summary>
        /// <param name="dc">The drawing context.</param>
        /// <param name="scale">The current scale that shall be applied to rendering</param>
        /// <param name="offset">The current offset that shall be applied to rendering.</param>
        protected virtual void OnRenderCustom(DrawingContext dc, double scale, Point offset)
        {
            
        }

        private class ColumnRenderInformation
        {
            public ColumnRenderInformation()
            {
                EditControlTopSpacingUsed = -1;
                GroupHeaderBottomSpacingUsed = -1;
                GroupHeaderTopSpacingUsed = -1;
                GroupSpacingUsed = -1;
                VerticalSpacingUsed = -1;
            }

            public double EditControlTopSpacingUsed { get; set; }
            public double GroupHeaderBottomSpacingUsed { get; set; }
            public double GroupHeaderTopSpacingUsed { get; set; }
            public double GroupSpacingUsed { get; set; }
            public double VerticalSpacingUsed { get; set; }
        }
    }

    /// <summary>
    /// This class is used to represent a "pair" of controls
    /// </summary>
    public class ControlPair
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ControlPair" /> class.
        /// </summary>
        /// <param name="secondarySpacing">The spacing for secondary controls.</param>
        public ControlPair(double secondarySpacing)
        {
            _secondarySpacing = secondarySpacing;
            SecondaryControls = new List<UIElement>();
            AdditionalEditControls = new List<UIElement>();
        }

        /// <summary>The label element</summary>
        public UIElement Label { get; set; }
        /// <summary>The edit element</summary>
        public UIElement Edit { get; set; }
        /// <summary>This element (if set) spans both label and edit control</summary>
        public UIElement Span { get; set; }

        private readonly double _secondarySpacing;

        /// <summary>List of controls that flow with this control pair as secondary "edit" controls</summary>
        public List<UIElement> SecondaryControls { get; set; }

        private double _desiredLabelWidthOverride = -1d;

        /// <summary>
        /// Desired width for the label element
        /// </summary>
        public double DesiredLabelWidth
        {
            get
            {
                if (_desiredLabelWidthOverride > -1) return _desiredLabelWidthOverride;
                return Label != null ? Label.DesiredSize.Width : 0d;
            }
        }

        /// <summary>
        /// This method can be used to override the width of the label
        /// </summary>
        /// <param name="width">Width the label is to be forced to</param>
        public void ForceLabelWidth(double width)
        {
            _desiredLabelWidthOverride = width;
        }

        /// <summary>
        /// Desired height for the label element
        /// </summary>
        /// <value>The height of the desired label.</value>
        public double DesiredLabelHeight
        {
            get { return Label != null ? Label.DesiredSize.Height : 0d; }
        }

        /// <summary>
        /// Desired width of the edit control
        /// </summary>
        /// <value>The width of the desired edit.</value>
        public double DesiredEditWidth
        {
            get
            {
                var desiredWidth = 0d;
                if (Edit != null)
                    desiredWidth = Edit.DesiredSize.Width;
                return desiredWidth;
            }
        }

        /// <summary>
        /// Desired width for the total label element
        /// </summary>
        /// <value>The width of the desired total edit.</value>
        public double DesiredTotalEditWidth
        {
            get
            {
                var desiredWidth = 0d;
                if (Edit != null)
                    desiredWidth = Edit.DesiredSize.Width;

                foreach (var control in SecondaryControls)
                    desiredWidth += _secondarySpacing + control.DesiredSize.Width;

                return desiredWidth;
            }
        }

        /// <summary>
        /// Desired height for the edit part of the pair
        /// </summary>
        /// <value>The height of the desired edit.</value>
        public double DesiredEditHeight
        {
            get
            {
                var desiredHeight = 0d;
                if (Edit != null)
                    desiredHeight = Edit.DesiredSize.Height;

                foreach (var control in SecondaryControls)
                    desiredHeight = Math.Max(desiredHeight, control.DesiredSize.Height);

                return desiredHeight;
            }
        }

        /// <summary>
        /// Desired width for a span element
        /// </summary>
        /// <value>The width of the desired span.</value>
        public double DesiredSpanWidth
        {
            get { return Span != null ? Span.DesiredSize.Width : 0d; }
        }

        /// <summary>
        /// Desired height for a span element
        /// </summary>
        /// <value>The height of the desired span.</value>
        public double DesiredSpanHeight
        {
            get { return Span != null ? Span.DesiredSize.Height : 0d; }
        }

        private bool _groupBreakSet;
        private bool _groupBreak;

        /// <summary>
        /// Indicates whether this pair starts a group break
        /// </summary>
        public bool GroupBreak
        {
            get
            {
                if (_groupBreakSet) return _groupBreak;

                var groupBreak = false;
                if (Label != null && SimpleView.GetGroupBreak(Label)) groupBreak = true;
                if (Edit != null && SimpleView.GetGroupBreak(Edit)) groupBreak = true;
                if (Span != null && SimpleView.GetGroupBreak(Span)) groupBreak = true;
                _groupBreak = groupBreak;
                _groupBreakSet = true;
                return _groupBreak;
            }
        }

        /// <summary>
        /// This method can be used to force a group break before this pair
        /// </summary>
        public void ForceGroupBreak()
        {
            _groupBreak = true;
        }

        /// <summary>
        /// Group header text
        /// </summary>
        public string GroupHeader
        {
            get
            {
                var text = string.Empty;

                if (Label != null) text = SimpleView.GetGroupTitle(Label);
                if (string.IsNullOrEmpty(text) && Edit != null) text = SimpleView.GetGroupTitle(Edit);
                if (string.IsNullOrEmpty(text) && Span != null) text = SimpleView.GetGroupTitle(Span);

                return text;
            }
        }

        /// <summary>
        /// List of additional UI controls that create a flow
        /// </summary>
        public List<UIElement> AdditionalEditControls { get; private set; }

        /// <summary>Indicates whether the label control spans the full available width of the property sheet (no edit controls are displayed in that case)</summary>
        /// <value><c>true</c> if [label spans full width]; otherwise, <c>false</c>.</value>
        public bool LabelSpansFullWidth { get; set; }
    }

    /// <summary>Defines the position of the labels in an edit form</summary>
    public enum EditFormLabelPositions
    {
        /// <summary>The label is to the left of the edit control</summary>
        Left,
        /// <summary>The label is positioned to the top of the edit control</summary>
        Top
    }

    /// <summary>Defines whether the layout can change smartly</summary>
    public enum LayoutElasticity
    {
        /// <summary>No auto-resizing</summary>
        None,
        /// <summary>The layout can change dynamically</summary>
        Layout,
        /// <summary>The layout can change dynamically and will scale down if need be</summary>
        LayoutAndScale,
        /// <summary>The layout can change dynamically and columns will re-flow is need be</summary>
        LayoutAndReflow
    }

    /// <summary>Defines which scrollbars are handled automatically in an edit form</summary>
    public enum EditFormScrollBarModes
    {
        /// <summary>No automatic scrollbar handling</summary>
        None,
        /// <summary>Automatically handle the horizontal scrollbar</summary>
        Horizontal,
        /// <summary>Automatically handle the vertical scrollbar</summary>
        Vertical,
        /// <summary>Automatically handle both scrollbars</summary>
        Both
    }

    /// <summary>Adorner UI for scrollbars of the edit form control</summary>
    public class EditFormScrollAdorner : Adorner
    {
        private readonly ScrollBar _horizontal;
        private readonly ScrollBar _vertical;

        /// <summary>Constructor</summary>
        /// <param name="adornedElement">Adorned element EditForm</param>
        /// <param name="horizontal">The horizontal scrollbar.</param>
        /// <param name="vertical">The vertical scrollbar.</param>
        public EditFormScrollAdorner(EditForm adornedElement, ScrollBar horizontal, ScrollBar vertical) : base(adornedElement)
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
            if (AdornedElement == null) return finalSize; // Not much we can do here

            var surfaceSize = AdornedElement.RenderSize;

            if (_horizontal.Visibility == Visibility.Visible)
                _horizontal.Arrange(GeometryHelper.NewRect(0, surfaceSize.Height - SystemParameters.HorizontalScrollBarHeight, _vertical.Visibility == Visibility.Visible ?  surfaceSize.Width - SystemParameters.VerticalScrollBarWidth : surfaceSize.Width, SystemParameters.HorizontalScrollBarHeight));
            if (_vertical.Visibility == Visibility.Visible)
                _vertical.Arrange(GeometryHelper.NewRect(surfaceSize.Width - SystemParameters.VerticalScrollBarWidth, 0, SystemParameters.VerticalScrollBarWidth, _horizontal.Visibility == Visibility.Visible ? surfaceSize.Height - SystemParameters.HorizontalScrollBarHeight : surfaceSize.Height));

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
                var adorner = parent as EditFormScrollAdorner;
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

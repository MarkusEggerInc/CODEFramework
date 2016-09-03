using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace CODE.Framework.Wpf.Controls
{
    /// <summary>
    /// Range slider with low and high value
    /// </summary>
    [TemplatePart(Name = "PART_Track", Type = typeof (FrameworkElement))]
    [TemplatePart(Name = "PART_Handle1", Type = typeof(FrameworkElement))]
    [TemplatePart(Name = "PART_Handle2", Type = typeof(FrameworkElement))]
    [TemplatePart(Name = "PART_SelectedRange", Type = typeof(FrameworkElement))]
    public class RangeSlider : Control
    {
        /// <summary>
        /// Constructor
        /// </summary>
        static RangeSlider()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof (RangeSlider), new FrameworkPropertyMetadata(typeof (RangeSlider)));
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public RangeSlider()
        {
            if (TryFindResource(typeof (RangeSlider)) == null)
                Resources.MergedDictionaries.Add(new ResourceDictionary {Source = new Uri("pack://application:,,,/CODE.Framework.Wpf;component/styles/RangeSlider.xaml", UriKind.Absolute)});

            UpdateTrackPosition();
        }

        /// <summary>
        /// Low value
        /// </summary>
        public double LowValue
        {
            get { return (double) GetValue(LowValueProperty); }
            set { SetValue(LowValueProperty, value); }
        }

        /// <summary>
        /// Low value
        /// </summary>
        public static readonly DependencyProperty LowValueProperty = DependencyProperty.Register("LowValue", typeof (double), typeof (RangeSlider), new FrameworkPropertyMetadata(25d, OnLowValueChanged) {BindsTwoWayByDefault = true});

        /// <summary>
        /// Handles the <see cref="E:LowValueChanged" /> event.
        /// </summary>
        /// <param name="d">The d.</param>
        /// <param name="e">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void OnLowValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var slider = d as RangeSlider;
            if (slider == null) return;
            if (!slider._suspendFinalLowValueUpdate)
            {
                slider._inLowValueFinalUpdate = true;
                slider.LowValueFinal = slider.LowValue;
                slider._inLowValueFinalUpdate = false;
            }
            slider.UpdateTrackPosition();

            var handler = slider.LowValueUpdated;
            if (handler != null)
                handler(slider, new EventArgs());
        }

        /// <summary>
        /// Low value (not updated during drag operations)
        /// </summary>
        public double LowValueFinal
        {
            get { return (double) GetValue(LowValueFinalProperty); }
            set { SetValue(LowValueFinalProperty, value); }
        }

        /// <summary>
        /// Low value (not updated during drag operations)
        /// </summary>
        public static readonly DependencyProperty LowValueFinalProperty = DependencyProperty.Register("LowValueFinal", typeof (double), typeof (RangeSlider), new FrameworkPropertyMetadata(25d, OnLowValueFinalChanged) {BindsTwoWayByDefault = true});

        /// <summary>
        /// Handles the <see cref="E:LowValueFinalChanged" /> event.
        /// </summary>
        /// <param name="d">The d.</param>
        /// <param name="e">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void OnLowValueFinalChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var slider = d as RangeSlider;
            if (slider == null) return;
            if (!slider._inLowValueFinalUpdate)
                slider.LowValue = slider.LowValueFinal;

            var handler = slider.LowValueFinalUpdated;
            if (handler != null)
                handler(slider, new EventArgs());
        }

        /// <summary>
        /// High value
        /// </summary>
        public double HighValue
        {
            get { return (double) GetValue(HighValueProperty); }
            set { SetValue(HighValueProperty, value); }
        }

        /// <summary>
        /// High value
        /// </summary>
        public static readonly DependencyProperty HighValueProperty = DependencyProperty.Register("HighValue", typeof (double), typeof (RangeSlider), new FrameworkPropertyMetadata(75d, OnHighValueChanged) {BindsTwoWayByDefault = true});

        /// <summary>
        /// Handles the <see cref="E:HighValueChanged" /> event.
        /// </summary>
        /// <param name="d">The d.</param>
        /// <param name="e">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void OnHighValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var slider = d as RangeSlider;
            if (slider == null) return;
            if (!slider._suspendFinalHighValueUpdate)
            {
                slider._inHighValueFinalUpdate = true;
                slider.HighValueFinal = slider.HighValue;
                slider._inHighValueFinalUpdate = false;
            }
            slider.UpdateTrackPosition();

            var handler = slider.HighValueUpdated;
            if (handler != null)
                handler(slider, new EventArgs());
        }

        /// <summary>
        /// High value (not updated during drag operations)
        /// </summary>
        public double HighValueFinal
        {
            get { return (double) GetValue(HighValueFinalProperty); }
            set { SetValue(HighValueFinalProperty, value); }
        }

        /// <summary>
        /// High value (not updated during drag operations)
        /// </summary>
        public static readonly DependencyProperty HighValueFinalProperty = DependencyProperty.Register("HighValueFinal", typeof (double), typeof (RangeSlider), new FrameworkPropertyMetadata(75d, OnHighValueFinalChanged) {BindsTwoWayByDefault = true});

        /// <summary>
        /// Handles the <see cref="E:HighValueFinalChanged" /> event.
        /// </summary>
        /// <param name="d">The d.</param>
        /// <param name="e">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void OnHighValueFinalChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var slider = d as RangeSlider;
            if (slider == null) return;
            if (!slider._inHighValueFinalUpdate)
                slider.HighValue = slider.HighValueFinal;

            var handler = slider.HighValueFinalUpdated;
            if (handler != null)
                handler(slider, new EventArgs());
        }

        /// <summary>
        /// Minimum value
        /// </summary>
        public double Minimum
        {
            get { return (double) GetValue(MinimumProperty); }
            set { SetValue(MinimumProperty, value); }
        }

        /// <summary>
        /// Minimum value
        /// </summary>
        public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register("Minimum", typeof (double), typeof (RangeSlider), new FrameworkPropertyMetadata(0d, RefreshAll) {BindsTwoWayByDefault = true});

        /// <summary>
        /// Minimum value
        /// </summary>
        public double Maximum
        {
            get { return (double) GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }

        /// <summary>
        /// Minimum value
        /// </summary>
        public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register("Maximum", typeof (double), typeof (RangeSlider), new FrameworkPropertyMetadata(100d, RefreshAll) {BindsTwoWayByDefault = true});

        /// <summary>
        /// Orientation
        /// </summary>
        public Orientation Orientation
        {
            get { return (Orientation) GetValue(OrientationProperty); }
            set { SetValue(OrientationProperty, value); }
        }

        /// <summary>
        /// Orientation
        /// </summary>
        public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register("Orientation", typeof (Orientation), typeof (RangeSlider), new PropertyMetadata(Orientation.Horizontal, RefreshAll));

        /// <summary>
        /// When overridden in a derived class, is invoked whenever application code or internal processes call System.Windows.FrameworkElement.ApplyTemplate.
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            Track = GetTemplateChild("PART_Track") as FrameworkElement;
            Handle1 = GetTemplateChild("PART_Handle1") as FrameworkElement;
            Handle2 = GetTemplateChild("PART_Handle2") as FrameworkElement;
            SelectedRange = GetTemplateChild("PART_SelectedRange") as FrameworkElement;
        }

        /// <summary>
        /// Brush for the selected range ("track")
        /// </summary>
        public Brush SelectedRangeBrush
        {
            get { return (Brush) GetValue(SelectedRangeBrushProperty); }
            set { SetValue(SelectedRangeBrushProperty, value); }
        }

        /// <summary>
        /// Brush for the selected range ("track")
        /// </summary>
        public static readonly DependencyProperty SelectedRangeBrushProperty = DependencyProperty.Register("SelectedRangeBrush", typeof (Brush), typeof (RangeSlider), new PropertyMetadata(Brushes.CornflowerBlue));

        /// <summary>
        /// Brush for the selected range ("track")
        /// </summary>
        public Brush RangeBrush
        {
            get { return (Brush) GetValue(RangeBrushProperty); }
            set { SetValue(RangeBrushProperty, value); }
        }

        /// <summary>
        /// Brush for the selected range ("track")
        /// </summary>
        public static readonly DependencyProperty RangeBrushProperty = DependencyProperty.Register("RangeBrush", typeof (Brush), typeof (RangeSlider), new PropertyMetadata(Brushes.Silver));

        /// <summary>
        /// Brush used for tick marks as well as labels
        /// </summary>
        public Brush TickBrush
        {
            get { return (Brush) GetValue(TickBrushProperty); }
            set { SetValue(TickBrushProperty, value); }
        }

        /// <summary>
        /// Brush used for tick marks as well as labels
        /// </summary>
        public static readonly DependencyProperty TickBrushProperty = DependencyProperty.Register("TickBrush", typeof (Brush), typeof (RangeSlider), new PropertyMetadata(Brushes.DarkGray));

        /// <summary>
        /// Font size for labels
        /// </summary>
        public double LabelFontSize
        {
            get { return (double) GetValue(LabelFontSizeProperty); }
            set { SetValue(LabelFontSizeProperty, value); }
        }

        /// <summary>
        /// Font size for labels
        /// </summary>
        public static readonly DependencyProperty LabelFontSizeProperty = DependencyProperty.Register("LabelFontSize", typeof (double), typeof (RangeSlider), new PropertyMetadata(10d));

        /// <summary>
        /// An inverted range slider goes from highest to lowest, rather than lowest to highest
        /// </summary>
        /// <value><c>true</c> if this instance is inverted; otherwise, <c>false</c>.</value>
        public bool IsInverted
        {
            get { return (bool)GetValue(IsInvertedProperty); }
            set { SetValue(IsInvertedProperty, value); }
        }
        /// <summary>
        /// An inverted range slider goes from highest to lowest, rather than lowest to highest
        /// </summary>
        /// <value><c>true</c> if this instance is inverted; otherwise, <c>false</c>.</value>
        public static readonly DependencyProperty IsInvertedProperty = DependencyProperty.Register("IsInverted", typeof(bool), typeof(RangeSlider), new PropertyMetadata(false, RefreshAll));

        private double _lowValueLeftMargin;
        private double _highValueRightMargin;
        private double _highValueTopMargin;
        private double _lowValueBottomMargin;
        private Point _lowDragInitialPosition;
        private Point _highDragInitialPosition;
        private Point _selectedDragInitialPosition;
        private double _lowDragInitialValue;
        private double _highDragInitialValue;
        private double _selectedDragInitialLowValue;
        private double _selectedDragInitialHighValue;
        private bool _suspendFinalLowValueUpdate;
        private bool _suspendFinalHighValueUpdate;
        private bool _inLowValueFinalUpdate;
        private bool _inHighValueFinalUpdate;

        /// <summary>
        /// Track
        /// </summary>
        public FrameworkElement Track { get; set; }

        /// <summary>
        /// Low range handle
        /// </summary>
        public FrameworkElement Handle1 { get; set; }

        /// <summary>
        /// High range handle
        /// </summary>
        public FrameworkElement Handle2 { get; set; }

        /// <summary>
        /// Selected Track
        /// </summary>
        public FrameworkElement SelectedRange { get; set; }

        /// <summary>
        /// This event fires when the LowValue property is updated
        /// </summary>
        public event EventHandler LowValueUpdated;

        /// <summary>
        /// This event fires when the LowValueFinal property is updated
        /// </summary>
        public event EventHandler LowValueFinalUpdated;

        /// <summary>
        /// This event fires when the HighValue property is updated
        /// </summary>
        public event EventHandler HighValueUpdated;

        /// <summary>
        /// This event fires when the HighValueFinal property is updated
        /// </summary>
        public event EventHandler HighValueFinalUpdated;

        /// <summary>
        /// Raises the System.Windows.FrameworkElement.SizeChanged event, using the specified information as part of the eventual event data.
        /// </summary>
        /// <param name="sizeInfo">Details of the old and new size involved in the change.</param>
        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            UpdateTrackPosition();
        }

        /// <summary>
        /// For internal use only. Updates the position of the track element
        /// </summary>
        private void UpdateTrackPosition()
        {
            if (Track == null) return;

            var totalRange = Maximum - Minimum;
            var lowerFromMinimum = LowValue - Minimum;
            var higherFromMaximum = Maximum - HighValue;
            var percentageFromMin = lowerFromMinimum/totalRange;
            var percentageFromMax = higherFromMaximum/totalRange;

            // TODO: Don't assume 8 pixel width (4) for the nubs... have a property instead
            if (!IsInverted)
            {
                if (Orientation == Orientation.Horizontal)
                {
                    _lowValueLeftMargin = Math.Max(ActualWidth*percentageFromMin, 0);
                    _highValueRightMargin = Math.Max(ActualWidth*percentageFromMax, 0);
                    Track.Margin = new Thickness(_lowValueLeftMargin - 4, 0, _highValueRightMargin - 4, 0);
                }
                else
                {
                    _lowValueBottomMargin = Math.Max(ActualHeight*percentageFromMin, 0);
                    _highValueTopMargin = Math.Max(ActualHeight*percentageFromMax, 0);
                    Track.Margin = new Thickness(0, _highValueTopMargin - 4, 0, _lowValueBottomMargin - 4);
                }
            }
            else
            {
                if (Orientation == Orientation.Horizontal)
                {
                    _lowValueLeftMargin = Math.Max(ActualWidth*percentageFromMax, 0);
                    _highValueRightMargin = Math.Max(ActualWidth*percentageFromMin, 0);
                    Track.Margin = new Thickness(_lowValueLeftMargin - 4, 0, _highValueRightMargin - 4, 0);
                }
                else
                {
                    _lowValueBottomMargin = Math.Max(ActualHeight*percentageFromMax, 0);
                    _highValueTopMargin = Math.Max(ActualHeight*percentageFromMin, 0);
                    Track.Margin = new Thickness(0, _highValueTopMargin - 4, 0, _lowValueBottomMargin - 4);
                }
            }
        }

        /// <summary>
        /// Refreshes all UI positions identified by their part names
        /// </summary>
        /// <param name="d">The d.</param>
        /// <param name="e">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void RefreshAll(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var slider = d as RangeSlider;
            if (slider == null) return;
            slider.UpdateTrackPosition();
        }

        /// <summary>
        /// Handles the <see cref="E:MouseDown" /> event.
        /// </summary>
        /// <param name="e">The <see cref="MouseButtonEventArgs"/> instance containing the event data.</param>
        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var position = Mouse.GetPosition(this);

                if (SelectedRange != null && SelectedRange.IsMouseDirectlyOver)
                {
                    _selectedDragInitialPosition = position;
                    _selectedDragInitialLowValue = LowValue;
                    _selectedDragInitialHighValue = HighValue;
                    e.Handled = true;
                    Mouse.Capture(this);
                    _suspendFinalHighValueUpdate = true;
                    _suspendFinalLowValueUpdate = true;
                    return;
                }

                if (Orientation == Orientation.Horizontal)
                {
                    if (Handle1 != null && Handle1.IsMouseDirectlyOver)
                    {
                        _lowDragInitialPosition = position;
                        _lowDragInitialValue = !IsInverted ? LowValue : HighValue;
                        e.Handled = true;
                        Mouse.Capture(this);
                        if (!IsInverted)
                            _suspendFinalLowValueUpdate = true;
                        else
                            _suspendFinalHighValueUpdate = true;
                        return;
                    }
                    if (Handle2 != null && Handle2.IsMouseDirectlyOver)
                    {
                        _highDragInitialPosition = position;
                        _highDragInitialValue = !IsInverted ? HighValue : LowValue;
                        e.Handled = true;
                        Mouse.Capture(this);
                        if (!IsInverted)
                            _suspendFinalHighValueUpdate = true;
                        else
                            _suspendFinalLowValueUpdate = true;
                        return;
                    }
                }
                else
                {
                    if (Handle1 != null && Handle1.IsMouseDirectlyOver)
                    {
                        _lowDragInitialPosition = position;
                        _lowDragInitialValue = !IsInverted ? LowValue : HighValue;
                        e.Handled = true;
                        Mouse.Capture(this);
                        if (!IsInverted)
                            _suspendFinalLowValueUpdate = true;
                        else
                            _suspendFinalHighValueUpdate = true;
                        return;
                    }
                    if (Handle2 != null && Handle2.IsMouseDirectlyOver)
                    {
                        _highDragInitialPosition = position;
                        _highDragInitialValue = !IsInverted ? HighValue : LowValue;
                        e.Handled = true;
                        Mouse.Capture(this);
                        if (!IsInverted)
                            _suspendFinalHighValueUpdate = true;
                        else
                            _suspendFinalLowValueUpdate = true;
                        return;
                    }
                }
            }

            base.OnMouseDown(e);
        }

        /// <summary>
        /// Handles the <see cref="E:MouseMove" /> event.
        /// </summary>
        /// <param name="e">The <see cref="MouseEventArgs"/> instance containing the event data.</param>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            var position = Mouse.GetPosition(this);

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (Orientation == Orientation.Horizontal)
                {
                    if (_selectedDragInitialPosition != new Point(0, 0))
                    {
                        // Dragging the entire range
                        var delta = _selectedDragInitialPosition.X - position.X;
                        var pixelRatio = (Maximum - Minimum)/ActualWidth;
                        var deltaValue = delta*pixelRatio;
                        if (IsInverted) deltaValue = deltaValue*-1;
                        if (_selectedDragInitialLowValue - deltaValue < Minimum) deltaValue = _selectedDragInitialLowValue - Minimum;
                        if (_selectedDragInitialHighValue - deltaValue > Maximum) deltaValue = _selectedDragInitialHighValue - Maximum;
                        LowValue = _selectedDragInitialLowValue - deltaValue;
                        HighValue = _selectedDragInitialHighValue - deltaValue;
                    }
                    if (_lowDragInitialPosition != new Point(0, 0))
                    {
                        // Dragging the low value
                        var delta = _lowDragInitialPosition.X - position.X;
                        var pixelRatio = (Maximum - Minimum)/ActualWidth;
                        var deltaValue = delta*pixelRatio;
                        if (!IsInverted)
                        {
                            var newValue = _lowDragInitialValue - deltaValue;
                            if (newValue < Minimum) newValue = Minimum;
                            if (newValue > HighValue) newValue = HighValue; // TODO: There should be a minimum range
                            LowValue = newValue;
                        }
                        else
                        {
                            var newValue = _lowDragInitialValue + deltaValue;
                            if (newValue > Maximum) newValue = Maximum;
                            if (newValue < LowValue) newValue = LowValue; // TODO: There should be a minimum range
                            HighValue = newValue;
                        }
                        e.Handled = true;
                        return;
                    }
                    if (_highDragInitialPosition != new Point(0, 0))
                    {
                        // Dragging the low value
                        var delta = _highDragInitialPosition.X - position.X;
                        var pixelRatio = (Maximum - Minimum)/ActualWidth;
                        var deltaValue = delta*pixelRatio;
                        if (!IsInverted)
                        {
                            var newValue = _highDragInitialValue - deltaValue;
                            if (newValue < LowValue) newValue = LowValue; // TODO: There should be a minimum range
                            if (newValue > Maximum) newValue = Maximum;
                            HighValue = newValue;
                        }
                        else
                        {
                            var newValue = _highDragInitialValue + deltaValue;
                            if (newValue > HighValue) newValue = HighValue; // TODO: There should be a minimum range
                            if (newValue < Minimum) newValue = Minimum;
                            LowValue = newValue;
                        }
                        e.Handled = true;
                        return;
                    }
                }
                else
                {
                    if (_selectedDragInitialPosition != new Point(0, 0))
                    {
                        // Dragging the entire range
                        var delta = _selectedDragInitialPosition.Y - position.Y;
                        var pixelRatio = (Maximum - Minimum) / ActualHeight;
                        var deltaValue = delta * pixelRatio;
                        if (!IsInverted) deltaValue = deltaValue * -1;
                        if (_selectedDragInitialLowValue - deltaValue < Minimum) deltaValue = _selectedDragInitialLowValue - Minimum;
                        if (_selectedDragInitialHighValue - deltaValue > Maximum) deltaValue = _selectedDragInitialHighValue - Maximum;
                        LowValue = _selectedDragInitialLowValue - deltaValue;
                        HighValue = _selectedDragInitialHighValue - deltaValue;
                    }
                    if (_lowDragInitialPosition != new Point(0, 0))
                    {
                        // Dragging the low value
                        var delta = _lowDragInitialPosition.Y - position.Y;
                        var pixelRatio = (Maximum - Minimum)/ActualHeight;
                        var deltaValue = delta*pixelRatio;
                        if (!IsInverted)
                        {
                            var newValue = _lowDragInitialValue + deltaValue;
                            if (newValue < Minimum) newValue = Minimum;
                            if (newValue > HighValue) newValue = HighValue; // TODO: There should be a minimum range
                            LowValue = newValue;
                        }
                        else
                        {
                            var newValue = _lowDragInitialValue - deltaValue;
                            if (newValue > Maximum) newValue = Maximum;
                            if (newValue < LowValue) newValue = LowValue; // TODO: There should be a minimum range
                            HighValue = newValue;
                        }
                        e.Handled = true;
                        return;
                    }
                    if (_highDragInitialPosition != new Point(0, 0))
                    {
                        // Dragging the low value
                        var delta = _highDragInitialPosition.Y - position.Y;
                        var pixelRatio = (Maximum - Minimum)/ActualHeight;
                        var deltaValue = delta*pixelRatio;
                        if (!IsInverted)
                        {
                            var newValue = _highDragInitialValue + deltaValue;
                            if (newValue < LowValue) newValue = LowValue; // TODO: There should be a minimum range
                            if (newValue > Maximum) newValue = Maximum;
                            HighValue = newValue;
                        }
                        else
                        {
                            var newValue = _highDragInitialValue - deltaValue;
                            if (newValue > HighValue) newValue = HighValue; // TODO: There should be a minimum range
                            if (newValue < Minimum) newValue = Minimum;
                            LowValue = newValue;
                        }
                        e.Handled = true;
                        return;
                    }
                }
            }

            base.OnMouseMove(e);
        }

        /// <summary>
        /// Handles the <see cref="E:MouseLeftButtonUp" /> event.
        /// </summary>
        /// <param name="e">The <see cref="MouseButtonEventArgs"/> instance containing the event data.</param>
        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            if (_selectedDragInitialPosition != new Point(0, 0))
            {
                _selectedDragInitialPosition = new Point(0, 0);
                UpdateTrackPosition();
                if (Mouse.Captured == this) Mouse.Capture(null);
                _suspendFinalLowValueUpdate = false;
                LowValueFinal = LowValue;
                _suspendFinalHighValueUpdate = false;
                HighValueFinal = HighValue;
                e.Handled = true;
                return;
            }
            if (_lowDragInitialPosition != new Point(0, 0))
            {
                _lowDragInitialPosition = new Point(0, 0);
                UpdateTrackPosition();
                if (Mouse.Captured == this) Mouse.Capture(null);
                if (!IsInverted)
                {
                    _suspendFinalLowValueUpdate = false;
                    LowValueFinal = LowValue;
                }
                else
                {
                    _suspendFinalHighValueUpdate = false;
                    HighValueFinal = HighValue;
                }
                e.Handled = true;
                return;
            }
            if (_highDragInitialPosition != new Point(0, 0))
            {
                _highDragInitialPosition = new Point(0, 0);
                UpdateTrackPosition();
                if (Mouse.Captured == this) Mouse.Capture(null);
                if (!IsInverted)
                {
                    _suspendFinalHighValueUpdate = false;
                    HighValueFinal = HighValue;
                }
                else
                {
                    _suspendFinalLowValueUpdate = false;
                    LowValueFinal = LowValue;
                }
                e.Handled = true;
                return;
            }

            base.OnMouseLeftButtonUp(e);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CODE.Framework.Wpf.Utilities;

namespace CODE.Framework.Wpf.Layout
{
    /// <summary>
    /// Provides automatic layout for a flowing data entry form
    /// </summary>
    public class FlowForm : Panel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FlowForm"/> class.
        /// </summary>
        public FlowForm()
        {
            VerticalAlignment = VerticalAlignment.Stretch;
            HorizontalAlignment = HorizontalAlignment.Stretch;
            ClipToBounds = true;
        }

        /// <summary>
        /// List of current rows of controls
        /// </summary>
        private List<ControlRow> _currentRows;

        /// <summary>Left padding/margin that separates the label from the edit control</summary>
        /// <remarks>Only applicable when the label is positioned to the left of the edit control</remarks>
        public double EditControlLeftSpacing
        {
            get { return (double)GetValue(EditControlLeftSpacingProperty); }
            set { SetValue(EditControlLeftSpacingProperty, value); }
        }
        /// <summary>Left padding/margin that separates the label from the edit control</summary>
        /// <remarks>Only applicable when the label is positioned to the left of the edit control</remarks>
        public static readonly DependencyProperty EditControlLeftSpacingProperty = DependencyProperty.Register("EditControlLeftSpacing", typeof(double), typeof(FlowForm), new UIPropertyMetadata(20d, (s, e) => InvalidateAllVisuals(s)));

        /// <summary>Minimum left padding/margin that separates the label from the edit control (only applies for elastic layouts)</summary>
        /// <remarks>Only applicable when the label is positioned to the left of the edit control</remarks>
        public double MinEditControlLeftSpacing
        {
            get { return (double)GetValue(MinEditControlLeftSpacingProperty); }
            set { SetValue(MinEditControlLeftSpacingProperty, value); }
        }
        /// <summary>Minimum left padding/margin that separates the label from the edit control (only applies for elastic layouts)</summary>
        /// <remarks>Only applicable when the label is positioned to the left of the edit control</remarks>
        public static readonly DependencyProperty MinEditControlLeftSpacingProperty = DependencyProperty.Register("MinEditControlLeftSpacing", typeof(double), typeof(FlowForm), new UIPropertyMetadata(2d, (s, e) => InvalidateAllVisuals(s)));

        /// <summary>Top padding/margin that separates the label from the edit control</summary>
        /// <remarks>Only applicable when the label is positioned to the top of the edit control</remarks>
        public double EditControlTopSpacing
        {
            get { return (double)GetValue(EditControlTopSpacingProperty); }
            set { SetValue(EditControlTopSpacingProperty, value); }
        }
        /// <summary>Top padding/margin that separates the label from the edit control</summary>
        /// <remarks>Only applicable when the label is positioned to the top of the edit control</remarks>
        public static readonly DependencyProperty EditControlTopSpacingProperty = DependencyProperty.Register("EditControlTopSpacing", typeof(double), typeof(FlowForm), new UIPropertyMetadata(2d, (s, e) => InvalidateAllVisuals(s)));

        /// <summary>Left padding/margin that separates the edit control from the next label</summary>
        public double LabelControlLeftSpacing
        {
            get { return (double)GetValue(LabelControlLeftSpacingProperty); }
            set { SetValue(LabelControlLeftSpacingProperty, value); }
        }
        /// <summary>Left padding/margin that separates the edit control from the next label</summary>
        public static readonly DependencyProperty LabelControlLeftSpacingProperty = DependencyProperty.Register("LabelControlLeftSpacing", typeof(double), typeof(FlowForm), new UIPropertyMetadata(20d, (s, e) => InvalidateAllVisuals(s)));

        /// <summary>Minimum left padding/margin that separates the edit control from the next label (only applies for elastic layouts)</summary>
        public double MinLabelControlLeftSpacing
        {
            get { return (double)GetValue(MinLabelControlLeftSpacingProperty); }
            set { SetValue(MinLabelControlLeftSpacingProperty, value); }
        }
        /// <summary>Minimum left padding/margin that separates the edit control from the next label (only applies for elastic layouts)</summary>
        public static readonly DependencyProperty MinLabelControlLeftSpacingProperty = DependencyProperty.Register("MinLabelControlLeftSpacing", typeof(double), typeof(FlowForm), new UIPropertyMetadata(2d, (s, e) => InvalidateAllVisuals(s)));

        /// <summary>Object used to render the group headers</summary>
        public AutoLayoutHeaderRenderer GroupHeaderRenderer
        {
            get { return (AutoLayoutHeaderRenderer)GetValue(GroupHeaderRendererProperty); }
            set { SetValue(GroupHeaderRendererProperty, value); }
        }
        /// <summary>Object used to render the group headers</summary>
        public static readonly DependencyProperty GroupHeaderRendererProperty = DependencyProperty.Register("GroupHeaderRenderer", typeof(AutoLayoutHeaderRenderer), typeof(FlowForm), new UIPropertyMetadata(null));

        /// <summary>Vertical additional spacing before new groups</summary>
        public double GroupHeaderTopSpacing
        {
            get { return (double)GetValue(GroupHeaderTopSpacingProperty); }
            set { SetValue(GroupHeaderTopSpacingProperty, value); }
        }
        /// <summary>Vertical additional spacing before new groups</summary>
        public static readonly DependencyProperty GroupHeaderTopSpacingProperty = DependencyProperty.Register("GroupHeaderTopSpacing", typeof(double), typeof(FlowForm), new UIPropertyMetadata(15d, (s, e) => InvalidateAllVisuals(s)));

        /// <summary>Vertical additional spacing before new groups</summary>
        public double GroupHeaderBottomSpacing
        {
            get { return (double)GetValue(GroupHeaderBottomSpacingProperty); }
            set { SetValue(GroupHeaderBottomSpacingProperty, value); }
        }
        /// <summary>Vertical additional spacing before new groups</summary>
        public static readonly DependencyProperty GroupHeaderBottomSpacingProperty = DependencyProperty.Register("GroupHeaderBottomSpacing", typeof(double), typeof(FlowForm), new UIPropertyMetadata(7d, (s, e) => InvalidateAllVisuals(s)));

        /// <summary>Minimum vertical additional spacing after new group headers (only used if elastic layout is enabled)</summary>
        public double MinGroupHeaderBottomSpacing
        {
            get { return (double)GetValue(MinGroupHeaderBottomSpacingProperty); }
            set { SetValue(MinGroupHeaderBottomSpacingProperty, value); }
        }
        /// <summary>Minimum vertical additional spacing after new group headers (only used if elastic layout is enabled)</summary>
        public static readonly DependencyProperty MinGroupHeaderBottomSpacingProperty = DependencyProperty.Register("MinGroupHeaderBottomSpacing", typeof(double), typeof(FlowForm), new UIPropertyMetadata(3d, (s, e) => InvalidateAllVisuals(s)));

        /// <summary>Minimum vertical additional spacing before new group headers (only used if elastic layout is enabled)</summary>
        public double MinGroupHeaderTopSpacing
        {
            get { return (double)GetValue(MinGroupHeaderTopSpacingProperty); }
            set { SetValue(MinGroupHeaderTopSpacingProperty, value); }
        }
        /// <summary>Minimum vertical additional spacing before new group headers (only used if elastic layout is enabled)</summary>
        public static readonly DependencyProperty MinGroupHeaderTopSpacingProperty = DependencyProperty.Register("MinGroupHeaderTopSpacing", typeof(double), typeof(FlowForm), new UIPropertyMetadata(5d, (s, e) => InvalidateAllVisuals(s)));

        /// <summary>Font family used to render group headers</summary>
        public FontFamily GroupHeaderFontFamily
        {
            get { return (FontFamily)GetValue(GroupHeaderFontFamilyProperty); }
            set { SetValue(GroupHeaderFontFamilyProperty, value); }
        }
        /// <summary>Font family used to render group headers</summary>
        public static readonly DependencyProperty GroupHeaderFontFamilyProperty = DependencyProperty.Register("GroupHeaderFontFamily", typeof(FontFamily), typeof(FlowForm), new UIPropertyMetadata(new FontFamily("Segoe UI"), (s, e) => InvalidateAllVisuals(s)));

        /// <summary>Font style used to render group headers</summary>
        public FontStyle GroupHeaderFontStyle
        {
            get { return (FontStyle)GetValue(GroupHeaderFontStyleProperty); }
            set { SetValue(GroupHeaderFontStyleProperty, value); }
        }
        /// <summary>Font style used to render group headers</summary>
        public static readonly DependencyProperty GroupHeaderFontStyleProperty = DependencyProperty.Register("GroupHeaderFontStyle", typeof(FontStyle), typeof(FlowForm), new UIPropertyMetadata(FontStyles.Normal, (s, e) => InvalidateAllVisuals(s)));

        /// <summary>Font weight used to render group headers</summary>
        public FontWeight GroupHeaderFontWeight
        {
            get { return (FontWeight)GetValue(GroupHeaderFontWeightProperty); }
            set { SetValue(GroupHeaderFontWeightProperty, value); }
        }
        /// <summary>Font weight used to render group headers</summary>
        public static readonly DependencyProperty GroupHeaderFontWeightProperty = DependencyProperty.Register("GroupHeaderFontWeight", typeof(FontWeight), typeof(FlowForm), new UIPropertyMetadata(FontWeights.Bold, (s, e) => InvalidateAllVisuals(s)));

        /// <summary>Font size used to render group headers</summary>
        public double GroupHeaderFontSize
        {
            get { return (double)GetValue(GroupHeaderFontSizeProperty); }
            set { SetValue(GroupHeaderFontSizeProperty, value); }
        }
        /// <summary>Font size used to render group headers</summary>
        public static readonly DependencyProperty GroupHeaderFontSizeProperty = DependencyProperty.Register("GroupHeaderFontSize", typeof(double), typeof(FlowForm), new UIPropertyMetadata(12d, (s, e) => InvalidateAllVisuals(s)));

        /// <summary>Foreground brush used to render group headers</summary>
        public Brush GroupHeaderForegroundBrush
        {
            get { return (Brush)GetValue(GroupHeaderForegroundBrushProperty); }
            set { SetValue(GroupHeaderForegroundBrushProperty, value); }
        }
        /// <summary>Foreground brush used to render group headers</summary>
        public static readonly DependencyProperty GroupHeaderForegroundBrushProperty = DependencyProperty.Register("GroupHeaderForegroundBrush", typeof(Brush), typeof(FlowForm), new UIPropertyMetadata(Brushes.Black, (s, e) => InvalidateAllVisuals(s)));

        /// <summary>Font family used to render labels</summary>
        public FontFamily LabelFontFamily
        {
            get { return (FontFamily)GetValue(LabelFontFamilyProperty); }
            set { SetValue(LabelFontFamilyProperty, value); }
        }
        /// <summary>Font family used to render labels</summary>
        public static readonly DependencyProperty LabelFontFamilyProperty = DependencyProperty.Register("LabelFontFamily", typeof(FontFamily), typeof(FlowForm), new UIPropertyMetadata(new FontFamily("Segoe UI"), (s, e) => InvalidateAllVisuals(s)));

        /// <summary>Font style used to render labels</summary>
        public FontStyle LabelFontStyle
        {
            get { return (FontStyle)GetValue(LabelFontStyleProperty); }
            set { SetValue(LabelFontStyleProperty, value); }
        }
        /// <summary>Font style used to render labels</summary>
        public static readonly DependencyProperty LabelFontStyleProperty = DependencyProperty.Register("LabelFontStyle", typeof(FontStyle), typeof(FlowForm), new UIPropertyMetadata(FontStyles.Normal, (s, e) => InvalidateAllVisuals(s)));

        /// <summary>Font weight used to render labels</summary>
        public FontWeight LabelFontWeight
        {
            get { return (FontWeight)GetValue(LabelFontWeightProperty); }
            set { SetValue(LabelFontWeightProperty, value); }
        }
        /// <summary>Font weight used to render labels</summary>
        public static readonly DependencyProperty LabelFontWeightProperty = DependencyProperty.Register("LabelFontWeight", typeof(FontWeight), typeof(FlowForm), new UIPropertyMetadata(FontWeights.Normal, (s, e) => InvalidateAllVisuals(s)));

        /// <summary>Font size used to render labels</summary>
        public double LabelFontSize
        {
            get { return (double)GetValue(LabelFontSizeProperty); }
            set { SetValue(LabelFontSizeProperty, value); }
        }
        /// <summary>Font size used to render labels</summary>
        public static readonly DependencyProperty LabelFontSizeProperty = DependencyProperty.Register("LabelFontSize", typeof(double), typeof(FlowForm), new UIPropertyMetadata(12d, (s, e) => InvalidateAllVisuals(s)));

        /// <summary>Text alignment used to render labels</summary>
        public TextAlignment LabelTextAlignment
        {
            get { return (TextAlignment)GetValue(LabelTextAlignmentProperty); }
            set { SetValue(LabelTextAlignmentProperty, value); }
        }
        /// <summary>Text alignment used to render labels</summary>
        public static readonly DependencyProperty LabelTextAlignmentProperty = DependencyProperty.Register("LabelTextAlignment", typeof(TextAlignment), typeof(FlowForm), new UIPropertyMetadata(TextAlignment.Left, (s, e) => InvalidateAllVisuals(s)));

        /// <summary>Foreground brush used to render labels</summary>
        public Brush LabelForegroundBrush
        {
            get { return (Brush)GetValue(LabelForegroundBrushProperty); }
            set { SetValue(LabelForegroundBrushProperty, value); }
        }
        /// <summary>Foreground brush used to render group headers</summary>
        public static readonly DependencyProperty LabelForegroundBrushProperty = DependencyProperty.Register("LabelForegroundBrush", typeof(Brush), typeof(FlowForm), new UIPropertyMetadata(Brushes.Black, (s, e) => InvalidateAllVisuals(s)));

        /// <summary>Number of pixels the label is offset down when the layout uses labels to the left of the edit control</summary>
        public double VerticalLabelControlOffset
        {
            get { return (double)GetValue(VerticalLabelControlOffsetProperty); }
            set { SetValue(VerticalLabelControlOffsetProperty, value); }
        }
        /// <summary>Number of pixels the label is offset down when the layout uses labels to the left of the edit control</summary>
        public static readonly DependencyProperty VerticalLabelControlOffsetProperty = DependencyProperty.Register("VerticalLabelControlOffset", typeof(double), typeof(FlowForm), new UIPropertyMetadata(5d, (s, e) => InvalidateAllVisuals(s)));

        /// <summary>Vertical spacing in between individual elements</summary>
        public double VerticalSpacing
        {
            get { return (double)GetValue(VerticalSpacingProperty); }
            set { SetValue(VerticalSpacingProperty, value); }
        }
        /// <summary>Vertical spacing in between individual elements</summary>
        public static readonly DependencyProperty VerticalSpacingProperty = DependencyProperty.Register("VerticalSpacing", typeof(double), typeof(FlowForm), new UIPropertyMetadata(5d, (s, e) => InvalidateAllVisuals(s)));

        /// <summary>Minimum vertical spacing in between individual elements (only used if elastic layout is enabled)</summary>
        public double MinVerticalSpacing
        {
            get { return (double)GetValue(MinVerticalSpacingProperty); }
            set { SetValue(MinVerticalSpacingProperty, value); }
        }
        /// <summary>Minimum vertical spacing in between individual elements (only used if elastic layout is enabled)</summary>
        public static readonly DependencyProperty MinVerticalSpacingProperty = DependencyProperty.Register("MinVerticalSpacing", typeof(double), typeof(FlowForm), new UIPropertyMetadata(1d, (s, e) => InvalidateAllVisuals(s)));

        /// <summary>Vertical additional spacing before new groups</summary>
        public double GroupSpacing
        {
            get { return (double)GetValue(GroupSpacingProperty); }
            set { SetValue(GroupSpacingProperty, value); }
        }
        /// <summary>Vertical additional spacing before new groups</summary>
        public static readonly DependencyProperty GroupSpacingProperty = DependencyProperty.Register("GroupSpacing", typeof(double), typeof(FlowForm), new UIPropertyMetadata(15d, (s, e) => InvalidateAllVisuals(s)));

        /// <summary>Minimum vertical additional spacing before new groups (only used if elastic layout is enabled)</summary>
        public double MinGroupSpacing
        {
            get { return (double)GetValue(MinGroupSpacingProperty); }
            set { SetValue(MinGroupSpacingProperty, value); }
        }
        /// <summary>Minimum vertical additional spacing before new groups (only used if elastic layout is enabled)</summary>
        public static readonly DependencyProperty MinGroupSpacingProperty = DependencyProperty.Register("MinGroupSpacing", typeof(double), typeof(FlowForm), new UIPropertyMetadata(4d, (s, e) => InvalidateAllVisuals(s)));

        /// <summary>Indicates whether or not checkboxes shoudl be handled special (typically kept together as groups)</summary>
        public bool SpecialCheckBoxBehaviorActive
        {
            get { return (bool)GetValue(SpecialCheckBoxBehaviorActiveProperty); }
            set { SetValue(SpecialCheckBoxBehaviorActiveProperty, value); }
        }
        /// <summary>Indicates whether or not checkboxes shoudl be handled special (typically kept together as groups)</summary>
        public static readonly DependencyProperty SpecialCheckBoxBehaviorActiveProperty = DependencyProperty.Register("SpecialCheckBoxBehaviorActive", typeof(bool), typeof(FlowForm), new PropertyMetadata(true, (s, e) => InvalidateAllVisuals(s)));

        /// <summary>Indicates whether or not radio buttons shoudl be handled special (typically kept together as groups)</summary>
        public bool SpecialRadioButtonBehaviorActive
        {
            get { return (bool)GetValue(SpecialRadioButtonActiveProperty); }
            set { SetValue(SpecialRadioButtonActiveProperty, value); }
        }
        /// <summary>Indicates whether or not checkboxes shoudl be handled special (typically kept together as groups)</summary>
        public static readonly DependencyProperty SpecialRadioButtonActiveProperty = DependencyProperty.Register("SpecialRadioButtonActive", typeof(bool), typeof(FlowForm), new PropertyMetadata(true, (s, e) => InvalidateAllVisuals(s)));

        /// <summary>
        /// Minimum Width to be used for edit controls
        /// </summary>
        /// <value>The minimum width of the edit control.</value>
        public double EditControlMinimumWidth
        {
            get { return (double)GetValue(EditControlMinimumWidthProperty); }
            set { SetValue(EditControlMinimumWidthProperty, value); }
        }
        /// <summary>
        /// Minimum Width to be used for edit controls
        /// </summary>
        /// <value>The minimum width of the edit control.</value>
        public static readonly DependencyProperty EditControlMinimumWidthProperty = DependencyProperty.Register("EditControlMinimumWidth", typeof(double), typeof(FlowForm), new PropertyMetadata(-1d, (s, e) => InvalidateAllVisuals(s)));

        /// <summary>Invalidates everything in the UI and forces a refresh</summary>
        /// <param name="source">Reference to an instance of the form itself</param>
        private static void InvalidateAllVisuals(DependencyObject source)
        {
            var form = source as FlowForm;
            if (form == null) return;

            form.InvalidateArrange();
            form.InvalidateMeasure();
            form.InvalidateVisual();
        }

        /// <summary>
        /// When overridden in a derived class, measures the size in layout required for child elements and determines a size for the <see cref="T:System.Windows.FrameworkElement" />-derived class.
        /// </summary>
        /// <param name="availableSize">The available size that this element can give to child elements. Infinity can be specified as a value to indicate that the element will size to whatever content is available.</param>
        /// <returns>The size that this element determines it needs during layout, based on its calculations of child element sizes.</returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            base.MeasureOverride(availableSize);

            InvalidateVisual();

            var resultingHeight = 0d;
            var resultingWidth = 0d;

            // Before we do anything else, we allow each control to measure itself to the size they would ideally like to be
            var measureWidth = double.IsNaN(availableSize.Width) ? 10000d : availableSize.Width;
            var veryLarge = GeometryHelper.NewSize(measureWidth, 100000d); // Giving the control a very large size so we see how much it really wants to use
            foreach (var child in Children.OfType<UIElement>().Where(e => e.Visibility != Visibility.Collapsed))
                child.Measure(veryLarge);

            // Arranging all controls into flowing rows (at this point assuming infinite horizontal space)
            _currentRows = new List<ControlRow>();
            var currentRow = AddNewRow(_currentRows);

            foreach (var element in Children.OfType<FrameworkElement>().Where(e => e.Visibility != Visibility.Collapsed))
            {
                var lineBreak = SimpleView.GetLineBreak(element);
                if (lineBreak && currentRow.Elements.Count > 0) currentRow = AddNewRow(_currentRows);

                var spanWidth = SimpleView.GetSpanFullWidth(element);
                if (spanWidth)
                {
                    if (currentRow.Elements.Count > 0) currentRow = AddNewRow(_currentRows);
                    currentRow.Elements.Add(new ControlRowElement {Element = element, IsFullSpanControl = true});
                    continue;
                }

                var label = SimpleView.GetLabel(element);
                var isStandAloneEditControl = SimpleView.GetIsStandAloneEditControl(element);
                if (label != null || isStandAloneEditControl)
                {
                    var individualFont = SimpleView.GetLabelFontFamily(element);
                    var font = individualFont ?? LabelFontFamily;

                    var individualStyle = SimpleView.GetLabelFontStyle(element);
                    var style = individualStyle == FontStyles.Normal ? LabelFontStyle : individualStyle;

                    var individualWeight = SimpleView.GetLabelFontWeight(element);
                    var weight = individualWeight == FontWeights.Normal ? LabelFontWeight : individualWeight;

                    var individualSize = SimpleView.GetLabelFontSize(element);
                    var size = individualSize > 0d ? individualSize : LabelFontSize;

                    var individualBrush = SimpleView.GetLabelForegroundBrush(element);
                    var brush = individualBrush ?? LabelForegroundBrush;

                    currentRow.Elements.Add(new ControlRowElement
                        {
                            Label = label ?? string.Empty,
                            IsAutoGeneratedLabel = true,
                            IsConsideredLabelElement = true,
                            HadOriginalLineBreak = currentRow.Elements.Count == 0,
                            DesiredSize = CalculateLabelSize(label ?? string.Empty, element),
                            FontFamily = font,
                            FontStyle = style,
                            FontWeight = weight,
                            FontSize = size,
                            ForegroundBrush = brush
                        });
                }

                var isLabelElement = false;
                if (currentRow.Elements.Count%2 == 0)
                    // Could be a label
                    if (element is Label || element is TextBlock)
                        isLabelElement = true;

                currentRow.Elements.Add(new ControlRowElement
                    {
                        Element = element, 
                        IsAutoGeneratedLabel = false, 
                        IsConsideredLabelElement = isLabelElement, 
                        HadOriginalLineBreak = currentRow.Elements.Count == 0,
                        DesiredSize = isLabelElement ? element.DesiredSize : GetControlSize(element.DesiredSize)
                    });
            }

            // See which items need to be wrapped to the next row (since we do not really have infinite horizontal space)
            var rowCounter = 0;
            while (true)
            {
                if (rowCounter >= _currentRows.Count) break;

                var widestLabelWidth = GetWidestFirstLabel(_currentRows);
                var row = _currentRows[rowCounter];
                var rowWidth = GetTotalRowWidth(row, widestLabelWidth, availableSize.Width);

                if (rowWidth > availableSize.Width)
                {
                    // This row is too wide... we need to do something about it
                    var fittingItemCount = GetNumberOfElementsThatFitInRow(row, widestLabelWidth, availableSize.Width);

                    if (fittingItemCount < row.Elements.Count)
                    {
                        // We insert a new row into the current flow
                        var insertedRow = new ControlRow();
                        var elementsToMove = row.Elements.Count - fittingItemCount;
                        for (var moveCounter = 0; moveCounter < elementsToMove; moveCounter++)
                        {
                            var moveElement = row.Elements[fittingItemCount]; // it will always be the next item since we are removing them as we go
                            row.Elements.RemoveAt(fittingItemCount);
                            if (moveCounter == 0 || !moveElement.IsFakeLabel)
                                insertedRow.Elements.Add(moveElement);
                        }
                        if (row.Elements.Count > 1 && row.Elements[row.Elements.Count - 1].IsConsideredLabelElement)
                        {
                            var elementToMove = row.Elements[row.Elements.Count - 1];
                            row.Elements.RemoveAt(row.Elements.Count - 1);
                            insertedRow.Elements.Insert(0, elementToMove);
                        }
                        _currentRows.Insert(rowCounter + 1, insertedRow);

                        rowCounter = 0; // We need to start over
                        continue;
                    }
                }

                rowCounter++;
            }

            // Ready to calculate the actual control positions
            var finalLabelWidth = GetWidestFirstLabel(_currentRows);
            var currentY = VerticalSpacing;
            foreach (var row in _currentRows)
            {
                if (row.Elements.Count == 1 && row.Elements[0].IsFullSpanControl)
                {
                    // TODO: Maybe we want some special margins for this case
                    row.Elements[0].DesiredPosition = new Point(0d, currentY);
                    row.Elements[0].DesiredSize = GeometryHelper.NewSize(availableSize.Width, row.Elements[0].Element.DesiredSize.Height);
                    currentY += row.Elements[0].Element.DesiredSize.Height + VerticalSpacing;
                    continue;
                }

                var currentX = 0d;
                var elementCounter = 0;
                var tallestElement = 0d;
                var mustUseVerticalLabelOffsetForLabelsInRow = false;
                foreach (var element in row.Elements)
                {
                    if (element.IsConsideredLabelElement)
                    {
                        currentX += LabelControlLeftSpacing;
                        if (elementCounter == 0) // We can still decide whether labels must be offset or not
                            if (row.Elements.Count > 1)
                            {
                                var nextElement = row.Elements[1];
                                if (nextElement != null)
                                    if (nextElement.DesiredSize.Height >= element.DesiredSize.Height + VerticalLabelControlOffset)
                                        mustUseVerticalLabelOffsetForLabelsInRow = true; // If the next label is taller than the current label, we add an offset.
                            }
                        var controlY = mustUseVerticalLabelOffsetForLabelsInRow ? currentY + VerticalLabelControlOffset : currentY;
                        element.DesiredPosition = new Point(currentX, controlY);
                        element.DesiredSize = GeometryHelper.NewSize(element.DesiredSize.Width, element.DesiredSize.Height);
                        if (elementCounter == 0)
                            element.MaxLabelWidth = finalLabelWidth;
                    }
                    else
                    {
                        currentX += EditControlLeftSpacing;
                        element.DesiredPosition = new Point(currentX, currentY);
                        element.DesiredSize = GeometryHelper.NewSize(element.DesiredSize.Width, element.DesiredSize.Height);
                    }

                    currentX += elementCounter == 0 ? finalLabelWidth : element.DesiredSize.Width;
                    tallestElement = Math.Max(element.DesiredSize.Height, tallestElement);

                    elementCounter++;
                }

                currentY += tallestElement + VerticalSpacing;

                resultingWidth = Math.Max(resultingWidth, currentX);
                resultingHeight = currentY;
            }

            return GeometryHelper.NewSize(resultingWidth, resultingHeight);
        }

        private Size GetControlSize(Size desiredSize)
        {
            if (EditControlMinimumWidth < 0d) return desiredSize;
            return GeometryHelper.NewSize(Math.Max(desiredSize.Width, EditControlMinimumWidth), desiredSize.Height);
        }

        /// <summary>
        /// Calculates the size required to show the specified text label for the element at hand
        /// </summary>
        /// <param name="label">Label (text) to measure</param>
        /// <param name="element">UI Element that label goes with (used to retrieve other settings, such as font info)</param>
        /// <returns>Size required</returns>
        protected virtual Size CalculateLabelSize(string label, DependencyObject element)
        {
            var individualFont = SimpleView.GetLabelFontFamily(element);
            var font = individualFont ?? LabelFontFamily;

            var individualStyle = SimpleView.GetLabelFontStyle(element);
            var style = individualStyle != FontStyles.Normal ? LabelFontStyle : individualStyle;

            var individualWeight = SimpleView.GetLabelFontWeight(element);
            var weight = individualWeight != FontWeights.Normal ? LabelFontWeight : individualWeight;

            var individualSize = SimpleView.GetLabelFontSize(element);
            var size = individualSize > 0d ? individualSize : LabelFontSize;

            var individualBrush = SimpleView.GetLabelForegroundBrush(element);
            var brush = individualBrush ?? LabelForegroundBrush;
            
            var typeface = new Typeface(font, style, weight, FontStretches.Normal);
            var ft = new FormattedText(label, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, typeface, size, brush) {Trimming = TextTrimming.WordEllipsis, TextAlignment = LabelTextAlignment};
            return GeometryHelper.NewSize(ft.Width, ft.Height);
        }

        /// <summary>
        /// Adds a new row of controls to the list of existing rows
        /// </summary>
        /// <param name="rows">Existing rows</param>
        /// <returns>Added row</returns>
        private static ControlRow AddNewRow(IList<ControlRow> rows)
        {
            rows.Add(new ControlRow());
            return rows[rows.Count - 1];
        }

        /// <summary>
        /// Calculates the width of the complete row
        /// </summary>
        /// <param name="row">The row.</param>
        /// <param name="labelWidth">Width of the first label.</param>
        /// <param name="availableWidth">Total available width.</param>
        /// <returns>Total width of the row including all spacing</returns>
        protected virtual double GetTotalRowWidth(ControlRow row, double labelWidth, double availableWidth)
        {
            if (row.Elements.Count == 1 && row.Elements[0].IsFullSpanControl) return availableWidth;

            var totalWidth = labelWidth + LabelControlLeftSpacing; // TODO: Should we support shrinking here?

            for (var counter = 1; counter < row.Elements.Count; counter++) // Calculating all but the first one (which is already handled with the labelWidth parameter)
            {
                var element = row.Elements[counter];
                totalWidth += element.IsConsideredLabelElement ? LabelControlLeftSpacing : EditControlLeftSpacing;
                totalWidth += element.DesiredSize.Width;
                //if (counter < row.Elements.Count - 1)
                //    totalWidth += element.IsConsideredLabelElement ? EditControlLeftSpacing : LabelControlLeftSpacing;
            }

            return totalWidth;
        }

        /// <summary>Returns how many of the specified row's elements fit in the available horizontal space</summary>
        /// <param name="row">The row.</param>
        /// <param name="labelWidth">Width of the label.</param>
        /// <param name="availableWidth">Total available width.</param>
        /// <returns>Number of elements that fit in the row.</returns>
        protected virtual int GetNumberOfElementsThatFitInRow(ControlRow row, double labelWidth, double availableWidth)
        {
            if (row.Elements.Count == 1 && row.Elements[0].IsFullSpanControl) return 1;

            var fitCount = 1;
            var totalWidth = labelWidth + LabelControlLeftSpacing; // TODO: Should we support shrinking here?

            for (var counter = 1; counter < row.Elements.Count; counter++) // Calculating all but the first two (which we will always leave on the first row, even if that forces scrolling)
            {
                var element = row.Elements[counter];
                totalWidth += element.IsConsideredLabelElement ? LabelControlLeftSpacing : EditControlLeftSpacing;
                totalWidth += element.DesiredSize.Width;
                if (totalWidth < availableWidth) fitCount++; // This control still fits on the row
                else
                {
                    // This one doesn't fit into this row anymore. 

                    // Maybe checkboxes are special
                    if (row.Elements[counter].Element is CheckBox && SpecialCheckBoxBehaviorActive)
                    {
                        // If this isn't the first special captioned checkbox in the group, we need to break the whole group
                        var labelIndex = counter - 1;
                        while (labelIndex > 0)
                        {
                            if (row.Elements[labelIndex].IsConsideredLabelElement) // This would make for a label that is not the first element in the row, and thus we break the whole group of checkboxes over to the next row
                            {
                                fitCount = labelIndex;
                                if (fitCount < 2) fitCount = 2; // Even if 2 controls don't fit, we leave them on that row anyway. Worst case, things have to scroll
                                return fitCount;
                            }
                            labelIndex--;
                        }

                        // Looks like we have not found an inline label yet, so we just need to break however many checkboxes we have
                        if (counter > 1)
                            // We insert another fake label if the control to the left is a checkbox and not a label
                            if (!row.Elements[counter - 1].IsConsideredLabelElement && row.Elements[counter - 1].Element is CheckBox)
                            {
                                InsertFakeLabel(row.Elements, row.Elements[counter].Element, fitCount);
                                if (fitCount < 2) fitCount = 2; // Even if 2 controls don't fit, we leave them on that row anyway. Worst case, things have to scroll
                                return fitCount;
                            }
                        if (fitCount == 1) // We can really only fit one control space-wise, but we still need to keep a label and control on the same line
                        {
                            if (row.Elements.Count > 2 && !row.Elements[2].IsConsideredLabelElement) // If there are any elements after the second one, we need to add another fake label
                                InsertFakeLabel(row.Elements, row.Elements[counter].Element, 2);
                            return 2;
                        }
                    }

                    // Maybe radio buttons are special
                    if (row.Elements[counter].Element is RadioButton && SpecialRadioButtonBehaviorActive)
                    {
                        // If this isn't the first special captioned radio button in the group, we need to break the whole group
                        var labelIndex = counter - 1;
                        while (labelIndex > 0)
                        {
                            if (row.Elements[labelIndex].IsConsideredLabelElement) // This would make for a label that is not the first element in the row, and thus we break the whole group of radio buttons over to the next row
                            {
                                fitCount = labelIndex;
                                if (fitCount < 2) fitCount = 2; // Even if 2 controls don't fit, we leave them on that row anyway. Worst case, things have to scroll
                                return fitCount;
                            }
                            labelIndex--;
                        }

                        // Looks like we have not found an inline label yet, so we just need to break however many radio buttons we have
                        if (counter > 1)
                            // We insert another fake label if the control to the left is a radio button and not a label
                            if (!row.Elements[counter - 1].IsConsideredLabelElement && row.Elements[counter - 1].Element is RadioButton)
                            {
                                InsertFakeLabel(row.Elements, row.Elements[counter].Element, fitCount);
                                if (fitCount < 2) fitCount = 2; // Even if 2 controls don't fit, we leave them on that row anyway. Worst case, things have to scroll
                                return fitCount;
                            }
                        if (fitCount == 1) // We can really only fit one control space-wise, but we still need to keep a label and control on the same line
                        {
                            if (row.Elements.Count > 2 && !row.Elements[2].IsConsideredLabelElement) // If there are any elements after the second one, we need to add another fake label
                                InsertFakeLabel(row.Elements, row.Elements[counter].Element, 2);
                            return 2;
                        }
                    }

                    // If the current control is the edit control, we also consider the label to not have fit
                    if (!element.IsConsideredLabelElement) fitCount--;
                    break;
                }
            }

            if (fitCount < 2) fitCount = 2; // Even if 2 controls don't fit, we leave them on that row anyway. Worst case, things have to scroll
            return fitCount;
        }

        /// <summary>
        /// Inserts a fake label to act as a spacer
        /// </summary>
        /// <param name="elements">The elements collection to insert the label into.</param>
        /// <param name="element">The element that follows (which could potentially define font attributes).</param>
        /// <param name="insertionIndex">Index of the insertion.</param>
        protected virtual void InsertFakeLabel(List<ControlRowElement> elements, UIElement element, int insertionIndex)
        {
            // Note: The font settings are technically superficial, since the label is an empty string. 
            //       It is still important to set this information though, since otherwise the rendering algorithm could misbehave
            var individualFont = SimpleView.GetLabelFontFamily(element);
            var font = individualFont ?? LabelFontFamily;

            var individualStyle = SimpleView.GetLabelFontStyle(element);
            var style = individualStyle == FontStyles.Normal ? LabelFontStyle : individualStyle;

            var individualWeight = SimpleView.GetLabelFontWeight(element);
            var weight = individualWeight == FontWeights.Normal ? LabelFontWeight : individualWeight;

            var individualSize = SimpleView.GetLabelFontSize(element);
            var size = individualSize > 0d ? individualSize : LabelFontSize;

            var individualBrush = SimpleView.GetLabelForegroundBrush(element);
            var brush = individualBrush ?? LabelForegroundBrush;

            elements.Insert(insertionIndex, new ControlRowElement
                {
                    Label = string.Empty,
                    IsAutoGeneratedLabel = true,
                    IsFakeLabel = true,
                    IsConsideredLabelElement = true,
                    HadOriginalLineBreak = false,
                    DesiredSize = CalculateLabelSize(string.Empty, element),
                    FontFamily = font,
                    FontStyle = style,
                    FontSize = size,
                    FontWeight = weight,
                    ForegroundBrush = brush
                });
        }

        /// <summary>
        /// Calculates the width of the widest first label in all the rows
        /// </summary>
        /// <param name="rows">Collection of control rows</param>
        /// <returns>Maximum width</returns>
        protected virtual double GetWidestFirstLabel(IEnumerable<ControlRow> rows)
        {
            var widestLabel = 0d;

            foreach (var row in rows)
                if (row.Elements.Count > 0)
                    widestLabel = Math.Max(row.Elements[0].DesiredSize.Width, widestLabel);

            return widestLabel;
        }

        /// <summary>
        /// When overridden in a derived class, positions child elements and determines a size for a <see cref="T:System.Windows.FrameworkElement" /> derived class.
        /// </summary>
        /// <param name="finalSize">The final area within the parent that this element should use to arrange itself and its children.</param>
        /// <returns>The actual size used.</returns>
        protected override Size ArrangeOverride(Size finalSize)
        {
            InvalidateVisual();
            if (_currentRows != null)
                foreach (var row in _currentRows)
                    foreach (var control in row.Elements.Where(element => !element.IsAutoGeneratedLabel))
                        control.Element.Arrange(GeometryHelper.NewRect(control.DesiredPosition, control.DesiredSize));

            return base.ArrangeOverride(finalSize);
        }

        /// <summary>
        /// Draws the content of a <see cref="T:System.Windows.Media.DrawingContext" /> object during the render pass of a <see cref="T:System.Windows.Controls.Panel" /> element.
        /// </summary>
        /// <param name="dc">The <see cref="T:System.Windows.Media.DrawingContext" /> object to draw.</param>
        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            // TODO: Also render all group headers using the renderer
            //GroupHeaderRenderer.RenderHeader(dc, new AutoHeaderTextRenderInfo{ }, );

            if (_currentRows != null)
                foreach (var row in _currentRows)
                    foreach (var control in row.Elements.Where(element => element.IsAutoGeneratedLabel))
                    {
                        var typeface = new Typeface(control.FontFamily, control.FontStyle, control.FontWeight, FontStretches.Normal);
                        var ft = new FormattedText(control.Label, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, typeface, control.FontSize, control.ForegroundBrush) {Trimming = TextTrimming.WordEllipsis, TextAlignment = LabelTextAlignment};
                        switch (LabelTextAlignment)
                        {
                            case TextAlignment.Left:
                                dc.DrawText(ft, control.DesiredPosition);
                                break;
                            case TextAlignment.Right:
                                var width = control.DesiredSize.Width;
                                if (control.MaxLabelWidth > 0d) width = control.MaxLabelWidth;
                                dc.DrawText(ft, new Point(control.DesiredPosition.X + width, control.DesiredPosition.Y));
                                break;
                            case TextAlignment.Center:
                                var width2 = control.DesiredSize.Width;
                                if (control.MaxLabelWidth > 0d) width2 = control.MaxLabelWidth;
                                width2 = width2/2;
                                dc.DrawText(ft, new Point(control.DesiredPosition.X + width2, control.DesiredPosition.Y));
                                break;
                            case TextAlignment.Justify:
                                var width3 = control.DesiredSize.Width;
                                if (control.MaxLabelWidth > 0d) width3 = control.MaxLabelWidth;
                                ft.MaxTextWidth = width3;
                                dc.DrawText(ft, control.DesiredPosition);
                                break;
                        }
                    }
        }
    }

    /// <summary>
    /// This class is designed for internal use only. It is used to logically organize controls within a FlowForm into distinct rows of controls
    /// </summary>
    public class ControlRow
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ControlRow"/> class.
        /// </summary>
        public ControlRow()
        {
            Elements = new List<ControlRowElement>();
        }

        /// <summary>List of elements within the row</summary>
        public List<ControlRowElement> Elements { get; set; }
    }

    /// <summary>
    /// This class is designed for internal use only. It is used to wrap a control/element within a row of controls
    /// </summary>
    public class ControlRowElement
    {
        /// <summary>Holds a reference to the actual element</summary>
        public FrameworkElement Element { get; set; }

        /// <summary>Desired control position</summary>
        public Point DesiredPosition { get; set; }

        /// <summary>Desired control size</summary>
        public Size DesiredSize { get; set; }

        /// <summary>Maximum label width in this column</summary>
        public double MaxLabelWidth { get; set; }

        /// <summary>Indicates whether this is a placeholder for an auto-generated label</summary>
        public bool IsAutoGeneratedLabel { get; set; }

        /// <summary>Label caption used for auto-generated labels</summary>
        public string Label { get; set; }

        /// <summary>True if the current element is considered to play the role of a label (typically an odd numbered element in the current row</summary>
        public bool IsConsideredLabelElement { get; set; }

        /// <summary>Indicates whether the control was originally flagged to force a line break before the control</summary>
        public bool HadOriginalLineBreak { get; set; }

        /// <summary>Indicates whether this control is meant to span the full width of the available surface</summary>
        public bool IsFullSpanControl { get; set; }

        /// <summary>
        /// Font family
        /// </summary>
        public FontFamily FontFamily { get; set; }

        /// <summary>
        /// Font Style
        /// </summary>
        public FontStyle FontStyle { get; set; }

        /// <summary>
        /// Font weight
        /// </summary>
        public FontWeight FontWeight { get; set; }

        /// <summary>
        /// Font size
        /// </summary>
        public double FontSize { get; set; }

        /// <summary>
        /// Color
        /// </summary>
        public Brush ForegroundBrush { get; set; }

        /// <summary>
        /// Indicates whether this control is a "fake" label control (which just acts as a spacer and can be removed from the flow at any time)
        /// </summary>
        public bool IsFakeLabel { get; set; }
    }
}

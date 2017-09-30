using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CODE.Framework.Wpf.BindingConverters;
using CODE.Framework.Wpf.Controls;
using System.Globalization;
using System.Linq;

namespace CODE.Framework.Wpf.Layout
{
    /// <summary>
    /// Special grid class that provides default behavior used by primary/secondary form layout styles
    /// </summary>
    public class GridPrimarySecondary : GridEx
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GridPrimarySecondary"/> class.
        /// </summary>
        public GridPrimarySecondary()
        {
            RowDefinitions.Clear();
            RowDefinitions.Add(new RowDefinition());
            RowDefinitions.Add(new RowDefinition {Height = new GridLength(3)});
            RowDefinitions.Add(new RowDefinition());

            ColumnDefinitions.Clear();
            ColumnDefinitions.Add(new ColumnDefinition());
            ColumnDefinitions.Add(new ColumnDefinition {Width = new GridLength(3)});
            ColumnDefinitions.Add(new ColumnDefinition());
        }

        /// <summary>Size of the secondary UI element (height only for horizontal splits)</summary>
        public GridLength SecondaryUIElementSize
        {
            get { return (GridLength) GetValue(SecondaryUIElementSizeProperty); }
            set { SetValue(SecondaryUIElementSizeProperty, value); }
        }

        /// <summary>Size of the secondary UI element (could be height or height)</summary>
        public static readonly DependencyProperty SecondaryUIElementSizeProperty = DependencyProperty.Register("SecondaryUIElementSize", typeof (GridLength), typeof (GridPrimarySecondary), new UIPropertyMetadata(new GridLength(250)));

        /// <summary>Spacing between primary and secondary UI elements (both horizontal and vertical)</summary>
        public double UIElementSpacing
        {
            get { return (double) GetValue(UIElementSpacingProperty); }
            set { SetValue(UIElementSpacingProperty, value); }
        }

        /// <summary>Spacing between primary and secondary UI elements (both horizontal and vertical)</summary>
        public static readonly DependencyProperty UIElementSpacingProperty = DependencyProperty.Register("UIElementSpacing", typeof (double), typeof (GridPrimarySecondary), new UIPropertyMetadata(10d, OnUIElementSpacingChanged));

        /// <summary>Handles changes in UI Element spacing</summary>
        /// <param name="obj">Object that changed</param>
        /// <param name="args">Event arguments</param>
        private static void OnUIElementSpacingChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            var grid = obj as GridPrimarySecondary;
            if (grid == null) return;
            grid.InvalidateMeasure();
        }

        /// <summary>Margin wrap around all elements flagged as primary UI elements</summary>
        public Thickness PrimaryUIElementMargin
        {
            get { return (Thickness) GetValue(PrimaryUIElementMarginProperty); }
            set { SetValue(PrimaryUIElementMarginProperty, value); }
        }

        /// <summary>Margin wrap around all elements flagged as primary UI elements</summary>
        public static readonly DependencyProperty PrimaryUIElementMarginProperty = DependencyProperty.Register("PrimaryUIElementMargin", typeof (Thickness), typeof (GridPrimarySecondary), new UIPropertyMetadata(new Thickness()));

        /// <summary>Margin wrap around all elements flagged as secondary UI elements</summary>
        public Thickness SecondaryUIElementMargin
        {
            get { return (Thickness) GetValue(SecondaryUIElementMarginProperty); }
            set { SetValue(SecondaryUIElementMarginProperty, value); }
        }

        /// <summary>Margin wrap around all elements flagged as secondary UI elements</summary>
        public static readonly DependencyProperty SecondaryUIElementMarginProperty = DependencyProperty.Register("SecondaryUIElementMargin", typeof (Thickness), typeof (GridPrimarySecondary), new UIPropertyMetadata(new Thickness()));

        /// <summary>Defines the size at which the layout automatically switches from primary (usually top/bottom) orientation to secondary (usually left/right) orientation</summary>
        public double SecondaryUIElementAlignmentChangeSize
        {
            get { return (double) GetValue(SecondaryUIElementAlignmentChangeSizeProperty); }
            set { SetValue(SecondaryUIElementAlignmentChangeSizeProperty, value); }
        }

        /// <summary>Defines the size at which the layout automatically switches from primary (usually top/bottom) orientation to secondary (usually left/right) orientation</summary>
        public static readonly DependencyProperty SecondaryUIElementAlignmentChangeSizeProperty = DependencyProperty.Register("SecondaryUIElementAlignmentChangeSize", typeof (double), typeof (GridPrimarySecondary), new UIPropertyMetadata(1d));

        /// <summary>Defines the logical orientation of primary and secondary UI elements</summary>
        /// <remarks>Each style/skin/theme can choose to interpret this setting differently</remarks>
        public PrimarySecondaryOrientation UIElementOrder
        {
            get { return (PrimarySecondaryOrientation) GetValue(UIElementOrderProperty); }
            set { SetValue(UIElementOrderProperty, value); }
        }

        /// <summary>Defines the logical orientation of primary and secondary UI elements</summary>
        /// <remarks>Each style/skin/theme can choose to interpret this setting differently</remarks>
        public static readonly DependencyProperty UIElementOrderProperty = DependencyProperty.Register("UIElementOrder", typeof (PrimarySecondaryOrientation), typeof (GridPrimarySecondary), new UIPropertyMetadata(PrimarySecondaryOrientation.SecondaryPrimary));

        /// <summary>Brush used for the background area of the secondary UI area</summary>
        /// <value>The secondary area background brush.</value>
        public Brush SecondaryAreaBackgroundBrush
        {
            get { return (Brush) GetValue(SecondaryAreaBackgroundBrushProperty); }
            set { SetValue(SecondaryAreaBackgroundBrushProperty, value); }
        }

        /// <summary>Brush used for the background area of the secondary UI area</summary>
        public static readonly DependencyProperty SecondaryAreaBackgroundBrushProperty = DependencyProperty.Register("SecondaryAreaBackgroundBrush", typeof (Brush), typeof (GridPrimarySecondary), new UIPropertyMetadata(null, SecondaryAreaBackgroundBrushPropertyChanged));

        /// <summary>Fires when the secondary area brush changed</summary>
        /// <param name="obj">The object the property was set on.</param>
        /// <param name="args">The <see cref="System.Windows.DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void SecondaryAreaBackgroundBrushPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            var grid = obj as GridPrimarySecondary;
            if (grid != null) grid.SetInternalSecondaryAreaBackgroundBrush();
        }

        /// <summary>Brush opacity used for the background area of the secondary UI area</summary>
        /// <value>The secondary area background brush opacity.</value>
        public double SecondaryAreaBackgroundBrushOpacity
        {
            get { return (double) GetValue(SecondaryAreaBackgroundBrushOpacityProperty); }
            set { SetValue(SecondaryAreaBackgroundBrushOpacityProperty, value); }
        }

        /// <summary>Brush opacity used for the background area of the secondary UI area</summary>
        public static readonly DependencyProperty SecondaryAreaBackgroundBrushOpacityProperty = DependencyProperty.Register("SecondaryAreaBackgroundBrushOpacity", typeof (double), typeof (GridPrimarySecondary), new UIPropertyMetadata(1d, SecondaryAreaBackgroundBrushOpacityPropertyChanged));

        /// <summary>Fires when the secondary area brush opacity changed</summary>
        /// <param name="obj">The object the property was set on.</param>
        /// <param name="args">The <see cref="System.Windows.DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void SecondaryAreaBackgroundBrushOpacityPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            var grid = obj as GridPrimarySecondary;
            if (grid != null) grid.SetInternalSecondaryAreaBackgroundBrush();
        }

        /// <summary>Brush LightFactor used for the background area of the secondary UI area</summary>
        /// <value>The secondary area background brush LightFactor.</value>
        public double SecondaryAreaBackgroundBrushLightFactor
        {
            get { return (double) GetValue(SecondaryAreaBackgroundBrushLightFactorProperty); }
            set { SetValue(SecondaryAreaBackgroundBrushLightFactorProperty, value); }
        }

        /// <summary>Brush LightFactor used for the background area of the secondary UI area</summary>
        public static readonly DependencyProperty SecondaryAreaBackgroundBrushLightFactorProperty = DependencyProperty.Register("SecondaryAreaBackgroundBrushLightFactor", typeof (double), typeof (GridPrimarySecondary), new UIPropertyMetadata(1d, SecondaryAreaBackgroundBrushLightFactorPropertyChanged));

        /// <summary>Fires when the secondary area brush LightFactor changed</summary>
        /// <param name="obj">The object the property was set on.</param>
        /// <param name="args">The <see cref="System.Windows.DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void SecondaryAreaBackgroundBrushLightFactorPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            var grid = obj as GridPrimarySecondary;
            if (grid != null) grid.SetInternalSecondaryAreaBackgroundBrush();
        }

        /// <summary>Updates the internal settings for the primary secondary brush</summary>
        private void SetInternalSecondaryAreaBackgroundBrush()
        {
            if (SecondaryAreaBackgroundBrush == null)
            {
                _secondaryAreaBackgroundBrush = null;
                return;
            }

            _secondaryAreaBackgroundBrush = SecondaryAreaBackgroundBrush.Clone();
            _secondaryAreaBackgroundBrush.Opacity = SecondaryAreaBackgroundBrushOpacity;

            if (SecondaryAreaBackgroundBrushLightFactor < .999d || SecondaryAreaBackgroundBrushLightFactor > 1.001d)
            {
                var converter = new LitBrushConverter();
                _secondaryAreaBackgroundBrush = converter.Convert(_secondaryAreaBackgroundBrush, typeof (Brush), SecondaryAreaBackgroundBrushLightFactor, CultureInfo.InvariantCulture) as Brush;
                if (_secondaryAreaBackgroundBrush != null) _secondaryAreaBackgroundBrush.Opacity = SecondaryAreaBackgroundBrushOpacity;
            }
        }

        /// <summary>Brush used for the background area of the primary UI area</summary>
        /// <value>The primary area background brush.</value>
        public Brush PrimaryAreaBackgroundBrush
        {
            get { return (Brush) GetValue(PrimaryAreaBackgroundBrushProperty); }
            set { SetValue(PrimaryAreaBackgroundBrushProperty, value); }
        }

        /// <summary>Brush used for the background area of the primary UI area</summary>
        public static readonly DependencyProperty PrimaryAreaBackgroundBrushProperty = DependencyProperty.Register("PrimaryAreaBackgroundBrush", typeof (Brush), typeof (GridPrimarySecondary), new UIPropertyMetadata(null, PrimaryAreaBackgroundBrushPropertyChanged));

        /// <summary>Fires when the primary area brush changed</summary>
        /// <param name="obj">The object the property was set on.</param>
        /// <param name="args">The <see cref="System.Windows.DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void PrimaryAreaBackgroundBrushPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            var grid = obj as GridPrimarySecondary;
            if (grid != null) grid.SetInternalPrimaryAreaBackgroundBrush();
        }

        /// <summary>Brush opacity used for the background area of the primary UI area</summary>
        /// <value>The secondary area background brush opacity.</value>
        public double PrimaryAreaBackgroundBrushOpacity
        {
            get { return (double) GetValue(PrimaryAreaBackgroundBrushOpacityProperty); }
            set { SetValue(PrimaryAreaBackgroundBrushOpacityProperty, value); }
        }

        /// <summary>Brush opacity used for the background area of the primary UI area</summary>
        public static readonly DependencyProperty PrimaryAreaBackgroundBrushOpacityProperty = DependencyProperty.Register("PrimaryAreaBackgroundBrushOpacity", typeof (double), typeof (GridPrimarySecondary), new UIPropertyMetadata(1d, PrimaryAreaBackgroundBrushOpacityPropertyChanged));

        /// <summary>Fires when the primary area brush opacity changed</summary>
        /// <param name="obj">The object the property was set on.</param>
        /// <param name="args">The <see cref="System.Windows.DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void PrimaryAreaBackgroundBrushOpacityPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            var grid = obj as GridPrimarySecondary;
            if (grid != null) grid.SetInternalPrimaryAreaBackgroundBrush();
        }

        /// <summary>Brush LightFactor used for the background area of the primary UI area</summary>
        /// <value>The secondary area background brush LightFactor.</value>
        public double PrimaryAreaBackgroundBrushLightFactor
        {
            get { return (double) GetValue(PrimaryAreaBackgroundBrushLightFactorProperty); }
            set { SetValue(PrimaryAreaBackgroundBrushLightFactorProperty, value); }
        }

        /// <summary>Brush LightFactor used for the background area of the primary UI area</summary>
        public static readonly DependencyProperty PrimaryAreaBackgroundBrushLightFactorProperty = DependencyProperty.Register("PrimaryAreaBackgroundBrushLightFactor", typeof (double), typeof (GridPrimarySecondary), new UIPropertyMetadata(1d, PrimaryAreaBackgroundBrushLightFactorPropertyChanged));

        /// <summary>Fires when the primary area brush LightFactor changed</summary>
        /// <param name="obj">The object the property was set on.</param>
        /// <param name="args">The <see cref="System.Windows.DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void PrimaryAreaBackgroundBrushLightFactorPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            var grid = obj as GridPrimarySecondary;
            if (grid != null) grid.SetInternalPrimaryAreaBackgroundBrush();
        }

        /// <summary>Updates the internal settings for the primary secondary brush</summary>
        private void SetInternalPrimaryAreaBackgroundBrush()
        {
            if (PrimaryAreaBackgroundBrush == null)
            {
                _primaryAreaBackgroundBrush = null;
                return;
            }

            _primaryAreaBackgroundBrush = PrimaryAreaBackgroundBrush.Clone();
            _primaryAreaBackgroundBrush.Opacity = PrimaryAreaBackgroundBrushOpacity;

            if (PrimaryAreaBackgroundBrushLightFactor < .999d || PrimaryAreaBackgroundBrushLightFactor > 1.001d)
            {
                var converter = new LitBrushConverter();
                _primaryAreaBackgroundBrush = converter.Convert(_primaryAreaBackgroundBrush, typeof (Brush), PrimaryAreaBackgroundBrushLightFactor, CultureInfo.InvariantCulture) as Brush;
                if (_primaryAreaBackgroundBrush != null) _primaryAreaBackgroundBrush.Opacity = PrimaryAreaBackgroundBrushOpacity;
            }
        }

        private Brush _primaryAreaBackgroundBrush;
        private Brush _secondaryAreaBackgroundBrush;

        /// <summary>
        /// For internal use only
        /// </summary>
        protected PrimarySecondaryLayout CalculatedLayout = PrimarySecondaryLayout.SecondaryTopPrimaryBottom;

        private bool SizeGrid(PrimarySecondaryLayout layout, GridLength secondaryUIElementSize)
        {
            var mustInvalidate = false;

            var star = new GridLength(1, GridUnitType.Star); // Standard measurement used repeatedly below
            var auto = new GridLength(1, GridUnitType.Auto); // Standard measurement used repeatedly below
            var zero = new GridLength(0, GridUnitType.Pixel); // Standard measurement used repeatedly below

            switch (layout)
            {
                case PrimarySecondaryLayout.PrimaryLeftSecondaryRight:
                    mustInvalidate = SetColumnWidth(0, star, false);
                    mustInvalidate = SetColumnWidth(1, new GridLength(UIElementSpacing), mustInvalidate);
                    mustInvalidate = SetColumnWidth(2, secondaryUIElementSize, mustInvalidate);
                    mustInvalidate = SetRowHeight(0, star, mustInvalidate);
                    mustInvalidate = SetRowHeight(1, zero, mustInvalidate);
                    mustInvalidate = SetRowHeight(2, zero, mustInvalidate);
                    break;
                case PrimarySecondaryLayout.SecondaryLeftPrimaryRight:
                    mustInvalidate = SetColumnWidth(0, secondaryUIElementSize, false);
                    mustInvalidate = SetColumnWidth(1, new GridLength(UIElementSpacing), mustInvalidate);
                    mustInvalidate = SetColumnWidth(2, star, mustInvalidate);
                    mustInvalidate = SetRowHeight(0, star, mustInvalidate);
                    mustInvalidate = SetRowHeight(1, zero, mustInvalidate);
                    mustInvalidate = SetRowHeight(2, zero, mustInvalidate);
                    break;
                case PrimarySecondaryLayout.PrimaryTopSecondaryBottom:
                    mustInvalidate = SetRowHeight(0, star, false);
                    mustInvalidate = SetRowHeight(1, new GridLength(UIElementSpacing), mustInvalidate);
                    mustInvalidate = SetRowHeight(2, auto, mustInvalidate);
                    mustInvalidate = SetColumnWidth(0, star, mustInvalidate);
                    mustInvalidate = SetColumnWidth(1, zero, mustInvalidate);
                    mustInvalidate = SetColumnWidth(2, zero, mustInvalidate);
                    break;
                case PrimarySecondaryLayout.SecondaryTopPrimaryBottom:
                    mustInvalidate = SetRowHeight(0, auto, false);
                    mustInvalidate = SetRowHeight(1, new GridLength(UIElementSpacing), mustInvalidate);
                    mustInvalidate = SetRowHeight(2, star, mustInvalidate);
                    mustInvalidate = SetColumnWidth(0, star, mustInvalidate);
                    mustInvalidate = SetColumnWidth(1, zero, mustInvalidate);
                    mustInvalidate = SetColumnWidth(2, zero, mustInvalidate);
                    break;
            }
            return mustInvalidate;
        }

        private bool SetColumnWidth(int columnIndex, GridLength width, bool oldMustInvalidate)
        {
            if (ColumnDefinitions[columnIndex].Width != width)
            {
                ColumnDefinitions[columnIndex].Width = width;
                return true;
            }
            return oldMustInvalidate;
        }

        private bool SetRowHeight(int rowIndex, GridLength height, bool oldMustInvalidate)
        {
            if (RowDefinitions[rowIndex].Height != height)
            {
                RowDefinitions[rowIndex].Height = height;
                return true;
            }
            return oldMustInvalidate;
        }

        /// <summary>
        /// Assigns the elements.
        /// </summary>
        /// <param name="layout">The layout.</param>
        /// <param name="primary">The primary.</param>
        /// <param name="secondary">The secondary.</param>
        /// <param name="mustInvalidate">if set to <c>true</c> [must invalidate].</param>
        /// <returns>True if successful</returns>
        protected virtual bool AssignElements(PrimarySecondaryLayout layout, UIElement primary, IEnumerable<UIElement> secondary, bool mustInvalidate)
        {
            switch (layout)
            {
                case PrimarySecondaryLayout.PrimaryLeftSecondaryRight:
                    mustInvalidate = AssignColumn(primary, 0, mustInvalidate);
                    mustInvalidate = AssignRow(primary, 0, mustInvalidate);
                    foreach (var element in secondary)
                    {
                        mustInvalidate = AssignColumn(element, 2, mustInvalidate);
                        mustInvalidate = AssignRow(element, 0, mustInvalidate);
                    }
                    break;
                case PrimarySecondaryLayout.SecondaryLeftPrimaryRight:
                    mustInvalidate = AssignColumn(primary, 2, mustInvalidate);
                    mustInvalidate = AssignRow(primary, 0, mustInvalidate);
                    foreach (var element in secondary)
                    {
                        mustInvalidate = AssignColumn(element, 0, mustInvalidate);
                        mustInvalidate = AssignRow(element, 0, mustInvalidate);
                    }
                    break;
                case PrimarySecondaryLayout.PrimaryTopSecondaryBottom:
                    mustInvalidate = AssignColumn(primary, 0, mustInvalidate);
                    mustInvalidate = AssignRow(primary, 0, mustInvalidate);
                    foreach (var element in secondary)
                    {
                        mustInvalidate = AssignColumn(element, 0, mustInvalidate);
                        mustInvalidate = AssignRow(element, 2, mustInvalidate);
                    }
                    break;
                case PrimarySecondaryLayout.SecondaryTopPrimaryBottom:
                    mustInvalidate = AssignColumn(primary, 0, mustInvalidate);
                    mustInvalidate = AssignRow(primary, 2, mustInvalidate);
                    foreach (var element in secondary)
                    {
                        mustInvalidate = AssignColumn(element, 0, mustInvalidate);
                        mustInvalidate = AssignRow(element, 0, mustInvalidate);
                    }
                    break;
            }
            return mustInvalidate;
        }

        private bool AssignColumn(UIElement element, int columnIndex, bool oldMustInvalidate)
        {
            if (GetColumn(element) != columnIndex)
            {
                SetColumn(element, columnIndex);
                return true;
            }
            return oldMustInvalidate;
        }

        private bool AssignRow(UIElement element, int rowIndex, bool oldMustInvalidate)
        {
            if (GetRow(element) != rowIndex)
            {
                SetRow(element, rowIndex);
                return true;
            }
            return oldMustInvalidate;
        }

        /// <summary>
        /// Measures the children of a <see cref="T:System.Windows.Controls.Grid" /> in anticipation of arranging them during the <see cref="M:System.Windows.Controls.Grid.ArrangeOverride(System.Windows.Size)" /> pass.
        /// </summary>
        /// <param name="constraint">Indicates an upper limit size that should not be exceeded.</param>
        /// <returns><see cref="T:System.Windows.Size" /> that represents the required size to arrange child content.</returns>
        protected override Size MeasureOverride(Size constraint)
        {
            var noMargin = new Thickness();
            foreach (var child in Children.OfType<UIElement>().Where(e => e.Visibility != Visibility.Collapsed))
            {
                var element = child as FrameworkElement;
                if (element == null) continue;
                var mode = SimpleView.GetUIElementType(element);
                if (mode == UIElementTypes.Primary)
                {
                    var localMargin = PrimaryUIElementMargin;
                    if (localMargin != noMargin && element.Margin != localMargin)
                        element.Margin = localMargin;
                }
                else if (mode == UIElementTypes.Secondary)
                {
                    var localMargin = SecondaryUIElementMargin;
                    if (localMargin != noMargin && element.Margin != localMargin)
                        element.Margin = localMargin;
                }
            }

            var size = base.MeasureOverride(constraint);

            bool mustInvalidate; // Used to potentially trigger a secondary measure pass
            var secondaryElementVisible = true;

            // We find the tallest secondary element. If it is taller than the threshold, we switch to horizontal layout
            var maxHeight = 0d;
            foreach (var element in Children.OfType<UIElement>().Where(e => e.Visibility != Visibility.Collapsed))
            {
                var type = SimpleView.GetUIElementType(element);
                if (type == UIElementTypes.Secondary)
                    if (element.Visibility != Visibility.Visible)
                    {
                        secondaryElementVisible = false;
                        maxHeight = 0;
                    }
                    else
                        maxHeight = Math.Max(element.DesiredSize.Height, maxHeight);
            }

            if (maxHeight > SecondaryUIElementAlignmentChangeSize)
            {
                // Going to left/right
                CalculatedLayout = UIElementOrder == PrimarySecondaryOrientation.SecondaryPrimary ? PrimarySecondaryLayout.SecondaryLeftPrimaryRight : PrimarySecondaryLayout.PrimaryLeftSecondaryRight;
                mustInvalidate = SizeGrid(CalculatedLayout, secondaryElementVisible ? SecondaryUIElementSize : new GridLength(0d));

                UIElement primary = null;
                var secondary = new List<UIElement>();

                // We need to assign children to columns
                foreach (var child in Children)
                {
                    var element = child as UIElement;
                    if (element == null) continue;
                    var type = SimpleView.GetUIElementType(element);
                    if (type == UIElementTypes.Secondary) secondary.Add(element);
                    else primary = element;
                }
                mustInvalidate = AssignElements(CalculatedLayout, primary, secondary, mustInvalidate);
            }
            else
            {
                // This is a top/bottom layout
                CalculatedLayout = UIElementOrder == PrimarySecondaryOrientation.SecondaryPrimary ? PrimarySecondaryLayout.SecondaryTopPrimaryBottom : PrimarySecondaryLayout.PrimaryTopSecondaryBottom;
                mustInvalidate = SizeGrid(CalculatedLayout, secondaryElementVisible ? SecondaryUIElementSize : new GridLength(0d));

                UIElement primary = null;
                var secondary = new List<UIElement>();

                // We need to assign children to rows
                foreach (var element in Children.OfType<UIElement>().Where(e => e.Visibility != Visibility.Collapsed))
                {
                    var type = SimpleView.GetUIElementType(element);
                    if (type == UIElementTypes.Secondary) secondary.Add(element);
                    else primary = element;
                }
                mustInvalidate = AssignElements(CalculatedLayout, primary, secondary, mustInvalidate);
            }

            if (maxHeight > 0 && mustInvalidate)
            {
                // Need to run all this one more time to make sure everything got picked up right
                MeasureOverride(constraint);
                InvalidateVisual();
            }

            return size;
        }

        /// <summary>Draws the content of a <see cref="T:System.Windows.Media.DrawingContext"/> object during the render pass of a <see cref="T:System.Windows.Controls.Panel"/> element.</summary>
        /// <param name="dc">The <see cref="T:System.Windows.Media.DrawingContext"/> object to draw.</param>
        protected override void OnRender(DrawingContext dc)
        {
            CustomRender(dc);
            base.OnRender(dc);
        }

        /// <summary>
        /// This method draws additional elements into the main display area
        /// </summary>
        /// <param name="dc">Drawing context</param>
        protected virtual void CustomRender(DrawingContext dc)
        {
            // Render the background color of the primary UI Area
            if (_primaryAreaBackgroundBrush != null)
            {
                var primaryRect = new Rect();
                switch (CalculatedLayout)
                {
                    case PrimarySecondaryLayout.PrimaryTopSecondaryBottom:
                        primaryRect.Y = 0d;
                        primaryRect.X = 0d;
                        primaryRect.Height = RowDefinitions[0].ActualHeight + RowDefinitions[1].ActualHeight;
                        primaryRect.Width = ActualWidth;
                        break;
                    case PrimarySecondaryLayout.SecondaryTopPrimaryBottom:
                        primaryRect.Y = RowDefinitions[0].ActualHeight;
                        primaryRect.X = 0d;
                        primaryRect.Height = RowDefinitions[1].ActualHeight + RowDefinitions[2].ActualHeight;
                        primaryRect.Width = ActualWidth;
                        break;
                    case PrimarySecondaryLayout.PrimaryLeftSecondaryRight:
                        primaryRect.Y = 0d;
                        primaryRect.X = 0d;
                        primaryRect.Height = ActualHeight;
                        primaryRect.Width = ColumnDefinitions[0].ActualWidth + ColumnDefinitions[1].ActualWidth;
                        break;
                    case PrimarySecondaryLayout.SecondaryLeftPrimaryRight:
                        primaryRect.Y = 0d;
                        primaryRect.X = ColumnDefinitions[0].ActualWidth;
                        primaryRect.Height = ActualHeight;
                        primaryRect.Width = ColumnDefinitions[1].ActualWidth + ColumnDefinitions[2].ActualWidth;
                        break;
                }
                dc.DrawRectangle(_primaryAreaBackgroundBrush, new Pen(_primaryAreaBackgroundBrush, 0), primaryRect);
            }

            // Render the background color of the secondary UI area
            if (_secondaryAreaBackgroundBrush != null)
            {
                var secondaryRect = new Rect();
                switch (CalculatedLayout)
                {
                    case PrimarySecondaryLayout.PrimaryTopSecondaryBottom:
                        secondaryRect.Y = RowDefinitions[0].ActualHeight + RowDefinitions[1].ActualHeight;
                        secondaryRect.X = 0d;
                        secondaryRect.Height = RowDefinitions[2].ActualHeight;
                        secondaryRect.Width = ActualWidth;
                        break;
                    case PrimarySecondaryLayout.SecondaryTopPrimaryBottom:
                        secondaryRect.Y = 0d;
                        secondaryRect.X = 0d;
                        secondaryRect.Height = RowDefinitions[0].ActualHeight;
                        secondaryRect.Width = ActualWidth;
                        break;
                    case PrimarySecondaryLayout.PrimaryLeftSecondaryRight:
                        secondaryRect.Y = 0d;
                        secondaryRect.X = ColumnDefinitions[0].ActualWidth + ColumnDefinitions[1].ActualWidth;
                        secondaryRect.Height = ActualHeight;
                        secondaryRect.Width = ColumnDefinitions[2].ActualWidth;
                        break;
                    case PrimarySecondaryLayout.SecondaryLeftPrimaryRight:
                        secondaryRect.Y = 0d;
                        secondaryRect.X = 0d;
                        secondaryRect.Height = ActualHeight;
                        secondaryRect.Width = ColumnDefinitions[0].ActualWidth;
                        break;
                }
                dc.DrawRectangle(_secondaryAreaBackgroundBrush, new Pen(_secondaryAreaBackgroundBrush, 0), secondaryRect);
            }
        }

        /// <summary>
        /// Layout options
        /// </summary>
        protected enum PrimarySecondaryLayout
        {
            /// <summary>
            /// Secondary on top, primary fills bottom
            /// </summary>
            SecondaryTopPrimaryBottom,

            /// <summary>
            /// Secondary on bottom, primary fills top
            /// </summary>
            PrimaryTopSecondaryBottom,

            /// <summary>
            /// Secondary on the left, primary fills right
            /// </summary>
            SecondaryLeftPrimaryRight,

            /// <summary>
            /// Secondary on the right, primary fills left
            /// </summary>
            PrimaryLeftSecondaryRight
        }
    }

    /// <summary>
    /// Defines the order in which UI elements are arranged
    /// </summary>
    public enum PrimarySecondaryOrientation
    {
        /// <summary>
        /// Secondary first, then primary (often secondary at the top and primary at the bottom, or secondary left and primary right)
        /// </summary>
        SecondaryPrimary,

        /// <summary>
        /// Primary first, then secondary (often secondary at the bottom and primary at the top, or secondary right and primary left)
        /// </summary>
        PrimarySecondary
    }
}
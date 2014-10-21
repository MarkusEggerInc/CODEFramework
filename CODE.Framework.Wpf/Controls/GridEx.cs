using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using CODE.Framework.Wpf.BindingConverters;

namespace CODE.Framework.Wpf.Controls
{
    /// <summary>
    /// Special subclassed version of a grid that supports styling column and row sizes
    /// </summary>
    public class GridEx : Grid
    {
        /// <summary>Attached property to set column widths</summary>
        /// <remarks>This attached property can be attached to any UI Element to define column widths</remarks>
        public static readonly DependencyProperty ColumnWidthsProperty = DependencyProperty.RegisterAttached("ColumnWidths", typeof (string), typeof (GridEx), new PropertyMetadata("*", ColumnWidthsPropertyChanged));

        /// <summary>
        /// Handler for column width changes
        /// </summary>
        /// <param name="d">Source object</param>
        /// <param name="e">Event arguments</param>
        private static void ColumnWidthsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var grid = d as Grid;
            if (grid == null) return;
            grid.ColumnDefinitions.Clear();
            var widths = e.NewValue.ToString();
            var parts = widths.Split(',');
            foreach (var part in parts)
            {
                var width = part.ToLower();
                if (string.IsNullOrEmpty(width)) width = "*";
                if (width.EndsWith("*"))
                {
                    string starWidth = width.Replace("*", string.Empty);
                    if (string.IsNullOrEmpty(starWidth)) starWidth = "1";
                    var stars = double.Parse(starWidth);
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(stars, GridUnitType.Star) });
                }
                else if (width == "auto")
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
                else
                {
                    var pixels = double.Parse(width);
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(pixels, GridUnitType.Pixel) });
                }
            }
        }

        /// <summary>Column widths</summary>
        /// <param name="obj">Object to set the columns widths on</param>
        /// <returns>Column width</returns>
        /// <remarks>This attached property can be attached to any UI Element to define the column width</remarks>
        public static string GetColumnWidths(DependencyObject obj)
        {
            return (string) obj.GetValue(ColumnWidthsProperty);
        }

        /// <summary>Column width</summary>
        /// <param name="obj">Object to set the column widths on</param>
        /// <param name="value">Value to set</param>
        public static void SetColumnWidths(DependencyObject obj, string value)
        {
            obj.SetValue(ColumnWidthsProperty, value);
        }

        /// <summary>Attached property to set row heights</summary>
        /// <remarks>This attached property can be attached to any UI Element to define row heights</remarks>
        public static readonly DependencyProperty RowHeightsProperty = DependencyProperty.RegisterAttached("RowHeights", typeof(string), typeof(GridEx), new PropertyMetadata("*", RowHeightsPropertyChanged));

        /// <summary>
        /// Handler for row height changes
        /// </summary>
        /// <param name="d">Source object</param>
        /// <param name="e">Event arguments</param>
        private static void RowHeightsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var grid = d as Grid;
            if (grid == null) return;
            grid.RowDefinitions.Clear();
            var heights = e.NewValue.ToString();
            var parts = heights.Split(',');
            foreach (var part in parts)
            {
                var height = part.ToLower();
                if (string.IsNullOrEmpty(height)) height = "*";
                if (height.EndsWith("*"))
                {
                    string starHeight = height.Replace("*", string.Empty);
                    if (string.IsNullOrEmpty(starHeight)) starHeight = "1";
                    var stars = double.Parse(starHeight);
                    grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(stars, GridUnitType.Star) });
                }
                else if (height == "auto")
                    grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                else
                {
                    var pixels = double.Parse(height);
                    grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(pixels, GridUnitType.Pixel) });
                }
            }
        }

        /// <summary>Row heights</summary>
        /// <param name="obj">Object to set the row heights on</param>
        /// <returns>Column width</returns>
        /// <remarks>This attached property can be attached to any UI Element to define the row height</remarks>
        public static string GetRowHeights(DependencyObject obj)
        {
            return (string)obj.GetValue(RowHeightsProperty);
        }

        /// <summary>Row heights</summary>
        /// <param name="obj">Object to set the row heights on</param>
        /// <param name="value">Value to set</param>
        public static void SetRowHeights(DependencyObject obj, string value)
        {
            obj.SetValue(RowHeightsProperty, value);
        }

        /// <summary>If this grid is used within a list box (list item), it can be set to automatically adjust its width to match the exact width available to list items</summary>
        /// <value><c>true</c> if [adjust width to parent list item]; otherwise, <c>false</c>.</value>
        public bool AdjustWidthToParentListItem
        {
            get { return (bool)GetValue(AdjustWidthToParentListItemProperty); }
            set { SetValue(AdjustWidthToParentListItemProperty, value); }
        }

        /// <summary>If this grid is used within a list box (list item), it can be set to automatically adjust its width to match the exact width available to list items</summary>
        public static readonly DependencyProperty AdjustWidthToParentListItemProperty = DependencyProperty.Register("AdjustWidthToParentListItem", typeof(bool), typeof(GridEx), new UIPropertyMetadata(false, AdjustWidthToParentListItemPropertyChanged));

        /// <summary>Fires when the adjust width to parent list item property changes</summary>
        /// <param name="d">The grid this was set on</param>
        /// <param name="e">Event arguments</param>
        private static void AdjustWidthToParentListItemPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var grid = d as GridEx;
            if (grid != null)
                if ((bool)e.NewValue) grid.CreateListItemWidthBinding();
        }

        /// <summary>Indicates whether the binding to the width of the list item has already been created for the grid</summary>
        private bool _listItemWidthBound;

        /// <summary>Creates a binding to set the grid to the width of the list item</summary>
        private void CreateListItemWidthBinding()
        {
            if (!_listItemWidthBound)
            {
                SetBinding(ListItemWidthProperty, new Binding("ActualWidth") { RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(ListBoxItem), 1) });
                _listItemWidthBound = true;
            }
        }
        /// <summary>Invoked when the parent of this element in the visual tree is changed. Overrides <see cref="M:System.Windows.UIElement.OnVisualParentChanged(System.Windows.DependencyObject)"/>.</summary>
        /// <param name="oldParent">The old parent element. May be null to indicate that the element did not have a visual parent previously.</param>
        protected override void OnVisualParentChanged(DependencyObject oldParent)
        {
            base.OnVisualParentChanged(oldParent);
            if (AdjustWidthToParentListItem) CreateListItemWidthBinding();
        }

        /// <summary>If this grid is used within a list box (list item), it can automatically expose an item index property that can be used to know if the row is odd or even and the like.</summary>
        /// <remarks>For this to have any effect, the list itself must have an alternation count set to something other than 0 or 1</remarks>
        /// <value>True of false</value>
        public bool UseItemIndex
        {
            get { return (bool)GetValue(UseItemIndexProperty); }
            set { SetValue(UseItemIndexProperty, value); }
        }

        /// <summary>If this grid is used within a list box (list item), it can automatically expose an alternation count property that can be used to know if the row is odd or even and the like.</summary>
        /// <remarks>For this to have any effect, the list itself must have an alternation count set to something other than 0 or 1</remarks>
        public static readonly DependencyProperty UseItemIndexProperty = DependencyProperty.Register("UseItemIndex", typeof(bool), typeof(GridEx), new UIPropertyMetadata(false, UseItemIndexPropertyChanged));

        /// <summary>Fires when the user alternation count property changes</summary>
        /// <param name="d">The grid this was set on</param>
        /// <param name="e">Event arguments</param>
        private static void UseItemIndexPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var grid = d as GridEx;
            if (grid != null && (bool)e.NewValue)
            {
                var item = FindAncestor<ListBoxItem>(grid);
                if (item == null) return;
                var listBox = ItemsControl.ItemsControlFromItemContainer(item);
                if (listBox == null) return;
                var index = listBox.ItemContainerGenerator.IndexFromContainer(item);
                grid.ItemIndex = index;
                grid.IsOddRow = index%2 == 1;
            }
        }

        /// <summary>Walks the visual tree to find the parent of a certain type</summary>
        /// <typeparam name="T">Type to search</typeparam>
        /// <param name="d">Object for which to find the ancestor</param>
        /// <returns>Object or null</returns>
        private static T FindAncestor<T>(DependencyObject d) where T: class
        {
            var parent = VisualTreeHelper.GetParent(d);
            if (parent == null) return null;
            if (parent is T) return parent as T;
            return FindAncestor<T>(parent);
        }

        /// <summary>If this grid is used within a list box (list item), and the UseAlternationCount property is set to true, this property will tell the alternation count (such as 0 and 1 for odd and even rows).</summary>
        /// <remarks>For this to have any effect, the list itself must have an alternation count set to something other than 0 or 1</remarks>
        public int ItemIndex
        {
            get { return (int)GetValue(ItemIndexProperty); }
            set { SetValue(ItemIndexProperty, value); }
        }

        /// <summary>If this grid is used within a list box (list item), and the UseAlternationCount property is set to true, this property will tell the alternation count (such as 0 and 1 for odd and even rows).</summary>
        /// <remarks>For this to have any effect, the list itself must have an alternation count set to something other than 0 or 1</remarks>
        public static readonly DependencyProperty ItemIndexProperty = DependencyProperty.Register("ItemIndex", typeof(int), typeof(GridEx), new UIPropertyMetadata(0));

        /// <summary>Indicates whether this grid is used in an item in a list control and that item has an odd row index</summary>
        /// <remarks>Only works if this grid is used in a data template inside of a listbox</remarks>
        public bool IsOddRow
        {
            get { return (bool)GetValue(IsOddRowProperty); }
            set { SetValue(IsOddRowProperty, value); }
        }
        /// <summary>Indicates whether this grid is used in an item in a list control and that item has an odd row index</summary>
        /// <remarks>Only works if this grid is used in a data template inside of a listbox</remarks>
        public static readonly DependencyProperty IsOddRowProperty = DependencyProperty.Register("IsOddRow", typeof(bool), typeof(GridEx), new UIPropertyMetadata(true));

        /// <summary>For internal use only</summary>
        public double ListItemWidth
        {
            get { return (double)GetValue(ListItemWidthProperty); }
            set { SetValue(ListItemWidthProperty, value); }
        }
        /// <summary>For internal use only</summary>
        public static readonly DependencyProperty ListItemWidthProperty = DependencyProperty.Register("ListItemWidth", typeof(double), typeof(GridEx), new UIPropertyMetadata(0d, OnListItemWidthChanged));
        /// <summary>For internal use only</summary>
        /// <param name="source">The source.</param>
        /// <param name="e">The <see cref="System.Windows.DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void OnListItemWidthChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var grid = source as Grid;
            if (grid != null)
            {
                var newValue = (double)e.NewValue;
                newValue -= 4;
                grid.Width = Math.Max(newValue, 0d);
            }
        }

        /// <summary>Internal method used to set the background brush on a Grid object</summary>
        /// <param name="grid">The grid on which to set these values.</param>
        /// <param name="brush">The brush.</param>
        /// <param name="lightFactor">The light factor.</param>
        /// <param name="opacity">The opacity.</param>
        /// <remarks>Combines BackgroundBrush, BackgroundBrushLightFactor, and BackgroundBrushOpacity to set the Background property</remarks>
        private static void SetBackground(Panel grid, Brush brush, double lightFactor, double opacity)
        {
            if (brush == null) return;
            var brush2 = brush.CloneCurrentValue();
            brush2.Opacity = opacity;

            if (lightFactor < .999d || lightFactor > 1.001d)
            {
                var converter = new LitBrushConverter();
                brush2 = converter.Convert(brush2, typeof(Brush), lightFactor, CultureInfo.InvariantCulture) as Brush;
                if (brush2 != null) brush2.Opacity = opacity;
            }

            grid.Background = brush2;
        }

        /// <summary>Background brush</summary>
        /// <remarks>This property is similar to the Background property, but can be used in conjunction with BackgroundBrushLightFactor and BackgroundBrushOpacity</remarks>
        public static readonly DependencyProperty BackgroundBrushProperty = DependencyProperty.RegisterAttached("BackgroundBrush", typeof(Brush), typeof(GridEx), new PropertyMetadata(null, BackgroundBrushPropertyChanged));
        /// <summary>Handler for background brush changed</summary>
        /// <param name="d">Source object</param>
        /// <param name="e">Event arguments</param>
        private static void BackgroundBrushPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var panel = d as Panel;
            if (panel == null) return;
            SetBackground(panel, e.NewValue as Brush, GetBackgroundBrushLightFactor(panel), GetBackgroundBrushOpacity(panel));
        }
        /// <summary>Background brush</summary>
        /// <returns>Background brush</returns>
        /// <remarks>This property is similar to the Background property, but can be used in conjunction with BackgroundLightFactor and BackgroundBrushTransparency</remarks>
        public static Brush GetBackgroundBrush(DependencyObject obj)
        {
            return (Brush)obj.GetValue(BackgroundBrushProperty);
        }
        /// <summary>Background brush</summary>
        /// <remarks>This property is similar to the Background property, but can be used in conjunction with BackgroundLightFactor and BackgroundBrushTransparency</remarks>
        /// <param name="obj">Object to set the value on</param>
        /// <param name="value">Value to set</param>
        public static void SetBackgroundBrush(DependencyObject obj, Brush value)
        {
            obj.SetValue(BackgroundBrushProperty, value);
        }

        /// <summary>Background brush light factor</summary>
        public static readonly DependencyProperty BackgroundBrushLightFactorProperty = DependencyProperty.RegisterAttached("BackgroundBrushLightFactor", typeof(double), typeof(GridEx), new PropertyMetadata(1d, BackgroundBrushLightFactorPropertyChanged));
        /// <summary>Handler for background brush light factor changed</summary>
        /// <param name="d">Source object</param>
        /// <param name="e">Event arguments</param>
        private static void BackgroundBrushLightFactorPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var panel = d as Panel;
            if (panel == null) return;
            SetBackground(panel, GetBackgroundBrush(panel), (double)e.NewValue, GetBackgroundBrushOpacity(panel));
        }
        /// <summary>Background brush light factor</summary>
        /// <returns>Background brush light factor</returns>
        public static double GetBackgroundBrushLightFactor(DependencyObject obj)
        {
            return (double)obj.GetValue(BackgroundBrushLightFactorProperty);
        }
        /// <summary>Background brush light factor</summary>
        /// <param name="obj">Object to set the value on</param>
        /// <param name="value">Value to set</param>
        public static void SetBackgroundBrushLightFactor(DependencyObject obj, double value)
        {
            obj.SetValue(BackgroundBrushLightFactorProperty, value);
        }

        /// <summary>Background brush light factor</summary>
        public static readonly DependencyProperty BackgroundBrushOpacityProperty = DependencyProperty.RegisterAttached("BackgroundBrushOpacity", typeof(double), typeof(GridEx), new PropertyMetadata(1d, BackgroundBrushOpacityPropertyChanged));
        /// <summary>Handler for background brush light factor changed</summary>
        /// <param name="d">Source object</param>
        /// <param name="e">Event arguments</param>
        private static void BackgroundBrushOpacityPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var panel = d as Panel;
            if (panel == null) return;
            SetBackground(panel, GetBackgroundBrush(panel), GetBackgroundBrushLightFactor(panel), (double)e.NewValue);
        }
        /// <summary>Background brush opacity</summary>
        /// <returns>Background brush opacity</returns>
        public static double GetBackgroundBrushOpacity(DependencyObject obj)
        {
            return (double)obj.GetValue(BackgroundBrushOpacityProperty);
        }
        /// <summary>Background brush opacity</summary>
        /// <param name="obj">Object to set the value on</param>
        /// <param name="value">Value to set</param>
        public static void SetBackgroundBrushOpacity(DependencyObject obj, double value)
        {
            obj.SetValue(BackgroundBrushOpacityProperty, value);
        }

        /// <summary>
        /// When set to true, this property enables mouse moving of the element using render transforms (translate transform)
        /// </summary>
        public static bool GetEnableRenderTransformMouseMove(DependencyObject obj, bool value)
        {
            return (bool)obj.GetValue(EnableRenderTransformMouseMoveProperty);
        }

        /// <summary>
        /// When set to true, this property enables mouse moving of the element using render transforms (translate transform)
        /// </summary>
        public static void SetEnableRenderTransformMouseMove(DependencyObject obj, bool value)
        {
            obj.SetValue(EnableRenderTransformMouseMoveProperty, value);
        }

        /// <summary>
        /// When set to true, this property enables mouse moving of the element using render transforms (translate transform)
        /// </summary>
        public static readonly DependencyProperty EnableRenderTransformMouseMoveProperty = DependencyProperty.RegisterAttached("EnableRenderTransformMouseMove", typeof(bool), typeof(GridEx), new PropertyMetadata(false, OnEnableRenderTransformMouseMoveChanged));

        private static Window GetWindow(UIElement element)
        {
            var parent = VisualTreeHelper.GetParent(element);
            while (!(parent is Window))
            {
                parent = VisualTreeHelper.GetParent(parent);
                if (parent == null) return null;
            }
            return parent as Window;
        }

        /// <summary>
        /// Fires when the EnableRenderTransformMouseMove property changes
        /// </summary>
        /// <param name="d">The d.</param>
        /// <param name="args">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        private static void OnEnableRenderTransformMouseMoveChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            if (!(bool) args.NewValue) return;

            var grid = d as Grid;
            if (grid == null) return;

            grid.MouseLeftButtonDown += (s, e) =>
            {
                var grid2 = s as Grid;
                if (grid2 == null) return;
                if (_mouseMoveDownPositions == null) _mouseMoveDownPositions = new Dictionary<DependencyObject, MouseMoveWrapper>();

                var window = GetWindow(grid2);
                if (window == null) return;
                var position = e.GetPosition(window);
                var mouseInfo = new MouseMoveWrapper {DownPosition = position, ParentWindow = window, IsButtonDown = true};
                var translateTransform = grid2.RenderTransform as TranslateTransform;
                if (translateTransform != null) mouseInfo.OriginalTranslate = new Point(translateTransform.X, translateTransform.Y);

                if (_mouseMoveDownPositions.ContainsKey(grid2)) _mouseMoveDownPositions[grid2] = mouseInfo;
                else
                {
                    _mouseMoveDownPositions.Add(grid2, mouseInfo);

                    window.MouseMove += (s2, e2) =>
                    {
                        if (!_mouseMoveDownPositions.ContainsKey(grid2)) return;
                        if (!_mouseMoveDownPositions[grid2].IsButtonDown) return;

                        var mouseInfo2 = _mouseMoveDownPositions[grid2];
                        var position2 = e2.GetPosition(mouseInfo2.ParentWindow);
                        var delta2 = new Point(position2.X - mouseInfo2.DownPosition.X, position2.Y - mouseInfo2.DownPosition.Y);
                        var newTranslate2 = new Point(delta2.X + mouseInfo2.OriginalTranslate.X, delta2.Y + mouseInfo2.OriginalTranslate.Y);

                        if (grid2.RenderTransform == null || grid.RenderTransform is MatrixTransform)
                        {
                            grid2.RenderTransform = new TranslateTransform(newTranslate2.X, newTranslate2.Y);
                            return;
                        }

                        var transformGroup = grid2.RenderTransform as TransformGroup;
                        if (transformGroup != null)
                        {
                            foreach (var transform in transformGroup.Children)
                            {
                                var translateTransform2 = transform as TranslateTransform;
                                if (translateTransform2 != null)
                                {
                                    translateTransform2.X = newTranslate2.X;
                                    translateTransform2.Y = newTranslate2.Y;
                                    return;
                                }
                            }
                            transformGroup.Children.Add(new TranslateTransform(newTranslate2.X, newTranslate2.Y));
                            return;
                        }

                        if ((delta2.X > 1 || delta2.X < -1) || (delta2.Y > 1 || delta2.Y < -1))
                        {
                            var translateTransform2 = grid2.RenderTransform as TranslateTransform;
                            if (translateTransform2 != null)
                            {
                                translateTransform2.X = newTranslate2.X;
                                translateTransform2.Y = newTranslate2.Y;
                                return;
                            }
                            grid2.RenderTransform = new TranslateTransform(newTranslate2.X, newTranslate2.Y);
                        }
                    };

                    window.MouseLeftButtonUp += (s3, e3) =>
                    {
                        if (!_mouseMoveDownPositions.ContainsKey(grid2)) return;
                        if (!_mouseMoveDownPositions[grid2].IsButtonDown) return;
                        _mouseMoveDownPositions[grid2].IsButtonDown = false;
                        Mouse.Capture(null);
                    };
                }

                Mouse.Capture(window);
            };
        }

        private static Dictionary<DependencyObject, MouseMoveWrapper> _mouseMoveDownPositions;

        private class MouseMoveWrapper
        {
            public Point DownPosition { get; set; }
            public Point OriginalTranslate { get; set; }
            public Window ParentWindow { get; set; }
            public bool IsButtonDown { get; set; }
        }
    }
}

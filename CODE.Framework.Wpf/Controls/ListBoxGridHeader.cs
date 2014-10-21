using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace CODE.Framework.Wpf.Controls
{
    /// <summary>Generic header control usable with Lists to create a data grid-style header</summary>
    public class ListBoxGridHeader : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ListBoxGridHeader"/> class.
        /// </summary>
        public ListBoxGridHeader()
        {
            Visibility = Visibility.Collapsed;
            ClipToBounds = true;
            IsHitTestVisible = true;
            Background = Brushes.Transparent;
        }

        /// <summary>Generic column definition</summary>
        public ListColumnsCollection Columns
        {
            get { return (ListColumnsCollection) GetValue(ColumnsProperty); }
            set { SetValue(ColumnsProperty, value); }
        }

        /// <summary>Generic column definition</summary>
        public static readonly DependencyProperty ColumnsProperty = DependencyProperty.Register("Columns", typeof (ListColumnsCollection), typeof (ListBoxGridHeader), new UIPropertyMetadata(null, OnColumnsChanged));

        /// <summary>Called when columns change.</summary>
        /// <param name="o">The object the columns changed on.</param>
        /// <param name="args">The <see cref="System.Windows.DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void OnColumnsChanged(DependencyObject o, DependencyPropertyChangedEventArgs args)
        {
            var header = o as ListBoxGridHeader;
            if (header == null) return;
            header.RepopulateHeaders();
            var columns = args.NewValue as ListColumnsCollection;
            if (columns == null) return;
            columns.CollectionChanged += (o2, a) => header.RepopulateHeaders();
        }

        /// <summary>Reference to the parent listbox this header belongs to</summary>
        /// <value>The parent ListBox.</value>
        public ListBox ParentListBox
        {
            get { return (ListBox)GetValue(ParentListBoxProperty); }
            set { SetValue(ParentListBoxProperty, value); }
        }
        /// <summary>Reference to the parent listbox this header belongs to</summary>
        /// <value>The parent ListBox.</value>
        public static readonly DependencyProperty ParentListBoxProperty = DependencyProperty.Register("ParentListBox", typeof(ListBox), typeof(ListBoxGridHeader), new PropertyMetadata(null, ParentListBoxChanged));
        /// <summary>Fires when the parent list box changes</summary>
        /// <param name="d">The d.</param>
        /// <param name="args">The <see cref="System.Windows.DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void ParentListBoxChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            var header = d as ListBoxGridHeader;
            if (header == null) return;
            if (args.NewValue == null) return;
            var listBox = args.NewValue as ListBox;
            if (listBox == null) return;

            header.SetEditControlVisibility(ListEx.GetShowHeaderEditControls(listBox));

            var pd = DependencyPropertyDescriptor.FromProperty(ListEx.ShowHeaderEditControlsProperty, typeof (ListEx));
            pd.AddValueChanged(listBox, (s, e) => header.SetEditControlVisibility(ListEx.GetShowHeaderEditControls(listBox)));
        }

        private void SetEditControlVisibility(bool visible)
        {
            foreach (var grid in _headerContentGrids)
                if (grid.RowDefinitions.Count > 0)
                {
                    grid.RowDefinitions[0].Height = visible ? new GridLength(1d, GridUnitType.Star) : new GridLength(0d, GridUnitType.Star);
                    foreach (var child in grid.Children)
                    {
                        var element = child as UIElement;
                        if (element != null)
                            if (Grid.GetRow(element) == 0)
                                element.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
                    }
                }
        }

        /// <summary>Horizontal offset of the header</summary>
        public double HorizontalHeaderOffset
        {
            get { return (double) GetValue(HorizontalHeaderOffsetProperty); }
            set { SetValue(HorizontalHeaderOffsetProperty, value); }
        }

        /// <summary>Horizontal offset of the header</summary>
        public static readonly DependencyProperty HorizontalHeaderOffsetProperty = DependencyProperty.Register("HorizontalHeaderOffset", typeof (double), typeof (ListBoxGridHeader), new UIPropertyMetadata(0d, HorizontalHeaderOffsetChanged));

        /// <summary>Horizontals the header offset changed.</summary>
        /// <param name="o">The object the property was changed on.</param>
        /// <param name="args">The <see cref="System.Windows.DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void HorizontalHeaderOffsetChanged(DependencyObject o, DependencyPropertyChangedEventArgs args)
        {
            var header = o as ListBoxGridHeader;
            if (header == null || header.Content == null) return;
            header.InvalidateHorizontalHeaderOffset();
        }

        /// <summary>
        /// Forces re-applying of the horizontal header offset
        /// </summary>
        public void InvalidateHorizontalHeaderOffset()
        {
            if (Content == null) return;
            var content = Content as Grid;
            if (content == null) return;
            content.Margin = new Thickness(HorizontalHeaderOffset * -1, 0, 0, 0);
        }

        private void ForceParentRefresh()
        {
            if (Parent == null) return;
            var element = Parent as FrameworkElement;
            if (element == null) return;
            element.InvalidateMeasure();
            element.InvalidateArrange();
            element.InvalidateVisual();
        }

        private readonly List<Grid> _headerContentGrids = new List<Grid>();

        private void RepopulateHeaders()
        {
            if (Columns == null)
            {
                Visibility = Visibility.Collapsed;
                ForceParentRefresh();
                return;
            }

            if (Visibility != Visibility.Visible)
            {
                Visibility = Visibility.Visible;
                ForceParentRefresh();
            }
            var grid = new Grid
                {
                    VerticalAlignment = VerticalAlignment.Stretch,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Width = 100000,
                    ClipToBounds = true
                };

            _headerContentGrids.Clear();
            var columnCounter = -1;
            var starColumnFound = false;
            foreach (var column in Columns)
            {
                columnCounter++;

                var gridColumn = new ColumnDefinition();
                gridColumn.SetBinding(ColumnDefinition.WidthProperty, new Binding("Width") {Source = column, Mode = BindingMode.TwoWay});
                var descriptor = DependencyPropertyDescriptor.FromProperty(ColumnDefinition.WidthProperty, typeof(ColumnDefinition));
                if (descriptor != null)
                {
                    var localColumn = column;
                    descriptor.AddValueChanged(gridColumn, (s2, e2) => { localColumn.ActualWidth = gridColumn.ActualWidth; });
                    grid.LayoutUpdated += (s3, e3) => { localColumn.ActualWidth = gridColumn.ActualWidth; };
                    grid.SizeChanged += (s4, e4) => { localColumn.ActualWidth = gridColumn.ActualWidth; };
                    SizeChanged += (s4, e4) => { localColumn.ActualWidth = gridColumn.ActualWidth; };
                }
                grid.ColumnDefinitions.Add(gridColumn);
                if (column.Width.GridUnitType == GridUnitType.Star) starColumnFound = true;

                var content = new HeaderContentControl {Column = column};

                if (!string.IsNullOrEmpty(column.HeaderClickCommandBindingPath))
                {
                    var binding = new Binding(column.HeaderClickCommandBindingPath);
                    content.SetBinding(HeaderContentControl.HeaderClickCommandProperty, binding);
                }

                var contentParent = new ContentControl {VerticalAlignment = VerticalAlignment.Stretch, HorizontalAlignment = HorizontalAlignment.Stretch};
                if (column.HeaderEventCommands != null && column.HeaderEventCommands.Count > 0)
                    Ex.SetEventCommands(contentParent, column.HeaderEventCommands);

                if (column.HeaderTemplate != null)
                {
                    var realContent = column.HeaderTemplate.LoadContent();
                    var realContentElement = realContent as UIElement;
                    if (realContentElement != null)
                        contentParent.Content = realContentElement;
                }
                else
                {
                    var contentGrid = new Grid {HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch};
                    _headerContentGrids.Add(contentGrid);
                    contentGrid.RowDefinitions.Add(new RowDefinition {Height = new GridLength(1d, GridUnitType.Star)});
                    contentGrid.RowDefinitions.Add(new RowDefinition {Height = new GridLength(1d, GridUnitType.Auto)});
                    if (column.ShowColumnHeaderText)
                    {
                        var textPlusGraphic = new TextPlusGraphic();
                        var headerText = new TextBlock();
                        textPlusGraphic.Children.Add(headerText);
                        if (column.Header != null)
                            headerText.Text = column.Header.ToString();
                        else
                            column.Header = " ";
                        var sort = new SortOrderIndicator();
                        if (!string.IsNullOrEmpty(column.SortOrderBindingPath))
                            sort.SetBinding(SortOrderIndicator.OrderProperty, new Binding(column.SortOrderBindingPath));
                        else
                            sort.Order = column.SortOrder;
                        textPlusGraphic.Children.Add(sort);
                        Grid.SetRow(textPlusGraphic, 1);
                        contentGrid.Children.Add(textPlusGraphic);
                    }
                    if (column.ShowColumnHeaderEditControl)
                    {
                        var headerTb = new TextBox();
                        if (!string.IsNullOrEmpty(column.ColumnHeaderEditControlBindingPath))
                            headerTb.SetBinding(TextBox.TextProperty, new Binding(column.ColumnHeaderEditControlBindingPath) {UpdateSourceTrigger = column.ColumnHeaderEditControlUpdateTrigger});
                        if (!string.IsNullOrEmpty(column.ColumnHeaderEditControlWatermarkText))
                            TextBoxEx.SetWatermarkText(headerTb, column.ColumnHeaderEditControlWatermarkText);
                        contentGrid.Children.Add(headerTb);
                    }
                    contentParent.Content = contentGrid;
                }
                content.Content = contentParent;

                grid.Children.Add(content);
                Grid.SetColumn(content, columnCounter);
                content.VerticalContentAlignment = VerticalAlignment.Stretch;
                content.HorizontalContentAlignment = HorizontalAlignment.Stretch;
                content.HorizontalAlignment = HorizontalAlignment.Stretch;
                content.VerticalAlignment = VerticalAlignment.Stretch;

                if (column.IsResizable)
                {
                    var splitter = new GridSplitter
                    {
                        VerticalAlignment = VerticalAlignment.Stretch, 
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Width = 3d,
                        Margin = new Thickness(0d, 0d, -1d, 0d),
                        IsHitTestVisible = true,
                        Background = Brushes.Transparent
                    };
                    Panel.SetZIndex(splitter, 30000);
                    grid.Children.Add(splitter);
                    Grid.SetColumn(splitter, columnCounter);
                }
            }
            if (!starColumnFound)
                grid.ColumnDefinitions.Add(new ColumnDefinition {Width = new GridLength(1, GridUnitType.Star)}); // Need this column to properly support resizing
            else
            {
                grid.HorizontalAlignment = HorizontalAlignment.Stretch;
                grid.Width = double.NaN;
            }
            Content = grid;

            if (ParentListBox != null)
                SetEditControlVisibility(ListEx.GetShowHeaderEditControls(ParentListBox));
        }
    }

    /// <summary>Parameters passed to the header click command</summary>
    public class HeaderClickCommandParameters
    {
        /// <summary>Reference to the clicked column</summary>
        public ListColumn Column { get; set; }
    }

    /// <summary>For internal use only</summary>
    public class HeaderContentControl : ContentControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HeaderContentControl"/> class.
        /// </summary>
        public HeaderContentControl()
        {
            IsHitTestVisible = true;
            Background = Brushes.Transparent;

            MouseLeftButtonDown += (s, e) =>
            {
                Mouse.Capture(this);
                e.Handled = true;
            };

            MouseLeftButtonUp += (s, e) =>
            {
                if (Mouse.Captured == this) Mouse.Capture(null);
                var position = Mouse.GetPosition(this);
                if (position.X < 0 || position.Y < 0 || position.X > ActualWidth || position.Y > ActualHeight) return;
                var localContent = s as HeaderContentControl;
                if (localContent == null) return;
                if (e.ClickCount != 1) return;
                if (HeaderClickCommand == null) return;
                var paras = new HeaderClickCommandParameters {Column = localContent.Column};
                if (HeaderClickCommand.CanExecute(paras))
                    HeaderClickCommand.Execute(paras);
            };
        }

        /// <summary>List column associated with this header click content control</summary>
        /// <value>The column.</value>
        public ListColumn Column { get; set; }

        /// <summary>Header click command</summary>
        /// <value>The header click command.</value>
        /// <remarks>This is usually populated by means of a binding</remarks>
        public ICommand HeaderClickCommand
        {
            get { return (ICommand) GetValue(HeaderClickCommandProperty); }
            set { SetValue(HeaderClickCommandProperty, value); }
        }

        /// <summary>Header click command</summary>
        /// <remarks>This is usually populated by means of a binding</remarks>
        public static readonly DependencyProperty HeaderClickCommandProperty = DependencyProperty.Register("HeaderClickCommand", typeof (ICommand), typeof (HeaderContentControl), new PropertyMetadata(null));
    }

    /// <summary>
    /// Arranges two elements left-to-right. Shrinks the left element if need be
    /// </summary>
    public class TextPlusGraphic : Panel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TextPlusGraphic"/> class.
        /// </summary>
        public TextPlusGraphic()
        {
            VerticalAlignment = VerticalAlignment.Stretch;
            HorizontalAlignment = HorizontalAlignment.Stretch;
        }

        /// <summary>
        /// When overridden in a derived class, measures the size in layout required for child elements and determines a size for the <see cref="T:System.Windows.FrameworkElement" />-derived class.
        /// </summary>
        /// <param name="availableSize">The available size that this element can give to child elements. Infinity can be specified as a value to indicate that the element will size to whatever content is available.</param>
        /// <returns>The size that this element determines it needs during layout, based on its calculations of child element sizes.</returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            if (Children.Count != 2)
                return base.MeasureOverride(availableSize);

            var text = Children[0];
            var graphic = Children[1];
            var large = new Size(100000d, availableSize.Height);
            text.Measure(large);
            graphic.Measure(large);
            var width = text.DesiredSize.Width + graphic.DesiredSize.Width;
            var height = Math.Min(Math.Max(text.DesiredSize.Height, graphic.DesiredSize.Height), availableSize.Height);

            if (width > availableSize.Width)
            {
                text.Measure(new Size(Math.Max(availableSize.Width - graphic.DesiredSize.Width, 0d), availableSize.Height));
                width = Math.Min(availableSize.Width, text.DesiredSize.Width + graphic.DesiredSize.Width);
            }

            base.MeasureOverride(availableSize);
            if (double.IsNaN(availableSize.Height) || double.IsNaN(availableSize.Width) || double.IsInfinity(availableSize.Height) || double.IsInfinity(availableSize.Width)) return new Size(width, height);
            return new Size(availableSize.Width, availableSize.Height);
        }

        /// <summary>
        /// When overridden in a derived class, positions child elements and determines a size for a <see cref="T:System.Windows.FrameworkElement" /> derived class.
        /// </summary>
        /// <param name="finalSize">The final area within the parent that this element should use to arrange itself and its children.</param>
        /// <returns>The actual size used.</returns>
        protected override Size ArrangeOverride(Size finalSize)
        {
            if (Children.Count != 2)
                return base.ArrangeOverride(finalSize);

            var text = Children[0];
            var graphic = Children[1];

            if (finalSize.Width >= text.DesiredSize.Width + graphic.DesiredSize.Width)
            {
                text.Arrange(new Rect(0d, 0d, text.DesiredSize.Width, finalSize.Height));
                graphic.Arrange(new Rect(text.DesiredSize.Width + 1d, 0d, graphic.DesiredSize.Width, finalSize.Height));
            }
            else if (finalSize.Width > graphic.DesiredSize.Width)
            {
                text.Arrange(new Rect(0d, 0d, finalSize.Width - graphic.DesiredSize.Width - 1, finalSize.Height));
                graphic.Arrange(new Rect(finalSize.Width - graphic.DesiredSize.Width, 0d, graphic.DesiredSize.Width, finalSize.Height));
            }
            else if (finalSize.Width <= graphic.DesiredSize.Width)
            {
                text.Arrange(new Rect(0d, 0d, 0d, finalSize.Height));
                graphic.Arrange(new Rect(0d, 0d, finalSize.Width, finalSize.Height));
            }

            return finalSize;
        }
    }
}
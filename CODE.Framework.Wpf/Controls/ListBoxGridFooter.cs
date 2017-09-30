using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace CODE.Framework.Wpf.Controls
{
    /// <summary>Generic footer control usable with Lists to create a data grid-style footer</summary>
    public class ListBoxGridFooter : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ListBoxGridFooter"/> class.
        /// </summary>
        public ListBoxGridFooter()
        {
            Visibility = Visibility.Collapsed;
            ClipToBounds = true;
            IsHitTestVisible = true;
            Background = Brushes.Transparent;
        }

        /// <summary>Generic column definition</summary>
        public ListColumnsCollection Columns
        {
            get { return (ListColumnsCollection)GetValue(ColumnsProperty); }
            set { SetValue(ColumnsProperty, value); }
        }

        /// <summary>Generic column definition</summary>
        public static readonly DependencyProperty ColumnsProperty = DependencyProperty.Register("Columns", typeof(ListColumnsCollection), typeof(ListBoxGridFooter), new UIPropertyMetadata(null, OnColumnsChanged));

        /// <summary>Called when columns change.</summary>
        /// <param name="o">The object the columns changed on.</param>
        /// <param name="args">The <see cref="System.Windows.DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void OnColumnsChanged(DependencyObject o, DependencyPropertyChangedEventArgs args)
        {
            var footer = o as ListBoxGridFooter;
            if (footer == null) return;
            footer.TriggerConsolidatedColumnRepopulate();
            var columns = args.NewValue as ListColumnsCollection;
            if (columns == null) return;
            columns.CollectionChangedDelayed -= footer.HandleColumnCollectionChanged;
            columns.CollectionChangedDelayed += footer.HandleColumnCollectionChanged;
            foreach (var column in columns)
            {
                column.VisibilityChanged -= footer.HandleColumnVisibilityChanged;
                column.VisibilityChanged += footer.HandleColumnVisibilityChanged;
            }
            columns.PropertyChangedPublic += (s, e) =>
            {
                if (e.PropertyName == "ShowFooters")
                    footer.TriggerConsolidatedColumnRepopulate();
            };
        }

        private void HandleColumnVisibilityChanged(object sender, EventArgs e)
        {
            TriggerConsolidatedColumnRepopulate();
        }

        private void HandleColumnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            RepopulateFooters(); // This handler is already triggered delayed, so we can go right ahead
        }

        private DispatcherTimer _delayTimer;

        private void TriggerConsolidatedColumnRepopulate()
        {
            // This timer fires with a delay of 25ms. This means that if this method gets called often on a tight loop, 
            // it will always reset the timer so it only fires once the last call happened and 25ms have gone by.
            if (_delayTimer == null)
                _delayTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(25), DispatcherPriority.Normal, (s, e) =>
                    {
                        _delayTimer.IsEnabled = false;
                        RepopulateFooters();
                    }, Application.Current.Dispatcher)
                    { IsEnabled = false };
            else
                _delayTimer.IsEnabled = false; // Resets the timer

            // Triggering the next timer run
            _delayTimer.IsEnabled = true;
        }

        /// <summary>Reference to the parent listbox this footer belongs to</summary>
        /// <value>The parent ListBox.</value>
        public ListBox ParentListBox
        {
            get { return (ListBox)GetValue(ParentListBoxProperty); }
            set { SetValue(ParentListBoxProperty, value); }
        }

        /// <summary>Reference to the parent listbox this footer belongs to</summary>
        /// <value>The parent ListBox.</value>
        public static readonly DependencyProperty ParentListBoxProperty = DependencyProperty.Register("ParentListBox", typeof(ListBox), typeof(ListBoxGridFooter), new PropertyMetadata(null, ParentListBoxChanged));

        /// <summary>Fires when the parent list box changes</summary>
        /// <param name="d">The d.</param>
        /// <param name="args">The <see cref="System.Windows.DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void ParentListBoxChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            var footer = d as ListBoxGridFooter;
            if (footer == null) return;
            if (args.NewValue == null) return;
            var listBox = args.NewValue as ListBox;
            if (listBox == null) return;

            footer.SetEditControlVisibility(ListEx.GetShowFooterEditControls(listBox));

            var pd = DependencyPropertyDescriptor.FromProperty(ListEx.ShowFooterEditControlsProperty, typeof(ListEx));
            pd.AddValueChanged(listBox, (s, e) => footer.SetEditControlVisibility(ListEx.GetShowFooterEditControls(listBox)));
        }

        private void SetEditControlVisibility(bool visible)
        {
            foreach (var grid in _footerContentGrids)
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

        /// <summary>Horizontal offset of the footer</summary>
        public double HorizontalFooterOffset
        {
            get { return (double)GetValue(HorizontalFooterOffsetProperty); }
            set { SetValue(HorizontalFooterOffsetProperty, value); }
        }

        /// <summary>Horizontal offset of the footer</summary>
        public static readonly DependencyProperty HorizontalFooterOffsetProperty = DependencyProperty.Register("HorizontalFooterOffset", typeof(double), typeof(ListBoxGridFooter), new UIPropertyMetadata(0d, HorizontalFooterOffsetChanged));

        /// <summary>Horizontals the footer offset changed.</summary>
        /// <param name="o">The object the property was changed on.</param>
        /// <param name="args">The <see cref="System.Windows.DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void HorizontalFooterOffsetChanged(DependencyObject o, DependencyPropertyChangedEventArgs args)
        {
            var footer = o as ListBoxGridFooter;
            if (footer == null || footer.Content == null) return;
            footer.InvalidateHorizontalFooterOffset();
        }

        /// <summary>
        /// Forces re-applying of the horizontal footer offset
        /// </summary>
        public void InvalidateHorizontalFooterOffset()
        {
            if (Content == null) return;
            var content = Content as Grid;
            if (content == null) return;
            content.Margin = new Thickness(HorizontalFooterOffset * -1, 0, 0, 0);
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

        private readonly List<Grid> _footerContentGrids = new List<Grid>();

        /// <summary>
        /// Repopulates the footers.
        /// </summary>
        private void RepopulateFooters()
        {
            if (Columns == null)
            {
                if (Visibility != Visibility.Collapsed)
                {
                    Visibility = Visibility.Collapsed;
                    ForceParentRefresh();
                }
                return;
            }

            if (!Columns.ShowFooters)
            {
                if (Visibility != Visibility.Collapsed)
                {
                    Visibility = Visibility.Collapsed;
                    ForceParentRefresh();
                }
                return;
            }

            if (Visibility != Visibility.Visible)
            {
                if (Visibility != Visibility.Visible)
                {
                    Visibility = Visibility.Visible;
                    ForceParentRefresh();
                }
            }

            if (Content != null)
            {
                var oldGrid = Content as Grid;
                if (oldGrid != null)
                {
                    foreach (var gridColumn in oldGrid.ColumnDefinitions)
                        BindingOperations.ClearBinding(gridColumn, ColumnDefinition.WidthProperty);
                    oldGrid.ColumnDefinitions.Clear();
                }
            }

            var grid = new Grid
            {
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Left,
                Width = 100000,
                ClipToBounds = true
            };

            _footerContentGrids.Clear();
            var columnCounter = -1;
            var starColumnFound = false;
            foreach (var column in Columns.Where(c => c.Visibility == Visibility.Visible))
            {
                columnCounter++;

                var gridColumn = new ColumnDefinition();
                BindingOperations.ClearBinding(gridColumn, ColumnDefinition.WidthProperty);
                var columnWidthBinding = new Binding("Width") {Source = column, Mode = BindingMode.TwoWay};
                gridColumn.SetBinding(ColumnDefinition.WidthProperty, columnWidthBinding);
                var descriptor = DependencyPropertyDescriptor.FromProperty(ColumnDefinition.WidthProperty, typeof(ColumnDefinition));
                if (descriptor != null)
                {
                    var localColumn = column;
                    descriptor.AddValueChanged(gridColumn, (s2, e2) => localColumn.SetActualWidth(gridColumn.ActualWidth));
                    grid.LayoutUpdated += (s3, e3) => { localColumn.SetActualWidth(gridColumn.ActualWidth); };
                    grid.SizeChanged += (s4, e4) => { localColumn.SetActualWidth(gridColumn.ActualWidth); };
                    SizeChanged += (s4, e4) => { localColumn.SetActualWidth(gridColumn.ActualWidth); };
                }
                grid.ColumnDefinitions.Add(gridColumn);
                if (column.Width.GridUnitType == GridUnitType.Star) starColumnFound = true;

                var content = new FooterContentControl { Column = column };

                if (!string.IsNullOrEmpty(column.FooterClickCommandBindingPath))
                {
                    var binding = new Binding(column.FooterClickCommandBindingPath);
                    content.SetBinding(FooterContentControl.FooterClickCommandProperty, binding);
                }

                var contentParent = new ContentControl { VerticalAlignment = VerticalAlignment.Stretch, HorizontalAlignment = HorizontalAlignment.Stretch };
                if (column.FooterEventCommands != null && column.FooterEventCommands.Count > 0)
                    Ex.SetEventCommands(contentParent, column.FooterEventCommands);

                if (column.FooterTemplate != null)
                {
                    var realContent = column.FooterTemplate.LoadContent();
                    var realContentElement = realContent as UIElement;
                    if (realContentElement != null)
                        contentParent.Content = realContentElement;
                }
                else
                {
                    var contentGrid = new Grid { HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };
                    _footerContentGrids.Add(contentGrid);
                    contentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1d, GridUnitType.Star) });
                    contentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1d, GridUnitType.Auto) });
                    if (column.ShowColumnFooterText)
                    {
                        var footerText = new TextBlock();
                        if (!string.IsNullOrEmpty(column.FooterBindingPath))
                            footerText.SetBinding(TextBlock.TextProperty, new Binding(column.FooterBindingPath));
                        else if (column.Footer != null)
                            footerText.SetBinding(TextBlock.TextProperty, new Binding("Footer") { Source = column });
                        else
                            column.Footer = " ";
                        if (column.FooterForeground != null)
                            footerText.SetBinding(TextBlock.ForegroundProperty, new Binding("FooterForeground") { Source = column });
                        else
                            footerText.SetBinding(TextBlock.ForegroundProperty, new Binding("Foreground") { RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(FooterContentControl), 1) });
                        Grid.SetRow(footerText, 1);
                        footerText.SetBinding(TextBlock.TextAlignmentProperty, new Binding("FooterTextAlignment") { Source = column });
                        contentGrid.Children.Add(footerText);
                    }
                    FrameworkElement footerEditControl = null;
                    if (column.ShowColumnFooterEditControl)
                    {
                        if (footerEditControl == null && column.ColumnFooterEditControlTemplate != null)
                            footerEditControl = column.ColumnFooterEditControlTemplate.LoadContent() as FrameworkElement;
                        if (footerEditControl == null)
                        {
                            footerEditControl = new TextBox();
                            if (!string.IsNullOrEmpty(column.ColumnFooterEditControlBindingPath))
                                footerEditControl.SetBinding(TextBox.TextProperty, new Binding(column.ColumnFooterEditControlBindingPath) { UpdateSourceTrigger = column.ColumnFooterEditControlUpdateTrigger });
                        }
                        if (column.ColumnFooterEditControlDataContext != null)
                            footerEditControl.DataContext = column.ColumnFooterEditControlDataContext;
                        if (!string.IsNullOrEmpty(column.ColumnFooterEditControlWatermarkText))
                            TextBoxEx.SetWatermarkText(footerEditControl, column.ColumnFooterEditControlWatermarkText);

                        contentGrid.Children.Add(footerEditControl);
                        column.UtilizedFooterEditControl = footerEditControl;
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
            }

            if (!starColumnFound)
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Need this column to properly support resizing
            else
            {
                grid.HorizontalAlignment = HorizontalAlignment.Stretch;
                grid.Width = double.NaN;
            }
            Content = grid;

            InvalidateHorizontalFooterOffset();

            if (ParentListBox != null)
                SetEditControlVisibility(ListEx.GetShowFooterEditControls(ParentListBox));
        }
    }

    /// <summary>
    /// Special footer control for comboboxes (similar to the list footer, but measures itself differently)
    /// </summary>
    /// <seealso cref="CODE.Framework.Wpf.Controls.ListBoxGridFooter" />
    public class ComboBoxGridFooter : ListBoxGridFooter
    {
        private double GetActualColumnWidth()
        {
            if (Columns == null || Columns.Count < 1) return 0d;

            var totalPixelWidth = 0d;
            for (var columnCounter = 0; columnCounter < Columns.Count; columnCounter++)
                if (Columns[columnCounter].Visibility == Visibility.Visible)
                    if (Columns[columnCounter].Width.GridUnitType == GridUnitType.Pixel)
                        totalPixelWidth += Columns[columnCounter].Width.Value;
                    else
                        // We treat autos as if they were stars
                        throw new NotSupportedException("Only pixek-width columns are supported in drop-downs.");

            return totalPixelWidth;
        }

        /// <summary>
        /// When overridden in a derived class, measures the size in layout required for child elements and determines a size for the <see cref="T:System.Windows.FrameworkElement" />-derived class.
        /// </summary>
        /// <param name="availableSize">The available size that this element can give to child elements. Infinity can be specified as a value to indicate that the element will size to whatever content is available.</param>
        /// <returns>The size that this element determines it needs during layout, based on its calculations of child element sizes.</returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            var baseSize = base.MeasureOverride(availableSize);
            return new Size(GetActualColumnWidth(), baseSize.Height);
        }
    }

    /// <summary>For internal use only</summary>
    public class FooterContentControl : ContentControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FooterContentControl"/> class.
        /// </summary>
        public FooterContentControl()
        {
            IsHitTestVisible = true;
            Background = Brushes.Transparent;

            MouseLeftButtonDown += (s, e) =>
            {
                Mouse.Capture(this);
                //e.Handled = true;
            };

            MouseLeftButtonUp += (s, e) =>
            {
                if (Equals(Mouse.Captured, this)) Mouse.Capture(null);
                var position = Mouse.GetPosition(this);
                if (position.X < 0 || position.Y < 0 || position.X > ActualWidth || position.Y > ActualHeight) return;
                var localContent = s as FooterContentControl;
                if (localContent == null) return;
                if (e.ClickCount != 1) return;

                if (FooterClickCommand == null) return;
                var paras = new FooterClickCommandParameters { Column = localContent.Column };
                if (FooterClickCommand.CanExecute(paras))
                    FooterClickCommand.Execute(paras);
            };
        }

        /// <summary>List column associated with this footer click content control</summary>
        /// <value>The column.</value>
        public ListColumn Column { get; set; }

        /// <summary>Footer click command</summary>
        /// <value>The footer click command.</value>
        /// <remarks>This is usually populated by means of a binding</remarks>
        public ICommand FooterClickCommand
        {
            get { return (ICommand)GetValue(FooterClickCommandProperty); }
            set { SetValue(FooterClickCommandProperty, value); }
        }

        /// <summary>Footer click command</summary>
        /// <remarks>This is usually populated by means of a binding</remarks>
        public static readonly DependencyProperty FooterClickCommandProperty = DependencyProperty.Register("FooterClickCommand", typeof(ICommand), typeof(FooterContentControl), new PropertyMetadata(null));

        /// <summary>
        /// Indicates whether the column is currently filtered
        /// </summary>
        public bool ColumnIsFiltered
        {
            get { return (bool)GetValue(ColumnIsFilteredProperty); }
            set { SetValue(ColumnIsFilteredProperty, value); }
        }

        /// <summary>
        /// Indicates whether the column is currently filtered
        /// </summary>
        public static readonly DependencyProperty ColumnIsFilteredProperty = DependencyProperty.Register("ColumnIsFiltered", typeof(bool), typeof(FooterContentControl), new PropertyMetadata(false));
    }

    /// <summary>Parameters passed to the footer click command</summary>
    public class FooterClickCommandParameters
    {
        /// <summary>Reference to the clicked column</summary>
        public ListColumn Column { get; set; }
    }
}
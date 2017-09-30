using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using CODE.Framework.Wpf.Utilities;

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
            MouseLeftButtonDown += HandleMouseLeftButtonDown;
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
            header.TriggerConsolidatedColumnRepopulate();
            var columns = args.NewValue as ListColumnsCollection;
            if (columns == null) return;
            columns.CollectionChangedDelayed -= header.HandleColumnCollectionChanged;
            columns.CollectionChangedDelayed += header.HandleColumnCollectionChanged;
            foreach (var column in columns)
            {
                column.VisibilityChanged -= header.HandleColumnVisibilityChanged;
                column.VisibilityChanged += header.HandleColumnVisibilityChanged;
            }
            columns.PropertyChangedPublic += (s, e) =>
            {
                if (e.PropertyName == "ShowHeaders")
                    header.TriggerConsolidatedColumnRepopulate();
            };
        }

        private void HandleColumnVisibilityChanged(object sender, EventArgs e)
        {
            TriggerConsolidatedColumnRepopulate();
        }

        private void HandleColumnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            RepopulateHeaders(); // This handler is already triggered delayed, so we can go right ahead
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
                        RepopulateHeaders();
                    }, Application.Current.Dispatcher)
                    {IsEnabled = false};
            else
                _delayTimer.IsEnabled = false; // Resets the timer

            // Triggering the next timer run
            _delayTimer.IsEnabled = true;
        }

        /// <summary>Reference to the parent listbox this header belongs to</summary>
        /// <value>The parent ListBox.</value>
        public ListBox ParentListBox
        {
            get { return (ListBox) GetValue(ParentListBoxProperty); }
            set { SetValue(ParentListBoxProperty, value); }
        }

        /// <summary>Reference to the parent listbox this header belongs to</summary>
        /// <value>The parent ListBox.</value>
        public static readonly DependencyProperty ParentListBoxProperty = DependencyProperty.Register("ParentListBox", typeof (ListBox), typeof (ListBoxGridHeader), new PropertyMetadata(null, ParentListBoxChanged));

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
            content.Margin = new Thickness(HorizontalHeaderOffset*-1, 0, 0, 0);
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
        private Point _mouseDownPosition;
        private bool _inHeaderDragMode;
        private DragHeaderAdorner _dragHeaderAdorner;
        private double _startDragHeaderLeft;
        private int _startDragColumnIndex;
        private double _startDragMouseOffsetWithinColumn;
        private bool _inValidDragMouseDown;

        /// <summary>
        /// Repopulates the headers.
        /// </summary>
        private void RepopulateHeaders()
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

            // We handle header visibility through a 0 column height, since the existence of headers is important for the operation of the list
            Height = !Columns.ShowHeaders ? 0 : double.NaN;

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

            _headerContentGrids.Clear();
            var columnCounter = -1;
            var starColumnFound = false;
            foreach (var column in Columns.Where(c => c.Visibility == Visibility.Visible))
            {
                columnCounter++;

                var gridColumn = new ColumnDefinition();
                BindingOperations.ClearBinding(gridColumn, ColumnDefinition.WidthProperty);
                gridColumn.SetBinding(ColumnDefinition.WidthProperty, new Binding("Width") {Source = column, Mode = BindingMode.TwoWay});
                var descriptor = DependencyPropertyDescriptor.FromProperty(ColumnDefinition.WidthProperty, typeof (ColumnDefinition));
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

                var content = new HeaderContentControl {Column = column};
                content.MouseLeftButtonDown += HandleMouseLeftButtonDown;

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
                    {
                        contentParent.Content = realContentElement;
                        realContentElement.MouseLeftButtonDown += HandleMouseLeftButtonDown;
                    }
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
                        textPlusGraphic.MouseLeftButtonDown += HandleMouseLeftButtonDown;
                        var headerText = new TextBlock();
                        headerText.MouseLeftButtonDown += HandleMouseLeftButtonDown;
                        textPlusGraphic.Children.Add(headerText);
                        if (column.Header != null)
                            headerText.Text = column.Header.ToString();
                        else
                            column.Header = " ";
                        if (column.HeaderForeground != null)
                            headerText.SetBinding(TextBlock.ForegroundProperty, new Binding("HeaderForeground") {Source = column});
                        else
                            headerText.SetBinding(TextBlock.ForegroundProperty, new Binding("Foreground") {RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof (HeaderContentControl), 1)});
                        var sort = new SortOrderIndicator();
                        if (!string.IsNullOrEmpty(column.SortOrderBindingPath))
                            sort.SetBinding(SortOrderIndicator.OrderProperty, new Binding(column.SortOrderBindingPath));
                        else
                            sort.SetBinding(SortOrderIndicator.OrderProperty, new Binding("SortOrder") {Source = column});
                        textPlusGraphic.Children.Add(sort);
                        Grid.SetRow(textPlusGraphic, 1);
                        headerText.SetBinding(TextBlock.TextAlignmentProperty, new Binding("HeaderTextAlignment") {Source = column});
                        contentGrid.Children.Add(textPlusGraphic);
                    }
                    FrameworkElement headerEditControl = null;
                    if (column.AutoFilter)
                    {
                        if (!column.ShowColumnHeaderEditControl) column.ShowColumnHeaderEditControl = true;
                        if (string.IsNullOrEmpty(column.ColumnHeaderEditControlWatermarkText)) column.ColumnHeaderEditControlWatermarkText = "Filter " + column.Header;
                        if (column.ColumnHeaderEditControlTemplate == null)
                            headerEditControl = new FilterHeaderTextBox(column);
                    }
                    if (column.ShowColumnHeaderEditControl)
                    {
                        if (headerEditControl == null && column.ColumnHeaderEditControlTemplate != null)
                            headerEditControl = column.ColumnHeaderEditControlTemplate.LoadContent() as FrameworkElement;
                        if (headerEditControl == null)
                        {
                            headerEditControl = new TextBox();
                            if (!string.IsNullOrEmpty(column.ColumnHeaderEditControlBindingPath))
                                headerEditControl.SetBinding(TextBox.TextProperty, new Binding(column.ColumnHeaderEditControlBindingPath) {UpdateSourceTrigger = column.ColumnHeaderEditControlUpdateTrigger});
                        }
                        else
                            headerEditControl.MouseLeftButtonDown += HandleMouseLeftButtonDown;
                        if (column.ColumnHeaderEditControlDataContext != null)
                            headerEditControl.DataContext = column.ColumnHeaderEditControlDataContext;
                        if (!string.IsNullOrEmpty(column.ColumnHeaderEditControlWatermarkText))
                            TextBoxEx.SetWatermarkText(headerEditControl, column.ColumnHeaderEditControlWatermarkText);

                        contentGrid.Children.Add(headerEditControl);
                        column.UtilizedHeaderEditControl = headerEditControl;
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
                        Background = Brushes.Transparent,
                        SnapsToDevicePixels = true
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

            InvalidateHorizontalHeaderOffset();

            if (ParentListBox != null)
                SetEditControlVisibility(ListEx.GetShowHeaderEditControls(ParentListBox));
        }

        /// <summary>
        /// Handles the <see cref="E:MouseLeftButtonDown" /> event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="MouseButtonEventArgs" /> instance containing the event data.</param>
        private void HandleMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (Equals(e.Source, this) || e.Source is Label || e.Source is TextBlock || e.Source is ContentControl || e.Source is Grid)
            {
                var clickPosition = e.GetPosition(this);
                _mouseDownPosition = new Point(clickPosition.X + HorizontalHeaderOffset, clickPosition.Y);
                _inValidDragMouseDown = true;
            }
            else
                _inValidDragMouseDown = false;
        }

        /// <summary>
        /// Handles the <see cref="E:MouseMove" /> event.
        /// </summary>
        /// <param name="e">The <see cref="MouseEventArgs"/> instance containing the event data.</param>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (!_inHeaderDragMode && e.LeftButton == MouseButtonState.Pressed && Columns.AllowColumnMove && _inValidDragMouseDown)
            {
                var dragPosition = e.GetPosition(this);
                dragPosition = new Point(dragPosition.X + HorizontalHeaderOffset, dragPosition.Y);
                var deltaX = Math.Abs(_mouseDownPosition.X - dragPosition.X);
                if (deltaX > SystemParameters.MinimumHorizontalDragDistance)
                {
                    // We are ready to start a drag operation since the user has moved far enough with the mouse button pressed
                    var adornerLayer = AdornerLayer.GetAdornerLayer(this);
                    if (adornerLayer != null)
                    {
                        var dragColumnIndex = GetColumnIndexAtHorizontalPosition(_mouseDownPosition.X);
                        if (dragColumnIndex > -1)
                        {
                            _startDragMouseOffsetWithinColumn = GetHorizontalOffsetFromLeftColumnEdge(dragColumnIndex, _mouseDownPosition.X);
                            var draggedColumn = Columns[dragColumnIndex];
                            var grid = Content as Grid;
                            if (grid != null)
                            {
                                var dragColumnElement = grid.Children.OfType<HeaderContentControl>().FirstOrDefault(c => Equals(c.Column, draggedColumn));
                                if (dragColumnElement != null)
                                {
                                    _dragHeaderAdorner = new DragHeaderAdorner(this, new Rectangle
                                    {
                                        Height = dragColumnElement.ActualHeight + dragColumnElement.Margin.Top + dragColumnElement.Margin.Bottom,
                                        Width = dragColumnElement.ActualWidth + dragColumnElement.Margin.Left + dragColumnElement.Margin.Right,
                                        Fill = new VisualBrush(dragColumnElement), Opacity = .5
                                    }, Columns, dragColumnIndex);
                                    adornerLayer.Add(_dragHeaderAdorner);
                                    _inHeaderDragMode = true;
                                    _startDragHeaderLeft = GetLeftEdgePosition(dragColumnIndex);
                                    _startDragColumnIndex = dragColumnIndex;
                                    Mouse.Capture(this);
                                }
                            }
                        }
                    }
                }
            }

            if (_inHeaderDragMode && _dragHeaderAdorner != null)
            {
                var deltaX = _mouseDownPosition.X - (e.GetPosition(this).X + HorizontalHeaderOffset);
                var visualLeft = _startDragHeaderLeft - deltaX;
                _dragHeaderAdorner.SetDragVisualLeft(visualLeft, _startDragMouseOffsetWithinColumn);
            }

            if (!_inHeaderDragMode)
                base.OnMouseMove(e);
            else
                e.Handled = true;
        }

        /// <summary>
        /// Handles the <see cref="E:MouseLeftButtonUp" /> event.
        /// </summary>
        /// <param name="e">The <see cref="MouseButtonEventArgs"/> instance containing the event data.</param>
        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            _inValidDragMouseDown = false;
            if (_inHeaderDragMode)
            {
                _inHeaderDragMode = false;
                Mouse.Capture(null);

                if (_dragHeaderAdorner != null)
                {
                    var dragDestinationIndex = _dragHeaderAdorner.LastCalculatedDropIndex;
                    if (dragDestinationIndex > _startDragColumnIndex) dragDestinationIndex--; // Since the current one is removed and inserted after, the index shifts by one if it was dragged to the right
                    if (dragDestinationIndex > -1)
                    {
                        var draggedColumn = Columns[_startDragColumnIndex];
                        Columns.Remove(draggedColumn);
                        Columns.Insert(dragDestinationIndex, draggedColumn);
                    }

                    var adornerLayer = AdornerLayer.GetAdornerLayer(this);
                    if (adornerLayer != null)
                    {
                        adornerLayer.Remove(_dragHeaderAdorner);
                        _dragHeaderAdorner = null;
                    }
                }
            }
            else
                base.OnMouseLeftButtonUp(e);
        }

        /// <summary>
        /// Gets the index of the column the specified horizontal (x) position falls in
        /// </summary>
        /// <param name="x">The x position within the control.</param>
        /// <remarks>When the control is scrolled to the right, the X position is still absolute within the control, so we do not have to consider the offset.</remarks>
        /// <returns>System.Int32.</returns>
        private int GetColumnIndexAtHorizontalPosition(double x)
        {
            var index = -1;
            var currentLeft = 0d;
            foreach (var column in Columns)
            {
                index++;
                if (column.ActualWidth + currentLeft > x)
                    return index;
                currentLeft += column.ActualWidth;
            }
            return index;
        }

        private double GetHorizontalOffsetFromLeftColumnEdge(int columnIndex, double x)
        {
            var columnLeftEdge = GetLeftEdgePosition(columnIndex);
            return x - columnLeftEdge;
        }

        /// <summary>
        /// Returns all the positions of column separators (the 'edges' of the columns)
        /// </summary>
        /// <returns>System.Double[].</returns>
        public double[] GetColumnSeparatorPositions()
        {
            var edgePositions = new double[Columns.Count + 1];
            for (var edgeCounter = 0; edgeCounter < edgePositions.Length; edgeCounter++)
                edgePositions[edgeCounter] = GetLeftEdgePosition(edgeCounter);
            return edgePositions;
        }

        /// <summary>
        /// Gets the left edge position of the specified column.
        /// </summary>
        /// <param name="columnIndex">The column index.</param>
        /// <returns>System.Double.</returns>
        public double GetLeftEdgePosition(int columnIndex)
        {
            var left = HorizontalHeaderOffset * -1;
            for (var counter = 0; counter < columnIndex; counter++)
                left += Columns[counter].ActualWidth;
            return left;
        }
    }

    /// <summary>
    /// Header class very similar to the listbox one, but it measures itself slightly different.
    /// </summary>
    /// <seealso cref="CODE.Framework.Wpf.Controls.ListBoxGridHeader" />
    public class ComboBoxGridHeader : ListBoxGridHeader
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

    /// <summary>For internal use only (implements header filtering in listboxes)</summary>
    public class FilterHeaderTextBox : TextBox
    {
        /// <summary>
        /// Column the textbox is associated with
        /// </summary>
        private readonly ListColumn _column;

        /// <summary>
        /// Internal dispatch timer
        /// </summary>
        private readonly DispatcherTimer _timer;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="column">Column this textbox is associated with</param>
        public FilterHeaderTextBox(ListColumn column)
        {
            _column = column;

            _timer = new DispatcherTimer(TimeSpan.FromMilliseconds(250), DispatcherPriority.Normal, (s, e) =>
            {
                _timer.IsEnabled = false;
                ListEx.SetInAutoFiltering(AssociatedList, true);
                ExecuteFilter();
                ListEx.SetInAutoFiltering(AssociatedList, false);
            }, Dispatcher) {IsEnabled = false};

            Loaded += (s, e) =>
            {
                var list = AssociatedList;
                if (list == null) return;
                list.DataContextChanged += (s2, e2) =>
                {
                    if (list.ItemsSource == null)
                    {
                        SetUnfilteredItemsSource(list, null);
                        return;
                    }
                    var unfilteredSource = list.ItemsSource;
                    var originalSource = unfilteredSource as dynamic;
                    SetUnfilteredItemsSource(list, Enumerable.ToList(originalSource));
                    HookDataSourceChanged(list);
                };

                HookDataSourceChanged(list);
                SetBinding(TextProperty, new Binding("FilterText") { Source = column, Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
            };

            MouseLeftButtonUp += (s, e) => { e.Handled = true; };
        }

        private void HookDataSourceChanged(ListBox list)
        {
            if (list.ItemsSource == null) return;
            var listType = list.ItemsSource.GetType();
            var eventInfo = listType.GetEvent("CollectionChanged");
            if (eventInfo != null)
                eventInfo.AddEventHandler(list.ItemsSource, Delegate.CreateDelegate(eventInfo.EventHandlerType, this, typeof (FilterHeaderTextBox).GetMethod(nameof(OriginalCollectionChangedHandler), BindingFlags.NonPublic | BindingFlags.Instance)));
        }

        private void OriginalCollectionChangedHandler(object sender, NotifyCollectionChangedEventArgs args)
        {
            var list = AssociatedList;
            if (list == null) return;
            if (ListEx.GetInAutoFiltering(list) || ListEx.GetInAutoSorting(list)) return; // The change is caused internally, so we ignore it

            var unfilteredSource = list.ItemsSource;
            var originalSource = unfilteredSource as dynamic;
            SetUnfilteredItemsSource(list, Enumerable.ToList(originalSource));
            WipeWithoutRefresh();
        }

        private void AssignItemsAsListItemSource(ItemsControl itemsControl, IEnumerable items)
        {
            if (itemsControl == null || itemsControl.ItemsSource == null) return;
            var listTypeName = itemsControl.ItemsSource.GetType().Name;

            if (listTypeName.StartsWith("ObservableCollection`1"))
            {
                // Since this is an observable collection, we can optimize change notification
                var dynamicSource = itemsControl.ItemsSource as dynamic;
                dynamicSource.Clear();

                var itemsCollection = itemsControl.ItemsSource.GetType().GetProperty("Items", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(dynamicSource, null);
                if (itemsCollection != null)
                    foreach (var item in items)
                        itemsCollection.Add((dynamic) item);

                var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
                GetCollectionChangedMethod(itemsControl.ItemsSource).Invoke(dynamicSource, new object[] {args});
                return;
            }
            if (listTypeName.StartsWith("List`1"))
            {
                var dynamicSource = itemsControl.ItemsSource as dynamic;
                dynamicSource.Clear();
                foreach (dynamic item in items)
                    dynamicSource.Add(item);
            }
        }

        private static readonly Dictionary<Type, MethodInfo> OnCollectionChangedMethodCollections = new Dictionary<Type, MethodInfo>();

        /// <summary>Returns collection changed method for a specific item in a cached fashion</summary>
        /// <param name="collection">The collection object on which to get the change method</param>
        /// <returns>MethodInfo.</returns>
        private static MethodInfo GetCollectionChangedMethod(object collection)
        {
            var collectionType = collection.GetType();
            if (OnCollectionChangedMethodCollections.ContainsKey(collectionType))
                return OnCollectionChangedMethodCollections[collectionType];

            var methods = collectionType.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic).Where(m => (m.Name == "OnCollectionChanged") && (m.Attributes & MethodAttributes.Virtual) == MethodAttributes.Virtual);
            var method = methods.FirstOrDefault();
            if (method != null)
            {
                OnCollectionChangedMethodCollections.Add(collectionType, method);
                return method;
            }
            throw new ArgumentNullException("OnCollectionChanged method not found on observable collection!");
        }

        /// <summary>
        /// Performs the actual filtering
        /// </summary>
        protected virtual void ExecuteFilter()
        {
            var list = AssociatedList;
            if (list == null) return;

            var filterMode = ListEx.GetAutoFilterMode(list);

            if (string.IsNullOrEmpty(Text) && filterMode == AutoFilterMode.OneColumnAtATime)
            {
                // Since we are only filtering based on this column, and since this column is empty, we get the original source back
                if (_ignoreTextChange) return;
                var unfilteredSource2 = GetUnfilteredItemsSource(list);
                if (unfilteredSource2 != null)
                    AssignItemsAsListItemSource(list, unfilteredSource2);
                return;
            }

            // Clearing out all the other filters on the current list if that is the mode we are in
            var columns = ListEx.GetColumns(list);
            if (columns != null)
            {
                if (filterMode == AutoFilterMode.OneColumnAtATime)
                    // Since we only want to filter one list at a time, we clear out the other lists
                    foreach (var column in columns)
                        if (column.UtilizedHeaderEditControl != null && column.UtilizedHeaderEditControl != this && column.UtilizedHeaderEditControl is FilterHeaderTextBox)
                            ((FilterHeaderTextBox) column.UtilizedHeaderEditControl).WipeWithoutRefresh();
            }

            // We need the original source, so we can perform the filter
            var unfilteredSource = GetUnfilteredItemsSource(list);
            if (unfilteredSource == null)
            {
                // We do not have the original source yet, so we retrieve it and store it away for future (fast) use
                unfilteredSource = list.ItemsSource;
                SetUnfilteredItemsSource(list, Enumerable.ToList((dynamic) unfilteredSource));
            }
            if (unfilteredSource == null) return; // We do not have an unfiltered source, so there isn't anything we can filter

            var sourceList = unfilteredSource.Cast<object>().ToList();
            switch (filterMode)
            {
                case AutoFilterMode.OneColumnAtATime:
                    PropertyInfo property = null;
                    var filteredSource = sourceList.Where(i =>
                    {
                        if (property == null) property = i.GetType().GetProperty(_column.BindingPath);
                        var propertyValue = property.GetValue(i, null).ToString().ToLower();

                        switch (_column.FilterMode)
                        {
                            case FilterMode.ContainedString:
                                return propertyValue.Contains(Text.ToLower());
                            case FilterMode.StartsWithString:
                                return propertyValue.StartsWith(Text.ToLower());
                            case FilterMode.ExactMatchString:
                                return propertyValue.Equals(Text.ToLower());
                            case FilterMode.Number:
                                var localText = Text.Trim();
                                var comparison = GetComparisonOperator(ref localText);
                                decimal decimalFilterValue;
                                decimal decimalPropertyValue;
                                if (decimal.TryParse(localText, out decimalFilterValue) && decimal.TryParse(propertyValue, out decimalPropertyValue))
                                    return CompareNumbers(comparison, decimalPropertyValue, decimalFilterValue);
                                break;
                        }
                        return false;
                    });
                    AssignItemsAsListItemSource(list, filteredSource);
                    break;

                case AutoFilterMode.AllColumnsAnd:
                    var filterOptions = GetAutoFilterValues(columns);
                    if (filterOptions.Keys.Count < 1)
                    {
                        // Nothing to filter, so we are back to showing everything
                        var unfilteredSource2 = GetUnfilteredItemsSource(list);
                        if (unfilteredSource2 != null)
                            AssignItemsAsListItemSource(list, unfilteredSource2);
                    }
                    else
                    {
                        var filteredSource2 = sourceList.Where(i =>
                        {
                            var include = true;
                            foreach (var key in filterOptions.Keys)
                            {
                                var option = filterOptions[key];
                                if (option.PropertyInfo == null) option.PropertyInfo = i.GetType().GetProperty(key);
                                var propertyValue = option.PropertyInfo.GetValue(i, null).ToString().ToLower();
                                switch (option.FilterMode)
                                {
                                    case FilterMode.ContainedString:
                                        if (!propertyValue.Contains(option.Text))
                                            include = false;
                                        break;
                                    case FilterMode.StartsWithString:
                                        if (!propertyValue.StartsWith(option.Text))
                                            include = false;
                                        break;
                                    case FilterMode.ExactMatchString:
                                        if (!propertyValue.Equals(option.Text))
                                            include = false;
                                        break;
                                    case FilterMode.Number:
                                        var localText = option.Text.Trim();
                                        var comparison = GetComparisonOperator(ref localText);
                                        decimal decimalFilterValue;
                                        decimal decimalPropertyValue;
                                        if (decimal.TryParse(localText, out decimalFilterValue) && decimal.TryParse(propertyValue, out decimalPropertyValue))
                                            if (!CompareNumbers(comparison, decimalPropertyValue, decimalFilterValue))
                                                include = false;
                                        break;
                                }
                            }
                            return include;
                        });
                        AssignItemsAsListItemSource(list, filteredSource2);
                    }
                    break;

                case AutoFilterMode.AllColumnsOr:
                    var filterOptions2 = GetAutoFilterValues(columns);
                    if (filterOptions2.Keys.Count < 1)
                    {
                        // Nothing to filter, so we are back to showing everything
                        var unfilteredSource2 = GetUnfilteredItemsSource(list);
                        if (unfilteredSource2 != null)
                            AssignItemsAsListItemSource(list, unfilteredSource2);
                    }
                    else
                    {
                        var filteredSource2 = sourceList.Where(i =>
                        {
                            foreach (var key in filterOptions2.Keys)
                            {
                                var option = filterOptions2[key];
                                if (option.PropertyInfo == null) option.PropertyInfo = i.GetType().GetProperty(key);
                                var propertyValue = option.PropertyInfo.GetValue(i, null).ToString().ToLower();
                                switch (option.FilterMode)
                                {
                                    case FilterMode.ContainedString:
                                        if (!propertyValue.Contains(option.Text))
                                            return true;
                                        break;
                                    case FilterMode.StartsWithString:
                                        if (!propertyValue.StartsWith(option.Text))
                                            return true;
                                        break;
                                    case FilterMode.ExactMatchString:
                                        if (!propertyValue.Equals(option.Text))
                                            return true;
                                        break;
                                    case FilterMode.Number:
                                        var localText = option.Text.Trim();
                                        var comparison = GetComparisonOperator(ref localText);
                                        decimal decimalFilterValue;
                                        var decimalPropertyValue = 0m;
                                        if (decimal.TryParse(localText, out decimalFilterValue) && decimal.TryParse(propertyValue, out decimalPropertyValue))
                                            if (!CompareNumbers(comparison, decimalPropertyValue, decimalFilterValue))
                                                return true;
                                        break;
                                }
                            }
                            return false;
                        });
                        AssignItemsAsListItemSource(list, filteredSource2);
                    }
                    break;
            }
        }

        private static ComparisonOperators GetComparisonOperator(ref string localText)
        {
            var comparison = ComparisonOperators.Equals;
            if (localText.StartsWith(">="))
            {
                comparison = ComparisonOperators.GreaterThanOrEqual;
                localText = localText.Substring(2);
            }
            else if (localText.StartsWith(">"))
            {
                comparison = ComparisonOperators.GreaterThan;
                localText = localText.Substring(1);
            }
            else if (localText.StartsWith("<="))
            {
                comparison = ComparisonOperators.LessThanOrEqual;
                localText = localText.Substring(2);
            }
            else if (localText.StartsWith("<"))
            {
                comparison = ComparisonOperators.LessThan;
                localText = localText.Substring(1);
            }
            return comparison;
        }

        private static bool CompareNumbers(ComparisonOperators comparison, decimal value1, decimal value)
        {
            switch (comparison)
            {
                case ComparisonOperators.Equals:
                    return value1 == value;
                case ComparisonOperators.GreaterThan:
                    return value1 > value;
                case ComparisonOperators.GreaterThanOrEqual:
                    return value1 >= value;
                case ComparisonOperators.LessThan:
                    return value1 < value;
                case ComparisonOperators.LessThanOrEqual:
                    return value1 <= value;
            }
            return false;
        }

        private enum ComparisonOperators
        {
            Equals,
            GreaterThan,
            GreaterThanOrEqual,
            LessThan,
            LessThanOrEqual
        }

        private class FilterInformation
        {
            public string Text { get; set; }
            public PropertyInfo PropertyInfo { get; set; }

            public FilterMode FilterMode { get; set; }
        }

        private Dictionary<string, FilterInformation> GetAutoFilterValues(ListColumnsCollection columns)
        {
            var result = new Dictionary<string, FilterInformation>();

            foreach (var column in columns)
                if (column.UtilizedHeaderEditControl != null && column.UtilizedHeaderEditControl is FilterHeaderTextBox)
                    if (!result.Keys.Contains(column.BindingPath))
                    {
                        var text = (FilterHeaderTextBox) column.UtilizedHeaderEditControl;
                        if (!string.IsNullOrEmpty(text.Text))
                            result.Add(column.BindingPath, new FilterInformation {Text = text.Text.Trim().ToLower(), FilterMode = column.FilterMode});
                    }

            return result;
        }

        private bool _ignoreTextChange;

        private void WipeWithoutRefresh()
        {
            _ignoreTextChange = true;
            Text = string.Empty;
            _ignoreTextChange = false;
        }

        private ListBox _associatedList;

        /// <summary>
        /// For internal use only
        /// </summary>
        private ListBox AssociatedList
        {
            get
            {
                if (_associatedList == null)
                    _associatedList = ElementHelper.FindVisualTreeParent<ListBox>(this);
                return _associatedList;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnTextChanged(TextChangedEventArgs e)
        {
            SetHeaderFilterMode(!string.IsNullOrEmpty(Text));
            if (_ignoreTextChange) return;
            _timer.IsEnabled = false; // Resetting the timespan before filtering kicks in
            _timer.IsEnabled = true; // Triggering the search
        }

        private HeaderContentControl _headerContent;

        private void SetHeaderFilterMode(bool isFiltered)
        {
            if (_headerContent == null)
                _headerContent = ElementHelper.FindVisualTreeParent<HeaderContentControl>(this);
            if (_headerContent != null)
                _headerContent.ColumnIsFiltered = isFiltered;
        }

        /// <summary>For internal use (stores the internal, unfiltered data source)</summary>
        public static readonly DependencyProperty UnfilteredItemsSourceProperty = DependencyProperty.RegisterAttached("UnfilteredItemsSource", typeof (IEnumerable), typeof (FilterHeaderTextBox), new PropertyMetadata(null));

        /// <summary>For internal use (stores the internal, unfiltered data source)</summary>
        public static IEnumerable GetUnfilteredItemsSource(DependencyObject d)
        {
            return (IEnumerable) d.GetValue(UnfilteredItemsSourceProperty);
        }

        /// <summary>For internal use (stores the internal, unfiltered data source)</summary>
        public static void SetUnfilteredItemsSource(DependencyObject d, IEnumerable value)
        {
            d.SetValue(UnfilteredItemsSourceProperty, value);
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
                //e.Handled = true;
            };

            MouseLeftButtonUp += (s, e) =>
            {
                if (Equals(Mouse.Captured, this)) Mouse.Capture(null);
                var position = Mouse.GetPosition(this);
                if (position.X < 0 || position.Y < 0 || position.X > ActualWidth || position.Y > ActualHeight) return;
                var localContent = s as HeaderContentControl;
                if (localContent == null) return;
                if (e.ClickCount != 1) return;

                if (HeaderClickCommand == null && Column.AutoSort)
                    if (SortByColumn())
                        return;

                if (HeaderClickCommand == null) return;
                var paras = new HeaderClickCommandParameters {Column = localContent.Column};
                if (HeaderClickCommand.CanExecute(paras))
                    HeaderClickCommand.Execute(paras);
            };

            Loaded += (s, e) =>
            {
                if (Column == null || !Column.AutoSort) return;
                var list = ElementHelper.FindVisualTreeParent<ListBox>(this);
                if (list == null) return;
                list.DataContextChanged += (s2, e2) => { if (!_inResort && Column.SortOrder != SortOrder.Unsorted) SortByColumn(false); };
                if (list.ItemsSource == null) return;

                var itemSourceType = list.ItemsSource.GetType();
                var changedEvent = itemSourceType.GetEvent("CollectionChanged");
                if (changedEvent != null)
                    changedEvent.AddEventHandler(list.ItemsSource, new NotifyCollectionChangedEventHandler((s3, e3) =>
                    {
                        if (_inResort || Column.SortOrder == SortOrder.Unsorted) return;
                        if (Application.Current == null) return;
                        if (Application.Current.Dispatcher == null) return;
                        // Delaying until the collection changed is complete
                        Application.Current.Dispatcher.BeginInvoke(new Action(() => { SortByColumn(false); }), DispatcherPriority.Normal);
                    }));
            };
        }

        /// <summary>
        /// Performs sorting the column
        /// </summary>
        /// <param name="toggleSort">Indicates whether we wan to toggle to the next order, or use the current one (false)</param>
        /// <returns></returns>
        protected virtual bool SortByColumn(bool toggleSort = true)
        {
            if (Column == null) return false;
            var list = ElementHelper.FindVisualTreeParent<ListBox>(this);
            if (list == null) return false;

            if (list.ItemsSource == null) return false;

            var source = list.ItemsSource.Cast<object>().ToList();

            var newSortOrder = Column.SortOrder;
            if (toggleSort) newSortOrder = Column.SortOrder != SortOrder.Ascending ? SortOrder.Ascending : SortOrder.Descending;
            else if (Column.SortOrder == SortOrder.Unsorted) return true;

            ListEx.SetInAutoSorting(list, true);
            _inResort = true;

            var fieldName = Column.BindingPath;
            IOrderedEnumerable<object> sortedSource;
            PropertyInfo property = null;
            if (newSortOrder == SortOrder.Ascending)
                sortedSource = source.OrderBy(i =>
                {
                    if (property == null) property = i.GetType().GetProperty(fieldName);
                    return property.GetValue(i, null);
                });
            else
                sortedSource = source.OrderByDescending(i =>
                {
                    if (property == null) property = i.GetType().GetProperty(fieldName);
                    return property.GetValue(i, null);
                });

            if (list.ItemsSource.GetType().Name.StartsWith("ObservableCollection`1") || list.ItemsSource.GetType().Name.StartsWith("List`1"))
            {
                var dynamicSource = list.ItemsSource as dynamic;
                var selectedItem = list.SelectedItem;
                dynamicSource.Clear();
                foreach (dynamic item in sortedSource)
                    dynamicSource.Add(item);
                if (selectedItem != null && ListEx.GetPreserveSelectionAfterResort(list) && list.SelectedItem != selectedItem)
                    list.SelectedItem = selectedItem;
            }
            else
                throw new Exception("Automatic sorting only works with observable data sources"); // Which a List<T> isn't, but we ignore that detail :-)

            var columnDefinitions = ListEx.GetColumns(list);
            if (columnDefinitions != null)
                foreach (var column in columnDefinitions)
                    column.SortOrder = SortOrder.Unsorted;
            Column.SortOrder = newSortOrder;

            ListEx.SetInAutoSorting(list, false);
            _inResort = false;

            return true;
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

        private bool _inResort;

        /// <summary>
        /// Indicates whether the column is currently filtered
        /// </summary>
        public bool ColumnIsFiltered
        {
            get { return (bool) GetValue(ColumnIsFilteredProperty); }
            set { SetValue(ColumnIsFilteredProperty, value); }
        }

        /// <summary>
        /// Indicates whether the column is currently filtered
        /// </summary>
        public static readonly DependencyProperty ColumnIsFilteredProperty = DependencyProperty.Register("ColumnIsFiltered", typeof (bool), typeof (HeaderContentControl), new PropertyMetadata(false));
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
            if (Children.Count < 1) return base.MeasureOverride(availableSize);

            var large = new Size(100000d, availableSize.Height);
            var text = Children[0];
            text.Measure(large);

            if (Children.Count < 2) return base.MeasureOverride(availableSize);
            var graphic = Children[1];
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
            if (Children.Count < 1) return base.ArrangeOverride(finalSize);

            var text = Children[0];
            if (Children.Count < 2)
            {
                text.Arrange(NewRect(0d, 0d, finalSize.Width, finalSize.Height));
                return finalSize;
            }

            var graphic = Children[1];
            if (graphic.Visibility != Visibility.Visible)
            {
                // This will also handle alignment automatically, since we give it the entire width
                text.Arrange(NewRect(0d, 0d, finalSize.Width, finalSize.Height));
                return finalSize;
            }

            var textBlock = text as TextBlock;
            if (finalSize.Width >= text.DesiredSize.Width + graphic.DesiredSize.Width + 1)
            {
                // We have plenty of room, so we can display both controls in full
                if (textBlock == null || textBlock.TextAlignment == TextAlignment.Left)
                {
                    text.Arrange(NewRect(0d, 0d, text.DesiredSize.Width, finalSize.Height));
                    graphic.Arrange(NewRect(text.DesiredSize.Width + 1d, 0d, graphic.DesiredSize.Width, finalSize.Height));
                }
                else if (textBlock.TextAlignment == TextAlignment.Right)
                {
                    text.Arrange(NewRect(0d, 0d, finalSize.Width - graphic.DesiredSize.Width - 1d, finalSize.Height));
                    graphic.Arrange(NewRect(finalSize.Width - graphic.DesiredSize.Width, 0d, graphic.DesiredSize.Width, finalSize.Height));
                }
                else
                {
                    var totalDesiredWidth = text.DesiredSize.Width + 1 + graphic.DesiredSize.Width;
                    var margin = (int) ((finalSize.Width - totalDesiredWidth)/2);
                    text.Arrange(NewRect(margin, 0d, text.DesiredSize.Width, finalSize.Height));
                    graphic.Arrange(NewRect(margin + text.DesiredSize.Width + 1d, 0d, graphic.DesiredSize.Width, finalSize.Height));
                }
            }
            else if (finalSize.Width > graphic.DesiredSize.Width)
            {
                // We don't have enough room to display both, so we do our best with the text and show the graphic in full
                text.Arrange(NewRect(0d, 0d, finalSize.Width - graphic.DesiredSize.Width - 1, finalSize.Height));
                graphic.Arrange(NewRect(finalSize.Width - graphic.DesiredSize.Width, 0d, graphic.DesiredSize.Width, finalSize.Height));
            }
            else if (finalSize.Width <= graphic.DesiredSize.Width)
            {
                // We don't have enough room to even show the graphic, so we hide the text and do our best with the graphic
                text.Arrange(NewRect(0d, 0d, 0d, finalSize.Height));
                graphic.Arrange(NewRect(0d, 0d, finalSize.Width, finalSize.Height));
            }

            return finalSize;
        }

        private static Rect NewRect(double x, double y, double width, double height)
        {
            if (width < 0) width = 0d;
            if (height < 0) height = 0d;
            return new Rect(x, y, width, height);
        }
    }

    /// <summary>Adorner UI used for header drag operations</summary>
    public class DragHeaderAdorner : Adorner
    {
        private readonly Rectangle _dragVisual;
        private double _visualLeft;
        private readonly FrameworkElement _parentElement;
        private readonly ListColumnsCollection _columns;
        private readonly int _dragColumnIndex;
        private double _mouseOffsetX;
        private readonly ColumnDropDestinationIndicator _columnDropDestinationIndicator;
        private readonly ListBoxGridHeader _parentHeader;

        /// <summary>
        /// Background brush for drag preview
        /// </summary>
        public Brush DragPreviewBackground
        {
            get { return (Brush) GetValue(DragPreviewBackgroundProperty); }
            set { SetValue(DragPreviewBackgroundProperty, value); }
        }

        /// <summary>
        /// Background brush for drag preview
        /// </summary>
        public static readonly DependencyProperty DragPreviewBackgroundProperty = DependencyProperty.Register("DragPreviewBackground", typeof (Brush), typeof (DragHeaderAdorner), new PropertyMetadata(new SolidColorBrush(Color.FromArgb(80, 0, 0, 0))));


        /// <summary>Constructor</summary>
        /// <param name="adornedElement">Adorned element (typically a textbox)</param>
        /// <param name="dragVisual">The UI that is to be used in the drop-down</param>
        /// <param name="columns">Overall columns collection this adorner goes with.</param>
        /// <param name="dragColumnIndex">Index of the column that is being dragged</param>
        public DragHeaderAdorner(FrameworkElement adornedElement, Rectangle dragVisual, ListColumnsCollection columns, int dragColumnIndex)
            : base(adornedElement)
        {
            LastCalculatedDropIndex = -1;
            _parentElement = adornedElement;
            _parentHeader = adornedElement as ListBoxGridHeader;
            _dragVisual = dragVisual;
            _columns = columns;
            _dragColumnIndex = dragColumnIndex;
            AddVisualChild(dragVisual);
            _columnDropDestinationIndicator = new ColumnDropDestinationIndicator();
            AddVisualChild(_columnDropDestinationIndicator);
        }

        /// <summary>
        /// Implements any custom measuring behavior for the adorner.
        /// </summary>
        /// <param name="constraint">A size to constrain the adorner to.</param>
        /// <returns>
        /// A <see cref="T:System.Windows.Size"/> object representing the amount of layout space needed by the adorner.
        /// </returns>
        protected override Size MeasureOverride(Size constraint)
        {
            _dragVisual.Measure(constraint);
            _columnDropDestinationIndicator.Measure(constraint);
            return new Size(_parentElement.ActualWidth, _parentElement.ActualHeight);
        }

        /// <summary>
        /// When overridden in a derived class, positions child elements and determines a size for a <see cref="T:System.Windows.FrameworkElement"/> derived class.
        /// </summary>
        /// <param name="finalSize">The final area within the parent that this element should use to arrange itself and its children.</param>
        /// <returns>
        /// The actual size used.
        /// </returns>
        protected override Size ArrangeOverride(Size finalSize)
        {
            // Arranging the preview 
            _dragVisual.Arrange(new Rect(_visualLeft, 0, _dragVisual.Width, _dragVisual.Height));

            // Arranging the drop indicator
            var closestSeparatorIndex = GetClosestColumnSeparatorIndexAtHorizontalPosition(_visualLeft + _mouseOffsetX - _parentHeader.HorizontalHeaderOffset);
            _columnDropDestinationIndicator.Visibility = closestSeparatorIndex < 0 || closestSeparatorIndex == _dragColumnIndex || closestSeparatorIndex == _dragColumnIndex + 1 ? Visibility.Collapsed : Visibility.Visible;
            var destinationIndicatorX = 0d;
            for (var columnCounter = 0; columnCounter < closestSeparatorIndex; columnCounter++)
                destinationIndicatorX += _columns[columnCounter].ActualWidth;
            _columnDropDestinationIndicator.Arrange(new Rect(destinationIndicatorX - _parentHeader.HorizontalHeaderOffset, 0, 1, _dragVisual.Height));
            LastCalculatedDropIndex = closestSeparatorIndex;

            return new Size(_parentElement.ActualWidth, _parentElement.ActualHeight);
        }

        /// <summary>
        /// Drop index calculated by moving the column
        /// </summary>
        public int LastCalculatedDropIndex { get; set; }

        private int GetClosestColumnSeparatorIndexAtHorizontalPosition(double x)
        {
            var separators = _parentHeader.GetColumnSeparatorPositions();
            var distancesFromSeparators = new double[separators.Length];
            for (var edgeCounter = 0; edgeCounter < separators.Length; edgeCounter++)
                distancesFromSeparators[edgeCounter] = Math.Abs(separators[edgeCounter] - x);

            var nearestEdgeIndex = -1;
            var nearestOffset = double.MaxValue;
            for (var edgeCounter = 0; edgeCounter < distancesFromSeparators.Length; edgeCounter++)
                if (distancesFromSeparators[edgeCounter] < nearestOffset)
                {
                    nearestEdgeIndex = edgeCounter;
                    nearestOffset = distancesFromSeparators[edgeCounter];
                }

            return nearestEdgeIndex;
        }

        /// <summary>
        /// Gets the number of visual child elements within this element.
        /// </summary>
        /// <returns>The number of visual child elements for this element.</returns>
        protected override int VisualChildrenCount
        {
            get { return 2; }
        }

        /// <summary>
        /// Overrides <see cref="M:System.Windows.Media.Visual.GetVisualChild(System.Int32)"/>, and returns a child at the specified index from a collection of child elements.
        /// </summary>
        /// <param name="index">The zero-based index of the requested child element in the collection.</param>
        /// <returns>
        /// The requested child element. This should not return null; if the provided index is out of range, an exception is thrown.
        /// </returns>
        protected override Visual GetVisualChild(int index)
        {
            if (index == 1) return _columnDropDestinationIndicator;
            return _dragVisual;
        }

        /// <summary>
        /// Called when the control needs to render itself.
        /// </summary>
        /// <param name="drawingContext">The drawing context.</param>
        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            drawingContext.DrawRectangle(DragPreviewBackground, null, new Rect(_visualLeft - 4, 0, _dragVisual.Width + 8, _dragVisual.Height));
        }

        /// <summary>
        /// Moves the 'preview' visual of the column header to the specified position
        /// </summary>
        /// <param name="visualLeft">X position to move the element to</param>
        /// <param name="mouseOffsetX">The mouse X offset within the column header.</param>
        public void SetDragVisualLeft(double visualLeft, double mouseOffsetX)
        {
            _visualLeft = visualLeft;
            _mouseOffsetX = mouseOffsetX;
            InvalidateVisual();
        }
    }

    /// <summary>
    /// This stylable control indicates the drop-destination for a dragged column
    /// </summary>
    public class ColumnDropDestinationIndicator : Control
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public ColumnDropDestinationIndicator()
        {
            MinWidth = 1;
            MinHeight = 1;
        }
    }
}
using System;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using CODE.Framework.Core.Utilities;
using CODE.Framework.Core.Utilities.Extensions;
using CODE.Framework.Wpf.Layout;
using CODE.Framework.Wpf.Utilities;
using System.Windows.Threading;

namespace CODE.Framework.Wpf.Controls
{
    /// <summary>Smart, self-populating data template that can be used in generic listboxes with columns</summary>
    public class ListBoxSmartDataTemplate : ColumnPanel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ListBoxSmartDataTemplate"/> class.
        /// </summary>
        public ListBoxSmartDataTemplate()
        {
            ClipToBounds = false;
            Focusable = true;

            Loaded += (s, a) =>
            {
                var item = ElementHelper.FindVisualTreeParent<ListBoxItem>(this);
                if (item == null)
                {
                    if (!string.IsNullOrEmpty(_defaultColumnsToSet))
                        PopulateColumnsFromDefaults(this, null);
                }

                var listBox = ItemsControl.ItemsControlFromItemContainer(item);
                if (listBox == null) return;

                var columns = ListEx.GetColumns(listBox);
                if (columns != null)
                {
                    Columns = columns;
                    if (!string.IsNullOrEmpty(columns.EditModeBindingPath))
                        SetBinding(IsManualEditEnabledProperty, new Binding(columns.EditModeBindingPath));
                    else
                        SetBinding(IsManualEditEnabledProperty, new Binding {Path = new PropertyPath("(0)", ListEx.IsEditableProperty), RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent)});
                }
                else if (string.IsNullOrEmpty(_defaultColumnsToSet))
                {
                    var textBlock = new TextBlock();
                    textBlock.SetBinding(TextBlock.TextProperty, new Binding {Path = new PropertyPath(listBox.DisplayMemberPath)});
                    Children.Add(textBlock);
                }
                else
                    PopulateColumnsFromDefaults(this, listBox);

                listBox.DataContextChanged += (s2, a2) =>
                {
                    var columns2 = ListEx.GetColumns(listBox);
                    if (columns2 == null) return;
                    Columns = columns2;
                    if (string.IsNullOrEmpty(columns2.EditModeBindingPath)) return;
                    BindingOperations.ClearBinding(this, IsManualEditEnabledProperty);
                    SetBinding(IsManualEditEnabledProperty, new Binding(columns2.EditModeBindingPath));
                };

                RaiseEditModeChanged();
            };
        }

        /// <summary>Occurs when the row edit mode changes</summary>
        private event EventHandler EditModeChanged;

        private void RaiseEditModeChanged()
        {
            var handler = EditModeChanged;
            if (handler != null)
                handler(this, new EventArgs());
        }

        /// <summary>Generic column definition</summary>
        public ListColumnsCollection Columns
        {
            get { return (ListColumnsCollection) GetValue(ColumnsProperty); }
            set { SetValue(ColumnsProperty, value); }
        }

        /// <summary>Generic column definition</summary>
        public static readonly DependencyProperty ColumnsProperty = DependencyProperty.Register("Columns", typeof (ListColumnsCollection), typeof (ListBoxSmartDataTemplate), new UIPropertyMetadata(null, OnColumnsChanged));

        /// <summary>Called when columns change.</summary>
        /// <param name="o">The object the columns changed on.</param>
        /// <param name="args">The <see cref="System.Windows.DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void OnColumnsChanged(DependencyObject o, DependencyPropertyChangedEventArgs args)
        {
            var item = o as ListBoxSmartDataTemplate;
            if (item == null) return;
            item.TriggerConsolidatedColumnRepopulate();
            var columns = args.NewValue as ListColumnsCollection;
            if (columns == null) return;
            columns.CollectionChangedDelayed -= item.HandleColumnCollectionChanged;
            columns.CollectionChangedDelayed += item.HandleColumnCollectionChanged;
            foreach (var column in columns)
            {
                column.VisibilityChanged -= item.HandleColumnVisibilityChanged;
                column.VisibilityChanged += item.HandleColumnVisibilityChanged;
            }
        }

        private void HandleColumnVisibilityChanged(object sender, EventArgs e)
        {
            TriggerConsolidatedColumnRepopulate();
        }

        private void HandleColumnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            RepopulateColumns(); // This handler is already triggered delayed, so we can go right ahead.
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
                    RepopulateColumns();
                }, Application.Current.Dispatcher) {IsEnabled = false};
            else
                _delayTimer.IsEnabled = false; // Resets the timer

            // Triggering the next timer run
            _delayTimer.IsEnabled = true;
        }

        /// <summary>Definition of default columns, which can be used in case the listbox itself does not define columns</summary>
        public string DefaultColumns
        {
            get { return (string) GetValue(DefaultColumnsProperty); }
            set { SetValue(DefaultColumnsProperty, value); }
        }

        /// <summary>Definition of default columns, which can be used in case the listbox itself does not define columns</summary>
        public static readonly DependencyProperty DefaultColumnsProperty = DependencyProperty.Register("DefaultColumns", typeof (string), typeof (ListBoxSmartDataTemplate), new UIPropertyMetadata("", DefaultColumnsChanged));

        private string _defaultColumnsToSet = string.Empty;

        /// <summary>Fires when the default columns change</summary>
        /// <param name="o">The o.</param>
        /// <param name="args">The <see cref="System.Windows.DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void DefaultColumnsChanged(DependencyObject o, DependencyPropertyChangedEventArgs args)
        {
            var listItem = o as ListBoxSmartDataTemplate;
            if (listItem == null) return;
            listItem._defaultColumnsToSet = args.NewValue.ToString();

            var item = ElementHelper.FindVisualTreeParent<ListBoxItem>(listItem);
            if (item == null) return;

            var listBox = ItemsControl.ItemsControlFromItemContainer(item);
            if (listBox == null) return;

            var columns = ListEx.GetColumns(listBox);
            if (columns == null)
                PopulateColumnsFromDefaults(listItem, listBox);
        }

        /// <summary>Edit mode for the current row</summary>
        /// <value>The edit mode.</value>
        public ListRowEditMode EditMode
        {
            get { return (ListRowEditMode) GetValue(EditModeProperty); }
            set { SetValue(EditModeProperty, value); }
        }

        /// <summary>Edit mode for the current row</summary>
        /// <value>The edit mode.</value>
        public static readonly DependencyProperty EditModeProperty = DependencyProperty.Register("EditMode", typeof (ListRowEditMode), typeof (ListBoxSmartDataTemplate), new PropertyMetadata(ListRowEditMode.ReadOnly, OnEditModeChanged));

        /// <summary>Indicates whether the current template is in manual editing mode</summary>
        /// <value><c>true</c> if this instance is manual edit enabled; otherwise, <c>false</c>.</value>
        public bool IsManualEditEnabled
        {
            get { return (bool) GetValue(IsManualEditEnabledProperty); }
            set { SetValue(IsManualEditEnabledProperty, value); }
        }

        /// <summary>Indicates whether the current template is in manual editing mode</summary>
        /// <value><c>true</c> if this instance is manual edit enabled; otherwise, <c>false</c>.</value>
        public static readonly DependencyProperty IsManualEditEnabledProperty = DependencyProperty.Register("IsManualEditEnabled", typeof (bool), typeof (ListBoxSmartDataTemplate), new PropertyMetadata(false, OnEditModeChanged));

        /// <summary>Occurs when the edit mode changes</summary>
        /// <param name="d">The source.</param>
        /// <param name="args">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void OnEditModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            var template = d as ListBoxSmartDataTemplate;
            if (template == null) return;
            template.RaiseEditModeChanged();
        }

        /// <summary>
        /// Style applied to image controls in image columns
        /// </summary>
        /// <value>The image style.</value>
        public Style ImageStyle
        {
            get { return (Style)GetValue(ImageStyleProperty); }
            set { SetValue(ImageStyleProperty, value); }
        }

        /// <summary>
        /// Style applied to image controls in image columns
        /// </summary>
        public static readonly DependencyProperty ImageStyleProperty = DependencyProperty.Register("ImageStyle", typeof(Style), typeof(ListBoxSmartDataTemplate), new PropertyMetadata(null));

        /// <summary>
        /// Style applied to image controls in logo columns
        /// </summary>
        /// <value>The image style.</value>
        public Style LogoStyle
        {
            get { return (Style)GetValue(LogoStyleProperty); }
            set { SetValue(LogoStyleProperty, value); }
        }

        /// <summary>
        /// Style applied to logo controls in image columns
        /// </summary>
        public static readonly DependencyProperty LogoStyleProperty = DependencyProperty.Register("LogoStyle", typeof(Style), typeof(ListBoxSmartDataTemplate), new PropertyMetadata(null));

        private static void PopulateColumnsFromDefaults(ListBoxSmartDataTemplate item, ItemsControl listBox)
        {
            var columns = new ListColumnsCollection();

            var dataSources = item._defaultColumnsToSet.Split(',');
            foreach (var dataSource in dataSources)
            {
                var path = dataSource.Trim();

                var width = 200;
                if (path.StartsWith("Image")) width = 30;
                else if (path.StartsWith("Logo")) width = 30;
                else if (path.StartsWith("Number")) width = 75;

                var header = string.Empty;
                if (!path.StartsWith("Image") && !path.StartsWith("Logo"))
                    header = path.SpaceCamelCase();

                columns.Add(new ListColumn
                {
                    BindingPath = path,
                    Width = new GridLength(width),
                    Header = header,
                    IsResizable = !(path.StartsWith("Image") || path.StartsWith("Logo"))
                });
            }

            if (listBox != null)
                ListEx.SetColumns(listBox, columns);
            else
                item.Columns = columns;

            item._defaultColumnsToSet = string.Empty;
        }

        private Grid GetContentGridForColumn(ListColumn column)
        {
            var grid = new Grid {VerticalAlignment = VerticalAlignment.Stretch, HorizontalAlignment = HorizontalAlignment.Stretch, IsHitTestVisible = true, Background = Brushes.Transparent, ClipToBounds = true};

            var paddingOffset = new Thickness();
            if (column.CellPadding.Left > 0d || column.CellPadding.Top > 0d || column.CellPadding.Right > 0d || column.CellPadding.Bottom > 0d)
            {
                paddingOffset = new Thickness(column.CellPadding.Left*-1, column.CellPadding.Top*-1, column.CellPadding.Right*-1, column.CellPadding.Bottom*-1);
                grid.Margin = new Thickness(column.CellPadding.Left, column.CellPadding.Top, column.CellPadding.Right, column.CellPadding.Bottom);
            }

            if (column.CellBackground != null)
            {
                if (column.CellBackgroundOpacity < 1d)
                    grid.Children.Add(new Rectangle
                    {
                        VerticalAlignment = VerticalAlignment.Stretch,
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        Fill = column.CellBackground,
                        Opacity = column.CellBackgroundOpacity,
                        Width = double.NaN,
                        Height = double.NaN,
                        Margin = paddingOffset
                    });
                else
                    grid.Background = column.CellBackground;
            }
            else if (!string.IsNullOrEmpty(column.CellBackgroundBindingPath))
            {
                var backgroundBinding = new Binding(column.CellBackgroundBindingPath);
                if (column.CellBackgroundOpacity < 1d)
                {
                    var rect = new Rectangle
                    {
                        VerticalAlignment = VerticalAlignment.Stretch,
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        Fill = column.CellBackground,
                        Opacity = column.CellBackgroundOpacity,
                        Width = double.NaN,
                        Height = double.NaN,
                        Margin = paddingOffset
                    };
                    rect.SetBinding(Shape.FillProperty, backgroundBinding);
                    grid.Children.Add(rect);
                }
                else
                    grid.SetBinding(BackgroundProperty, backgroundBinding);
            }

            if (Columns.ShowGridLines != ListGridLineMode.Never)
            {
                var cellLines = new Border
                {
                    VerticalAlignment = VerticalAlignment.Stretch,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    BorderThickness = new Thickness(0, 0, 1, 1),
                    BorderBrush = column.GridLineBrush,
                    Background = null,
                    Visibility = Visibility.Collapsed,
                    IsHitTestVisible = false,
                    Margin = paddingOffset
                };
                SetZIndex(cellLines, 10000);
                if (Columns.ShowGridLines == ListGridLineMode.Always)
                    cellLines.Visibility = Visibility.Visible;
                if (Columns.ShowGridLines == ListGridLineMode.EditOnly)
                    EditModeChanged += (s, e) => { cellLines.Visibility = IsManualEditEnabled ? Visibility.Visible : Visibility.Collapsed; };
                grid.Children.Add(cellLines);
            }

            if (!double.IsNaN(column.CellContentWidth)) grid.Width = column.CellContentWidth;
            if (!double.IsNaN(column.CellContentHeight)) grid.Height = column.CellContentHeight;

            return grid;
        }

        private static void SetDoubleClickCommands(DependencyObject element, ICommand command)
        {
            if (element == null || command == null) return;
            var control = element as Control;
            if (control == null)
            {
                var textBlock = element as TextBlock;
                if (textBlock == null) return;
                textBlock.MouseLeftButtonDown += (s2, e2) =>
                {
                    if (e2.ClickCount != 2) return;
                    if (command.CanExecute(null))
                        command.Execute(null);
                };
                return;
            }
            control.MouseDoubleClick += (s, e) =>
            {
                if (command.CanExecute(null))
                    command.Execute(null);
            };
        }

        private static void SetEventCommands(DependencyObject element, EventCommandsCollection commands)
        {
            if (element == null) return;
            Ex.SetEventCommands(element, commands);
        }

        private void RepopulateColumns()
        {
            Children.Clear();
            ColumnDefinitions.Clear();
            if (Columns == null) return;

            var columnCounter = -1;
            var starColumnFound = false;
            foreach (var column in Columns.Where(c => c.Visibility == Visibility.Visible))
            {
                columnCounter++;

                var gridColumn = new ColumnDefinition();
                BindingOperations.ClearBinding(gridColumn, ColumnDefinition.WidthProperty);
                gridColumn.SetBinding(ColumnDefinition.WidthProperty, new Binding("ActualWidth") {Source = column, Mode = BindingMode.OneWay, Converter = new LengthToGridLengthConverter()});
                ColumnDefinitions.Add(new ColumnPanelColumnDefinition(gridColumn));
                if (column.Width.GridUnitType == GridUnitType.Star)
                    starColumnFound = true;

                if (column.ItemTemplate != null)
                {
                    var content = column.ItemTemplate.LoadContent() as UIElement;
                    if (content == null) continue;
                    Children.Add(content);
                    SetColumn(content, columnCounter);
                }
                else if (column.ColumnControl != ListColumnControls.Auto || (!column.BindingPath.StartsWith("Number") && !column.BindingPath.StartsWith("Image") && !column.BindingPath.StartsWith("Logo")))
                {
                    var content = GetContentGridForColumn(column);

                    switch (column.ColumnControl)
                    {
                        case ListColumnControls.Auto:
                        case ListColumnControls.Text:
                            TextBlock text = null;
                            TextBox tb = null;
                            if (column.EditMode != ListRowEditMode.ReadWriteAll)
                            {
                                text = new TextBlock {VerticalAlignment = VerticalAlignment.Center};
                                SetEventCommands(text, column.ReadOnlyControlEventCommands);
                                if (!string.IsNullOrEmpty(column.DoubleClickCommandBindingPath)) SetDoubleClickCommands(text, DataContext.GetPropertyValue<ICommand>(column.DoubleClickCommandBindingPath));
                                SetHorizontalAlignment(text, column.CellContentAlignment);
                                if (!string.IsNullOrEmpty(column.BindingPath)) text.SetBinding(TextBlock.TextProperty, new Binding(column.BindingPath));
                                if (!string.IsNullOrEmpty(column.ToolTipBindingPath)) text.SetBinding(ToolTipProperty, new Binding(column.ToolTipBindingPath));
                                else if (!string.IsNullOrEmpty(column.ToolTip)) text.ToolTip = column.ToolTip;
                                if (!string.IsNullOrEmpty(column.ToolTipBindingPath) || !string.IsNullOrEmpty(column.ToolTip))
                                {
                                    if (column.ToolTipInitialShowDelay > int.MinValue) ToolTipService.SetInitialShowDelay(text, column.ToolTipInitialShowDelay);
                                    if (column.ToolTipShowDuration > int.MinValue) ToolTipService.SetShowDuration(text, column.ToolTipShowDuration);
                                    if (column.ToolTipPlacement != PlacementMode.Mouse) ToolTipService.SetPlacement(text, column.ToolTipPlacement);
                                }
                                if (EditMode == ListRowEditMode.Manual && IsManualEditEnabled) text.Visibility = Visibility.Collapsed;
                                if (column.CellForeground != null) text.Foreground = column.CellForeground;
                                else if (!string.IsNullOrEmpty(column.CellForegroundBindingPath)) text.SetBinding(TextBlock.ForegroundProperty, new Binding(column.CellForegroundBindingPath));
                                content.Children.Add(text);
                                column.RaiseCellControlCreated(text);
                            }
                            if (column.EditMode != ListRowEditMode.ReadOnly)
                            {
                                tb = new TextBox {VerticalAlignment = VerticalAlignment.Stretch, HorizontalAlignment = HorizontalAlignment.Stretch, VerticalContentAlignment = VerticalAlignment.Center};
                                SetEventCommands(tb, column.WriteControlEventCommands);
                                if (!string.IsNullOrEmpty(column.DoubleClickCommandBindingPath)) SetDoubleClickCommands(tb, DataContext.GetPropertyValue<ICommand>(column.DoubleClickCommandBindingPath));
                                SetHorizontalContentAlignment(tb, column.CellContentAlignment, HorizontalAlignment.Stretch);
                                if (!string.IsNullOrEmpty(column.EditControlBindingPath) || !string.IsNullOrEmpty(column.BindingPath)) tb.SetBinding(TextBox.TextProperty, new Binding(string.IsNullOrEmpty(column.EditControlBindingPath) ? column.BindingPath : column.EditControlBindingPath) {UpdateSourceTrigger = column.EditControlUpdateSourceTrigger, StringFormat = column.EditControlStringFormat});
                                if (!string.IsNullOrEmpty(column.ToolTipBindingPath)) tb.SetBinding(ToolTipProperty, new Binding(column.ToolTipBindingPath));
                                else if (!string.IsNullOrEmpty(column.ToolTip)) tb.ToolTip = column.ToolTip;
                                if (!string.IsNullOrEmpty(column.ToolTipBindingPath) || !string.IsNullOrEmpty(column.ToolTip))
                                {
                                    if (column.ToolTipInitialShowDelay > int.MinValue) ToolTipService.SetInitialShowDelay(tb, column.ToolTipInitialShowDelay);
                                    if (column.ToolTipShowDuration > int.MinValue) ToolTipService.SetShowDuration(tb, column.ToolTipShowDuration);
                                    if (column.ToolTipPlacement != PlacementMode.Mouse) ToolTipService.SetPlacement(tb, column.ToolTipPlacement);
                                }
                                if (EditMode == ListRowEditMode.Manual && IsManualEditEnabled) tb.Visibility = Visibility.Visible;
                                else if (column.EditMode == ListRowEditMode.ReadWriteAll) tb.Visibility = Visibility.Visible;
                                else tb.Visibility = Visibility.Collapsed;
                                if (column.EditTextBoxStyle != null) tb.Style = column.EditTextBoxStyle;
                                else
                                {
                                    tb.Padding = new Thickness(0);
                                    tb.BorderThickness = new Thickness(0);
                                }
                                if (column.CellForeground != null) tb.Foreground = column.CellForeground;
                                else if (!string.IsNullOrEmpty(column.CellForegroundBindingPath)) tb.SetBinding(Control.ForegroundProperty, new Binding(column.CellForegroundBindingPath));
                                if (!string.IsNullOrEmpty(column.ColumnControlIsEnabledBindingPath)) tb.SetBinding(IsEnabledProperty, new Binding(column.ColumnControlIsEnabledBindingPath));
                                content.Children.Add(tb);
                                column.RaiseCellControlCreated(tb);
                            }
                            if (column.EditMode == ListRowEditMode.Manual)
                                EditModeChanged += (s, e) =>
                                {
                                    if (text != null) text.Visibility = IsManualEditEnabled ? Visibility.Collapsed : Visibility.Visible;
                                    if (tb != null) tb.Visibility = IsManualEditEnabled ? Visibility.Visible : Visibility.Collapsed;
                                };
                            break;
                        case ListColumnControls.Checkmark:
                            var check = new CheckBox {VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right, Content = string.Empty, IsEnabled = false};
                            SetEventCommands(check, column.ReadOnlyControlEventCommands);
                            if (!string.IsNullOrEmpty(column.DoubleClickCommandBindingPath)) SetDoubleClickCommands(check, DataContext.GetPropertyValue<ICommand>(column.DoubleClickCommandBindingPath));
                            if (!string.IsNullOrEmpty(column.EditControlBindingPath) || !string.IsNullOrEmpty(column.BindingPath)) check.SetBinding(ToggleButton.IsCheckedProperty, new Binding(string.IsNullOrEmpty(column.EditControlBindingPath) ? column.BindingPath : column.EditControlBindingPath) {UpdateSourceTrigger = column.EditControlUpdateSourceTrigger, StringFormat = column.EditControlStringFormat});
                            if (!string.IsNullOrEmpty(column.ToolTipBindingPath)) check.SetBinding(ToolTipProperty, new Binding(column.ToolTipBindingPath));
                            else if (!string.IsNullOrEmpty(column.ToolTip)) check.ToolTip = column.ToolTip;
                            if (!string.IsNullOrEmpty(column.ToolTipBindingPath) || !string.IsNullOrEmpty(column.ToolTip))
                            {
                                if (column.ToolTipInitialShowDelay > int.MinValue) ToolTipService.SetInitialShowDelay(check, column.ToolTipInitialShowDelay);
                                if (column.ToolTipShowDuration > int.MinValue) ToolTipService.SetShowDuration(check, column.ToolTipShowDuration);
                                if (column.ToolTipPlacement != PlacementMode.Mouse) ToolTipService.SetPlacement(check, column.ToolTipPlacement);
                            }
                            SetHorizontalAlignment(check, column.CellContentAlignment, HorizontalAlignment.Center);
                            if (column.EditCheckmarkStyle != null) check.Style = column.EditCheckmarkStyle;
                            if (column.CellForeground != null) check.Foreground = column.CellForeground;
                            else if (!string.IsNullOrEmpty(column.CellForegroundBindingPath)) check.SetBinding(Control.ForegroundProperty, new Binding(column.CellForegroundBindingPath));
                            if (!string.IsNullOrEmpty(column.ColumnControlIsEnabledBindingPath)) check.SetBinding(IsEnabledProperty, new Binding(column.ColumnControlIsEnabledBindingPath));
                            content.Children.Add(check);
                            column.RaiseCellControlCreated(check);
                            if (column.EditMode == ListRowEditMode.Manual)
                            {
                                var col2 = column;
                                EditModeChanged += (s, e) =>
                                {
                                    if (check != null)
                                    {
                                        check.IsEnabled = IsManualEditEnabled;
                                        SetEventCommands(check, check.IsEnabled ? col2.ReadOnlyControlEventCommands : col2.WriteControlEventCommands);
                                    }
                                };
                            }
                            else if (column.EditMode == ListRowEditMode.ReadWriteAll) check.IsEnabled = true;
                            break;
                        case ListColumnControls.TextList:
                            TextBlock text2 = null;
                            ComboBox combo = null;
                            if (column.EditMode != ListRowEditMode.ReadWriteAll)
                            {
                                text2 = new TextBlock {VerticalAlignment = VerticalAlignment.Center};
                                SetEventCommands(text2, column.ReadOnlyControlEventCommands);
                                if (!string.IsNullOrEmpty(column.DoubleClickCommandBindingPath)) SetDoubleClickCommands(text2, DataContext.GetPropertyValue<ICommand>(column.DoubleClickCommandBindingPath));
                                SetHorizontalAlignment(text2, column.CellContentAlignment);
                                if (!string.IsNullOrEmpty(column.ToolTipBindingPath)) text2.SetBinding(ToolTipProperty, new Binding(column.ToolTipBindingPath));
                                else if (!string.IsNullOrEmpty(column.ToolTip)) text2.ToolTip = column.ToolTip;
                                if (!string.IsNullOrEmpty(column.ToolTipBindingPath) || !string.IsNullOrEmpty(column.ToolTip))
                                {
                                    if (column.ToolTipInitialShowDelay > int.MinValue) ToolTipService.SetInitialShowDelay(text2, column.ToolTipInitialShowDelay);
                                    if (column.ToolTipShowDuration > int.MinValue) ToolTipService.SetShowDuration(text2, column.ToolTipShowDuration);
                                    if (column.ToolTipPlacement != PlacementMode.Mouse) ToolTipService.SetPlacement(text2, column.ToolTipPlacement);
                                }
                                if (!string.IsNullOrEmpty(column.BindingPath)) text2.SetBinding(TextBlock.TextProperty, new Binding(column.BindingPath));
                                if (EditMode == ListRowEditMode.Manual && IsManualEditEnabled) text2.Visibility = Visibility.Collapsed;
                                if (column.CellForeground != null) text2.Foreground = column.CellForeground;
                                else if (!string.IsNullOrEmpty(column.CellForegroundBindingPath)) text2.SetBinding(TextBlock.ForegroundProperty, new Binding(column.CellForegroundBindingPath));
                                content.Children.Add(text2);
                                column.RaiseCellControlCreated(text2);
                            }
                            if (column.EditMode != ListRowEditMode.ReadOnly)
                            {
                                combo = new ComboBox {VerticalAlignment = VerticalAlignment.Stretch, HorizontalAlignment = HorizontalAlignment.Stretch, VerticalContentAlignment = VerticalAlignment.Center};
                                SetEventCommands(combo, column.WriteControlEventCommands);
                                if (!string.IsNullOrEmpty(column.DoubleClickCommandBindingPath)) SetDoubleClickCommands(combo, DataContext.GetPropertyValue<ICommand>(column.DoubleClickCommandBindingPath));
                                SetHorizontalContentAlignment(combo, column.CellContentAlignment, HorizontalAlignment.Stretch);
                                if (!string.IsNullOrEmpty(column.BindingPath) || !string.IsNullOrEmpty(column.EditControlBindingPath)) combo.SetBinding(Selector.SelectedValueProperty, new Binding(string.IsNullOrEmpty(column.EditControlBindingPath) ? column.BindingPath : column.EditControlBindingPath) {UpdateSourceTrigger = column.EditControlUpdateSourceTrigger, StringFormat = column.EditControlStringFormat});
                                if (!string.IsNullOrEmpty(column.TextListItemsSourceBindingPath)) combo.SetBinding(ItemsControl.ItemsSourceProperty, new Binding(column.TextListItemsSourceBindingPath));
                                if (!string.IsNullOrEmpty(column.TextListSelectedValuePath)) combo.SelectedValuePath = column.TextListSelectedValuePath;
                                if (!string.IsNullOrEmpty(column.TextListDisplayMemberPath)) combo.DisplayMemberPath = column.TextListDisplayMemberPath;
                                if (!string.IsNullOrEmpty(column.ToolTipBindingPath)) combo.SetBinding(ToolTipProperty, new Binding(column.ToolTipBindingPath));
                                else if (!string.IsNullOrEmpty(column.ToolTip)) combo.ToolTip = column.ToolTip;
                                if (!string.IsNullOrEmpty(column.ToolTipBindingPath) || !string.IsNullOrEmpty(column.ToolTip))
                                {
                                    if (column.ToolTipInitialShowDelay > int.MinValue) ToolTipService.SetInitialShowDelay(combo, column.ToolTipInitialShowDelay);
                                    if (column.ToolTipShowDuration > int.MinValue) ToolTipService.SetShowDuration(combo, column.ToolTipShowDuration);
                                    if (column.ToolTipPlacement != PlacementMode.Mouse) ToolTipService.SetPlacement(combo, column.ToolTipPlacement);
                                }
                                if (column.EditMode == ListRowEditMode.Manual && IsManualEditEnabled) combo.Visibility = Visibility.Visible;
                                else if (column.EditMode == ListRowEditMode.ReadWriteAll) combo.Visibility = Visibility.Visible;
                                else combo.Visibility = Visibility.Collapsed;
                                if (!string.IsNullOrEmpty(column.ColumnControlIsEnabledBindingPath)) combo.SetBinding(IsEnabledProperty, new Binding(column.ColumnControlIsEnabledBindingPath));
                                if (column.EditTextListStyle != null) combo.Style = column.EditTextListStyle;
                                else
                                {
                                    combo.Padding = new Thickness(0);
                                    combo.BorderThickness = new Thickness(0);
                                }
                                if (column.CellForeground != null) combo.Foreground = column.CellForeground;
                                else if (!string.IsNullOrEmpty(column.CellForegroundBindingPath)) combo.SetBinding(Control.ForegroundProperty, new Binding(column.CellForegroundBindingPath));
                                if (column.TextListColumns != null) ListEx.SetColumns(combo, column.TextListColumns);
                                content.Children.Add(combo);
                                column.RaiseCellControlCreated(combo);
                            }
                            if (column.EditMode == ListRowEditMode.Manual)
                                EditModeChanged += (s, e) =>
                                {
                                    if (text2 != null) text2.Visibility = IsManualEditEnabled ? Visibility.Collapsed : Visibility.Visible;
                                    if (combo != null) combo.Visibility = IsManualEditEnabled ? Visibility.Visible : Visibility.Collapsed;
                                };
                            break;
                        case ListColumnControls.Image:
                            var image = new Image {VerticalAlignment = VerticalAlignment.Stretch, HorizontalAlignment = HorizontalAlignment.Stretch, Stretch = Stretch.UniformToFill};
                            SetEventCommands(image, column.ReadOnlyControlEventCommands);
                            if (!string.IsNullOrEmpty(column.DoubleClickCommandBindingPath)) SetDoubleClickCommands(image, DataContext.GetPropertyValue<ICommand>(column.DoubleClickCommandBindingPath));
                            if (ImageStyle != null) image.Style = ImageStyle;
                            if (!string.IsNullOrEmpty(column.BindingPath)) image.SetBinding(Image.SourceProperty, new Binding(column.BindingPath) {Mode = BindingMode.OneWay});
                            if (!string.IsNullOrEmpty(column.ToolTipBindingPath)) image.SetBinding(ToolTipProperty, new Binding(column.ToolTipBindingPath));
                            else if (!string.IsNullOrEmpty(column.ToolTip)) image.ToolTip = column.ToolTip;
                            if (!string.IsNullOrEmpty(column.ToolTipBindingPath) || !string.IsNullOrEmpty(column.ToolTip))
                            {
                                if (column.ToolTipInitialShowDelay > int.MinValue) ToolTipService.SetInitialShowDelay(image, column.ToolTipInitialShowDelay);
                                if (column.ToolTipShowDuration > int.MinValue) ToolTipService.SetShowDuration(image, column.ToolTipShowDuration);
                                if (column.ToolTipPlacement != PlacementMode.Mouse) ToolTipService.SetPlacement(image, column.ToolTipPlacement);
                            }
                            content.Children.Add(image);
                            column.RaiseCellControlCreated(image);
                            break;
                    }

                    Children.Add(content);
                    SetColumn(content, columnCounter);
                }
                else if (column.BindingPath.StartsWith("Number"))
                {
                    var content = GetContentGridForColumn(column);
                    var text = new TextBlock {VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right, TextAlignment = TextAlignment.Right};
                    SetEventCommands(text, column.ReadOnlyControlEventCommands); text.SetBinding(TextBlock.TextProperty, new Binding(column.BindingPath));
                    if (!string.IsNullOrEmpty(column.DoubleClickCommandBindingPath)) SetDoubleClickCommands(text, DataContext.GetPropertyValue<ICommand>(column.DoubleClickCommandBindingPath));
                    if (!string.IsNullOrEmpty(column.ToolTipBindingPath)) text.SetBinding(ToolTipProperty, new Binding(column.ToolTipBindingPath));
                    else if (!string.IsNullOrEmpty(column.ToolTip)) text.ToolTip = column.ToolTip;
                    if (!string.IsNullOrEmpty(column.ToolTipBindingPath) || !string.IsNullOrEmpty(column.ToolTip))
                    {
                        if (column.ToolTipInitialShowDelay > int.MinValue) ToolTipService.SetInitialShowDelay(text, column.ToolTipInitialShowDelay);
                        if (column.ToolTipShowDuration > int.MinValue) ToolTipService.SetShowDuration(text, column.ToolTipShowDuration);
                        if (column.ToolTipPlacement != PlacementMode.Mouse) ToolTipService.SetPlacement(text, column.ToolTipPlacement);
                    }
                    if (column.CellForeground != null) text.Foreground = column.CellForeground;
                    else if (!string.IsNullOrEmpty(column.CellForegroundBindingPath)) text.SetBinding(TextBlock.ForegroundProperty, new Binding(column.CellForegroundBindingPath));
                    content.Children.Add(text);
                    Children.Add(content);
                    SetColumn(content, columnCounter);
                }
                else if (column.BindingPath.StartsWith("Image") || column.BindingPath.StartsWith("Logo"))
                {
                    var content = GetContentGridForColumn(column);
                    var logo = new Rectangle
                    {
                        VerticalAlignment = VerticalAlignment.Stretch,
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        Height = double.NaN,
                        Width = double.NaN
                    };
                    SetEventCommands(logo, column.ReadOnlyControlEventCommands);
                    if (!string.IsNullOrEmpty(column.DoubleClickCommandBindingPath)) SetDoubleClickCommands(logo, DataContext.GetPropertyValue<ICommand>(column.DoubleClickCommandBindingPath));
                    if (LogoStyle != null) logo.Style = LogoStyle;
                    logo.SetBinding(Shape.FillProperty, new Binding(column.BindingPath));
                    if (!string.IsNullOrEmpty(column.ToolTipBindingPath)) logo.SetBinding(ToolTipProperty, new Binding(column.ToolTipBindingPath));
                    else if (!string.IsNullOrEmpty(column.ToolTip)) logo.ToolTip = column.ToolTip;
                    if (!string.IsNullOrEmpty(column.ToolTipBindingPath) || !string.IsNullOrEmpty(column.ToolTip))
                    {
                        if (column.ToolTipInitialShowDelay > int.MinValue) ToolTipService.SetInitialShowDelay(logo, column.ToolTipInitialShowDelay);
                        if (column.ToolTipShowDuration > int.MinValue) ToolTipService.SetShowDuration(logo, column.ToolTipShowDuration);
                        if (column.ToolTipPlacement != PlacementMode.Mouse) ToolTipService.SetPlacement(logo, column.ToolTipPlacement);
                    }
                    content.Children.Add(logo);
                    Children.Add(content);
                    SetColumn(content, columnCounter);
                }
            }

            if (Columns.DetailTemplate != null)
            {
                var content = Columns.DetailTemplate.LoadContent() as FrameworkElement;
                if (content != null)
                {
                    DetailSpansFullWidth = Columns.DetailSpansFullWidth;
                    Children.Add(content);
                    SetIsDetail(content, true);
                    if (content.DataContext == null) content.DataContext = DataContext;
                    if (!string.IsNullOrEmpty(Columns.DetailExpandedPath))
                        SetBinding(DetailIsExpandedProperty, new Binding(Columns.DetailExpandedPath) {Source = content.DataContext, Mode = BindingMode.TwoWay});
                }
            }

            if (starColumnFound)
            {
                var presenter = ElementHelper.FindVisualTreeParent<ContentPresenter>(this);
                if (presenter != null)
                    presenter.HorizontalAlignment = HorizontalAlignment.Stretch;
            }
        }

        private static void SetHorizontalAlignment(FrameworkElement element, ListColumnContentAlignment desiredAlignment, HorizontalAlignment defaultAlignment = HorizontalAlignment.Left)
        {
            switch (desiredAlignment)
            {
                case ListColumnContentAlignment.Left:
                    element.HorizontalAlignment = HorizontalAlignment.Left;
                    break;
                case ListColumnContentAlignment.Center:
                    element.HorizontalAlignment = HorizontalAlignment.Center;
                    break;
                case ListColumnContentAlignment.Stretch:
                    element.HorizontalAlignment = HorizontalAlignment.Stretch;
                    break;
                case ListColumnContentAlignment.Right:
                    element.HorizontalAlignment = HorizontalAlignment.Right;
                    break;
                default:
                    element.HorizontalAlignment = defaultAlignment;
                    break;
            }
        }

        private static void SetHorizontalContentAlignment(Control element, ListColumnContentAlignment desiredAlignment, HorizontalAlignment defaultAlignment = HorizontalAlignment.Left)
        {
            switch (desiredAlignment)
            {
                case ListColumnContentAlignment.Left:
                    element.HorizontalContentAlignment = HorizontalAlignment.Left;
                    break;
                case ListColumnContentAlignment.Center:
                    element.HorizontalContentAlignment = HorizontalAlignment.Center;
                    break;
                case ListColumnContentAlignment.Stretch:
                    element.HorizontalContentAlignment = HorizontalAlignment.Stretch;
                    break;
                case ListColumnContentAlignment.Right:
                    element.HorizontalContentAlignment = HorizontalAlignment.Right;
                    break;
                default:
                    element.HorizontalContentAlignment = defaultAlignment;
                    break;
            }
        }
    }

    /// <summary>
    /// Converts double-lengths to grid lengths
    /// </summary>
    public class LengthToGridLengthConverter : IValueConverter
    {
        /// <summary>
        /// Converts a value.
        /// </summary>
        /// <param name="value">The value produced by the binding source.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>A converted value. If the method returns null, the valid null value is used.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var length = (double)value;
            return new GridLength(length);
        }

        /// <summary>
        /// Converts a value.
        /// </summary>
        /// <param name="value">The value that is produced by the binding target.</param>
        /// <param name="targetType">The type to convert to.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>A converted value. If the method returns null, the valid null value is used.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var length = (GridLength)value;
            return length.Value;
        }
    }

    /// <summary>
    /// Template selector used to create a different display value for a drop-down item in a combobox, vs. the text that is displayed in the box itself.
    /// </summary>
    /// <seealso cref="System.Windows.Controls.DataTemplateSelector" />
    public class ComboBoxSmartTemplateSelector : DataTemplateSelector
    {
        /// <summary>
        /// Item template used for items in the drop-down part of the combobox.
        /// </summary>
        public DataTemplate DropDownItemTemplate { get; set; }
        /// <summary>
        /// Item template used inline in the combobox.
        /// </summary>
        public DataTemplate DisplayItemTemplate { get; set; }

        /// <summary>
        /// When overridden in a derived class, returns a <see cref="T:System.Windows.DataTemplate" /> based on custom logic.
        /// </summary>
        /// <param name="item">The data object for which to select the template.</param>
        /// <param name="container">The data-bound object.</param>
        /// <returns>
        /// Returns a <see cref="T:System.Windows.DataTemplate" /> or null. The default value is null.
        /// </returns>
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var parent = container;

            // Search up the visual tree, stopping at either a ComboBox or a ComboBoxItem (or null).
            while (parent != null && !(parent is ComboBoxItem) && !(parent is ComboBox))
                parent = VisualTreeHelper.GetParent(parent);

            // If we found a ComboBoxItem, then this template is being used inside the drop-down part
            var inDropDown = (parent is ComboBoxItem);

            return inDropDown ? DropDownItemTemplate : DisplayItemTemplate;
        }
    }

    public class ComboBoxInlineTextBlock : TextBlock
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ComboBoxInlineTextBlock"/> class.
        /// </summary>
        public ComboBoxInlineTextBlock()
        {
            Loaded += (s, a) =>
            {
                var combo = ElementHelper.FindVisualTreeParent<ComboBox>(this);
                if (combo == null) return;

                //var itemsControl = ItemsControl.ItemsControlFromItemContainer(combo);
                //if (itemsControl == null) return;

                var columns = ListEx.GetColumns(combo);
                if (columns != null)
                    ContentBindingPath = columns.DefaultDisplayBindingPath;
            };
        }


        /// <summary>
        /// Binding path for the text.
        /// </summary>
        public string ContentBindingPath
        {
            get { return (string)GetValue(ContentBindingPathProperty); }
            set { SetValue(ContentBindingPathProperty, value); }
        }

        /// <summary>
        /// Binding path for the text.
        /// </summary>
        public static readonly DependencyProperty ContentBindingPathProperty = DependencyProperty.Register("ContentBindingPath", typeof(string), typeof(ComboBoxInlineTextBlock), new PropertyMetadata("", OnContentBindingPathChanged));

        private static void OnContentBindingPathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var text = d as ComboBoxInlineTextBlock;
            if (text == null) return;

            BindingOperations.ClearBinding(text, TextProperty);
            text.SetBinding(TextProperty, new Binding(text.ContentBindingPath));
        }
    }
}
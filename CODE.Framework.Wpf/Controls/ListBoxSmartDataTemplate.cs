using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;
using CODE.Framework.Core.Utilities.Extensions;
using CODE.Framework.Wpf.Layout;
using CODE.Framework.Wpf.Utilities;

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
                var item = FindAncestor<ListBoxItem>(this);
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
            if (EditModeChanged != null)
                EditModeChanged(this, new EventArgs());
        }

        /// <summary>Walks the visual tree to find the parent of a certain type</summary>
        /// <typeparam name="T">Type to search</typeparam>
        /// <param name="d">Object for which to find the ancestor</param>
        /// <returns>Object or null</returns>
        private static T FindAncestor<T>(DependencyObject d) where T : class
        {
            var parent = VisualTreeHelper.GetParent(d);
            if (parent == null) return null;
            if (parent is T) return parent as T;
            return FindAncestor<T>(parent);
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
            item.RepopulateColumns();
            var columns = args.NewValue as ListColumnsCollection;
            if (columns == null) return;
            columns.CollectionChanged += (o2, a) => item.RepopulateColumns();
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

            var item = FindAncestor<ListBoxItem>(listItem);
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
                {
                    grid.SetBinding(BackgroundProperty, backgroundBinding);
                    grid.Background = column.CellBackground;
                }
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
            foreach (var column in Columns)
            {
                columnCounter++;

                var gridColumn = new ColumnDefinition();
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
                                SetHorizontalAlignment(text, column.CellContentAlignment);
                                text.SetBinding(TextBlock.TextProperty, new Binding(column.BindingPath));
                                if (!string.IsNullOrEmpty(column.ToolTipBindingPath))
                                    text.SetBinding(ToolTipProperty, new Binding(column.ToolTipBindingPath));
                                else if (!string.IsNullOrEmpty(column.ToolTip))
                                    text.ToolTip = column.ToolTip;
                                if (EditMode == ListRowEditMode.Manual && IsManualEditEnabled)
                                    text.Visibility = Visibility.Collapsed;
                                if (column.CellForeground != null)
                                    text.Foreground = column.CellForeground;
                                else if (!string.IsNullOrEmpty(column.CellForegroundBindingPath))
                                    text.SetBinding(TextBlock.ForegroundProperty, new Binding(column.CellForegroundBindingPath));
                                content.Children.Add(text);
                            }
                            if (column.EditMode != ListRowEditMode.ReadOnly)
                            {
                                tb = new TextBox {VerticalAlignment = VerticalAlignment.Stretch, HorizontalAlignment = HorizontalAlignment.Stretch, VerticalContentAlignment = VerticalAlignment.Center};
                                SetEventCommands(tb, column.WriteControlEventCommands);
                                SetHorizontalContentAlignment(tb, column.CellContentAlignment, HorizontalAlignment.Stretch);
                                tb.SetBinding(TextBox.TextProperty, new Binding(string.IsNullOrEmpty(column.EditControlBindingPath) ? column.BindingPath : column.EditControlBindingPath) {UpdateSourceTrigger = column.EditControlUpdateSourceTrigger, StringFormat = column.EditControlStringFormat});
                                if (!string.IsNullOrEmpty(column.ToolTipBindingPath))
                                    tb.SetBinding(ToolTipProperty, new Binding(column.ToolTipBindingPath));
                                else if (!string.IsNullOrEmpty(column.ToolTip))
                                    tb.ToolTip = column.ToolTip;
                                if (EditMode == ListRowEditMode.Manual && IsManualEditEnabled)
                                    tb.Visibility = Visibility.Visible;
                                else if (column.EditMode == ListRowEditMode.ReadWriteAll)
                                    tb.Visibility = Visibility.Visible;
                                else
                                    tb.Visibility = Visibility.Collapsed;
                                if (column.EditTextBoxStyle != null)
                                    tb.Style = column.EditTextBoxStyle;
                                else
                                {
                                    tb.Padding = new Thickness(0);
                                    tb.BorderThickness = new Thickness(0);
                                }
                                if (column.CellForeground != null)
                                    tb.Foreground = column.CellForeground;
                                else if (!string.IsNullOrEmpty(column.CellForegroundBindingPath))
                                    tb.SetBinding(Control.ForegroundProperty, new Binding(column.CellForegroundBindingPath));
                                content.Children.Add(tb);
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
                            check.SetBinding(ToggleButton.IsCheckedProperty, new Binding(string.IsNullOrEmpty(column.EditControlBindingPath) ? column.BindingPath : column.EditControlBindingPath) { UpdateSourceTrigger = column.EditControlUpdateSourceTrigger, StringFormat = column.EditControlStringFormat });
                            if (!string.IsNullOrEmpty(column.ToolTipBindingPath))
                                check.SetBinding(ToolTipProperty, new Binding(column.ToolTipBindingPath));
                            else if (!string.IsNullOrEmpty(column.ToolTip))
                                check.ToolTip = column.ToolTip;
                            SetHorizontalAlignment(check, column.CellContentAlignment, HorizontalAlignment.Center);
                            if (column.EditCheckmarkStyle != null)
                                check.Style = column.EditCheckmarkStyle;
                            if (column.CellForeground != null)
                                check.Foreground = column.CellForeground;
                            else if (!string.IsNullOrEmpty(column.CellForegroundBindingPath))
                                check.SetBinding(Control.ForegroundProperty, new Binding(column.CellForegroundBindingPath));
                            content.Children.Add(check);
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
                            break;
                        case ListColumnControls.TextList:
                            TextBlock text2 = null;
                            ComboBox combo = null;
                            if (column.EditMode != ListRowEditMode.ReadWriteAll)
                            {
                                text2 = new TextBlock {VerticalAlignment = VerticalAlignment.Center};
                                SetEventCommands(text2, column.ReadOnlyControlEventCommands);
                                SetHorizontalAlignment(text2, column.CellContentAlignment);
                                if (!string.IsNullOrEmpty(column.ToolTipBindingPath))
                                    text2.SetBinding(ToolTipProperty, new Binding(column.ToolTipBindingPath));
                                else if (!string.IsNullOrEmpty(column.ToolTip))
                                    text2.ToolTip = column.ToolTip;
                                text2.SetBinding(TextBlock.TextProperty, new Binding(column.BindingPath));
                                if (EditMode == ListRowEditMode.Manual && IsManualEditEnabled)
                                    text2.Visibility = Visibility.Collapsed;
                                if (column.CellForeground != null)
                                    text2.Foreground = column.CellForeground;
                                else if (!string.IsNullOrEmpty(column.CellForegroundBindingPath))
                                    text2.SetBinding(TextBlock.ForegroundProperty, new Binding(column.CellForegroundBindingPath));
                                content.Children.Add(text2);
                            }
                            if (column.EditMode != ListRowEditMode.ReadOnly)
                            {
                                combo = new ComboBox {VerticalAlignment = VerticalAlignment.Stretch, HorizontalAlignment = HorizontalAlignment.Stretch, VerticalContentAlignment = VerticalAlignment.Center};
                                SetEventCommands(combo, column.WriteControlEventCommands);
                                SetHorizontalContentAlignment(combo, column.CellContentAlignment, HorizontalAlignment.Stretch);
                                if (!string.IsNullOrEmpty(column.BindingPath) || !string.IsNullOrEmpty(column.EditControlBindingPath))
                                    combo.SetBinding(Selector.SelectedValueProperty, new Binding(string.IsNullOrEmpty(column.EditControlBindingPath) ? column.BindingPath : column.EditControlBindingPath) {UpdateSourceTrigger = column.EditControlUpdateSourceTrigger, StringFormat = column.EditControlStringFormat});
                                combo.SetBinding(ItemsControl.ItemsSourceProperty, new Binding(column.TextListItemsSourceBindingPath));
                                combo.SelectedValuePath = column.TextListSelectedValuePath;
                                combo.DisplayMemberPath = column.TextListDisplayMemberPath;
                                if (!string.IsNullOrEmpty(column.ToolTipBindingPath))
                                    combo.SetBinding(ToolTipProperty, new Binding(column.ToolTipBindingPath));
                                else if (!string.IsNullOrEmpty(column.ToolTip))
                                    combo.ToolTip = column.ToolTip;
                                if (EditMode == ListRowEditMode.Manual && IsManualEditEnabled)
                                    combo.Visibility = Visibility.Visible;
                                else
                                    combo.Visibility = Visibility.Collapsed;
                                if (column.EditTextListStyle != null)
                                    combo.Style = column.EditTextListStyle;
                                else
                                {
                                    combo.Padding = new Thickness(0);
                                    combo.BorderThickness = new Thickness(0);
                                }
                                if (column.CellForeground != null)
                                    combo.Foreground = column.CellForeground;
                                else if (!string.IsNullOrEmpty(column.CellForegroundBindingPath))
                                    combo.SetBinding(Control.ForegroundProperty, new Binding(column.CellForegroundBindingPath));
                                content.Children.Add(combo);
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
                            image.SetBinding(Image.SourceProperty, new Binding(column.BindingPath) {Mode = BindingMode.OneWay});
                            if (!string.IsNullOrEmpty(column.ToolTipBindingPath))
                                image.SetBinding(ToolTipProperty, new Binding(column.ToolTipBindingPath));
                            else if (!string.IsNullOrEmpty(column.ToolTip))
                                image.ToolTip = column.ToolTip;
                            content.Children.Add(image);
                            break;
                    }

                    Children.Add(content);
                    SetColumn(content, columnCounter);
                }
                else if (column.BindingPath.StartsWith("Number"))
                {
                    var content = GetContentGridForColumn(column);
                    var text = new TextBlock {VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right, TextAlignment = TextAlignment.Right};
                    SetEventCommands(text, column.ReadOnlyControlEventCommands);
                    text.SetBinding(TextBlock.TextProperty, new Binding(column.BindingPath));
                    if (!string.IsNullOrEmpty(column.ToolTipBindingPath))
                        text.SetBinding(ToolTipProperty, new Binding(column.ToolTipBindingPath));
                    else if (!string.IsNullOrEmpty(column.ToolTip))
                        text.ToolTip = column.ToolTip;
                    if (column.CellForeground != null)
                        text.Foreground = column.CellForeground;
                    else if (!string.IsNullOrEmpty(column.CellForegroundBindingPath))
                        text.SetBinding(TextBlock.ForegroundProperty, new Binding(column.CellForegroundBindingPath));
                    content.Children.Add(text);
                    Children.Add(content);
                    SetColumn(content, columnCounter);
                }
                else if (column.BindingPath.StartsWith("Image") || column.BindingPath.StartsWith("Logo"))
                {
                    var content = GetContentGridForColumn(column);
                    var image = new Rectangle
                    {
                        VerticalAlignment = VerticalAlignment.Stretch,
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        Height = double.NaN,
                        Width = double.NaN
                    };
                    SetEventCommands(image, column.ReadOnlyControlEventCommands);
                    image.SetBinding(Shape.FillProperty, new Binding(column.BindingPath));
                    if (!string.IsNullOrEmpty(column.ToolTipBindingPath))
                        image.SetBinding(ToolTipProperty, new Binding(column.ToolTipBindingPath));
                    else if (!string.IsNullOrEmpty(column.ToolTip))
                        image.ToolTip = column.ToolTip;
                    content.Children.Add(image);
                    Children.Add(content);
                    SetColumn(content, columnCounter);
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
}
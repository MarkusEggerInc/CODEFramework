using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace CODE.Framework.Wpf.Controls
{
    /// <summary>Special features that can be attached to listboxes</summary>
    public class ListEx : DependencyObject
    {
        /// <summary>This attached property can be used to generically express the content of columns</summary>
        public static readonly DependencyProperty ColumnsProperty = DependencyProperty.RegisterAttached("Columns", typeof (ListColumnsCollection), typeof (ListEx), new FrameworkPropertyMetadata(null) {BindsTwoWayByDefault = false});
        /// <summary>Sets the columns.</summary>
        /// <param name="o">The DependencyObject to set the value on.</param>
        /// <param name="value">The value.</param>
        public static void SetColumns(DependencyObject o, ListColumnsCollection value)
        {
            o.SetValue(ColumnsProperty, value);
        }
        /// <summary>Gets the columns.</summary>
        /// <param name="o">The DependencyObject to get the value on.</param>
        public static ListColumnsCollection GetColumns(DependencyObject o)
        {
            return (ListColumnsCollection)o.GetValue(ColumnsProperty);
        }

        /// <summary>Defines whether header edit controls are to be displayed (if there are any defined)</summary>
        public static readonly DependencyProperty ShowHeaderEditControlsProperty = DependencyProperty.RegisterAttached("ShowHeaderEditControls", typeof(bool), typeof(ListEx), new PropertyMetadata(true));
        /// <summary>Defines whether header edit controls are to be displayed (if there are any defined)</summary>
        public static void SetShowHeaderEditControls(DependencyObject o, bool value)
        {
            o.SetValue(ShowHeaderEditControlsProperty, value);
        }
        /// <summary>Defines whether header edit controls are to be displayed (if there are any defined)</summary>
        public static bool GetShowHeaderEditControls(DependencyObject o)
        {
            return (bool)o.GetValue(ShowHeaderEditControlsProperty);
        }

        /// <summary>
        /// Setting this property to true on a ListBox automatically loads appropriate templates for the ListBox to support rows and columns
        /// </summary>
        public static readonly DependencyProperty AutoEnableListColumnsProperty = DependencyProperty.RegisterAttached("AutoEnableListColumns", typeof(bool), typeof(ListEx), new PropertyMetadata(false, OnAutoEnableListColumnsChanged));

        /// <summary>
        /// Fires when the AutoEnableListColumns property changed
        /// </summary>
        /// <param name="d">The dependency object (ListBox)</param>
        /// <param name="args">Event args.</param>
        private static void OnAutoEnableListColumnsChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            if (!((bool)args.NewValue)) return;
            var list = d as ListBox;
            if (list == null) return;

            var dictionaryFound = false;
            foreach (var resource in list.Resources.OfType<ResourceDictionary>())
                foreach (var dictionary in resource.MergedDictionaries)
                    if (dictionary.Source.AbsoluteUri.EndsWith("component/styles/MultiColumnList.xaml"))
                    {
                        dictionaryFound = true;
                        break;
                    }

            if (!dictionaryFound)
                list.Resources.MergedDictionaries.Add(new ResourceDictionary {Source = new Uri("pack://application:,,,/CODE.Framework.Wpf;component/styles/MultiColumnList.xaml", UriKind.Absolute)});

            list.SetResourceReference(FrameworkElement.StyleProperty, "CODE.Framework-MultiColumnList");
        }

        /// <summary>
        /// Setting this property to true on a ListBox automatically loads appropriate templates for the ListBox to support rows and columns
        /// </summary>
        public static void SetAutoEnableListColumns(DependencyObject d, bool value)
        {
            d.SetValue(AutoEnableListColumnsProperty, value);
        }
        /// <summary>
        /// Setting this property to true on a ListBox automatically loads appropriate templates for the ListBox to support rows and columns
        /// </summary>
        public static bool GetAutoEnableListColumns(DependencyObject d)
        {
            return (bool)d.GetValue(AutoEnableListColumnsProperty);
        }

        /// <summary>
        /// Indicates whether the league is to be forced into edit more (if set to true)
        /// </summary>
        public static readonly DependencyProperty IsEditableProperty = DependencyProperty.RegisterAttached("IsEditable", typeof(bool), typeof(ListEx), new PropertyMetadata(false));

        /// <summary>
        /// Indicates whether the league is to be forced into edit more (if set to true)
        /// </summary>
        /// <param name="d">The object to set the value on</param>
        /// <param name="value">True or false</param>
        public static void SetIsEditable(DependencyObject d, bool value)
        {
            d.SetValue(IsEditableProperty, value);
        }

        /// <summary>
        /// Indicates whether the league is to be forced into edit more (if set to true)
        /// </summary>
        /// <param name="d">The object to get the value for.</param>
        /// <returns>True or false</returns>
        public static bool GetIsEditable(DependencyObject d)
        {
            return (bool) d.GetValue(IsEditableProperty);
        }

        /// <summary>
        /// Defines how the list applies auto-filtering
        /// </summary>
        public static readonly DependencyProperty AutoFilterModeProperty = DependencyProperty.Register("AutoFilterMode", typeof(AutoFilterMode), typeof(ListEx), new PropertyMetadata(Controls.AutoFilterMode.OneColumnAtATime));

        /// <summary>
        /// Defines how the list applies auto-filtering
        /// </summary>
        /// <param name="d">The object to get the value for.</param>
        /// <param name="value">The new value to be set</param>
        public static void SetAutoFilterMode(DependencyObject d, AutoFilterMode value)
        {
            d.SetValue(AutoFilterModeProperty, value);
        }
        /// <summary>
        /// Defines how the list applies auto-filtering
        /// </summary>
        /// <param name="d">The object to get the value for.</param>
        /// <returns>True or false</returns>
        public static AutoFilterMode GetAutoFilterMode(DependencyObject d)
        {
            return (AutoFilterMode) d.GetValue(AutoFilterModeProperty);
        }

        /// <summary>
        /// For internal use only
        /// </summary>
        [Browsable(false)]
        public static readonly DependencyProperty InAutoFilteringProperty = DependencyProperty.RegisterAttached("InAutoFiltering", typeof(bool), typeof(ListEx), new PropertyMetadata(false));
        /// <summary>
        /// For internal use only
        /// </summary>
        /// <param name="d">The object to get the value for.</param>
        /// <param name="value">The new value to be set</param>
        [Browsable(false)]
        public static void SetInAutoFiltering(DependencyObject d, bool value)
        {
            d.SetValue(InAutoFilteringProperty, value);
        }
        /// <summary>
        /// For internal use only
        /// </summary>
        /// <param name="d">The object to get the value for.</param>
        /// <returns>True or false</returns>
        [Browsable(false)]
        public static bool GetInAutoFiltering(DependencyObject d)
        {
            return (bool)d.GetValue(InAutoFilteringProperty);
        }

        /// <summary>
        /// For internal use only
        /// </summary>
        [Browsable(false)]
        public static readonly DependencyProperty InAutoSortingProperty = DependencyProperty.RegisterAttached("InAutoSorting", typeof(bool), typeof(ListEx), new PropertyMetadata(false));
        /// <summary>
        /// For internal use only
        /// </summary>
        /// <param name="d">The object to get the value for.</param>
        /// <param name="value">The new value to be set</param>
        [Browsable(false)]
        public static void SetInAutoSorting(DependencyObject d, bool value)
        {
            d.SetValue(InAutoSortingProperty, value);
        }
        /// <summary>
        /// For internal use only
        /// </summary>
        /// <param name="d">The object to get the value for.</param>
        /// <returns>True or false</returns>
        [Browsable(false)]
        public static bool GetInAutoSorting(DependencyObject d)
        {
            return (bool)d.GetValue(InAutoSortingProperty);
        }
    }

    /// <summary>
    /// Defines the auto-filter mode of a list
    /// </summary>
    public enum AutoFilterMode
    {
        /// <summary>
        /// Filtering one column at a time. When a column is filtered while another column already
        /// has a filter applied, the old filter is removed
        /// </summary>
        OneColumnAtATime,
        /// <summary>
        /// Allows for filters on multiple columns at once. All the filters are combined with 
        /// an AND operation.
        /// </summary>
        AllColumnsAnd,
        /// <summary>
        /// Allows for filters on multiple columns at once. All the filters are combined with 
        /// an OR operation.
        /// </summary>
        AllColumnsOr
    }

    /// <summary>Observable collection of generic list columns</summary>
    [Serializable]
    public class ListColumnsCollection : ObservableCollection<ListColumn>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ListColumnsCollection"/> class.
        /// </summary>
        public ListColumnsCollection()
        {
            ShowGridLines = ListGridLineMode.Never;
        }

        /// <summary>
        /// Path to a boolean source that defines whether a row is editable or not
        /// </summary>
        public string EditModeBindingPath { get; set; }

        /// <summary>
        /// Defines whether and when grid lines shall be displayed
        /// </summary>
        public ListGridLineMode ShowGridLines { get; set; }
    }

    /// <summary>
    /// An abstract definition of a column
    /// </summary>
    public class ListColumn : DependencyObject
    {
        /// <summary>Column Header</summary>
        public object Header
        {
            get { return GetValue(HeaderProperty); }
            set { SetValue(HeaderProperty, value); }
        }
        /// <summary>Column Header</summary>
        public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register("Header", typeof(object), typeof(ListColumn), new UIPropertyMetadata(null));

        /// <summary>Defines whether a header label (text) shall be displayed</summary>
        /// <value>True (default) or false</value>
        public bool ShowColumnHeaderText
        {
            get { return (bool)GetValue(ShowColumnHeaderTextProperty); }
            set { SetValue(ShowColumnHeaderTextProperty, value); }
        }
        /// <summary>Defines whether a header label (text) shall be displayed</summary>
        /// <value>True (default) or false</value>
        public static readonly DependencyProperty ShowColumnHeaderTextProperty = DependencyProperty.Register("ShowColumnHeaderText", typeof(bool), typeof(ListColumn), new PropertyMetadata(true));

        /// <summary>Defines whether a header edit control (textbox) shall be displayed</summary>
        /// <value>True or false (default)</value>
        public bool ShowColumnHeaderEditControl
        {
            get { return (bool)GetValue(ShowColumnHeaderEditControlProperty); }
            set { SetValue(ShowColumnHeaderEditControlProperty, value); }
        }
        /// <summary>Defines whether a header edit control (textbox) shall be displayed</summary>
        /// <value>True or false (default)</value>
        public static readonly DependencyProperty ShowColumnHeaderEditControlProperty = DependencyProperty.Register("ShowColumnHeaderEditControl", typeof(bool), typeof(ListColumn), new PropertyMetadata(false));

        /// <summary>Binding path for a column header edit control</summary>
        /// <value>The column header edit control binding path.</value>
        public string ColumnHeaderEditControlBindingPath
        {
            get { return (string)GetValue(ColumnHeaderEditControlBindingPathProperty); }
            set { SetValue(ColumnHeaderEditControlBindingPathProperty, value); }
        }
        /// <summary>Binding path for a column header edit control</summary>
        /// <value>The column header edit control binding path.</value>
        public static readonly DependencyProperty ColumnHeaderEditControlBindingPathProperty = DependencyProperty.Register("ColumnHeaderEditControlBindingPath", typeof(string), typeof(ListColumn), new PropertyMetadata(""));

        /// <summary>Binding update trigger for a column header edit control</summary>
        /// <value>The column header edit control update trigger.</value>
        public UpdateSourceTrigger ColumnHeaderEditControlUpdateTrigger
        {
            get { return (UpdateSourceTrigger)GetValue(ColumnHeaderEditControlUpdateTriggerProperty); }
            set { SetValue(ColumnHeaderEditControlUpdateTriggerProperty, value); }
        }
        /// <summary>Binding update trigger for a column header edit control</summary>
        /// <value>The column header edit control update trigger.</value>
        public static readonly DependencyProperty ColumnHeaderEditControlUpdateTriggerProperty = DependencyProperty.Register("ColumnHeaderEditControlUpdateTrigger", typeof(UpdateSourceTrigger), typeof(ListColumn), new PropertyMetadata(UpdateSourceTrigger.Default));

        /// <summary>Watermark text for a potential header control</summary>
        /// <value>The column header edit control watermark text.</value>
        public string ColumnHeaderEditControlWatermarkText
        {
            get { return (string)GetValue(ColumnHeaderEditControlWatermarkTextProperty); }
            set { SetValue(ColumnHeaderEditControlWatermarkTextProperty, value); }
        }
        /// <summary>Watermark text for a potential header control</summary>
        /// <value>The column header edit control watermark text.</value>
        public static readonly DependencyProperty ColumnHeaderEditControlWatermarkTextProperty = DependencyProperty.Register("ColumnHeaderEditControlWatermarkText", typeof(string), typeof(ListColumn), new PropertyMetadata(""));

        /// <summary>Column Width</summary>
        public GridLength Width
        {
            get { return (GridLength)GetValue(WidthProperty); }
            set { SetValue(WidthProperty, value); }
        }
        /// <summary>Column Width</summary>
        public static readonly DependencyProperty WidthProperty = DependencyProperty.Register("Width", typeof(GridLength), typeof(ListColumn), new UIPropertyMetadata(new GridLength(100d), OnWidthChanged));

        /// <summary>
        /// Fires when the column width changes
        /// </summary>
        /// <param name="d">Column object</param>
        /// <param name="args">Event arguments</param>
        private static void OnWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            var column = d as ListColumn;
            if (column == null) return;

            if (column.WidthChanged != null)
                column.WidthChanged(column, new EventArgs());
        }

        /// <summary>
        /// Occurs when the column width changes
        /// </summary>
        public event EventHandler WidthChanged;

        /// <summary>For internal use only</summary>
        public double ActualWidth
        {
            get { return (double)GetValue(ActualWidthProperty); }
            set { SetValue(ActualWidthProperty, value); }
        }
        /// <summary>For internal use only</summary>
        public static readonly DependencyProperty ActualWidthProperty = DependencyProperty.Register("ActualWidth", typeof(double), typeof(ListColumn), new PropertyMetadata(-1d));

        /// <summary>Path the column is bound to</summary>
        public string BindingPath
        {
            get { return (string)GetValue(BindingPathProperty); }
            set { SetValue(BindingPathProperty, value); }
        }
        /// <summary>Path expression the column is bound to (used as a standard binding expression into each item's data context</summary>
        public static readonly DependencyProperty BindingPathProperty = DependencyProperty.Register("BindingPath", typeof(string), typeof(ListColumn), new UIPropertyMetadata(""));

        /// <summary>Binding path used for edit control (if empty, the regular BindingPath property applies)</summary>
        /// <value>The edit control binding path.</value>
        public string EditControlBindingPath
        {
            get { return (string)GetValue(EditControlBindingPathProperty); }
            set { SetValue(EditControlBindingPathProperty, value); }
        }
        /// <summary>Binding path used for edit control (if empty, the regular BindingPath property applies)</summary>
        /// <value>The edit control binding path.</value>
        public static readonly DependencyProperty EditControlBindingPathProperty = DependencyProperty.Register("EditControlBindingPath", typeof(string), typeof(ListColumn), new PropertyMetadata(""));

        /// <summary>Defines when the binding of edit controls triggers an update</summary>
        /// <value>The edit control update source trigger.</value>
        public UpdateSourceTrigger EditControlUpdateSourceTrigger
        {
            get { return (UpdateSourceTrigger)GetValue(EditControlUpdateSourceTriggerProperty); }
            set { SetValue(EditControlUpdateSourceTriggerProperty, value); }
        }
        /// <summary>Defines when the binding of edit controls triggers an update</summary>
        /// <value>The edit control update source trigger.</value>
        public static readonly DependencyProperty EditControlUpdateSourceTriggerProperty = DependencyProperty.Register("EditControlUpdateSourceTrigger", typeof(UpdateSourceTrigger), typeof(ListColumn), new PropertyMetadata(UpdateSourceTrigger.Default));

        /// <summary>Binding StringFormat for the edit control binding</summary>
        /// <value>The edit control string format.</value>
        public string EditControlStringFormat
        {
            get { return (string)GetValue(EditControlStringFormatProperty); }
            set { SetValue(EditControlStringFormatProperty, value); }
        }
        /// <summary>Binding StringFormat for the edit control binding</summary>
        /// <value>The edit control string format.</value>
        public static readonly DependencyProperty EditControlStringFormatProperty = DependencyProperty.Register("EditControlStringFormat", typeof(string), typeof(ListColumn), new PropertyMetadata(""));

        /// <summary>Item template used for the column</summary>
        public DataTemplate ItemTemplate
        {
            get { return (DataTemplate)GetValue(ItemTemplateProperty); }
            set { SetValue(ItemTemplateProperty, value); }
        }
        /// <summary>Item template used for the column</summary>
        public static readonly DependencyProperty ItemTemplateProperty = DependencyProperty.Register("ItemTemplate", typeof(DataTemplate), typeof(ListColumn), new UIPropertyMetadata(null));

        /// <summary>Template used for column headers</summary>
        public ControlTemplate HeaderTemplate
        {
            get { return (ControlTemplate)GetValue(HeaderTemplateProperty); }
            set { SetValue(HeaderTemplateProperty, value); }
        }
        /// <summary>Template used for column headers</summary>
        public static readonly DependencyProperty HeaderTemplateProperty = DependencyProperty.Register("HeaderTemplate", typeof(ControlTemplate), typeof(ListColumn), new PropertyMetadata(null));

        /// <summary>Defines whether the column is resizable</summary>
        public bool IsResizable
        {
            get { return (bool)GetValue(IsResizableProperty); }
            set { SetValue(IsResizableProperty, value); }
        }
        /// <summary>Defines whether the column is resizable</summary>
        public static readonly DependencyProperty IsResizableProperty = DependencyProperty.Register("IsResizable", typeof(bool), typeof(ListColumn), new UIPropertyMetadata(true));

        /// <summary>Defines whether a vertical header line shall be shown to indicate the size of the header</summary>
        /// <value><c>true</c> if line is to be displayed; otherwise, <c>false</c>.</value>
        public bool ShowHeaderGridLine
        {
            get { return (bool)GetValue(ShowHeaderGridLineProperty); }
            set { SetValue(ShowHeaderGridLineProperty, value); }
        }
        /// <summary>Defines whether a vertical header line shall be shown to indicate the size of the header</summary>
        /// <value><c>true</c> if line is to be displayed; otherwise, <c>false</c>.</value>
        public static readonly DependencyProperty ShowHeaderGridLineProperty = DependencyProperty.Register("ShowHeaderGridLine", typeof(bool), typeof(ListColumn), new PropertyMetadata(true));

        /// <summary>Style to be used for header grid lines</summary>
        /// <value>The header grid line style.</value>
        /// <remarks>Header grid line objects are Border objects that wrap the entire header</remarks>
        public Style HeaderGridLineStyle
        {
            get { return (Style)GetValue(HeaderGridLineStyleProperty); }
            set { SetValue(HeaderGridLineStyleProperty, value); }
        }
        /// <summary>Style to be used for header grid lines</summary>
        /// <value>The header grid line style.</value>
        /// <remarks>Header grid line objects are Border objects that wrap the entire header</remarks>
        public static readonly DependencyProperty HeaderGridLineStyleProperty = DependencyProperty.Register("HeaderGridLineStyle", typeof(Style), typeof(ListColumn), new PropertyMetadata(null));

        /// <summary>Binding path to bind a header click command</summary>
        /// <remarks>Note that standard binding would not be applicable int his case, since column headers do not exist within the visual tree structure that would provide the standard data context</remarks>
        public string HeaderClickCommandBindingPath
        {
            get { return (string)GetValue(HeaderClickCommandBindingPathProperty); }
            set { SetValue(HeaderClickCommandBindingPathProperty, value); }
        }
        /// <summary>Binding path to bind a header click command</summary>
        /// <remarks>Note that standard binding would not be applicable int his case, since column headers do not exist within the visual tree structure that would provide the standard data context</remarks>
        public static readonly DependencyProperty HeaderClickCommandBindingPathProperty = DependencyProperty.Register("HeaderClickCommandBindingPath", typeof(string), typeof(ListColumn), new PropertyMetadata(null));

        /// <summary>Background brush for individual cells</summary>
        /// <value>The cell background.</value>
        /// <remarks>Only applies for columns without item templates. Note that in order to data bind individual row background colors, the CellBackgroundBindingPath property has to be used.</remarks>
        public Brush CellBackground
        {
            get { return (Brush)GetValue(CellBackgroundProperty); }
            set { SetValue(CellBackgroundProperty, value); }
        }
        /// <summary>Background brush for individual cells</summary>
        /// <value>The cell background.</value>
        /// <remarks>Only applies for columns without item templates. Note that in order to data bind individual row background colors, the CellBackgroundBindingPath property has to be used.</remarks>
        public static readonly DependencyProperty CellBackgroundProperty = DependencyProperty.Register("CellBackground", typeof(Brush), typeof(ListColumn), new PropertyMetadata(null));

        /// <summary>Defines a binding path for individual cell background colors.</summary>
        /// <value>The cell background binding path.</value>
        /// <remarks>It is not possible to just bind the CellBackground property using standard WPF binding, as that would bind the generic definition of the cell, not each actual cell in the list. Using the binding path property, a new binding to the path will be created for each item.</remarks>
        public string CellBackgroundBindingPath
        {
            get { return (string)GetValue(CellBackgroundBindingPathProperty); }
            set { SetValue(CellBackgroundBindingPathProperty, value); }
        }
        /// <summary>Defines a binding path for individual cell background colors.</summary>
        /// <value>The cell background binding path.</value>
        /// <remarks>It is not possible to just bind the CellBackground property using standard WPF binding, as that would bind the generic definition of the cell, not each actual cell in the list. Using the binding path property, a new binding to the path will be created for each item.</remarks>
        public static readonly DependencyProperty CellBackgroundBindingPathProperty = DependencyProperty.Register("CellBackgroundBindingPath", typeof(string), typeof(ListColumn), new PropertyMetadata(""));
        
        /// <summary>Defines the background opacity of the cell</summary>
        /// <value>The cell background opacity.</value>
        /// <remarks>Only applies if the CellBackground property is not null.</remarks>
        public double CellBackgroundOpacity
        {
            get { return (double)GetValue(CellBackgroundOpacityProperty); }
            set { SetValue(CellBackgroundOpacityProperty, value); }
        }
        /// <summary>Defines the background opacity of the cell</summary>
        /// <value>The cell background opacity.</value>
        /// <remarks>Only applies if the CellBackground property is not null</remarks>
        public static readonly DependencyProperty CellBackgroundOpacityProperty = DependencyProperty.Register("CellBackgroundOpacity", typeof(double), typeof(ListColumn), new PropertyMetadata(1d));

        /// <summary>
        /// Defines the foreground color for the cell
        /// </summary>
        public Brush CellForeground
        {
            get { return (Brush)GetValue(CellForegroundProperty); }
            set { SetValue(CellForegroundProperty, value); }
        }
        /// <summary>
        /// Defines the foreground color for the cell
        /// </summary>
        public static readonly DependencyProperty CellForegroundProperty = DependencyProperty.Register("CellForeground", typeof(Brush), typeof(ListColumn), new PropertyMetadata(null));

        /// <summary>Defines a binding path for individual cell foreground colors.</summary>
        /// <value>The cell foreground binding path.</value>
        /// <remarks>It is not possible to just bind the CellForeground property using standard WPF binding, as that would bind the generic definition of the cell, not each actual cell in the list. Using the binding path property, a new binding to the path will be created for each item.</remarks>
        public string CellForegroundBindingPath
        {
            get { return (string)GetValue(CellForegroundBindingPathProperty); }
            set { SetValue(CellForegroundBindingPathProperty, value); }
        }
        /// <summary>Defines a binding path for individual cell foreground colors.</summary>
        /// <value>The cell foreground binding path.</value>
        /// <remarks>It is not possible to just bind the CellForeground property using standard WPF binding, as that would bind the generic definition of the cell, not each actual cell in the list. Using the binding path property, a new binding to the path will be created for each item.</remarks>
        public static readonly DependencyProperty CellForegroundBindingPathProperty = DependencyProperty.Register("CellForegroundBindingPath", typeof(string), typeof(ListColumn), new PropertyMetadata(""));

        /// <summary>Defines which type of control the column should be using</summary>
        /// <value>The column controls.</value>
        /// <remarks>Only applies for columns without item templates</remarks>
        public ListColumnControls ColumnControl
        {
            get { return (ListColumnControls)GetValue(ColumnControlProperty); }
            set { SetValue(ColumnControlProperty, value); }
        }
        /// <summary>Defines which type of control the column should be using</summary>
        /// <value>The column controls.</value>
        /// <remarks>Only applies for columns without item templates</remarks>
        public static readonly DependencyProperty ColumnControlProperty = DependencyProperty.Register("ColumnControl", typeof(ListColumnControls), typeof(ListColumn), new PropertyMetadata(ListColumnControls.Auto));

        /// <summary>Content alignment for cell content</summary>
        /// <value>The cell content alignment.</value>
        public ListColumnContentAlignment CellContentAlignment
        {
            get { return (ListColumnContentAlignment)GetValue(CellContentAlignmentProperty); }
            set { SetValue(CellContentAlignmentProperty, value); }
        }
        /// <summary>Content alignment for cell content</summary>
        public static readonly DependencyProperty CellContentAlignmentProperty = DependencyProperty.Register("CellContentAlignment", typeof(ListColumnContentAlignment), typeof(ListColumn), new PropertyMetadata(ListColumnContentAlignment.Default));

        /// <summary>Text alignment for simple text headers</summary>
        public TextAlignment HeaderTextAlignment
        {
            get { return (TextAlignment)GetValue(HeaderTextAlignmentProperty); }
            set { SetValue(HeaderTextAlignmentProperty, value); }
        }
        /// <summary>Text alignment for simple text headers</summary>
        public static readonly DependencyProperty HeaderTextAlignmentProperty = DependencyProperty.Register("HeaderTextAlignment", typeof(TextAlignment), typeof(ListColumn), new PropertyMetadata(TextAlignment.Left));

        /// <summary>
        /// Gets or sets the header foreground color/brush.
        /// </summary>
        /// <value>The header foreground.</value>
        /// <remarks>Ignored when initially null</remarks>
        public Brush HeaderForeground
        {
            get { return (Brush)GetValue(HeaderForegroundProperty); }
            set { SetValue(HeaderForegroundProperty, value); }
        }
        /// <summary>
        /// Gets or sets the header foreground color/brush.
        /// </summary>
        public static readonly DependencyProperty HeaderForegroundProperty = DependencyProperty.Register("HeaderForeground", typeof(Brush), typeof(ListColumn), new PropertyMetadata(null));

        /// <summary>Binding path expression used for the list (such as a combobox) of a text list hosted control</summary>
        /// <value>The text list item source binding path.</value>
        public string TextListItemsSourceBindingPath
        {
            get { return (string)GetValue(TextListItemsSourceBindingPathProperty); }
            set { SetValue(TextListItemsSourceBindingPathProperty, value); }
        }
        /// <summary>Binding path expression used for the list (such as a combobox) of a text list hosted control</summary>
        /// <value>The text list item source binding path.</value>
        public static readonly DependencyProperty TextListItemsSourceBindingPathProperty = DependencyProperty.Register("TextListItemsSourceBindingPath", typeof(string), typeof(ListColumn), new PropertyMetadata(""));

        /// <summary>Display member binding path path expression used for the list (such as a combobox) of a text list hosted control</summary>
        /// <value>The text list display member path.</value>
        public string TextListDisplayMemberPath
        {
            get { return (string)GetValue(TextListDisplayMemberPathProperty); }
            set { SetValue(TextListDisplayMemberPathProperty, value); }
        }
        /// <summary>Display member binding path path expression used for the list (such as a combobox) of a text list hosted control</summary>
        /// <value>The text list display member path.</value>
        public static readonly DependencyProperty TextListDisplayMemberPathProperty = DependencyProperty.Register("TextListDisplayMemberPath", typeof(string), typeof(ListColumn), new PropertyMetadata(""));

        /// <summary>Selected value binding path path expression used for the list (such as a combobox) of a text list hosted control</summary>
        /// <value>The text list selected value path.</value>
        public string TextListSelectedValuePath
        {
            get { return (string)GetValue(TextListSelectedValuePathProperty); }
            set { SetValue(TextListSelectedValuePathProperty, value); }
        }
        /// <summary>Selected value binding path path expression used for the list (such as a combobox) of a text list hosted control</summary>
        /// <value>The text list selected value path.</value>
        public static readonly DependencyProperty TextListSelectedValuePathProperty = DependencyProperty.Register("TextListSelectedValuePath", typeof(string), typeof(ListColumn), new PropertyMetadata(""));

        /// <summary>Defines an (optional) style that can be applied to image previews when the column control is set to ImageWithPreview</summary>
        /// <value>The image preview style.</value>
        /// <remarks>The base object for the preview is an Image element</remarks>
        public Style ImagePreviewStyle
        {
            get { return (Style)GetValue(ImagePreviewStyleProperty); }
            set { SetValue(ImagePreviewStyleProperty, value); }
        }
        /// <summary>Defines an (optional) style that can be applied to image previews when the column control is set to ImageWithPreview</summary>
        /// <value>The image preview style.</value>
        /// <remarks>The base object for the preview is an Image element</remarks>
        public static readonly DependencyProperty ImagePreviewStyleProperty = DependencyProperty.Register("ImagePreviewStyle", typeof(Style), typeof(ListColumn), new PropertyMetadata(null));

        /// <summary>Padding for the cell's content</summary>
        /// <value>The cell padding.</value>
        public Thickness CellPadding
        {
            get { return (Thickness)GetValue(CellPaddingProperty); }
            set { SetValue(CellPaddingProperty, value); }
        }
        /// <summary>Padding for the cell's content</summary>
        /// <value>The cell padding.</value>
        public static readonly DependencyProperty CellPaddingProperty = DependencyProperty.Register("CellPadding", typeof(Thickness), typeof(ListColumn), new PropertyMetadata(new Thickness(0d)));

        /// <summary>Cell content width. If set, forces the cell to explicitly take on a certain width. (This can be especially useful for images)</summary>
        /// <value>The width of the cell content.</value>
        public double CellContentWidth
        {
            get { return (double)GetValue(CellContentWidthProperty); }
            set { SetValue(CellContentWidthProperty, value); }
        }
        /// <summary>Cell content width. If set, forces the cell to explicitly take on a certain width. (This can be especially useful for images)</summary>
        /// <value>The width of the cell content.</value>
        public static readonly DependencyProperty CellContentWidthProperty = DependencyProperty.Register("CellContentWidth", typeof(double), typeof(ListColumn), new PropertyMetadata(double.NaN));

        /// <summary>Cell content height. If set, forces the cell to explicitly take on a certain height. (This can be especially useful for images)</summary>
        /// <value>The height of the cell content.</value>
        public double CellContentHeight
        {
            get { return (double)GetValue(CellContentHeightProperty); }
            set { SetValue(CellContentHeightProperty, value); }
        }
        /// <summary>Cell content height. If set, forces the cell to explicitly take on a certain height. (This can be especially useful for images)</summary>
        /// <value>The height of the cell content.</value>
        public static readonly DependencyProperty CellContentHeightProperty = DependencyProperty.Register("CellContentHeight", typeof(double), typeof(ListColumn), new PropertyMetadata(double.NaN));

        /// <summary>Defines the edit mode for the cell</summary>
        /// <value>The edit mode.</value>
        public ListRowEditMode EditMode
        {
            get { return (ListRowEditMode)GetValue(EditModeProperty); }
            set { SetValue(EditModeProperty, value); }
        }
        /// <summary>Defines the edit mode for the cell</summary>
        /// <value>The edit mode.</value>
        public static readonly DependencyProperty EditModeProperty = DependencyProperty.Register("EditMode", typeof(ListRowEditMode), typeof(ListColumn), new PropertyMetadata(ListRowEditMode.ReadOnly));

        /// <summary>Style used for text edit textboxes</summary>
        /// <value>The edit text box style.</value>
        public Style EditTextBoxStyle
        {
            get { return (Style)GetValue(EditTextBoxStyleProperty); }
            set { SetValue(EditTextBoxStyleProperty, value); }
        }
        /// <summary>Style used for text edit textboxes</summary>
        /// <value>The edit text box style.</value>
        public static readonly DependencyProperty EditTextBoxStyleProperty = DependencyProperty.Register("EditTextBoxStyle", typeof(Style), typeof(ListColumn), new PropertyMetadata(null));

        /// <summary>Style used for checkmark edit checkboxes</summary>
        /// <value>The edit checkmark style.</value>
        public Style EditCheckmarkStyle
        {
            get { return (Style)GetValue(EditCheckmarkStyleProperty); }
            set { SetValue(EditCheckmarkStyleProperty, value); }
        }
        /// <summary>Style used for checkmark edit checkboxes</summary>
        /// <value>The edit checkmark style.</value>
        public static readonly DependencyProperty EditCheckmarkStyleProperty = DependencyProperty.Register("EditCheckmarkStyle", typeof(Style), typeof(ListColumn), new PropertyMetadata(null));

        /// <summary>Style used for tex list edit controls</summary>
        /// <value>The edit text list style.</value>
        public Style EditTextListStyle
        {
            get { return (Style)GetValue(EditTextListStyleProperty); }
            set { SetValue(EditTextListStyleProperty, value); }
        }
        /// <summary>Style used for tex list edit controls</summary>
        /// <value>The edit text list style.</value>
        public static readonly DependencyProperty EditTextListStyleProperty = DependencyProperty.Register("EditTextListStyle", typeof(Style), typeof(ListColumn), new PropertyMetadata(null));

        /// <summary>Defines the brushed to be used to draw grid lines</summary>
        /// <value>The grid line brush.</value>
        public Brush GridLineBrush
        {
            get { return (Brush)GetValue(GridLineBrushProperty); }
            set { SetValue(GridLineBrushProperty, value); }
        }
        /// <summary>Defines the brushed to be used to draw grid lines</summary>
        /// <value>The grid line brush.</value>
        public static readonly DependencyProperty GridLineBrushProperty = DependencyProperty.Register("GridLineBrush", typeof(Brush), typeof(ListColumn), new PropertyMetadata(new SolidColorBrush(Colors.Silver)));

        /// <summary>Collection of event commands associated with the control that is used to display cell data in read-only fashion</summary>
        /// <value>The read only control event commands collection.</value>
        public EventCommandsCollection ReadOnlyControlEventCommands
        {
            get { return (EventCommandsCollection)GetValue(ReadOnlyControlEventCommandsProperty); }
            set { SetValue(ReadOnlyControlEventCommandsProperty, value); }
        }
        /// <summary>Collection of event commands associated with the control that is used to display cell data in read-only fashion</summary>
        /// <value>The read only control event commands collection.</value>
        public static readonly DependencyProperty ReadOnlyControlEventCommandsProperty = DependencyProperty.Register("ReadOnlyControlEventCommands", typeof(EventCommandsCollection), typeof(ListColumn), new PropertyMetadata(new EventCommandsCollection()));

        /// <summary>Collection of event commands associated with the control that is used to display cell data for editing</summary>
        /// <value>The read only control event commands collection.</value>
        public EventCommandsCollection WriteControlEventCommands
        {
            get { return (EventCommandsCollection)GetValue(WriteControlEventCommandsProperty); }
            set { SetValue(WriteControlEventCommandsProperty, value); }
        }
        /// <summary>Collection of event commands associated with the control that is used to display cell data for editing</summary>
        /// <value>The read only control event commands collection.</value>
        public static readonly DependencyProperty WriteControlEventCommandsProperty = DependencyProperty.Register("WriteControlEventCommands", typeof(EventCommandsCollection), typeof(ListColumn), new PropertyMetadata(new EventCommandsCollection()));

        /// <summary>Collection of event commands associated with the header control</summary>
        /// <value>The header event commands.</value>
        public EventCommandsCollection HeaderEventCommands
        {
            get { return (EventCommandsCollection)GetValue(HeaderEventCommandsProperty); }
            set { SetValue(HeaderEventCommandsProperty, value); }
        }
        /// <summary>Collection of event commands associated with the header control</summary>
        /// <value>The header event commands.</value>
        public static readonly DependencyProperty HeaderEventCommandsProperty = DependencyProperty.Register("HeaderEventCommands", typeof(EventCommandsCollection), typeof(ListColumn), new PropertyMetadata(new EventCommandsCollection()));

        /// <summary>Column sort order indicator</summary>
        /// <value>The sort order indicator.</value>
        /// <remarks>Note that setting this value does NOT actually sort the bound data unless AutoSort = true. It simply creates a visual indicator showing that the column is sorted.</remarks>
        public SortOrder SortOrder
        {
            get { return (SortOrder)GetValue(SortOrderProperty); }
            set { SetValue(SortOrderProperty, value); }
        }
        /// <summary>Column sort order indicator</summary>
        /// <value>The sort order indicator.</value>
        /// <remarks>Note that setting this value does NOT actually sort the bound data unless AutoSort = true. It simply creates a visual indicator showing that the column is sorted.</remarks>
        public static readonly DependencyProperty SortOrderProperty = DependencyProperty.Register("SortOrder", typeof(SortOrder), typeof(ListColumn), new PropertyMetadata(SortOrder.Unsorted));

        /// <summary>Indicates whether the list should automatically support sorting by this column</summary>
        public bool AutoSort
        {
            get { return (bool)GetValue(AutoSortProperty); }
            set { SetValue(AutoSortProperty, value); }
        }
        /// <summary>Indicates whether the list should automatically support sorting by this column</summary>
        public static readonly DependencyProperty AutoSortProperty = DependencyProperty.Register("AutoSort", typeof(bool), typeof(ListColumn), new PropertyMetadata(false));

        /// <summary>Indicates whether the list should automatically support filtering by this column</summary>
        public bool AutoFilter
        {
            get { return (bool)GetValue(AutoFilterProperty); }
            set { SetValue(AutoFilterProperty, value); }
        }
        /// <summary>Indicates whether the list should automatically support filtering by this column</summary>
        public static readonly DependencyProperty AutoFilterProperty = DependencyProperty.Register("AutoFilter", typeof(bool), typeof(ListColumn), new PropertyMetadata(false));

        /// <summary>
        /// Indicates the mode of the filter operation
        /// </summary>
        public FilterMode FilterMode
        {
            get { return (FilterMode)GetValue(FilterModeProperty); }
            set { SetValue(FilterModeProperty, value); }
        }

        /// <summary>
        /// Indicates the mode of the filter operation
        /// </summary>
        public static readonly DependencyProperty FilterModeProperty = DependencyProperty.Register("FilterMode", typeof(FilterMode), typeof(ListColumn), new PropertyMetadata(FilterMode.ContainedString));

        /// <summary>Binding path for a dynamically set sort order</summary>
        /// <value>The sort order binding path.</value>
        public string SortOrderBindingPath
        {
            get { return (string)GetValue(SortOrderBindingPathProperty); }
            set { SetValue(SortOrderBindingPathProperty, value); }
        }

        /// <summary>Binding path for a dynamically set sort order</summary>
        /// <remarks>Overrules the Tooltip property.</remarks>
        /// <value>The sort order binding path.</value>
        public static readonly DependencyProperty SortOrderBindingPathProperty = DependencyProperty.Register("SortOrderBindingPath", typeof(string), typeof(ListColumn), new PropertyMetadata(""));

        /// <summary>Binding path for a hardcoded tooltip of the control inside a column</summary>
        /// <remarks>Overrules the Tooltip property.</remarks>
        /// <value>The tooltip.</value>
        public string ToolTip
        {
            get { return (string)GetValue(ToolTipProperty); }
            set { SetValue(ToolTipProperty, value); }
        }

        /// <summary>Binding path for a hardcoded tooltip of the control inside a column</summary>
        /// <remarks>If TooltipBindingPath is set, it overrules this property</remarks>
        /// <value>The tooltip.</value>
        public static readonly DependencyProperty ToolTipProperty = DependencyProperty.Register("ToolTip", typeof(string), typeof(ListColumn), new PropertyMetadata(""));

        /// <summary>Binding path for a dynamically set tooltip of the control inside a column</summary>
        /// <remarks>If TooltipBindingPath is set, it overrules this property</remarks>
        /// <value>The tooltip binding path.</value>
        public string ToolTipBindingPath
        {
            get { return (string)GetValue(ToolTipBindingPathProperty); }
            set { SetValue(ToolTipBindingPathProperty, value); }
        }

        /// <summary>Binding path for a dynamically set tooltip of the control inside a column</summary>
        /// <value>The tooltip binding path.</value>
        public static readonly DependencyProperty ToolTipBindingPathProperty = DependencyProperty.Register("ToolTipBindingPath", typeof(string), typeof(ListColumn), new PropertyMetadata(""));

        /// <summary>Indicates whether the column is considered to be "frozen"</summary>
        /// <remarks>Frozen status usually indicates that all frozen columns are kept visible on the left side of the listbox. Note that different controls and styles may interpret this property differently.</remarks>
        public bool IsFrozen
        {
            get { return (bool)GetValue(IsFrozenProperty); }
            set { SetValue(IsFrozenProperty, value); }
        }

        /// <summary>Indicates whether the column is considered to be "frozen"</summary>
        /// <remarks>Frozen status usually indicates that all frozen columns are kept visible on the left side of the listbox. Note that different controls and styles may interpret this property differently.</remarks>
        public static readonly DependencyProperty IsFrozenProperty = DependencyProperty.Register("IsFrozen", typeof(bool), typeof(ListColumn), new PropertyMetadata(false));

        /// <summary>
        /// Gets or sets the visible of the column.
        /// </summary>
        /// <value>The visible.</value>
        public Visibility Visibility
        {
            get { return (Visibility)GetValue(VisibilityProperty); }
            set { SetValue(VisibilityProperty, value); }
        }

        /// <summary>
        /// Template for control header edit controls
        /// </summary>
        public DataTemplate ColumnHeaderEditControlTemplate
        {
            get { return (DataTemplate)GetValue(ColumnHeaderEditControlTemplateProperty); }
            set { SetValue(ColumnHeaderEditControlTemplateProperty, value); }
        }
        /// <summary>
        /// Template for control header edit controls
        /// </summary>
        public static readonly DependencyProperty ColumnHeaderEditControlTemplateProperty = DependencyProperty.Register("ColumnHeaderEditControlTemplate", typeof(DataTemplate), typeof(ListEx), new PropertyMetadata(null));

        /// <summary>For internal use only</summary>
        [Browsable(false)]
        public FrameworkElement UtilizedHeaderEditControl { get; set; }

        /// <summary>
        /// Gets or sets the column header edit control data context.
        /// </summary>
        /// <value>The column header edit control data context.</value>
        public object ColumnHeaderEditControlDataContext { get; set; }

        /// <summary>
        /// Gets or sets the visible of the column.
        /// </summary>
        public static readonly DependencyProperty VisibilityProperty = DependencyProperty.Register("Visibility", typeof(Visibility), typeof(ListColumn), new PropertyMetadata(Visibility.Visible, OnVisibilityChanged));

        /// <summary>
        /// Occurs when the visibility of the column changes
        /// </summary>
        /// <param name="d">ListColumn</param>
        /// <param name="args">Event args</param>
        private static void OnVisibilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            var column = d as ListColumn;
            if (column == null) return;

            if (column.VisibilityChanged != null)
                column.VisibilityChanged(column, new EventArgs());
        }

        /// <summary>
        /// Occurs when the column width changes
        /// </summary>
        public event EventHandler VisibilityChanged;

        /// <summary>
        /// Sets the actual width if the new value is indeed significantly different from the original color (more than 1/10th of a pixel difference).
        /// </summary>
        /// <param name="actualWidth">The actual width.</param>
        public void SetActualWidth(double actualWidth)
        {
            if (actualWidth > 0 && Math.Abs(ActualWidth - actualWidth) > .01)
                ActualWidth = actualWidth;
        }
    }

    /// <summary>
    /// Supported list column controls
    /// </summary>
    public enum ListColumnControls
    {
        /// <summary>
        /// Data templates automatically pick the control they feel is most appropriate
        /// </summary>
        Auto,
        /// <summary>
        /// Text element
        /// </summary>
        Text,
        /// <summary>
        /// Check mark (check box)
        /// </summary>
        Checkmark,
        /// <summary>
        /// Text element populated from a list of possible values (typically expressed as a drop down list in edit mode)
        /// </summary>
        TextList,
        /// <summary>
        /// Image
        /// </summary>
        Image
    }

    /// <summary>Content alignment options for list columns</summary>
    public enum ListColumnContentAlignment
    {
        /// <summary>Default content alignment for each control</summary>
        Default,
        /// <summary>Content is left aligned</summary>
        Left,
        /// <summary>Content is center aligned</summary>
        Center,
        /// <summary>Content is right aligned</summary>
        Right,
        /// <summary>Stretch across entire width</summary>
        Stretch
    }

    /// <summary>
    /// Row edit mode for multi-column lists
    /// </summary>
    public enum ListRowEditMode
    {
        /// <summary>All cells are read-only</summary>
        ReadOnly,
        /// <summary>All cells are read/write</summary>
        ReadWriteAll,
        /// <summary>Mode is set on a row-by-row basis manually by means of a binding (true/false)</summary>
        Manual
    }

    /// <summary>
    /// Defines when grid lines should be displayed in a list with columns
    /// </summary>
    public enum ListGridLineMode
    {
        /// <summary>Never show grid lines</summary>
        Never,
        /// <summary>Always show grid lines</summary>
        Always,
        /// <summary>Only show grid lines when the cell is being edited</summary>
        EditOnly
    }

    /// <summary>
    /// Filter mode
    /// </summary>
    public enum FilterMode
    {
        /// <summary>
        /// Filter searches for a contained string
        /// </summary>
        ContainedString,
        /// <summary>
        /// Filter searches for an exact start of the string
        /// </summary>
        StartsWithString,
        /// <summary>
        /// String must be an exact match
        /// </summary>
        ExactMatchString,
        /// <summary>
        /// Filter attempts to match an exact number (and supports greater than and less than)
        /// </summary>
        Number
    }
}

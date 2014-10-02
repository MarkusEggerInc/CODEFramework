using System;
using System.Collections.ObjectModel;
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
        public static readonly DependencyProperty ColumnsProperty = DependencyProperty.RegisterAttached("Columns", typeof(ListColumnsCollection), typeof(ListEx), new UIPropertyMetadata(null));
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
        public static readonly DependencyProperty WidthProperty = DependencyProperty.Register("Width", typeof(GridLength), typeof(ListColumn), new UIPropertyMetadata(new GridLength(100d)));

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
        /// <remarks>Note that setting this value does NOT actually sort the bound data. It simply creates a visual indicator showing that the column is sorted.</remarks>
        public SortOrder SortOrder
        {
            get { return (SortOrder)GetValue(SortOrderProperty); }
            set { SetValue(SortOrderProperty, value); }
        }
        /// <summary>Column sort order indicator</summary>
        /// <value>The sort order indicator.</value>
        /// <remarks>Note that setting this value does NOT actually sort the bound data. It simply creates a visual indicator showing that the column is sorted.</remarks>
        public static readonly DependencyProperty SortOrderProperty = DependencyProperty.Register("SortOrder", typeof(SortOrder), typeof(ListColumn), new PropertyMetadata(SortOrder.Unsorted));

        /// <summary>Binding path for a dynamically set sort order</summary>
        /// <value>The sort order binding path.</value>
        public string SortOrderBindingPath
        {
            get { return (string)GetValue(SortOrderBindingPathProperty); }
            set { SetValue(SortOrderBindingPathProperty, value); }
        }
        /// <summary>Binding path for a dynamically set sort order</summary>
        /// <value>The sort order binding path.</value>
        public static readonly DependencyProperty SortOrderBindingPathProperty = DependencyProperty.Register("SortOrderBindingPath", typeof(string), typeof(ListColumn), new PropertyMetadata(""));
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
}

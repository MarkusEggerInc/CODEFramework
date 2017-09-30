using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml.Linq;
using CODE.Framework.Core.Utilities;

namespace CODE.Framework.Wpf.Mvvm.Tools
{
    /// <summary>
    /// Interaction logic for ViewVisualizer.xaml
    /// </summary>
    public partial class ViewVisualizer : IViewHandler, INotifyPropertyChanged, IModelStatus
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ViewVisualizer"/> class.
        /// </summary>
        public ViewVisualizer()
        {
            CurrentVisualizer = this;
            LoadingVisibility = Visibility.Collapsed;
            DataContext = this;
            Views = new ObservableCollection<ViewVisualizerItem>();
            InitializeComponent();
        }

        /// <summary>
        /// For internal use only
        /// </summary>
        public static ViewVisualizer CurrentVisualizer;

        /// <summary>
        /// Gets or sets the views.
        /// </summary>
        /// <value>
        /// The views.
        /// </value>
        public ObservableCollection<ViewVisualizerItem> Views { get; set; }

        private ViewVisualizerItem _currentItem;
        private ModelStatus _modelStatus;
        private Visibility _loadingVisibility;

        /// <summary>
        /// Currently selected view
        /// </summary>
        public ViewVisualizerItem CurrentItem
        {
            get { return _currentItem; }
            set
            {
                _currentItem = value;
                if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs("CurrentItem"));
            }
        }

        /// <summary>
        /// This method is invoked when a view is opened
        /// </summary>
        /// <param name="context">Request context (contains information about the view)</param>
        /// <returns>True if handled successfully</returns>
        public bool OpenView(RequestContext context)
        {
            if (context.Result is StatusMessageResult) return false;
            if (context.Result is NotificationMessageResult) return false;

            var viewResult = context.Result as ViewResult;
            if (viewResult != null && !viewResult.IsPartial)
            {
                var viewItem = new ViewVisualizerItem
                                   {
                                       ViewSource = viewResult.ViewSource,
                                       ViewObject = viewResult.View,
                                       View = new VisualBrush(viewResult.View) {Stretch = Stretch.Uniform},
                                       Model = viewResult.Model,
                                       Controller = context.ProcessingController,
                                       Title = viewResult.ViewTitle
                                   };
                if (context.RouteData.Data.ContainsKey("action")) viewItem.Action = context.RouteData.Data["action"].ToString();
                if (context.RouteData.Data.ContainsKey("Action")) viewItem.Action = context.RouteData.Data["Action"].ToString();
                Views.Add(viewItem);
                return true;
            }
            return false;
        }

        /// <summary>
        /// This method is invoked when a view that is associated with a certain model should be closed
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public bool CloseViewForModel(object model)
        {
            foreach (var view in Views)
                if (view.Model != null && view.Model == model)
                {
                    Views.Remove(view);
                    return true;
                }
            return false;
        }

        /// <summary>
        /// This method is invoked when a view that is associated with a certain model should be activated/shown
        /// </summary>
        /// <param name="model">Model</param>
        /// <returns>
        /// True if successful
        /// </returns>
        public bool ActivateViewForModel(object model)
        {
            return false;
        }

        /// <summary>
        /// This method closes all currently open views
        /// </summary>
        /// <returns>True if the handler successfully closed all views. False if it didn't close all views or generally does not handle view closing</returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public bool CloseAllViews()
        {
            // This handler does not handle view closing
            return false;
        }

        /// <summary>
        /// This method is used to retrieve a view associated with the specified model
        /// </summary>
        /// <param name="model">Model</param>
        /// <returns>
        /// Document if found (null otherwise)
        /// </returns>
        public object GetViewForModel(object model)
        {
            return null;
        }

        /// <summary>
        /// Returns true, if a model instance of the specified type and selector criteria is already open
        /// </summary>
        /// <typeparam name="TModel">The type of the model.</typeparam>
        /// <param name="selector">Selector used to pick an appropriate model instance</param>
        /// <returns>
        /// A reference to the open model instance
        /// </returns>
        public TModel GetOpenModel<TModel>(Func<TModel, bool> selector) where TModel : class
        {
            return default(TModel);
        }

        /// <summary>
        /// Returns true, if a model instance of the specified type is already open
        /// </summary>
        /// <typeparam name="TModel">The type of the model.</typeparam>
        /// <returns>
        /// A reference to the open model instance
        /// </returns>
        public TModel GetOpenModel<TModel>() where TModel : class
        {
            return default(TModel);
        }

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Handles the MouseDoubleClick event of the ScaleSlider control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Input.MouseButtonEventArgs"/> instance containing the event data.</param>
        private void ScaleSlider_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ScaleSlider.Value = 1;
        }

        /// <summary>
        /// Handles the Checked event of the CheckBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            ViewRectangle.Effect = ShadowCheck.IsChecked == true ? new DropShadowEffect() : null;
        }

        /// <summary>
        /// Handles the SelectionChanged event of the ComboBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Controls.SelectionChangedEventArgs"/> instance containing the event data.</param>
        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (ColorDropDown.SelectedIndex)
            {
                case 0:
                    ContentScroll.Background = null;
                    break;
                case 1:
                    ContentScroll.Background = Brushes.White;
                    break;
                case 2:
                    ContentScroll.Background = Brushes.Black;
                    break;
                case 3:
                    ContentScroll.Background = Brushes.Gray;
                    break;
                case 4:
                    ContentScroll.Background = Brushes.CornflowerBlue;
                    break;
            }
        }

        /// <summary>
        /// Handles the SelectedItemChanged event of the TreeView control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="object"/> instance containing the event data.</param>
        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue != null)
                CurrentItem.SelectedElement = e.NewValue as UIElementViewModel;
            else if (CurrentItem != null && CurrentItem.Elements != null && CurrentItem.Elements.Count > 0)
                CurrentItem.SelectedElement = CurrentItem.Elements[0];
        }

        /// <summary>
        /// Status
        /// </summary>
        public ModelStatus ModelStatus
        {
            get { return _modelStatus; }
            set
            {
                _modelStatus = value;
                switch (value)
                {
                    case ModelStatus.Loading:
                        LoadingVisibility = Visibility.Visible;
                        break;
                    default:
                        LoadingVisibility = Visibility.Collapsed;
                        break;
                }
            }
        }

        /// <summary>
        /// Indicates whether the loading indicator is visible
        /// </summary>
        /// <value>The loading visibility.</value>
        public Visibility LoadingVisibility
        {
            get { return _loadingVisibility; }
            set
            {
                _loadingVisibility = value;
                if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs("LoadingVisibility"));
            }
        }

        /// <summary>
        /// Counts the current number of operations in progress
        /// </summary>
        /// <value>The operations in progress.</value>
        public int OperationsInProgress { get; set; }
    }

    /// <summary>
    /// Document visualizer item
    /// </summary>
    public class ViewVisualizerItem : INotifyPropertyChanged
    {
        /// <summary>
        /// Document description
        /// </summary>
        public string ViewSource { get; set; }

        /// <summary>
        /// Bindable text for the view information
        /// </summary>
        public string ViewSourceText
        {
            get
            {
                var sourceText = ViewSource;
                if (string.IsNullOrEmpty(sourceText)) sourceText = "n/a";
                return "Document: " + sourceText;
            }
        }

        /// <summary>
        /// Visual brush representing the view
        /// </summary>
        public VisualBrush View { get; set; }

        /// <summary>
        /// The actual view
        /// </summary>
        public FrameworkElement ViewObject { get; set; }

        /// <summary>
        /// Model associated with the view
        /// </summary>
        public object Model
        {
            get { return _model; }
            set
            {
                _model = value;
                ModelClass = _model != null ? _model.GetType().ToString() : "n/a";
            }
        }

        private object _model;

        /// <summary>
        /// Exposes the properties of the current model
        /// </summary>
        /// <value>The model properties.</value>
        public ObservableCollection<ViewModelProperty> ModelProperties
        {
            get
            {
                var list = new ObservableCollection<ViewModelProperty>();

                if (_model != null)
                    try
                    {
                        PopulateModelProperties(_model, list);
                    }
                    catch
                    {
                    }

                return list;
            }
        }

        private static void PopulateModelProperties(object model, ObservableCollection<ViewModelProperty> list)
        {
            if (model == null) return;
            var modelType = model.GetType();
            var properties = modelType.GetProperties(BindingFlags.Instance | BindingFlags.Public).OrderBy(p => p.Name);

            foreach (var property in properties)
            {
                if (property == null) continue;
                if (!property.CanRead) continue;

                var currentValue = property.GetValue(model, null);
                var valString = currentValue == null ? "null" : currentValue.ToString();

                if (property.PropertyType.FullName.StartsWith("System.Collections.ObjectModel.ObservableCollection`1[[CODE.Framework.Wpf.Mvvm.IViewAction,"))
                {
                    var newProp = new ViewModelProperty {Name = property.Name, IsAction = true, IsCollection = true};
                    list.Add(newProp);
                    var collection = currentValue as IEnumerable;
                    if (collection != null)
                    {
                        var collectionCounter = 0;
                        foreach (var item in collection)
                        {
                            var isStandardViewModel = item.GetType().GetInterfaces().Any(i => i == typeof(IStandardViewModel));
                            var propName = string.Empty;
                            var nameProp = item.GetType().GetProperty("Text1");
                            if (nameProp != null)
                                propName = nameProp.GetValue(item, null).ToString();
                            var newProp2 = new ViewModelProperty {Name = propName, IsAction = true, IsCollectionItem = true, CollectionItemCount = collectionCounter, IsStandardViewModel = isStandardViewModel, RealValue = item};
                            newProp.ModelProperties.Add(newProp2);
                            collectionCounter++;
                            PopulateModelProperties(item, newProp2.ModelProperties);
                        }
                        newProp.Name += " (Count: " + collectionCounter + ")";
                    }
                }
                else if (property.PropertyType.FullName.StartsWith("System.Collections.ObjectModel.ObservableCollection`") || property.PropertyType.FullName.StartsWith("System.Collections.ObjectModel.List`"))
                {
                    var newProp = new ViewModelProperty {Name = property.Name, Type = property.PropertyType.Name, IsCollection = true};
                    list.Add(newProp);
                    var collection = currentValue as IEnumerable;
                    if (collection != null)
                    {
                        var collectionCounter = 0;
                        foreach (var item in collection)
                        {
                            var isStandardViewModel = item.GetType().GetInterfaces().Any(i => i == typeof(IStandardViewModel));
                            var newProp2 = new ViewModelProperty {IsCollectionItem = true, CollectionItemCount = collectionCounter, IsStandardViewModel = isStandardViewModel, RealValue = item};
                            newProp.ModelProperties.Add(newProp2);
                            collectionCounter++;
                            PopulateModelProperties(item, newProp2.ModelProperties);
                        }
                        newProp.Name += " (Count: " + collectionCounter + ")";
                    }
                }
                else if (property.PropertyType.GetInterfaces().Any(i => i == typeof (IViewAction)))
                {
                    var isStandardViewModel = property.PropertyType.GetInterfaces().Any(i => i == typeof(IStandardViewModel));
                    var newProp = new ViewModelProperty {Name = property.Name, Type = property.PropertyType.Name, IsAction = true, RealValue = currentValue, IsStandardViewModel = isStandardViewModel};
                    list.Add(newProp);
                    PopulateModelProperties(currentValue, newProp.ModelProperties);
                }
                else
                {
                    var isStandardViewModel = property.PropertyType.GetInterfaces().Any(i => i == typeof(IStandardViewModel));
                    var newProp2 = new ViewModelProperty {Name = property.Name, Value = valString, RealValue = currentValue, Type = property.PropertyType.Name, IsStandardViewModel = isStandardViewModel};
                    list.Add(newProp2);
                    if (property.PropertyType.GetInterfaces().Any(i => i == typeof (INotifyPropertyChanged)))
                        PopulateModelProperties(currentValue, newProp2.ModelProperties);
                }
            }
        }

        /// <summary>
        /// Class used for the view model
        /// </summary>
        public string ModelClass { get; private set; }

        /// <summary>
        /// Bindable text for the view information
        /// </summary>
        public string ModelText
        {
            get { return "Model: " + ModelClass; }
        }

        /// <summary>
        /// Model associated with the view
        /// </summary>
        public object Controller
        {
            get { return _controller; }
            set
            {
                _controller = value;
                ControllerClass = _controller != null ? _controller.GetType().ToString() : "Unknown";
            }
        }

        private object _controller;

        /// <summary>
        /// Class used for the controller
        /// </summary>
        public string ControllerClass { get; private set; }

        /// <summary>
        /// Bindable text for the controller information
        /// </summary>
        public string ControllerText
        {
            get
            {
                if (string.IsNullOrEmpty(ControllerClass)) return "Controller: n/a";
                string text = "Controller: " + ControllerClass;
                if (!string.IsNullOrEmpty(Action)) text += "::" + Action + "(...)";
                return text;
            }
        }

        /// <summary>
        /// Controller Action
        /// </summary>
        public string Action { get; set; }

        /// <summary>
        /// Document title
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// List of available resources for a view
        /// </summary>
        public List<ResourceItem> AvailableResources
        {
            get
            {
                var result = new List<ResourceItem>();
                if (ViewObject != null && ViewObject.Resources != null)
                    foreach (var dictionary in ViewObject.Resources.MergedDictionaries)
                        result.Add(new ResourceItem {Name = StringHelper.ToStringSafe(dictionary.Source)});
                return result;
            }
        }

        /// <summary>
        /// Inspects a specific element and adds it to the list
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="collection">The collection.</param>
        /// <param name="rootView">The root view.</param>
        private static void AddElement(UIElement element, ObservableCollection<UIElementViewModel> collection, UIElement rootView)
        {
            var parts1 = element.ToString().Split(' ');
            var parts2 = parts1[0].Split('.');
            var elementName = parts2[parts2.Length - 1].Replace(":", "");
            var content = string.Empty;
            if (parts1.Length > 1) content = parts1[1];

            var cc = element as ContentControl;
            if (cc != null && cc.Content != null)
                content = cc.Content.ToString();

            var newElement = new UIElementViewModel(rootView)
                                 {
                                     Name = elementName,
                                     Content = content,
                                     Type = element.GetType().ToString(),
                                     UIElement = element
                                 };
            var contentControl = element as ContentControl;
            if (contentControl != null)
            {
                if (contentControl.Content is UIElement)
                    AddElement(contentControl.Content as UIElement, newElement.Elements, rootView);
            }
            else
            {
                var childControl = element as Panel;
                if (childControl != null)
                    foreach (var child in childControl.Children)
                    {
                        if (child is UIElement)
                            AddElement(child as UIElement, newElement.Elements, rootView);
                    }
                else
                {
                    var itemsControl = element as ItemsControl;
                    if (itemsControl != null)
                        foreach (var item in itemsControl.Items)
                            if (item is UIElement)
                                AddElement(item as UIElement, newElement.Elements, rootView);
                }
            }
            collection.Add(newElement);
        }

        /// <summary>Hierarchical list of UI elements</summary>
        public ObservableCollection<UIElementViewModel> Elements
        {
            get
            {
                var elements = new ObservableCollection<UIElementViewModel>();
                if (ViewObject != null)
                    AddElement(ViewObject, elements, ViewObject);
                return elements;
            }
        }

        private UIElementViewModel _selectedElement;

        /// <summary>Gets or sets the selected element from the view hierarchy tree.</summary>
        public UIElementViewModel SelectedElement
        {
            get { return _selectedElement; }
            set
            {
                _selectedElement = value;
                NotifyChanged("SelectedElement");
            }
        }

        /// <summary>
        /// Current e
        /// </summary>
        public FrameworkElement CurrentElement { get; set; }

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyChanged(string propertyName = "")
        {
            try
            {
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
            catch (Exception)
            {
            }
        }
    }

    /// <summary>
    /// View model for a view model property
    /// </summary>
    public class ViewModelProperty
    {
        private static IStandardViewFactory _standardViewFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="ViewModelProperty"/> class.
        /// </summary>
        public ViewModelProperty()
        {
            ModelProperties = new ObservableCollection<ViewModelProperty>();
        }
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }
        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>The value.</value>
        public string Value { get; set; }
        /// <summary>
        /// Gets or sets the real value.
        /// </summary>
        /// <value>The real value.</value>
        public object RealValue { get; set; }
        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>The type.</value>
        public string Type { get; set; }

        /// <summary>
        /// For internal use only
        /// </summary>
        /// <value>The property2.</value>
        public string Property2
        {
            get
            {
                return " " + Name;
            }
        }

        /// <summary>
        /// Indicates whether this property is a collection item
        /// </summary>
        /// <value><c>true</c> if this instance is collection item; otherwise, <c>false</c>.</value>
        public bool IsCollectionItem { get; set; }
        /// <summary>
        /// Indicates the number of items in a collection
        /// </summary>
        /// <value>The collection item count.</value>
        public int CollectionItemCount { get; set; }

        /// <summary>
        /// Indicates whether this item represents a collection
        /// </summary>
        /// <value><c>true</c> if this instance is collection; otherwise, <c>false</c>.</value>
        public bool IsCollection { get; set; }

        /// <summary>
        /// Display value representing the current property
        /// </summary>
        /// <value>The display value.</value>
        public string DisplayValue
        {
            get
            {
                if (IsCollectionItem && IsAction) return "[" + CollectionItemCount + "] " + Name;
                if (IsCollectionItem) return "[" + CollectionItemCount + "]";
                if (IsCollection && IsAction) return Name;
                if (IsCollection) return Name;
                if (IsAction) return Name + " (" + Type + ")";

                var val = Value;
                if (val.Length > 250) val = val.Substring(0, 250);
                return Name + " = " + val;
            }
        }

        /// <summary>
        /// Sub-properties of the current property
        /// </summary>
        /// <value>The model properties.</value>
        public ObservableCollection<ViewModelProperty> ModelProperties { get; set; }
        /// <summary>
        /// Indicates whether the current item is a view action
        /// </summary>
        /// <value><c>true</c> if this instance is action; otherwise, <c>false</c>.</value>
        public bool IsAction { get; set; }

        /// <summary>
        /// Display foreground color
        /// </summary>
        /// <value>The foreground.</value>
        public Brush Foreground
        {
            get
            {
                if (IsAction) return Brushes.Blue;
                if (IsStandardViewModel) return Brushes.Goldenrod;
                return Brushes.Black;
            }
        }

        /// <summary>
        /// Display tooltip
        /// </summary>
        /// <value>The tooltip.</value>
        public string Tooltip
        {
            get
            {
                var sb = new StringBuilder();
                if (IsAction) sb.AppendLine("View Action");
                if (IsCollection) sb.AppendLine("Collection");
                if (IsStandardViewModel) sb.Append("Standard View Model");
                return sb.ToString().Trim();
            }
        }

        /// <summary>
        /// Contents of the detail pane
        /// </summary>
        /// <value>The details.</value>
        public UIElement Details
        {
            get
            {
                var propertyValue = RealValue;

                if (propertyValue == null) return GetText("{x:Null}");

                if (propertyValue is SolidColorBrush)
                {
                    var grid = new Grid { VerticalAlignment = VerticalAlignment.Stretch, HorizontalAlignment = HorizontalAlignment.Stretch };
                    grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1d, GridUnitType.Auto) });
                    grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1d, GridUnitType.Star) });
                    grid.Children.Add(new TextBlock { Text = "Solid Color: " + (propertyValue as SolidColorBrush) });
                    var rect = new Rectangle { VerticalAlignment = VerticalAlignment.Stretch, HorizontalAlignment = HorizontalAlignment.Stretch, Fill = propertyValue as SolidColorBrush };
                    Grid.SetRow(rect, 1);
                    grid.Children.Add(rect);
                    return grid;
                }
                if (propertyValue is Brush)
                {
                    var grid = new Grid { VerticalAlignment = VerticalAlignment.Stretch, HorizontalAlignment = HorizontalAlignment.Stretch };
                    grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1d, GridUnitType.Auto) });
                    grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1d, GridUnitType.Star) });
                    grid.Children.Add(new TextBlock { Text = "Brush: " + (propertyValue as Brush) });
                    var rect = new Rectangle { VerticalAlignment = VerticalAlignment.Stretch, HorizontalAlignment = HorizontalAlignment.Stretch, Fill = propertyValue as Brush };
                    Grid.SetRow(rect, 1);
                    grid.Children.Add(rect);
                    return grid;
                }
                if (propertyValue is FontFamily)
                {
                    var font = propertyValue as FontFamily;
                    var fontNames = font.FamilyNames.Aggregate("Font name: ", (current, name) => current + name.Value + ", ");
                    fontNames = fontNames.Substring(0, fontNames.Length - 2);
                    var panel = new StackPanel();
                    panel.Children.Add(new TextBlock { FontSize = 14, Text = fontNames, TextWrapping = TextWrapping.NoWrap, Margin = new Thickness(0, 0, 0, 10d) });
                    panel.Children.Add(new TextBlock { FontSize = 14d, Text = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ", TextWrapping = TextWrapping.NoWrap });
                    panel.Children.Add(new TextBlock { FontSize = 14d, Text = "1234567890.:,;'\"(!?)+-*/=", TextWrapping = TextWrapping.NoWrap, Margin = new Thickness(0, 0, 0, 10d) });
                    panel.Children.Add(new TextBlock { FontSize = 8d, Text = "8px: The quick brown fox jumps over the lazy dog. 1234567890", TextWrapping = TextWrapping.NoWrap });
                    panel.Children.Add(new TextBlock { FontSize = 10d, Text = "10px: The quick brown fox jumps over the lazy dog. 1234567890", TextWrapping = TextWrapping.NoWrap });
                    panel.Children.Add(new TextBlock { FontSize = 12d, Text = "12px: The quick brown fox jumps over the lazy dog. 1234567890", TextWrapping = TextWrapping.NoWrap });
                    panel.Children.Add(new TextBlock { FontSize = 14d, Text = "14px: The quick brown fox jumps over the lazy dog. 1234567890", TextWrapping = TextWrapping.NoWrap });
                    panel.Children.Add(new TextBlock { FontSize = 18d, Text = "18px: The quick brown fox jumps over the lazy dog. 1234567890", TextWrapping = TextWrapping.NoWrap });
                    panel.Children.Add(new TextBlock { FontSize = 24d, Text = "24px: The quick brown fox jumps over the lazy dog. 1234567890", TextWrapping = TextWrapping.NoWrap });
                    panel.Children.Add(new TextBlock { FontSize = 36d, Text = "36px: The quick brown fox jumps over the lazy dog. 1234567890", TextWrapping = TextWrapping.NoWrap });
                    panel.Children.Add(new TextBlock { FontSize = 48d, Text = "48px: The quick brown fox jumps over the lazy dog. 1234567890", TextWrapping = TextWrapping.NoWrap });
                    panel.Children.Add(new TextBlock { FontSize = 72d, Text = "72px: The quick brown fox jumps over the lazy dog. 1234567890", TextWrapping = TextWrapping.NoWrap });
                    return panel;
                }
                if (IsStandardViewModel)
                {
                    var grid = new Grid { VerticalAlignment = VerticalAlignment.Stretch, HorizontalAlignment = HorizontalAlignment.Stretch };
                    grid.RowDefinitions.Add(new RowDefinition{ Height = new GridLength(1d, GridUnitType.Auto)});
                    grid.RowDefinitions.Add(new RowDefinition{ Height = new GridLength(1d, GridUnitType.Star)});
                    var combo = new ComboBox {HorizontalAlignment = HorizontalAlignment.Stretch, IsEditable = false};
                    var enumNames = Enum.GetNames(typeof (StandardViews));
                    var enumValues = Enum.GetValues(typeof (StandardViews));
                    var enumCounter = 0;
                    foreach (var enumValue in enumValues)
                    {
                        if ((StandardViews) enumValue != StandardViews.None)
                            combo.Items.Add(new ComboBoxItem {Content = "Standard View: " + enumNames[enumCounter], Tag = enumValue});
                        enumCounter++;
                    }
                    grid.Children.Add(combo);
                    var content = new ContentControl {VerticalAlignment = VerticalAlignment.Stretch, HorizontalAlignment = HorizontalAlignment.Stretch, Background = Brushes.LightGoldenrodYellow};
                    Grid.SetRow(content, 1);
                    grid.Children.Add(content);
                    combo.SelectionChanged += (s, e) =>
                    {
                        try
                        {
                            var factory = StandardViewFactory;
                            var comboItem = combo.SelectedItem as ComboBoxItem;
                            if (comboItem == null) return;
                            var enumValue = (StandardViews) comboItem.Tag;
                            var standardView = factory.GetStandardView(enumValue);
                            standardView.DataContext = RealValue;
                            content.Content = standardView;
                        }
                        catch (Exception ex)
                        {
                            content.Content = new TextBlock {Text = ExceptionHelper.GetExceptionText(ex)};
                        }
                    };
                    combo.SelectedIndex = 0;
                    return grid;
                }
                if (propertyValue is UIElement || propertyValue is FrameworkTemplate) return GetText(GetXaml(propertyValue));

                return GetText(propertyValue.ToString());
            }
        }

        /// <summary>
        /// Returns a factor object for the current standard view theme
        /// </summary>
        /// <value>The standard view factory.</value>
        public static IStandardViewFactory StandardViewFactory
        {
            get
            {
                if (_standardViewFactory == null)
                    _standardViewFactory = ApplicationEx.GetStandardThemeFeatures().StandardViewFactory;
                return _standardViewFactory;
            }
        }

        /// <summary>
        /// Indicates whether the current item is a standard view model
        /// </summary>
        /// <value><c>true</c> if this instance is standard view model; otherwise, <c>false</c>.</value>
        public bool IsStandardViewModel { get; set; }

        /// <summary>
        /// Gets a text representation for the current element.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns>UIElement.</returns>
        private UIElement GetText(string text)
        {
            return new TextBox
            {
                Background = null,
                BorderBrush = null,
                Text = text,
                IsReadOnly = true,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 12d,
                TextWrapping = TextWrapping.NoWrap,
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden
            };
        }

        private string GetXaml(object element)
        {
            if (element == null) return "{x:Null}";

            try
            {
                var xml = XamlWriter.Save(element);
                return XDocument.Parse(xml).ToString();
            }
            catch (Exception ex)
            {
                var exceptionText = "Unable to retrieve XAML.\r\n\r\nException:\r\n" + ExceptionHelper.GetExceptionText(ex);
                if (exceptionText.Contains("Cannot serialize")) exceptionText = "Unable to retrieve XAML since the value contains non-serializable elements.";
                return exceptionText;
            }
        }
    }

    /// <summary>Resource item in visualizer</summary>
    public class ResourceItem
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; set; }
    }

    /// <summary>Document model for individual UI elements</summary>
    public class UIElementViewModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UIElementViewModel"/> class.
        /// </summary>
        public UIElementViewModel(UIElement rootView)
        {
            CurrentView = rootView;
            Elements = new ObservableCollection<UIElementViewModel>();
        }

        /// <summary>Object or instance name</summary>
        public string Name { get; set; }

        /// <summary>Additional information about the object, such as the value of a textbox, or the caption of a label</summary>
        public string Content { get; set; }

        /// <summary>Display specific version of the content</summary>
        public string ContentDisplay
        {
            get
            {
                if (string.IsNullOrEmpty(Content)) return string.Empty;
                return " (" + Content.Trim() + ")";
            }
        }

        /// <summary>Type of the UI element (class)</summary>
        public string Type { get; set; }

        /// <summary>Actual UI element</summary>
        public UIElement UIElement { get; set; }

        /// <summary>Hierarchical list of UI elements</summary>
        public ObservableCollection<UIElementViewModel> Elements { get; set; }

        /// <summary>Actual visual of the UI element this item represents</summary>
        public VisualBrush UIElementVisual
        {
            get { return new VisualBrush(UIElement); }
        }

        /// <summary>List of associated resource dictionaries</summary>
        public ObservableCollection<ResourceDictionaryViewModel> ResourceDictionaries
        {
            get
            {
                var dictionaries = new ObservableCollection<ResourceDictionaryViewModel>();

                var appDictionaries = new ResourceDictionaryViewModel {Source = "Application"};
                dictionaries.Add(appDictionaries);
                foreach (var dictionary in Application.Current.Resources.MergedDictionaries)
                    AddDictionaries(appDictionaries.ResourceDictionaries, dictionary);

                var viewDictionaries = new ResourceDictionaryViewModel {Source = "Document"};
                dictionaries.Add(viewDictionaries);
                var currentElement2 = CurrentView as FrameworkElement;
                if (currentElement2 != null)
                    foreach (var dictionary in currentElement2.Resources.MergedDictionaries)
                        AddDictionaries(viewDictionaries.ResourceDictionaries, dictionary);

                if (CurrentView.Equals(UIElement))
                {
                    var elementDictionaries = new ResourceDictionaryViewModel {Source = "Element"};
                    dictionaries.Add(elementDictionaries);
                    var currentElement = UIElement as FrameworkElement;
                    if (currentElement != null)
                        foreach (var dictionary in currentElement.Resources.MergedDictionaries)
                            AddDictionaries(elementDictionaries.ResourceDictionaries, dictionary);
                }

                return dictionaries;
            }
        }

        /// <summary>Collection of styles applying to the current control</summary>
        public ObservableCollection<ControlStyleViewModel> ControlStyles
        {
            get
            {
                var styles = new ObservableCollection<ControlStyleViewModel>();

                var currentElement = UIElement as FrameworkElement;
                if (currentElement != null && currentElement.Style != null)
                    AddStyleInformation(styles, currentElement, currentElement.Style);

                // We are now checking to see if any setters are overridden in inherited styles
                for (var counter = 0; counter < styles.Count; counter++ )
                {
                    var style = styles[counter];
                    foreach (var setter in style.ControlStyles)
                        for (var counter2 = counter + 1; counter2 < styles.Count; counter2++)
                            foreach (var setter2 in styles[counter2].ControlStyles)
                                if (setter2.Property == setter.Property)
                                {
                                    setter.IsOverridden = true;
                                    break;
                                }
                }
                
                return styles;
            }
        }

        /// <summary>
        /// Properties of the current control.
        /// </summary>
        /// <value>The control properties.</value>
        public ObservableCollection<ControlPropertyViewModel> ControlProperties
        {
            get
            {
                var properties = new List<ControlPropertyViewModel>();

                var currentElement = UIElement as FrameworkElement;
                if (currentElement == null) return new ObservableCollection<ControlPropertyViewModel>();

                var retVal = new ObservableCollection<ControlPropertyViewModel>();
                ViewVisualizer.CurrentVisualizer.OperationsInProgress++;
                ViewVisualizer.CurrentVisualizer.ModelStatus = ModelStatus.Loading;
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate { })); // DoEvents()

                PopulateProperties(currentElement, currentElement.GetType(), properties);
                var sortedProperties = properties.OrderBy(p => p.Name).ToList();

                retVal.AddRange(sortedProperties);

                ViewVisualizer.CurrentVisualizer.OperationsInProgress--;
                if (ViewVisualizer.CurrentVisualizer.OperationsInProgress < 1)
                    ViewVisualizer.CurrentVisualizer.ModelStatus = ModelStatus.Ready;
                return retVal;
            }
        }

        private void PopulateProperties(DependencyObject currentElement, Type elementType, List<ControlPropertyViewModel> properties)
        {
            var fieldList = elementType.GetFields(BindingFlags.Static | BindingFlags.Public).ToList();
            var fields = fieldList.Where(p => p.FieldType == typeof(DependencyProperty));

            var resourceReferenceExpressionConverter = new ResourceReferenceExpressionConverter();

            foreach (var field in fields)
            {
                var dependencyProperty = field.GetValue(currentElement) as DependencyProperty;
                if (dependencyProperty == null) continue;
                var currentValue = currentElement.GetValue(dependencyProperty);
                
                var isDefault = false;
                var isExtension = false;
                var resourceName = string.Empty;
                var isBound = false;
                var bindingExpression = string.Empty;
                if (currentValue != null)
                {
                    isDefault = currentValue.Equals(dependencyProperty.DefaultMetadata.DefaultValue);

                    try
                    {
                        var localValue = currentElement.ReadLocalValue(dependencyProperty);
                        if (localValue.GetType().Name == "ResourceReferenceExpression") // Note: This is an internal type and hence it not visible to us as an actual type
                        {
                            var markupExtension = resourceReferenceExpressionConverter.ConvertTo(localValue, typeof (MarkupExtension));
                            var extension = markupExtension as DynamicResourceExtension;
                            if (extension != null)
                            {
                                isExtension = true;
                                resourceName = extension.ResourceKey.ToString();
                            }
                        }
                    }
                    catch (ArgumentException)
                    {
                    }

                    var binding = BindingOperations.GetBinding(currentElement, dependencyProperty);
                    if (binding != null)
                    {
                        isBound = true;
                        bindingExpression = binding.Path.Path;
                        if (binding.Mode != BindingMode.Default) bindingExpression += ", Mode=" + binding.Mode.ToString();
                        if (!string.IsNullOrEmpty(binding.ElementName)) bindingExpression += ", ElementName=" + binding.ElementName;
                        if (binding.RelativeSource != null) bindingExpression += ", RelativeSource={RelativeSource " + binding.RelativeSource.Mode + "}";
                    }
                }

                var newProp = new ControlPropertyViewModel
                {
                    Name = field.Name, 
                    DependencyObject = currentElement, 
                    Value = currentValue,
                    IsDefault = isDefault,
                    IsResource = isExtension,
                    ResourceName = resourceName,
                    IsBound = isBound,
                    BindingExpression = bindingExpression
                };

                properties.Add(newProp);
            }

            if (elementType.BaseType != null)
                PopulateProperties(currentElement, elementType.BaseType, properties);
        }

        /// <summary>
        /// Adds style information to the provided collection
        /// </summary>
        /// <param name="styles">The styles.</param>
        /// <param name="currentElement">The current element.</param>
        /// <param name="style">The style.</param>
        /// <param name="inheritedStyle">if set to <c>true</c> [inherited style].</param>
        private static void AddStyleInformation(IList<ControlStyleViewModel> styles, DependencyObject currentElement, Style style, bool inheritedStyle = false)
        {
            var appliedStyle = new ControlStyleViewModel
                                   {
                                       TargetType = style.TargetType.ToString(),
                                       Style = style,
                                       IsInheritedStyle = inheritedStyle
                                   };
            styles.Insert(0, appliedStyle);

            foreach (Setter setter in style.Setters)
            {
                var newSetter = new ControlStyleViewModel {Property = setter.Property.OwnerType.Name + "." + setter.Property.Name, Setter = setter, DependencyObject = currentElement, DependencyProperty = setter.Property};
                if (setter.Value == null) newSetter.Value = "{x:Null}";
                else
                {
                    var extension = setter.Value as DynamicResourceExtension;
                    if (extension != null)
                        newSetter.Value = "{DynamicResource " + extension.ResourceKey + "} = " + currentElement.GetValue(setter.Property);
                    else
                        newSetter.Value = setter.Value.ToString();
                }
                appliedStyle.ControlStyles.Add(newSetter);
            }

            if (!inheritedStyle)
            {
                var local = currentElement.ReadLocalValue(FrameworkElement.StyleProperty);
                if (local != null)
                {
                    var localType = local.GetType();
                    if (localType.Name == "ResourceReferenceExpression")
                    {
                        var resourceKeyProperty = localType.GetProperty("ResourceKey");
                        if (resourceKeyProperty != null)
                            appliedStyle.Key = resourceKeyProperty.GetValue(local, null) as string;
                    }
                }
            }

            if (style.BasedOn != null)
                AddStyleInformation(styles, currentElement, style.BasedOn, true);
        }

        /// <summary>
        /// Document associated with this item (root view)
        /// </summary>
        public UIElement CurrentView { get; set; }

        /// <summary>Recursively populates resource dictionaries</summary>
        /// <param name="collection">The collection.</param>
        /// <param name="resourceDictionary">The resource dictionary.</param>
        private static void AddDictionaries(ObservableCollection<ResourceDictionaryViewModel> collection, ResourceDictionary resourceDictionary)
        {
            var model = new ResourceDictionaryViewModel
                            {
                                Source = resourceDictionary.Source.ToString(),
                                ResourceDictionary = resourceDictionary
                            };
            collection.Add(model);

            foreach (var dictionary in resourceDictionary.MergedDictionaries)
                AddDictionaries(model.ResourceDictionaries, dictionary);
        }
    }

    /// <summary>
    /// Information about available view models
    /// </summary>
    public class ResourceDictionaryViewModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceDictionaryViewModel"/> class.
        /// </summary>
        public ResourceDictionaryViewModel()
        {
            ResourceDictionaries = new ObservableCollection<ResourceDictionaryViewModel>();
        }

        /// <summary>
        /// Source from which the resource dictionary was loaded
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// Link to the actual resource dictionary
        /// </summary>
        public ResourceDictionary ResourceDictionary { get; set; }

        /// <summary>
        /// Linked resource dictionaries
        /// </summary>
        public ObservableCollection<ResourceDictionaryViewModel> ResourceDictionaries { get; set; }
    }

    /// <summary>
    /// Information about available control styles
    /// </summary>
    public class ControlStyleViewModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ControlStyleViewModel"/> class.
        /// </summary>
        public ControlStyleViewModel()
        {
            ControlStyles = new ObservableCollection<ControlStyleViewModel>();
        }
        /// <summary>Property name that is to be set</summary>
        public string Property { get; set; }

        /// <summary>
        /// Property with space
        /// </summary>
        public string Property2
        {
            get { return " " + Property; }
        }
        /// <summary>Property value that is to be set</summary>
        public string Value { get; set; }
        /// <summary>Style key</summary>
        public string Key { get; set; }
        /// <summary>Target type</summary>
        public string TargetType { get; set; }
        /// <summary>Display name for key or target type</summary>
        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrEmpty(Property)) return Property + " = " + Value;
                if (IsInheritedStyle) return "[Inherited]";
                if (string.IsNullOrEmpty(Key)) return "Implicit for type [" + TargetType + "]";
                return "Key: " + Key + " for type [" + TargetType + "]";
            }
        }

        /// <summary>
        /// Detail about the current element
        /// </summary>
        public UIElement Details
        {
            get
            {
                if (Setter == null) return GetText("{x:Null}");

                var setterValue = Setter.Value;
                var resourceExtension = setterValue as DynamicResourceExtension;
                if (resourceExtension != null && DependencyObject != null && DependencyProperty != null)
                {
                    try
                    {
                        setterValue = DependencyObject.GetValue(DependencyProperty);
                    }
                    catch
                    {
                        setterValue = null;
                    }
                }

                if (setterValue is SolidColorBrush)
                {
                    var grid = new Grid {VerticalAlignment = VerticalAlignment.Stretch, HorizontalAlignment = HorizontalAlignment.Stretch};
                    grid.RowDefinitions.Add(new RowDefinition {Height = new GridLength(1d, GridUnitType.Auto)});
                    grid.RowDefinitions.Add(new RowDefinition {Height = new GridLength(1d, GridUnitType.Star)});
                    grid.Children.Add(new TextBlock {Text = "Solid Color: " + (setterValue as SolidColorBrush)});
                    var rect = new Rectangle {VerticalAlignment = VerticalAlignment.Stretch, HorizontalAlignment = HorizontalAlignment.Stretch, Fill = setterValue as SolidColorBrush};
                    Grid.SetRow(rect, 1);
                    grid.Children.Add(rect);
                    return grid;
                }
                if (setterValue is Brush)
                {
                    var grid = new Grid {VerticalAlignment = VerticalAlignment.Stretch, HorizontalAlignment = HorizontalAlignment.Stretch};
                    grid.RowDefinitions.Add(new RowDefinition {Height = new GridLength(1d, GridUnitType.Auto)});
                    grid.RowDefinitions.Add(new RowDefinition {Height = new GridLength(1d, GridUnitType.Star)});
                    grid.Children.Add(new TextBlock {Text = "Brush: " + (setterValue as Brush)});
                    var rect = new Rectangle {VerticalAlignment = VerticalAlignment.Stretch, HorizontalAlignment = HorizontalAlignment.Stretch, Fill = setterValue as Brush};
                    Grid.SetRow(rect, 1);
                    grid.Children.Add(rect);
                    return grid;
                }
                if (setterValue is FontFamily)
                {
                    var font = setterValue as FontFamily;
                    var fontNames = font.FamilyNames.Aggregate("Font name: ", (current, name) => current + name.Value + ", ");
                    fontNames = fontNames.Substring(0, fontNames.Length - 2);
                    var panel = new StackPanel();
                    panel.Children.Add(new TextBlock {FontSize = 14, Text = fontNames, TextWrapping = TextWrapping.NoWrap, Margin = new Thickness(0, 0, 0, 10d)});
                    panel.Children.Add(new TextBlock {FontSize = 14d, Text = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ", TextWrapping = TextWrapping.NoWrap});
                    panel.Children.Add(new TextBlock {FontSize = 14d, Text = "1234567890.:,;'\"(!?)+-*/=", TextWrapping = TextWrapping.NoWrap, Margin = new Thickness(0, 0, 0, 10d)});
                    panel.Children.Add(new TextBlock {FontSize = 8d, Text = "8px: The quick brown fox jumps over the lazy dog. 1234567890", TextWrapping = TextWrapping.NoWrap});
                    panel.Children.Add(new TextBlock {FontSize = 10d, Text = "10px: The quick brown fox jumps over the lazy dog. 1234567890", TextWrapping = TextWrapping.NoWrap});
                    panel.Children.Add(new TextBlock {FontSize = 12d, Text = "12px: The quick brown fox jumps over the lazy dog. 1234567890", TextWrapping = TextWrapping.NoWrap});
                    panel.Children.Add(new TextBlock {FontSize = 14d, Text = "14px: The quick brown fox jumps over the lazy dog. 1234567890", TextWrapping = TextWrapping.NoWrap});
                    panel.Children.Add(new TextBlock {FontSize = 18d, Text = "18px: The quick brown fox jumps over the lazy dog. 1234567890", TextWrapping = TextWrapping.NoWrap});
                    panel.Children.Add(new TextBlock {FontSize = 24d, Text = "24px: The quick brown fox jumps over the lazy dog. 1234567890", TextWrapping = TextWrapping.NoWrap});
                    panel.Children.Add(new TextBlock {FontSize = 36d, Text = "36px: The quick brown fox jumps over the lazy dog. 1234567890", TextWrapping = TextWrapping.NoWrap});
                    panel.Children.Add(new TextBlock {FontSize = 48d, Text = "48px: The quick brown fox jumps over the lazy dog. 1234567890", TextWrapping = TextWrapping.NoWrap});
                    panel.Children.Add(new TextBlock {FontSize = 72d, Text = "72px: The quick brown fox jumps over the lazy dog. 1234567890", TextWrapping = TextWrapping.NoWrap});
                    return panel;
                }
                if (setterValue is UIElement || setterValue is FrameworkTemplate) return GetText(GetXaml(setterValue));

                var stringValue = string.Empty;
                if (setterValue != null) stringValue = setterValue.ToString();
                return GetText(stringValue);
            }
        }

        /// <summary>
        /// Returns a text representation
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns>UIElement.</returns>
        private UIElement GetText(string text)
        {
            return new TextBox
            {
                Background = null,
                BorderBrush = null,
                Text = text,
                IsReadOnly = true,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 12d,
                TextWrapping = TextWrapping.NoWrap,
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden
            };
        }

        private string GetXaml(object element)
        {
            if (element == null) return "{x:Null}";

            try
            {
                var xml = XamlWriter.Save(element);
                return XDocument.Parse(xml).ToString();
            }
            catch (Exception ex)
            {
                return "Unable to retrieve XAML.\r\n\r\nException:\r\n" + ExceptionHelper.GetExceptionText(ex);
            }
        }

        /// <summary>Indicates whether this style is based on the prior style</summary>
        public bool IsInheritedStyle { get; set; }

        /// <summary>Actual style</summary>
        public Style Style { get; set; }

        /// <summary>Control style members</summary>
        public ObservableCollection<ControlStyleViewModel> ControlStyles { get; set; }

        /// <summary>Indicates whether the style is overridden by an inherited style</summary>
        public bool IsOverridden { get; set; }

        /// <summary>
        /// Gets the text decorations.
        /// </summary>
        public object TextDecorations
        {
            get
            {
                var converter = new TextDecorationCollectionConverter();
                return converter.ConvertFrom(IsOverridden ? "Strikethrough" : "None");
            }
        }

        /// <summary>
        /// Setter reference
        /// </summary>
        public Setter Setter { get; set; }

        /// <summary>
        /// Actual dependency object this goes with
        /// </summary>
        public DependencyObject DependencyObject { get; set; }

        /// <summary>
        /// Actual dependency property this goes with
        /// </summary>
        public DependencyProperty DependencyProperty { get; set; }

        /// <summary>
        /// Foreground color
        /// </summary>
        public Brush Foreground
        {
            get
            {
                if (IsOverridden) return Brushes.Gray;
                if (Value != null && Value.Contains("{DynamicResource")) return Brushes.Green;
                return Brushes.Black;
            }
        }
    }

    /// <summary>
    /// View model for control properties
    /// </summary>
    public class ControlPropertyViewModel
    {
        private string _name;

        /// <summary>
        /// Name
        /// </summary>
        public string Name
        {
            get { return _name; }
            set
            {
                if (value.EndsWith("Property")) value = value.Substring(0, value.Length - 8);
                _name = value;
            }
        }

        /// <summary>
        /// Property exposed with leading space
        /// </summary>
        /// <value>The property2.</value>
        public string Property2
        {
            get
            {
                return " " + Name;
            }
        }

        /// <summary>
        /// Dependency object
        /// </summary>
        /// <value>The dependency object.</value>
        public DependencyObject DependencyObject { get; set; }
        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>The value.</value>
        public object Value { get; set; }

        /// <summary>
        /// Gets the display value.
        /// </summary>
        /// <value>The display value.</value>
        public string DisplayValue
        {
            get
            {
                var val = Value;
                if (val == null) val = "null";

                var secondary = string.Empty;

                if (IsBound) secondary = "{Binding " + BindingExpression + "}";
                else if (IsResource) secondary = "{DynamicResource " + ResourceName + "}";

                var retVal = string.IsNullOrEmpty(secondary) ? Name + " = " + val : Name + " = " + secondary + " = " + val;
                if (Name == "Style") retVal += " -- Note: Check the Element Style tab for details...";
                return retVal;
            }
        }

        /// <summary>
        /// Indicates whether the property value is the property default
        /// </summary>
        /// <value><c>true</c> if this instance is default; otherwise, <c>false</c>.</value>
        public bool IsDefault { get; set; }
        /// <summary>
        /// Indicates whether the property is data bound
        /// </summary>
        /// <value><c>true</c> if this instance is bound; otherwise, <c>false</c>.</value>
        public bool IsBound { get; set; }
        /// <summary>
        /// Indicates whether this property is bound to a resource
        /// </summary>
        /// <value><c>true</c> if this instance is resource; otherwise, <c>false</c>.</value>
        public bool IsResource { get; set; }

        /// <summary>
        /// Foreground color
        /// </summary>
        public Brush Foreground
        {
            get
            {
                if (IsBound) return Brushes.Goldenrod;
                if (IsResource) return Brushes.Green;
                if (IsDefault) return Brushes.Gray;
                return Brushes.Black;
            }
        }

        /// <summary>
        /// Name of a resource the property is bound to
        /// </summary>
        /// <value>The name of the resource.</value>
        public string ResourceName { get; set; }
        /// <summary>
        /// Binding expression (if the property is data bound)
        /// </summary>
        /// <value>The binding expression.</value>
        public string BindingExpression { get; set; }

        /// <summary>
        /// Detail pane content
        /// </summary>
        public UIElement Details
        {
            get
            {
                var propertyValue = Value;

                if (propertyValue == null) return GetText("{x:Null}");

                if (propertyValue is SolidColorBrush)
                {
                    var grid = new Grid { VerticalAlignment = VerticalAlignment.Stretch, HorizontalAlignment = HorizontalAlignment.Stretch };
                    grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1d, GridUnitType.Auto) });
                    grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1d, GridUnitType.Star) });
                    grid.Children.Add(new TextBlock { Text = "Solid Color: " + (propertyValue as SolidColorBrush) });
                    var rect = new Rectangle { VerticalAlignment = VerticalAlignment.Stretch, HorizontalAlignment = HorizontalAlignment.Stretch, Fill = propertyValue as SolidColorBrush };
                    Grid.SetRow(rect, 1);
                    grid.Children.Add(rect);
                    return grid;
                }
                if (propertyValue is Brush)
                {
                    var grid = new Grid { VerticalAlignment = VerticalAlignment.Stretch, HorizontalAlignment = HorizontalAlignment.Stretch };
                    grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1d, GridUnitType.Auto) });
                    grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1d, GridUnitType.Star) });
                    grid.Children.Add(new TextBlock { Text = "Brush: " + (propertyValue as Brush) });
                    var rect = new Rectangle { VerticalAlignment = VerticalAlignment.Stretch, HorizontalAlignment = HorizontalAlignment.Stretch, Fill = propertyValue as Brush };
                    Grid.SetRow(rect, 1);
                    grid.Children.Add(rect);
                    return grid;
                }
                if (propertyValue is FontFamily)
                {
                    var font = propertyValue as FontFamily;
                    var fontNames = font.FamilyNames.Aggregate("Font name: ", (current, name) => current + name.Value + ", ");
                    fontNames = fontNames.Substring(0, fontNames.Length - 2);
                    var panel = new StackPanel();
                    panel.Children.Add(new TextBlock { FontSize = 14, Text = fontNames, TextWrapping = TextWrapping.NoWrap, Margin = new Thickness(0, 0, 0, 10d) });
                    panel.Children.Add(new TextBlock { FontSize = 14d, Text = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ", TextWrapping = TextWrapping.NoWrap });
                    panel.Children.Add(new TextBlock { FontSize = 14d, Text = "1234567890.:,;'\"(!?)+-*/=", TextWrapping = TextWrapping.NoWrap, Margin = new Thickness(0, 0, 0, 10d) });
                    panel.Children.Add(new TextBlock { FontSize = 8d, Text = "8px: The quick brown fox jumps over the lazy dog. 1234567890", TextWrapping = TextWrapping.NoWrap });
                    panel.Children.Add(new TextBlock { FontSize = 10d, Text = "10px: The quick brown fox jumps over the lazy dog. 1234567890", TextWrapping = TextWrapping.NoWrap });
                    panel.Children.Add(new TextBlock { FontSize = 12d, Text = "12px: The quick brown fox jumps over the lazy dog. 1234567890", TextWrapping = TextWrapping.NoWrap });
                    panel.Children.Add(new TextBlock { FontSize = 14d, Text = "14px: The quick brown fox jumps over the lazy dog. 1234567890", TextWrapping = TextWrapping.NoWrap });
                    panel.Children.Add(new TextBlock { FontSize = 18d, Text = "18px: The quick brown fox jumps over the lazy dog. 1234567890", TextWrapping = TextWrapping.NoWrap });
                    panel.Children.Add(new TextBlock { FontSize = 24d, Text = "24px: The quick brown fox jumps over the lazy dog. 1234567890", TextWrapping = TextWrapping.NoWrap });
                    panel.Children.Add(new TextBlock { FontSize = 36d, Text = "36px: The quick brown fox jumps over the lazy dog. 1234567890", TextWrapping = TextWrapping.NoWrap });
                    panel.Children.Add(new TextBlock { FontSize = 48d, Text = "48px: The quick brown fox jumps over the lazy dog. 1234567890", TextWrapping = TextWrapping.NoWrap });
                    panel.Children.Add(new TextBlock { FontSize = 72d, Text = "72px: The quick brown fox jumps over the lazy dog. 1234567890", TextWrapping = TextWrapping.NoWrap });
                    return panel;
                }
                if (propertyValue is UIElement || propertyValue is FrameworkTemplate) return GetText(GetXaml(propertyValue));

                return GetText(propertyValue.ToString());
            }
        }

        /// <summary>
        /// Gets the text representation fo the element.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns>UIElement.</returns>
        private UIElement GetText(string text)
        {
            return new TextBox
            {
                Background = null,
                BorderBrush = null,
                Text = text,
                IsReadOnly = true,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 12d,
                TextWrapping = TextWrapping.NoWrap,
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden
            };
        }

        /// <summary>
        /// Serialzies the current property value as XAML
        /// </summary>
        /// <param name="element">The element.</param>
        /// <returns>System.String.</returns>
        private string GetXaml(object element)
        {
            if (element == null) return "{x:Null}";

            try
            {
                var xml = XamlWriter.Save(element);
                return XDocument.Parse(xml).ToString();
            }
            catch (Exception ex)
            {
                return "Unable to retrieve XAML.\r\n\r\nException:\r\n" + ExceptionHelper.GetExceptionText(ex);
            }
        }
    }
}

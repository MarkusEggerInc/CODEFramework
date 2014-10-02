using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.ServiceModel;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using CODE.Framework.Core.Utilities;
using CODE.Framework.Services.Client;


namespace CODE.Framework.Services.Tools.Windows
{
    /// <summary>
    /// Interaction logic for ServiceTestHarnessUI.xaml
    /// </summary>
    public partial class ServiceTestHarnessUI : UserControl
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public ServiceTestHarnessUI()
        {
            InitializeComponent();
        }

        private int _port = 80;
        private string _serviceId = string.Empty;
        private Protocol _protocol = Protocol.NetTcp;
        private MessageSize _messageSize = MessageSize.Medium;
        private Uri _serviceUri;
        private Type _contractType;
        private string _selectedOperationName = string.Empty;

        /// <summary>
        /// Shows the service.
        /// </summary>
        /// <param name="host">The host.</param>
        /// <param name="port">The port.</param>
        /// <param name="serviceId">The service id.</param>
        /// <param name="protocol">The protocol.</param>
        /// <param name="messageSize">Size of the message.</param>
        /// <param name="serviceUri">The service URI.</param>
        public void ShowService(ServiceHost host, int port, string serviceId, Protocol protocol, MessageSize messageSize, Uri serviceUri)
        {
            _port = port;
            _serviceId = serviceId;
            _protocol = protocol;
            _messageSize = messageSize;
            _serviceUri = serviceUri;

            listView1.ItemsSource = null;

            label2.Content = "Service URI: " + serviceUri.AbsoluteUri;

            var fieldInfo = typeof(ServiceHost).GetField("serviceType", BindingFlags.Instance | BindingFlags.NonPublic);
            if (fieldInfo == null) return;
            var serviceType = fieldInfo.GetValue(host) as Type;
            if (serviceType == null) return;

            var interfaces = serviceType.GetInterfaces();
            if (interfaces.Length < 1) return;
            var serviceInterface = interfaces[0];
            _contractType = serviceInterface;

            var methods = serviceInterface.GetMethods(BindingFlags.Instance | BindingFlags.Public).OrderBy(m => m.Name);
            var methodList = new ObservableCollection<MethodItem>();
            listView1.ItemsSource = methodList;

            foreach (var method in methods)
            {
                var isOperationContract = false;

                var attributes = method.GetCustomAttributes(true);
                foreach (var attribute in attributes)
                    if (attribute is OperationContractAttribute)
                    {
                        isOperationContract = true;
                        break;
                    }

                if (isOperationContract)
                    methodList.Add(new MethodItem {Name = method.Name, MethodInfo = method});
            }
        }

        private class MethodItem
        {
            public string Name { get; set; }
            public MethodInfo MethodInfo { get; set; }
        }

        private void NewOperationSelected(object sender, SelectionChangedEventArgs e)
        {
            if (listView1.SelectedItems.Count == 0) return;

            label1.Visibility = Visibility.Visible;

            foreach (var methodListItem in listView1.SelectedItems)
            {
                var methodItem = methodListItem as MethodItem;
                if (methodItem != null)
                {
                    label1.Content = methodItem.Name + " Operation";
                    _selectedOperationName = methodItem.Name;
                    var methodInfo = methodItem.MethodInfo;
                    var parameters = methodInfo.GetParameters();
                    if (parameters.Length > 0)
                    {
                        var firstParameter = parameters[0];

                        treeView1.DataContext = Activator.CreateInstance(firstParameter.ParameterType);

                        PopulateTreeView(firstParameter.ParameterType, firstParameter.Name, methodItem.Name);

                        if (parameters.Length > 1)
                            MessageBox.Show("Operations should only accept a single input message (parameter)");
                    }
                }
            }

        }

        private void PopulateTreeView(Type parameter, string parameterName, string methodName)
        {
            treeView1.Visibility = Visibility.Visible;

            var parameterList = new ObservableCollection<ParameterItem>();
            var rootParameter = new ParameterItem(treeView1.DataContext)
                                    {
                                        Name = parameterName, 
                                        NodeType = ParameterNodeType.Root,
                                        MethodName = methodName,
                                        ContractType = parameter.ToString(),
                                        IsRequired = false
                                    };
            parameterList.Add(rootParameter);

            treeView1.ItemsSource = parameterList;

            var valueObject = Activator.CreateInstance(parameter);

            var properties = parameter.GetProperties(BindingFlags.Instance | BindingFlags.Public).ToList().OrderBy(p => p.Name);
            foreach (var property in properties)
            {
                var attributes = property.GetCustomAttributes(true);
                foreach (var attribute in attributes)
                    if (attribute is DataMemberAttribute)
                    {
                        var dataMemberAttribute = (DataMemberAttribute)attribute;
                        var actualValue = property.GetValue(valueObject, null);
                        var newParameter = new ParameterItem(treeView1.DataContext)
                            {
                                Name = property.Name,
                                MemberType = property.PropertyType.Name,
                                IsEnum = property.PropertyType.IsEnum,
                                NodeType = ParameterNodeType.SimpleParameter,
                                ManipulationTextBoxVisible = Visibility.Visible,
                                TypeVisible = Visibility.Visible,
                                Value = actualValue,
                                IsRequired = dataMemberAttribute.IsRequired
                            };
                        if (newParameter.IsEnum)
                            newParameter.ConfigureEnum(property.PropertyType);
                        rootParameter.SubItems.Add(newParameter);
                        break;
                    }
            }
        }

        private void InvokeService(object sender, RoutedEventArgs e)
        {
            var data = treeView1.DataContext;

            if (_protocol != Protocol.RestHttpJson && _protocol != Protocol.RestHttpXml)
            {
                var service = ServiceClient.GetChannel(_contractType, _port, _serviceId, _protocol, _messageSize, _serviceUri);

                var methods = _contractType.GetMethods();
                foreach (var method in methods)
                    if (method.Name == _selectedOperationName)
                    {
                        var method1 = method;
                        new Thread(() =>
                                       {
                                           var response = method1.Invoke(service, new[] { data });
                                           Dispatcher.Invoke(new Action<object>(ShowDataContract), response);
                                       }).Start();
                        break;
                    }
            }
            else
            {
                // We make a plain old REST call over HTTP-POST
                if (_protocol == Protocol.RestHttpXml)
                {
                    new Thread(() =>
                        {
                            var client = new WebClient();
                            client.Headers.Add("Content-Type", "application/xml; charset=utf-8");
                            var postData = SerializeToRestXml(data);
                            var restResponse = client.UploadString(_serviceUri.AbsoluteUri + "/" + _selectedOperationName, postData);
                            Dispatcher.Invoke(new Action<string>(ShowRawData), restResponse);
                        }).Start();
                }
                else
                {
                    new Thread(() =>
                        {
                            var client = new WebClient();
                            client.Headers.Add("Content-Type", "application/json; charset=utf-8");
                            var postData = SerializeToRestJson(data);
                            var restResponse = client.UploadString(_serviceUri.AbsoluteUri + "/" + _selectedOperationName, postData);
                            Dispatcher.Invoke(new Action<string>(ShowRawData), restResponse);
                        }).Start();
                }
            }
        }

        /// <summary>
        /// Serializes to REST XML.
        /// </summary>
        /// <param name="objectToSerialize">The object to serialize.</param>
        /// <returns></returns>
        public static string SerializeToRestXml(object objectToSerialize)
        {
            var stream = new MemoryStream();
            var serializer = new DataContractSerializer(objectToSerialize.GetType());
            serializer.WriteObject(stream, objectToSerialize);
            return StreamHelper.ToString(stream);
        }

        /// <summary>
        /// Serializes to REST JSON.
        /// </summary>
        /// <param name="objectToSerialize">The object to serialize.</param>
        /// <returns></returns>
        public static string SerializeToRestJson(object objectToSerialize)
        {
            var stream = new MemoryStream();
            var serializer = new DataContractJsonSerializer(objectToSerialize.GetType());
            serializer.WriteObject(stream, objectToSerialize);
            return StreamHelper.ToString(stream);
        }

        private void ShowRawData(string data)
        {
            var text = new TextBox
                           {
                               Text = data, 
                               AcceptsReturn = true, 
                               AcceptsTab = true, 
                               TextWrapping = TextWrapping.Wrap, 
                               FontFamily = new FontFamily("Consolas"),
                               HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                               VerticalScrollBarVisibility = ScrollBarVisibility.Auto
                           };
            var window = new Window
                             {
                                 Title = "Response Data", 
                                 Content = text
                             };
            window.Show();
        }

        private void ShowDataContract(object contract)
        {
            Windows.ShowDataContract.Show(contract);
        }

        private void NumericNoDecimalKeyDown(object sender, KeyEventArgs e)
        {
            if (KeyIsMovement(e.Key)) return;

            if (!KeyIsNumeric(e.Key))
                e.Handled = true;
        }

        private void NumericWithDecimalKeyDown(object sender, KeyEventArgs e)
        {
            if (KeyIsMovement(e.Key)) return;

            if (!KeyIsNumeric(e.Key) && e.Key != Key.OemPeriod && e.Key != Key.OemComma)
                e.Handled = true;
        }

        private static bool KeyIsMovement(Key key)
        {
            return key == Key.Tab || key == Key.OemBackTab || key == Key.Home || key == Key.End ||
                   key == Key.Left || key == Key.Right || key == Key.Up || key == Key.Down ||
                   key == Key.Delete || key == Key.Back;
        }

        private static bool KeyIsNumeric(Key key)
        {
            return key == Key.D0 || key == Key.D1 || key == Key.D2 || key == Key.D3 || key == Key.D4 ||
                   key == Key.D5 || key == Key.D6 || key == Key.D7 || key == Key.D8 || key == Key.D9 ||
                   key == Key.NumPad0 || key == Key.NumPad1 || key == Key.NumPad2 || key == Key.NumPad3 || key == Key.NumPad4 ||
                   key == Key.NumPad5 || key == Key.NumPad6 || key == Key.NumPad7 || key == Key.NumPad8 || key == Key.NumPad9;
        }

        private void EmptyGuid_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null)
            {
                var grid = button.Parent as FrameworkElement;
                if (grid != null)
                {
                    var item = grid.DataContext as ParameterItem;
                    if (item != null)
                        item.Value = Guid.Empty;
                }
            }
        }

        private void RandomGuid_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null)
            {
                var grid = button.Parent as FrameworkElement;
                if (grid != null)
                {
                    var item = grid.DataContext as ParameterItem;
                    if (item != null)
                        item.Value = Guid.NewGuid();
                }
            }
        }

        private void GetRestXmlClick(object sender, RoutedEventArgs e)
        {
            ShowRawData(SerializeToRestXml(treeView1.DataContext));
        }

        private void GetRestJsonClick(object sender, RoutedEventArgs e)
        {
            ShowRawData(SerializeToRestJson(treeView1.DataContext));
        }
    }

    /// <summary>
    /// Template selector for the contracts
    /// </summary>
    public class EditContractTemplateSelector : DataTemplateSelector
    {
        /// <summary>
        /// When overridden in a derived class, returns a <see cref="T:System.Windows.DataTemplate"/> based on custom logic.
        /// </summary>
        /// <param name="item">The data object for which to select the template.</param>
        /// <param name="container">The data-bound object.</param>
        /// <returns>
        /// Returns a <see cref="T:System.Windows.DataTemplate"/> or null. The default value is null.
        /// </returns>
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var uiElement = container as UIElement;
            if (uiElement == null) return base.SelectTemplate(item, container);

            var ui = GetParentTillTree(uiElement);
            if (ui == null) return base.SelectTemplate(item, container);

            var node = item as ParameterItem;
            if (node == null) return base.SelectTemplate(item, container);

            var returnTemplate = ui.FindResource("Generic-Item") as HierarchicalDataTemplate;
            switch (node.NodeType)
            {
                case ParameterNodeType.Root:
                    returnTemplate = ui.FindResource("Root-Item") as HierarchicalDataTemplate;
                    break;
                case ParameterNodeType.SimpleParameter:
                    switch (node.MemberType.ToLower())
                    {
                        case "bool":
                        case "boolean":
                            returnTemplate = ui.FindResource("Boolean-Item") as HierarchicalDataTemplate;
                            break;
                        case "int":
                        case "integer":
                        case "int32":
                            returnTemplate = ui.FindResource("Integer-Item") as HierarchicalDataTemplate;
                            break;
                        case "decimal":
                            returnTemplate = ui.FindResource("Decimal-Item") as HierarchicalDataTemplate;
                            break;
                        case "guid":
                            returnTemplate = ui.FindResource("Guid-Item") as HierarchicalDataTemplate;
                            break;
                        case "single":
                        case "float":
                            returnTemplate = ui.FindResource("Float-Item") as HierarchicalDataTemplate;
                            break;
                        case "double":
                            returnTemplate = ui.FindResource("Double-Item") as HierarchicalDataTemplate;
                            break;
                        case "int64":
                        case "long":
                            returnTemplate = ui.FindResource("Long-Item") as HierarchicalDataTemplate;
                            break;
                        case "datetime":
                            returnTemplate = ui.FindResource("DateTime-Item") as HierarchicalDataTemplate;
                            break;

                        // TODO: byte[], collections
                    }

                    if (node.IsEnum) returnTemplate = ui.FindResource("Enum-Item") as HierarchicalDataTemplate;

                    break;
            }

            return returnTemplate;
        }

        private static FrameworkElement GetParentTillTree(UIElement item)
        {
            var returnValue = item as FrameworkElement;
            if (returnValue == null) return null;
            while (true)
            {
                returnValue = VisualTreeHelper.GetParent(returnValue) as FrameworkElement;
                if (returnValue == null) break;
                if (returnValue is TreeView) break;
            }
            return returnValue;
        }
    }

    /// <summary>
    /// Represents a parameter
    /// </summary>
    public class ParameterItem : INotifyPropertyChanged
    {
        private readonly object _dataContext;
        /// <summary>
        /// Initializes a new instance of the <see cref="ParameterItem"/> class.
        /// </summary>
        /// <param name="dataContext">The data context.</param>
        public ParameterItem(object dataContext)
        {
            _dataContext = dataContext;
            MemberType = "String";
            SubItems = new ObservableCollection<ParameterItem>();
            ManipulationTextBoxVisible = Visibility.Collapsed;
            TypeVisible = Visibility.Collapsed;
            TextValue = string.Empty;
        }
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; set; }
        /// <summary>
        /// Gets or sets the type of the member.
        /// </summary>
        /// <value>
        /// The type of the member.
        /// </value>
        public string MemberType { get; set; }
        /// <summary>
        /// Gets or sets the name of the method.
        /// </summary>
        /// <value>
        /// The name of the method.
        /// </value>
        public string MethodName { get; set; }
        /// <summary>
        /// Gets or sets the type of the contract.
        /// </summary>
        /// <value>
        /// The type of the contract.
        /// </value>
        public string ContractType { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is an enum.
        /// </summary>
        /// <value><c>true</c> if this instance is enum; otherwise, <c>false</c>.</value>
        public bool IsEnum { get; set; }

        /// <summary>
        /// Is the null element visible?
        /// </summary>
        public Visibility NullElementVisible
        {
            get
            {
                if (_value == null) return Visibility.Visible;
                return Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Gets the member type full text.
        /// </summary>
        public string MemberTypeFullText
        {
            get
            {
                var fullText = MemberType;
                if (IsRequired) fullText += " (required)";
                return fullText;
            }
        }

        private object _value;
        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>
        /// The value.
        /// </value>
        public object Value
        {
            get { return _value; }
            set
            {
                _value = value;

                if (!string.IsNullOrEmpty(Name) && _dataContext != null)
                {
                    var contractType = _dataContext.GetType();
                    var member = contractType.GetProperty(Name, BindingFlags.Public | BindingFlags.Instance);
                    member.SetValue(_dataContext, value, null);
                }

                NotifyChanged();
            }
        }
        /// <summary>
        /// Gets or sets the text value.
        /// </summary>
        /// <value>
        /// The text value.
        /// </value>
        public string TextValue
        {
            get
            {
                if (Value == null) return string.Empty;
                return Value.ToString();
            }
            set
            {
                try
                {
                    switch (MemberType.ToLower())
                    {
                        case "bool":
                        case "boolean":
                            Value = value.ToLower() == "true";
                            break;
                        case "datetime":
                            Value = DateTime.Parse(value);
                            break;
                        case "int":
                        case "int32":
                        case "integer":
                            Value = int.Parse(value);
                            break;
                        case "int64":
                        case "long":
                            Value = long.Parse(value);
                            break;
                        case "decimal":
                            Value = decimal.Parse(value, CultureInfo.InvariantCulture);
                            break;
                        case "double":
                            Value = double.Parse(value);
                            break;
                        case "guid":
                            Value = new Guid(value);
                            break;
                        case "float":
                        case "single":
                            Value = float.Parse(value);
                            break;
                        default:
                            Value = value;
                            break;
                    }

                    NotifyChanged();
                }
                catch (Exception)
                {
                }
            }
        }
        /// <summary>
        /// Gets or sets a value indicating whether [boolean value].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [boolean value]; otherwise, <c>false</c>.
        /// </value>
        public bool BooleanValue
        {
            get { return (bool)Value; }
            set { Value = value; }
        }
        /// <summary>
        /// Gets or sets the integer value.
        /// </summary>
        /// <value>
        /// The integer value.
        /// </value>
        public int IntegerValue
        {
            get { return (int)Value; }
            set { Value = value; }
        }
        /// <summary>
        /// Gets or sets the decimal value.
        /// </summary>
        /// <value>
        /// The decimal value.
        /// </value>
        public decimal DecimalValue
        {
            get { return (decimal)Value; }
            set { Value = value; }
        }
        /// <summary>
        /// Gets or sets the double value.
        /// </summary>
        /// <value>
        /// The double value.
        /// </value>
        public double DoubleValue
        {
            get { return (double)Value; }
            set { Value = value; }
        }
        /// <summary>
        /// Gets or sets the float value.
        /// </summary>
        /// <value>
        /// The float value.
        /// </value>
        public float FloatValue
        {
            get { return (float)Value; }
            set { Value = value; }
        }
        /// <summary>
        /// Gets or sets the long value.
        /// </summary>
        /// <value>
        /// The long value.
        /// </value>
        public long LongValue
        {
            get { return (long)Value; }
            set { Value = value; }
        }

        /// <summary>
        /// Gets or sets the enum value.
        /// </summary>
        /// <value>The enum value.</value>
        public string EnumValue
        {
            get { return Value.ToString(); }
            set { Value = Enum.Parse(_enumType, value); }
        }
        /// <summary>
        /// Gets or sets the decimal value.
        /// </summary>
        /// <value>
        /// The decimal value.
        /// </value>
        public string DateTimeValue
        {
            get { return ((DateTime)Value).ToString(CultureInfo.CurrentUICulture); }
            set
            {
                DateTime newDate;
                if (DateTime.TryParse(value, out newDate))
                    Value = newDate;
            }
        }

        /// <summary>
        /// Gets or sets the enum values.
        /// </summary>
        /// <value>The enum values.</value>
        public ObservableCollection<string> EnumValues { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is required.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is required; otherwise, <c>false</c>.
        /// </value>
        public bool IsRequired { get; set; }
        /// <summary>
        /// Gets or sets the manipulation text box visible.
        /// </summary>
        /// <value>
        /// The manipulation text box visible.
        /// </value>
        public Visibility ManipulationTextBoxVisible { get; set; }
        /// <summary>
        /// Gets or sets the type visible.
        /// </summary>
        /// <value>
        /// The type visible.
        /// </value>
        public Visibility TypeVisible { get; set; }
        /// <summary>
        /// Gets or sets the type of the node.
        /// </summary>
        /// <value>
        /// The type of the node.
        /// </value>
        public ParameterNodeType NodeType { get; set; }
        /// <summary>
        /// Gets or sets the sub items.
        /// </summary>
        /// <value>
        /// The sub items.
        /// </value>
        public ObservableCollection<ParameterItem> SubItems { get; set; }

        private void NotifyChanged()
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(string.Empty));
        }

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Configures enum settings
        /// </summary>
        /// <param name="propertyType">Type of the property.</param>
        public void ConfigureEnum(Type propertyType)
        {
            _enumType = propertyType;
            var names = Enum.GetNames(propertyType);
            var values = new ObservableCollection<string>();
            foreach (var name in names)
                values.Add(name);
            EnumValues = values;
            NotifyChanged();
        }

        private Type _enumType;
    }

    /// <summary>
    /// Parameter node type
    /// </summary>
    public enum ParameterNodeType
    {
        /// <summary>
        /// Root element
        /// </summary>
        Root,
        /// <summary>
        /// Simple parameter
        /// </summary>
        SimpleParameter,
        /// <summary>
        /// Complex parameter
        /// </summary>
        ComplexParameter,
        /// <summary>
        /// List paramete
        /// </summary>
        List
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using CODE.Framework.Core.Utilities;
using CODE.Framework.Core.Utilities.Extensions;
using CODE.Framework.Services.Client;
using Button = System.Windows.Controls.Button;
using Control = System.Windows.Controls.Control;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;
using RestHelper = CODE.Framework.Services.Server.RestHelper;
using TextBox = System.Windows.Controls.TextBox;
using TreeView = System.Windows.Controls.TreeView;
using UserControl = System.Windows.Controls.UserControl;

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
            Logo.Source = BitmapToImageSource(Properties.Resources.CodeFramework_Logo160);
        }

        /// <summary>
        /// Retrieves an image source from the bitmap
        /// </summary>
        /// <param name="bitmap">The bitmap.</param>
        /// <returns>ImageSource.</returns>
        public static ImageSource BitmapToImageSource(System.Drawing.Image bitmap)
        {
            using (var memory = new MemoryStream())
            {
                bitmap.Save(memory, ImageFormat.Png);
                memory.Position = 0;
                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                return bitmapImage;
            }
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

            OperationsList.ItemsSource = null;

            if (protocol == Protocol.BasicHttp || protocol == Protocol.WsHttp)
            {
                var brush = Resources["IconBackground"] as SolidColorBrush;
                if (brush != null)
                    brush.Color = Color.FromRgb(255, 153, 51);
            }
            else if (protocol == Protocol.NetTcp || protocol == Protocol.InProcess)
            {
                var brush = Resources["IconBackground"] as SolidColorBrush;
                if (brush != null)
                    brush.Color = Color.FromRgb(0, 153, 51);
            }

            var fieldInfo = typeof (ServiceHost).GetField("serviceType", BindingFlags.Instance | BindingFlags.NonPublic);
            if (fieldInfo == null) return;
            var serviceType = fieldInfo.GetValue(host) as Type;
            if (serviceType == null) return;

            var interfaces = serviceType.GetInterfaces();
            if (interfaces.Length < 1) return;
            var serviceInterface = interfaces[0];
            _contractType = serviceInterface;

            var methods = ObjectHelper.GetAllMethodsForInterface(serviceInterface).OrderBy(m => m.Name).ToList();
            var methodList = new ObservableCollection<MethodItem>();
            OperationsList.ItemsSource = methodList;

            foreach (var method in methods)
                if (method.GetCustomAttributes(true).OfType<OperationContractAttribute>().Any())
                    methodList.Add(new MethodItem {Name = method.Name, MethodInfo = method});
        }

        private class MethodItem
        {
            public string Name { get; set; }
            public MethodInfo MethodInfo { get; set; }
        }

        private void NewOperationSelected(object sender, SelectionChangedEventArgs e)
        {
            if (OperationsList.SelectedItems.Count == 0) return;

            OperationNameLabel.Visibility = Visibility.Visible;
            RefreshSelectedOperations();
            RefreshJsonAndXmlFromTree();
        }

        private void RefreshSelectedOperations(object parameterValue = null)
        {
            foreach (var methodListItem in OperationsList.SelectedItems)
            {
                var methodItem = methodListItem as MethodItem;
                if (methodItem != null)
                {
                    OperationNameLabel.Content = methodItem.Name + " Operation";
                    _selectedOperationName = methodItem.Name;
                    var methodInfo = methodItem.MethodInfo;
                    var parameters = methodInfo.GetParameters();
                    if (parameters.Length > 0)
                    {
                        var firstParameter = parameters[0];

                        RequestContractTree.DataContext = parameterValue ?? Activator.CreateInstance(firstParameter.ParameterType);
                        PopulateTreeView(firstParameter.ParameterType, firstParameter.Name, methodItem.Name, parameterValue);

                        if (parameters.Length > 1)
                            MessageBox.Show("Operations should only accept a single input message (parameter)");
                    }
                }
            }
        }

        private void PopulateTreeView(Type parameter, string parameterName, string methodName, object parameterValue = null)
        {
            RequestContractTree.Visibility = Visibility.Visible;
            _mustRefreshUrlOnDataChange = false;

            var parameterList = new ObservableCollection<ParameterItem>();
            var rootParameter = new ParameterItem(RequestContractTree.DataContext)
            {
                Name = parameterName,
                NodeType = ParameterNodeType.Root,
                MethodName = methodName,
                ContractType = parameter.ToString(),
                IsRequired = false
            };
            if (_protocol != Protocol.RestHttpJson && _protocol != Protocol.RestHttpXml)
                rootParameter.OperationUrl = _serviceUri.AbsoluteUri;
            else
            {
                _requestHttpMethod = RestHelper.GetHttpMethodFromContract(_selectedOperationName, _contractType);
                _currentRootUrl = _serviceUri.AbsoluteUri + "/" + RestHelper.GetExposedMethodNameFromContract(methodName, _requestHttpMethod, _contractType);
                _mustRefreshUrlOnDataChange = true;
                rootParameter.OperationUrl = _requestHttpMethod + ": " + GetUrlForCurrentData();
            }
            parameterList.Add(rootParameter);
            _currentRootParameter = rootParameter;

            RequestContractTree.ItemsSource = parameterList;

            var valueObject = parameterValue ?? Activator.CreateInstance(parameter);
            PopulateTreeBranch(parameter, valueObject, rootParameter, RequestContractTree.DataContext);
        }

        private string GetUrlForCurrentData()
        {
            return _currentRootUrl + RestHelper.SerializeToUrlParameters(RequestContractTree.DataContext, _requestHttpMethod);
        }

        private void PopulateTreeBranch(Type parameter, object valueObject, ParameterItem parentParameter, object dataContext)
        {
            var properties = parameter.GetProperties(BindingFlags.Instance | BindingFlags.Public).ToList().OrderBy(p => p.Name);
            foreach (var property in properties)
            {
                var attributes = property.GetCustomAttributes(true);
                foreach (var attribute in attributes)
                {
                    var memberAttribute = attribute as DataMemberAttribute;
                    if (memberAttribute == null) continue;
                    var dataMemberAttribute = memberAttribute;
                    var actualValue = property.GetValue(valueObject, null);
                    var newParameter = new ParameterItem(dataContext)
                    {
                        Name = property.Name,
                        MemberType = property.PropertyType.Name.Replace("Byte[]", "Byte[]/Binary"),
                        IsEnum = property.PropertyType.IsEnum,
                        NodeType = ParameterNodeType.SimpleParameter,
                        ManipulationTextBoxVisible = Visibility.Visible,
                        TypeVisible = Visibility.Visible,
                        Value = actualValue,
                        IsRequired = dataMemberAttribute.IsRequired
                    };
                    if (newParameter.IsEnum)
                        newParameter.ConfigureEnum(property.PropertyType);
                    parentParameter.SubItems.Add(newParameter);

                    if (_mustRefreshUrlOnDataChange)
                        newParameter.PropertyChanged += (s, e) => _currentRootParameter.OperationUrl = _requestHttpMethod + ": " + GetUrlForCurrentData();

                    if (!property.PropertyType.IsValueType && property.PropertyType != typeof(Guid) && property.PropertyType != typeof(string) && property.PropertyType != typeof(byte[]))
                    {
                        if (property.PropertyType.IsArray || property.PropertyType.FullName.StartsWith("System.Collections.Generic.List`1["))
                        {
                            newParameter.NodeType = ParameterNodeType.List;
                            if (property.PropertyType.IsGenericType)
                            {
                                var genericTypes = property.PropertyType.GetGenericArguments();
                                var genericTypeNames = string.Empty;
                                foreach (var genericType in genericTypes)
                                {
                                    if (!string.IsNullOrEmpty(genericTypeNames)) genericTypeNames += ",";
                                    genericTypeNames += genericType.Name;
                                    newParameter.ListGenericType = genericType;
                                }
                                newParameter.MemberType = newParameter.MemberType.Replace("List`1", "List<" + genericTypeNames + ">");
                            }

                            dynamic enumerable = newParameter.Value;
                            var itemCounter = 0;
                            foreach (var item in enumerable)
                            {
                                var itemType = item.GetType();
                                var newParameter2 = new ParameterItem(item)
                                {
                                    Name = itemCounter.ToString(CultureInfo.InvariantCulture),
                                    MemberType = itemType.Name,
                                    IsEnum = itemType.IsEnum,
                                    NodeType = ParameterNodeType.SimpleParameter,
                                    ManipulationTextBoxVisible = Visibility.Visible,
                                    TypeVisible = Visibility.Visible,
                                    Value = item,
                                    IsRequired = dataMemberAttribute.IsRequired,
                                    EnumIndex = itemCounter,
                                    ParentCollection = enumerable
                                };
                                if (!itemType.IsValueType && itemType != typeof(Guid) && itemType != typeof(string) && itemType != typeof(byte[]))
                                    newParameter2.NodeType = ParameterNodeType.ComplexParameter;
                                newParameter.SubItems.Add(newParameter2);
                                PopulateTreeBranch(itemType, item, newParameter2, item);
                                itemCounter++;
                            }
                        }
                        else
                        {
                            newParameter.NodeType = ParameterNodeType.ComplexParameter;
                            if (newParameter.Value != null) PopulateTreeBranch(property.PropertyType, newParameter.Value, newParameter, newParameter.Value);
                        }
                    }

                    break;
                }
            }
        }

        private void InvokeService(object sender, RoutedEventArgs e)
        {
            var data = RequestContractTree.DataContext;
            if (data == null)
            {
                MessageBox.Show("Please select the operation you would like to invoke.");
                return;
            }

            if (_protocol != Protocol.RestHttpXml && _protocol != Protocol.RestHttpJson)
            {
                var service = ServiceClient.GetChannel(_contractType, _port, _serviceId, _protocol, _messageSize, _serviceUri);

                var methods = ObjectHelper.GetAllMethodsForInterface(_contractType);
                foreach (var method in methods.Where(method => method.Name == _selectedOperationName))
                {
                    var method1 = method;
                    new Thread(() =>
                    {
                        var response = method1.Invoke(service, new[] {data});
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
                        var httpMethod = RestHelper.GetHttpMethodFromContract(_selectedOperationName, _contractType);
                        var exposedMethodName = RestHelper.GetExposedMethodNameFromContract(_selectedOperationName, httpMethod, _contractType);
                        using (var client = new WebClient())
                        {
                            client.Headers.Add("Content-Type", "application/xml; charset=utf-8");
                            string restResponse;
                            switch (httpMethod)
                            {
                                case "POST":
                                    restResponse = client.UploadString(_serviceUri.AbsoluteUri + "/" + exposedMethodName, SerializeToRestXml(data));
                                    break;
                                case "GET":
                                    restResponse = client.DownloadString(_serviceUri.AbsoluteUri + "/" + exposedMethodName + RestHelper.SerializeToUrlParameters(data));
                                    break;
                                default:
                                    restResponse = client.UploadString(_serviceUri.AbsoluteUri + "/" + exposedMethodName, httpMethod, SerializeToRestXml(data));
                                    break;
                            }
                            Dispatcher.Invoke(new Action<string>(ShowRawData), restResponse);
                        }
                    }).Start();
                }
                else
                {
                    new Thread(() =>
                    {
                        var httpMethod = RestHelper.GetHttpMethodFromContract(_selectedOperationName, _contractType);
                        var exposedMethodName = RestHelper.GetExposedMethodNameFromContract(_selectedOperationName, httpMethod, _contractType);
                        using (var client = new WebClient())
                        {
                            client.Headers.Add("Content-Type", "application/json; charset=utf-8");
                            string restResponse;
                            switch (httpMethod)
                            {
                                case "POST":
                                    var jsonPost = JsonHelper.SerializeToRestJson(data);
                                    restResponse = client.UploadString(_serviceUri.AbsoluteUri + "/" + exposedMethodName, jsonPost);
                                    break;
                                case "GET":
                                    MessageBox.Show("REST operations with HTTP-GET Verbs are not currently supported for self-hosted services. Please use a different HTTP Verb or host your REST service in a different host (wuch as WebApi). We are hoping to add HTTP-GET support for self-hosted services in the future.\r\n\r\nIf you have further questions, feel free to contact us: info@codemag.com", "CODE Framework");
                                    return;
                                    // TODO: Add support for this.
                                    var serializedData = RestHelper.SerializeToUrlParameters(data);
                                    restResponse = client.DownloadString(_serviceUri.AbsoluteUri + "/" + exposedMethodName + serializedData);
                                    break;
                                default:
                                    var json = JsonHelper.SerializeToRestJson(data);
                                    restResponse = client.UploadString(_serviceUri.AbsoluteUri + "/" + exposedMethodName, httpMethod, json);
                                    break;
                            }
                            Dispatcher.Invoke(new Action<string>(ShowRawData), restResponse);
                        }
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
            try
            {
                serializer.WriteObject(stream, objectToSerialize);
                return StreamHelper.ToString(stream);
            }
            catch (Exception ex)
            {
                return ExceptionHelper.GetExceptionText(ex);
            }
        }

        /// <summary>
        /// Deserializes from REST XML
        /// </summary>
        /// <param name="xml">The XML.</param>
        /// <param name="resultType">Type of the result object.</param>
        /// <returns>Deserialized object</returns>
        public static object DeserializeFromRestXml(string xml, Type resultType)
        {
            try
            {
                var isUtf16 = xml.Contains("encoding=\"utf-16\"");
                var serializer = new DataContractSerializer(resultType);
                using (var stream = new MemoryStream(isUtf16 ? System.Text.Encoding.Unicode.GetBytes(xml) : System.Text.Encoding.ASCII.GetBytes(xml)))
                    return serializer.ReadObject(stream);
            }
            catch (Exception)
            {
                return null;
            }
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

        private void LoadByteArrayFromFile_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null)
            {
                var grid = button.Parent as FrameworkElement;
                if (grid != null)
                {
                    var item = grid.DataContext as ParameterItem;
                    if (item != null)
                    {
                        var dlg = new OpenFileDialog {CheckFileExists = true, CheckPathExists = true, Multiselect = false, RestoreDirectory = true};
                        if (dlg.ShowDialog() == DialogResult.OK)
                            using (var file = File.OpenRead(dlg.FileName))
                            {
                                var bytes = new byte[file.Length];
                                file.Read(bytes, 0, (int) file.Length);
                                item.Value = bytes;
                            }
                    }
                }
            }
        }

        private void AddItemToList(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null) return;
            var grid = button.Parent as FrameworkElement;
            if (grid == null) return;
            var item = grid.DataContext as ParameterItem;
            if (item == null || item.NodeType != ParameterNodeType.List) return;
            dynamic list = item.Value;
            if (item.ListGenericType == null)
                MessageBox.Show("Arrays cannot be resized on the fly.");
            else if (item.ListGenericType == typeof (string))
                list.Add("");
            else if (item.ListGenericType == typeof (int))
                list.Add(0);
            else if (item.ListGenericType == typeof (decimal))
                list.Add(0m);
            else if (item.ListGenericType == typeof (double))
                list.Add(0d);
            else if (item.ListGenericType == typeof(float))
                list.Add(0f);
            else if (item.ListGenericType == typeof (bool))
                list.Add(false);
            else if (item.ListGenericType == typeof (Guid))
                list.Add(Guid.Empty);
            else
            {
                try
                {
                    dynamic newItem = Activator.CreateInstance(item.ListGenericType);
                    list.Add(newItem);
                }
                catch
                {
                }
            }
            var parameterItem = RequestContractTree.Items[0] as ParameterItem;
            if (parameterItem != null) RefreshSelectedOperations(parameterItem.DataContext);
        }

        private int _lastSelectedContractDisplayIndex;

        private void HandleContractViewSwitched(object sender, SelectionChangedEventArgs e)
        {
            var lastIndex = _lastSelectedContractDisplayIndex;
            if (lastIndex == ContractDisplayTab.SelectedIndex) return;

            var data = RequestContractTree.DataContext;
            if (data == null) return;

            JsonContract.Background = Brushes.White;
            XmlContract.Background = Brushes.White;

            switch (lastIndex)
            {
                case 0: // Going from the tree to other items
                    RefreshJsonAndXmlFromTree();
                    _lastSelectedContractDisplayIndex = ContractDisplayTab.SelectedIndex;
                    break;
                case 1: // Going from JSON to something else
                    var contractObject = JsonHelper.DeserializeFromRestJson(JsonContract.Text, RequestContractTree.DataContext.GetType());
                    if (contractObject != null)
                    {
                        RefreshSelectedOperations(contractObject);
                        XmlContract.Text = XmlHelper.Format(SerializeToRestXml(RequestContractTree.DataContext));
                        _lastSelectedContractDisplayIndex = ContractDisplayTab.SelectedIndex;
                    }
                    else
                    {
                        JsonContract.Background = new SolidColorBrush(Color.FromArgb(40, 255, 0, 0));
                        MessageBox.Show("JSON is either invalid or doesn't match the current contract structure.");
                        ContractDisplayTab.SelectedIndex = 1;
                    }
                    break;
                case 2:
                    var contractObject2 = DeserializeFromRestXml(XmlContract.Text, RequestContractTree.DataContext.GetType());
                    if (contractObject2 != null)
                    {
                        RefreshSelectedOperations(contractObject2);
                        JsonContract.Text = JsonHelper.Format(JsonHelper.SerializeToRestJson(RequestContractTree.DataContext));
                        _lastSelectedContractDisplayIndex = ContractDisplayTab.SelectedIndex;
                    }
                    else
                    {
                        XmlContract.Background = new SolidColorBrush(Color.FromArgb(40, 255, 0, 0));
                        MessageBox.Show("XML is either invalid or doesn't match the current contract structure.");
                        ContractDisplayTab.SelectedIndex = 2;
                    }
                    break;
            }
        }

        private void HandleSaveAs(object sender, RoutedEventArgs e)
        {
            var data = RequestContractTree.DataContext;
            if (data == null)
            {
                MessageBox.Show("Please select an operation first.");
                return;
            }
            
            var currentFolderName = GetFullPathForCurrentRequest(data.GetType());
            var dlg = new SaveFileDialog {InitialDirectory = currentFolderName, DefaultExt = "json", AddExtension = true, FileName = "Request.json", RestoreDirectory = true, SupportMultiDottedExtensions = true, Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*"};
            if (dlg.ShowDialog() != DialogResult.OK) return;
            var path = dlg.FileName.JustPath();
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            StringHelper.ToFile(JsonHelper.Format(JsonHelper.SerializeToRestJson(RequestContractTree.DataContext)), dlg.FileName);
        }

        private void HandleLoad(object sender, RoutedEventArgs e)
        {
            var data = RequestContractTree.DataContext;
            if (data == null)
            {
                MessageBox.Show("Please select an operation first.");
                return;
            }
            var currentFolderName = GetFullPathForCurrentRequest(data.GetType());
            var dlg = new OpenFileDialog {InitialDirectory = currentFolderName, DefaultExt = "json", AddExtension = true, FileName = "Request.json", RestoreDirectory = true, SupportMultiDottedExtensions = true, Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*", CheckFileExists = true, CheckPathExists = true};
            if (dlg.ShowDialog() != DialogResult.OK || !File.Exists(dlg.FileName)) return;
            var json = StringHelper.FromFile(dlg.FileName);
            var contractObject = JsonHelper.DeserializeFromRestJson(json, data.GetType());
            if (contractObject == null)
            {
                MessageBox.Show("The JSON file didn't contain valid JSON, or the structure didn't match the current request contract.");
                return;
            }
            RefreshSelectedOperations(contractObject);
            RefreshJsonAndXmlFromTree();
        }

        private void HandleQuickSave(object sender, RoutedEventArgs e)
        {
            var data = RequestContractTree.DataContext;
            if (data == null)
            {
                MessageBox.Show("Please select an operation first.");
                return;
            }
            var currentFolderName = GetFullPathForCurrentRequest(data.GetType());
            if (!Directory.Exists(currentFolderName)) Directory.CreateDirectory(currentFolderName);
            var quickFileName = currentFolderName + @"\QuickSave.json";
            StringHelper.ToFile(JsonHelper.Format(JsonHelper.SerializeToRestJson(RequestContractTree.DataContext)), quickFileName);
            DisplayFlashMessage("Saved", Brushes.SeaGreen);
        }

        private void HandleQuickLoad(object sender, RoutedEventArgs e)
        {
            var data = RequestContractTree.DataContext;
            if (data == null)
            {
                MessageBox.Show("Please select an operation first.");
                return;
            }
            var currentFolderName = GetFullPathForCurrentRequest(data.GetType());
            if (!Directory.Exists(currentFolderName))
            {
                DisplayFlashMessage("Not found!", Brushes.Crimson);
                return;
            }
            var quickFileName = currentFolderName + @"\QuickSave.json";
            if (!File.Exists(quickFileName))
            {
                DisplayFlashMessage("Not found!", Brushes.Crimson);
                return;
            }
            var json = StringHelper.FromFile(quickFileName);
            var contractObject = JsonHelper.DeserializeFromRestJson(json, data.GetType());
            if (contractObject == null)
            {
                MessageBox.Show("The JSON file didn't contain valid JSON, or the structure didn't match the current request contract.");
                return;
            }
            RefreshSelectedOperations(contractObject);
            RefreshJsonAndXmlFromTree();
            DisplayFlashMessage("Loaded", Brushes.CornflowerBlue);
        }

        private void RefreshJsonAndXmlFromTree()
        {
            var json = JsonHelper.SerializeToRestJson(RequestContractTree.DataContext);
            json = JsonHelper.Format(json);
            JsonContract.Text = json;
            XmlContract.Text = XmlHelper.Format(SerializeToRestXml(RequestContractTree.DataContext));
        }

        private void DisplayFlashMessage(string message, Brush color)
        {
            FlashLabel.Content = message;
            FlashLabel.Foreground = color;
            var storyboard = Resources["ShowFlashMessage"] as Storyboard;
            if (storyboard != null) storyboard.Begin();
        }

        private string GetFullPathForCurrentRequest(Type requestType)
        {
            var currentDirectory = Directory.GetCurrentDirectory().JustPath();
            if (currentDirectory.ToLower().EndsWith(@"\bin\debug")) currentDirectory = currentDirectory.Substring(0, currentDirectory.Length - 10);
            if (currentDirectory.ToLower().EndsWith(@"\bin\release")) currentDirectory = currentDirectory.Substring(0, currentDirectory.Length - 12);
            if (currentDirectory.ToLower().EndsWith(@"\bin")) currentDirectory = currentDirectory.Substring(0, currentDirectory.Length - 4);
            currentDirectory += @"\Requests\" + _contractType.FullName + @"\" + requestType.FullName;
            return currentDirectory;
        }

        private bool _ctrlDown;
        private string _requestHttpMethod;
        private string _currentRootUrl;
        private bool _mustRefreshUrlOnDataChange;
        private ParameterItem _currentRootParameter;

        /// <summary>
        /// Invoked when an unhandled PreviewKeyDown attached event reaches an element in its route that is derived from this class. Implement this method to add class handling for this event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.Windows.Input.KeyEventArgs" /> that contains the event data.</param>
        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);
            if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl) _ctrlDown = true;
            if (_ctrlDown && e.Key == Key.S) HandleQuickSave(this, new RoutedEventArgs());
            if (_ctrlDown && (e.Key == Key.L || e.Key == Key.O)) HandleQuickLoad(this, new RoutedEventArgs());
            if (_ctrlDown && (e.Key == Key.I || e.Key == Key.Enter)) InvokeService(this, new RoutedEventArgs());
        }

        /// <summary>
        /// Invoked when an unhandled PreviewKeyUp attached event reaches an element in its route that is derived from this class. Implement this method to add class handling for this event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.Windows.Input.KeyEventArgs" /> that contains the event data.</param>
        protected override void OnPreviewKeyUp(KeyEventArgs e)
        {
            base.OnPreviewKeyUp(e);
            if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl) _ctrlDown = false;
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
                        case "byte[]/binary":
                        case "byte[]":
                            returnTemplate = ui.FindResource("ByteArray-Item") as HierarchicalDataTemplate;
                            break;
                    }

                    if (node.IsEnum) returnTemplate = ui.FindResource("Enum-Item") as HierarchicalDataTemplate;
                    break;

                case ParameterNodeType.List:
                    returnTemplate = ui.FindResource("List-Item") as HierarchicalDataTemplate;
                    break;

                case ParameterNodeType.ComplexParameter:
                    returnTemplate = ui.FindResource("SubObject-Item") as HierarchicalDataTemplate;
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
        private object _dataContext;

        /// <summary>
        /// Gets the data context.
        /// </summary>
        /// <value>The data context.</value>
        public object DataContext
        {
            get
            {
                return _dataContext;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ParameterItem"/> class.
        /// </summary>
        /// <param name="dataContext">The data context.</param>
        public ParameterItem(object dataContext)
        {
            _dataContext = dataContext;
            MemberType = "String";
            OperationUrl = string.Empty;
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
        /// URL information for the current operation (only used on root items)
        /// </summary>
        /// <value>The operation URL.</value>
        public string OperationUrl
        {
            get { return _operationUrl; }
            set { _operationUrl = value; NotifyChanged(); }
        }

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
                    if (ParentCollection != null)
                    {
                        _dataContext = value;
                        if (ParentCollection is List<string>)
                        {
                            var collection = (List<string>)ParentCollection;
                            collection[EnumIndex] = value.ToString();
                        }
                        else if (ParentCollection is List<int>)
                        {
                            var collection = (List<int>)ParentCollection;
                            collection[EnumIndex] = (int)value;
                        }
                        else if (ParentCollection is List<decimal>)
                        {
                            var collection = (List<decimal>)ParentCollection;
                            collection[EnumIndex] = (decimal)value;
                        }
                        else if (ParentCollection is List<float>)
                        {
                            var collection = (List<float>)ParentCollection;
                            collection[EnumIndex] = (float)value;
                        }
                        else if (ParentCollection is List<DateTime>)
                        {
                            var collection = (List<DateTime>)ParentCollection;
                            collection[EnumIndex] = (DateTime)value;
                        }
                        else if (ParentCollection is List<bool>)
                        {
                            var collection = (List<bool>)ParentCollection;
                            collection[EnumIndex] = (bool)value;
                        }
                        else if (ParentCollection is List<double>)
                        {
                            var collection = (List<double>)ParentCollection;
                            collection[EnumIndex] = (double)value;
                        }
                        else if (ParentCollection is List<Guid>)
                        {
                            var collection = (List<Guid>)ParentCollection;
                            collection[EnumIndex] = (Guid)value;
                        }
                    }
                    else
                    {
                        var contractType = _dataContext.GetType();
                        var member = contractType.GetProperty(Name, BindingFlags.Public | BindingFlags.Instance);
                        if (member != null)
                            member.SetValue(_dataContext, value, null);
                    }
                }

                NotifyChanged();
            }
        }

        /// <summary>
        /// Gets the byte array display value.
        /// </summary>
        /// <value>The byte array display value.</value>
        public string ByteArrayDisplayValue
        {
            get
            {
                if (_value is byte[])
                {
                    var byteArray = _value as byte[];
                    var displayText = string.Empty;
                    foreach (var byteItem in byteArray)
                    {
                        displayText += byteItem.ToString(CultureInfo.InvariantCulture);
                        if (displayText.Length > 25)
                        {
                            displayText += "...";
                            break;
                        }
                    }
                    return displayText;
                }
                return string.Empty;
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
                catch
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
            get { return (bool) Value; }
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
            get { return (int) Value; }
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
            get { return (decimal) Value; }
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
            get { return (double) Value; }
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
            get { return (float) Value; }
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
            get { return (long) Value; }
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
            get { return ((DateTime) Value).ToString(CultureInfo.CurrentUICulture); }
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

        /// <summary>
        /// Generic type used by the list
        /// </summary>
        /// <value>The type of the list's generic parameter.</value>
        public Type ListGenericType { get; set; }

        /// <summary>
        /// Gets or sets the index of the enum.
        /// </summary>
        /// <value>The index of the enum.</value>
        public int EnumIndex { get; set; }

        /// <summary>
        /// Gets or sets the parent collection.
        /// </summary>
        /// <value>The parent collection.</value>
        public object ParentCollection { get; set; }

        /// <summary>
        /// Notifies the changed.
        /// </summary>
        private void NotifyChanged()
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(string.Empty));
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
        private string _operationUrl;
    }

    /// <summary>
    /// Helper class used to move the focus within elements of the contract entry tree
    /// </summary>
    public class ControlFocusHelper : DependencyObject
    {
        /// <summary>If set to true, moves the cursor on key press events</summary>
        public static readonly DependencyProperty AutoMoveCursorToNextTreeItemProperty = DependencyProperty.RegisterAttached("AutoMoveCursorToNextTreeItem", typeof(bool), typeof(ControlFocusHelper), new PropertyMetadata(false, OnAutoMoveCursorToNextTreeItemChanged));

        /// <summary>
        /// Fires when AutoMoveCursorToNextTreeItem property changes
        /// </summary>
        /// <param name="d">The d.</param>
        /// <param name="args">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void OnAutoMoveCursorToNextTreeItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            if (!(bool) args.NewValue) return;
            var control = d as Control;
            if (control == null) return;
            control.PreviewKeyDown += (s, e) =>
            {
                if (e.Key == Key.Tab)
                {
                    var element = s as UIElement;
                    if (element == null) return;
                    var treeViewItem = GetParent<TreeViewItem>(element);
                    if (treeViewItem == null) return;
                    // TODO: This is not currently working
                }
            };
        }

        private static T GetParent<T>(DependencyObject element) where T : class
        {
            while (true)
            {
                element = VisualTreeHelper.GetParent(element);
                if (element == null) return null;
                if (element is T) return element as T;
            }
        }

        /// <summary>
        /// Sets the automatic move cursor to next tree item.
        /// </summary>
        /// <param name="d">The d.</param>
        /// <param name="value">if set to <c>true</c> [value].</param>
        public static void SetAutoMoveCursorToNextTreeItem(DependencyObject d, bool value)
        {
            d.SetValue(AutoMoveCursorToNextTreeItemProperty, value);
        }
        /// <summary>
        /// Gets the automatic move cursor to next tree item.
        /// </summary>
        /// <param name="d">The d.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public static bool GetAutoMoveCursorToNextTreeItem(DependencyObject d)
        {
            return (bool)d.GetValue(AutoMoveCursorToNextTreeItemProperty);
        }

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
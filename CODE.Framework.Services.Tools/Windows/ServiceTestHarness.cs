using System;
using System.Reflection;
using System.ServiceModel;
using System.Windows.Forms;
using CODE.Framework.Services.Client;

namespace CODE.Framework.Services.Tools.Windows
{
    /// <summary>
    /// Service test harness
    /// </summary>
    public partial class ServiceTestHarness : Form
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceTestHarness"/> class.
        /// </summary>
        public ServiceTestHarness()
        {
            InitializeComponent();

            _ui = new ServiceTestHarnessUI();
            elementHost1.Child = _ui;
        }

        private ServiceTestHarnessUI _ui;

        /// <summary>
        /// Shows the service.
        /// </summary>
        /// <param name="serviceInfo">The service info.</param>
        public void ShowService(ServiceInfoWrapper serviceInfo)
        {
            var fieldInfo = typeof(ServiceHost).GetField("serviceType", BindingFlags.Instance | BindingFlags.NonPublic);
            if (fieldInfo != null)
            {
                var serviceType = fieldInfo.GetValue(serviceInfo.Host) as Type;
                if (serviceType == null) return;

                var interfaces = serviceType.GetInterfaces();
                if (interfaces.Length < 1) return;
                var serviceInterface = interfaces[0];

                Text = serviceInterface.Name + " - WCF Service Test Harness";
            }

            var protocol = Protocol.NetTcp;
            switch (serviceInfo.Protocol.ToLower())
            {
                case "basic http":
                case "basichttp":
                    protocol = Protocol.BasicHttp;
                    Text = Text.Replace(" - WCF Service Test Harness", " - Basic HTTP - WCF Service Test Harness");
                    break;
                case "ws http":
                case "wshttp":
                    protocol = Protocol.WsHttp;
                    Text = Text.Replace(" - WCF Service Test Harness", " - WsHTTP - WCF Service Test Harness");
                    break;
                case "in process":
                case "inprocess":
                    protocol = Protocol.InProcess;
                    Text = Text.Replace(" - WCF Service Test Harness", " - In Process - WCF Service Test Harness");
                    break;
                case "rest http (xml)":
                    protocol = Protocol.RestHttpXml;
                    Text = Text.Replace(" - WCF Service Test Harness", " - REST XML - WCF Service Test Harness");
                    break;
                case "rest http (json)":
                    protocol = Protocol.RestHttpJson;
                    Text = Text.Replace(" - WCF Service Test Harness", " - REST JSON - WCF Service Test Harness");
                    break;
                case "net.tcp":
                case "nettcp":
                    Text = Text.Replace(" - WCF Service Test Harness", " - TCP/IP - WCF Service Test Harness");
                    break;
            }

            var size = MessageSize.Medium;
            switch (serviceInfo.MessageSize.ToLower())
            {
                case "normal":
                    size = MessageSize.Normal;
                    break;
                case "large":
                    size = MessageSize.Large;
                    break;
            }

            _ui.ShowService(serviceInfo.Host, serviceInfo.Port, string.Empty, protocol, size, new Uri(serviceInfo.Url));
        }
    }
}

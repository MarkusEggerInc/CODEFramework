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
            var serviceType = fieldInfo.GetValue(serviceInfo.Host) as Type;
            if (serviceType == null) return;

            var interfaces = serviceType.GetInterfaces();
            if (interfaces.Length < 1) return;
            var serviceInterface = interfaces[0];

            Text = serviceInterface.Name + " - WCF Service Test Harness";

            var protocol = Protocol.NetTcp;
            switch (serviceInfo.Protocol.ToLower())
            {
                case "basic http":
                case "basichttp":
                    protocol = Protocol.BasicHttp;
                    break;
                case "ws http":
                case "wshttp":
                    protocol = Protocol.WsHttp;
                    break;
                case "in process":
                case "inprocess":
                    protocol = Protocol.InProcess;
                    break;
                case "rest http (xml)":
                    protocol = Protocol.RestHttpXml;
                    break;
                case "rest http (json)":
                    protocol = Protocol.RestHttpJson;
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

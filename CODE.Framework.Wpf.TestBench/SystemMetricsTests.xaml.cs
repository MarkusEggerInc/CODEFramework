using System.Windows;
using System.Windows.Controls;
using CODE.Framework.Wpf.Utilities;

namespace CODE.Framework.Wpf.TestBench
{
    /// <summary>
    /// Interaction logic for SystemMetricsTests.xaml
    /// </summary>
    public partial class SystemMetricsTests : Window
    {
        public SystemMetricsTests()
        {
            InitializeComponent();

            AddItem("Boot mode: " + SystemMetricsHelper.BootMode);
            AddItem("Number of display monitors: " + SystemMetricsHelper.NumberOfDisplayMonitors);
            AddItem("Number of mouse buttons: " + SystemMetricsHelper.NumberOfMouseButtons);
            AddItem("Convertible is in slate mode: " + SystemMetricsHelper.ConvertibleIsInSlateMode);
        }

        private void AddItem(string text)
        {
            metrics.Items.Add(new ListBoxItem {Content = text});
        }
    }
}

using System.Globalization;
using System.Text;
using System.Windows;
using CODE.Framework.Core.Configuration;

namespace CODE.Framework.Wpf.TestBench
{
    /// <summary>
    /// Interaction logic for SettingsTest.xaml
    /// </summary>
    public partial class SettingsTest : Window
    {
        public SettingsTest()
        {
            InitializeComponent();

            RefreshAllSettings(null, null);
            RefreshAllKeys(null, null);
        }

        private void RefreshAllSettings(object sender, RoutedEventArgs e)
        {
            var sb = new StringBuilder();
            sb.AppendLine("All settings per source:");
            foreach (var source in ConfigurationSettings.Sources.Values)
            {
                sb.AppendLine();
                sb.AppendLine(source.FriendlyName);
                foreach (var setting in source.Settings.GetAllKeys())
                    sb.AppendLine("    " + setting + ":  " + source.Settings[setting]);
            }
            allSettings.Text = sb.ToString();
        }

        private void SetServerName(object sender, RoutedEventArgs e)
        {
            ConfigurationSettings.Settings["database:Server"] = System.Environment.TickCount.ToString(CultureInfo.InvariantCulture);
            RefreshAllSettings(null, null);
        }

        private void RefreshAllKeys(object sender, RoutedEventArgs e)
        {
            var keys = ConfigurationSettings.GetAllKeys();
            var sb = new StringBuilder();
            sb.AppendLine("All keys across sources:\r\n");
            foreach (var key in keys)
                sb.AppendLine(key);
            allKeys.Text = sb.ToString();
        }

        private void SetTempKey(object sender, RoutedEventArgs e)
        {
            ConfigurationSettings.Settings["Temp"] = System.Environment.TickCount.ToString(CultureInfo.InvariantCulture);
            RefreshAllSettings(null, null);
            RefreshAllKeys(null, null);
        }

        private void GetSingleSetting(object sender, RoutedEventArgs e)
        {
            var result = ConfigurationSettings.Settings["DataServices"];
            MessageBox.Show(result);
        }
    }
}

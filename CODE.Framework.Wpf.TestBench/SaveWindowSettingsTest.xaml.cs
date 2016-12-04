using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using CODE.Framework.Core.Utilities;

namespace CODE.Framework.Wpf.TestBench
{
    /// <summary>
    /// Interaction logic for SaveWindowSettingsTest.xaml
    /// </summary>
    public partial class SaveWindowSettingsTest : Window
    {
        public SaveWindowSettingsTest()
        {
            InitializeComponent();

            Closing += (s, e) =>
            {
                var mruGuid = Guid.NewGuid();
                var data = new Dictionary<string, string> {{"User", "Test User"}, {"SomeOtherKey", Guid.NewGuid().ToString()}};
                SettingsManager.SaveMostRecentlyUsed("WindowUse", mruGuid.ToString(), "Window Usage: Timestamp " + DateTime.Now.ToString(CultureInfo.InvariantCulture) + " Id " + mruGuid, scope: SettingScope.Workstation, data: data);
            };

            var recentUses = SettingsManager.LoadMostRecentlyUsed("WindowUse", SettingScope.Workstation);
            mruList.ItemsSource = recentUses;
        }
    }
}

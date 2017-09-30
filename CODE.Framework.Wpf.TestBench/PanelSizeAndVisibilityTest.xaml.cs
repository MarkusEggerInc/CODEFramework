using System.Windows;

namespace CODE.Framework.Wpf.TestBench
{
    /// <summary>
    /// Interaction logic for PanelSizeAndVisibilityTest.xaml
    /// </summary>
    public partial class PanelSizeAndVisibilityTest : Window
    {
        public PanelSizeAndVisibilityTest()
        {
            InitializeComponent();
        }

        private void ToggleVisibility(object sender, RoutedEventArgs e)
        {
            visiblePanel.Visibility = visiblePanel.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        }
    }
}

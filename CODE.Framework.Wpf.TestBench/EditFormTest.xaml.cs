using System.Windows;
using System.Windows.Input;
using CODE.Framework.Wpf.Layout;

namespace CODE.Framework.Wpf.TestBench
{
    /// <summary>
    /// Interaction logic for EditFormTest.xaml
    /// </summary>
    public partial class EditFormTest : Window
    {
        public EditFormTest()
        {
            InitializeComponent();
        }

        private void Window_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            edit1.LabelPosition = edit1.LabelPosition == EditFormLabelPositions.Left ? EditFormLabelPositions.Top : EditFormLabelPositions.Left;
        }
    }
}

using System.Windows;
using CODE.Framework.Wpf.Mvvm;

namespace CODE.Framework.Wpf.TestBench
{
    /// <summary>
    /// Interaction logic for FlowDocumentReaderTest.xaml
    /// </summary>
    public partial class FlowDocumentReaderTest : Window
    {
        public FlowDocumentReaderTest()
        {
            InitializeComponent();

            DataContext = new FlowDocReaderModel();
        }
    }

    public class FlowDocReaderModel : ViewModel
    {
        public FlowDocReaderModel()
        {
            TestAction = new ViewAction(execute: (a, o) => MessageBox.Show("Now!"));
        }

        public IViewAction TestAction { get; set; }
    }
}

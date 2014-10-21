using System.Windows;

namespace CODE.Framework.Wpf.TestBench
{
    /// <summary>
    /// Interaction logic for FlowFormTest.xaml
    /// </summary>
    public partial class FlowFormTest : Window
    {
        public FlowFormTest()
        {
            InitializeComponent();
            DataContext = new TestContext();
        }
    }

    public class TestContext
    {
        public string Name { get; set; }
    }
}

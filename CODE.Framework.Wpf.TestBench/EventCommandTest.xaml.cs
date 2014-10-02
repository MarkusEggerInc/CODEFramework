using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using CODE.Framework.Wpf.Mvvm;

namespace CODE.Framework.Wpf.TestBench
{
    /// <summary>
    /// Interaction logic for EventCommandTest.xaml
    /// </summary>
    public partial class EventCommandTest : Window
    {
        public EventCommandTest()
        {
            DataContext = new EventCommandTestViewModel();
            InitializeComponent();
            //new Button().Click += new RoutedEventHandler(EventCommandTest_Click);
            //new Button().MouseDoubleClick += new System.Windows.Input.MouseButtonEventHandler(EventCommandTest_MouseDoubleClick);
        }

        //void EventCommandTest_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        //{
        //    throw new System.NotImplementedException();
        //}

        //void EventCommandTest_Click(object sender, RoutedEventArgs e)
        //{
        //    throw new System.NotImplementedException();
        //}
    }

    public class EventCommandTestViewModel
    {
        public EventCommandTestViewModel()
        {
            TestCommand = new ViewAction("Test", execute: (a, o) => MessageBox.Show("Now!"));
            TestCaption = "Hello";
        }
        public IViewAction TestCommand { get; set; }
        public string TestCaption { get; set; }
    }
}

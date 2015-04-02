using System.Windows;
using CODE.Framework.Wpf.Mvvm;

namespace CODE.Framework.Wpf.TestBench
{
    /// <summary>
    /// Interaction logic for MultiPanelTest.xaml
    /// </summary>
    public partial class MultiPanelTest : Window
    {
        public MultiPanelTest()
        {
            InitializeComponent();
            DataContext = new MultiPanelTestViewModel();
        }
    }

    public class MultiPanelTestViewModel : ViewModel
    {
        private Visibility _panel1Visible = Visibility.Visible;

        public MultiPanelTestViewModel()
        {
            CloseAction = new ViewAction(execute: (a, o) => Panel1Visible = Visibility.Collapsed);
        }

        public ViewAction CloseAction { get; set; }

        public Visibility Panel1Visible
        {
            get { return _panel1Visible; }
            set
            {
                _panel1Visible = value;
                NotifyChanged("Panel1Visible");
            }
        }
    }
}

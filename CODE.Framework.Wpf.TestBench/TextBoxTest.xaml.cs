using System.ComponentModel;
using System.Windows;

namespace CODE.Framework.Wpf.TestBench
{
    /// <summary>
    /// Interaction logic for TextBoxTest.xaml
    /// </summary>
    public partial class TextBoxTest : Window
    {
        public TextBoxTest()
        {
            InitializeComponent();

            DataContext = new TextBoxTestModel();
        }
    }

    public class TextBoxTestModel : INotifyPropertyChanged
    {
        private decimal _testValue;

        public TextBoxTestModel()
        {
            TestValue = 19.95m;
        }

        public string TestPhone { get; set; }

        public decimal TestValue
        {
            get { return _testValue; }
            set
            {
                _testValue = value;
                OnPropertyChanged("TestValue");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            var onPropertyChanged = PropertyChanged;
            if (onPropertyChanged != null)
                onPropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

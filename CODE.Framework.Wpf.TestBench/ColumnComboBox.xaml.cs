using System.Windows;

namespace CODE.Framework.Wpf.TestBench
{
    /// <summary>
    /// Interaction logic for ColumnComboBox.xaml
    /// </summary>
    public partial class ColumnComboBox : Window
    {
        public ColumnComboBox()
        {
            InitializeComponent();

            var model = new MyModel(25);
            DataContext = model;
            combo.ItemsSource = model.OtherModels;
        }
    }
}

using System.Collections.Generic;
using System.Windows;

namespace CODE.Framework.Wpf.TestBench
{
    /// <summary>
    /// Interaction logic for MetroListBoxes.xaml
    /// </summary>
    public partial class MetroListBoxes : Window
    {
        public MetroListBoxes()
        {
            InitializeComponent();

            var list = new List<ExampleData>();
            for (int i = 0; i < 50; i++)
                list.Add(new ExampleData {Title = "Item #" + i});

            list1.ItemsSource = list;
            list2.ItemsSource = list;
            list3.ItemsSource = list;
        }
    }

    public class ExampleData
    {
        public string Title { get; set; }
    }
}

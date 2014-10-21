using System.Collections.Generic;
using System.Windows;

namespace CODE.Framework.Wpf.TestBench
{
    /// <summary>
    /// Interaction logic for ListBoxGridReadOnly.xaml
    /// </summary>
    public partial class ListBoxGridReadOnly : Window
    {
        public ListBoxGridReadOnly()
        {
            InitializeComponent();

            var list = new List<Customer>();
            for (var counter = 0; counter < 100; counter++)
                list.Add(new Customer {Company = "EPS/CODE #" + counter, Name = "Markus"});
            uiList.ItemsSource = list;
        }
    }

    public class Customer
    {
        public string Name { get; set; }
        public string Company { get; set; }
    }
}

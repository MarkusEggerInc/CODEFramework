using System;
using System.Windows;
using CODE.Framework.Wpf.Mvvm;
using System.Windows.Controls;
using CODE.Framework.Wpf.Controls;

namespace CODE.Framework.Wpf.TestBench
{
    /// <summary>
    /// Interaction logic for TreeViewTest.xaml
    /// </summary>
    public partial class TreeViewTest : Window
    {
        public TreeViewTest()
        {
            InitializeComponent();
            DataContext = new TreeTestViewModel();
        }

        private void ShowSelection(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Selected node: " + tree.SelectedNode);
        }

        private void SelectLastItem(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as TreeTestViewModel;
            vm.SelectedTreeItem = tree.Items[tree.Items.Count - 1];
        }
    }

    public class TreeTestViewModel : ViewModel
    {
        private object _selectedTreeItem;

        public object SelectedTreeItem
        {
            get { return _selectedTreeItem; }
            set
            {
                _selectedTreeItem = value;
                NotifyChanged("SelectedTreeItem");
                Console.WriteLine("New node selected: " + value);
            }
        }
    }
}

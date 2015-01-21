using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using CODE.Framework.Wpf.Mvvm;

namespace CODE.Framework.Wpf.TestBench
{
    /// <summary>
    /// Interaction logic for SyncCollectionTest.xaml
    /// </summary>
    public partial class SyncCollectionTest : Window
    {
        private readonly SyncTestViewModel _vm = new SyncTestViewModel();
        public SyncCollectionTest()
        {
            InitializeComponent();
            DataContext = _vm;
        }

        private void AddItem(object sender, RoutedEventArgs e)
        {
            _vm.Customers.Add(new SyncCustomerViewModel());
        }

        private void RemoveItem(object sender, RoutedEventArgs e)
        {
            if (_vm.Customers.Count > 0)
                _vm.Customers.RemoveAt(_vm.Customers.Count - 1);
        }

        private void ResetCollection(object sender, RoutedEventArgs e)
        {
            _vm.Customers.Clear();
        }

        private void Sync1(object sender, RoutedEventArgs e)
        {
            _vm.Customers.Sync(_vm.Target1);
        }

        private void Unsync1(object sender, RoutedEventArgs e)
        {
            _vm.Customers.RemoveSync(_vm.Target1);
        }

        private void Sync2(object sender, RoutedEventArgs e)
        {
            _vm.Customers.Sync(_vm.Target2);
        }

        private void Unsync2(object sender, RoutedEventArgs e)
        {
            _vm.Customers.RemoveSync(_vm.Target2);
        }
    }

    public class SyncTestViewModel
    {
        public SyncTestViewModel()
        {
            Customers = new ObservableCollection<SyncCustomerViewModel>();
            Target1 = new ObservableCollection<SyncCustomerViewModel>();
            Target2 = new ObservableCollection<SyncCustomerViewModel>();
        }

        public ObservableCollection<SyncCustomerViewModel> Customers { get; set; }
        public ObservableCollection<SyncCustomerViewModel> Target1 { get; set; }
        public ObservableCollection<SyncCustomerViewModel> Target2 { get; set; }
    }

    public class SyncCustomerViewModel
    {
        public SyncCustomerViewModel()
        {
            Display = System.Environment.TickCount.ToString(CultureInfo.InvariantCulture);
        }
        public string Display { get; set; }
    }
}

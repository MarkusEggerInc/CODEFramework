using System.Windows;
using CODE.Framework.Wpf.Mvvm;

namespace CODE.Framework.Wpf.TestBench
{
    /// <summary>
    /// Interaction logic for CanExecuteChangedTest.xaml
    /// </summary>
    public partial class CanExecuteChangedTest : Window
    {
        public CanExecuteChangedTest()
        {
            InitializeComponent();
            //DataContext = new CanExecuteChangedViewModel();
        }
    }

    //public class CanExecuteChangedViewModel : ViewModel
    //{
    //    public CanExecuteChangedViewModel()
    //    {
    //        Search = new ViewAction("Search",
    //                                execute: (a, o) => MessageBox.Show("Searching for " + SearchText),
    //                                canExecute: (a, o) => !string.IsNullOrEmpty(SearchText));
    //    }

    //    public ViewAction Search { get; set; }

    //    private string _searchText;
    //    public string SearchText
    //    {
    //        get { return _searchText; }
    //        set
    //        {
    //            _searchText = value;
    //            NotifyChanged("SearchText");
    //            Search.InvalidateCanExecute();
    //        }
    //    }
    //}
}

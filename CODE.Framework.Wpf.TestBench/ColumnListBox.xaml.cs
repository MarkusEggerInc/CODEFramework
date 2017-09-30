using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows;
using CODE.Framework.Core.Utilities;
using CODE.Framework.Wpf.Controls;
using CODE.Framework.Wpf.Mvvm;

namespace CODE.Framework.Wpf.TestBench
{
    /// <summary>
    /// Interaction logic for ColumnListBox.xaml
    /// </summary>
    public partial class ColumnListBox : Window
    {
        public ColumnListBox()
        {
            InitializeComponent();

            var model = new MyModel();
            DataContext = model;
            list.ItemsSource = model.OtherModels;

            Closing += (s, e) =>
            {
                SettingsManager.SaveSettings(ListEx.GetColumns(list));
            };
        }

        private void ToggleColumnC2(object sender, RoutedEventArgs e)
        {
            c2.Visibility = c2.Visibility != Visibility.Visible ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ToggleHeaders(object sender, RoutedEventArgs e)
        {
            columns.ShowHeaders = !columns.ShowHeaders;
        }

        private void ToggleFooters(object sender, RoutedEventArgs e)
        {
            columns.ShowFooters = !columns.ShowFooters;
        }
    }

    public class MyModel : ViewModel
    {
        public MyModel(int count = 5)
        {
            HeaderClick = new ViewAction("Test", execute: (a, o) =>
            {
                var paras = o as HeaderClickCommandParameters;
                if (paras == null) return;
                MessageBox.Show(paras.Column.Header.ToString());
            });

            OtherModels = new ObservableCollection<ExtendedStandardViewModel>();
            for (var x = 0; x < count; x++)
                OtherModels.Add(new ExtendedStandardViewModel
                {
                    Text1 = "Item #" + (x + 1) + " a - wwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwww",
                    Text2 = "Item #" + (x + 1) + " b",
                    Text3 = "Item #" + (x + 1) + " c",
                    Text4 = "Item #" + (x + 1) + " d",
                    Text5 = "Item #" + (x + 1) + " e",
                    Text6 = "List Item #" + (x + 1),
                    IsChecked = x%2 == 0,
                    IsExpanded = x%2 == 1
                });
        }

        public ObservableCollection<ExtendedStandardViewModel> OtherModels { get; set; }

        private ViewAction _headerClick;

        public ViewAction HeaderClick
        {
            get { return _headerClick; }
            set
            {
                _headerClick = value;
                NotifyChanged("HeaderClick");
            }
        }
    }

    public class ExtendedStandardViewModel : StandardViewModel
    {
        private bool _isExpanded;

        public ExtendedStandardViewModel()
        {
            TextList = new ObservableCollection<string>();
            for (var counter = 0; counter < 10; counter++)
                TextList.Add("List Item #" + (counter + 1));
        }

        public ObservableCollection<string> TextList { get; set; }

        public bool IsExpanded
        {
            get { return _isExpanded; }
            set
            {
                _isExpanded = value;
                NotifyChanged("IsExpanded");
            }
        }
    }
}
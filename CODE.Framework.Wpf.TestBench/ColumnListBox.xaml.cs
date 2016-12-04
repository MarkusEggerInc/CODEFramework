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
    }

    public class MyModel : ViewModel
    {
        public MyModel()
        {
            HeaderClick = new ViewAction("Test", execute: (a, o) =>
                {
                    var paras = o as HeaderClickCommandParameters;
                    if (paras == null) return;
                    MessageBox.Show(paras.Column.Header.ToString());
                });

            OtherModels = new ObservableCollection<StandardViewModel>();
            for (var x = 0; x < 5; x++)
                OtherModels.Add(new StandardViewModel
                    {
                        Text1 = "Item #" + x + " a",
                        Text2 = "Item #" + x + " b",
                        Text3 = "Item #" + x + " c",
                        Text4 = "Item #" + x + " d",
                        Text5 = "Item #" + x + " e"
                    });
        }

        public ObservableCollection<StandardViewModel> OtherModels { get; set; }

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
}
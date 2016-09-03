using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;

namespace CODE.Framework.Wpf.TestBench
{
    /// <summary>
    /// Interaction logic for MetroControls.xaml
    /// </summary>
    public partial class MetroControls : Window
    {
        public MetroControls()
        {
            Background = Brushes.Black;
            Resources.MergedDictionaries.Clear();
            Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/CODE.Framework.Wpf.Theme.Battleship;component/ThemeRoot.xaml", UriKind.Absolute) });

            InitializeComponent();

            combo1.DataContext = new ComboBoxModel();
        }
    }

    public class ComboBoxModel
    {
        public ComboBoxModel()
        {
            Items = new ObservableCollection<ComboItems>
            {
                new ComboItems {DisplayText = "Item 1", Value = Guid.NewGuid()},
                new ComboItems {DisplayText = "Item 2", Value = Guid.NewGuid()},
                new ComboItems {DisplayText = "Item 3", Value = Guid.NewGuid()},
                new ComboItems {DisplayText = "Item 4", Value = Guid.NewGuid()},
                new ComboItems {DisplayText = "Item 5", Value = Guid.NewGuid()}
            };
        }

        public object SelectedItem { get; set; }
        public ObservableCollection<ComboItems> Items { get; set; }
    }

    public class ComboItems
    {
        public Guid Value { get; set; }
        public string DisplayText { get; set; }
    }
}

using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CODE.Framework.Wpf.Mvvm;

namespace CODE.Framework.Wpf.TestBench
{
    /// <summary>
    /// Interaction logic for RibbonTest.xaml
    /// </summary>
    public partial class RibbonTest : Window
    {
        public RibbonTest()
        {
            Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/CODE.Framework.Wpf.Theme.Workplace;component/Workplace-Icon-Standard.xaml", UriKind.Relative) });
            InitializeComponent();
            ribbon.Model = new RibbonData();
        }
    }

    public class RibbonData : IHaveActions
    {
        public RibbonData()
        {
            Actions = new ViewActionsCollection
            {
                new ViewAction("Save", groupTitle: "File") {Significance = ViewActionSignificance.AboveNormal, Visibility = Visibility.Collapsed, BrushResourceKey = "CODE.Framework-Icon-Save"},
                new ViewAction("Change Save Visibility", groupTitle: "File", execute: (a, o) => Actions["Save"].Visibility = Actions["Save"].Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible) {Significance = ViewActionSignificance.AboveNormal, BrushResourceKey = "CODE.Framework-Icon-Settings"},
                new ViewAction("The quick brown fox jumps over the lazy dog", groupTitle: "File", canExecute: (o, e) => false) {Significance = ViewActionSignificance.AboveNormal, BrushResourceKey = "CODE.Framework-Icon-Shop"},
                
                new ViewAction("Enable/Disable", category: "Page 2", execute: (s, e) => Actions["Two B"].InvalidateCanExecute()) {Significance = ViewActionSignificance.AboveNormal, BrushResourceKey = "CODE.Framework-Icon-Read"},
                new ViewAction("Two B", category: "Page 2", canExecute: (a, e) =>
                {
                    if (a.Caption == "Two B")
                    {
                        a.Caption = "Two BX";
                        return false;
                    }
                    a.Caption = "Two B";
                    return true;
                }) {Significance = ViewActionSignificance.AboveNormal, BrushResourceKey = "CODE.Framework-Icon-Remote"},
                new ViewAction("Three B", category: "Page 2") {Significance = ViewActionSignificance.AboveNormal, BrushResourceKey = "CODE.Framework-Icon-RotateCamera"},

                new ViewAction("Example Button", category: "Custom Views") {ActionView = new Button{Content = "Test", Margin = new Thickness(5)}},
                new ViewAction("Font Controls", execute: (a, o) => { /* Launch an action UI */ }, category: "Custom Views", beginGroup: true, groupTitle: "Format") {ActionView = new FontControls()},
                new ViewAction("Example List", category: "Custom Views", beginGroup: true, groupTitle: "List") {ActionView = new ExampleListInRibbon(), ActionViewModel = new RibbonListViewModel()}
            };
        }

        public ViewActionsCollection Actions { get; private set; }
        public event NotifyCollectionChangedEventHandler ActionsChanged;
    }

    public class RibbonListViewModel
    {
        public RibbonListViewModel()
        {
            Items = new ObservableCollection<RibbonListItem>();
            for (var counter = 0; counter < 100; counter++)
                Items.Add(new RibbonListItem {Text = "Item #" + counter, Icon = Application.Current.FindResource("CODE.Framework-Icon-Video") as Brush});
        }
        public ObservableCollection<RibbonListItem> Items { get; set; }
    }

    public class RibbonListItem
    {
        public string Text { get; set; }
        public Brush Icon { get; set; }
    }
}
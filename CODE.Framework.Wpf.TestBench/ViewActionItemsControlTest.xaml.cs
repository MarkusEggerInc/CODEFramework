using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CODE.Framework.Wpf.Mvvm;

namespace CODE.Framework.Wpf.TestBench
{
    /// <summary>
    /// Interaction logic for ViewActionItemsControlTest.xaml
    /// </summary>
    public partial class ViewActionItemsControlTest : Window
    {
        public ViewActionItemsControlTest()
        {
            InitializeComponent();
            DataContext = new ViewActionItemsControlTestViewModel();
        }
    }

    public class ViewActionItemsControlTestViewModel : ViewModel
    {
        public ViewActionItemsControlTestViewModel()
        {
            Actions.Add(new ViewAction("Action #1", brushResourceKey: "CODE.Framework-Icon-Save", logoBrushResourceKey: "CODE.Framework-Icon-Save", category: "File", execute: (a,o) => MessageBox.Show("Now!")));
            Actions.Add(new ViewAction("Action #2", brushResourceKey: "CODE.Framework-Icon-Save", logoBrushResourceKey: "CODE.Framework-Icon-Save", category: "File", execute: (a, o) => MessageBox.Show("Now!")));
            Actions.Add(new ViewAction("Action #5", brushResourceKey: "CODE.Framework-Icon-Save", logoBrushResourceKey: "CODE.Framework-Icon-Save", category: "File", execute: (a, o) => MessageBox.Show("Now!")) { Significance = ViewActionSignificance.BelowNormal });
            Actions.Add(new ViewAction("Action #5", brushResourceKey: "CODE.Framework-Icon-Save", logoBrushResourceKey: "CODE.Framework-Icon-Save", category: "File", execute: (a, o) => MessageBox.Show("Now!")) { Significance = ViewActionSignificance.BelowNormal });
            Actions.Add(new ViewAction("Action #5", brushResourceKey: "CODE.Framework-Icon-Save", logoBrushResourceKey: "CODE.Framework-Icon-Save", category: "File", execute: (a, o) => MessageBox.Show("Now!")) { Significance = ViewActionSignificance.BelowNormal });
            Actions.Add(new ViewAction("Action #3", brushResourceKey: "CODE.Framework-Icon-Save", logoBrushResourceKey: "CODE.Framework-Icon-Save", category: "Test", execute: (a, o) => MessageBox.Show("Now!")) { Significance = ViewActionSignificance.AboveNormal });
            Actions.Add(new ViewAction("Action #4", brushResourceKey: "CODE.Framework-Icon-Save", logoBrushResourceKey: "CODE.Framework-Icon-Save", category: "Test", execute: (a, o) => MessageBox.Show("Now!")) { Significance = ViewActionSignificance.Highest });
            Actions.Add(new ViewAction("Action #4", brushResourceKey: "CODE.Framework-Icon-Save", logoBrushResourceKey: "CODE.Framework-Icon-Save", category: "Test", execute: (a, o) => MessageBox.Show("Now!")) { Significance = ViewActionSignificance.Highest, ActionView = new Button{Content = "Hello World!", Background=Brushes.Red} });
        }
    }
}

using System.Collections.Generic;
using System.Windows;
using CODE.Framework.Wpf.Mvvm;

namespace CODE.Framework.Wpf.TestBench
{
    /// <summary>
    /// Interaction logic for ViewActionMenuTest.xaml
    /// </summary>
    public partial class ViewActionMenuTest : Window
    {
        public ViewActionMenuTest()
        {
            InitializeComponent();

            var model = new ViewActionMenuTestViewModel();
            menu.Model = model;
            ribbon.Model = model;
            button.Actions = model.Actions;
        }
    }

    public class ViewActionMenuTestViewModel : ViewModel
    {
        public ViewActionMenuTestViewModel()
        {
            Actions.Add(new ViewAction("Open", category: "File", execute: (a, o) => MessageBox.Show("Open!")));
            Actions.Add(new ViewAction("File 1", execute: (a, o) => MessageBox.Show("File 1!"))
            {
                Categories = new List<ViewActionCategory>
                {
                    new ViewActionCategory("File"),
                    new ViewActionCategory("Recent Files"),
                    new ViewActionCategory("Very Recent Files")
                }
            });
        }
    }
}

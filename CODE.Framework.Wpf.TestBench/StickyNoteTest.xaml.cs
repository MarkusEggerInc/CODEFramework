using System.Collections.Generic;
using System.Windows;
using CODE.Framework.Wpf.Mvvm;

namespace CODE.Framework.Wpf.TestBench
{
    /// <summary>
    /// Interaction logic for StickyNoteTest.xaml
    /// </summary>
    public partial class StickyNoteTest : Window
    {
        public StickyNoteTest()
        {
            InitializeComponent();

            var listX = new List<StandardViewModel>();
            for (var counter = 0; counter < 100; counter++)
                listX.Add(new StandardViewModel {Text1 = "Test #" + counter, Text2 = "Some long text that is really really long #" + counter});
            list.ItemsSource = listX;
        }
    }
}

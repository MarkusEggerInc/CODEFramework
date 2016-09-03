using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace CODE.Framework.Wpf.TestBench
{
    /// <summary>
    /// Interaction logic for BiStackTest2.xaml
    /// </summary>
    public partial class BiStackTest2 : Window
    {
        public BiStackTest2()
        {
            InitializeComponent();
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            list.Items.Add(new Button {Content = "Another Button"});
        }
    }
}

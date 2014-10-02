using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using CODE.Framework.Wpf.Layout;
using CODE.Framework.Wpf.Mvvm;
using CODE.Framework.Wpf.Controls;

namespace CODE.Framework.Wpf.TestBench
{
    /// <summary>
    /// Interaction logic for ListDependencyProperties.xaml
    /// </summary>
    public partial class ListDependencyProperties : Window
    {
        public ListDependencyProperties()
        {
            InitializeComponent();

            DataContext = this;

            CopyProps = new ViewAction(execute: (a, o) => Clipboard.SetText(DependencyPropList));

            var layout = new MetroTiles();

            Title = layout.ToString();
            DependencyProps = GetDependencyProperties(layout);
        }

        public ObservableCollection<string> DependencyProps { get; set; }
        public string DependencyPropList { get; set; }
        public ViewAction CopyProps { get; set; }

        public ObservableCollection<string> GetDependencyProperties(Object element)
        {
            DependencyPropList = string.Empty;
            var attachedProperties = new ObservableCollection<string>();
            var desiredAttributes = new Attribute[] { new PropertyFilterAttribute(PropertyFilterOptions.SetValues | PropertyFilterOptions.UnsetValues | PropertyFilterOptions.Valid) };
            var props = TypeDescriptor.GetProperties(element, desiredAttributes).Sort(new [] { "Name" });
            foreach (PropertyDescriptor pd in props)
            {
                var dpd = DependencyPropertyDescriptor.FromProperty(pd);
                if (dpd != null && dpd.DependencyProperty.OwnerType.FullName.StartsWith("CODE.Framework", StringComparison.InvariantCultureIgnoreCase))
                {
                    var propName = dpd.DependencyProperty.Name + (dpd.IsAttached ? "*" : string.Empty);
                    attachedProperties.Add(propName);
                    DependencyPropList += propName + ", ";
                }
            }
            if (DependencyPropList.EndsWith(", ")) DependencyPropList = DependencyPropList.Remove(DependencyPropList.Length - 2);
            return attachedProperties;
        }
    }
}

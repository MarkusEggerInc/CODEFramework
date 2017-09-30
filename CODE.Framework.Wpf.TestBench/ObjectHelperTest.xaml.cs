using System;
using System.Collections.Generic;
using System.Windows;
using CODE.Framework.Core.Utilities;

namespace CODE.Framework.Wpf.TestBench
{
    /// <summary>
    /// Interaction logic for ObjectHelper.xaml
    /// </summary>
    public partial class ObjectHelperTest : Window
    {
        public ObjectHelperTest()
        {
            InitializeComponent();
        }

        private void GetSimpleValue(object sender, RoutedEventArgs e)
        {
            var invoice = new Invoice();

            var firstName = invoice.GetPropertyValue<string>("FirstName");
            Console.WriteLine(firstName);
            var addressLine1 = invoice.GetPropertyValue<string>("Address.Line1");
            Console.WriteLine(addressLine1);
            var description2 = invoice.GetPropertyValue<string>("LineItems[1].Description");
            Console.WriteLine(description2);

            invoice.SetPropertyValue("FirstName", "John");
            invoice.SetPropertyValue("Address.Line1", "Cypresswood Dr.");
            invoice.SetPropertyValue("LineItems[1].Description", "Test description");

            firstName = invoice.GetPropertyValue<string>("FirstName");
            Console.WriteLine(firstName);
            addressLine1 = invoice.GetPropertyValue<string>("Address.Line1");
            Console.WriteLine(addressLine1);
            description2 = invoice.GetPropertyValue<string>("LineItems[1].Description");
            Console.WriteLine(description2);
        }

        private void GetJson(object sender, RoutedEventArgs e)
        {
            var invoice = new Invoice();
            invoice.FirstName = "©®™";
            var json = JsonHelper.SerializeToRestJson(invoice, true);
            MessageBox.Show(json);

            var invoice2 = JsonHelper.DeserializeFromRestJson<Invoice>(json);
        }

        private void GetDateJson(object sender, RoutedEventArgs e)
        {
            var json = JsonHelper.SerializeToRestJson(DateTimeOffset.Now);
            MessageBox.Show(json);
        }
    }

    public class Invoice
    {
        public Invoice()
        {
            FirstName = "Markus";
            Address = new Address();
            LineItems = new List<LineItem>
            {
                new LineItem {Description = "Item #1"}, 
                new LineItem {Description = "Item #2"}, 
                new LineItem {Description = "Item #3"}
            };
        }

        public string FirstName { get; set; }
        public Address Address { get; set; }
        public List<LineItem> LineItems { get; set; }
    }

    public class Address
    {
        public Address()
        {
            Line1 = "Address Line 1";
        }

        public string Line1 { get; set; }
    }

    public class LineItem
    {
        public string Description { get; set; }
    }
}
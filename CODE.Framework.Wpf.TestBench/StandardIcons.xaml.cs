using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace CODE.Framework.Wpf.TestBench
{
    /// <summary>
    /// Interaction logic for StandardIcons.xaml
    /// </summary>
    public partial class StandardIcons : Window
    {
        public StandardIcons()
        {
            InitializeComponent();

            var list = new List<IconWrapper>();

            var keys = new List<string>();
            foreach (var key in Resources.MergedDictionaries[0].Keys)
                keys.Add(key.ToString());

            foreach (string key in keys.OrderBy(k => k))
                if (Resources.MergedDictionaries[0][key] is Brush)
                    list.Add(new IconWrapper {Name = key, Dictionary = Resources.MergedDictionaries[0]});


            icons.ItemsSource = list;

            //CopyIconListHtmlToClipboard(list);
        }

        private void CopyIconListHtmlToClipboard(List<IconWrapper> icons)
        {
            var sb = new StringBuilder();

            sb.AppendLine("<table style=\"border-width: 0px;\" border=\"0\" cellpadding=\"0\"><thead><tr align=\"center\" valign=\"middle\">");
            sb.AppendLine("<th>Resource Name</th><th>Default Theme</th><th>Metro Theme</th></tr></thead><tbody>");

            foreach (var icon in icons)
            {
                sb.AppendLine("<tr>");
                sb.AppendLine("<td align=\"left\" valign=\"middle\">" + icon.Name + "</td>");
                sb.AppendLine("<td align=\"center\" valign=\"middle\"> </td><td align=\"center\" valign=\"middle\"> </td>");
                sb.AppendLine("</tr>");
            }

            sb.AppendLine("</tbody></table>");

            Clipboard.SetText(sb.ToString());
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var icon = (sender as Button).DataContext as IconWrapper;

            var grid = new Grid {Height = 36, Width = 36, Background = Brushes.White};
            var visual = new Rectangle {Fill = icon.Icon};
            grid.Children.Add(visual);
            var renderBitmap = new RenderTargetBitmap(36, 36, 96d, 96d, PixelFormats.Pbgra32);

            var size = new Size(36, 36);
            grid.Measure(size);
            grid.Arrange(new Rect(size));

            // Update the layout for the surface. This should flush out any layout queues that hold a reference.

            renderBitmap.Render(grid);

            Clipboard.SetImage(renderBitmap);
        }
    }

    public class IconWrapper
    {
        public ResourceDictionary Dictionary { get; set; }

        public string Name { get; set; }

        public Brush Icon
        {
            get { return (Brush)Dictionary[Name]; }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Rectangle = System.Windows.Shapes.Rectangle;
using Size = System.Windows.Size;

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
                if (key.ToString().StartsWith("CODE.Framework-Icon"))
                    keys.Add(key.ToString());

            foreach (string key in keys.OrderBy(k => k))
                if (Resources.MergedDictionaries[0][key] is Brush)
                    list.Add(new IconWrapper { Name = key, Dictionary = Resources.MergedDictionaries[0] });
            Title = "Standard Icons - Count: " + list.Count;

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

            var grid = new Grid { Height = 36, Width = 36, Background = Brushes.White };
            var visual = new Rectangle { Fill = icon.Icon };
            grid.Children.Add(visual);
            var renderBitmap = new RenderTargetBitmap(36, 36, 96d, 96d, PixelFormats.Pbgra32);

            var size = new Size(36, 36);
            grid.Measure(size);
            grid.Arrange(new Rect(size));

            // Update the layout for the surface. This should flush out any layout queues that hold a reference.

            renderBitmap.Render(grid);

            Clipboard.SetImage(renderBitmap);
        }

        private void ExportToImage(object sender, RoutedEventArgs e)
        {
            var width = 32;
            var height = 32;
            var keys = (from string key in Resources.MergedDictionaries[0].Keys where key.StartsWith("CODE.Framework-Icon") orderby key select key).ToList();
            var stack = new StackPanel { Width = width, Height = height * keys.Count, Background = Brushes.Transparent };
            foreach (string key in keys.OrderBy(k => k))
                if (Resources.MergedDictionaries[0][key] is Brush)
                {
                    stack.Children.Add(new Rectangle { Fill = Resources.MergedDictionaries[0][key] as Brush, Height = height, Width = width });
                    Console.WriteLine(key);
                }
            var wnd = new Window { Width = 100 };
            var grd = new Grid();
            grd.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            grd.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            wnd.Content = grd;
            var btn = new Button { Content = "Save as File", Width = 100, Margin = new Thickness(5) };
            btn.Click += (s, e2) =>
            {
                var dlg = new SaveFileDialog { FileName = "Icons.png", InitialDirectory = @"c:\", Filter = "PNG Files|*.png|All Files|*.*", RestoreDirectory = true, AddExtension = true, DefaultExt = "png" };
                if (dlg.ShowDialog() == true)
                {
                    var renderBitmap = new RenderTargetBitmap(width, height * keys.Count, 96, 96, PixelFormats.Pbgra32);
                    renderBitmap.Render(stack);
                    var pngEncoder = new PngBitmapEncoder();
                    pngEncoder.Frames.Add(BitmapFrame.Create(renderBitmap));
                    using (var fileStream = File.Create(dlg.FileName.ToString(CultureInfo.InvariantCulture)))
                        pngEncoder.Save(fileStream);
                }
            };
            grd.Children.Add(btn);
            var scrl = new ScrollViewer { Content = stack, HorizontalContentAlignment = HorizontalAlignment.Left, HorizontalAlignment = HorizontalAlignment.Left };
            Grid.SetRow(scrl, 1);
            grd.Children.Add(scrl);
            wnd.Show();
        }

        private void CreateHtmlTable(object sender, RoutedEventArgs e)
        {
            var keys = (from string key in Resources.MergedDictionaries[0].Keys where key.StartsWith("CODE.Framework-Icon") orderby key select key).ToList();

            var sb = new StringBuilder();

            sb.AppendLine("<table>");
            sb.AppendLine("<thead>");
            sb.AppendLine("<tr>");
            sb.AppendLine("<td>Resource Name</td>");
            sb.AppendLine("<td style=\"text-align:center; width:60px;\">Battleship</td>");
            sb.AppendLine("<td style=\"text-align:center; width:60px;\">Geek</td>");
            sb.AppendLine("<td style=\"text-align:center; width:60px;\">Metro</td>");
            sb.AppendLine("<td style=\"text-align:center; width:60px;\">Newsroom</td>");
            sb.AppendLine("<td style=\"text-align:center; width:60px;\">Universe</td>");
            sb.AppendLine("<td style=\"text-align:center; width:60px;\">Vapor</td>");
            sb.AppendLine("<td style=\"text-align:center; width:60px;\">Wildcat</td>");
            sb.AppendLine("<td style=\"text-align:center; width:60px;\">Workplace</td>");
            sb.AppendLine("</tr>");
            sb.AppendLine("</thead>");

            sb.AppendLine("<tbody>");
            var top = 0;
            foreach (var key in keys)
            {
                sb.AppendLine("<tr>");
                sb.AppendLine("<td>" + key + "</td>");
                sb.AppendLine("<td style=\"background: lightgray; text-align:center;\"><img src=\"1x1.gif\" style=\"margin: 8px; height: 32px; width: 32px; background: url('BattleshipIcons.png') 0px " + (top * -1) + "px;\"></td>");
                sb.AppendLine("<td style=\"background: #BCC6D6; text-align:center;\"><img src=\"1x1.gif\" style=\"margin: 8px; height: 32px; width: 32px; background: url('GeekIcons.png') 0px " + (top * -1) + "px;\"></td>");
                sb.AppendLine("<td style=\"background: navy; text-align:center;\"><img src=\"1x1.gif\" style=\"margin: 8px; height: 32px; width: 32px; background: url('MetroIcons.png') 0px " + (top * -1) + "px;\"></td>");
                sb.AppendLine("<td style=\"background: whitesmoke; text-align:center;\"><img src=\"1x1.gif\" style=\"margin: 8px; height: 32px; width: 32px; background: url('NewsroomIcons.png') 0px " + (top * -1) + "px;\"></td>");
                sb.AppendLine("<td style=\"background: darkgreen; text-align:center;\"><img src=\"1x1.gif\" style=\"margin: 8px; height: 32px; width: 32px; background: url('UniverseIcons.png') 0px " + (top * -1) + "px;\"></td>");
                sb.AppendLine("<td style=\"background: black; text-align:center;\"><img src=\"1x1.gif\" style=\"margin: 8px; height: 32px; width: 32px; background: url('VaporIcons.png') 0px " + (top * -1) + "px;\"></td>");
                sb.AppendLine("<td style=\"background: lightblue; text-align:center;\"><img src=\"1x1.gif\" style=\"margin: 8px; height: 32px; width: 32px; background: url('WildcatIcons.png') 0px " + (top * -1) + "px;\"></td>");
                sb.AppendLine("<td style=\"background: whitesmoke; text-align:center;\"><img src=\"1x1.gif\" style=\"margin: 8px; height: 32px; width: 32px; background: url('WorkplaceIcons.png') 0px " + (top * -1) + "px;\"></td>");
                sb.AppendLine("</tr>");
                top += 32;
            }
            sb.AppendLine("</tbody>");

            sb.AppendLine("</table>");

            Clipboard.SetText(sb.ToString());
            MessageBox.Show("HTML has been created and pasted into the clipboard.");
        }

        private void ExportToCss(object sender, RoutedEventArgs e)
        {
            var keys = (from string key in Resources.MergedDictionaries[0].Keys where key.StartsWith("CODE.Framework-Icon") orderby key select key).ToList();

            var sb = new StringBuilder();

            var color = "White";

            var topOffset = 0;

            foreach (var key in keys)
            {
                sb.AppendLine(key.ToLowerInvariant().Replace("code.framework-icon-", ".code-framework-icon-" + color.ToLowerInvariant() + "-") + " { height: 32px; width: 32px; background: url(/Themes/Blade/StandardIcons" + color + ".png) 0px -" + topOffset + "px; }");
                topOffset += 32;
            }

            Clipboard.SetText(sb.ToString());
            MessageBox.Show("CSS has been created and pasted into the clipboard.");
        }

        private void ExportToOffsetJS(object sender, RoutedEventArgs e)
        {
            var keys = (from string key in Resources.MergedDictionaries[0].Keys where key.StartsWith("CODE.Framework-Icon") orderby key select key).ToList();

            var sb = new StringBuilder();

            var topOffset = 0;

            foreach (var key in keys)
            {
                sb.AppendLine("if (brushResourceKey == '" + key + "') return -" + topOffset + ";");
                topOffset += 32;
            }

            Clipboard.SetText(sb.ToString());
            MessageBox.Show("CSS has been created and pasted into the clipboard.");
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

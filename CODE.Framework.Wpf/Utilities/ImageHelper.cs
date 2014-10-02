using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CODE.Framework.Wpf.Utilities
{
    /// <summary>
    /// This class provides various useful utility functions related to images and bitmaps
    /// </summary>
    /// <remarks>
    /// This image helper is specific to WPF image features. For GDI+ image features see the 
    /// ImageHelper class in the CODE.Framework.Core.Utilities namespace
    /// </remarks>
    public static class ImageHelper
    {
        /// <summary>
        /// Converts a GDI+ image/bitmap to a WPF Image Source
        /// </summary>
        /// <param name="bitmap">The bitmap.</param>
        /// <returns>ImageSource.</returns>
        public static ImageSource BitmapToImageSource(Image bitmap)
        {
            using (var memory = new MemoryStream())
            {
                bitmap.Save(memory, ImageFormat.Png);
                memory.Position = 0;
                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                return bitmapImage;
            }
        }
    }
}

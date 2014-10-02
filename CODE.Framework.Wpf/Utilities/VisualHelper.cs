using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CODE.Framework.Wpf.Utilities
{
    /// <summary>
    /// Various helper methods for visual elements
    /// </summary>
    public static class VisualHelper
    {
        /// <summary>Converts a Visual to an ImageSource</summary>
        /// <param name="surface">The surface.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="format">The format.</param>
        /// <returns></returns>
        public static ImageSource ToImageSource(this Visual surface, int width, int height, RenderedImageFormat format)
        {
            if (surface == null) return null;

            var element = surface as FrameworkElement;
            if (element != null)
            {
                element.Height = 96;
                element.Width = 96;
                element.Arrange(new Rect(0d, 0d, 96d, 96d));
            }

            var renderBitmap = new RenderTargetBitmap(width, height, 96d, 96d, PixelFormats.Pbgra32);
            renderBitmap.Render(surface);

            switch (format)
            {
                case RenderedImageFormat.Png:
                    var pngEncoder = new PngBitmapEncoder();
                    pngEncoder.Frames.Add(BitmapFrame.Create(renderBitmap));
                    return pngEncoder.Frames[0];
                case RenderedImageFormat.Jpeg:
                    var jpegEncoder = new JpegBitmapEncoder();
                    jpegEncoder.Frames.Add(BitmapFrame.Create(renderBitmap));
                    return jpegEncoder.Frames[0];
                case RenderedImageFormat.Tiff:
                    var tiffEncoder = new TiffBitmapEncoder();
                    tiffEncoder.Frames.Add(BitmapFrame.Create(renderBitmap));
                    return tiffEncoder.Frames[0];
                case RenderedImageFormat.Bmp:
                    var bmpEncoder = new BmpBitmapEncoder();
                    bmpEncoder.Frames.Add(BitmapFrame.Create(renderBitmap));
                    return bmpEncoder.Frames[0];
                case RenderedImageFormat.Gif:
                    var gifEncoder = new GifBitmapEncoder();
                    gifEncoder.Frames.Add(BitmapFrame.Create(renderBitmap));
                    return gifEncoder.Frames[0];
            }
            return null;
        }

        /// <summary>Converts a Visual to an ImageSource</summary>
        /// <param name="surface">The surface.</param>
        /// <returns></returns>
        public static ImageSource ToIconSource(this Visual surface)
        {
            if (surface == null) return null;

            var element = surface as FrameworkElement;
            if (element != null)
            {
                element.Height = 32;
                element.Width = 32;
                element.Arrange(new Rect(0d, 0d, 96d, 96d));
            }

            var renderBitmap = new RenderTargetBitmap(32, 32, 96d, 96d, PixelFormats.Pbgra32);
            renderBitmap.Render(surface);

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(renderBitmap));
            return encoder.Frames[0];
        }
    }

    /// <summary>
    /// Image formats the utilities can render
    /// </summary>
    public enum RenderedImageFormat
    {
        /// <summary>
        /// PNG
        /// </summary>
        Png,
        /// <summary>
        /// JPEG
        /// </summary>
        Jpeg,
        /// <summary>
        /// TIFF
        /// </summary>
        Tiff,
        /// <summary>
        /// BMP
        /// </summary>
        Bmp,
        /// <summary>
        /// GIF
        /// </summary>
        Gif
    }
}

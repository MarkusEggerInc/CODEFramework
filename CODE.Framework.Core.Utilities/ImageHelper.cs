using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Drawing.Drawing2D;

namespace CODE.Framework.Core.Utilities
{
    /// <summary>
    /// This class contains a number of methods that are useful in imaging scenarios.
    /// </summary>
    public static class ImageHelper
    {
        /// <summary>
        /// Returns a new bitmap that has a 'disabled' look
        /// </summary>
        /// <param name="originalBitmap">Original ('Enabled') Bitmap</param>
        /// <returns>Disabled Bitmap (image)</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("EPS.MilosBusinessObjects", "EPS0022:VariablesShouldHaveMeaningfulNames", Justification = "g is a pseudo-standard for instances of the Graphics object.")]
        public static Image GetDisabledImage(Image originalBitmap)
        {
            var disabledBitmap = new Bitmap(originalBitmap.Width, originalBitmap.Height);
            var g = Graphics.FromImage(disabledBitmap);

            // We create a color matrix to generated the disabled look
            var array = new float[5][];
            array[0] = new float[] { 0.2125f, 0.2125f, 0.2125f, 0, 0 };
            array[1] = new float[] { 0.2577f, 0.2577f, 0.2577f, 0, 0 };
            array[2] = new float[] { 0.0361f, 0.0361f, 0.0361f, 0, 0 };
            array[3] = new float[] { 0, 0, 0, 1, 0 };
            array[4] = new float[] { 0.38f, 0.38f, 0.38f, 0, 1 };
            var grayMatrix = new ColorMatrix(array);
            var disabledImageAttr = new ImageAttributes();
            disabledImageAttr.ClearColorKey();
            disabledImageAttr.SetColorMatrix(grayMatrix);

            // We draw the image using the color matrix
            var rectImage = new Rectangle(0, 0, originalBitmap.Width, originalBitmap.Height);
            g.DrawImage(originalBitmap, rectImage, 0, 0, originalBitmap.Width, originalBitmap.Height, GraphicsUnit.Pixel, disabledImageAttr);
            g.Dispose();

            return disabledBitmap;
        }

        /// <summary>
        /// Converts a byte array containing an image to a bitmap object.
        /// </summary>
        /// <param name="imageIn">The image in.</param>
        /// <returns></returns>
        public static Bitmap ByteArrayToBitmap(byte[] imageIn)
        {
            if (imageIn != null && imageIn.Length == 0)
                return null;

            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(imageIn);
                return new Bitmap(stream);
            }
        }

        /// <summary>
        /// Converts a bitmap object to an array of bytes.
        /// </summary>
        /// <param name="image">The bitmap in.</param>
        /// <returns>Byte array representing the image data</returns>
        public static byte[] BitmapToByteArray(Image image)
        {
            return BitmapToByteArray(image, ImageFormat.Bmp);
        }

        /// <summary>
        /// Converts a bitmap object to an array of bytes.
        /// </summary>
        /// <param name="image">The bitmap in.</param>
        /// <param name="format">Desired storage format</param>
        /// <returns>Byte array representing the image data</returns>
        public static byte[] BitmapToByteArray(Image image, ImageFormat format)
        {
            using (var stream = new MemoryStream())
            {
                image.Save(stream, format);
                var byteArrayOut = stream.ToArray();
                return byteArrayOut;
            }
        }

        /// <summary>
        /// Creates a high quality resized version of the provided image
        /// </summary>
        /// <param name="original">The original image</param>
        /// <param name="newWidth">The width of the new image</param>
        /// <param name="newHeight">The height of the new image</param>
        /// <param name="resizeMode">Resize mode (defines stretching)</param>
        /// <returns>The image in its new size</returns>
        public static Image ResizeImage(Image original, int newWidth, int newHeight, ImageResizeMode resizeMode)
        {
            return ResizeImage(original, newWidth, newHeight, resizeMode, 0, Color.Transparent);
        }
        /// <summary>
        /// Creates a high quality resized version of the provided image
        /// </summary>
        /// <param name="original">The original image</param>
        /// <param name="newWidth">The width of the new image</param>
        /// <param name="newHeight">The height of the new image</param>
        /// <returns>The image in its new size</returns>
        public static Image ResizeImage(Image original, int newWidth, int newHeight)
        {
            return ResizeImage(original, newWidth, newHeight, ImageResizeMode.StretchToFillKeepProportions, 0, Color.Transparent);
        }
        /// <summary>
        /// Creates a high quality resized version of the provided image
        /// </summary>
        /// <param name="original">The original image</param>
        /// <param name="newWidth">The width of the new image</param>
        /// <param name="newHeight">The height of the new image</param>
        /// <param name="padding">Padding around the new image (can be - and often is - negative)</param>
        /// <returns>The image in its new size</returns>
        public static Image ResizeImage(Image original, int newWidth, int newHeight, int padding)
        {
            return ResizeImage(original, newWidth, newHeight, ImageResizeMode.StretchToFillKeepProportions, padding, Color.Transparent);
        }

        /// <summary>
        /// Creates a high quality resized version of the provided image
        /// </summary>
        /// <param name="original">The original image</param>
        /// <param name="newWidth">The width of the new image</param>
        /// <param name="newHeight">The height of the new image</param>
        /// <param name="resizeMode">Resize mode (defines stretching)</param>
        /// <param name="padding">Padding around the new image (can be - and often is - negative)</param>
        /// <param name="backgroundColor">Background color for areas not overlapped by the original image (due to stretch or padding)</param>
        /// <returns>The image in its new size</returns>
        public static Image ResizeImage(Image original, int newWidth, int newHeight, ImageResizeMode resizeMode, int padding, Color backgroundColor)
        {
            var bmp = new Bitmap(newWidth, newHeight);

            var g = Graphics.FromImage(bmp);
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.InterpolationMode = InterpolationMode.High;
            g.SmoothingMode = SmoothingMode.HighQuality;

            g.Clear(backgroundColor);

            // This is the area actually used by the resized image
            int targetWidth = newWidth - (padding * 2);
            int targetHeight = newHeight - (padding * 2);

            var renderRect = new Rectangle(0, 0, newWidth, newHeight);
            switch (resizeMode)
            {
                case ImageResizeMode.Stretch:
                    renderRect = new Rectangle((int)((newWidth - targetWidth) / 2), (int)((newHeight - targetHeight) / 2), targetWidth, targetHeight);
                    break;
                case ImageResizeMode.StretchToFillKeepProportions:
                    decimal verticalFactor = (decimal)targetHeight / (decimal)original.Height;
                    decimal horizontalFactor = (decimal)targetWidth / (decimal)original.Width;
                    var factor = Math.Min(verticalFactor, horizontalFactor);
                    int renderHeight = (int)(original.Height * factor);
                    int renderWidth = (int)(original.Width * factor);
                    renderRect = new Rectangle((int)((newWidth - renderWidth) / 2), (int)((newHeight - renderHeight) / 2), renderWidth, renderHeight);
                    break;
                case ImageResizeMode.StretchToNearestEdge:
                    decimal verticalFactor2 = (decimal)targetHeight / (decimal)original.Height;
                    decimal horizontalFactor2 = (decimal)targetWidth / (decimal)original.Width;
                    var factor2 = Math.Max(verticalFactor2, horizontalFactor2);
                    int renderHeight2 = (int)(original.Height * factor2);
                    int renderWidth2 = (int)(original.Width * factor2);
                    renderRect = new Rectangle((int)((newWidth - renderWidth2) / 2), (int)((newHeight - renderHeight2) / 2), renderWidth2, renderHeight2);
                    break;
            }

            g.DrawImage(original, renderRect);

            return bmp;
        }
    }

    /// <summary>
    /// Defines how the image is to be stretched
    /// </summary>
    public enum ImageResizeMode
    {
        /// <summary>
        /// Stretch to new image size, even if the stretch is disproportionate
        /// </summary>
        Stretch,
        /// <summary>
        /// Stretch to the nearest edge (horizontal or vertical). Proportions are kept,
        /// and white areas may occur on the sides (top.bottom or left/right)
        /// </summary>
        StretchToNearestEdge,
        /// <summary>
        /// Stretches the image proportionally and overlaps the edges of the new image if need
        /// be, so no unused areas remain
        /// </summary>
        StretchToFillKeepProportions
    }
}

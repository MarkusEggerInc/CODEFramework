using System.Drawing;
using System.Drawing.Imaging;

namespace CODE.Framework.Core.Utilities.Extensions
{
    /// <summary>
    /// Extension methods for ImageHelper functionality
    /// </summary>
    public static class ImageExtensions
    {
        /// <summary>
        /// Returns a new bitmap that has a 'disabled' look
        /// </summary>
        /// <param name="originalBitmap">Original ('Enabled') Bitmap</param>
        /// <returns>Disabled Bitmap (image)</returns>
        public static Image GetDisabledImage(this Image originalBitmap)
        {
            return ImageHelper.GetDisabledImage(originalBitmap);
        }

        /// <summary>
        /// Converts a byte array containing an image to a bitmap object.
        /// </summary>
        /// <param name="imageIn">The image in.</param>
        /// <returns></returns>
        public static Bitmap ToBitmap(this byte[] imageIn)
        {
            return ImageHelper.ByteArrayToBitmap(imageIn);
        }

        /// <summary>
        /// Converts a bitmap object to an array of bytes.
        /// </summary>
        /// <param name="image">The bitmap in.</param>
        /// <returns>Byte array representing the image data</returns>
        public static byte[] ToByteArray(this Image image)
        {
            return ImageHelper.BitmapToByteArray(image);
        }

        /// <summary>
        /// Converts a bitmap object to an array of bytes.
        /// </summary>
        /// <param name="image">The bitmap in.</param>
        /// <param name="format">Desired storage format</param>
        /// <returns>Byte array representing the image data</returns>
        public static byte[] ToByteArray(this Image image, ImageFormat format)
        {
            return ImageHelper.BitmapToByteArray(image, format);
        }

        /// <summary>
        /// Creates a high quality resized version of the provided image
        /// </summary>
        /// <param name="original">The original image</param>
        /// <param name="newWidth">The width of the new image</param>
        /// <param name="newHeight">The height of the new image</param>
        /// <param name="resizeMode">Resize mode (defines stretching)</param>
        /// <returns>The image in its new size</returns>
        public static Image Resize(this Image original, int newWidth, int newHeight, ImageResizeMode resizeMode)
        {
            return ImageHelper.ResizeImage(original, newWidth, newHeight, resizeMode);
        }

        /// <summary>
        /// Creates a high quality resized version of the provided image
        /// </summary>
        /// <param name="original">The original image</param>
        /// <param name="newWidth">The width of the new image</param>
        /// <param name="newHeight">The height of the new image</param>
        /// <returns>The image in its new size</returns>
        public static Image Resize(this Image original, int newWidth, int newHeight)
        {
            return ImageHelper.ResizeImage(original, newWidth, newHeight);
        }

        /// <summary>
        /// Creates a high quality resized version of the provided image
        /// </summary>
        /// <param name="original">The original image</param>
        /// <param name="newWidth">The width of the new image</param>
        /// <param name="newHeight">The height of the new image</param>
        /// <param name="padding">Padding around the new image (can be - and often is - negative)</param>
        /// <returns>The image in its new size</returns>
        public static Image Resize(this Image original, int newWidth, int newHeight, int padding)
        {
            return ImageHelper.ResizeImage(original, newWidth, newHeight, padding);
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
        public static Image Resize(this Image original, int newWidth, int newHeight, ImageResizeMode resizeMode, int padding, Color backgroundColor)
        {
            return ImageHelper.ResizeImage(original, newWidth, newHeight, resizeMode, padding, backgroundColor);
        }


    }
}

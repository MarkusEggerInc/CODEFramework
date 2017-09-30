using System;
using System.Windows;

namespace CODE.Framework.Wpf.Utilities
{
    /// <summary>
    /// Provides helper features for geometry-related issues
    /// </summary>
    public static class GeometryHelper
    {
        /// <summary>
        /// Returns a rectangle structure with guaranteed valid height and width (non-negative)
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="forceToPixelInteger">If set to true, values are always rounded down to the next pixel.</param>
        /// <returns>Rect.</returns>
        public static Rect NewRect(double x, double y, double width, double height, bool forceToPixelInteger = false)
        {
            if (width < 0 || double.IsNaN(width) || double.IsInfinity(width)) width = 0;
            if (height < 0 || double.IsNaN(height) || double.IsInfinity(height)) height = 0;

            if (forceToPixelInteger) // Rounds down to full pixels 
                return new Rect((int) x, (int) y, (int) width, (int) height);
            return new Rect(x, y, width, height);
        }

        /// <summary>
        /// Returns a rectangle structure with guaranteed valid height and width (non-negative)
        /// </summary>
        /// <param name="topLeft">The top left position of the rectangle.</param>
        /// <param name="size">The size of the rectangle.</param>
        /// <param name="forceToPixelInteger">If set to true, values are always rounded down to the next pixel.</param>
        /// <returns>Rect.</returns>
        public static Rect NewRect(Point topLeft, Size size, bool forceToPixelInteger = false)
        {
            return NewRect(topLeft.X, topLeft.Y, size.Width, size.Height);
        }

        /// <summary>
        /// Returns a new rectangle structure with potential padding applied
        /// </summary>
        /// <param name="original">The original rectangle.</param>
        /// <param name="padding">The padding.</param>
        /// <returns>System.Windows.Rect.</returns>
        public static Rect NewRect(Rect original, Thickness padding)
        {
            if (Math.Abs(padding.Right) < .1d && Math.Abs(padding.Left) < .1d && Math.Abs(padding.Top) < .1d && Math.Abs(padding.Bottom) < .1d)
                return original;

            return NewRect(original.X + padding.Left, original.Y + padding.Top, original.Width - padding.Left - padding.Right, original.Height - padding.Top - padding.Right);
        }

        /// <summary>
        /// Returns a size structure with guaranteed valid height and width (non-negative)
        /// </summary>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="forceToPixelInteger">If set to true, values are always rounded down to the next pixel.</param>
        /// <returns>Size.</returns>
        public static Size NewSize(double width, double height, bool forceToPixelInteger = false)
        {
            if (width < 0 || double.IsNaN(width) || double.IsInfinity(width)) width = 0;
            if (height < 0 || double.IsNaN(height) || double.IsInfinity(height)) height = 0;

            if (forceToPixelInteger)
                return new Size((int)width, (int)height);
            return new Size(width, height);
        }
    }
}

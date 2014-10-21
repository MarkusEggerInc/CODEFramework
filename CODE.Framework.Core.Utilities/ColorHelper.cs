using System.Drawing;

namespace CODE.Framework.Core.Utilities
{
    /// <summary>
    /// This class provides various features useful for handling and manipulating colors
    /// </summary>
    public static class ColorHelper
    {
        /// <summary>
        /// This method applies lighting to a color.
        /// For instance, a color that has a lighting factor of 1 applies, appears at its original value.
        /// A color with a lighting factor of 0.5 appears only half as bright as it was before.
        /// A color with a lighting factor of 1.5 appears roughly twice as bright as before.
        /// A color with a lightning factor of 2 appears white.
        /// </summary>
        /// <param name="originalColor">Base color</param>
        /// <param name="lightFactor">Amount of light applied to the color</param>
        /// <returns>Lit color</returns>
        /// <remarks>This routine is very fast. Even when using it in tight loops, I (Markus) have not been able to 
        /// meassure a significant amount of time spent in this routine (always less than 1ms). I was originally
        /// concerened about the performance of this, so I added a caching mechanism, but that slowed things down
        /// by 2 orders of magnitude.</remarks>
        public static Color GetLitColor(Color originalColor, float lightFactor)
        {
            // Depending on the transformation required, we will proceed differently
            if (lightFactor - 1.0f < 0.01f && lightFactor - 1.0f > -0.01f)
                // No transformation needed
                return originalColor;
            if (lightFactor <= 0.0f)
                // No calculations needed. This is white
                return Color.Black;
            if (lightFactor >= 2.0f)
                // No calculations needed. This is white
                return Color.White;

            // OK, lighting is required, so here we go
            int red = originalColor.R;
            int green = originalColor.G;
            int blue = originalColor.B;

            // Do we need to add or remove light?
            if (lightFactor < 1.0f)
                // Darken
                // We can simply reduce the color intensity
                return Color.FromArgb((int) (red*lightFactor), (int) (green*lightFactor), (int) (blue*lightFactor));
            // Lighten
            // We do this by approaching 255 for a light factor of 2.0f
            var lightFactor2 = lightFactor;
            if (lightFactor2 > 1.0f) lightFactor2 -= 1.0f;
            var red2 = 255 - red;
            var green2 = 255 - green;
            var blue2 = 255 - blue;
            red += (int) (red2*lightFactor2);
            green += (int) (green2*lightFactor2);
            blue += (int) (blue2*lightFactor2);
            if (red > 255) red = 255;
            if (green > 255) green = 255;
            if (blue > 255) blue = 255;
            var newColor = Color.FromArgb(red, green, blue);
            return newColor;
        }

        /// <summary>
        /// Current theme color
        /// </summary>
        public static Color ThemeColor
        {
            get { return GetLitColor(SystemColors.ActiveCaption, 1.5f); }
        }

        /// <summary>
        /// Current light theme color
        /// </summary>
        public static Color ThemeColorLight
        {
            get { return GetLitColor(ThemeColor, 1.2f); }
        }

        /// <summary>
        /// Current dark theme color
        /// </summary>
        public static Color ThemeColorDark
        {
            get { return GetLitColor(ThemeColor, 0.925f); }
        }
    }
}

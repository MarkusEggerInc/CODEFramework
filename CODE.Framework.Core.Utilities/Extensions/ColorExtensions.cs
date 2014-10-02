using System.Drawing;

namespace CODE.Framework.Core.Utilities.Extensions
{
    /// <summary>
    /// Extension methods for ColorHelper functionality
    /// </summary>
    public static class ColorExtensions
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
        public static Color GetLitColor(this Color originalColor, float lightFactor)
        {
            return ColorHelper.GetLitColor(originalColor, lightFactor);
        }
    }
}

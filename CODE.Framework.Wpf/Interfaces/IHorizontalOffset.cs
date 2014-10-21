namespace CODE.Framework.Wpf.Interfaces
{
    /// <summary>
    /// Interfaced to express a UI element's intrinsic ability to render itself with a horizontal offset (for things like scrolling)
    /// </summary>
    public interface IHorizontalOffset
    {
        /// <summary>
        /// The actual horizontal offset
        /// </summary>
        /// <remarks>The offset may be beyond the extent of the element in scenarios where multiple offset controls coexist</remarks>
        double Offset { get; set; }

        /// <summary>
        /// The total width of the element (including visible and invisible parts)
        /// </summary>
        double ExtentWidth { get; set; }

        /// <summary>
        /// The width of the visible part of the element
        /// </summary>
        double ViewPortWidth { get; set; }
    }
}

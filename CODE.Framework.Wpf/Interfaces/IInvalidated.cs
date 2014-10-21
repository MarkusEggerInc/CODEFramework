using System;

namespace CODE.Framework.Wpf.Interfaces
{
    /// <summary>
    /// This interface defines events that can fire on various invalidation events
    /// </summary>
    public interface IInvalidated
    {
        /// <summary>
        /// Fires when InvalidateArrange() is called
        /// </summary>
        event EventHandler ArrangeInvalidated;

        /// <summary>
        /// Fires when InvalidateMeasure() is called
        /// </summary>
        event EventHandler MeasureInvalidated;

        /// <summary>
        /// Fires when InvalidateVisual() is called
        /// </summary>
        event EventHandler VisualInvalidated;
    }
}

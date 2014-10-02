using System;

namespace CODE.Framework.Wpf.Interfaces
{
    /// <summary>
    /// This interface can be implemented by UI elements who desire to support activation notification
    /// </summary>
    public interface IUserInterfaceActivation
    {
        /// <summary>
        /// Occurs when the user interface got activated
        /// </summary>
        event EventHandler Activated;

        /// <summary>
        /// Raises the activated events.
        /// </summary>
        void RaiseActivated();
    }
}

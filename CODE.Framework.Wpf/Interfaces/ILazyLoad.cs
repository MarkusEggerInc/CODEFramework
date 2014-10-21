namespace CODE.Framework.Wpf.Interfaces
{
    /// <summary>Lazy load interface</summary>
    /// <remarks>This interface can be used vor various lazy load (a.k.a. "on demand load") scenarios</remarks>
    public interface ILazyLoad
    {
        /// <summary>
        /// Indicates whether the control has loaded successfully
        /// </summary>
        /// <remarks>Should usually return true after the Load() method has been called the first time.</remarks>
        bool HasLoaded { get; }

        /// <summary>
        /// This method gets invoked whenever the object needs to load.
        /// </summary>
        /// <remarks>Typically, the implementation of this method should set HasLoaded to true</remarks>
        void Load();
    }
}

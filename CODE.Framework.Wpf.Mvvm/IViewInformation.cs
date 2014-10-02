namespace CODE.Framework.Wpf.Mvvm
{
    /// <summary>
    /// Provides information about a launched view
    /// </summary>
    public interface IViewInformation
    {
        /// <summary>
        /// Location this view was originally loaded from
        /// </summary>
        string OriginalViewLoadLocation { get; set; }
    }
}

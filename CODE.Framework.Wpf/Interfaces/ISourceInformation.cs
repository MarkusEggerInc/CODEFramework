namespace CODE.Framework.Wpf.Interfaces
{
    /// <summary>
    /// Interface to identify the source of some object or element
    /// </summary>
    public interface ISourceInformation
    {
        /// <summary>
        /// Location an element was originally loaded from
        /// </summary>
        /// <value>The original document load location.</value>
        string OriginalLoadLocation { get; set; }
    }
}

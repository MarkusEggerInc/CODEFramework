namespace CODE.Framework.Wpf.Mvvm
{
    /// <summary>
    /// This interface is used to indicate the load status (for instance on a view model)
    /// </summary>
    public interface IModelStatus
    {
        /// <summary>
        /// Indicates the load status of the model
        /// </summary>
        ModelStatus ModelStatus { get; set; }

        /// <summary>Indicates the number of operations of any kind currently in progress</summary>
        int OperationsInProgress { get; set; }
    }

    /// <summary>Indicates model load status</summary>
    public enum ModelStatus
    {
        /// <summary>Unknown (not yet set)</summary>
        Unknown,
        /// <summary>Ready (load or save complete)</summary>
        Ready,
        /// <summary>Load operation in progress</summary>
        Loading,
        /// <summary>Save operation in progress</summary>
        Saving,
        /// <summary>N/a status (usually used to indicate that no status change is desired)</summary>
        NotApplicable
    }
}

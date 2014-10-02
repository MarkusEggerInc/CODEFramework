using System;

namespace CODE.Framework.Core.ComponentModel
{
    /// <summary>
    /// Interface to be implemented by data sources that may
    /// send update messages to bound controls.
    /// </summary>
    public interface IDataBindingRefresher
    {
        /// <summary>
        /// Event that is to be raised when source updates
        /// </summary>
        event EventHandler DataSourceChanged;
    }
}

using System.Windows;

namespace CODE.Framework.Wpf.Mvvm
{
    /// <summary>Indicates that the object implementing this interface can provide a reference to an associated view</summary>
    public interface IHaveViewInformation
    {
        /// <summary>Reference to the associated view object</summary>
        UIElement AssociatedView { get; set; }
    }
}

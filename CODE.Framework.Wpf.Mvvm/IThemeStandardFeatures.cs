using System.Windows;

namespace CODE.Framework.Wpf.Mvvm
{
    /// <summary>This interface defines standard features defined in a theme</summary>
    /// <remarks>
    /// When a theme implements this interface, it not only must implement the interface, but the resulting
    /// class name must be configured in the resources as a string resource called ThemeStandardFeaturesType
    /// </remarks>
    public interface IThemeStandardFeatures
    {
        /// <summary>Reference to the standard view factory (if supported)</summary>
        IStandardViewFactory StandardViewFactory { get; }
    }

    /// <summary>This interface can be implemented to create a standard theme factory (and object that can create instances of standard themes</summary>
    public interface IStandardViewFactory
    {
        /// <summary>Returns a standard view based on the view name as a string</summary>
        /// <param name="viewName">Standard view name</param>
        /// <returns>Standard view or null</returns>
        FrameworkElement GetStandardView(string viewName);
        /// <summary>Returns a standard view based on the standard view enumeration</summary>
        /// <param name="standardView">Standard view identifier</param>
        /// <returns>Standard view or null</returns>
        FrameworkElement GetStandardView(StandardViews standardView);
    }
}

using CODE.Framework.Wpf.Controls;
using CODE.Framework.Wpf.Mvvm;

namespace CODE.Framework.Wpf.Theme.Geek.Controls
{
    /// <summary>
    /// Special zoom slider class used by the Geek shell
    /// </summary>
    public class GeekShellZoomSlider : ZoomSlider
    {
        /// <summary>
        /// Called when the zoom factor changes
        /// </summary>
        protected override void OnZoomFactorChanged()
        {
            Shell.Current.DesiredContentZoomFactor = Zoom;
        }
    }
}

using System.Windows.Controls;
using System.Windows.Input;

namespace CODE.Framework.Wpf.Theme.Newsroom.Controls
{
    /// <summary>
    /// Special scroll viewer for Newsroom UIs.
    /// </summary>
    public class NewsroomScrollViewer : ScrollViewer
    {
        /// <summary>
        /// Responds to a click of the mouse wheel.
        /// </summary>
        /// <param name="e">Required arguments that describe this event.</param>
        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            if (e.Handled) return;

            if (ScrollInfo != null)
            {
                if (e.Delta < 0) ScrollInfo.MouseWheelRight();
                else ScrollInfo.MouseWheelLeft();
            }

            e.Handled = true;
        }
    }
}

using System.Windows.Controls;
using CODE.Framework.Wpf.Mvvm;

namespace CODE.Framework.Wpf.Theme.Geek.Controls
{
    /// <summary>
    /// Special button to close notifications
    /// </summary>
    public class CloseNotificationsButton : Button
    {
        /// <summary>
        /// Called when a <see cref="T:System.Windows.Controls.Button" /> is clicked.
        /// </summary>
        protected override void OnClick()
        {
            base.OnClick();

            Shell.Current.ClearNotifications();
        }
    }
}

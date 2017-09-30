using System.ComponentModel;
using System.Windows.Controls;
using CODE.Framework.Wpf.Utilities;

namespace CODE.Framework.Wpf.Controls
{
    /// <summary>
    /// For internal use only
    /// </summary>
    [Browsable(false)]
    public class ClearTextButton : Button
    {
        /// <summary>
        /// Constructor
        /// </summary>
        protected override void OnClick()
        {
            var text = ElementHelper.FindVisualTreeParent<TextBox>(this);
            if (text == null) return;
            text.Text = string.Empty;
        }
    }
}

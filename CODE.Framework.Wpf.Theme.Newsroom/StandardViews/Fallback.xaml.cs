using System.Windows.Controls;

namespace CODE.Framework.Wpf.Theme.Newsroom.StandardViews
{
    /// <summary>
    /// Metro fallback standard view, which is used in case a desired standard view can't be found
    /// </summary>
    public partial class Fallback : Grid
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Fallback"/> class.
        /// </summary>
        public Fallback()
        {
            InitializeComponent();
        }
    }
}

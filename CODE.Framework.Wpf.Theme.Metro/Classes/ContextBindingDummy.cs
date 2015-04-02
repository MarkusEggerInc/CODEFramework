using System.Windows;
using CODE.Framework.Wpf.Mvvm;

namespace CODE.Framework.Wpf.Theme.Metro.Classes
{
    /// <summary>Invisible object used to bind elements together that are otherwise not directly bindable</summary>
    public class ContextBindingDummy : FrameworkElement
    {
        /// <summary>
        /// Gets or sets the source.
        /// </summary>
        /// <value>
        /// The source.
        /// </value>
        public object Source
        {
            get { return GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }
        /// <summary>
        /// Source
        /// </summary>
        public static readonly DependencyProperty SourceProperty = DependencyProperty.Register("Source", typeof(object), typeof(ContextBindingDummy), new UIPropertyMetadata(null, UpdateDestination));

        /// <summary>
        /// Updates the destination.
        /// </summary>
        /// <param name="s">The source</param>
        /// <param name="e">The <see cref="System.Windows.DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void UpdateDestination(DependencyObject s, DependencyPropertyChangedEventArgs e)
        {
            var dummy = s as ContextBindingDummy;
            if (dummy != null)
                if (dummy.Source != null && dummy.Destination != null)
                {
                    var action = dummy.Destination as ViewAction;
                    if (action != null)
                        action.ResourceContextObject = dummy.Source;

                    var model = dummy.Destination as StandardViewModel;
                    if (model != null)
                        model.ResourceContextObject = dummy.Source as FrameworkElement;
                }
        }

        /// <summary>
        /// Gets or sets the destination.
        /// </summary>
        /// <value>
        /// The destination.
        /// </value>
        public object Destination
        {
            get { return GetValue(DestinationProperty); }
            set { SetValue(DestinationProperty, value); }
        }

        /// <summary>
        /// Destination
        /// </summary>
        public static readonly DependencyProperty DestinationProperty = DependencyProperty.Register("Destination", typeof (object), typeof (ContextBindingDummy), new UIPropertyMetadata(null, UpdateDestination));
    }
}

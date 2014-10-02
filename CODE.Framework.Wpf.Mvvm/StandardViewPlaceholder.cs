using System.Windows;
using System.Windows.Controls;

namespace CODE.Framework.Wpf.Mvvm
{
    /// <summary>Placeholder for a standard view that is to be loaded dynamically</summary>
    public class StandardViewPlaceholder : ContentControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StandardViewPlaceholder"/> class.
        /// </summary>
        public StandardViewPlaceholder()
        {
            Margin = new Thickness(0);
            Padding = new Thickness(0);
        }

        /// <summary>Name of the standard view that is to be loaded into this placeholder control</summary>
        public string StandardViewName
        {
            get { return (string) GetValue(StandardViewNameProperty); }
            set { SetValue(StandardViewNameProperty, value); }
        }

        /// <summary>Name of the standard view that is to be loaded into this placeholder control</summary>
        public static readonly DependencyProperty StandardViewNameProperty = DependencyProperty.Register("StandardViewName", typeof (string), typeof (StandardViewPlaceholder), new UIPropertyMetadata("", StandardViewNameChanged));

        /// <summary>Fires when the standard view name changes</summary>
        /// <param name="s">Source</param>
        /// <param name="a">The <see cref="System.Windows.DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void StandardViewNameChanged(DependencyObject s, DependencyPropertyChangedEventArgs a)
        {
            var placeholder = s as StandardViewPlaceholder;
            if (placeholder == null) return;

            if (a.NewValue != a.OldValue)
            {
                var result = StandardViewEngine.GetView(a.NewValue.ToString(), string.Empty);
                if (result.FoundView && result.View != null)
                {
                    var view = result.View;
                    placeholder.Content = view;
                    view.DataContext = placeholder.DataContext;
                }
            }
        }

        private readonly static StandardViewEngine StandardViewEngine = new StandardViewEngine();

        /// <summary>Standard view (identifier) of the view that is to be loaded into this placeholder</summary>
        public StandardViews StandardView
        {
            get { return (StandardViews) GetValue(StandardViewProperty); }
            set { SetValue(StandardViewProperty, value); }
        }

        /// <summary>Standard view (identifier) of the view that is to be loaded into this placeholder</summary>
        public static readonly DependencyProperty StandardViewProperty = DependencyProperty.Register("StandardView", typeof (StandardViews), typeof (StandardViewPlaceholder), new UIPropertyMetadata(StandardViews.None, StandardViewChanged));

        /// <summary>Fires when the standard view changed</summary>
        /// <param name="s">Source</param>
        /// <param name="a">The <see cref="System.Windows.DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void StandardViewChanged(DependencyObject s, DependencyPropertyChangedEventArgs a)
        {
            var placeholder = s as StandardViewPlaceholder;
            if (placeholder == null) return;
            placeholder.StandardViewName = "CODEFrameworkStandardView" + a.NewValue;
        }
    }
}
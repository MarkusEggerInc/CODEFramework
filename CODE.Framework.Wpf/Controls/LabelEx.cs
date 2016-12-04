using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CODE.Framework.Wpf.Controls
{
    /// <summary>
    /// This class provides additional features for labels
    /// </summary>
    /// <seealso cref="System.Windows.Controls.Label" />
    public class LabelEx : Label
    {
        /// <summary>
        /// This command fires when the label is clicked
        /// </summary>
        public static readonly DependencyProperty CommandProperty = DependencyProperty.RegisterAttached("Command", typeof(ICommand), typeof(LabelEx), new PropertyMetadata(null, OnCommandChanged));

        /// <summary>
        /// Fires when a new command is assigned
        /// </summary>
        /// <param name="d">The label the command is set on</param>
        /// <param name="e">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void OnCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var label = d as Label;
            if (label == null) return;
            var command = e.NewValue as ICommand;
            if (command == null) return;

            label.MouseUp += (s, e2) =>
            {
                if (e2.ClickCount == 1 && e2.ChangedButton == MouseButton.Left)
                {
                    var command2 = GetCommand(label);
                    if (command2 == null) return;
                    if (command2.CanExecute(null))
                        command2.Execute(null);
                }
            };
        }

        /// <summary>
        /// This command fires when the label is clicked
        /// </summary>
        /// <param name="d">The label the command is set on</param>
        /// <param name="value">The command/action to be executed on click</param>
        public static void SetCommand(DependencyObject d, ICommand value)
        {
            d.SetValue(CommandProperty, value);
        }

        /// <summary>
        /// This command fires when the label is clicked
        /// </summary>
        /// <param name="d">The label the command is set on.</param>
        /// <returns>ICommand.</returns>
        public static ICommand GetCommand(DependencyObject d)
        {
            return (ICommand)d.GetValue(CommandProperty);
        }
    }
}

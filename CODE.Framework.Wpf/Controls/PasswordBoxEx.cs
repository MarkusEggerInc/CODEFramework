using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace CODE.Framework.Wpf.Controls
{
    /// <summary>
    ///     This class adds features to the PasswordBox control (in particular a bindable value property)
    /// </summary>
    /// <remarks>
    ///     Note: This object can only be used through attached properties, since the default PasswordBox control is
    ///     sealed and can thus not be used as a baseclass for this object.
    /// </remarks>
    public class PasswordBoxEx : Control
    {
        /// <summary>Attached property to set the password</summary>
        /// <remarks>This attached property can be attached to any UI Element to define row heights</remarks>
        public static readonly DependencyProperty ValueProperty = DependencyProperty.RegisterAttached("Value", typeof (string), typeof (PasswordBoxEx), new FrameworkPropertyMetadata("", ValuePropertyChanged) {BindsTwoWayByDefault = true, DefaultUpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged});

        private static bool _inPasswordChange;
        private static bool _inExternalPasswordChange;

        /// <summary>
        ///     Handler for password value changes
        /// </summary>
        /// <param name="d">Source object</param>
        /// <param name="e">Event arguments</param>
        private static void ValuePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (_inPasswordChange) return;
            var passwordBox = d as PasswordBox;
            if (passwordBox == null) return;
            passwordBox.PasswordChanged += (s, e2) =>
            {
                if (_inExternalPasswordChange) return;
                _inPasswordChange = true;
                SetValue(d, passwordBox.Password);
                _inPasswordChange = false;
            };

            _inExternalPasswordChange = true;
            if (e.NewValue != null) passwordBox.Password = e.NewValue.ToString();
            else passwordBox.Password = string.Empty;
            _inExternalPasswordChange = false;
        }

        /// <summary>Gets the password value</summary>
        /// <param name="obj">Object to set the password on</param>
        /// <returns>Password value</returns>
        public static string GetValue(DependencyObject obj)
        {
            return (string) obj.GetValue(ValueProperty);
        }

        /// <summary>Password value</summary>
        /// <param name="obj">Object to set the password value on</param>
        /// <param name="value">Value to set</param>
        public static void SetValue(DependencyObject obj, string value)
        {
            obj.SetValue(ValueProperty, value);
        }
    }
}
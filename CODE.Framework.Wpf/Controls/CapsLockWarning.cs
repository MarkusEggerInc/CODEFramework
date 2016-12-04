using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CODE.Framework.Wpf.Utilities;

namespace CODE.Framework.Wpf.Controls
{
    /// <summary>
    /// This control provides an indicator whether or not Caps-Lock is on
    /// </summary>
    /// <seealso cref="System.Windows.Controls.Control" />
    public class CapsLockWarning : Control
    {
        /// <summary>
        /// Gets or sets a value indicating whether caps lock is on.
        /// </summary>
        /// <value><c>true</c> if [caps lock on]; otherwise, <c>false</c>.</value>
        public bool CapsLockOn
        {
            get { return (bool)GetValue(CapsLockOnProperty); }
            set { SetValue(CapsLockOnProperty, value); }
        }
        /// <summary>
        /// Gets or sets a value indicating whether caps lock is on.
        /// </summary>
        public static readonly DependencyProperty CapsLockOnProperty = DependencyProperty.Register("CapsLockOn", typeof(bool), typeof(CapsLockWarning), new PropertyMetadata(false));

        private readonly GlobalKeyboardHookHelper _keyHelper;

        /// <summary>
        /// Initializes a new instance of the <see cref="CapsLockWarning"/> class.
        /// </summary>
        public CapsLockWarning()
        {
            CapsLockOn = Keyboard.GetKeyStates(Key.CapsLock) == KeyStates.Toggled;
            _keyHelper = new GlobalKeyboardHookHelper();
            _keyHelper.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Capital || e.Key == Key.CapsLock)
                    CapsLockOn = Keyboard.GetKeyStates(Key.CapsLock) != KeyStates.Toggled;
            };

            Unloaded += (s2, e2) =>
            {
                _keyHelper.Dispose();
            };
        }
    }
}

using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace CODE.Framework.Wpf.Security
{
    /// <summary>
    /// This class provides various security features for WPF applications
    /// </summary>
    public class Security : DependencyObject
    {
        /// <summary>
        /// Defines the roles the current user has to be in to be granted read-only access to the control this applies to
        /// </summary>
        public static string GetReadOnlyRoles(DependencyObject d)
        {
            return (string)d.GetValue(ReadOnlyRolesProperty);
        }
        /// <summary>
        /// Defines the roles the current user has to be in to be granted read-only access to the control this applies to
        /// </summary>
        public static void SetReadOnlyRoles(DependencyObject d, string value)
        {
            d.SetValue(ReadOnlyRolesProperty, value);
        }

        /// <summary>
        /// Defines the roles the current user has to be in to be granted read-only access to the control this applies to
        /// </summary>
        public static readonly DependencyProperty ReadOnlyRolesProperty = DependencyProperty.RegisterAttached("ReadOnlyRoles", typeof(string), typeof(Security), new PropertyMetadata("", OnReadOnlyRolesChanged));

        /// <summary>
        /// Fires when read-only roles are assigned
        /// </summary>
        /// <param name="d">The object the roles are set on</param>
        /// <param name="e">Event arguments</param>
        private static void OnReadOnlyRolesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var element = d as UIElement;
            if (element == null) return;

            var readOnlyRoles = e.NewValue == null ? string.Empty : e.NewValue.ToString().Trim();
            var fullAccessRoles = GetFullAccessRoles(d);
            fullAccessRoles = fullAccessRoles == null ? string.Empty : fullAccessRoles.Trim();
            if (!string.IsNullOrEmpty(readOnlyRoles) && string.IsNullOrEmpty(fullAccessRoles))
                fullAccessRoles = "#######"; // Faking full access roles, because if there are read-only roles and no full-access roles, then it is nonsensical to let everyone have full access no matter what role they are in
            SetControlRights(element, readOnlyRoles, fullAccessRoles);
        }

        /// <summary>
        /// Defines the roles the current user has to be in to be granted full access to the control this applies to
        /// </summary>
        public static string GetFullAccessRoles(DependencyObject d)
        {
            return (string)d.GetValue(FullAccessRolesProperty);
        }
        /// <summary>
        /// Defines the roles the current user has to be in to be granted full access to the control this applies to
        /// </summary>
        public static void SetFullAccessRoles(DependencyObject d, string value)
        {
            d.SetValue(FullAccessRolesProperty, value);
        }

        /// <summary>
        /// Defines the roles the current user has to be in to be granted full access to the control this applies to
        /// </summary>
        public static readonly DependencyProperty FullAccessRolesProperty = DependencyProperty.RegisterAttached("FullAccessRoles", typeof(string), typeof(Security), new PropertyMetadata("", OnFullAccessRolesChanged));

        /// <summary>
        /// Fires when read-only roles are assigned
        /// </summary>
        /// <param name="d">The object the roles are set on</param>
        /// <param name="e">Event arguments</param>
        private static void OnFullAccessRolesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var element = d as UIElement;
            if (element == null) return;

            var readOnlyRoles = GetReadOnlyRoles(d) ?? string.Empty;
            readOnlyRoles = readOnlyRoles.Trim();
            var fullAccessRoles = e.NewValue == null ? string.Empty : e.NewValue.ToString().Trim();
            SetControlRights(element, readOnlyRoles, fullAccessRoles);
        }

        private static void SetControlRights(UIElement element, string readOnlyRoles, string fullAccessRoles)
        {
            var isReadOnlyAccess = DoesUserHaveAccess(readOnlyRoles);
            var isFullAccess = DoesUserHaveAccess(fullAccessRoles);
            if (isFullAccess)
            {
                element.Visibility = Visibility.Visible;
                SetReadOnlyState(element, false);
            }
            else if (isReadOnlyAccess)
            {
                element.Visibility = Visibility.Visible;
                SetReadOnlyState(element, true);
            }
            else
                // The user has no access. The control should not be accessible to the user at all
                element.Visibility = Visibility.Collapsed;
        }

        private static void SetReadOnlyState(UIElement element, bool isReadOnly)
        {
            // If we have a control with a real read-only property, we use that. Otherwise, we disable the element

            var textBoxBase = element as TextBoxBase;
            if (textBoxBase != null)
            {
                textBoxBase.IsReadOnly = isReadOnly;
                return;
            }

            var comboBox = element as ComboBox;
            if (comboBox != null)
            {
                comboBox.IsReadOnly = isReadOnly;
                return;
            }

            var dataGrid = element as DataGrid;
            if (dataGrid != null)
            {
                dataGrid.IsReadOnly = isReadOnly;
                return;
            }

            element.IsEnabled = !isReadOnly;
        }

        private static bool DoesUserHaveAccess(string allRoles)
        {
            if (string.IsNullOrEmpty(allRoles)) return true; // If no roles are defined, everyone is considered to have access

            var roles = allRoles.Split(',');
            var currentPrincipal = Thread.CurrentPrincipal;
            return roles.Any(role => currentPrincipal.IsInRole(role));
        }
    }
}
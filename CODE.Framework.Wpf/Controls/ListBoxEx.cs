using System;
using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CODE.Framework.Core.Utilities;

namespace CODE.Framework.Wpf.Controls
{
    /// <summary>
    /// ListBox with additional features
    /// </summary>
    public class ListBoxEx : ListBox
    {
        /// <summary>Attached property to set a listbox's command</summary>
        public static readonly DependencyProperty CommandProperty = DependencyProperty.RegisterAttached("Command", typeof(ICommand), typeof(ListBoxEx), new PropertyMetadata(null, CommandPropertyChanged));

        /// <summary>
        /// Handler for command changes
        /// </summary>
        /// <param name="d">Source object</param>
        /// <param name="e">Event arguments</param>
        private static void CommandPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var source = d as ListBox;
            if (source == null) return;

            // We reset all the handlers we attached, to make sure we don't have old ones hanging around after changes
            source.SelectionChanged -= SelectionChangedCommandTrigger;
            source.MouseDoubleClick -= MouseDoubleClickCommandTrigger;
            source.MouseLeftButtonUp -= MouseClickCommandTrigger;

            // We also hook both triggers (each of which will then check whether it needs to be executed as it happens)
            source.SelectionChanged += SelectionChangedCommandTrigger;
            source.MouseDoubleClick += MouseDoubleClickCommandTrigger;
            source.MouseLeftButtonUp += MouseClickCommandTrigger;

            var command = e.NewValue as ICommand;
            if (command != null && GetTriggerCommandOnEnter(source))
                source.InputBindings.Add(new InputBinding(command, new KeyGesture(Key.Enter)));
        }

        /// <summary>
        /// Triggers a potential attached command after a click (which is essentially a select)
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="MouseButtonEventArgs"/> instance containing the event data.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        private static void MouseClickCommandTrigger(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount != 1) return;
            if (e.ChangedButton != MouseButton.Left) return;

            if (_doNotTriggerClick)
            {
                _doNotTriggerClick = false;
                return;
            }

            _doNotTriggerSelect = true; // We do not also want to trigger a select

            var listBox = sender as ListBox;
            if (listBox == null) return;
            if (listBox.SelectedItem == null) return;
            if (GetCommandTrigger(listBox) != ListBoxCommandTrigger.DoubleClick)
                TriggerCommand(listBox, listBox.SelectedItem);
        }

        private static bool _doNotTriggerSelect;
        private static bool _doNotTriggerClick;

        /// <summary>
        /// Triggers a potentially attached command after double-click
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.Windows.Input.MouseButtonEventArgs"/> instance containing the event data.</param>
        private static void MouseDoubleClickCommandTrigger(object sender, MouseButtonEventArgs e)
        {
            var listBox = sender as ListBox;
            if (listBox == null) return;
            if (GetCommandTrigger(listBox) != ListBoxCommandTrigger.Select)
                TriggerCommand(listBox, listBox.SelectedItem);
        }

        /// <summary>
        /// Triggers a potentially attached command after selection changes.
        /// </summary>
        /// <param name="sender">The ListBox.</param>
        /// <param name="e">The <see cref="System.Windows.Controls.SelectionChangedEventArgs"/> instance containing the event data.</param>
        private static void SelectionChangedCommandTrigger(object sender, SelectionChangedEventArgs e)
        {
            if (_doNotTriggerSelect)
            {
                _doNotTriggerSelect = false;
                return;
            }

            _doNotTriggerClick = true;

            var listBox = sender as ListBox;
            if (listBox == null) return;
            if (GetCommandTrigger(listBox) == ListBoxCommandTrigger.DoubleClick) return;
            if (e.AddedItems.Count > 0)
                TriggerCommand(listBox, GetCommandParameterMode(listBox) == ListBoxCommandParameterMode.SingleSelectedObject ? e.AddedItems[e.AddedItems.Count - 1] : listBox.SelectedItems);
            else
                TriggerCommand(listBox, null);
        }

        /// <summary>
        /// Triggers the associated command.
        /// </summary>
        /// <param name="sender">The sender (ListBox) that triggered the operation.</param>
        /// <param name="selectedItem">The selected item (used as the command parameter unless an explicit parameter is set).</param>
        private static void TriggerCommand(DependencyObject sender, object selectedItem)
        {
            var command = GetCommand(sender);
            if (command == null) return;
            var parameter = GetCommandParameter(sender) ?? selectedItem;
            if (command.CanExecute(parameter)) command.Execute(parameter);
        }

        /// <summary>Command to be triggered on items in the list</summary>
        /// <param name="obj">Object to set command on</param>
        /// <returns>Command</returns>
        /// <remarks>This attached property can be attached to any UI Element to define a command</remarks>
        public static ICommand GetCommand(DependencyObject obj)
        {
            return (ICommand) obj.GetValue(CommandProperty);
        }

        /// <summary>Command</summary>
        /// <param name="obj">Object to set the command on</param>
        /// <param name="value">Value to set</param>
        public static void SetCommand(DependencyObject obj, ICommand value)
        {
            obj.SetValue(CommandProperty, value);
        }

        /// <summary>Attached property to set command trigger</summary>
        /// <remarks>This attached property can be attached to any UI Element to define a command trigger mode</remarks>
        public static readonly DependencyProperty CommandTriggerProperty = DependencyProperty.RegisterAttached("CommandTrigger", typeof(ListBoxCommandTrigger), typeof(ListBoxEx), new PropertyMetadata(ListBoxCommandTrigger.DoubleClickAndSelect));

        /// <summary>Command trigger mode</summary>
        /// <param name="obj">Object to set the command trigger on</param>
        /// <returns>Command trigger mode</returns>
        /// <remarks>This attached property can be attached to any UI Element to define the command trigger mode</remarks>
        public static ListBoxCommandTrigger GetCommandTrigger(DependencyObject obj)
        {
            return (ListBoxCommandTrigger) obj.GetValue(CommandTriggerProperty);
        }

        /// <summary>Command trigger</summary>
        /// <param name="obj">Object to set the command trigger on</param>
        /// <param name="value">Value to set</param>
        public static void SetCommandTrigger(DependencyObject obj, ListBoxCommandTrigger value)
        {
            obj.SetValue(CommandTriggerProperty, value);
        }

        /// <summary>Attached property to set command parameter mode</summary>
        /// <remarks>This attached property can be attached to any UI Element to define a command parameter mode</remarks>
        public static readonly DependencyProperty CommandParameterModeProperty = DependencyProperty.RegisterAttached("CommandParameterMode", typeof(ListBoxCommandParameterMode), typeof(ListBoxEx), new PropertyMetadata(ListBoxCommandParameterMode.SingleSelectedObject));

        /// <summary>Command parameter mode</summary>
        /// <param name="obj">Object to set the command parameter mode on</param>
        /// <returns>Command parameter mode</returns>
        /// <remarks>This attached property can be attached to any UI Element to define the command parameter mode</remarks>
        public static ListBoxCommandParameterMode GetCommandParameterMode(DependencyObject obj)
        {
            return (ListBoxCommandParameterMode) obj.GetValue(CommandParameterModeProperty);
        }

        /// <summary>Command parameter mode</summary>
        /// <param name="obj">Object to set the command parameter mode on</param>
        /// <param name="value">Value to set</param>
        public static void SetCommandParameterMode(DependencyObject obj, ListBoxCommandTrigger value)
        {
            obj.SetValue(CommandParameterModeProperty, value);
        }

        /// <summary>Attached property to set the command parameter</summary>
        /// <remarks>This attached property can be attached to any UI Element to define the command parameter</remarks>
        public static readonly DependencyProperty CommandParameterProperty = DependencyProperty.RegisterAttached("CommandParameter", typeof(object), typeof(ListBoxEx), new PropertyMetadata(null));

        /// <summary>Command parameter</summary>
        /// <param name="obj">Object to set the command parameter on</param>
        /// <returns>Command parameter</returns>
        /// <remarks>This attached property can be attached to any UI Element to define the command parameter</remarks>
        public static object GetCommandParameter(DependencyObject obj)
        {
            return obj.GetValue(CommandParameterProperty);
        }

        /// <summary>Command parameter</summary>
        /// <param name="obj">Object to set the command parameter on</param>
        /// <param name="value">Value to set</param>
        public static void SetCommandParameter(DependencyObject obj, object value)
        {
            obj.SetValue(CommandParameterProperty, value);
        }

        /// <summary>Indicates whether the associated command shall be triggered when the user hits ENTER on the listbox </summary>
        public static bool GetTriggerCommandOnEnter(DependencyObject d)
        {
            return (bool) d.GetValue(TriggerCommandOnEnterProperty);
        }

        /// <summary>Indicates whether the associated command shall be triggered when the user hits ENTER on the listbox </summary>
        public static void SetTriggerCommandOnEnter(DependencyObject d, bool value)
        {
            d.SetValue(TriggerCommandOnEnterProperty, value);
        }

        /// <summary>Indicates whether the associated command shall be triggered when the user hits ENTER on the listbox </summary>
        public static readonly DependencyProperty TriggerCommandOnEnterProperty = DependencyProperty.RegisterAttached("TriggerCommandOnEnter", typeof(bool), typeof(ListBoxEx), new PropertyMetadata(false, OnTriggerCommandOnEnterChanged));

        /// <summary>
        /// Fires when TriggerCommandOnEnter changes
        /// </summary>
        /// <param name="d">The object the parameter is set on</param>
        /// <param name="e">Event arguments</param>
        private static void OnTriggerCommandOnEnterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(bool) e.NewValue) return;
            var control = d as ListBox;
            if (control == null) return;
            var command = GetCommand(d);
            if (command != null)
                control.InputBindings.Add(new InputBinding(command, new KeyGesture(Key.Enter)));
        }

        /// <summary>Provides a way to bind selected items into an observable collection</summary>
        public static readonly DependencyProperty SelectedItemCollectionProperty = DependencyProperty.RegisterAttached("SelectedItemCollection", typeof(object), typeof(ListBoxEx), new UIPropertyMetadata(null, SelectedItemCollectionPropertyChanged));

        /// <summary>Collection containing selected items</summary>
        /// <param name="obj">Object to set collection on</param>
        /// <returns>Collection</returns>
        public static object GetSelectedItemCollection(DependencyObject obj)
        {
            return obj.GetValue(SelectedItemCollectionProperty);
        }

        /// <summary>Selected item collection</summary>
        /// <param name="obj">Object to set the collection on</param>
        /// <param name="value">Value to set</param>
        public static void SetSelectedItemCollection(DependencyObject obj, ICommand value)
        {
            obj.SetValue(SelectedItemCollectionProperty, value);
        }

        /// <summary>Fires when the selected item collection is changed</summary>
        /// <param name="d">Dependency object (listbox, most likely)</param>
        /// <param name="e">The <see cref="System.Windows.DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void SelectedItemCollectionPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            If.Real<ListBox, IList>(d, e.NewValue, (listBox, collection) =>
            {
                listBox.SelectionChanged += (s, e2) =>
                {
                    collection.Clear();
                    foreach (var item in listBox.SelectedItems)
                        collection.Add(item);
                };
            });
        }

        /// <summary>Indicates whether or not the listbox shall automatically scroll to selected items when the selected item changes</summary>
        public static readonly DependencyProperty AutoScrollToSelectedItemProperty = DependencyProperty.RegisterAttached("AutoScrollToSelectedItem", typeof(bool), typeof(ListBoxEx), new UIPropertyMetadata(false, AutoScrollToSelectedItemChanged));

        /// <summary>
        /// If true, the listbox automatically scrolls to the selected item.
        /// </summary>
        /// <param name="o">The object the value has been set on.</param>
        /// <param name="args">The <see cref="System.Windows.DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void AutoScrollToSelectedItemChanged(DependencyObject o, DependencyPropertyChangedEventArgs args)
        {
            var list = o as ListBox;
            if (list == null || list.Items == null) return;

            list.SelectionChanged += (s, e) =>
            {
                var index = list.SelectedIndex;
                if (!GetAutoScrollToSelectedItem(list)) return;
                if (list.Items.Count > index && index >= 0)
                    list.ScrollIntoView(list.Items[index]);
            };
        }

        /// <summary>Indicates whether or not the listbox shall automatically scroll to selected items when the selected item changes</summary>
        public static bool GetAutoScrollToSelectedItem(DependencyObject o)
        {
            return (bool) o.GetValue(AutoScrollToSelectedItemProperty);
        }

        /// <summary>Indicates whether or not the listbox shall automatically scroll to selected items when the selected item changes</summary>
        public static void SetAutoScrollToSelectedItem(DependencyObject o, bool value)
        {
            o.SetValue(AutoScrollToSelectedItemProperty, value);
        }
    }

    /// <summary>
    /// Defines when the command on the ListBox is to be triggered
    /// </summary>
    public enum ListBoxCommandTrigger
    {
        /// <summary>
        /// Trigger command on item double-click
        /// </summary>
        DoubleClick,

        /// <summary>
        /// Trigger command on item selection
        /// </summary>
        Select,

        /// <summary>
        /// Trigger command after either double-click or selection changed
        /// </summary>
        DoubleClickAndSelect
    }

    /// <summary>
    /// Defines how a default listbox command passes selection information as the command parameter
    /// </summary>
    public enum ListBoxCommandParameterMode
    {
        /// <summary>
        /// Pass only one selected object, even if there are multiple selected objects (passes the first object in the list of *newly selected items* if more than one item is selected)
        /// </summary>
        SingleSelectedObject,

        /// <summary>
        /// Passes the entire list of selected objects
        /// </summary>
        ListOfSelectedObjects
    }
}
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CODE.Framework.Wpf.Controls
{
    /// <summary>
    /// Tree view with additional features
    /// </summary>
    public class TreeViewEx : TreeView
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TreeViewEx"/> class.
        /// </summary>
        public TreeViewEx()
        {
            SelectedItemChanged += (s, e) => SelectedNode = e.NewValue;
        }

        /// <summary>Selected node (item)</summary>
        public object SelectedNode
        {
            get { return GetValue(SelectedNodeProperty); }
            set { SetValue(SelectedNodeProperty, value); }
        }

        /// <summary>Selected node (item)</summary>
        public static readonly DependencyProperty SelectedNodeProperty = DependencyProperty.Register("SelectedNode", typeof (object), typeof (TreeViewEx), new FrameworkPropertyMetadata(null, SelectedNodePropertyChanged) {BindsTwoWayByDefault = true});

        /// <summary>Change-handler for the selected node property</summary>
        /// <param name="d">The dependency object the property is set on (TreeViewEx).</param>
        /// <param name="e">The <see cref="System.Windows.DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void SelectedNodePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var tree = d as TreeViewEx;
            if (tree == null) return;

            if (tree.SelectedNode != tree.SelectedItem)
                tree.SelectItem(tree.SelectedNode);
        }

        /// <summary>
        /// Selects the item in the tree
        /// </summary>
        /// <param name="item">The data bound tree item</param>
        public void SelectItem(object item)
        {
            var container = ItemContainerGenerator.ContainerFromItem(item);
            if (container == null) return;
            var treeItem = container as TreeViewItem;
            if (treeItem == null) return;

            treeItem.IsSelected = true;
            if (BringItemIntoViewWhenSelected) treeItem.BringIntoView();
            treeItem.Focus();

            var info = typeof (TreeViewItem).GetMethod("Select", BindingFlags.NonPublic | BindingFlags.Instance);
            if (info == null) return;
            info.Invoke(treeItem, new object[] {true});
        }

        ///<summary>When set to true, programmatically selecting an item in the tree will also bring it into view</summary>
        public bool BringItemIntoViewWhenSelected
        {
            get { return (bool)GetValue(BringItemIntoViewWhenSelectedProperty); }
            set { SetValue(BringItemIntoViewWhenSelectedProperty, value); }
        }

        ///<summary>When set to true, programmatically selecting an item in the tree will also bring it into view</summary>
        public static readonly DependencyProperty BringItemIntoViewWhenSelectedProperty 
            = DependencyProperty.Register("BringItemIntoViewWhenSelected", typeof(bool), typeof(TreeViewEx));

        /// <summary>Attached property to set a tree view's command</summary>
        public static readonly DependencyProperty CommandProperty = DependencyProperty.RegisterAttached("Command", typeof(ICommand), typeof(TreeViewEx), new PropertyMetadata(null, CommandPropertyChanged));
        /// <summary>
        /// Handler for command changes
        /// </summary>
        /// <param name="d">Source object</param>
        /// <param name="e">Event arguments</param>
        private static void CommandPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var source = d as TreeView;
            if (source != null)
            {
                // We reset all the handlers we attached, to make sure we don't have old ones hanging around after changes
                source.SelectedItemChanged -= SelectionChangedCommandTrigger;
                source.MouseDoubleClick -= MouseDoubleClickCommandTrigger;

                // We also hook both triggers (each of which will then check whether it needs to be executed as it happens)
                source.SelectedItemChanged += SelectionChangedCommandTrigger;
                source.MouseDoubleClick += MouseDoubleClickCommandTrigger;
            }
        }

        /// <summary>
        /// Triggers a potentially attached command after double-click
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.Windows.Input.MouseButtonEventArgs"/> instance containing the event data.</param>
        static void MouseDoubleClickCommandTrigger(object sender, MouseButtonEventArgs e)
        {
            var treeView = sender as TreeView;
            if (treeView == null) return;
            if (GetCommandTrigger(treeView) != TreeViewCommandTrigger.Select)
                TriggerCommand(treeView, treeView.SelectedItem);
        }

        /// <summary>
        /// Triggers a potentially attached command after selection changes.
        /// </summary>
        /// <param name="sender">The TreeViewEx object.</param>
        /// <param name="e">The <see cref="object"/> instance containing the event data.</param>
        static void SelectionChangedCommandTrigger(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var treeView = sender as TreeView;
            if (treeView == null) return;
            if (GetCommandTrigger(treeView) != TreeViewCommandTrigger.DoubleClick)
                TriggerCommand(treeView, e.NewValue);
        }

        /// <summary>
        /// Triggers the associated command.
        /// </summary>
        /// <param name="sender">The sender (TreeViewEx) that triggered the operation.</param>
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
        public static ICommand GetCommand(DependencyObject obj) { return (ICommand)obj.GetValue(CommandProperty); }
        /// <summary>Command</summary>
        /// <param name="obj">Object to set the command on</param>
        /// <param name="value">Value to set</param>
        public static void SetCommand(DependencyObject obj, ICommand value) { obj.SetValue(CommandProperty, value); }

        /// <summary>Attached property to set command trigger</summary>
        /// <remarks>This attached property can be attached to any UI Element to define a command trigger mode</remarks>
        public static readonly DependencyProperty CommandTriggerProperty = DependencyProperty.RegisterAttached("CommandTrigger", typeof(TreeViewCommandTrigger), typeof(TreeViewEx), new PropertyMetadata(TreeViewCommandTrigger.DoubleClickAndSelect));
        /// <summary>Command trigger mode</summary>
        /// <param name="obj">Object to set the command trigger on</param>
        /// <returns>Command trigger mode</returns>
        /// <remarks>This attached property can be attached to any UI Element to define the command trigger mode</remarks>
        public static TreeViewCommandTrigger GetCommandTrigger(DependencyObject obj) { return (TreeViewCommandTrigger)obj.GetValue(CommandTriggerProperty); }
        /// <summary>Command trigger</summary>
        /// <param name="obj">Object to set the command trigger on</param>
        /// <param name="value">Value to set</param>
        public static void SetCommandTrigger(DependencyObject obj, TreeViewCommandTrigger value) { obj.SetValue(CommandTriggerProperty, value); }

        /// <summary>Attached property to set the command parameter</summary>
        /// <remarks>This attached property can be attached to any UI Element to define the command parameter</remarks>
        public static readonly DependencyProperty CommandParameterProperty = DependencyProperty.RegisterAttached("CommandParameter", typeof(object), typeof(TreeViewEx), new PropertyMetadata(null));
        /// <summary>Command parameter</summary>
        /// <param name="obj">Object to set the command parameter on</param>
        /// <returns>Command parameter</returns>
        /// <remarks>This attached property can be attached to any UI Element to define the command parameter</remarks>
        public static object GetCommandParameter(DependencyObject obj) { return obj.GetValue(CommandParameterProperty); }
        /// <summary>Cmmand parameter</summary>
        /// <param name="obj">Object to set the command parameter on</param>
        /// <param name="value">Value to set</param>
        public static void SetCommandParameter(DependencyObject obj, object value) { obj.SetValue(CommandParameterProperty, value); }
    }

    /// <summary>
    /// Defines when the command on the TreeView is to be triggered
    /// </summary>
    public enum TreeViewCommandTrigger
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
}

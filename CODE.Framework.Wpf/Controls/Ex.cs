using System;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using CODE.Framework.Core.Utilities;

namespace CODE.Framework.Wpf.Controls
{
    /// <summary>
    /// Generic extensions applicable to all controls and panels
    /// </summary>
    public class Ex : DependencyObject
    {
        /// <summary>Attached property to set a single command event</summary>
        /// <remarks>This attached property can be attached to any UI Element to define a command used to handle an event</remarks>
        public static readonly DependencyProperty EventCommandProperty = DependencyProperty.RegisterAttached("EventCommand", typeof(EventCommand), typeof(Ex), new PropertyMetadata(null, EventCommandPropertyChanged));

        /// <summary>Handler for event command changes</summary>
        /// <param name="d">Source object</param>
        /// <param name="e">Event arguments</param>
        private static void EventCommandPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            If.Real<FrameworkElement, EventCommand>(d, e.NewValue, (e2, c) => c.SetUIElement(e2));
            If.Real<FrameworkContentElement, EventCommand>(d, e.NewValue, (e2, c) => c.SetUIElement(e2));
        }

        /// <summary>Event command</summary>
        /// <param name="obj">Object to get the event command for</param>
        /// <returns>Event command</returns>
        /// <remarks>This attached property can be attached to any UI Element to define an event command</remarks>
        public static EventCommand GetEventCommand(DependencyObject obj)
        {
            return (EventCommand) obj.GetValue(EventCommandProperty);
        }

        /// <summary>Event command</summary>
        /// <param name="obj">Object to set the event command on</param>
        /// <param name="value">Value to set</param>
        public static void SetEventCommand(DependencyObject obj, EventCommand value)
        {
            obj.SetValue(EventCommandProperty, value);
        }

        /// <summary>Attached property to set multiple command events</summary>
        /// <remarks>This attached property can be attached to any UI Element to define a command used to handle an event</remarks>
        public static readonly DependencyProperty EventCommandsProperty = DependencyProperty.RegisterAttached("EventCommands", typeof(EventCommandsCollection), typeof(Ex), new PropertyMetadata(null, EventCommandsPropertyChanged));

        /// <summary>Handler for event commands changes</summary>
        /// <param name="d">Source object</param>
        /// <param name="e">Event arguments</param>
        private static void EventCommandsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            If.Real<FrameworkElement, EventCommandsCollection>(d, e.NewValue, (e2, commands) =>
            {
                commands.SetUIElement(e2);
                foreach (var eventCommand in commands)
                    commands.ConfigureEventCommand(eventCommand);
            });
            If.Real<FrameworkContentElement, EventCommandsCollection>(d, e.NewValue, (e2, commands) =>
            {
                commands.SetUIElement(e2);
                foreach (var eventCommand in commands)
                    commands.ConfigureEventCommand(eventCommand);
            });
        }

        /// <summary>Event commands</summary>
        /// <param name="obj">Object to get the event commands for</param>
        /// <returns>Event command</returns>
        /// <remarks>This attached property can be attached to any UI Element to define an event command</remarks>
        public static EventCommandsCollection GetEventCommands(DependencyObject obj)
        {
            return (EventCommandsCollection) obj.GetValue(EventCommandsProperty);
        }

        /// <summary>Event commands</summary>
        /// <param name="obj">Object to set the event command on</param>
        /// <param name="value">Value to set</param>
        public static void SetEventCommands(DependencyObject obj, EventCommandsCollection value)
        {
            obj.SetValue(EventCommandsProperty, value);
        }

        /// <summary>Defines whether an object (such as a textbox) automatically is selected when focus moves into it</summary>
        public static readonly DependencyProperty SelectOnEntryProperty = DependencyProperty.RegisterAttached("SelectOnEntry", typeof(bool), typeof(Ex), new UIPropertyMetadata(false, SelectOnEntryChanged));

        private static void SelectOnEntryChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if ((bool) e.NewValue)
                If.Real<TextBoxBase>(d, t => { t.GotFocus += (s, e2) => t.SelectAll(); });
        }

        /// <summary>Defines whether an object (such as a textbox) automatically is selected when focus moves into it</summary>
        /// <param name="obj">The object to set the value on</param>
        /// <param name="value">True for auto-select</param>
        public static void SetSelectOnEntry(DependencyObject obj, bool value)
        {
            obj.SetValue(SelectOnEntryProperty, value);
        }

        /// <summary>Defines whether an object (such as a textbox) automatically is selected when focus moves into it</summary>
        /// <param name="obj">The object to retrieve the value for</param>
        /// <returns>True if auto-select</returns>
        public static bool GetSelectOnEntry(DependencyObject obj)
        {
            return (bool) obj.GetValue(SelectOnEntryProperty);
        }
    }

    /// <summary>
    /// This object can be used to route any event to a command
    /// </summary>
    public class EventCommand : FrameworkElement
    {
        /// <summary>Called ot set the UI element this event command belongs to.</summary>
        /// <param name="attachedTo">Associated object  </param>
        public void SetUIElement(FrameworkElement attachedTo)
        {
            _attachedTo = attachedTo;
            if (attachedTo != null)
            {
                if (attachedTo.DataContext != null)
                    DataContext = attachedTo.DataContext;
                attachedTo.DataContextChanged += (s, o) =>
                {
                    var at2 = s as FrameworkElement; // Using the source object rather than the local attachedTo property to avoid the overhead of an enclosure
                    if (at2 == null) return;
                    if (at2.DataContext != null)
                        DataContext = at2.DataContext;
                };
            }
            HookEvent();
        }

        /// <summary>Called ot set the UI element this event command belongs to.</summary>
        /// <param name="attachedTo">Associated object  </param>
        public void SetUIElement(FrameworkContentElement attachedTo)
        {
            _attachedTo2 = attachedTo;
            if (attachedTo != null)
            {
                if (attachedTo.DataContext != null)
                    DataContext = attachedTo.DataContext;
                attachedTo.DataContextChanged += (s, o) =>
                {
                    var at2 = s as FrameworkContentElement; // Using the source object rather than the local attachedTo property to avoid the overhead of an enclosure
                    if (at2 == null) return;
                    if (at2.DataContext != null)
                        DataContext = at2.DataContext;
                };
            }
            HookEvent();
        }

        private FrameworkElement _attachedTo;
        private FrameworkContentElement _attachedTo2;

        /// <summary>Event that is to fire the command</summary>
        public string Event
        {
            get { return (string) GetValue(EventProperty); }
            set { SetValue(EventProperty, value); }
        }

        /// <summary>Event that is to fire the command</summary>
        public static readonly DependencyProperty EventProperty = DependencyProperty.Register("Event", typeof(string), typeof(EventCommand), new UIPropertyMetadata("", (d, e) => If.Real<EventCommand>(d, d2 => d2.HookEvent())));

        /// <summary>Command that is to be executed when the desired event fires.</summary>
        public ICommand Command
        {
            get { return (ICommand) GetValue(CommandProperty); }
            set { SetValue(CommandProperty, value); }
        }

        /// <summary>Command that is to be executed when the desired event fires.</summary>
        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register("Command", typeof(ICommand), typeof(EventCommand), new UIPropertyMetadata(null, (d, e) => If.Real<EventCommand>(d, d2 => d2.HookEvent())));

        /// <summary>Command parameter associated with the command.</summary>
        public object CommandParameter
        {
            get { return GetValue(CommandParameterProperty); }
            set { SetValue(CommandParameterProperty, value); }
        }

        /// <summary>Command parameter associated with the command.</summary>
        public static readonly DependencyProperty CommandParameterProperty = DependencyProperty.Register("CommandParameter", typeof(object), typeof(EventCommand), new UIPropertyMetadata(null, (d, e) => If.Real<EventCommand>(d, d2 => d2.HookEvent())));

        private void HookEvent()
        {
            string eventName = Event;
            if ((_attachedTo == null && _attachedTo2 == null) || Command == null || string.IsNullOrEmpty(eventName)) return;

            var attachedType = _attachedTo != null ? _attachedTo.GetType() : _attachedTo2.GetType();
            var eventToAttach = attachedType.GetEvent(eventName);
            if (eventToAttach != null)
            {
                var methodInfo = GetType().GetMethod("Execute", BindingFlags.Instance | BindingFlags.NonPublic);
                var del = Delegate.CreateDelegate(eventToAttach.EventHandlerType, this, methodInfo);
                if (_attachedTo != null) eventToAttach.AddEventHandler(_attachedTo, del);
                else eventToAttach.AddEventHandler(_attachedTo2, del);
            }
            else
                throw new NullReferenceException("Command cannot be bound to event '" + eventName + "'. Event not found.");
        }

        /// <summary>
        /// This method is referenced by reflection and used to pass an event to the associated command
        /// </summary>
        /// <param name="sender">Original event sender</param>
        /// <param name="e">Event parameters</param>
        private void Execute(object sender, object e)
        {
            var parameter = new EventCommandParameters
            {
                CommandParameter = CommandParameter,
                Sender = sender,
                EventArgs = e
            };
            if (Command.CanExecute(parameter)) Command.Execute(parameter);
        }
    }

    /// <summary>
    /// Used to pass sender, event arguments, and parameter information to an event command
    /// </summary>
    public class EventCommandParameters
    {
        /// <summary>Original source of the event</summary>
        public object Sender { get; set; }

        /// <summary>Original event arguments</summary>
        public object EventArgs { get; set; }

        /// <summary>Command parameter</summary>
        public object CommandParameter { get; set; }
    }

    /// <summary>Collection of Event Command objects</summary>
    public class EventCommandsCollection : ObservableCollection<EventCommand>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventCommandsCollection"/> class.
        /// </summary>
        public EventCommandsCollection()
        {
            CollectionChanged += (s, e) =>
            {
                foreach (var newItem in e.NewItems)
                    If.Real<EventCommand>(newItem, ConfigureEventCommand);
            };
        }

        /// <summary>Called ot set the UI element this event command belongs to.</summary>
        /// <param name="attachedTo">Associated object  </param>
        public void SetUIElement(FrameworkElement attachedTo)
        {
            _attachedTo = attachedTo;
        }

        /// <summary>Called ot set the UI element this event command belongs to.</summary>
        /// <param name="attachedTo">Associated object  </param>
        public void SetUIElement(FrameworkContentElement attachedTo)
        {
            _attachedTo2 = attachedTo;
        }

        /// <summary>Configures a newly added event command</summary>
        public void ConfigureEventCommand(EventCommand command)
        {
            if (_attachedTo != null) command.SetUIElement(_attachedTo);
            else command.SetUIElement(_attachedTo2);
        }

        private FrameworkElement _attachedTo;
        private FrameworkContentElement _attachedTo2;
    }
}
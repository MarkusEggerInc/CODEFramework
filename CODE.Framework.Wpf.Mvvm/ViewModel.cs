using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;

namespace CODE.Framework.Wpf.Mvvm
{
    /// <summary>
    /// This class is designed to be a base class for view models.
    /// </summary>
    public class ViewModel : IHaveActions, INotifyPropertyChanged, IModelStatus, IHaveViewInformation, IClosable, IOpenable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ViewModel"/> class.
        /// </summary>
        public ViewModel()
        {
            Actions = new ViewActionsCollection();
            Actions.CollectionChanged += (s, e) =>
                {
                    if (ActionChangeNotificationActive && ActionsChanged != null)
                        ActionsChanged(s, e);
                    else
                    {
                        _actionsChangedSinceLatNotification = true;
                        if (e.Action == NotifyCollectionChangedAction.Add || e.Action == NotifyCollectionChangedAction.Replace)
                        {
                            if (_newActions == null) _newActions = new List<IViewAction>();
                            foreach (var item in e.NewItems)
                                _newActions.Add(item as IViewAction);
                        }
                    }
                };
            ActionChangeNotificationActive = true;

            ModelStatus = ModelStatus.Unknown;
            OperationsInProgress = 0;
        }

        /// <summary>
        /// Can be used to indicate a property changed
        /// </summary>
        /// <param name="propertyName">Name of the changed property (or empty string to indicate a refresh of all properties)</param>
        protected virtual void NotifyChanged(string propertyName = "")
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool _actionsChangedSinceLatNotification;

        /// <summary>Defines whether the action changed event fires</summary>
        public bool ActionChangeNotificationActive
        {
            get { return _actionChangeNotificationActive; }
            set
            {
                _actionChangeNotificationActive = value;
                if (value && _actionsChangedSinceLatNotification && ActionsChanged != null)
                {
                    if (_newActions == null) _newActions = new List<IViewAction>();
                    ActionsChanged(Actions, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, _newActions));
                    _newActions.Clear();
                }
                _actionsChangedSinceLatNotification = false;
            }
        }

        private bool _actionChangeNotificationActive;
        private List<IViewAction> _newActions;

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>Causes the canExecute() method of all actions in the Actions collection to be re-evaluated</summary>
        protected void InvalidateAllActions()
        {
            foreach (var action in Actions)
                action.InvalidateCanExecute();
        }

        /// <summary>
        /// Collection of actions
        /// </summary>
        public ViewActionsCollection Actions { get; private set; }

        /// <summary>
        /// Fires when the list of actions changed (assuming change notification is active)
        /// </summary>
        public event NotifyCollectionChangedEventHandler ActionsChanged;

        /// <summary>
        /// Indicates the load status of the model
        /// </summary>
        public ModelStatus ModelStatus
        {
            get { return _modelStatus; }
            set
            {
                _modelStatus = value;
                NotifyChanged("ModelStatus");
            }
        }

        private ModelStatus _modelStatus;

        /// <summary>
        /// Indicates the number of operations of any kind currently in progress
        /// </summary>
        public int OperationsInProgress { get; set; }

        /// <summary>Reference to the associated view object</summary>
        public UIElement AssociatedView { get; set; }

        /// <summary>
        /// Occurs when the system is getting ready to close (has not started closing yet)
        /// </summary>
        public event EventHandler<CancelEventArgs> BeforeClosing;

        /// <summary>Occurs when the object is closing (has not closed yet)</summary>
        /// <remarks>The Closing event occurs when the view is closed using the controller or when themes support implicit view closing (which may or may not work for some views)</remarks>
        public event EventHandler Closing;

        /// <summary>Occurs when the object has closed (has finished closing)</summary>
        /// <remarks>The Close event occurs when the view is closed using the controller or when themes support implicit view closing (which may or may not work for some views)</remarks>
        public event EventHandler Closed;

        /// <summary>
        /// This method can be used to raise the before closing event
        /// </summary>
        /// <returns>True, if the closing operation has been canceled</returns>
        public bool RaiseBeforeClosingEvent()
        {
            var handler = BeforeClosing;
            if (handler != null)
            {
                var args = new CancelEventArgs();
                handler(this, args);
                return args.Cancel;
            }

            return false;
        }

        /// <summary>This method can be used to raise the closing event</summary>
        public void RaiseClosingEvent()
        {
            var handler = Closing;
            if (handler != null)
                handler(this, new EventArgs());
        }

        /// <summary>This method can be used to raise the closed event</summary>
        public void RaiseClosedEvent()
        {
            var handler = Closed;
            if (handler != null)
                handler(this, new EventArgs());
        }

        /// <summary>Occurs when the object is opening (has not opened yet)</summary>
        public event EventHandler Opening;

        /// <summary>Occurs when the object has opened (has finished opening)</summary>
        public event EventHandler Opened;

        /// <summary>This method can be used to raise the opening event</summary>
        public void RaiseOpeningEvent()
        {
            if (Opening != null)
                Opening(this, new EventArgs());
        }

        /// <summary>This method can be used to raise the open event</summary>
        public void RaiseOpenedEvent()
        {
            if (Opened != null)
                Opened(this, new EventArgs());
        }
    }
}
using System;
using System.Collections;
using System.Collections.Generic;

namespace CODE.Framework.Core.ComponentModel
{
    /// <summary>
    /// Basic definition of an interface that can be applied to
    /// a UI container (such as a form) that is intended to load data.
    /// </summary>
    public interface IDataHandler : IDisposable
    {
        /// <summary>
        /// Content load status
        /// </summary>
        ContentStatus ContentStatus { get; }
        /// <summary>
        /// This method gets called when the pane first loads.
        /// Parameters will be passed to this method.
        /// </summary>
        /// <param name="queryString">Name value collection of parameters</param>
        void InitialLoad(Dictionary<string, object> queryString);
        /// <summary>
        /// This method gets called when the pane first loads.
        /// Parameters will be passed to this method.
        /// </summary>
        /// <param name="mainEntityId">Primary Key of the entity that is to be loaded</param>
        void InitialLoad(Guid mainEntityId);
        /// <summary>
        /// This method gets called when the pane first loads.
        /// Parameters will be passed to this method.
        /// </summary>
        /// <param name="mainEntityId">Primary Key of the entity that is to be loaded</param>
        void InitialLoad(int mainEntityId);
        /// <summary>
        /// This method gets called when the pane first loads.
        /// Parameters will be passed to this method.
        /// </summary>
        /// <param name="mainEntityId">Primary Key of the entity that is to be loaded</param>
        void InitialLoad(string mainEntityId);
        /// <summary>
        /// Loads the form and indicates that the user intends to create a new item.
        /// </summary>
        void InitialNew();
        /// <summary>
        /// Loads the form and indicates that the user intends to create a new item.
        /// </summary>
        /// <param name="queryString">Name value collection of parameters</param>
        void InitialNew(Dictionary<string, object> queryString);
        /// <summary>
        /// This method is invoked whenever data needs to be loaded (including reloading data)
        /// </summary>
        object LoadContents();
        /// <summary>
        /// Called whenever secondary data needs to be loaded
        /// </summary>
        /// <returns>Data object</returns>
        object LoadSecondaryData();
        /// <summary>
        /// This method is invoked whenever the pane contents need to be presented to the user,
        /// after the contents have been loaded.
        /// This method is designed to be used internally only.
        /// </summary>
        void ShowContentsAuto();
        /// <summary>
        /// This method is invoked whenever the pane contents need to be presented to the user,
        /// after the contents have been loaded.
        /// </summary>
        void ShowContents();
        /// <summary>
        /// This method gets called whenever new content needs to be created (such as a new entity)
        /// </summary>
        object NewContents();
        /// <summary>
        /// This method gets called before any data loading starts. This provides the ability to
        /// change the UI before the data is loaded (such as showing a wait screen)
        /// </summary>
        void BeforeLoadContents();
        /// <summary>
        /// This method is called whenever an exception is raised during data loading.
        /// </summary>
        /// <param name="ex">Exception</param>
        /// <remarks>
        /// It is important to override this method and add code that accepts the error information
        /// and funnels it to the foreground thread (since exceptions that occur on background threads
        /// will not be visible on the foreground thread and thus not be obvious at all).
        /// </remarks>
        void HandleDataException(Exception ex);
        /// <summary>
        /// This method is used to present the data to the user by means of a dialog
        /// or something similar.
        /// </summary>
        /// <param name="ex">Exception</param>
        /// <remarks>
        /// Generally, this method is called by HandleDataException(ex).
        /// It is important to override this method, since otherwise, the exception
        /// information will not be displayed to the user.
        /// It is IS OK to override this method without calling the default behavior.
        /// </remarks>
        void ShowDataException(Exception ex);
        /// <summary>
        /// This event fires whenever a data exception (load, save, verify, new,...) is fired.
        /// </summary>
        /// <remarks>
        /// Generally, this event is fired by HandleDataException(ex).
        /// </remarks>
        event EventHandler<DataExceptionEventArgs> DataExceptionThrown;
    }

    /// <summary>
    /// Event arguments class for data exception events
    /// </summary>
    public class DataExceptionEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataExceptionEventArgs"/> class.
        /// </summary>
        /// <param name="ex">The ex.</param>
        public DataExceptionEventArgs(Exception ex)
        {
            Exception = ex;
        }

        /// <summary>
        /// Exception information
        /// </summary>
        /// <value>The exception.</value>
        public Exception Exception { get; set; }
    }

    /// <summary>
    /// Basic definition of an edit form interface
    /// </summary>
    public interface IDataEditHandler : IDataHandler
    {
        /// <summary>
        /// Basic save method
        /// </summary>
        /// <returns>True or False</returns>
        bool Save();
        /// <summary>
        /// Basic data verification method
        /// </summary>
        /// <returns>True or false</returns>
        bool Verify();
        /// <summary>
        /// Basic data deletion method
        /// </summary>
        /// <returns>True or false</returns>
        bool Delete();
    }

    /// <summary>
    /// Basic definition for a data list handler, such as a content pane.
    /// </summary>
    /// <remarks>This interface is not finalized at this point and will change!!!</remarks>
    public interface IDataListHandler : IDataHandler
    {
        /// <summary>
        /// Basic method to reload the current information and refresh the display
        /// </summary>
        void RefreshContents();
    }

    /// <summary>
    /// This interface defines data interactions
    /// </summary>
    public interface IDataEditInteractions
    {
        /// <summary>
        /// Triggers a data save operation
        /// </summary>
        void SaveData();

        /// <summary>
        /// Triggers data verification
        /// </summary>
        void VerifyData();

        /// <summary>
        /// Triggers data deletion
        /// </summary>
        void DeleteData();

        /// <summary>
        /// Triggers the creation of new data
        /// </summary>
        void NewData();

        /// <summary>
        /// Triggers an undo for the changes on the current data.
        /// </summary>
        void UndoData();
    }

    /// <summary>
    /// This interface defines all the events fired by data interfaces.
    /// These events are used for data handling based on events rather than inheritance.
    /// </summary>
    public interface IDataInterfaceEvents
    {
        /// <summary>
        /// Occurs when the system triggers a delete. This event can be used to handle the delete or cancel it.
        /// </summary>
        event EventHandler<DataEventArgs> HandleDelete;

        /// <summary>
        /// Occurs when the system triggers a save. This event can be used to handle the save or cancel it.
        /// </summary>
        event EventHandler<DataEventArgs> HandleSave;

        /// <summary>
        /// Occurs when the system triggers a verify. This event can be used to handle the verify or cancel it.
        /// </summary>
        event EventHandler<DataEventArgs> HandleVerify;

        /// <summary>
        /// Content status changed event
        /// </summary>
        event EventHandler<ContentStatusChangedEventArgs> ContentStatusChanged;

        /// <summary>
        /// Occurs when content needs to be loaded
        /// </summary>
        event EventHandler<DataLoaderEventArgs> HandleLoadContents;

        /// <summary>
        /// Occurs when content needs to be created
        /// </summary>
        event EventHandler<DataLoaderEventArgs> HandleNewContents;

        /// <summary>
        /// Occurs when secondary data needs to be loaded
        /// </summary>
        event EventHandler<DataLoaderEventArgs> HandleLoadSecondaryData;

        /// <summary>
        /// Occurs when on screen contents need to be refreshed.
        /// </summary>
        event EventHandler<ShowContentsEventArgs> HandleShowContents;

        /// <summary>
        /// Occurs when a save operation succeeds.
        /// </summary>
        event EventHandler<SavedEventArgs> SaveSucceeded;

        /// <summary>
        /// Occurs when a save operation fails.
        /// </summary>
        event EventHandler<SavedEventArgs> SaveFailed;
    }

    /// <summary>
    /// Event arguments used by data loading events
    /// </summary>
    public class DataLoaderEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataLoaderEventArgs"/> class.
        /// </summary>
        /// <param name="queryString">The query string.</param>
        /// <param name="dataLoader">The data loader.</param>
        public DataLoaderEventArgs(Dictionary<string, object> queryString, IEditDataLoader dataLoader)
        {
            QueryString = queryString;
            DataLoader = dataLoader;
        }

        /// <summary>
        /// Provides access to query string parameters
        /// </summary>
        /// <value>The query string parameter collection.</value>
        public Dictionary<string, object> QueryString { get; protected set; }

        /// <summary>
        /// Gets or sets the data loader.
        /// </summary>
        /// <value>The data loader.</value>
        public IEditDataLoader DataLoader { get; protected set; }

        /// <summary>
        /// The data returned to the loading object
        /// </summary>
        /// <value>The data.</value>
        /// <remarks>
        /// Data could be a single data object (such as a DataSet) or an enumerable list of objects
        /// </remarks>
        public virtual object Data { get; protected set; }

        /// <summary>
        /// Adds loaded data to the list of loaded data objects
        /// </summary>
        /// <param name="data">The data object that is to be added.</param>
        /// <remarks>
        /// The data object could be a single object or an enumerable list of objects.
        /// Data that may have been previously assigned as a result, either by the current
        /// event handled or another, will be preserved and the provided data object will \
        /// be added to the list.
        /// </remarks>
        /// <returns>True is fuccessful</returns>
        public virtual bool AddData(object data)
        {
            if (Data == null)
            {
                Data = data;
                return true;
            }
            // We already have data there, so we add to it
            var dataList = GetDataAsArrayList();
            var newData = data as IEnumerable;
            if (newData == null)
                dataList.Add(data);
            else
                foreach (var item in newData)
                    dataList.Add(item);
            return true;
        }

        /// <summary>
        /// Checks the current data object and makes sure the object is an array list.
        /// If it isn't an array list, it turns it into an array list.
        /// </summary>
        /// <returns>Data source as an enumerable object</returns>
        private ArrayList GetDataAsArrayList()
        {
            var dataList = Data as ArrayList;
            if (dataList != null)
                return dataList;
            // The object that was there previously is not a list, so we create a new list.
            var newList = new ArrayList {Data};
            Data = newList;
            return newList;
        }
    }

    /// <summary>
    /// Show content event arguments
    /// </summary>
    public class ShowContentsEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ShowContentsEventArgs"/> class.
        /// </summary>
        /// <param name="source">The event source.</param>
        /// <param name="dataLoader">The data loader.</param>
        public ShowContentsEventArgs(object source, IEditDataLoader dataLoader)
        {
            Source = source;
            DataLoader = dataLoader;
        }

        /// <summary>
        /// Gets or sets the event source.
        /// </summary>
        /// <value>The source.</value>
        public object Source { get; protected set; }

        /// <summary>
        /// Gets or sets the data loader.
        /// </summary>
        /// <value>The data loader.</value>
        public IEditDataLoader DataLoader { get; protected set; }
    }

    /// <summary>
    /// Interfaces used by secondary data loader objects
    /// </summary>
    public interface IEditDataLoader
    {
        /// <summary>
        /// Sets the host object used to load data
        /// </summary>
        /// <param name="host">Host</param>
        void SetHost(IDataEditHandler host);

        /// <summary>
        /// Saves all the current data
        /// </summary>
        void Save();

        /// <summary>
        /// Saves all the current data
        /// </summary>
        /// <param name="successCallback">Callback for success</param>
        /// <param name="failureCallback">Callback for failure</param>
        void Save(EventHandler<SavedEventArgs> successCallback, EventHandler<SavedEventArgs> failureCallback);

        /// <summary>
        /// Saved event
        /// </summary>
        event EventHandler<SavedEventArgs> Saved;

        /// <summary>
        /// Verifies the current data
        /// </summary>
        void Verify();

        /// <summary>
        /// Verifies the current data
        /// </summary>
        /// <param name="successCallback">Callback for success</param>
        /// <param name="failureCallback">Callback for failure</param>
        void Verify(EventHandler<VerifiedEventArgs> successCallback, EventHandler<VerifiedEventArgs> failureCallback);

        /// <summary>
        /// Verified event
        /// </summary>
        event EventHandler<VerifiedEventArgs> Verified;

        /// <summary>
        /// Content status
        /// </summary>
        ContentStatus Status { get; }

        /// <summary>
        /// Content status changed event
        /// </summary>
        event EventHandler<ContentStatusChangedEventArgs> ContentStatusChanged;

        /// <summary>
        /// Loads data
        /// </summary>
        /// <param name="parameters">Parameters collection</param>
        void Load(Dictionary<string, object> parameters);

        /// <summary>
        /// Loads data
        /// </summary>
        /// <param name="parameters">Parameters collection</param>
        /// <param name="loadPrimaryData">if set to <c>true</c> [load primary data].</param>
        /// <param name="loadSecondaryData">if set to <c>true</c> [load secondary data].</param>
        void Load(Dictionary<string, object> parameters, bool loadPrimaryData, bool loadSecondaryData);

        /// <summary>
        /// Loaded event
        /// </summary>
        event EventHandler<LoadedEventArgs> Loaded;

        /// <summary>
        /// Creates new data 
        /// </summary>
        /// <param name="parameters">Parameters collection</param>
        void New(Dictionary<string, object> parameters);

        /// <summary>
        /// Creates new data 
        /// </summary>
        /// <param name="parameters">Parameters collection</param>
        /// <param name="newPrimaryData">if set to <c>true</c> [new primary data].</param>
        /// <param name="loadSecondaryData">if set to <c>true</c> [load secondary data].</param>
        void New(Dictionary<string, object> parameters, bool newPrimaryData, bool loadSecondaryData);

        /// <summary>
        /// Newed event
        /// </summary>
        event EventHandler<NewedEventArgs> Newed;

        /// <summary>
        /// Deletes data
        /// </summary>
        void Delete();

        /// <summary>
        /// Deleted event
        /// </summary>
        event EventHandler<DeletedEventArgs> Deleted;

        /// <summary>
        /// Should data operations be performed multi threaded?
        /// </summary>
        /// <remarks>
        /// This property shall always default to true.
        /// </remarks>
        bool MultiThreaded { get; set; }

        /// <summary>
        /// Array list (collection) of primary data objects
        /// </summary>
        ArrayList PrimaryData { get; }

        /// <summary>
        /// Array list (collection) of secondary data objects
        /// </summary>
        ArrayList SecondaryData { get; }

        /// <summary>
        /// Retrieves the value of a parameter by name
        /// </summary>
        /// <param name="parameterName">Parameter name</param>
        /// <returns>Parameter value</returns>
        /// <typeparam name="TType">Expected return type</typeparam>
        TType GetParameterValue<TType>(string parameterName);

        /// <summary>
        /// Retrieves the value of a parameter by name
        /// </summary>
        /// <param name="parameterName">Parameter name</param>
        /// <returns>Parameter value</returns>
        object GetParameterValue(string parameterName);

        /// <summary>
        /// Parameters collection (thread-safe)
        /// </summary>
        Dictionary<string, object> Parameters { get; }

        /// <summary>
        /// Sets the parameter value (and adds the key to the collection if need be).
        /// </summary>
        /// <typeparam name="TType">The type of the type.</typeparam>
        /// <param name="parameterName">Name of the parameter.</param>
        /// <param name="value">The value.</param>
        void SetParameterValue<TType>(string parameterName, TType value);

        /// <summary>
        /// Returns true if the specified parameter exists
        /// </summary>
        /// <param name="parameterName">Name of the parameter.</param>
        /// <returns>True or false</returns>
        bool ParameterExists(string parameterName);

        /// <summary>
        /// Reference to the main data entity object
        /// </summary>
        /// <remarks>
        /// This object typically is the first object in the primary data collection
        /// </remarks>
        object MainDataEntity { get; }

        /// <summary>
        /// Sets the main entity type used for auto-loading of content.
        /// </summary>
        /// <typeparam name="TType">Data entity type</typeparam>
        /// <remarks>
        /// Any object can be set as the main data entity type. However, 
        /// to perform auto-data-handling, the object must have certain characteristics.
        /// 
        /// New data creation:
        /// The type must have a static NewEntity() method (no parameters).
        /// 
        /// Data loading:
        /// The type must have a static LoadEntity(Guid) method, -- and/or --
        /// The type must have a static LoadEntity(int) method, -- and/or --
        /// The type must have a static LoadEntity(string) method.
        /// 
        /// Data saving:
        /// The type must implement ISavable (and probably should implement IVerifyable)
        /// 
        /// Data verification:
        /// The type must implement IVerifyable
        /// 
        /// Data delition:
        /// The type must implement IDeletable
        /// </remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter")]
        void SetEntityType<TType>();

        /// <summary>
        /// Should the object perform auto news based on the entity type specified?
        /// </summary>
        /// <remarks>This property only has an impact after an entity type has been set by means of the SetEntityType() method</remarks>
        bool PerformAutoNew { get; set; }
        /// <summary>
        /// Should the object perform auto loads based on the entity type specified?
        /// </summary>
        /// <remarks>This property only has an impact after an entity type has been set by means of the SetEntityType() method</remarks>
        bool PerformAutoLoad { get; set; }
        /// <summary>
        /// Should the object perform auto saves based on the entity type specified?
        /// </summary>
        /// <remarks>This property only has an impact after an entity type has been set by means of the SetEntityType() method</remarks>
        bool PerformAutoSave { get; set; }
        /// <summary>
        /// Should the object perform auto verifies based on the entity type specified?
        /// </summary>
        /// <remarks>This property only has an impact after an entity type has been set by means of the SetEntityType() method</remarks>
        bool PerformAutoVerify { get; set; }
        /// <summary>
        /// Should the object perform auto deletes based on the entity type specified?
        /// </summary>
        /// <remarks>This property only has an impact after an entity type has been set by means of the SetEntityType() method</remarks>
        bool PerformAutoDelete { get; set; }

        /// <summary>
        /// Gets the data load controllers.
        /// </summary>
        /// <value>The data load controllers.</value>
        DataLoadControllerCollection DataLoadControllers { get; }
    }

    /// <summary>
    /// Standard deletable interface
    /// </summary>
    public interface IDeletable
    {
        /// <summary>
        /// Delete
        /// </summary>
        /// <returns>Success (true or false)</returns>
        bool Delete();
    }

    /// <summary>
    /// Standard verifyable interface
    /// </summary>
    public interface IVerifyable
    {
        /// <summary>
        /// Verify
        /// </summary>
        bool Verify();
    }

    /// <summary>
    /// Standard savable interface
    /// </summary>
    public interface ISavable
    {
        /// <summary>
        /// Save
        /// </summary>
        /// <returns>Success (true or false)</returns>
        bool Save();
    }

    /// <summary>
    /// Standard is-dirty interface (can be used to indicate whether data is dirty)
    /// </summary>
    public interface IDirty
    {
        /// <summary>
        /// Gets a value indicating whether this instance is dirty (has modified data).
        /// </summary>
        /// <value><c>true</c> if this instance is dirty; otherwise, <c>false</c>.</value>
        bool IsDirty { get; }
    }

    /// <summary>
    /// Event arguments for data operation events
    /// </summary>
    public class DataEventArgs : EventArgs
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="success">Was the operation successful?</param>
        public DataEventArgs(bool success)
        {
            Success = success;
        }

        /// <summary>
        /// Was the operation successful?
        /// </summary>
        public bool Success { get; protected set; }
    }

    /// <summary>
    /// Saved event arguments
    /// </summary>
    public class SavedEventArgs : DataEventArgs
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="success">Was the save successful?</param>
        public SavedEventArgs(bool success) : base(success) { }
    }

    /// <summary>
    /// Verified event arguments
    /// </summary>
    public class VerifiedEventArgs : DataEventArgs
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="success">Was the verify successful?</param>
        public VerifiedEventArgs(bool success) : base(success) { }
    }

    /// <summary>
    /// Loaded event arguments
    /// </summary>
    public class LoadedEventArgs : DataEventArgs
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="success">Was the load successful?</param>
        public LoadedEventArgs(bool success) : base(success) { }
    }

    /// <summary>
    /// Newed event arguments
    /// </summary>
    public class NewedEventArgs : DataEventArgs
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="success">Was the new successful?</param>
        public NewedEventArgs(bool success) : base(success) { }
    }

    /// <summary>
    /// Deleted event arguments
    /// </summary>
    public class DeletedEventArgs : DataEventArgs
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="success">Was the delete successful?</param>
        public DeletedEventArgs(bool success) : base(success) { }
    }

    /// <summary>
    /// Content status changed event arguments
    /// </summary>
    public class ContentStatusChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="status">New status</param>
        public ContentStatusChangedEventArgs(ContentStatus status)
        {
            NewStatus = status;
        }

        /// <summary>
        /// New content status
        /// </summary>
        public ContentStatus NewStatus { get; private set; }
    }

    /// <summary>
    /// Content load status of any given data loader
    /// </summary>
    public enum ContentStatus
    {
        /// <summary>
        /// Virgin (empty)
        /// </summary>
        Virgin,
        /// <summary>
        /// Data is being loaded
        /// </summary>
        Loading,
        /// <summary>
        /// Data has been loaded completely
        /// </summary>
        LoadComplete,
        /// <summary>
        /// Data display has been completed
        /// </summary>
        DisplayComplete
    }
}

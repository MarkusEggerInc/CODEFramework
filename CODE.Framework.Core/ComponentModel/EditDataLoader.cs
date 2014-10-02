using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Globalization;

namespace CODE.Framework.Core.ComponentModel
{
    /// <summary>
    /// Data loader object used to load
    /// </summary>
    public class EditDataLoader : IEditDataLoader, IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EditDataLoader"/> class.
        /// </summary>
        public EditDataLoader()
        {
            PerformAutoDelete = true;
            PerformAutoVerify = true;
            PerformAutoSave = true;
            PerformAutoLoad = true;
            PerformAutoNew = true;
            MultiThreaded = true;
        }

        /// <summary>
        /// Internal reference for the data load controllers
        /// </summary>
        private DataLoadControllerCollection _dataLoadControllers;
        /// <summary>
        /// Collection of registered data load controllers
        /// </summary>
        /// <value>The data load controllers.</value>
        public DataLoadControllerCollection DataLoadControllers
        {
            get { return _dataLoadControllers ?? (_dataLoadControllers = new DataLoadControllerCollection(this)); }
        }

        /// <summary>
        /// Loader host
        /// </summary>
        protected virtual IDataEditHandler Host { get; set; }

        /// <summary>
        /// For internal use only
        /// </summary>
        private ArrayList _primaryData;
        /// <summary>
        /// Array list (collection) of primary data objects
        /// </summary>
        public virtual ArrayList PrimaryData
        {
            get { return _primaryData ?? (_primaryData = new ArrayList()); }
        }

        /// <summary>
        /// Reference to the main data entity object
        /// </summary>
        /// <remarks>
        /// This object is the first object in the primary 
        /// data collection (if available)
        /// </remarks>
        public object MainDataEntity
        {
            get
            {
                if (PrimaryData.Count > 0)
                    return PrimaryData[0];
                return null;
            }
        }

        /// <summary>
        /// For internal use only
        /// </summary>
        private ArrayList _secondaryData;
        /// <summary>
        /// Array list (collection) of secondary data objects
        /// </summary>
        public virtual ArrayList SecondaryData
        {
            get { return _secondaryData ?? (_secondaryData = new ArrayList()); }
        }

        /// <summary>
        /// Should data operations be performed multi threaded?
        /// </summary>
        public bool MultiThreaded { get; set; }

        /// <summary>
        /// Should the object perform auto news based on the entity type specified?
        /// </summary>
        /// <remarks>This property only has an impact after an entity type has been set by means of the SetEntityType() method</remarks>
        public bool PerformAutoNew { get; set; }

        /// <summary>
        /// Should the object perform auto loads based on the entity type specified?
        /// </summary>
        /// <remarks>This property only has an impact after an entity type has been set by means of the SetEntityType() method</remarks>
        public bool PerformAutoLoad { get; set; }

        /// <summary>
        /// Should the object perform auto saves based on the entity type specified?
        /// </summary>
        /// <remarks>This property only has an impact after an entity type has been set by means of the SetEntityType() method</remarks>
        public bool PerformAutoSave { get; set; }

        /// <summary>
        /// Should the object perform auto verifies based on the entity type specified?
        /// </summary>
        /// <remarks>This property only has an impact after an entity type has been set by means of the SetEntityType() method</remarks>
        public bool PerformAutoVerify { get; set; }

        /// <summary>
        /// Should the object perform auto deletes based on the entity type specified?
        /// </summary>
        /// <remarks>This property only has an impact after an entity type has been set by means of the SetEntityType() method</remarks>
        public bool PerformAutoDelete { get; set; }

        /// <summary>
        /// Sets the host that is used to load data
        /// </summary>
        /// <param name="host">Host</param>
        public virtual void SetHost(IDataEditHandler host)
        {
            Host = host;
        }

        /// <summary>
        /// For internal use only
        /// </summary>
        private ContentStatus _status = ContentStatus.Virgin;
        /// <summary>
        /// Status
        /// </summary>
        public virtual ContentStatus Status
        {
            get { return _status; }
            protected set
            {
                _status = value;
                if (ContentStatusChanged != null)
                    ContentStatusChanged(this, new ContentStatusChangedEventArgs(value));
            }
        }

        /// <summary>
        /// Content status changed event
        /// </summary>
        public event EventHandler<ContentStatusChangedEventArgs> ContentStatusChanged;

        /// <summary>
        /// For internal use only
        /// </summary>
        private Dictionary<string, object> _parameters;
        /// <summary>
        /// Lock dummy
        /// </summary>
        [Browsable(false)]
        private readonly string _parametersLock = string.Empty;

        /// <summary>
        /// Retrieves the value of a parameter by name
        /// </summary>
        /// <param name="parameterName">Parameter name</param>
        /// <returns>Parameter value</returns>
        /// <typeparam name="TType">Expected return type</typeparam>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter")]
        public TType GetParameterValue<TType>(string parameterName)
        {
            lock (_parametersLock)
                if (_parameters != null)
                    if (_parameters.ContainsKey(parameterName))
                    {
                        if (_parameters[parameterName] is TType)
                            return (TType)_parameters[parameterName];
                        if (_parameters[parameterName] is string)
                        {
                            // This is a string, so we can try to parse it into 
                            // the expected return type
                            if (typeof (TType) == typeof (Guid))
                                try
                                {
                                    return (TType)(object)(new Guid(_parameters[parameterName].ToString()));
                                }
                                catch
                                {
                                    return default(TType);
                                }
                            if (typeof (TType) == typeof (int))
                                try
                                {
                                    return (TType)(object)int.Parse(_parameters[parameterName].ToString());
                                }
                                catch
                                {
                                    return default(TType);
                                }
                            if (typeof (TType) == typeof (bool))
                                try
                                {
                                    return (TType)(object)bool.Parse(_parameters[parameterName].ToString());
                                }
                                catch
                                {
                                    return default(TType);
                                }
                            if (typeof (TType) == typeof (DateTime))
                                try
                                {
                                    return (TType)(object)DateTime.Parse(_parameters[parameterName].ToString());
                                }
                                catch
                                {
                                    return default(TType);
                                }
                            return default(TType);
                        }
                        return default(TType);
                    }
            return default(TType);
        }

        /// <summary>
        /// Retrieves the value of a parameter by name
        /// </summary>
        /// <param name="parameterName">Parameter name</param>
        /// <returns>Parameter value</returns>
        public object GetParameterValue(string parameterName)
        {
            lock (_parametersLock)
                if (_parameters != null)
                    if (_parameters.ContainsKey(parameterName))
                        return _parameters[parameterName];
            return null;
        }

        /// <summary>
        /// Parameters collection (thread-safe)
        /// </summary>
        /// <remarks>
        /// The dictionary returned by this property is made thread-safe by means of cloning.
        /// This means that every time this collection is accessed, a copy of the original (internal)
        /// collection is created. If you then interact with the collection, it will not update the
        /// original parameters. Therefore, most interaction with this collection should
        /// not happen through this property, but by means of the SetParameterValue()/GetParameterValue() 
        /// methods instead.
        /// </remarks>
        public Dictionary<string, object> Parameters
        {
            get
            {
                Dictionary<string, object> safeDictionary;
                lock (_parametersLock)
                    safeDictionary = CloneDictionary(_parameters);
                return safeDictionary;
            }
        }

        /// <summary>
        /// Sets the parameter value (and adds the key to the collection if need be).
        /// </summary>
        /// <typeparam name="TType">The type of the type.</typeparam>
        /// <param name="parameterName">Name of the parameter.</param>
        /// <param name="value">The value.</param>
        public void SetParameterValue<TType>(string parameterName, TType value)
        {
            lock (_parametersLock)
                if (_parameters != null)
                    if (!_parameters.ContainsKey(parameterName))
                        _parameters.Add(parameterName, value);
                    else
                        _parameters[parameterName] = value;
        }

        /// <summary>
        /// Returns true if the specified parameter exists
        /// </summary>
        /// <param name="parameterName">Name of the parameter.</param>
        /// <returns>True or false</returns>
        public bool ParameterExists(string parameterName)
        {
            bool retVal = false;
            lock (_parametersLock)
                if (_parameters != null)
                    retVal = _parameters.ContainsKey(parameterName);
            return retVal;
        }

        /// <summary>
        /// For internal use only
        /// </summary>
        private BackgroundWorker _saveWorker;
        /// <summary>
        /// Background worker used to save data
        /// </summary>
        protected virtual BackgroundWorker SaveWorker
        {
            get
            {
                if (_saveWorker == null)
                {
                    _saveWorker = new BackgroundWorker();
                    _saveWorker.DoWork += WorkSaveContents;
                    _saveWorker.RunWorkerCompleted += WorkSaveContentsCompleted;
                }
                return _saveWorker;
            }
        }
        /// <summary>
        /// Returns an instance of an available (non-busy) save worker
        /// </summary>
        protected virtual BackgroundWorker AvailableSaveWorker
        {
            get
            {
                var worker = SaveWorker;
                while (worker.IsBusy)
                {
                    // We wait
                }
                return worker;
            }
        }

        /// <summary>
        /// Save
        /// </summary>
        public virtual void Save()
        {
            Save(null, null);
        }

        /// <summary>
        /// Saves all the current data
        /// </summary>
        /// <param name="successCallback">Callback for success</param>
        /// <param name="failureCallback">Callback for failure</param>
        public virtual void Save(EventHandler<SavedEventArgs> successCallback, EventHandler<SavedEventArgs> failureCallback)
        {
            if (MultiThreaded)
            {
                var callbacks = new SaveCallbacks(successCallback, failureCallback);
                AvailableSaveWorker.RunWorkerAsync(callbacks);
            }
            else
            {
                bool result;
                try
                {
                    result = AutoSave();
                    if (result && _dataLoadControllers != null)
                        if (DataLoadControllers.Any(controller => !controller.AutoSave()))
                            result = false;
                    if (result)
                        result = Host.Save();
                }
                catch (Exception ex)
                {
                    CommunicateExceptionToForegroundThread(ex);
                    result = false;
                }
                if (Saved != null) Saved(this, new SavedEventArgs(result));
                if (result && successCallback != null) successCallback(this, new SavedEventArgs(true));
                if (!result && failureCallback != null) failureCallback(this, new SavedEventArgs(false));
            }
        }

        /// <summary>
        /// Performs an auto-save on the current data entity
        /// </summary>
        /// <returns>True or false</returns>
        protected virtual bool AutoSave()
        {
            object dataEntity = MainDataEntity;
            if (!PerformAutoSave || dataEntity == null)
                // We can not consider this failed, so we return true
                return true;
            var savableDataEntity = dataEntity as ISavable;
            if (savableDataEntity != null)
                // This is a savable entity, so we have at it...
                return savableDataEntity.Save();
            // We can not save this, but that doesn't mean that the
            // save for the form failed, so we still return true.
            // After all, the entity never claims it is savable this way,
            // since it does not implement ISavable
            return true;
        }

        /// <summary>
        /// Lock dummy
        /// </summary>
        [Browsable(false)]
        private readonly string _saveResultsLocker = string.Empty;
        /// <summary>
        /// For internal use only
        /// </summary>
        private bool _saveResult;

        /// <summary>
        /// Triggers the save contents processing
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event args</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "This method is specifically designed to catch all exceptions so they can be funnelled to the foreground thread and be re-thrown there.")]
        private void WorkSaveContents(object sender, DoWorkEventArgs e)
        {
            bool result;
            try
            {
                result = AutoSave();
                if (result)
                    result = Host.Save();
            }
            catch (Exception ex)
            {
                CommunicateExceptionToForegroundThread(ex);
                result = false;
            }
            lock (_saveResultsLocker)
            {
                _saveResult = result;
            }
            // We cheat and use the results to funnel the arguments
            // (callbacks) to the complete method.
            e.Result = e.Argument;
        }

        /// <summary>
        /// Event handler for completion of save contents
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event arguments</param>
        private void WorkSaveContentsCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            bool result;
            lock (_saveResultsLocker)
                result = _saveResult;
            if (Saved != null)
                Saved(this, new SavedEventArgs(result));
            var callbacks = e.Result as SaveCallbacks;
            if (callbacks != null)
            {
                if (result && callbacks.SuccessCallback != null) callbacks.SuccessCallback(this, new SavedEventArgs(true));
                if (!result && callbacks.FailureCallback != null) callbacks.FailureCallback(this, new SavedEventArgs(false));
            }
        }

        /// <summary>
        /// Saved event
        /// </summary>
        public event EventHandler<SavedEventArgs> Saved;

        /// <summary>
        /// For internal use only
        /// </summary>
        private BackgroundWorker _verifyWorker;
        /// <summary>
        /// Background worker used to verify data
        /// </summary>
        protected virtual BackgroundWorker VerifyWorker
        {
            get
            {
                if (_verifyWorker == null)
                {
                    _verifyWorker = new BackgroundWorker();
                    _verifyWorker.DoWork += WorkVerifyContents;
                    _verifyWorker.RunWorkerCompleted += WorkVerifyContentsCompleted;
                }
                return _verifyWorker;
            }
        }
        /// <summary>
        /// Returns an instance of an available (non-busy) verify worker
        /// </summary>
        protected virtual BackgroundWorker AvailableVerifyWorker
        {
            get
            {
                var worker = VerifyWorker;
                while (worker.IsBusy)
                {
                    // We wait
                }
                return worker;
            }
        }

        /// <summary>
        /// Verifies the current data
        /// </summary>
        /// <param name="successCallback">Callback for success</param>
        /// <param name="failureCallback">Callback for failure</param>
        public void Verify(EventHandler<VerifiedEventArgs> successCallback, EventHandler<VerifiedEventArgs> failureCallback)
        {
            if (MultiThreaded)
            {
                // We trigger background verification
                var callbacks = new VerifyCallbacks(successCallback, failureCallback);
                AvailableVerifyWorker.RunWorkerAsync(callbacks);
            }
            else
            {
                // Since we do this on a single thread, we can simply launch the
                // verify method and then call the event which reports on the result
                // We even do this if nobody is listening to the event,
                // since someone could check the result later in some other fashion.
                bool result = false;
                try
                {
                    result = AutoVerify();
                    if (result && _dataLoadControllers != null)
                        if (DataLoadControllers.Any(controller => !controller.AutoVerify()))
                            result = false;
                }
                catch (Exception ex)
                {
                    CommunicateExceptionToForegroundThread(ex);
                }
                if (result)
                    try
                    {
                        result = Host.Verify();
                    }
                    catch (Exception ex)
                    {
                        CommunicateExceptionToForegroundThread(ex);
                        result = false;
                    }
                if (Verified != null) Verified(this, new VerifiedEventArgs(result));
                if (result && successCallback != null) successCallback(this, new VerifiedEventArgs(true));
                if (!result && failureCallback != null) failureCallback(this, new VerifiedEventArgs(false));
            }
        }

        /// <summary>
        /// Verify
        /// </summary>
        public virtual void Verify()
        {
            Verify(null, null);
        }

        /// <summary>
        /// Performs an auto-verification on the current data entity
        /// </summary>
        /// <returns>True or false</returns>
        protected virtual bool AutoVerify()
        {
            object dataEntity = MainDataEntity;
            if (!PerformAutoVerify || dataEntity == null)
                // We can not consider this failed, so we return true
                return true;
            var verifyableDataEntity = dataEntity as IVerifyable;
            if (verifyableDataEntity != null)
                // This is a verifyable entity, so we have at it...
                return verifyableDataEntity.Verify();
            // We can not verify this, but that doesn't mean that the
            // verify for the form failed, so we still return true.
            // After all, the entity never claims it is verifyable this way,
            // since it does not implement IVerifyable
            return true;
        }

        /// <summary>
        /// Lock dummy
        /// </summary>
        [Browsable(false)]
        private readonly string _verifyResultsLocker = string.Empty;
        /// <summary>
        /// For internal use only
        /// </summary>
        private bool _verifyResult;

        /// <summary>
        /// Triggers the verify contents processing
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event args</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "This method is specifically designed to catch all exceptions so they can be funnelled to the foreground thread and be re-thrown there.")]
        private void WorkVerifyContents(object sender, DoWorkEventArgs e)
        {
            // Since we do this on a single thread, we can simply launch the
            // verify method and then call the event which reports on the result
            // We even do this if nobody is listening to the event,
            // since someone could check the result later in some other fashion.
            bool result = false;
            try
            {
                result = AutoVerify();
            }
            catch (Exception ex)
            {
                CommunicateExceptionToForegroundThread(ex);
            }
            if (result)
                try
                {
                    result = Host.Verify();
                }
                catch (Exception ex)
                {
                    CommunicateExceptionToForegroundThread(ex);
                    result = false;
                }
            lock (_verifyResultsLocker)
                _verifyResult = result;
            // We cheat and use the results to funnel the arguments
            // (callbacks) to the complete method.
            e.Result = e.Argument;
        }

        /// <summary>
        /// Event handler for completion of verify contents
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event arguments</param>
        private void WorkVerifyContentsCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            bool result;
            lock (_verifyResultsLocker)
                result = _verifyResult;
            if (Verified != null)
                Verified(this, new VerifiedEventArgs(result));
            var callbacks = e.Result as VerifyCallbacks;
            if (callbacks != null)
            {
                if (result && callbacks.SuccessCallback != null) callbacks.SuccessCallback(this, new VerifiedEventArgs(true));
                if (!result && callbacks.FailureCallback != null) callbacks.FailureCallback(this, new VerifiedEventArgs(false));
            }
        }

        /// <summary>
        /// Verified event
        /// </summary>
        public event EventHandler<VerifiedEventArgs> Verified;

        /// <summary>
        /// For internal use only
        /// </summary>
        private BackgroundWorker _loadWorker;
        /// <summary>
        /// Background worker used to load existing contents
        /// </summary>
        protected virtual BackgroundWorker LoadWorker
        {
            get
            {
                if (_loadWorker == null)
                {
                    _loadWorker = new BackgroundWorker();
                    _loadWorker.DoWork += WorkLoadContents;
                }
                return _loadWorker;
            }
        }
        /// <summary>
        /// Returns an instance of an available (non-busy) load worker
        /// </summary>
        protected virtual BackgroundWorker AvailableLoadWorker
        {
            get
            {
                var worker = LoadWorker;
                while (worker.IsBusy)
                {
                    // We wait
                }
                return worker;
            }
        }

        /// <summary>
        /// Triggers the loading operation.
        /// </summary>
        /// <param name="parameters">Parameters collection</param>
        public virtual void Load(Dictionary<string, object> parameters)
        {
            Load(parameters, true, true);
        }

        /// <summary>
        /// Triggers the loading operation.
        /// </summary>
        /// <param name="parameters">Parameters collection</param>
        /// <param name="loadPrimaryData">if set to <c>true</c> [load primary data].</param>
        /// <param name="loadSecondaryData">if set to <c>true</c> [load secondary data].</param>
        public virtual void Load(Dictionary<string, object> parameters, bool loadPrimaryData, bool loadSecondaryData)
        {
            lock (_parametersLock)
                // We create our own copy of the collection, so we can 
                // do whatever we want on background threads
                _parameters = CloneDictionary(parameters);

            Status = ContentStatus.Virgin;
            try
            {
                Host.BeforeLoadContents();
            }
            catch (Exception ex)
            {
                CommunicateExceptionToForegroundThread(ex);
            }
            Status = ContentStatus.Loading;

            if (MultiThreaded)
                HandleMultiThreadedLoad(loadPrimaryData, loadSecondaryData);
            else
                HandleSingleThreadedLoad(loadPrimaryData, loadSecondaryData);
        }

        /// <summary>
        /// Handles the load operation in multi-threaded scenario.
        /// If loadSecondaryData is true, the secondary data gets triggered only after
        /// the primary data has been retrieved.
        /// The Loaded event is raised after all loading operations have been finished.
        /// </summary>
        /// <param name="loadPrimaryData">if set to <c>true</c> [load primary data].</param>
        /// <param name="loadSecondaryData">if set to <c>true</c> [load secondary data].</param>
        private void HandleMultiThreadedLoad(bool loadPrimaryData, bool loadSecondaryData)
        {
            if (loadPrimaryData)
            {
                if (loadSecondaryData)
                {
                    RegisterLoadSupportDataToRunAfterLoadPrimaryData(true);
                    RegisterForRaisingLoadedEventAfterLoadSupportData(true);
                }
                else
                    RegisterForRaisingLoadedEventAfterLoadPrimaryData(true);
                AvailableLoadWorker.RunWorkerAsync();
            }
            else
            {
                if (loadSecondaryData)
                {
                    RegisterForRaisingLoadedEventAfterLoadSupportData(true);
                    AvailableLoadSupportWorker.RunWorkerAsync();
                }
            }
        }

        /// <summary>
        /// Registers for raising Loaded event after load support data.
        /// </summary>
        /// <param name="register">if set to <c>true</c> [register].</param>
        private void RegisterForRaisingLoadedEventAfterLoadSupportData(bool register)
        {
            RunWorkerCompletedEventHandler handler = (sender, e) => TriggerLoaded();

            // Right after all data has been loaded, we make sure to 
            // unregister some handlers, in order for them to not keep stacking up.
            EventHandler<LoadedEventArgs> loadedHandler = (sender, args) =>
                                                              {
                                                                  RegisterLoadSupportDataToRunAfterLoadPrimaryData(false);
                                                                  RegisterForRaisingLoadedEventAfterLoadSupportData(false);
                                                              };

            if (register)
            {
                AvailableLoadSupportWorker.RunWorkerCompleted += handler;
                Loaded += loadedHandler;
            }
            else
            {
                AvailableLoadSupportWorker.RunWorkerCompleted -= handler;
                Loaded -= loadedHandler;
            }
        }

        /// <summary>
        /// Registers for raising loaded event after load primary data.
        /// </summary>
        /// <param name="register">if set to <c>true</c> [register].</param>
        private void RegisterForRaisingLoadedEventAfterLoadPrimaryData(bool register)
        {
            RunWorkerCompletedEventHandler handler = (sender, e) => TriggerLoaded();

            EventHandler<LoadedEventArgs> loadedHandler = (sender, e) => RegisterForRaisingLoadedEventAfterLoadPrimaryData(false);

            if (register)
            {
                AvailableLoadWorker.RunWorkerCompleted += handler;
                Loaded += loadedHandler;
            }
            else
            {
                AvailableLoadWorker.RunWorkerCompleted -= handler;
                Loaded -= loadedHandler;
            }
        }

        /// <summary>
        /// Registers the load support data to run after load primary data has finished.
        /// </summary>
        /// <param name="register">if set to <c>true</c> support data is loaded after primary data is done loading.
        /// If set to false, it unregisters the execution, in case it's been registered before.</param>
        private void RegisterLoadSupportDataToRunAfterLoadPrimaryData(bool register)
        {
            RunWorkerCompletedEventHandler handler = (sender, e) => AvailableLoadSupportWorker.RunWorkerAsync();

            if (register)
                AvailableLoadWorker.RunWorkerCompleted += handler;
            else
                AvailableLoadWorker.RunWorkerCompleted -= handler;
        }

        /// <summary>
        /// Handles the load operation in single-threaded scenario.
        /// If loadSecondaryData is true, the secondary data gets triggered only after
        /// the primary data has been retrieved.
        /// The Loaded event is raised after all loading operations have been finished.
        /// </summary>
        /// <param name="loadPrimaryData">if set to <c>true</c> [load primary data].</param>
        /// <param name="loadSecondaryData">if set to <c>true</c> [load secondary data].</param>
        private void HandleSingleThreadedLoad(bool loadPrimaryData, bool loadSecondaryData)
        {
            try
            {
                if (loadPrimaryData)
                    RunLoadContents();
                if (loadSecondaryData)
                    RunLoadSupportData();
                if (loadPrimaryData || loadSecondaryData)
                    TriggerLoaded();
            }
            catch (Exception ex)
            {
                CommunicateExceptionToForegroundThread(ex);
            }
            try
            {
                Host.ShowContentsAuto();
                Host.ShowContents();
            }
            catch (Exception ex)
            {
                CommunicateExceptionToForegroundThread(ex);
            }
            Status = ContentStatus.DisplayComplete;
        }

        /// <summary>
        /// Triggers the loaded event
        /// </summary>
        protected virtual void TriggerLoaded()
        {
            Status = ContentStatus.LoadComplete;
            if (Loaded != null)
                Loaded(this, new LoadedEventArgs(true));
        }

        /// <summary>
        /// Triggers the load contents processing
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event args</param>
        private void WorkLoadContents(object sender, DoWorkEventArgs e)
        {
            RunLoadContents();
        }

        /// <summary>
        /// Performs the load-contents work
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "This method is specifically designed to catch all exceptions so they can be funnelled to the foreground thread and be re-thrown there.")]
        protected virtual void RunLoadContents()
        {
            object dataAuto = null;
            object dataManual;
            var dataFromControllers = new ArrayList();
            try
            {
                dataAuto = AutoLoad();
                dataManual = Host.LoadContents();
                if (_dataLoadControllers != null)
                    foreach (var controller in DataLoadControllers)
                    {
                        dataFromControllers.Add(controller.AutoLoad());
                        dataFromControllers.Add(controller.LoadContents());
                    }
            }
            catch (Exception ex)
            {
                CommunicateExceptionToForegroundThread(ex);
                dataManual = null;
            }
            object data = MergeDataSources(dataAuto, dataManual);
            if (dataFromControllers.Count > 0)
                foreach (object dataFromController in dataFromControllers)
                    data = MergeDataSources(data, dataFromController);
            PopulatePrimaryDataList(data);
        }

        /// <summary>
        /// For internal use only (cache)
        /// </summary>
        private MethodInfo _loadDataMethod;

        /// <summary>
        /// Performs an automatic load operation, if possible
        /// </summary>
        /// <returns>Data object or data object list</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "The explicit purpose of this is to catch all exceptions.")]
        protected virtual object AutoLoad()
        {
            if (!PerformAutoLoad)
                return null;

            // Maybe we have a shortcut already
            if (_loadDataMethod != null)
            {
                var loadParas = new[] { GetParameterValue("key") };
                var paras = _loadDataMethod.GetParameters();
                if (paras.Length == 1) // If the length is not 1, we have another problem, and we just let it fall into the error that will be raised next
                {
                    var parameterType = paras[0].ParameterType;
                    if (parameterType == typeof(int) && loadParas[0] is string)
                    {
                        int value;
                        loadParas[0] = int.TryParse(loadParas[0].ToString(), out value) ? value : -1;
                    }
                    if (parameterType == typeof(Guid) && loadParas[0] is string)
                    {
                        Guid value;
                        loadParas[0] = Guid.TryParse(loadParas[0].ToString(), out value) ? value : Guid.Empty;
                    }
                }
                return _loadDataMethod.Invoke(null, loadParas);
            }

            if (_entityType == null)
                // Nothing to see here... move along...
                return null;

            var entityType = _entityType;
            var methods = entityType.GetMethods(BindingFlags.Static | BindingFlags.Public);
            foreach (var method in methods)
                if (method.Name == "LoadEntity")
                {
                    var paras = method.GetParameters();
                    if (paras.Length == 1)
                    {
                        object key = GetParameterValue("key");
                        var keyString = key as string;

                        if (paras[0].ParameterType == typeof(Guid))
                        {
                            bool goodToGo = false;
                            var key2 = Guid.Empty;
                            if (key is Guid)
                            {
                                key2 = (Guid)key;
                                goodToGo = true;
                            }
                            else
                            {
                                if (keyString != null)
                                    try
                                    {
                                        key2 = new Guid(keyString);
                                        goodToGo = true;
                                    }
                                    catch { } // not a guid in string format
                            }
                            if (goodToGo)
                            {
                                // This is the one we were looking for
                                _loadDataMethod = method;
                                var loadParas = new object[] {key2};
                                return _loadDataMethod.Invoke(null, loadParas);
                            }
                        }

                        if (paras[0].ParameterType == typeof(int))
                        {
                            bool goodToGo = false;
                            int key2 = -1;
                            if (key is int)
                            {
                                key2 = (int)key;
                                goodToGo = true;
                            }
                            else
                            {
                                if (keyString != null)
                                    try
                                    {
                                        key2 = int.Parse(keyString, CultureInfo.InvariantCulture);
                                        goodToGo = true;
                                    }
                                    catch { } // not an int in string format
                            }
                            if (goodToGo)
                            {
                                // This is the one we were looking for
                                _loadDataMethod = method;
                                var loadParas = new[] {(object)key2};
                                return _loadDataMethod.Invoke(null, loadParas);
                            }
                        }

                        // At this point, we pretty much assume we have a string
                        if (paras[0].ParameterType == typeof (string))
                        {
                            string key2 = keyString;
                            // This is the one we were looking for
                            _loadDataMethod = method;
                            var loadParas = new object[] {key2};
                            return _loadDataMethod.Invoke(null, loadParas);
                        }
                    }
                }

            return null;
        }

        /// <summary>
        /// Loaded event
        /// </summary>
        public event EventHandler<LoadedEventArgs> Loaded;

        /// <summary>
        /// For internal use only
        /// </summary>
        private BackgroundWorker _newWorker;
        /// <summary>
        /// Background worker used to load new contents
        /// </summary>
        protected virtual BackgroundWorker NewWorker
        {
            get
            {
                if (_newWorker == null)
                {
                    _newWorker = new BackgroundWorker();
                    _newWorker.DoWork += WorkNewContents;
                    _newWorker.RunWorkerCompleted += WorkNewContentsCompleted;
                }
                return _newWorker;
            }
        }
        /// <summary>
        /// Returns an instance of an available (non-busy) new worker
        /// </summary>
        protected virtual BackgroundWorker AvailableNewWorker
        {
            get
            {
                var worker = NewWorker;
                while (worker.IsBusy)
                {
                    // We wait
                }
                return worker;
            }
        }

        /// <summary>
        /// New
        /// </summary>
        /// <param name="parameters">Parameters collection</param>
        public virtual void New(Dictionary<string, object> parameters)
        {
            New(parameters, true, true);
        }

        /// <summary>
        /// New
        /// </summary>
        /// <param name="parameters">Parameters collection</param>
        /// <param name="newPrimaryData">if set to <c>true</c> [new primary data].</param>
        /// <param name="loadSecondaryData">if set to <c>true</c> [load secondary data].</param>
        public virtual void New(Dictionary<string, object> parameters, bool newPrimaryData, bool loadSecondaryData)
        {
            lock (_parametersLock)
                // We create our own copy of the collection, so we can 
                // do whatever we want on background threads
                _parameters = CloneDictionary(parameters);

            Status = ContentStatus.Virgin;
            try
            {
                Host.BeforeLoadContents();
            }
            catch (Exception ex)
            {
                CommunicateExceptionToForegroundThread(ex);
            }
            Status = ContentStatus.Loading;
            if (MultiThreaded)
                AvailableNewWorker.RunWorkerAsync();
            else
            {
                if (newPrimaryData)
                {
                    RunNewContents();
                    TriggerNewed();
                }
                if (loadSecondaryData)
                    RunLoadSupportData();
                try
                {
                    Host.ShowContentsAuto();
                    Host.ShowContents();
                }
                catch (Exception ex)
                {
                    CommunicateExceptionToForegroundThread(ex);
                }
                Status = ContentStatus.DisplayComplete;
            }
        }

        /// <summary>
        /// Triggers the newed event
        /// </summary>
        protected virtual void TriggerNewed()
        {
            Status = ContentStatus.LoadComplete;
            if (Newed != null)
                Newed(this, new NewedEventArgs(true));
        }

        /// <summary>
        /// Triggers the new contents processing
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event args</param>
        private void WorkNewContents(object sender, DoWorkEventArgs e)
        {
            RunNewContents();
        }

        /// <summary>
        /// Performs the new-contents work
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "This method is specifically designed to catch all exceptions so they can be funnelled to the foreground thread and be re-thrown there.")]
        protected virtual void RunNewContents()
        {
            object dataAuto = null;
            object dataManual = null;
            var dataFromControllers = new ArrayList();
            try
            {
                dataAuto = AutoNew();
                dataManual = Host.NewContents();
                if (_dataLoadControllers != null)
                    foreach (var controller in DataLoadControllers)
                    {
                        dataFromControllers.Add(controller.AutoNew());
                        dataFromControllers.Add(controller.NewContents());
                    }
            }
            catch (Exception ex)
            {
                CommunicateExceptionToForegroundThread(ex);
            }
            object data = MergeDataSources(dataAuto, dataManual);
            if (dataFromControllers.Count > 0)
                foreach (object dataFromController in dataFromControllers)
                    data = MergeDataSources(data, dataFromController);
            PopulatePrimaryDataList(data);
        }

        /// <summary>
        /// For internal use only (cache)
        /// </summary>
        private MethodInfo _newDataMethod;

        /// <summary>
        /// Performs an automatic new operation, if possible
        /// </summary>
        /// <returns>Data object or data object list</returns>
        protected virtual object AutoNew()
        {
            if (!PerformAutoNew)
                return null;

            // Maybe we have a shortcut already
            if (_newDataMethod != null)
                return _newDataMethod.Invoke(null, null);

            if (_entityType == null)
                // Nothing to see here... move along...
                return null;

            var entityType = _entityType;
            var methods = entityType.GetMethods(BindingFlags.Static | BindingFlags.Public);
            foreach (var method in methods)
                if (method.Name == "NewEntity")
                    if (method.GetParameters().Length == 0)
                    {
                        // This is the one we were looking for
                        _newDataMethod = method;
                        return _newDataMethod.Invoke(null, null);
                    }

            return null;
        }

        /// <summary>
        /// Event handler for completion of new contents
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event arguments</param>
        private void WorkNewContentsCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            TriggerNewed();
            AvailableLoadSupportWorker.RunWorkerAsync();
        }

        /// <summary>
        /// Newed event
        /// </summary>
        public event EventHandler<NewedEventArgs> Newed;

        /// <summary>
        /// For internal use only
        /// </summary>
        private BackgroundWorker _loadSupportWorker;
        /// <summary>
        /// Background worker used to load support data
        /// </summary>
        protected virtual BackgroundWorker LoadSupportWorker
        {
            get
            {
                if (_loadSupportWorker == null)
                {
                    _loadSupportWorker = new BackgroundWorker();
                    _loadSupportWorker.DoWork += WorkLoadSupportData;
                    _loadSupportWorker.RunWorkerCompleted += WorkLoadSupportDataCompleted;
                }
                return _loadSupportWorker;
            }
        }
        /// <summary>
        /// Returns an instance of an available (non-busy) load support worker
        /// </summary>
        protected virtual BackgroundWorker AvailableLoadSupportWorker
        {
            get
            {
                var worker = LoadSupportWorker;
                while (worker.IsBusy)
                {
                    // We wait
                }
                return worker;
            }
        }

        /// <summary>
        /// Triggers the load of support data
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event arguments</param>
        private void WorkLoadSupportData(object sender, DoWorkEventArgs e)
        {
            RunLoadSupportData();
        }

        /// <summary>
        /// Performs the load support data work
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "This method is specifically designed to catch all exceptions so they can be funnelled to the foreground thread and be re-thrown there.")]
        protected virtual void RunLoadSupportData()
        {
            object data;
            var dataFromControllers = new ArrayList();
            try
            {
                data = Host.LoadSecondaryData();
                if (_dataLoadControllers != null)
                    foreach (var controller in DataLoadControllers)
                        dataFromControllers.Add(controller.LoadSecondaryData());
                if (dataFromControllers.Count > 0)
                    foreach (var dataFromController in dataFromControllers)
                        data = MergeDataSources(data, dataFromController);
            }
            catch (Exception ex)
            {
                CommunicateExceptionToForegroundThread(ex);
                data = null;
            }
            PopulateSecondaryDataList(data);
        }

        /// <summary>
        /// Fires when the load of support data has been completed
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event arguments</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "This method is specifically designed to catch all exceptions so they can be funnelled to the foreground thread and be re-thrown there.")]
        private void WorkLoadSupportDataCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            try
            {
                Host.ShowContentsAuto();
                Host.ShowContents();
            }
            catch (Exception ex)
            {
                CommunicateExceptionToForegroundThread(ex);
            }
            Status = ContentStatus.DisplayComplete;
        }

        /// <summary>
        /// For internal use only
        /// </summary>
        private BackgroundWorker _deleteWorker;
        /// <summary>
        /// Background worker used to delete data
        /// </summary>
        protected virtual BackgroundWorker DeleteWorker
        {
            get
            {
                if (_deleteWorker == null)
                {
                    _deleteWorker = new BackgroundWorker();
                    _deleteWorker.DoWork += WorkDeleteData;
                    _deleteWorker.RunWorkerCompleted += WorkDeleteDataCompleted;
                }
                return _deleteWorker;
            }
        }
        /// <summary>
        /// Returns an instance of an available (non-busy) delete worker
        /// </summary>
        protected virtual BackgroundWorker AvailableDeleteWorker
        {
            get
            {
                var worker = DeleteWorker;
                while (worker.IsBusy)
                {
                    // We wait
                }
                return worker;
            }
        }

        /// <summary>
        /// Deleted
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "This is explicitly designed to catch all exceptions, so they can be passed on to the foreground thread.")]
        public virtual void Delete()
        {
            if (MultiThreaded)
                AvailableDeleteWorker.RunWorkerAsync();
            else
            {
                bool result = false;
                try
                {
                    result = AutoDelete();
                    if (result && _dataLoadControllers != null)
                        foreach (var controller in DataLoadControllers)
                            if (!controller.AutoDelete())
                            {
                                result = false;
                                break;
                            }
                }
                catch (Exception ex)
                {
                    CommunicateExceptionToForegroundThread(ex);
                }
                if (result)
                {
                    try
                    {
                        result = Host.Delete();
                    }
                    catch (Exception ex)
                    {
                        CommunicateExceptionToForegroundThread(ex);
                        result = false;
                    }
                }
                if (Deleted != null)
                    Deleted(this, new DeletedEventArgs(result));
            }
        }

        /// <summary>
        /// Performs an auto-delete on the current data entity
        /// </summary>
        /// <returns>True or false</returns>
        protected virtual bool AutoDelete()
        {
            var dataEntity = MainDataEntity;
            if (!PerformAutoDelete || dataEntity == null)
                // We can not consider this failed, so we return true
                return true;
            var deletableDataEntity = dataEntity as IDeletable;
            if (deletableDataEntity != null)
                // This is a deletable entity, so we have at it...
                return deletableDataEntity.Delete();
            // We can not delete this, but that doesn't mean that the
            // delete for the form failed, so we still return true.
            // After all, the entity never claims it is deletable this way,
            // since it does not implement IDeletable
            return true;
        }

        /// <summary>
        /// Triggers the delete of data
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event arguments</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "This method is specifically designed to catch all exceptions so they can be funnelled to the foreground thread and be re-thrown there.")]
        private void WorkDeleteData(object sender, DoWorkEventArgs e)
        {
            bool result = false;
            try
            {
                result = AutoDelete();
                if (result && _dataLoadControllers != null)
                    foreach (var controller in DataLoadControllers)
                        if (!controller.AutoDelete())
                        {
                            result = false;
                            break;
                        }
            }
            catch (Exception ex)
            {
                CommunicateExceptionToForegroundThread(ex);
            }
            if (result)
            {
                try
                {
                    result = Host.Delete();
                }
                catch (Exception ex)
                {
                    CommunicateExceptionToForegroundThread(ex);
                    result = false;
                }
            }
            e.Result = result;
        }

        /// <summary>
        /// Fires when the delete of data has been completed
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event arguments</param>
        private void WorkDeleteDataCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            var result = (bool)e.Result;
            if (Deleted != null)
                Deleted(this, new DeletedEventArgs(result));
        }

        /// <summary>
        /// Deleted event
        /// </summary>
        public event EventHandler<DeletedEventArgs> Deleted;

        /// <summary>
        /// This method takes a data object and uses it to populate the list of primary data objects
        /// </summary>
        /// <param name="data">Data object</param>
        protected virtual void PopulatePrimaryDataList(object data)
        {
            lock (PrimaryData)
            {
                PrimaryData.Clear();
                var dataList = data as IEnumerable;
                if (dataList != null)
                    // This is a list of data objects, so we add them to the primary data list one by one
                    foreach (object dataObject in dataList)
                        PrimaryData.Add(dataObject);
                else
                    // This appears to be a single item only, so we add it
                    PrimaryData.Add(data);
            }
        }

        /// <summary>
        /// This method takes a data object and uses it to populate the list of secondary data objects
        /// </summary>
        /// <param name="data">Data object</param>
        protected virtual void PopulateSecondaryDataList(object data)
        {
            lock (SecondaryData)
            {
                SecondaryData.Clear();
                var dataList = data as IEnumerable;
                if (dataList != null)
                    // This is a list of data objects, so we add them to the primary data list one by one
                    foreach (var dataObject in dataList)
                        SecondaryData.Add(dataObject);
                else
                    // This appears to be a single item only, so we add it
                    SecondaryData.Add(data);
            }
        }

        /// <summary>
        /// For internal use only
        /// </summary>
        private Type _entityType;

        /// <summary>
        /// Sets the main entity type used for auto-loading of content.
        /// </summary>
        /// <typeparam name="TType">Data entity type</typeparam>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Justification = "This method is specifically designed to set the type internally.")]
        public virtual void SetEntityType<TType>()
        {
            _entityType = typeof(TType);
            _newDataMethod = null;
            _loadDataMethod = null;
        }

        /// <summary>
        /// Creates a clone of a dictionary, so it can safely be used on a background thread.
        /// </summary>
        /// <param name="originalCollection">Original collection</param>
        /// <returns>Copy of the collection</returns>
        protected virtual Dictionary<string, object> CloneDictionary(Dictionary<string, object> originalCollection)
        {
            var keys = originalCollection.Keys;
            var newCollection = new Dictionary<string, object>(originalCollection.Count);
            foreach (var key in keys)
                newCollection.Add(key, originalCollection[key]);
            return newCollection;
        }

        /// <summary>
        /// Merges two data entity sources
        /// </summary>
        /// <param name="dataAuto">Source 1 (often auto-generated)</param>
        /// <param name="dataManual">Source 2 (often manually retrieved)</param>
        /// <returns>Data object, or list of data objects</returns>
        private static object MergeDataSources(object dataAuto, object dataManual)
        {
            object data = null;
            var dataAutoEnumerable = dataAuto as IEnumerable;
            var dataManualEnumerable = dataManual as IEnumerable;

            if (dataAuto != null && dataManual != null)
            {
                // We have two different data objects, so we combine them
                var dataObjects = new ArrayList();

                // First, the auto-generated data
                if (dataAutoEnumerable != null)
                    // This is an enumerable source, which we add to the list
                    foreach (var data2 in dataAutoEnumerable)
                        dataObjects.Add(data2);
                else
                    dataObjects.Add(dataAuto);

                // Then, the manually-generated data
                if (dataManualEnumerable != null)
                    // This is an enumerable source, which we add to the list
                    foreach (var data2 in dataManualEnumerable)
                        dataObjects.Add(data2);
                else
                    dataObjects.Add(dataManual);
                data = dataObjects;
            }
            else if (dataAuto != null)
                if (dataAutoEnumerable != null)
                {
                    var dataObjects = new ArrayList();
                    // This is an enumerable source, which we add to the list
                    foreach (var data2 in dataAutoEnumerable)
                        dataObjects.Add(data2);
                    data = dataObjects;
                }
                else
                    data = dataAuto;
            else if (dataManual != null)
                if (dataManualEnumerable != null)
                {
                    var dataObjects = new ArrayList();
                    // This is an enumerable source, which we add to the list
                    foreach (var data2 in dataManualEnumerable)
                        dataObjects.Add(data2);
                    data = dataObjects;
                }
                else
                    data = dataManual;
            return data;
        }

        /// <summary>
        /// This method is used to funnel an exception to a different thread.
        /// </summary>
        /// <param name="ex">Exception</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "This is explicitly designed to catch all exceptions.")]
        private void CommunicateExceptionToForegroundThread(Exception ex)
        {
            Host.HandleDataException(ex);
        }

        /// <summary>
        /// This class is used to pass the callbacks to the worker thread.
        /// </summary>
        private class VerifyCallbacks
        {
            /// <summary>
            /// Success callback
            /// </summary>
            public EventHandler<VerifiedEventArgs> SuccessCallback { get; private set; }

            /// <summary>
            /// Failure callback
            /// </summary>
            public EventHandler<VerifiedEventArgs> FailureCallback { get; private set; }

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="successCallback">Success callback</param>
            /// <param name="failureCallback">Failure callback</param>
            public VerifyCallbacks(EventHandler<VerifiedEventArgs> successCallback, EventHandler<VerifiedEventArgs> failureCallback)
            {
                SuccessCallback = successCallback;
                FailureCallback = failureCallback;
            }
        }

        /// <summary>
        /// This class is used to pass the callbacks to the worker thread.
        /// </summary>
        private class SaveCallbacks
        {
            /// <summary>
            /// Success callback
            /// </summary>
            public EventHandler<SavedEventArgs> SuccessCallback { get; private set; }

            /// <summary>
            /// Failure callback
            /// </summary>
            public EventHandler<SavedEventArgs> FailureCallback { get; private set; }

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="successCallback">Success callback</param>
            /// <param name="failureCallback">Failure callback</param>
            public SaveCallbacks(EventHandler<SavedEventArgs> successCallback, EventHandler<SavedEventArgs> failureCallback)
            {
                SuccessCallback = successCallback;
                FailureCallback = failureCallback;
            }
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Destructor
        /// </summary>
        ~EditDataLoader()
        {
            Dispose(false);
        }

        /// <summary>
        /// Dispose
        /// </summary>
        /// <param name="disposing">Are we disposing managed resources?</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _disposed = true;

                if (_loadSupportWorker != null) _loadSupportWorker.Dispose();
                if (_loadWorker != null) _loadWorker.Dispose();
                if (_newWorker != null) _newWorker.Dispose();
                if (_saveWorker != null) _saveWorker.Dispose();
                if (_verifyWorker != null) _verifyWorker.Dispose();
                if (_deleteWorker != null) _deleteWorker.Dispose();

                // Since this is a manual call of Dispose(), we can suppress
                // further finalization.
                if (disposing) GC.SuppressFinalize(this);
            }
        }

        /// <summary>
        /// For internal use only
        /// </summary>
        private bool _disposed;
    }
}

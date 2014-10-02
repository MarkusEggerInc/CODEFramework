using System;
using System.Collections.Generic;
using System.Reflection;
using System.Collections.ObjectModel;
using System.Collections;

namespace CODE.Framework.Core.ComponentModel
{
    /// <summary>
    /// Abstract data load controller class used to define data environments for forms.
    /// </summary>
    public abstract class DataLoadController : IDataLoadController
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataLoadController"/> class.
        /// </summary>
        protected DataLoadController()
        {
            PerformAutoDelete = true;
            PerformAutoLoad = true;
            PerformAutoNew = true;
            PerformAutoSave = true;
            PerformAutoVerify = true;
        }

        /// <summary>
        /// Configures this instance.
        /// </summary>
        public virtual void Configure() { }

        /// <summary>
        /// This method gets called whenever new content needs to be created (such as a new entity)
        /// </summary>
        /// <returns>Data object</returns>
        public virtual object NewContents()
        {
            return null;
        }

        /// <summary>
        /// This method is invoked whenever data needs to be loaded (including reloading data)
        /// </summary>
        /// <returns>Data object</returns>
        public virtual object LoadContents()
        {
            return null;
        }

        /// <summary>
        /// Called whenever secondary data needs to be loaded
        /// </summary>
        /// <returns>Data object</returns>
        public virtual object LoadSecondaryData()
        {
            return null;
        }

        /// <summary>
        /// Defines an entity type that is to be loaded
        /// </summary>
        /// <typeparam name="TType">The type of the type.</typeparam>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter")]
        public virtual void SetEntityType<TType>()
        {
            _entityType = typeof(TType);
            _newDataMethod = null;
            _loadDataMethod = null;
        }

        /// <summary>
        /// Internal reference to the data loader
        /// </summary>
        private IEditDataLoader _loader;

        /// <summary>
        /// Internal reference to the data loader
        /// </summary>
        protected IEditDataLoader Loader
        {
            get { return _loader ?? (_loader = GetInternalLoader()); }
            private set { _loader = value; }
        }

        /// <summary>
        /// Returns an instance of an automatically created internal loader.
        /// </summary>
        /// <remarks>
        /// Internal loaders are generally used for unit testing only. It is often useful to override 
        /// this method to return a different loader. The loader is generally only used to return parameters
        /// that may be used during loading. To do so, subclass the default loader (TestEditDataLoader)
        /// or implement the IEditDataLoader interface and return it from this method. The controller will 
        /// then automatically use that loader.
        /// </remarks>
        protected virtual IEditDataLoader GetInternalLoader()
        {
            return new TestEditDataLoader();
        }

        /// <summary>
        /// Defines the data loader object this controller targets
        /// </summary>
        /// <param name="loader">The loader.</param>
        public virtual void SetDataLoader(IEditDataLoader loader)
        {
            Loader = loader;
        }

        /// <summary>
        /// Should the object perform auto news based on the entity type specified?
        /// </summary>
        /// <value></value>
        /// <remarks>This property only has an impact after an entity type has been set by means of the SetEntityType() method</remarks>
        public bool PerformAutoNew { get; set; }
        /// <summary>
        /// Should the object perform auto loads based on the entity type specified?
        /// </summary>
        /// <value></value>
        /// <remarks>This property only has an impact after an entity type has been set by means of the SetEntityType() method</remarks>
        public bool PerformAutoLoad { get; set; }
        /// <summary>
        /// Should the object perform auto deletes based on the entity type specified?
        /// </summary>
        /// <value></value>
        /// <remarks>This property only has an impact after an entity type has been set by means of the SetEntityType() method</remarks>
        public bool PerformAutoDelete { get; set; }
        /// <summary>
        /// Should the object perform auto saves based on the entity type specified?
        /// </summary>
        /// <value></value>
        /// <remarks>This property only has an impact after an entity type has been set by means of the SetEntityType() method</remarks>
        public bool PerformAutoSave { get; set; }
        /// <summary>
        /// Should the object perform auto verifies based on the entity type specified?
        /// </summary>
        /// <value></value>
        /// <remarks>This property only has an impact after an entity type has been set by means of the SetEntityType() method</remarks>
        public bool PerformAutoVerify { get; set; }

        /// <summary>
        /// Gets the status.
        /// </summary>
        /// <value>The status.</value>
        protected virtual ContentStatus Status
        {
            get
            {
                return Loader.Status;
            }
        }

        /// <summary>
        /// Retrieves the value of a parameter by name
        /// </summary>
        /// <param name="parameterName">Parameter name</param>
        /// <returns>Parameter value</returns>
        /// <typeparam name="TType">Expected return type</typeparam>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter")]
        protected virtual TType GetParameterValue<TType>(string parameterName)
        {
            return Loader.GetParameterValue<TType>(parameterName);
        }

        /// <summary>
        /// Retrieves the value of a parameter by name
        /// </summary>
        /// <param name="parameterName">Parameter name</param>
        /// <returns>Parameter value</returns>
        protected virtual object GetParameterValue(string parameterName)
        {
            return Loader.GetParameterValue(parameterName);
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
        protected virtual Dictionary<string, object> Parameters
        {
            get
            {
                return Loader.Parameters;
            }
        }

        /// <summary>
        /// Sets the parameter value (and adds the key to the collection if need be).
        /// </summary>
        /// <typeparam name="TType">The type of the type.</typeparam>
        /// <param name="parameterName">Name of the parameter.</param>
        /// <param name="value">The value.</param>
        protected virtual void SetParameterValue<TType>(string parameterName, TType value)
        {
            Loader.SetParameterValue(parameterName, value);
        }

        /// <summary>
        /// Returns true if the specified parameter exists
        /// </summary>
        /// <param name="parameterName">Name of the parameter.</param>
        /// <returns>True or false</returns>
        protected virtual bool ParameterExists(string parameterName)
        {
            return Loader.ParameterExists(parameterName);
        }

        /// <summary>
        /// For internal use only (cache)
        /// </summary>
        private MethodInfo _loadDataMethod;

        /// <summary>
        /// For internal use only (cache)
        /// </summary>
        private MethodInfo _newDataMethod;

        /// <summary>
        /// For internal use only
        /// </summary>
        private Type _entityType;

        /// <summary>
        /// Main entity created by auto-load or auto-new
        /// </summary>
        /// <value>The main entity.</value>
        protected virtual object MainEntity { get; set; }

        /// <summary>
        /// Performs an automatic load operation, if possible
        /// </summary>
        /// <returns>Data object or data object list</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "The explicit purpose of this is to catch all exceptions.")]
        public virtual object AutoLoad()
        {
            if (!PerformAutoLoad) return null;

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
                MainEntity = _loadDataMethod.Invoke(null, loadParas);
                return MainEntity;
            }

            if (_entityType == null)
            {
                // Nothing to see here... move along...
                return null;
            }

            Type entityType = _entityType;
            MethodInfo[] methods = entityType.GetMethods(BindingFlags.Static | BindingFlags.Public);
            foreach (MethodInfo method in methods)
            {
                if (method.Name == "LoadEntity")
                {
                    ParameterInfo[] paras = method.GetParameters();
                    if (paras.Length == 1)
                    {
                        var key = GetParameterValue("key");
                        var keyString = key as string;

                        if (paras[0].ParameterType == typeof(Guid))
                        {
                            var goodToGo = false;
                            var key2 = Guid.Empty;
                            if (key is Guid)
                            {
                                key2 = (Guid)key;
                                goodToGo = true;
                            }
                            else if (keyString != null)
                            {
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
                                var loadParas = new object[] { key2 };
                                MainEntity = _loadDataMethod.Invoke(null, loadParas);
                                return MainEntity;
                            }
                        }

                        if (paras[0].ParameterType == typeof(int))
                        {
                            var goodToGo = false;
                            var key2 = -1;
                            if (key is int)
                            {
                                key2 = (int)key;
                                goodToGo = true;
                            }
                            else if (keyString != null)
                            {
                                try
                                {
                                    key2 = int.Parse(keyString, System.Globalization.CultureInfo.InvariantCulture);
                                    goodToGo = true;
                                }
                                catch { } // not an int in string format
                            }
                            if (goodToGo)
                            {
                                // This is the one we were looking for
                                _loadDataMethod = method;
                                var loadParas = new[] { (object)key2 };
                                MainEntity = _loadDataMethod.Invoke(null, loadParas);
                                return MainEntity;
                            }
                        }

                        // At this point, we pretty much assume we have a string
                        if (paras[0].ParameterType == typeof(string))
                        {
                            var key2 = keyString;
                            // This is the one we were looking for
                            _loadDataMethod = method;
                            var loadParas = new object[] { key2 };
                            MainEntity = _loadDataMethod.Invoke(null, loadParas);
                            return MainEntity;
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Performs an automatic new operation, if possible
        /// </summary>
        /// <returns>Data object or data object list</returns>
        public virtual object AutoNew()
        {
            if (!PerformAutoNew) return null;

            // Maybe we have a shortcut already
            if (_newDataMethod != null) return _newDataMethod.Invoke(null, null);

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
                        MainEntity = _newDataMethod.Invoke(null, null);
                        return MainEntity;
                    }

            return null;
        }

        /// <summary>
        /// Performs an auto-delete on the current data entity
        /// </summary>
        /// <returns>True or false</returns>
        public virtual bool AutoDelete()
        {
            var dataEntity = MainEntity;
            if (!PerformAutoDelete || dataEntity == null)
                // We can not consider this failed, so we return true
                return true;
            var deletableDataEntity = dataEntity as IDeletable;
            return deletableDataEntity == null || deletableDataEntity.Delete();
        }

        /// <summary>
        /// Performs an auto-save on the current data entity
        /// </summary>
        /// <returns>True or false</returns>
        public virtual bool AutoSave()
        {
            var dataEntity = MainEntity;
            if (!PerformAutoSave || dataEntity == null)
                // We can not consider this failed, so we return true
                return true;
            var savableDataEntity = dataEntity as ISavable;
            return savableDataEntity == null || savableDataEntity.Save();
        }

        /// <summary>
        /// Performs an auto-verification on the current data entity
        /// </summary>
        /// <returns>True or false</returns>
        public virtual bool AutoVerify()
        {
            var dataEntity = MainEntity;
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
    }

    /// <summary>
    /// Basic data controller interface
    /// </summary>
    public interface IDataLoadController
    {
        /// <summary>
        /// Configures this instance.
        /// </summary>
        void Configure();
        /// <summary>
        /// This method gets called whenever new content needs to be created (such as a new entity)
        /// </summary>
        /// <returns>Data object</returns>
        object NewContents();
        /// <summary>
        /// This method is invoked whenever data needs to be loaded (including reloading data)
        /// </summary>
        /// <returns>Data object</returns>
        object LoadContents();
        /// <summary>
        /// Called whenever secondary data needs to be loaded
        /// </summary>
        /// <returns>Data object</returns>
        object LoadSecondaryData();

        /// <summary>
        /// Defines an entity type that is to be loaded
        /// </summary>
        /// <typeparam name="TType">The type of the type.</typeparam>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter")]
        void SetEntityType<TType>();

        /// <summary>
        /// Defines the data loader object this controller targets
        /// </summary>
        /// <param name="loader">The loader.</param>
        void SetDataLoader(IEditDataLoader loader);

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
        /// Should the object perform auto deletes based on the entity type specified?
        /// </summary>
        /// <remarks>This property only has an impact after an entity type has been set by means of the SetEntityType() method</remarks>
        bool PerformAutoDelete { get; set; }
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
        /// Performs an automatic load operation, if possible
        /// </summary>
        /// <returns>Data object or data object list</returns>
        object AutoLoad();

        /// <summary>
        /// Performs an automatic new operation, if possible
        /// </summary>
        /// <returns>Data object or data object list</returns>
        object AutoNew();

        /// <summary>
        /// Performs an auto-delete on the current data entity
        /// </summary>
        /// <returns>True or false</returns>
        bool AutoDelete();

        /// <summary>
        /// Performs an auto-save on the current data entity
        /// </summary>
        /// <returns>True or false</returns>
        bool AutoSave();

        /// <summary>
        /// Performs an auto-verification on the current data entity
        /// </summary>
        /// <returns>True or false</returns>
        bool AutoVerify();
    }

    /// <summary>
    /// Collection of data loader controllers
    /// </summary>
    public class DataLoadControllerCollection : Collection<IDataLoadController>
    {
        /// <summary>
        /// Internal reference to the controller
        /// </summary>
        private readonly IEditDataLoader _loader;
        /// <summary>
        /// Initializes a new instance of the <see cref="DataLoadControllerCollection"/> class.
        /// </summary>
        public DataLoadControllerCollection(IEditDataLoader loader)
        {
            _loader = loader;
        }

        /// <summary>
        /// Adds a data loader controller
        /// </summary>
        /// <param name="controller">The controller.</param>
        public new void Add(IDataLoadController controller)
        {
            controller.Configure();
            controller.SetDataLoader(_loader);

            base.Add(controller);
        }
    }

    /// <summary>
    /// Data loader class that is automatically invoked for stand-alone unit testing scenarios
    /// </summary>
    class TestEditDataLoader : IEditDataLoader
    {
        /// <summary>
        /// Sets the host object used to load data
        /// </summary>
        /// <param name="host">Host</param>
        public void SetHost(IDataEditHandler host) { }

        /// <summary>
        /// Saves all the current data
        /// </summary>
        public void Save() { }

        /// <summary>
        /// Saves all the current data
        /// </summary>
        /// <param name="successCallback">Callback for success</param>
        /// <param name="failureCallback">Callback for failure</param>
        public void Save(EventHandler<SavedEventArgs> successCallback, EventHandler<SavedEventArgs> failureCallback) { }

        /// <summary>
        /// Saved event
        /// </summary>
        public event EventHandler<SavedEventArgs> Saved;

        /// <summary>
        /// Verifies the current data
        /// </summary>
        public void Verify() { }

        /// <summary>
        /// Verifies the current data
        /// </summary>
        /// <param name="successCallback">Callback for success</param>
        /// <param name="failureCallback">Callback for failure</param>
        public void Verify(EventHandler<VerifiedEventArgs> successCallback, EventHandler<VerifiedEventArgs> failureCallback) { }

        /// <summary>
        /// Verified event
        /// </summary>
        public event EventHandler<VerifiedEventArgs> Verified;

        /// <summary>
        /// Content status
        /// </summary>
        /// <value></value>
        public ContentStatus Status
        {
            get { return ContentStatus.Virgin; }
        }

        /// <summary>
        /// Content status changed event
        /// </summary>
        public event EventHandler<ContentStatusChangedEventArgs> ContentStatusChanged;

        /// <summary>
        /// Loads data
        /// </summary>
        /// <param name="parameters">Parameters collection</param>
        public void Load(Dictionary<string, object> parameters) { }

        /// <summary>
        /// Loads data
        /// </summary>
        /// <param name="parameters">Parameters collection</param>
        /// <param name="loadPrimaryData">if set to <c>true</c> [load primary data].</param>
        /// <param name="loadSecondaryData">if set to <c>true</c> [load secondary data].</param>
        public void Load(Dictionary<string, object> parameters, bool loadPrimaryData, bool loadSecondaryData) { }

        /// <summary>
        /// Loaded event
        /// </summary>
        public event EventHandler<LoadedEventArgs> Loaded;

        /// <summary>
        /// Creates new data
        /// </summary>
        /// <param name="parameters">Parameters collection</param>
        public void New(Dictionary<string, object> parameters) { }

        /// <summary>
        /// Creates new data
        /// </summary>
        /// <param name="parameters">Parameters collection</param>
        /// <param name="newPrimaryData">if set to <c>true</c> [new primary data].</param>
        /// <param name="loadSecondaryData">if set to <c>true</c> [load secondary data].</param>
        public void New(Dictionary<string, object> parameters, bool newPrimaryData, bool loadSecondaryData) { }

        /// <summary>
        /// Newed event
        /// </summary>
        public event EventHandler<NewedEventArgs> Newed;

        /// <summary>
        /// Deletes data
        /// </summary>
        public void Delete() { }

        /// <summary>
        /// Deleted event
        /// </summary>
        public event EventHandler<DeletedEventArgs> Deleted;

        /// <summary>
        /// Should data operations be performed multi threaded?
        /// </summary>
        /// <value></value>
        /// <remarks>
        /// This property shall always default to true.
        /// </remarks>
        public bool MultiThreaded { get; set; }

        /// <summary>
        /// Array list (collection) of primary data objects
        /// </summary>
        /// <value></value>
        public ArrayList PrimaryData
        {
            get { return null; }
        }

        /// <summary>
        /// Array list (collection) of secondary data objects
        /// </summary>
        /// <value></value>
        public ArrayList SecondaryData
        {
            get { return null; }
        }

        /// <summary>
        /// Retrieves the value of a parameter by name
        /// </summary>
        /// <typeparam name="TType">Expected return type</typeparam>
        /// <param name="parameterName">Parameter name</param>
        /// <returns>Parameter value</returns>
        public TType GetParameterValue<TType>(string parameterName)
        {
            return default(TType);
        }

        /// <summary>
        /// Retrieves the value of a parameter by name
        /// </summary>
        /// <param name="parameterName">Parameter name</param>
        /// <returns>Parameter value</returns>
        public object GetParameterValue(string parameterName)
        {
            return null;
        }

        /// <summary>
        /// Internal dictionary
        /// </summary>
        private readonly Dictionary<string, object> _parameters = new Dictionary<string, object>();
        /// <summary>
        /// Parameters collection (thread-safe)
        /// </summary>
        /// <value></value>
        public Dictionary<string, object> Parameters
        {
            get { return _parameters; }
        }

        /// <summary>
        /// Sets the parameter value (and adds the key to the collection if need be).
        /// </summary>
        /// <typeparam name="TType">The type of the type.</typeparam>
        /// <param name="parameterName">Name of the parameter.</param>
        /// <param name="value">The value.</param>
        public void SetParameterValue<TType>(string parameterName, TType value) { }

        /// <summary>
        /// Returns true if the specified parameter exists
        /// </summary>
        /// <param name="parameterName">Name of the parameter.</param>
        /// <returns>True or false</returns>
        public bool ParameterExists(string parameterName)
        {
            return false;
        }

        /// <summary>
        /// Reference to the main data entity object
        /// </summary>
        /// <value></value>
        /// <remarks>
        /// This object typically is the first object in the primary data collection
        /// </remarks>
        public object MainDataEntity
        {
            get { return null; }
        }

        /// <summary>
        /// Sets the main entity type used for auto-loading of content.
        /// </summary>
        /// <typeparam name="TType">Data entity type</typeparam>
        /// <remarks>
        /// Any object can be set as the main data entity type. However,
        /// to perform auto-data-handling, the object must have certain characteristics.
        /// New data creation:
        /// The type must have a static NewEntity() method (no parameters).
        /// Data loading:
        /// The type must have a static LoadEntity(Guid) method, -- and/or --
        /// The type must have a static LoadEntity(int) method, -- and/or --
        /// The type must have a static LoadEntity(string) method.
        /// Data saving:
        /// The type must implement ISavable (and probably should implement IVerifyable)
        /// Data verification:
        /// The type must implement IVerifyable
        /// Data delition:
        /// The type must implement IDeletable
        /// </remarks>
        public void SetEntityType<TType>() { }

        /// <summary>
        /// Should the object perform auto news based on the entity type specified?
        /// </summary>
        /// <value></value>
        /// <remarks>This property only has an impact after an entity type has been set by means of the SetEntityType() method</remarks>
        public bool PerformAutoNew { get; set; }

        /// <summary>
        /// Should the object perform auto loads based on the entity type specified?
        /// </summary>
        /// <value></value>
        /// <remarks>This property only has an impact after an entity type has been set by means of the SetEntityType() method</remarks>
        public bool PerformAutoLoad { get; set; }

        /// <summary>
        /// Should the object perform auto saves based on the entity type specified?
        /// </summary>
        /// <value></value>
        /// <remarks>This property only has an impact after an entity type has been set by means of the SetEntityType() method</remarks>
        public bool PerformAutoSave { get; set; }

        /// <summary>
        /// Should the object perform auto verifies based on the entity type specified?
        /// </summary>
        /// <value></value>
        /// <remarks>This property only has an impact after an entity type has been set by means of the SetEntityType() method</remarks>
        public bool PerformAutoVerify { get; set; }

        /// <summary>
        /// Should the object perform auto deletes based on the entity type specified?
        /// </summary>
        /// <value></value>
        /// <remarks>This property only has an impact after an entity type has been set by means of the SetEntityType() method</remarks>
        public bool PerformAutoDelete { get; set; }

        /// <summary>
        /// Internal reference for data controllers
        /// </summary>
        private DataLoadControllerCollection _dataLoadControllers;
        /// <summary>
        /// Gets the data load controllers.
        /// </summary>
        /// <value>The data load controllers.</value>
        public DataLoadControllerCollection DataLoadControllers
        {
            get { return _dataLoadControllers ?? (_dataLoadControllers = new DataLoadControllerCollection(this)); }
        }
    }
}

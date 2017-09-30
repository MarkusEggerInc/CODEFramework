using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Threading;
using CODE.Framework.Core.Newtonsoft;
using CODE.Framework.Core.Newtonsoft.Utilities;
using CODE.Framework.Core.Utilities.Extensions;

namespace CODE.Framework.Core.Utilities
{
    /// <summary>
    /// This class is used to save user and machine settings and preferences
    /// </summary>
    public static class SettingsManager
    {
        /// <summary>
        /// Static constructor
        /// </summary>
        static SettingsManager()
        {
            LastSaveSettingsError = string.Empty;
        }

        /// <summary>
        /// Saves the object with the provided settings according to current SettingsManager configuration.
        /// </summary>
        /// <param name="stateObject">The object that contains the preserved state</param>
        /// <param name="id">ID (name) of the setting that is to be saved. If omitted, the name of the state object's class is used.</param>
        /// <param name="scope">Scope of the setting (default is Workstation)</param>
        /// <param name="serializerTypeFilter">If not null, only serializers of this type will be considered for serialization.</param>
        /// <param name="includeDerivedTypeFilterTypes">If set to true, serializer type filters will match derived types, otherwise, only the exact type will be used.</param>
        /// <returns>True if the operation was successful</returns>
        public static bool SaveSettings(object stateObject, string id = "", SettingScope scope = SettingScope.Workstation, Type serializerTypeFilter = null, bool includeDerivedTypeFilterTypes = false)
        {
            LastSaveSettingsError = string.Empty;
            if (stateObject == null)
            {
                LastSaveSettingsError = "State object was null.";
                return false;
            }

            try
            {
                var stateId = GetSettingId(id, stateObject);
                var serializers = GetSerializers(stateObject, stateId, scope);
                var allSucceeded = true;
                var isFirst = true;
                foreach (var serializer in serializers)
                {
                    if (serializerTypeFilter != null)
                    {
                        // We may have to skip the type if it doesn't match the filter
                        if (includeDerivedTypeFilterTypes && !serializerTypeFilter.IsInstanceOfType(serializer)) continue;
                        if (!includeDerivedTypeFilterTypes && serializer.GetType() != serializerTypeFilter) continue;
                    }

                    if (!isFirst && !serializer.UseInAdditionToOtherAppliedSerializers) continue;
                    var json = serializer.SerializeToJson(stateObject);
                    var handler = GetSettingsHandler(scope);
                    if (handler != null)
                    {
                        if (string.IsNullOrEmpty(json))
                        {
                            handler.Clear(id, serializer.GetSuggestedFileName(stateObject, id, scope));
                            continue;
                        }
                        if (!handler.Save(json, stateId, serializer.GetSuggestedFileName(stateId, stateId, scope)))
                        {
                            allSucceeded = false;
                            LastSaveSettingsError += "Handler " + handler.GetType().FullName + " was unable to save the provided state.\r\n";
                        }
                    }
                    else
                        LastSaveSettingsError += "No handler registered for scope " + scope + "\r\n";
                    isFirst = false;
                }
                return allSucceeded;
            }
            catch (Exception ex)
            {
                LastSaveSettingsError = ExceptionHelper.GetExceptionText(ex);
                return false; // We do not let this fail, but we return false if we had trouble saving a setting
            }
        }

        /// <summary>
        /// Clears the specified settings.
        /// </summary>
        /// <param name="stateObject">The state object.</param>
        /// <param name="id">The identifier.</param>
        /// <param name="scope">The scope.</param>
        /// <returns>True if the operation was successful</returns>
        public static bool ClearSettings(object stateObject, string id = "", SettingScope scope = SettingScope.Workstation)
        {
            LastClearSettingsError = string.Empty;
            if (stateObject == null)
            {
                LastClearSettingsError = "State object was null.";
                return false;
            }

            try
            {
                var stateId = GetSettingId(id, stateObject);
                var serializers = GetSerializers(stateObject, stateId, scope);
                var allSucceeded = true;
                var isFirst = true;
                foreach (var serializer in serializers)
                {
                    if (!isFirst && !serializer.UseInAdditionToOtherAppliedSerializers) continue;
                    var handler = GetSettingsHandler(scope);
                    if (handler != null)
                    {
                        if (!handler.Clear(stateId, serializer.GetSuggestedFileName(stateId, stateId, scope)))
                        {
                            allSucceeded = false;
                            LastClearSettingsError += "Handler " + handler.GetType().FullName + " was unable to clear the desired state.\r\n";
                        }
                    }
                    else
                        LastClearSettingsError += "No handler registered for scope " + scope + "\r\n";
                    isFirst = false;
                }
                return allSucceeded;
            }
            catch (Exception ex)
            {
                LastClearSettingsError = ExceptionHelper.GetExceptionText(ex);
                return false; // We do not let this fail, but we return false if we had trouble saving a setting
            }
        }

        /// <summary>
        /// Loads the specified settings and applies them to the state object.
        /// </summary>
        /// <param name="stateObject">The object that needs to be updated with the persisted settings</param>
        /// <param name="id">ID (name) of the setting that is to be loaded. If not present, the name of the state object's class is used.</param>
        /// <param name="scope">Scope of the setting (default is Workstation)</param>
        /// <returns>
        /// Returns true if the setting was loaded. If no existing settings were found, the method returns false.
        /// </returns>
        public static bool LoadSettings(object stateObject, string id = "", SettingScope scope = SettingScope.Workstation)
        {
            LastLoadSettingsError = string.Empty;
            if (stateObject == null)
            {
                LastLoadSettingsError = "State object was null.";
                return false;
            }

            try
            {
                var stateId = GetSettingId(id, stateObject);
                var handler = GetSettingsHandler(scope);
                if (handler == null)
                {
                    LastLoadSettingsError = "No handler registered for scope " + scope;
                    return false;
                }
                var serializers = GetSerializers(stateObject, stateId, scope);
                var allSucceeded = true;
                var isFirst = true;
                foreach (var serializer in serializers)
                {
                    if (!isFirst && !serializer.UseInAdditionToOtherAppliedSerializers) continue;
                    var json = handler.Load(stateId, serializer.GetSuggestedFileName(stateId, stateId, scope));
                    if (string.IsNullOrEmpty(json)) allSucceeded = false;
                    serializer.DeserializeFromJson(stateObject, json);
                    isFirst = false;
                }
                return allSucceeded;
            }
            catch (Exception ex)
            {
                LastLoadSettingsError = ExceptionHelper.GetExceptionText(ex);
                return false; // We do not let this fail, but we return false if we had trouble saving a setting
            }
        }

        /// <summary>
        /// Checks if the specified MRU item exists, and if so, removes it from the list.
        /// </summary>
        /// <param name="area">The area (such as "Invoice").</param>
        /// <param name="mruItem">The MRU item.</param>
        /// <param name="scope">Scope of the setting (default is User)</param>
        /// <returns>True if success</returns>
        public static bool RemoveMostRecentlyUsed(string area, MostRecentlyUsed mruItem, SettingScope scope = SettingScope.User)
        {
            // If users did not register a special MRU serializer by the time we run this, we will register the default one
            if (!RegisteredSerializers.OfType<MostRecentlyUsedListSerializer>().Any()) RegisterSerializer<MostRecentlyUsedListSerializer>();

            var itemFound = false;
            var existingList = LoadMostRecentlyUsed(area, scope);
            while (true)
            {
                var duplicateItem = existingList.FirstOrDefault(i => i.Id == mruItem.Id);
                if (duplicateItem == null) break;
                existingList.Remove(duplicateItem);
                itemFound = true;
            }
            if (!itemFound)
                // We didn't find the item, so we consider this a success
                return true;

            var response = SaveSettings(existingList, area, scope, typeof(MostRecentlyUsedListSerializer), true);

            var handler = MostRecentlyUsedChanged;
            if (handler != null)
                handler(null, new MostRecentlyUsedEventArgs {Area = area, Scope = scope, LastItemAdded = mruItem, CompleteList = existingList});

            return response;
        }

        /// <summary>
        /// Saves an MRU item for the specified area to the list
        /// </summary>
        /// <param name="area">The area (such as "Invoice").</param>
        /// <param name="mruItem">The MRU item.</param>
        /// <param name="scope">Scope of the setting (default is User)</param>
        /// <returns>True if success</returns>
        public static bool SaveMostRecentlyUsed(string area, MostRecentlyUsed mruItem, SettingScope scope = SettingScope.User)
        {
            // If users did not register a special MRU serializer by the time we run this, we will register the default one
            if (!RegisteredSerializers.OfType<MostRecentlyUsedListSerializer>().Any()) RegisterSerializer<MostRecentlyUsedListSerializer>();

            var existingList = LoadMostRecentlyUsed(area, scope);
            while (true)
            {
                var duplicateItem = existingList.FirstOrDefault(i => i.Id == mruItem.Id);
                if (duplicateItem == null) break;
                existingList.Remove(duplicateItem);
            }
            existingList.Add(mruItem);
            var response = SaveSettings(existingList, area, scope, typeof(MostRecentlyUsedListSerializer), true);

            var handler = MostRecentlyUsedChanged;
            if (handler != null)
                handler(null, new MostRecentlyUsedEventArgs { Area = area, Scope = scope, LastItemAdded = mruItem, CompleteList = existingList });

            return response;
        }

        /// <summary>
        /// Occurs when the most-recently
        /// </summary>
        public static event EventHandler<MostRecentlyUsedEventArgs> MostRecentlyUsedChanged;

        /// <summary>
        /// Saves an MRU item for the specified area to the list
        /// </summary>
        /// <param name="area">The area (such as "Invoice").</param>
        /// <param name="id">String that uniquely identifies the item in question (often a string representation of a GUID)</param>
        /// <param name="title">Title to be displayed in MRU lists.</param>
        /// <param name="timestamp">Time the item was used (if mull, DateTime.Now is assumed)</param>
        /// <param name="data">Name/value pairs of other data.</param>
        /// <param name="scope">Scope of the setting (default is User)</param>
        /// <returns>True if success</returns>
        public static bool SaveMostRecentlyUsed(string area, string id, string title, DateTime? timestamp = null, Dictionary<string, string> data = null, SettingScope scope = SettingScope.User)
        {
            if (data == null) data = new Dictionary<string, string>();
            if (!timestamp.HasValue) timestamp = DateTime.Now;

            var item = new MostRecentlyUsed
            {
                Id = id,
                Title = title,
                Timestamp = timestamp.Value,
                Data = data
            };
            return SaveMostRecentlyUsed(area, item, scope);
        }

        /// <summary>
        /// Saves an MRU item for the specified area to the list
        /// </summary>
        /// <param name="area">The area (such as "Invoice").</param>
        /// <param name="id">String that uniquely identifies the item in question (often a string representation of a GUID)</param>
        /// <param name="title">Title to be displayed in MRU lists.</param>
        /// <param name="data">Name/value pairs of other data.</param>
        /// <param name="scope">Scope of the setting (default is User)</param>
        /// <returns>True if success</returns>
        public static bool RemoveMostRecentlyUsed(string area, string id, string title, Dictionary<string, string> data = null, SettingScope scope = SettingScope.User)
        {
            if (data == null) data = new Dictionary<string, string>();

            var item = new MostRecentlyUsed
            {
                Id = id,
                Title = title,
                Data = data
            };
            return RemoveMostRecentlyUsed(area, item, scope);
        }

        /// <summary>
        /// Returns a list of most-recently-used entries for the specified area
        /// </summary>
        /// <param name="area">The area (such as "Invoice").</param>
        /// <param name="scope">Scope of the setting (default is User)</param>
        /// <param name="existingList">Optionally, an existing list that is to be populated can be passed in (useful for custom implementations of IMostRecentlyUsed)</param>
        /// <returns>Most-recently-used items list</returns>
        public static List<MostRecentlyUsed> LoadMostRecentlyUsed(string area, SettingScope scope = SettingScope.User, List<MostRecentlyUsed> existingList = null)
        {
            // If users did not register a special MRU serializer by the time we run this, we will register the default one
            if (!RegisteredSerializers.OfType<MostRecentlyUsedListSerializer>().Any()) RegisterSerializer<MostRecentlyUsedListSerializer>();

            if (existingList == null) existingList = new List<MostRecentlyUsed>();
            LoadSettings(existingList, area, scope);
            return existingList.OrderByDescending(i => i.Timestamp).ToList();
        }

        /// <summary>
        /// Provides details information about the last error that occurred during a save operation
        /// </summary>
        /// <remarks>
        /// Information gets cleared for every call to SaveSettings()
        /// </remarks>
        public static string LastSaveSettingsError { get; set; }

        /// <summary>
        /// Provides details information about the last error that occurred during a clear operation
        /// </summary>
        /// <remarks>
        /// Information gets cleared for every call to ClearSettings()
        /// </remarks>
        public static string LastClearSettingsError { get; set; }

        /// <summary>
        /// Provides details information about the last error that occurred during a load operation
        /// </summary>
        /// <remarks>
        /// Information gets cleared for every call to LoadSettings()
        /// </remarks>
        public static string LastLoadSettingsError { get; set; }

        /// <summary>
        /// Returns a standardized ID for the provided object and optional ID
        /// </summary>
        /// <param name="id">ID to be used</param>
        /// <param name="stateObject">State object to be handled</param>
        /// <returns>Standardized ID</returns>
        private static string GetSettingId(string id, object stateObject)
        {
            if (string.IsNullOrEmpty(id))
                id = stateObject.GetType().FullName;
            return id;
        }

        /// <summary>
        /// Finds a serializer that can handle the specified object
        /// </summary>
        /// <param name="stateObject">The object that needs to be serialized/deserialized</param>
        /// <param name="id">Key of the setting</param>
        /// <param name="scope">Scope of the setting</param>
        /// <returns></returns>
        private static List<ISettingsSerializer> GetSerializers(object stateObject, string id, SettingScope scope)
        {
            var matchingSerializers = RegisteredSerializers.Where(s => s.CanHandle(stateObject, id, scope)).ToList();
            if (matchingSerializers.Count < 1)
                matchingSerializers.Add(new DefaultSettingSerializer());
            return matchingSerializers;
        }

        /// <summary>
        /// Registered the specified serializer
        /// </summary>
        /// <remarks>
        /// Adds the new serializer to the bottom of the list of serializers
        /// </remarks>
        /// <param name="serializer">Serializer</param>
        public static void RegisterSerializer(ISettingsSerializer serializer)
        {
            if (RegisteredSerializers.Any(s => s.GetType() == serializer.GetType())) return; // Can only register each type of serializer once
            RegisteredSerializers.Add(serializer);
        }

        /// <summary>
        /// Checks if a serializer of the specified type has already been registered, and if not, Registered the specified serializer
        /// instantiates and registers a serializer of the specified type.
        /// </summary>
        /// <remarks>
        /// Adds the new serializer to the bottom of the list of serializers.
        /// Creates a default (parameterless) instance of the serializer.
        /// </remarks>
        /// <typeparam name="T">Serializer Type</typeparam>
        public static void RegisterSerializer<T>() where T : ISettingsSerializer, new()
        {
            if (!HasRegisteredSerializer<T>())
                RegisterSerializer(new T());
        }

        /// <summary>
        /// Removes all registered serializers from the list
        /// </summary>
        public static void ResetSerializers()
        {
            RegisteredSerializers.Clear();
        }

        /// <summary>
        /// Checks for the existence of the registered serializer, and, if found, removes it fromt he list.
        /// </summary>
        /// <param name="serializer">Serializer to remove</param>
        public static void UnregisterSerializer(ISettingsSerializer serializer)
        {
            if (RegisteredSerializers.Contains(serializer))
                RegisteredSerializers.Remove(serializer);
        }

        private static readonly List<ISettingsSerializer> RegisteredSerializers = new List<ISettingsSerializer>();
        private static ISettingsHandler _settingsHandlerWorkstation;
        private static ISettingsHandler _settingsHandlerWorkstationAndUser;
        private static ISettingsHandler _settingsHandlerUser;

        private static ISettingsHandler GetSettingsHandler(SettingScope scope)
        {
            switch (scope)
            {
                case SettingScope.User:
                    return _settingsHandlerUser;
                case SettingScope.WorkstationAndUser:
                    if (_settingsHandlerWorkstationAndUser == null) _settingsHandlerWorkstationAndUser = new WorkstationAndUserSettingsHandler();
                    return _settingsHandlerWorkstationAndUser;
                default:
                    if (_settingsHandlerWorkstation == null) _settingsHandlerWorkstation = new WorkstationSettingsHandler();
                    return _settingsHandlerWorkstation;
            }
        }

        /// <summary>
        /// Registers handler (the object that loads and saves state) for a specific setting scope.
        /// </summary>
        /// <param name="handler">Handler object</param>
        /// <param name="scope">Scope</param>
        public static void RegisterSettingsHandler(ISettingsHandler handler, SettingScope scope)
        {
            switch (scope)
            {
                case SettingScope.User:
                    _settingsHandlerUser = handler;
                    break;
                case SettingScope.WorkstationAndUser:
                    _settingsHandlerWorkstationAndUser = handler;
                    break;
                case SettingScope.Workstation:
                    _settingsHandlerWorkstation = handler;
                    break;
            }
        }

        /// <summary>
        /// Indicates whether a serializer of the specified type has already been registered
        /// </summary>
        /// <typeparam name="T">Serializer Type</typeparam>
        /// <returns><c>true</c> if [has registered serializer]; otherwise, <c>false</c>.</returns>
        public static bool HasRegisteredSerializer<T>()
        {
            return RegisteredSerializers.Any(s => s.GetType() == typeof (T));
        }
    }

    /// <summary>
    /// Event arguments used whenever the MRU list changes
    /// </summary>
    /// <seealso cref="System.EventArgs" />
    public class MostRecentlyUsedEventArgs : EventArgs
    {
        /// <summary>
        /// Area (such as "Invoice")
        /// </summary>
        /// <value>The area.</value>
        public string Area { get; set; }

        /// <summary>
        /// Scope (User, Workstation,...)
        /// </summary>
        /// <value>The scope.</value>
        public SettingScope Scope { get; set; }

        /// <summary>
        /// The last MRU item added to the list
        /// </summary>
        /// <value>The last item added.</value>
        public MostRecentlyUsed LastItemAdded { get; set; }

        /// <summary>
        /// Complete list of MRU items in this area, including the changed ones
        /// </summary>
        /// <value>The complete list.</value>
        public List<MostRecentlyUsed> CompleteList { get; set; }
    }

    /// <summary>
    /// Provides all the functionality required to serialize and deserialize settings
    /// </summary>
    public interface ISettingsSerializer
    {
        /// <summary>
        /// Serializes the state object and returns the state as JSON
        /// </summary>
        /// <param name="stateObject">Object to serialize</param>
        /// <returns>State JSON</returns>
        string SerializeToJson(object stateObject);

        /// <summary>
        /// Deserializes the provides JSON state and updates the state object with the settings
        /// </summary>
        /// <param name="stateObject">Object to set the persisted state on.</param>
        /// <param name="state">State information (JSON)</param>
        void DeserializeFromJson(object stateObject, string state);

        /// <summary>
        /// Returns true if the provided serializer can handle the object in question
        /// </summary>
        /// <param name="stateObject">Object containing the state</param>
        /// <param name="id">ID of the setting that is to be persisted</param>
        /// <param name="scope">Scope of the setting</param>
        /// <returns>True if the serializer can handle the provided object</returns>
        bool CanHandle(object stateObject, string id, SettingScope scope);

        /// <summary>
        /// Can be used to suggest a file name for the setting, in case the handler is file-based
        /// </summary>
        /// <param name="stateObject">Object containing the state</param>
        /// <param name="id">ID of the setting that is to be persisted</param>
        /// <param name="scope">Scope of the setting</param>
        /// <returns>File name, or string.Empty if no default is suggested</returns>
        string GetSuggestedFileName(object stateObject, string id, SettingScope scope);

        /// <summary>
        /// If set to true, this serializer will be invoked, even if other serializers have already 
        /// handled the process
        /// </summary>
        /// <value>True or False</value>
        bool UseInAdditionToOtherAppliedSerializers { get; }
    }

    /// <summary>
    /// Objects implementing this interface can persist (save and load) settings
    /// </summary>
    public interface ISettingsHandler
    {
        /// <summary>
        /// Saves the specified state
        /// </summary>
        /// <param name="state">State to persist (typically JSON)</param>
        /// <param name="id">ID of the state</param>
        /// <param name="suggestedFileName">Potentially suggested file name created by a serializer (may be blank)</param>
        /// <returns>True if successful</returns>
        bool Save(string state, string id, string suggestedFileName);

        /// <summary>
        /// Loads the specified state and returns it as a string (typically JSON)
        /// </summary>
        /// <param name="id">ID of the setting</param>
        /// <param name="suggestedFileName">Potentially suggested file name created by a serializer (may be blank)</param>
        /// <returns>State (typically JSON). If setting is not found, returns an empty string.</returns>
        string Load(string id, string suggestedFileName);

        /// <summary>
        /// Clears the specified settings.
        /// </summary>
        /// <param name="id">ID of the setting</param>
        /// <param name="suggestedFileName">Potentially suggested file name created by a serializer (may be blank)</param>
        /// <returns>True if successful</returns>
        bool Clear(string id, string suggestedFileName);
    }

    /// <summary>
    /// Scope of the setting (such as settings that are specific to a workstation or travel with a user no matter where they log on)
    /// </summary>
    public enum SettingScope
    {
        /// <summary>
        /// The setting is user-specific, no matter which workstation they log on at
        /// </summary>
        User,

        /// <summary>
        /// The setting is specific to the current workstation, no matter the user
        /// </summary>
        Workstation,

        /// <summary>
        /// The setting is workstation-specific, but varies by user
        /// </summary>
        WorkstationAndUser
    }

    /// <summary>
    /// Standard serializer that attempts to handle all objects
    /// </summary>
    public class DefaultSettingSerializer : ISettingsSerializer
    {
        /// <summary>
        /// Serializes the state object and returns the state as JSON
        /// </summary>
        /// <param name="stateObject">Object to serialize</param>
        /// <returns>State JSON</returns>
        public virtual string SerializeToJson(object stateObject)
        {
            return JsonConvert.SerializeObject(stateObject);
        }

        /// <summary>
        /// Deserializes the provides JSON state and updates the state object with the settings
        /// </summary>
        /// <param name="stateObject">Object to set the persisted state on.</param>
        /// <param name="state">State information (JSON)</param>
        public virtual void DeserializeFromJson(object stateObject, string state)
        {
            JsonConvert.PopulateObject(state, stateObject);
        }

        /// <summary>
        /// Returns true if the provided serializer can handle the object in question
        /// </summary>
        /// <param name="stateObject">Object containing the state</param>
        /// <param name="id">ID of the setting that is to be persisted</param>
        /// <param name="scope">Scope of the setting</param>
        /// <returns>True if the serializer can handle the provided object</returns>
        public virtual bool CanHandle(object stateObject, string id, SettingScope scope)
        {
            return true; // We at least attempt to handle everything that comes our way
        }

        /// <summary>
        /// Can be used to suggest a file name for the setting, in case the handler is file-based
        /// </summary>
        /// <param name="stateObject">Object containing the state</param>
        /// <param name="id">ID of the setting that is to be persisted</param>
        /// <param name="scope">Scope of the setting</param>
        /// <returns>File name, or string.Empty if no default is suggested</returns>
        public virtual string GetSuggestedFileName(object stateObject, string id, SettingScope scope)
        {
            return string.Empty;
        }

        /// <summary>
        /// If set to true, this serializer will be invoked, even if other serializers have already
        /// handled the process
        /// </summary>
        /// <value>True or False</value>
        public virtual bool UseInAdditionToOtherAppliedSerializers
        {
            get { return false; }
        }
    }

    /// <summary>
    /// Base class used to create classes that serialize based on attributes associated with objects
    /// </summary>
    /// <seealso cref="CODE.Framework.Core.Utilities.ISettingsSerializer" />
    public abstract class AttributeSettingSerializer<T> : ISettingsSerializer where T:Attribute
    {
        /// <summary>
        /// Serializes the state object and returns the state as JSON
        /// </summary>
        /// <param name="stateObject">Object to serialize</param>
        /// <returns>State JSON</returns>
        public string SerializeToJson(object stateObject)
        {
            var properties = GetPropertiesToSerialize(stateObject);
            if (properties.Count < 1) return string.Empty;
            var jb = new JsonBuilder();
            foreach (var property in properties)
                jb.Append(property.Name, property.GetValue(stateObject, null));
            return jb.ToString();
        }

        /// <summary>
        /// Deserializes the provides JSON state and updates the state object with the settings
        /// </summary>
        /// <param name="stateObject">Object to set the persisted state on.</param>
        /// <param name="state">State information (JSON)</param>
        public void DeserializeFromJson(object stateObject, string state)
        {
            if (string.IsNullOrEmpty(state) || stateObject == null) return;
            var properties = GetPropertiesToSerialize(stateObject);
            if (properties.Count < 1) return;

            JsonHelper.QuickParse(state, (n, v) =>
            {
                var property = properties.FirstOrDefault(p => p.Name == n);
                if (property == null) return;

                if (property.PropertyType == typeof (string))
                    property.SetValue(stateObject, v, null);
                else if (property.PropertyType == typeof(bool))
                    property.SetValue(stateObject, v.ToLower() == "true", null);
                else if (property.PropertyType == typeof (int))
                {
                    int value;
                    if (int.TryParse(v, out value))
                        property.SetValue(stateObject, value, null);
                }
                else if (property.PropertyType == typeof(decimal))
                {
                    decimal value;
                    if (decimal.TryParse(v, out value))
                        property.SetValue(stateObject, value, null);
                }
                else if (property.PropertyType == typeof (double))
                {
                    double value;
                    if (double.TryParse(v, out value))
                        property.SetValue(stateObject, value, null);
                }
                else if (property.PropertyType == typeof(Guid))
                {
                    Guid value;
                    if (Guid.TryParse(v, out value))
                        property.SetValue(stateObject, value, null);
                }
                else if (property.PropertyType == typeof(Guid?))
                {
                    if (string.IsNullOrEmpty(v))
                        property.SetValue(stateObject, null, null);
                    else
                    {
                        Guid value;
                        if (Guid.TryParse(v, out value))
                        {
                            var value2 = new Guid?(value);
                            property.SetValue(stateObject, value2, null);
                        }
                    }
                }
                else if (property.PropertyType == typeof(DateTime))
                {
                    DateTime value;
                    if (DateTimeUtils.TryParseDateTimeIso(new StringReference(v.ToCharArray(), 0, v.Length), DateTimeZoneHandling.Unspecified, out value))
                        property.SetValue(stateObject, value, null);
                }
                else
                    throw new NotSupportedException("Type " + property.PropertyType + " not supported for member setting serialization");
            });
        }

        /// <summary>
        /// Returns true if the provided serializer can handle the object in question
        /// </summary>
        /// <param name="stateObject">Object containing the state</param>
        /// <param name="id">ID of the setting that is to be persisted</param>
        /// <param name="scope">Scope of the setting</param>
        /// <returns>True if the serializer can handle the provided object</returns>
        public abstract bool CanHandle(object stateObject, string id, SettingScope scope);

        /// <summary>
        /// Can be used to suggest a file name for the setting, in case the handler is file-based
        /// </summary>
        /// <param name="stateObject">Object containing the state</param>
        /// <param name="id">ID of the setting that is to be persisted</param>
        /// <param name="scope">Scope of the setting</param>
        /// <returns>File name, or string.Empty if no default is suggested</returns>
        public virtual string GetSuggestedFileName(object stateObject, string id, SettingScope scope)
        {
            return id + "." + typeof(T).Name + ".json";
        }

        /// <summary>
        /// If set to true, this serializer will be invoked, even if other serializers have already
        /// handled the process
        /// </summary>
        /// <value>True or False</value>
        public bool UseInAdditionToOtherAppliedSerializers { get { return true; } }

        /// <summary>
        /// Returns a list of all the properties that need to be serialized
        /// </summary>
        /// <param name="stateObject">The state object.</param>
        /// <returns>List of PropertyInfo.</returns>
        protected virtual List<PropertyInfo> GetPropertiesToSerialize(object stateObject)
        {
            var propertyList = new List<PropertyInfo>();
            var stateObjectType = stateObject.GetType();
            var properties = stateObjectType.GetProperties();
            foreach (var property in properties)
            {
                var attributes = property.GetCustomAttributes(typeof(T), true);
                if (attributes.Length > 0) propertyList.Add(property);
            }
            return propertyList;
        }
    }

    /// <summary>
    /// This serializer inspects an object for all properties decorated with a WorkstationSetting attribute and
    /// preserves their value as local workstation settings.
    /// </summary>
    public class WorkstationAttributeSettingSerializer : AttributeSettingSerializer<WorkstationSettingAttribute>
    {
        /// <summary>
        /// Returns true if the provided serializer can handle the object in question
        /// </summary>
        /// <param name="stateObject">Object containing the state</param>
        /// <param name="id">ID of the setting that is to be persisted</param>
        /// <param name="scope">Scope of the setting</param>
        /// <returns>True if the serializer can handle the provided object</returns>
        public override bool CanHandle(object stateObject, string id, SettingScope scope)
        {
            return stateObject != null && scope == SettingScope.Workstation;
        }
    }

    /// <summary>
    /// This serializer inspects an object for all properties decorated with a UserSetting attribute and
    /// preserves their value as user settings.
    /// </summary>
    public class UserAttributeSettingSerializer : AttributeSettingSerializer<UserSettingAttribute>
    {
        /// <summary>
        /// Returns true if the provided serializer can handle the object in question
        /// </summary>
        /// <param name="stateObject">Object containing the state</param>
        /// <param name="id">ID of the setting that is to be persisted</param>
        /// <param name="scope">Scope of the setting</param>
        /// <returns>True if the serializer can handle the provided object</returns>
        public override bool CanHandle(object stateObject, string id, SettingScope scope)
        {
            return stateObject != null && scope == SettingScope.User;
        }
    }

    /// <summary>
    /// This serializer inspects an object for all properties decorated with a UserSetting attribute and
    /// preserves their value as user settings.
    /// </summary>
    public class WorkstationUserAttributeSettingSerializer : AttributeSettingSerializer<WorkstationUserSettingAttribute>
    {
        /// <summary>
        /// Returns true if the provided serializer can handle the object in question
        /// </summary>
        /// <param name="stateObject">Object containing the state</param>
        /// <param name="id">ID of the setting that is to be persisted</param>
        /// <param name="scope">Scope of the setting</param>
        /// <returns>True if the serializer can handle the provided object</returns>
        public override bool CanHandle(object stateObject, string id, SettingScope scope)
        {
            return stateObject != null && scope == SettingScope.WorkstationAndUser;
        }
    }

    /// <summary>
    /// This attribute can be used to identify properties that shall be serialized as workstation settings
    /// </summary>
    /// <seealso cref="System.Attribute" />
    [AttributeUsage(AttributeTargets.Property)]
    public class WorkstationSettingAttribute : Attribute { }

    /// <summary>
    /// This attribute can be used to identify properties that shall be serialized as user settings
    /// </summary>
    /// <seealso cref="System.Attribute" />
    [AttributeUsage(AttributeTargets.Property)]
    public class UserSettingAttribute : Attribute { }

    /// <summary>
    /// This attribute can be used to identify properties that shall be serialized as workstation settings per user
    /// </summary>
    /// <seealso cref="System.Attribute" />
    [AttributeUsage(AttributeTargets.Property)]
    public class WorkstationUserSettingAttribute : Attribute { }

    /// <summary>
    /// Settings handler used to save settings in local machine files
    /// </summary>
    /// <seealso cref="CODE.Framework.Core.Utilities.ISettingsHandler" />
    public class WorkstationSettingsHandler : ISettingsHandler
    {
        /// <summary>
        /// Saves the specified state
        /// </summary>
        /// <param name="state">State to persist (typically JSON)</param>
        /// <param name="id">ID of the state</param>
        /// <param name="suggestedFileName">Potentially suggested file name created by a serializer (may be blank)</param>
        /// <returns>True if successful</returns>
        public virtual bool Save(string state, string id, string suggestedFileName)
        {
            var path = GetPath();
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            var fileName = path + GetFileName(id, suggestedFileName);
            state.ToFile(fileName);
            return true;
        }

        /// <summary>
        /// Loads the specified state and returns it as a string (typically JSON)
        /// </summary>
        /// <param name="id">ID of the setting</param>
        /// <param name="suggestedFileName">Potentially suggested file name created by a serializer (may be blank)</param>
        /// <returns>State (typically JSON). If setting is not found, returns an empty string.</returns>
        public virtual string Load(string id, string suggestedFileName)
        {
            var fileName = GetPath() + GetFileName(id, suggestedFileName);
            if (File.Exists(fileName))
                return StringHelper.FromFile(fileName);
            return string.Empty;
        }

        /// <summary>
        /// Clears the specified settings.
        /// </summary>
        /// <param name="id">ID of the setting</param>
        /// <param name="suggestedFileName">Potentially suggested file name created by a serializer (may be blank)</param>
        /// <returns>True if successful</returns>
        public bool Clear(string id, string suggestedFileName)
        {
            var fileName = GetPath() + GetFileName(id, suggestedFileName);
            if (File.Exists(fileName))
                File.Delete(fileName);
            return true;
        }

        /// <summary>
        /// Returns the path to where settings for this app and workstation are to be stored
        /// </summary>
        /// <returns>Path</returns>
        protected virtual string GetPath()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            var subFolder = AppDataSubFolder;
            if (string.IsNullOrEmpty(subFolder))
                subFolder = AppDomain.CurrentDomain.FriendlyName;
            var path = (appDataPath.AddBS() + subFolder).AddBS();
            return path;
        }

        /// <summary>
        /// Name of the folder within Environment.SpecialFolder.CommonApplicationData where the files for these settings are to be saved
        /// </summary>
        /// <value>The application data sub folder.</value>
        public static string AppDataSubFolder { get; set; }

        /// <summary>
        /// Returns a file name for the specified settings ID
        /// </summary>
        /// <param name="id">Settings ID</param>
        /// <param name="suggestedFileName">Potentially suggested file name by the caller</param>
        /// <returns>File name</returns>
        protected virtual string GetFileName(string id, string suggestedFileName)
        {
            if (!string.IsNullOrEmpty(suggestedFileName))
            {
                suggestedFileName = suggestedFileName.Trim();
                if (!suggestedFileName.ToLower().EndsWith(".json"))
                    suggestedFileName += ".json";
                return suggestedFileName;
            }
            if (string.IsNullOrEmpty(id)) return "Unknown.json";
            return id.Trim() + ".json";
        }
    }

    /// <summary>
    /// Base implementation of a settings handler with standardized access to the current user name
    /// </summary>
    /// <seealso cref="CODE.Framework.Core.Utilities.ISettingsHandler" />
    public abstract class UserSettingsHandlerBase : ISettingsHandler
    {
        /// <summary>
        /// Returns a standardized version of the current user name
        /// </summary>
        public virtual string UserName
        {
            get
            {
                var username = string.Empty;
                if (Thread.CurrentPrincipal != null && Thread.CurrentPrincipal.Identity != null && !string.IsNullOrEmpty(Thread.CurrentPrincipal.Identity.Name))
                    username = Thread.CurrentPrincipal.Identity.Name.ToLower();
                if (string.IsNullOrEmpty(username))
                {
                    var windowsIdentity = WindowsIdentity.GetCurrent();
                    if (windowsIdentity != null)
                        username = windowsIdentity.Name;
                }

                if (!string.IsNullOrEmpty(username))
                    return username;

                return "Anonymous";
            }
        }

        /// <summary>
        /// Saves the specified state
        /// </summary>
        /// <param name="state">State to persist (typically JSON)</param>
        /// <param name="id">ID of the state</param>
        /// <param name="suggestedFileName">Potentially suggested file name created by a serializer (may be blank)</param>
        /// <returns>True if successful</returns>
        public abstract bool Save(string state, string id, string suggestedFileName);

        /// <summary>
        /// Loads the specified state and returns it as a string (typically JSON)
        /// </summary>
        /// <param name="id">ID of the setting</param>
        /// <param name="suggestedFileName">Potentially suggested file name created by a serializer (may be blank)</param>
        /// <returns>State (typically JSON). If setting is not found, returns an empty string.</returns>
        public abstract string Load(string id, string suggestedFileName);

        /// <summary>
        /// Clears the specified settings.
        /// </summary>
        /// <param name="id">ID of the setting</param>
        /// <param name="suggestedFileName">Potentially suggested file name created by a serializer (may be blank)</param>
        /// <returns>True if successful</returns>
        public abstract bool Clear(string id, string suggestedFileName);
    }

    /// <summary>
    /// Standard settings handler that saves the specified settings on the local workstation
    /// separated by user.
    /// </summary>
    public class WorkstationAndUserSettingsHandler : WorkstationSettingsHandler
    {
        /// <summary>
        /// Returns a standardized version of the current user name
        /// </summary>
        public virtual string UserName
        {
            get
            {
                var username = string.Empty;
                if (Thread.CurrentPrincipal != null && Thread.CurrentPrincipal.Identity != null && !string.IsNullOrEmpty(Thread.CurrentPrincipal.Identity.Name))
                    username = Thread.CurrentPrincipal.Identity.Name.ToLower();
                if (string.IsNullOrEmpty(username))
                {
                    var windowsIdentity = WindowsIdentity.GetCurrent();
                    if (windowsIdentity != null)
                        username = windowsIdentity.Name;
                }

                if (!string.IsNullOrEmpty(username))
                    return username;

                return "Anonymous";
            }
        }

        /// <summary>
        /// Returns the path to where settings for this app and workstation are to be stored
        /// </summary>
        /// <returns>Path</returns>
        protected override string GetPath()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            var subFolder = AppDataSubFolder;
            if (string.IsNullOrEmpty(subFolder))
                subFolder = AppDomain.CurrentDomain.FriendlyName;
            var path = (appDataPath.AddBS() + UserName.AddBS() + subFolder).AddBS();
            return path;
        }
    }

    /// <summary>
    /// Handles MRU (most-recently-used) lists
    /// </summary>
    public class MostRecentlyUsedListSerializer : ISettingsSerializer
    {
        /// <summary>
        /// Initializes static members of the <see cref="MostRecentlyUsedListSerializer"/> class.
        /// </summary>
        static MostRecentlyUsedListSerializer()
        {
            MaxItemCount = 20;
        }

        /// <summary>
        /// Maximum number of items stored in MRU lists
        /// </summary>
        /// <value>The maximum item count.</value>
        public static int MaxItemCount { get; set; }

        /// <summary>
        /// Serializes the state object and returns the state as JSON
        /// </summary>
        /// <param name="stateObject">Object to serialize</param>
        /// <returns>State JSON</returns>
        public string SerializeToJson(object stateObject)
        {
            var list = stateObject as List<MostRecentlyUsed>;
            if (list == null) return string.Empty;

            var mostRecentItems = list.OrderByDescending(i => i.Timestamp).Take(MaxItemCount).ToList();
            var json = JsonHelper.SerializeToRestJson(mostRecentItems);
            json = JsonHelper.Format(json);

            if (list.Count > MaxItemCount) // If there are too many items in the list, we will actually modify the original list in case it is used later (which it likely is)
            {
                list.Clear();
                list.AddRange(mostRecentItems);
            }

            return json;
        }

        /// <summary>
        /// Deserializes the provides JSON state and updates the state object with the settings
        /// </summary>
        /// <param name="stateObject">Object to set the persisted state on.</param>
        /// <param name="state">State information (JSON)</param>
        public void DeserializeFromJson(object stateObject, string state)
        {
            var list = stateObject as List<MostRecentlyUsed>;
            if (list == null) return;

            var list2 = JsonHelper.DeserializeFromRestJson<List<MostRecentlyUsed>>(state);
            list.Clear();
            if (list2 != null) list.AddRange(list2);
        }

        /// <summary>
        /// Returns true if the provided serializer can handle the object in question
        /// </summary>
        /// <param name="stateObject">Object containing the state</param>
        /// <param name="id">ID of the setting that is to be persisted</param>
        /// <param name="scope">Scope of the setting</param>
        /// <returns>True if the serializer can handle the provided object</returns>
        public bool CanHandle(object stateObject, string id, SettingScope scope)
        {
            return stateObject is List<MostRecentlyUsed>;
        }

        /// <summary>
        /// Can be used to suggest a file name for the setting, in case the handler is file-based
        /// </summary>
        /// <param name="stateObject">Object containing the state</param>
        /// <param name="id">ID of the setting that is to be persisted</param>
        /// <param name="scope">Scope of the setting</param>
        /// <returns>File name, or string.Empty if no default is suggested</returns>
        public string GetSuggestedFileName(object stateObject, string id, SettingScope scope)
        {
            return id + ".mru.json";
        }

        /// <summary>
        /// If set to true, this serializer will be invoked, even if other serializers have already
        /// handled the process
        /// </summary>
        /// <value>True or False</value>
        public bool UseInAdditionToOtherAppliedSerializers
        {
            get { return true; }
        }
    }

    /// <summary>
    /// Standard implementation for an MRU item
    /// </summary>
    public class MostRecentlyUsed 
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MostRecentlyUsed"/> class.
        /// </summary>
        public MostRecentlyUsed()
        {
            Timestamp = DateTime.Now;
            Data = new Dictionary<string, string>();
        }

        /// <summary>
        /// Time the item was last used
        /// </summary>
        /// <value>The time stamp.</value>
        public DateTime Timestamp { get; set; }
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
        public string Id { get; set; }
        /// <summary>
        /// Main title of the item
        /// </summary>
        /// <value>The title.</value>
        public string Title { get; set; }
        /// <summary>
        /// Other data associated with the MRU item
        /// </summary>
        /// <value>The data.</value>
        public Dictionary<string, string> Data { get; set; }
    }
}

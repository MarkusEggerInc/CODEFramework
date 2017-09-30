using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace CODE.Framework.Core.Utilities
{
    /// <summary>
    /// Class used to map two contracts
    /// </summary>
    public class Mapper
    {
        /// <summary>
        /// Internal list of defined maps
        /// </summary>
        private List<Map> _maps = new List<Map>();

        /// <summary>
        /// A list of all maps.
        /// </summary>
        /// <value>
        /// The maps.
        /// </value>
        public List<Map> Maps
        {
            get { return _maps; }
            set { _maps = value; }
        }

        /// <summary>
        /// Provides a list of default maps as typically used by Milos
        /// </summary>
        /// <remarks>
        /// The following maps are included:
        /// PK -> Id
        /// PKString -> Id
        /// PKInteger -> Id
        /// </remarks>
        /// <value>The Milos default maps.</value>
        public static List<Map> MilosDefaultMaps
        {
            get
            {
                var maps = new List<Map>
                {
                    new Map("PK", "Id"),
                    new Map("PKString", "Id"),
                    new Map("PKInteger", "Id")
                };
                return maps;
            }
        }

        /// <summary>
        /// Adds the map.
        /// </summary>
        /// <param name="sourceField">The source field.</param>
        /// <param name="destinationField">The destination field.</param>
        public void AddMap(string sourceField, string destinationField)
        {
            _maps.Add(new Map(sourceField, destinationField));
        }

        /// <summary>
        /// Adds a list of maps to the mappings
        /// </summary>
        /// <param name="maps">The maps.</param>
        public void AddMaps(List<Map> maps)
        {
            foreach (var map in maps)
                _maps.Add(map);
        }

        /// <summary>
        /// Attempts to fill all the read/write properties in the destination object
        /// from the properties int he source object
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="destination">The destination.</param>
        /// <remarks>
        /// This is identical to the MapObjects() instance method.
        /// This method is simply provided for convenience.
        /// </remarks>
        /// <returns></returns>
        public static bool Map(object source, object destination)
        {
            var mapper = new Mapper();
            return mapper.MapObjects(source, destination);
        }

        /// <summary>
        /// Attempts to fill all the read/write properties in the destination object
        /// from the properties int he source object
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="destination">The destination.</param>
        /// <param name="options">The options.</param>
        /// <remarks>
        /// This is identical to the MapObjects() instance method.
        /// This method is simply provided for convenience.
        /// </remarks>
        /// <returns>True if map operation is a success</returns>
        public static bool Map(object source, object destination, MappingOptions options)
        {
            var mapper = new Mapper();
            return mapper.MapObjects(source, destination, options);
        }

        /// <summary>
        /// Attempts to fill all the read/write properties in the destination object
        /// from the properties int he source object
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="destination">The destination.</param>
        /// <returns>True if successful</returns>
        public bool MapObjects(object source, object destination)
        {
            return MapObjects(source, destination, new MappingOptions());
        }

        /// <summary>
        /// Attempts to fill all the read/write properties in the destination object
        /// from the properties int he source object
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="destination">The destination.</param>
        /// <param name="options">The options.</param>
        /// <returns>True if successful</returns>
        public bool MapObjects(object source, object destination, MappingOptions options)
        {
            var oldMaps = _maps;
            if (options.Maps != null)
                _maps = options.Maps;

            var retVal = true;
            try
            {
                var sourceType = source.GetType();
                var properties = destination.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
                foreach (var property in properties)
                    if (property.CanRead && property.CanWrite && !options.ExcludedFields.Contains(property.Name))
                    {
                        // looks like we have one we are interested in
                        var sourcePropertyName = GetMappedSourcePropertyName(property.Name, options.MapDirection);
                        if (options.ExcludedFields.Contains(sourcePropertyName)) continue;
                        var sourceProperty = sourceType.GetProperty(sourcePropertyName, BindingFlags.Public | BindingFlags.Instance);
                        if (sourceProperty == null) continue;
                        if (sourceProperty.CanRead && property.CanWrite)
                            if (options.MapFunctionsFiltered.ContainsKey(sourceProperty.Name) && options.MapFunctionsFiltered[property.Name](source, destination, options.MapDirection))
                                continue; // Successfully mapped with a function filtered to this property
                            else if (sourceProperty.PropertyType == property.PropertyType)
                                try
                                {
                                    var currentValue = property.GetValue(destination, null);
                                    var newValue = sourceProperty.GetValue(source, null);
                                    UpdateValueIfNeeded(destination, property, currentValue, newValue);
                                }
                                catch
                                {
                                } // Nothing we can do, really
                            else if (sourceProperty.PropertyType.Name == "Nullable`1" && !property.PropertyType.Name.StartsWith("Nullable"))
                                try
                                {
                                    // The source is nullable, while the destination is not. This could be problematic, but we give it a try
                                    var newValue = sourceProperty.GetValue(source, null);
                                    if (newValue != null) // Can't set it if it is null.
                                        property.SetValue(destination, newValue, null);
                                }
                                catch
                                {
                                } // Nothing we can do, really
                            else if (property.PropertyType.Name == "Nullable`1" && !sourceProperty.PropertyType.Name.StartsWith("Nullable"))
                                try
                                {
                                    // The destination is nullable although the source isn't. This has a pretty good chance of working
                                    var newValue = sourceProperty.GetValue(source, null);
                                    property.SetValue(destination, newValue, null);
                                }
                                catch
                                {
                                } // Nothing we can do, really
                            else
                            // Property types differ, but maybe we can still map them
                                if (options.MapEnums)
                                    if (sourceProperty.PropertyType.IsEnum && property.PropertyType.IsEnum)
                                        MapEnumValues(source, destination, property, sourceProperty, options);
                                    else if (PropertyTypeIsNumeric(sourceProperty.PropertyType) && property.PropertyType.IsEnum)
                                        MapNumericToEnum(source, destination, property, sourceProperty);
                                    else if (sourceProperty.PropertyType.IsEnum && PropertyTypeIsNumeric(property.PropertyType))
                                        MapEnumToNumeric(source, destination, property, sourceProperty);
                    }
            }
            catch
            {
                retVal = false;
            }

            // We also respect potential delegates
            if (retVal)
                foreach (var function in options.MapFunctions)
                    if (!function(source, destination, options.MapDirection))
                    {
                        retVal = false;
                        break;
                    }

            _maps = oldMaps;

            return retVal;
        }

        /// <summary>
        /// Returns true if the property's type is some numeric type
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        protected virtual bool PropertyTypeIsNumeric(Type type)
        {
            return type == typeof (Byte)
                   || type == typeof (SByte)
                   || type == typeof (Int16)
                   || type == typeof (UInt16)
                   || type == typeof (Int32)
                   || type == typeof (UInt32)
                   || type == typeof (Int64)
                   || type == typeof (UInt64)
                   || type == typeof (Single)
                   || type == typeof (Double)
                   || type == typeof (Decimal);
        }

        /// <summary>
        /// Maps collections and all the objects in the collection
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="destination">The destination.</param>
        /// <param name="keyField">The source key field.</param>
        /// <param name="destinationItemType">Type of the items to be added to the destination collection.</param>
        /// <returns></returns>
        public static bool Collections(IList source, IList destination, string keyField, Type destinationItemType)
        {
            var mapper = new Mapper();
            return mapper.MapCollections(source, destination, keyField, destinationItemType, new MappingOptions());
        }

        /// <summary>
        /// Maps collections and all the objects in the collection
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="destination">The destination.</param>
        /// <param name="keyField">The source key field.</param>
        /// <param name="destinationItemType">Type of the items to be added to the destination collection.</param>
        /// <param name="options">The options.</param>
        /// <returns></returns>
        public static bool Collections(IList source, IList destination, string keyField, Type destinationItemType, MappingOptions options)
        {
            var mapper = new Mapper();
            return mapper.MapCollections(source, destination, keyField, destinationItemType, options);
        }

        /// <summary>
        /// Maps any enumerable to a Milos Business Entity Sub Item collection
        /// </summary>
        /// <remarks>
        /// Can also map any other collection that has a simple parameterless Add() method that 
        /// returns a new object. And each item object needs to have a PK or PK_Integer or PK_String
        /// property (matching the type on the source). The source also needs to have a key property
        /// that maps to PK. Typically, this property is called "Id".
        /// That this method can fail at runtime if the destination type doesn't fulfill that criteria at runtime.
        /// </remarks>
        /// <param name="source">Any enumerable source</param>
        /// <param name="destination">Destination collection (weakly typed, but checked for the correct Add() method)</param>
        /// <param name="options">Mapping options</param>
        /// <returns>True if the operation is a success</returns>
        public bool EnumerableToBusinessItemCollection(IEnumerable source, IEnumerable destination, MappingOptions options)
        {
            if (source == null) return false;
            if (destination == null) return false;

            var oldMaps = _maps;
            if (options.Maps != null) _maps = options.Maps;

            const BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public;
            var keyType = GetKeyTypeForObject(source);
            var sourceKeyField = GetSourceKeyField(keyType);
            var destinationKeyField = GetMilosKeyField(keyType);

            // We make sure the destination collection has all the items we had in the source)
            var sourceItems = source as object[] ?? source.Cast<object>().ToArray();
            var destinationCheckItems = destination as object[] ?? destination.Cast<object>().ToArray();
            foreach (var sourceItem in sourceItems)
            {
                var sourceKeyProperty = sourceItem.GetType().GetProperty(sourceKeyField, bindingFlags);
                if (sourceKeyProperty == null) continue;
                var sourceKeyValue = sourceKeyProperty.GetValue(sourceItem, null) as IComparable;

                object destinationItem = null;
                foreach (var destinationCheckItem in destinationCheckItems)
                {
                    var destinationKeyProperty = destinationCheckItem.GetType().GetProperty(destinationKeyField, bindingFlags);
                    if (destinationKeyProperty == null) continue;
                    var destinationKeyValue = destinationKeyProperty.GetValue(destinationCheckItem, null) as IComparable;
                    if (sourceKeyValue != null && destinationKeyValue != null)
                        if (destinationKeyValue.CompareTo(sourceKeyValue) == 0)
                        {
                            destinationItem = destinationCheckItem;
                            break;
                        }
                }
                if (destinationItem == null) // Not found yet, so we have to add it
                {
                    var addMethod = destination.GetType().GetMethod("Add", bindingFlags);
                    if (addMethod != null) destinationItem = addMethod.Invoke(destination, null);
                }
                if (destinationItem == null) continue;
                MapObjects(sourceItem, destinationItem, options);
                // The PK field can not be mapped easily, so we have to run this extra code
                SetPrimaryKeyValue(sourceItem, destinationItem, sourceKeyField);
            }

            // We need to make sure the destination doesn't have any items the source doesn't have
            var removalCounter = -1;
            var removalIndexes = new List<int>();
            foreach (var destinationItem in destinationCheckItems)
            {
                removalCounter++;
                var destinationKeyProperty = destinationItem.GetType().GetProperty(destinationKeyField, bindingFlags);
                if (destinationKeyProperty == null) continue;
                var destinationKeyValue = destinationKeyProperty.GetValue(destinationItem, null) as IComparable;
                var foundInSource = false;
                foreach (var sourceItem in sourceItems)
                {
                    var sourceKeyProperty = sourceItem.GetType().GetProperty(sourceKeyField, bindingFlags);
                    if (sourceKeyProperty == null) continue;
                    var sourceKeyValue = sourceKeyProperty.GetValue(sourceItem, null) as IComparable;
                    if (sourceKeyValue != null && destinationKeyValue != null)
                        if (destinationKeyValue.CompareTo(sourceKeyValue) == 0)
                        {
                            foundInSource = true;
                            break;
                        }
                }
                if (!foundInSource) // This item has no business being in the destination
                    removalIndexes.Add(removalCounter);
            }
            // Ready to remove the items starting from the rear forward, so indexes don't change as we go
            for (var removalCounter2 = removalIndexes.Count - 1; removalCounter2 > -1; removalCounter2--)
            {
                var removeMethod = destination.GetType().GetMethod("Remove", bindingFlags);
                if (removeMethod.GetParameters().Length == 1)
                    removeMethod.Invoke(destination, new object[] {removalIndexes[removalCounter2]});
            }

            _maps = oldMaps;
            return true;
        }

        /// <summary>
        /// Sets the primary key value.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="destination">The destination.</param>
        /// <param name="sourceKeyFieldName">Name of the source key field.</param>
        protected virtual void SetPrimaryKeyValue(object source, object destination, string sourceKeyFieldName)
        {
            try
            {
                var destinationType = destination.GetType();

                var propIdField = source.GetType().GetProperty(sourceKeyFieldName, BindingFlags.Instance | BindingFlags.Public);
                var propPrimaryKeyField = destinationType.GetProperty("PrimaryKeyField", BindingFlags.Public | BindingFlags.Instance);
                var primaryKeyField = propPrimaryKeyField.GetValue(destination, null);
                var methods = destinationType.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance);

                var propItemState = destinationType.GetProperty("ItemState", BindingFlags.Public | BindingFlags.Instance);
                var itemState = (int) propItemState.GetValue(destination, null);
                if (itemState == 4)
                {
                    // New item. We should not map the key back as it would probably/potentially cause a violation with things that already exist
                    // However, this also causes the removal mechanism to kick in as the keys do not match. 
                    // To avoid that problem, we update the value on the source object
                    foreach (var method in methods)
                    {
                        if (method.Name != "GetFieldValue" || method.GetParameters().Length != 1) continue;
                        var pk = method.Invoke(destination, new[] {primaryKeyField});
                        propIdField.SetValue(source, pk, null);
                        break;
                    }
                    return;
                }

                var idValue = propIdField.GetValue(source, null);
                foreach (var method in methods)
                {
                    if (method.Name != "SetFieldValue" || method.GetParameters().Length != 2) continue;
                    method.Invoke(destination, new[] {primaryKeyField, idValue});
                    break;
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Attempts to find the key property name on a source object
        /// </summary>
        /// <param name="keyType">Key type for which to find the appropriate property</param>
        /// <returns>Field name</returns>
        protected virtual string GetSourceKeyField(MapKeyType keyType)
        {
            switch (keyType)
            {
                case MapKeyType.Guid:
                    return GetMappedSourcePropertyName("PK", MapDirection.Backward);
                case MapKeyType.Integer:
                    return GetMappedSourcePropertyName("PK_Integer", MapDirection.Backward);
                case MapKeyType.String:
                    return GetMappedSourcePropertyName("PK_String", MapDirection.Backward);
            }
            return "PK";
        }

        /// <summary>
        /// Returns the appropriate Milos key field name
        /// </summary>
        /// <param name="keyType">Key type</param>
        /// <returns>Key field name</returns>
        protected virtual string GetMilosKeyField(MapKeyType keyType)
        {
            switch (keyType)
            {
                case MapKeyType.Guid:
                    return "PK";
                case MapKeyType.Integer:
                    return "PK_Integer";
                case MapKeyType.String:
                    return "PK_String";
            }
            return "PK";
        }

        /// <summary>
        /// Attempts to find the key type for the provided object (which generally only works for Milos business objects)
        /// </summary>
        /// <param name="x"></param>
        /// <returns>Key type</returns>
        private static MapKeyType GetKeyTypeForObject(object x)
        {
            var type = x.GetType();
            var propertyInfo = type.GetProperty("ParentEntity", BindingFlags.Instance | BindingFlags.Public);
            if (propertyInfo != null)
                return GetKeyTypeForObject(propertyInfo.GetValue(x, null));

            var propertyInfo2 = type.GetProperty("PrimaryKeyType", BindingFlags.Instance | BindingFlags.Public);
            if (propertyInfo2 != null)
            {
                try
                {
                    var keyType = (int) propertyInfo2.GetValue(x, null);
                    if (keyType == 0) return MapKeyType.Guid;
                    if (keyType == 3) return MapKeyType.String;
                    return MapKeyType.Integer;
                }
                catch
                {
                }
            }

            return MapKeyType.Guid; // This is our best guess at this point
        }

        /// <summary>
        /// Maps collections and all the objects in the collection
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="destination">The destination.</param>
        /// <param name="keyField">The source key field.</param>
        /// <param name="destinationItemType">Type of the items to be added to the destination collection.</param>
        /// <returns></returns>
        public bool MapCollections(IList source, IList destination, string keyField, Type destinationItemType)
        {
            return MapCollections(source, destination, keyField, destinationItemType, new MappingOptions());
        }

        /// <summary>
        /// Maps collections and all the objets in the collection
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="destination">The destination.</param>
        /// <param name="keyField">The source key field.</param>
        /// <param name="destinationItemType">Type of the items to be added to the destination collection.</param>
        /// <param name="options">The options.</param>
        /// <returns></returns>
        public bool MapCollections(IList source, IList destination, string keyField, Type destinationItemType, MappingOptions options)
        {
            var oldMaps = _maps;
            if (options.Maps != null)
                _maps = options.Maps;

            var destinationKeyProperty = destinationItemType.GetProperty(keyField, BindingFlags.Public | BindingFlags.Instance);
            PropertyInfo sourceKeyProperty = null;

            // We make sure the destination collection has all the items we had in the source
            foreach (var sourceItem in source)
            {
                if (sourceKeyProperty == null)
                    sourceKeyProperty = sourceItem.GetType().GetProperty(GetMappedSourcePropertyName(keyField, options.MapDirection), BindingFlags.Public | BindingFlags.Instance);
                var sourceKeyValue = sourceKeyProperty.GetValue(sourceItem, null) as IComparable;

                object destinationItem = null;
                foreach (var destinationCheckItem in destination)
                {
                    var destinationKeyValue = destinationKeyProperty.GetValue(destinationCheckItem, null) as IComparable;
                    if (sourceKeyValue == null || destinationKeyValue == null) continue;
                    if (destinationKeyValue.CompareTo(sourceKeyValue) != 0) continue;
                    destinationItem = destinationCheckItem;
                    break;
                }
                if (destinationItem == null) // Not found yet, so we have to add it
                {
                    destinationItem = Activator.CreateInstance(destinationItemType);
                    destination.Add(destinationItem);
                }
                MapObjects(sourceItem, destinationItem, options);

                // We also call all registered delegates
                foreach (var function in options.MapFunctions)
                    function(sourceItem, destinationItem, options.MapDirection);
            }

            // We also make sure there aren't any items in the destination that are not in the source
            var removalCounter = -1;
            var removalIndexes = new List<int>();
            foreach (var destinationItem in destination)
            {
                removalCounter++;
                var destinationKeyValue = destinationKeyProperty.GetValue(destinationItem, null) as IComparable;
                var foundInSource = false;
                foreach (var sourceItem in source)
                {
                    if (sourceKeyProperty == null) continue;
                    var sourceKeyValue = sourceKeyProperty.GetValue(sourceItem, null) as IComparable;
                    if (sourceKeyValue == null || destinationKeyValue == null) continue;
                    if (destinationKeyValue.CompareTo(sourceKeyValue) == 0)
                    {
                        foundInSource = true;
                        break;
                    }
                }
                if (!foundInSource) // This item has no business being in the destination
                    removalIndexes.Add(removalCounter);
            }
            // Ready to remove the items starting from the rear forward, so indexes don't change as we go
            for (var removalCounter2 = removalIndexes.Count - 1; removalCounter2 > -1; removalCounter--)
                destination.RemoveAt(removalIndexes[removalCounter2]);

            _maps = oldMaps;
            return true;
        }

        /// <summary>
        /// Maps values between two types that are different, but both enums
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="destination">The destination.</param>
        /// <param name="property">The property.</param>
        /// <param name="sourceProperty">The source property.</param>
        /// <param name="options">The options.</param>
        protected virtual void MapEnumValues(object source, object destination, PropertyInfo property, PropertyInfo sourceProperty, MappingOptions options)
        {
            var currentValue = property.GetValue(destination, null);
            var newValue = sourceProperty.GetValue(source, null);

            // We attempt to match the enum names
            var newEnumValue = Enum.GetName(sourceProperty.PropertyType, newValue);
            var currentNames = Enum.GetNames(property.PropertyType);
            var enumCounter = -1;
            var foundName = false;
            foreach (var currentName in currentNames)
            {
                enumCounter++;
                if (!StringHelper.Compare(newEnumValue, currentName, options.MatchEnumCase)) continue;
                // We found a match, so all we need now is the corresponding value
                var values = Enum.GetValues(property.PropertyType);
                var newValue2 = values.GetValue(enumCounter);
                UpdateValueIfNeeded(destination, property, currentValue, newValue2);
                foundName = true;
                break;
            }

            if (!foundName)
            {
                // There was no match based on names
                var underlyingDestinationType = Enum.GetUnderlyingType(property.PropertyType);
                var newIntegerValue = (int) newValue;
                var currentIntegerValue = (int) currentValue;
                if (currentIntegerValue == newIntegerValue) return;
                if (underlyingDestinationType == typeof (byte))
                    property.SetValue(destination, (byte) newIntegerValue, null);
                else
                    property.SetValue(destination, newIntegerValue, null);
            }
        }

        /// <summary>
        /// Maps the numeric to enum.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="destination">The destination.</param>
        /// <param name="destinationProperty">The destination property.</param>
        /// <param name="sourceProperty">The source property.</param>
        protected virtual void MapNumericToEnum(object source, object destination, PropertyInfo destinationProperty, PropertyInfo sourceProperty)
        {
            try
            {
                var currentValue = (int) destinationProperty.GetValue(destination, null);
                var newObj = sourceProperty.GetValue(source, null);
                //TODO: Optimize this later - parsing is slow - can't cast a boxed byte to an int
                var newValue = int.Parse(newObj.ToString());

                UpdateValueIfNeeded(destination, destinationProperty, currentValue, newValue);
            }
            catch
            {
                Debug.WriteLine("Mapper could not map " +
                                sourceProperty.PropertyType + " [" + sourceProperty.Name + "] to  [" +
                                destinationProperty.PropertyType + " [" + destinationProperty.Name + "]");
            }
        }

        /// <summary>
        /// Maps the enum to numeric.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="destination">The destination.</param>
        /// <param name="destinationProperty">The destination property.</param>
        /// <param name="sourceProperty">The source property.</param>
        protected virtual void MapEnumToNumeric(object source, object destination, PropertyInfo destinationProperty, PropertyInfo sourceProperty)
        {
            try
            {
                var currentObject = destinationProperty.GetValue(destination, null);
                var newObject = sourceProperty.GetValue(source, null);

                object currentValue = null;
                object newValue = null;
                if (destinationProperty.PropertyType == typeof (byte))
                {
                    currentValue = byte.Parse(currentObject.ToString());
                    newValue = (byte) (int) newObject;
                }
                else if (destinationProperty.PropertyType == typeof (sbyte))
                {
                    currentValue = sbyte.Parse(currentObject.ToString());
                    newValue = (sbyte) (int) newObject;
                }
                else if (destinationProperty.PropertyType == typeof (short))
                {
                    currentValue = short.Parse(currentObject.ToString());
                    newValue = (short) (int) newObject;
                }
                else if (destinationProperty.PropertyType == typeof (ushort))
                {
                    currentValue = ushort.Parse(currentObject.ToString());
                    newValue = (ushort) (int) newObject;
                }
                else if (destinationProperty.PropertyType == typeof (int))
                {
                    currentValue = int.Parse(currentObject.ToString());
                    newValue = (int) newObject;
                }
                else if (destinationProperty.PropertyType == typeof (uint))
                {
                    currentValue = uint.Parse(currentObject.ToString());
                    newValue = (uint) (int) newObject;
                }
                else if (destinationProperty.PropertyType == typeof (long))
                {
                    currentValue = long.Parse(currentObject.ToString());
                    newValue = (long) newObject;
                }
                else if (destinationProperty.PropertyType == typeof (ulong))
                {
                    currentValue = ulong.Parse(currentObject.ToString());
                    newValue = (ulong) (int) newObject;
                }
                else if (destinationProperty.PropertyType == typeof (float))
                {
                    currentValue = float.Parse(currentObject.ToString());
                    newValue = (float) (int) newObject;
                }
                else if (destinationProperty.PropertyType == typeof (double))
                {
                    currentValue = double.Parse(currentObject.ToString());
                    newValue = (double) (int) newObject;
                }
                else if (destinationProperty.PropertyType == typeof (decimal))
                {
                    currentValue = decimal.Parse(currentObject.ToString());
                    newValue = (decimal) (int) newObject;
                }

                UpdateValueIfNeeded(destination, destinationProperty, currentValue, newValue);
            }
            catch
            {
                Debug.WriteLine("Mapper could not map " +
                                sourceProperty.PropertyType + " [" + sourceProperty.Name + "] to  [" +
                                destinationProperty.PropertyType + " [" + destinationProperty.Name + "]");
            }
        }

        /// <summary>
        /// Updates the enum if needed.
        /// </summary>
        /// <param name="parentObject">The parent object.</param>
        /// <param name="property">The property.</param>
        /// <param name="currentValue">The current value.</param>
        /// <param name="newValue">The new value.</param>
        protected virtual void UpdateEnumIfNeeded(object parentObject, PropertyInfo property, object currentValue, object newValue)
        {
            try
            {
                UpdateValueIfNeeded(parentObject, property, (int) currentValue, newValue);
            }
            catch
            {
            }
        }

        /// <summary>
        /// Updates the value if needed.
        /// </summary>
        /// <param name="parentObject">The parent object that contains the property that may need to be set.</param>
        /// <param name="property">The property that may need to be set</param>
        /// <param name="currentValue">The current value.</param>
        /// <param name="newValue">The new value.</param>
        protected virtual void UpdateValueIfNeeded(object parentObject, PropertyInfo property, object currentValue, object newValue)
        {
            var value1 = currentValue as IComparable;
            var value2 = newValue as IComparable;
            if (value1 != null && value2 != null)
            {
                if (value1.CompareTo(value2) != 0)
                    SetValueOnProperty(parentObject, property, newValue); // Only update if the value is different
            }
            else
                SetValueOnProperty(parentObject, property, newValue);
        }

        /// <summary>
        /// Sets the given value to the given property.
        /// </summary>
        /// <param name="parentObject"></param>
        /// <param name="property"></param>
        /// <param name="newValue"></param>
        protected virtual void SetValueOnProperty(object parentObject, PropertyInfo property, object newValue)
        {
            property.SetValue(parentObject, newValue, null);
        }

        /// <summary>
        /// Returns the field of the matching source property based on a known destination property name
        /// </summary>
        /// <param name="destinationProperty">The destination property.</param>
        /// <param name="direction">The direction.</param>
        /// <returns></returns>
        protected virtual string GetMappedSourcePropertyName(string destinationProperty, MapDirection direction)
        {
            return GetMappedSourcePropertyName(destinationProperty, direction, string.Empty, string.Empty);
        }

        /// <summary>
        /// Returns the field of the matching source property based on a known destination property name
        /// </summary>
        /// <param name="destinationProperty">Name of the source.</param>
        /// <param name="direction">The direction.</param>
        /// <param name="destinationContainer">The destination container.</param>
        /// <param name="sourceContainer">The source container.</param>
        /// <returns></returns>
        protected virtual string GetMappedSourcePropertyName(string destinationProperty, MapDirection direction, string destinationContainer, string sourceContainer)
        {
            foreach (var map in _maps)
                if (StringHelper.Compare(sourceContainer, map.SourceContainer, false) && StringHelper.Compare(destinationContainer, map.DestinationContainer))
                    if (direction == MapDirection.Forward)
                    {
                        if (StringHelper.Compare(destinationProperty, map.DestinationField, false))
                        {
                            destinationProperty = map.SourceField;
                            break;
                        }
                    }
                    else
                    {
                        if (StringHelper.Compare(destinationProperty, map.SourceField, false))
                        {
                            destinationProperty = map.DestinationField;
                            break;
                        }
                    }
            return destinationProperty;
        }

        /// <summary>
        /// Maps tables to generic lists.
        /// </summary>
        /// <typeparam name="TDestination">The type of the destination.</typeparam>
        /// <param name="sourceRows">The source rows.</param>
        /// <param name="options">The options.</param>
        /// <returns></returns>
        /// <remarks>This is the same as the instance method MapTableToList&lt;TDestination&gt;()</remarks>
        public static List<TDestination> TableToList<TDestination>(DataRow[] sourceRows, MappingOptions options)
        {
            var mapper = new Mapper();
            return mapper.MapTableToList<TDestination>(sourceRows, options);
        }

        /// <summary>
        /// Maps tables to generic lists.
        /// </summary>
        /// <typeparam name="TDestination">The type of the destination.</typeparam>
        /// <param name="table">The table.</param>
        /// <param name="options">The options.</param>
        /// <returns></returns>
        /// <remarks>This is the same as the instance method MapTableToList&lt;TDestination&gt;()</remarks>
        public static List<TDestination> TableToList<TDestination>(DataTable table, MappingOptions options)
        {
            var mapper = new Mapper();
            return mapper.MapTableToList<TDestination>(table, options);
        }

        /// <summary>
        /// Maps tables to generic lists.
        /// </summary>
        /// <typeparam name="TDestination">The type of the destination.</typeparam>
        /// <param name="table">The table.</param>
        /// <param name="options">The options.</param>
        /// <param name="startRowIndex">First row index to be mapped (9 = row 10 is the first included row)</param>
        /// <param name="numberOfRowsToMap">Number of rows to map (start index = 20, number of rows = 10 means that rows 21-30 will be mapped)</param>
        /// <returns></returns>
        /// <remarks>This is the same as the instance method MapTableToList&lt;TDestination&gt;()</remarks>
        public static List<TDestination> TableToList<TDestination>(DataTable table, MappingOptions options, int startRowIndex, int numberOfRowsToMap)
        {
            var mapper = new Mapper();
            return mapper.MapTableToList<TDestination>(typeof (TDestination), table, options, startRowIndex, numberOfRowsToMap);
        }

        /// <summary>
        /// Maps tables to generic lists.
        /// </summary>
        /// <typeparam name="TDestination">The type of the destination.</typeparam>
        /// <param name="table">The table.</param>
        /// <returns></returns>
        /// <remarks>This is the same as the instance method MapTableToList&lt;TDestination&gt;()</remarks>
        public static List<TDestination> TableToList<TDestination>(DataTable table)
        {
            var mapper = new Mapper();
            return mapper.MapTableToList<TDestination>(table);
        }

        /// <summary>
        /// Maps a data table's records to a generic List
        /// </summary>
        /// <typeparam name="TDestination">The type of the destination.</typeparam>
        /// <param name="sourceRows">The source rows.</param>
        /// <param name="options">The options.</param>
        /// <returns></returns>
        public List<TDestination> MapTableToList<TDestination>(DataRow[] sourceRows, MappingOptions options)
        {
            var activator = new ExpressionTreeObjectActivator(typeof (TDestination));

            var oldMaps = _maps;
            if (options.Maps != null)
                _maps = options.Maps;

            var result = new List<TDestination>();

            var properties = typeof (TDestination).GetProperties(BindingFlags.Instance | BindingFlags.Public);

            foreach (var row in sourceRows)
            {
                result.Add(CopyFromRowToItem<TDestination>(row, options, properties, activator));

                // We also call all registered delegates
                foreach (var function in options.MapFunctions)
                    function(row, result[result.Count - 1], options.MapDirection);
            }

            _maps = oldMaps;

            return result;
        }

        /// <summary>
        /// Copies from row to item.
        /// </summary>
        /// <typeparam name="TDestination">The type of the destination.</typeparam>
        /// <param name="row">The row.</param>
        /// <param name="options">The options.</param>
        /// <param name="properties">The properties.</param>
        /// <param name="activator">Activator</param>
        /// <returns></returns> 
        protected virtual TDestination CopyFromRowToItem<TDestination>(DataRow row, MappingOptions options, IEnumerable<PropertyInfo> properties, ExpressionTreeObjectActivator activator)
        {
            var item = (TDestination) activator.InstantiateType();

            foreach (var property in properties) // We try to match each property on the new item to an element in the row
            {
                var sourceFieldName = GetMappedSourcePropertyName(property.Name, options.MapDirection);
                if (!row.Table.Columns.Contains(sourceFieldName) && options.SmartPrefixMap)
                    sourceFieldName = GetPrefixedFieldName(row.Table, sourceFieldName);
                if (!row.Table.Columns.Contains(sourceFieldName)) continue;
                object rowValue = null;
                if (row[sourceFieldName] != DBNull.Value) rowValue = row[sourceFieldName];
                if (!property.PropertyType.IsEnum) UpdateValueIfNeeded(item, property, property.GetValue(item, null), rowValue);
                else UpdateEnumIfNeeded(item, property, property.GetValue(item, null), rowValue);
            }
            return item;
        }

        /// <summary>
        /// Tries to find a field name that matches if it had a prefix.
        /// </summary>
        /// <param name="table">The table.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>Field name</returns>
        private static string GetPrefixedFieldName(DataTable table, string fieldName)
        {
            foreach (DataColumn column in table.Columns)
            {
                if (string.Compare(fieldName, column.ColumnName.Substring(1), StringComparison.OrdinalIgnoreCase) == 0)
                    return column.ColumnName;
                if (string.Compare("_" + fieldName, column.ColumnName, StringComparison.OrdinalIgnoreCase) == 0)
                    return column.ColumnName;
                if (string.Compare("_" + fieldName, column.ColumnName.Substring(1), StringComparison.OrdinalIgnoreCase) == 0)
                    return column.ColumnName;
            }
            return fieldName;
        }

        /// <summary>
        /// Maps the enumerables.
        /// </summary>
        /// <typeparam name="TDestination">The type of the destination.</typeparam>
        /// <param name="source">The source.</param>
        /// <returns></returns>
        public List<TDestination> MapTableToList<TDestination>(DataTable source)
        {
            return MapTableToList<TDestination>(source, new MappingOptions());
        }

        /// <summary>
        /// Maps the enumerables.
        /// </summary>
        /// <typeparam name="TDestination">The type of the destination.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="options">The options.</param>
        /// <returns></returns>
        public List<TDestination> MapTableToList<TDestination>(DataTable source, MappingOptions options)
        {
            return MapTableToList<TDestination>(typeof (TDestination), source, options);
        }

        /// <summary>
        /// Maps the enumerables.
        /// </summary>
        /// <typeparam name="TDestination">The type of the destination.</typeparam>
        /// <param name="destinationType">Destination type</param>
        /// <param name="source">The source.</param>
        /// <returns></returns>
        public List<TDestination> MapTableToList<TDestination>(Type destinationType, DataTable source)
        {
            return MapTableToList<TDestination>(destinationType, source, new MappingOptions());
        }

        /// <summary>
        /// Maps the enumerables.
        /// </summary>
        /// <typeparam name="TDestination">The type of the destination.</typeparam>
        /// <param name="destinationType">Destination type</param>
        /// <param name="source">The source.</param>
        /// <param name="options">The options.</param>
        /// <returns></returns>
        public List<TDestination> MapTableToList<TDestination>(Type destinationType, DataTable source, MappingOptions options)
        {
            var activator = new ExpressionTreeObjectActivator(destinationType);

            var oldMaps = _maps;
            if (options.Maps != null)
                _maps = options.Maps;

            var result = new List<TDestination>();

            var helper = new ReflectionHelper();
            var properties = helper.GetAllProperties(typeof (TDestination));

            foreach (DataRow row in source.Rows)
            {
                result.Add(CopyFromRowToItem<TDestination>(row, options, properties, activator));

                // We also call all registered delegates
                foreach (var function in options.MapFunctions)
                    function(row, result[result.Count - 1], options.MapDirection);
            }

            _maps = oldMaps;

            return result;
        }

        /// <summary>
        /// Maps the enumerables.
        /// </summary>
        /// <typeparam name="TDestination">The type of the destination.</typeparam>
        /// <param name="destinationType">Destination type</param>
        /// <param name="source">The source.</param>
        /// <param name="options">The options.</param>
        /// <param name="startRowIndex">First row index to be mapped (9 = row 10 is the first included row)</param>
        /// <param name="numberOfRowsToMap">Number of rows to map (start index = 20, number of rows = 10 means that rows 21-30 will be mapped)</param>
        /// <returns></returns>
        public List<TDestination> MapTableToList<TDestination>(Type destinationType, DataTable source, MappingOptions options, int startRowIndex, int numberOfRowsToMap)
        {
            var activator = new ExpressionTreeObjectActivator(destinationType);

            var oldMaps = _maps;
            if (options.Maps != null)
                _maps = options.Maps;

            var result = new List<TDestination>();

            if (startRowIndex < source.Rows.Count)
            {
                var helper = new ReflectionHelper();
                var properties = helper.GetAllProperties(typeof (TDestination));

                var maxCount = Math.Min(startRowIndex + numberOfRowsToMap, source.Rows.Count);
                for (var rowCounter = startRowIndex; rowCounter < maxCount; rowCounter++)
                {
                    result.Add(CopyFromRowToItem<TDestination>(source.Rows[rowCounter], options, properties, activator));
                    // We also call all registered delegates
                    foreach (var function in options.MapFunctions)
                        function(source.Rows[rowCounter], result[result.Count - 1], options.MapDirection);
                }
            }

            _maps = oldMaps;

            return result;
        }

        /// <summary>
        /// Maps tables to generic collection.
        /// </summary>
        /// <typeparam name="TDestination">The type of the destination.</typeparam>
        /// <param name="sourceRows">The source rows.</param>
        /// <param name="options">The options.</param>
        /// <returns></returns>
        /// <remarks>This is the same as the instance method MapTableToCollection&lt;TDestination&gt;()</remarks>
        public static Collection<TDestination> TableToCollection<TDestination>(DataRow[] sourceRows, MappingOptions options)
        {
            var mapper = new Mapper();
            return mapper.MapTableToCollection<TDestination>(sourceRows, options);
        }

        /// <summary>
        /// Maps tables to generic lists.
        /// </summary>
        /// <typeparam name="TDestination">The type of the destination.</typeparam>
        /// <param name="table">The table.</param>
        /// <param name="options">The options.</param>
        /// <returns></returns>
        /// <remarks>This is the same as the instance method MapTableToCollection&lt;TDestination&gt;()</remarks>
        public static Collection<TDestination> TableToCollection<TDestination>(DataTable table, MappingOptions options)
        {
            var mapper = new Mapper();
            return mapper.MapTableToCollection<TDestination>(table, options);
        }

        /// <summary>
        /// Maps tables to generic lists.
        /// </summary>
        /// <typeparam name="TDestination">The type of the destination.</typeparam>
        /// <param name="table">The table.</param>
        /// <param name="options">The options.</param>
        /// <param name="startRowIndex">First row index to be mapped (9 = row 10 is the first included row)</param>
        /// <param name="numberOfRowsToMap">Number of rows to map (start index = 20, number of rows = 10 means that rows 21-30 will be mapped)</param>
        /// <returns></returns>
        /// <remarks>This is the same as the instance method MapTableToCollection&lt;TDestination&gt;()</remarks>
        public static Collection<TDestination> TableToCollection<TDestination>(DataTable table, MappingOptions options, int startRowIndex, int numberOfRowsToMap)
        {
            var mapper = new Mapper();
            return mapper.MapTableToCollection<TDestination>(table, options, startRowIndex, numberOfRowsToMap);
        }

        /// <summary>
        /// Maps tables to generic lists.
        /// </summary>
        /// <typeparam name="TDestination">The type of the destination.</typeparam>
        /// <param name="table">The table.</param>
        /// <returns></returns>
        /// <remarks>This is the same as the instance method MapTableToCollection&lt;TDestination&gt;()</remarks>
        public static Collection<TDestination> TableToCollection<TDestination>(DataTable table)
        {
            var mapper = new Mapper();
            return mapper.MapTableToCollection<TDestination>(table);
        }

        /// <summary>
        /// Maps a data table's records to a generic collection
        /// </summary>
        /// <typeparam name="TDestination">The type of the destination.</typeparam>
        /// <param name="sourceRows">The source rows.</param>
        /// <param name="options">The options.</param>
        /// <returns></returns>
        public Collection<TDestination> MapTableToCollection<TDestination>(DataRow[] sourceRows, MappingOptions options)
        {
            var activator = new ExpressionTreeObjectActivator(typeof (TDestination));

            var oldMaps = _maps;
            if (options.Maps != null)
                _maps = options.Maps;

            var result = new Collection<TDestination>();

            var properties = typeof (TDestination).GetProperties(BindingFlags.Instance | BindingFlags.Public);

            foreach (var row in sourceRows)
            {
                result.Add(CopyFromRowToItem<TDestination>(row, options, properties, activator));

                // We also call all registered delegates
                foreach (var function in options.MapFunctions)
                    function(row, result[result.Count - 1], options.MapDirection);
            }

            _maps = oldMaps;

            return result;
        }

        /// <summary>
        /// Maps a data table to a generic collection of the specified type
        /// </summary>
        /// <typeparam name="TDestination">The type of the destination.</typeparam>
        /// <param name="source">The source.</param>
        /// <returns></returns>
        public Collection<TDestination> MapTableToCollection<TDestination>(DataTable source)
        {
            return MapTableToCollection<TDestination>(source, new MappingOptions());
        }

        /// <summary>
        /// Maps the enumerables.
        /// </summary>
        /// <typeparam name="TDestination">The type of the destination.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="options">The options.</param>
        /// <returns></returns>
        public Collection<TDestination> MapTableToCollection<TDestination>(DataTable source, MappingOptions options)
        {
            var activator = new ExpressionTreeObjectActivator(typeof (TDestination));

            var oldMaps = _maps;
            if (options.Maps != null)
                _maps = options.Maps;

            var result = new Collection<TDestination>();

            var properties = typeof (TDestination).GetProperties(BindingFlags.Instance | BindingFlags.Public);

            foreach (DataRow row in source.Rows)
            {
                result.Add(CopyFromRowToItem<TDestination>(row, options, properties, activator));

                // We also call all registered delegates
                foreach (var function in options.MapFunctions)
                    function(row, result[result.Count - 1], options.MapDirection);
            }

            _maps = oldMaps;

            return result;
        }

        /// <summary>
        /// Maps the enumerables.
        /// </summary>
        /// <typeparam name="TDestination">The type of the destination.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="options">The options.</param>
        /// <param name="startRowIndex">First row index to be mapped (9 = row 10 is the first included row)</param>
        /// <param name="numberOfRowsToMap">Number of rows to map (start index = 20, number of rows = 10 means that rows 21-30 will be mapped)</param>
        /// <returns></returns>
        public Collection<TDestination> MapTableToCollection<TDestination>(DataTable source, MappingOptions options, int startRowIndex, int numberOfRowsToMap)
        {
            var activator = new ExpressionTreeObjectActivator(typeof (TDestination));

            var oldMaps = _maps;
            if (options.Maps != null)
                _maps = options.Maps;

            var result = new Collection<TDestination>();

            if (startRowIndex < source.Rows.Count)
            {
                var properties = typeof (TDestination).GetProperties(BindingFlags.Instance | BindingFlags.Public);

                var maxCount = Math.Min(startRowIndex + numberOfRowsToMap, source.Rows.Count);
                for (var rowCounter = startRowIndex; rowCounter < maxCount; rowCounter++)
                {
                    result.Add(CopyFromRowToItem<TDestination>(source.Rows[rowCounter], options, properties, activator));
                    // We also call all registered delegates
                    foreach (var function in options.MapFunctions)
                        function(source.Rows[rowCounter], result[result.Count - 1], options.MapDirection);
                }
            }

            _maps = oldMaps;

            return result;
        }

         /// <summary>
        /// Attempts to compare all the read/write properties in the first object
        /// and the properties in the second object and return those that don't match
        /// </summary>
        /// <param name="first">The first object.</param>
        /// <param name="second">The second object.</param>
        /// <remarks>
        /// This is identical to the CompareObjects() instance method.
        /// This method is simply provided for convenience.
        /// </remarks>
        /// <returns>A list of all the properties that don't match</returns>
        public static List<MappperCompareResults> Compare(object first, object second)
        {
            var mapper = new Mapper();
            return mapper.CompareObjects(first, second);
        }

        /// <summary>
        /// Attempts to compare all the read/write properties in the first object
        /// and the properties in the second object and return those that don't 
        /// match based on the options provided.
        /// </summary>
        /// <param name="first">The source.</param>
        /// <param name="second">The destination.</param>
        /// <param name="options">The options.</param>
        /// <remarks>
        /// This is identical to the CompareObjects() instance method.
        /// This method is simply provided for convenience.
        /// </remarks>
        /// <returns>A list of all the properties that don't match</returns>
        public static List<MappperCompareResults> Compare(object first, object second, MappingOptions options)
        {
            var mapper = new Mapper();
            return mapper.CompareObjects(first, second, options);
        }

        /// <summary>
        /// Attempts to compare all the read/write properties in the first object
        /// to the properties in the second object and returns those that
        /// that don't match
        /// </summary>
        /// <param name="first">The first object.</param>
        /// <param name="second">The second object.</param>
        /// <returns>A list of all the properties that don't match</returns>
        public List<MappperCompareResults> CompareObjects(object first, object second)
        {
            return CompareObjects(first, second, new MappingOptions());
        }

        /// <summary>
        /// Attempts to compare all the read/write properties in the 
        /// first object with the properties in the second object and
        /// returns a list of those that don't match and info on why.
        /// </summary>
        /// <param name="first">The source.</param>
        /// <param name="second">The destination.</param>
        /// <param name="options">Options - but the only one 
        /// supported is the ExcludedFields at this time.</param>
        /// <param name="level">This is only used internally and tracks how many levels down the conflict may be.</param>
        /// <returns>A list of all the properties that don't match</returns>
        public List<MappperCompareResults> CompareObjects(object first, object second, MappingOptions options, int level = 0)
        {
            var results = new List<MappperCompareResults>();

            var oldMaps = _maps;
            if (options.Maps != null)
                _maps = options.Maps;

            var firstType = first.GetType();
            var secondProperties = second.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var secondProperty in secondProperties)
            {
                if (!secondProperty.CanRead || !secondProperty.CanWrite ||options.ExcludedFields.Contains(secondProperty.Name)) continue;

                // looks like we have one we are interested in,
                //  find the property name so we can get the first 
                //  property reference
                var firstPropertyName = GetMappedSourcePropertyName(secondProperty.Name, options.MapDirection);
                if (options.ExcludedFields.Contains(firstPropertyName)) continue;

                var firstProperty = firstType.GetProperty(firstPropertyName, BindingFlags.Public | BindingFlags.Instance);

                // If we don't have a property we can read or is null, skip it.
                if (firstProperty == null || !firstProperty.CanRead || !secondProperty.CanRead) continue;

                // Check the types first.  We won't bother if the types don't match
                //  (too much work for a diagnostic feature.)
                if (firstProperty.PropertyType != secondProperty.PropertyType)
                {
                    // Data types don't agree... 
                    try
                    {
                        results.Add(new MappperCompareResults
                        {
                            Property = firstPropertyName,
                            ValueFirstObject = firstProperty.GetValue(first, null),
                            ValueSecondObject = secondProperty.GetValue(second, null),
                            Conflict = "Property Type Mismatch",
                            Level = level,
                        });
                    }
                    catch (Exception ex)
                    {
                        results.Add(new MappperCompareResults
                        {
                            Property = firstPropertyName,
                            Conflict = "Property Type Mismatch - error including values: " + ex.Message,
                        });
                    }
                }
                else
                {
                    // Ok, see if they compare
                    try
                    {
                        // The source is nullable, while the destination is not. This could be problematic, but we give it a try
                        var value1 = firstProperty.GetValue(first, null);
                        var value2 = secondProperty.GetValue(second, null);

                        // Check for collections
                        if (value1 != null && value2 != null && (value1.ToString().StartsWith("System.Collections.") || firstProperty.PropertyType.IsArray))
                        {
                            // We have a collection, are the counts different?
                            if (((ICollection) value1).Count != ((ICollection) value2).Count)
                                results.Add(new MappperCompareResults
                                {
                                    Property = firstPropertyName,
                                    ValueFirstObject = value1,
                                    ValueSecondObject = value2,
                                    Conflict = "Collection Count Mismatch",
                                    Level = level,
                                });
                            else if (((ICollection) value1).Count > 0)
                            {
                                // Ok, counts are the same and more than 0, 
                                //  enumerate through the collections.  If this
                                //  is a collection of data objects, call
                                //  Mapper.Compare.  Otherwise, enumerate through
                                //  the collection.

                                // How will we compare the objects?
                                var useMapperComp = value1.ToString().Contains(".Services.Contracts");

                                // Note: this could probably be optimized and
                                //  I am assuming that enumerators work the same
                                //  with identical sets.  I could use LINQ and 
                                //  search one element in the other list, but that
                                //  could become a big issue....  Not sure at this time.
                                // But the following does work.
                                var en1 = ((ICollection) value1).GetEnumerator();
                                var en2 = ((ICollection) value2).GetEnumerator();
                                var matched = true;
                                var working = en1.MoveNext();
                                working = working && en2.MoveNext();

                                // This is just to set the type of
                                //  item1 & item2 so we can reference
                                //  them after the loop, if we need to.
                                var item1 = en1.Current;
                                var item2 = en2.Current;
                                var count = -1;

                                while (matched && working)
                                {
                                    count++;
                                    item1 = en1.Current;
                                    item2 = en2.Current;
                                    if (useMapperComp)
                                    {
                                        var tempResults = CompareObjects(item1, item2, options, level);
                                        if (tempResults.Count > 0)
                                            // Add whatever was returned from the above Compare
                                            results.AddRange(tempResults);
                                    }
                                    else // Just see if they are equal.
                                        matched = item1.Equals(item2);

                                    // Move on to the next item
                                    working = en1.MoveNext();
                                    working = working && en2.MoveNext();
                                }
                                if (!matched)
                                    // Ok, add this to the results
                                    results.Add(new MappperCompareResults
                                    {
                                        Property = firstPropertyName,
                                        ValueFirstObject = item1,
                                        ValueSecondObject = item2,
                                        Conflict = $"Element {count} does not match, no more elements checked.",
                                        Level = level,
                                    });
                            }
                        } //  if (value1 != null && value2 !=null && value1.ToString().StartsWith("System.Collections."))
                        else
                        {
                            // Ok, not a collection, let's compare values.
                            if (value1 == null && value2 != null || value1 != null && value2 == null || (value1 != null && !value1.Equals(value2)))
                                // 1 is null, the other isn't OR they
                                //  don't match - add it
                                results.Add(new MappperCompareResults
                                {
                                    Property = firstPropertyName,
                                    ValueFirstObject = value1,
                                    ValueSecondObject = value2,
                                    Conflict = "!=",
                                    Level = level,
                                });
                        }
                    }
                    catch (Exception ex)
                    {
                        // We had an error, at least track the property 
                        //  that caused the error and report the error.
                        results.Add(new MappperCompareResults
                        {
                            Property = firstPropertyName,
                            Conflict = $"Error comparing values: {ex.Message}",
                            Level = level,
                        });
                    }
                }
            }
            _maps = oldMaps;
            return results;
        }

        /// <summary>
        /// Class used internally to aid in reflection tasks
        /// </summary>
        private class ReflectionHelper
        {
            public PropertyInfo[] GetAllProperties(Type type)
            {
                var properties = from property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    select property;

                var list = new List<PropertyInfo>();
                list.AddRange(properties);

                ProcessInterfaces(type, list);

                return list.ToArray();
            }

            private void ProcessInterfaces(Type type, List<PropertyInfo> list)
            {
                var interfaces = type.GetInterfaces();

                foreach (var item in interfaces)
                {
                    GetProperties(item, list);
                    ProcessInterfaces(item, list);
                }
            }

            private void GetProperties(Type type, List<PropertyInfo> list)
            {
                var properties = from property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance) select property;
                list.AddRange(properties);
            }
        }
    }

    /// <summary>
    /// Used as a return type for Mapper.Compare()
    /// </summary>
    public class MappperCompareResults
    {
        /// <summary>
        /// If there is an error or other message
        /// (other than not equal) this should explain
        /// the issue.  Most commonly, it's for data
        /// types that don't agree....
        /// </summary>
        public string Conflict { get; set; }

        /// <summary>
        /// This can be used when processing lists/collections
        /// of objects....
        /// </summary>
        public int Level { get; set; }

        /// <summary>
        /// Name of the property that does not compare
        /// </summary>
        public string Property { get; set; }

        /// <summary>
        /// The value of the property in the first object
        /// </summary>
        public object ValueFirstObject { get; set; }

        /// <summary>
        /// The value of the property in the 2nd object 
        /// </summary>
        public object ValueSecondObject { get; set; }

    }


    ///<summary>
    /// Key type used for mapping
    ///</summary>
    public enum MapKeyType
    {
        ///<summary>
        /// Guid key
        ///</summary>
        Guid,

        ///<summary>
        /// Integer key (auto-increment or otherwise)
        ///</summary>
        Integer,

        ///<summary>
        /// String key
        ///</summary>
        String
    }

    /// <summary>
    /// Defines a single field map
    /// </summary>
    public class Map
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Map"/> class.
        /// </summary>
        /// <param name="sourceField">The source field.</param>
        /// <param name="destinationField">The destination field.</param>
        public Map(string sourceField, string destinationField)
        {
            SourceField = sourceField;
            SourceContainer = string.Empty;
            DestinationField = destinationField;
            DestinationContainer = string.Empty;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Map"/> class.
        /// </summary>
        /// <param name="sourceField">The source field.</param>
        /// <param name="sourceContainer">The source container.</param>
        /// <param name="destinationField">The destination field.</param>
        /// <param name="destinationContainer">The destination container.</param>
        public Map(string sourceField, string sourceContainer, string destinationField, string destinationContainer)
        {
            SourceField = sourceField;
            SourceContainer = sourceContainer;
            DestinationField = destinationField;
            DestinationContainer = destinationContainer;
        }

        /// <summary>
        /// Gets or sets the source field.
        /// </summary>
        /// <value>The source field.</value>
        public string SourceField { get; private set; }

        /// <summary>
        /// Gets or sets the source container.
        /// </summary>
        /// <value>The source container.</value>
        public string SourceContainer { get; private set; }

        /// <summary>
        /// Gets or sets the destination field.
        /// </summary>
        /// <value>The destination field.</value>
        public string DestinationField { get; private set; }

        /// <summary>
        /// Gets or sets the destination container.
        /// </summary>
        /// <value>The destination container.</value>
        public string DestinationContainer { get; private set; }
    }

    /// <summary>
    /// Map direction
    /// </summary>
    public enum MapDirection
    {
        /// <summary>
        /// Source to destination
        /// </summary>
        Forward,

        /// <summary>
        /// Destination to source
        /// </summary>
        Backward
    }

    /// <summary>
    /// Mapping options (settings)
    /// </summary>
    public class MappingOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MappingOptions"/> class.
        /// </summary>
        public MappingOptions()
        {
            MapEnums = true;
            MatchEnumCase = true;
            Maps = new List<Map>();
            ExcludedFields = new List<string>();
            MapDirection = MapDirection.Forward;
            MapFunctions = new List<Func<object, object, MapDirection, bool>>();
            MapFunctionsFiltered = new Dictionary<string, Func<object, object, MapDirection, bool>>();
            SmartPrefixMap = false;
        }

        /// <summary>
        /// Defines whether enums should be mapped, even if they are not the exact same enum.
        /// </summary>
        /// <remarks>
        /// Tries to map enums by their names and if that doesn't work, by their value (integer or byte)
        /// </remarks>
        /// <value><c>true</c> if enums are to be mapped, otherwise, <c>false</c>.</value>
        public bool MapEnums { get; set; }

        /// <summary>
        /// Indicates whether auto-map enum attempts shall be case sensitive or not
        /// </summary>
        /// <value><c>true</c> if case sensitive, otherwise, <c>false</c>.</value>
        public bool MatchEnumCase { get; set; }

        /// <summary>
        /// If set to true, the mapper will automatically try to make prefixed fields to 
        /// non-prefixed properties, such as cName or _name or c_name to Name.
        /// </summary>
        /// <value><c>true</c> if [smart prefix map]; otherwise, <c>false</c>.</value>
        public bool SmartPrefixMap { get; set; }

        /// <summary>
        /// If not null, this list of maps will be used instead of other defined maps
        /// </summary>
        /// <value>The maps.</value>
        public List<Map> Maps { get; set; }

        /// <summary>
        /// List of excluded fields
        /// </summary>
        /// <value>The excluded fields.</value>
        public List<string> ExcludedFields { get; set; }

        /// <summary>
        /// Direction of the mapping operation
        /// </summary>
        /// <remarks>
        /// Forward = Source to destination
        /// Backward = Destination to source
        /// </remarks>
        /// <value>The map direction.</value>
        public MapDirection MapDirection { get; set; }

        /// <summary>
        /// A list of functions that get called for mapped objects.
        /// For iterative maps (tables,...), map functions are called for each record.
        /// </summary>
        /// <value>The map functions.</value>
        /// <remarks>
        /// Parameter 1 = source object (forward map, otherwise destination)
        /// Parameter 2 = destination object (forward map, otherwise source)
        /// Parameter 3 = direction
        /// Return value = true if successful
        /// </remarks>
        public List<Func<object, object, MapDirection, bool>> MapFunctions { get; set; }

        /// <summary>
        /// A list of functions that get called for mapped objects.
        /// Maps are only called for properties that match the key in the dictionary.
        /// For iterative maps (tables,...), map functions are called for each record.
        /// </summary>
        /// <value>The map functions. </value>
        /// <remarks>
        /// Parameter 1 = source object (forward map, otherwise destination)
        /// Parameter 2 = destination object (forward map, otherwise source)
        /// Parameter 3 = direction
        /// Return value = true if successful
        /// </remarks>
        public Dictionary<string, Func<object, object, MapDirection, bool>> MapFunctionsFiltered { get; set; }
    }

    /// <summary>
    /// Instantiates a given type by building an expression tree
    /// that invokes the default construction on the type.
    /// </summary>
    public class ExpressionTreeObjectActivator
    {
        private readonly ObjectActivator _activator;
        private readonly Type _objectType;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="objectType">Type of the object to be instantiated.</param>
        public ExpressionTreeObjectActivator(Type objectType)
        {
            _objectType = objectType;
            var constructorInfo = FindDefaultParameterlessConstructor();
            _activator = GetActivator(constructorInfo);
        }

        private ConstructorInfo FindDefaultParameterlessConstructor()
        {
            var constructors = _objectType.GetConstructors().Where(x => x.GetParameters().Count().Equals(0));

            var constructorInfos = constructors as ConstructorInfo[] ?? constructors.ToArray();
            if (!constructorInfos.Any())
                throw new InvalidOperationException("Parameterless constructor required on type '" + _objectType.FullName + "'.");
            return constructorInfos.Single();
        }

        /// <summary>
        /// Instantiates the type using its default parameterless constructor.
        /// </summary>
        /// <returns></returns>
        public object InstantiateType()
        {
            return _activator(null);
        }

        private static ObjectActivator GetActivator(ConstructorInfo constructorInfo)
        {
            var parameters = constructorInfo.GetParameters();

            // create a single param of type object[]
            var param = Expression.Parameter(typeof (object[]), "args");

            var argsExp = new Expression[parameters.Length];

            // make a NewExpression that calls the constructor with the args we just created
            var newExp = Expression.New(constructorInfo, argsExp);

            // create a lambda with the New
            // Expression as body and our param object[] as arg
            var lambda = Expression.Lambda(typeof (ObjectActivator), newExp, param);

            // compile it
            var compiled = (ObjectActivator) lambda.Compile();

            return compiled;
        }

        private delegate object ObjectActivator(params object[] args);
    }
}
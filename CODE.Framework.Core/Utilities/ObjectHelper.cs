using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization.Formatters.Soap;
using System.Xml;
using System.Xml.Serialization;

namespace CODE.Framework.Core.Utilities
{
    /// <summary>
    /// This object features a number of methods that are useful in dealing with objects
    /// </summary>
    public static class ObjectHelper
    {
        /// <summary>
        /// Loads a named object from an assembly
        /// </summary>
        /// <param name="className">Fully qualified name of the class</param>
        /// <param name="assemblyName">Assembly name (preferrably the fully or partially qualified name, or the file name)</param>
        /// <returns>Newly instantiated object</returns>
        /// <example>SqlDataService oService = (SqlDataService)ObjectHelper.CreateObject("EPS.Data.SqlClient","SqlDataService")</example>
        public static object CreateObject(string className, string assemblyName)
        {
            // We simply try to instantiate the object based on the information
            // that was passed to us. There are two basic ways of loading the assembly
            // the object lives in:
            // 1) Load the assembly based on it's file name (xxx.dll)
            // 2) Load the assembly based on it's assembly name (Namespace.Assembly)
            //    a) This can happen with a fully qualified name (including version and culture information)
            //    b) ... or a partially qualified name (just the fully qualified assembly name)
            Assembly assembly = null;

            if (assemblyName.ToLower(CultureInfo.InvariantCulture).EndsWith(".dll") || assemblyName.ToLower(CultureInfo.InvariantCulture).EndsWith(".exe"))
                // We are loading based on an assembly file name.
                try
                {
                    // Load the assembly to use.
                    assembly = Assembly.LoadFrom(assemblyName);
                }
                catch
                {
                    // Plain instantiation seems to have failed. 
                    // We need to check whether the path is incorrect.
                    // Problems could be caused due to changed paths
                    // which seems to happen when certain components, such
                    // as the file-open dialog is used.

                    // We check whether the path is fully qualified
                    if (assemblyName.IndexOf("\\", StringComparison.Ordinal) < 0)
                    {
                        string location;
                        try
                        {
                            location = Assembly.GetEntryAssembly() != null ? Assembly.GetEntryAssembly().Location : Assembly.GetAssembly(typeof (ObjectHelper)).Location;
                        }
                        catch
                        {
                            try
                            {
                                location = Assembly.GetAssembly(typeof (ObjectHelper)).Location;
                            }
                            catch
                            {
                                throw new ObjectInstantiationException("Unable to find assemblie's location(" + assemblyName + ")");
                            }
                        }
                        var startPath = StringHelper.AddBS(StringHelper.JustPath(location));
                        assemblyName = startPath + assemblyName;

                        // We now load the assembly with the new path
                        try
                        {
                            assembly = Assembly.LoadFrom(assemblyName);
                        }
                        catch (Exception ex)
                        {
                            throw new ObjectInstantiationException("Unable to load assembly " + assemblyName + ". Error: " + ex.Message, ex);
                        }
                    }
                }
            else
                // This assembly is identified by its name.
                if (assemblyName.IndexOf(",", StringComparison.Ordinal) > -1)
                    // There is a comma in the assembly name. Therefore, we assume that this is a fully qualified name
                    // Note: An example for a fully qualified name would be the following:
                    //       System.data, version=1.0.3300.0, Culture=neutral, PublicKeyToken=b77a5c561934e089
                    try
                    {
                        assembly = Assembly.Load(assemblyName);
                    }
                    catch (Exception ex)
                    {
                        throw new ObjectInstantiationException("Unable to load assembly " + assemblyName + ". Error: " + ex.Message, ex);
                    }
                else
                    // This just seems to be a partially qualified name
                    // Note: An example for a partially qualified name would be the following:
                    //       System.data
                    try
                    {
                        //asm = Assembly.LoadWithPartialName(AssemblyName);
                        assembly = Assembly.Load(assemblyName);
                    }
                    catch (Exception ex)
                    {
                        throw new ObjectInstantiationException("Unable to load assembly " + assemblyName + ". Error: " + ex.Message, ex);
                    }


            // Now that the assembly is loaded, we can get the interfaceType of the specified class
            Type objectType;
            try
            {
                if (assembly != null) 
                    objectType = assembly.GetType(className, true);
                else
                    throw new ObjectInstantiationException("Unable to create instance " + className + ". Error: Unable to load assembly.");
            }
            catch (Exception ex)
            {
                throw new ObjectInstantiationException("Unable to get interfaceType " + className + ". Error: " + ex.Message, ex);
            }

            // Creates an instance of the specified interfaceType
            try
            {
                return Activator.CreateInstance(objectType);
            }
            catch (Exception ex)
            {
                throw new ObjectInstantiationException("Unable to create instance " + className + ". Error: " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Serializes an object to its binary state
        /// </summary>
        /// <param name="objectToSerialize">The object to serialize.</param>
        /// <returns>Stream of binary information for the object</returns>
        /// <remarks>
        /// For this to work, the provided object must be serializable.
        /// 
        /// This method can be used as an extension method.
        /// </remarks>
        /// <example>
        /// using EPS.Utilities;
        /// // More code...
        /// var stream = customer.SerializeToBinaryStream();
        /// 
        /// // Or
        /// 
        /// var stream = EPS.Utilities.ObjectHelper.SerializeToBinaryStream(customer);
        /// </example>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1720:AvoidTypeNamesInParameters", MessageId = "0#", Justification = "Following the rule would lead to more misleading code in this specific case.")]
        public static Stream SerializeToBinaryStream(this object objectToSerialize)
        {
            var stream = new MemoryStream();
            var formatter = new BinaryFormatter();
            formatter.Serialize(stream, objectToSerialize);
            return stream;
        }

        /// <summary>
        /// Deserializes an object from its state stored in a binary stream.
        /// </summary>
        /// <param name="stateStream">The state stream.</param>
        /// <returns>Object instance.</returns>
        /// <remarks>For this to work, the stream must contain a serialized object</remarks>
        /// <example>
        /// Customer customer = (Customer)EPS.Utilities.ObjectHelper.DeserializeFromBinaryStream(stream);
        /// </example>
        public static object DeserializeFromBinaryStream(Stream stateStream)
        {
            return (new BinaryFormatter()).Deserialize(stateStream);
        }

        /// <summary>
        /// Deserializes the stream to an object
        /// </summary>
        /// <param name="stateStream">The state stream.</param>
        /// <returns>Object instance</returns>
        /// <remarks>
        /// For this to work, the stream must contain a serialized object
        /// This is an extension method.
        /// </remarks>
        /// <example>
        /// using EPS.Utilities;
        /// // more code
        /// Customer customer = (Customer)stream.DeserializeFromBinary();
        /// </example>
        public static object DeserializeFromBinary(this Stream stateStream)
        {
            return DeserializeFromBinaryStream(stateStream);
        }

        /// <summary>
        /// Serializes an object to its binary state
        /// </summary>
        /// <param name="objectToSerialize">The object to serialize.</param>
        /// <returns>
        /// Byte array of binary information for the object
        /// </returns>
        /// <remarks>
        /// For this to work, the provided object must be serializable.
        /// This method can be used as an extension method.
        /// </remarks>
        /// <example>
        /// using EPS.Utilities;
        /// // more code...
        /// byte[] serialized = customer.SerializeToBinaryArray();
        /// // or
        /// byte[] serialized = EPS.Utilities.ObjectHelper.SerializeToBinaryArray(customer);
        /// </example>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1720:AvoidTypeNamesInParameters", MessageId = "0#", Justification = "Following the rule would lead to more misleading code in this specific case.")]
        public static byte[] SerializeToBinaryArray(this object objectToSerialize)
        {
            using (var stream = objectToSerialize.SerializeToBinaryStream())
            {
                var buffer = new byte[stream.Length];
                stream.Read(buffer, 0, (int)stream.Length);
                return buffer;
            }
        }

        /// <summary>
        /// Serializes an object to its XML state
        /// </summary>
        /// <param name="objectToSerialize">The object to serialize.</param>
        /// <returns>
        /// XML stream representing the object's state
        /// </returns>
        /// <remarks>
        /// For this to work, the provided object must be serializable.
        /// This method can be used as an extension method.
        /// </remarks>
        /// <example>
        /// using EPS.Utilities;
        /// // more code
        /// Stream xmlStream = customer.SerializeToXmlStream();
        /// // or
        /// Stream xmlStream = EPS.Utilities.ObjectHelper.SerializeToXmlStream(customer);
        /// </example>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1720:AvoidTypeNamesInParameters", MessageId = "0#", Justification = "Following the rule would lead to more misleading code in this specific case.")]
        public static Stream SerializeToXmlStream(this object objectToSerialize)
        {
            var stream = new MemoryStream();
            var serializer = new XmlSerializer(objectToSerialize.GetType());
            serializer.Serialize(stream, objectToSerialize);
            return stream;
        }

        /// <summary>
        /// Serializes an object to its XML state
        /// </summary>
        /// <param name="objectToSerialize">The object to serialize.</param>
        /// <returns>
        /// XML string representing the object's state
        /// </returns>
        /// <remarks>
        /// For this to work, the provided object must be serializable.
        /// This method can be used as an extension method.
        /// </remarks>
        /// <example>
        /// using EPS.Utilities;
        /// // more code
        /// string xml = customer.SerializeToXmlString();
        /// // or
        /// string xml = EPS.Utilities.ObjectHelper.SerializeToXmlString(customer);
        /// </example>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1720:AvoidTypeNamesInParameters", MessageId = "0#", Justification = "Following the rule would lead to more misleading code in this specific case.")]
        public static string SerializeToXmlString(this object objectToSerialize)
        {
            return StreamHelper.ToString(SerializeToXmlStream(objectToSerialize));
        }

        /// <summary>
        /// Serializes an object to its XML state
        /// </summary>
        /// <param name="objectToSerialize">The object to serialize.</param>
        /// <returns>
        /// XML document representing the object's state
        /// </returns>
        /// <remarks>
        /// For this to work, the provided object must be serializable.
        /// This method can be used as an extension method.
        /// </remarks>
        /// <example>
        /// using EPS.Utilities;
        /// // more code
        /// XmlDocument xml = customer.SerializeToXmlDocument();
        /// // or
        /// XmlDocument xml = EPS.Utilities.ObjectHelper.SerializeToXmlDocument(customer);
        /// </example>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1059:MembersShouldNotExposeCertainConcreteTypes", MessageId = "System.Xml.XmlNode", Justification = "The goal of this method is to specifically expose an object of this interfaceType. We may add IXPathNavigable later."),
        System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1720:AvoidTypeNamesInParameters", MessageId = "0#", Justification = "Following the rule would lead to more misleading code in this specific case.")]
        public static XmlDocument SerializeToXmlDocument(object objectToSerialize)
        {
            var document = new XmlDocument();
            document.LoadXml(objectToSerialize.SerializeToXmlString());
            return document;
        }

        /// <summary>
        /// Deserializes an object from its state stored in an xml stream.
        /// </summary>
        /// <param name="stateStream">The state stream.</param>
        /// <param name="expectedType">The expected interfaceType (which will be the returned interfaceType).</param>
        /// <returns>Object instance.</returns>
        /// <remarks>
        /// For this to work, the XML Stream must contain a seralized object
        /// </remarks>
        /// <example>
        /// Customer customer = (Customer)EPS.Utilities.ObjectHelper.DeserializeFromXmlStream(stream, typeof(Customer));
        /// </example>
        public static object DeserializeFromXmlStream(Stream stateStream, Type expectedType)
        {
            var serializer = new XmlSerializer(expectedType);
            return serializer.Deserialize(stateStream);
        }

        /// <summary>
        /// Deserializes an object from its state stored in an xml stream.
        /// </summary>
        /// <param name="stateStream">The state stream.</param>
        /// <param name="expectedType">The expected interfaceType.</param>
        /// <returns>Object instance</returns>
        /// <remarks>
        /// For this to work, the XML Stream must contain a seralized object
        /// </remarks>
        /// <example>
        /// using EPS.Utilities;
        /// // more code
        /// Customer customer = (Customer)stream.DeserializeFromXmlStream(typeof(Customer));
        /// </example>
        public static object DeserializeFromXml(this Stream stateStream, Type expectedType)
        {
            return DeserializeFromXmlStream(stateStream, expectedType);
        }

        /// <summary>
        /// Serializes an object to its SOAP representation
        /// </summary>
        /// <param name="objectToSerialize">The object to serialize.</param>
        /// <returns>
        /// XML stream representing the object's state
        /// </returns>
        /// <remarks>
        /// For this to work, the provided object must be serializable.
        /// This method can be used as an extension method.
        /// </remarks>
        /// <example>
        /// using EPS.Utilities;
        /// // more code
        /// Stream stream = customer.SerializeToSoapStream();
        /// // or
        /// Stream stream = EPS.Utilities.ObjectHelper.SerializeToSoapStream(customer);
        /// </example>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1720:AvoidTypeNamesInParameters", MessageId = "0#", Justification = "Following the rule would lead to more misleading code in this specific case.")]
        public static Stream SerializeToSoapStream(this object objectToSerialize)
        {
            var stream = new MemoryStream();
            var formatter = new SoapFormatter();
            formatter.Serialize(stream, objectToSerialize);
            return stream;
        }

        /// <summary>
        /// Serializes an object to its SOAP state
        /// </summary>
        /// <param name="objectToSerialize">The object to serialize.</param>
        /// <returns>
        /// XML string representing the object's state
        /// </returns>
        /// <remarks>
        /// For this to work, the provided object must be serializable.
        /// This method can be used as an extension method.
        /// </remarks>
        /// <example>
        /// using EPS.Utilities;
        /// // more code
        /// string state = customer.SerializeToSoapString();
        /// // or
        /// string state = EPS.Utilities.ObjectHelper.SerializeToSoapString(customer);
        /// </example>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1720:AvoidTypeNamesInParameters", MessageId = "0#", Justification = "Following the rule would lead to more misleading code in this specific case.")]
        public static string SerializeToSoapString(this object objectToSerialize)
        {
            return StreamHelper.ToString(SerializeToSoapStream(objectToSerialize));
        }

        /// <summary>
        /// Serializes an object to its SOAP state
        /// </summary>
        /// <param name="objectToSerialize">The object to serialize.</param>
        /// <returns>
        /// XML document representing the object's state
        /// </returns>
        /// <remarks>
        /// For this to work, the provided object must be serializable.
        /// This method can be used as an extension method.
        /// </remarks>
        /// <example>
        /// using EPS.Utilities;
        /// // more code
        /// XmlDocument xml = customer.SerializeToSoapDocument();
        /// // or
        /// XmlDocument xml = EPS.Utilities.ObjectHelper.SerializeToSoapDocument(customer);
        /// </example>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1059:MembersShouldNotExposeCertainConcreteTypes", MessageId = "System.Xml.XmlNode", Justification = "The goal of this method is to specifically expose an object of this interfaceType. We may add IXPathNavigable later."),
        System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1720:AvoidTypeNamesInParameters", MessageId = "0#", Justification = "Following the rule would lead to more misleading code in this specific case.")]
        public static XmlDocument SerializeToSoapDocument(object objectToSerialize)
        {
            var document = new XmlDocument();
            document.LoadXml(SerializeToSoapString(objectToSerialize));
            return document;
        }

        /// <summary>
        /// Compares the values of two objects and returns true if the values are different
        /// </summary>
        /// <param name="value1">The first value.</param>
        /// <param name="value2">The second value.</param>
        /// <returns>True if values DIFFER</returns>
        /// <example>
        /// object o1 = "Hello";
        /// object o2 = "World";
        /// object o3 = 25;
        /// ObjectHelper.ValuesDiffer(o1, o2); // returns true;
        /// ObjectHelper.ValuesDiffer(o1, o3); // returns true;
        /// </example>
        /// <remarks>
        /// This method has been created to be easily able to compare objects of unknown types.
        /// In particular, this is useful when comparing two fields in a DataSet.
        /// This method can even handle byte arrays.
        /// </remarks>
        public static bool ValuesDiffer(object value1, object value2)
        {
            if (value1 == null) { throw new Exceptions.NullReferenceException(Properties.Resources.ParameterCanNotBeNull + " (value1)"); }
            if (value2 == null) { throw new Exceptions.NullReferenceException(Properties.Resources.ParameterCanNotBeNull + " (value2)"); }

            var fieldsDiffer = false;

            var comparableValue1 = value1 as IComparable;
            var comparableValue2 = value2 as IComparable;
            if (comparableValue1 != null && comparableValue2 != null) return !comparableValue1.Equals(comparableValue2);

            // Apparently, the values are not comparable. A likely scenario for this is that the
            // data is byte arrays, which do not implement IComparable, but they can still be compared.
            var array1 = value1 as byte[];
            var array2 = value2 as byte[];
            if (array1 != null && array2 != null)
            {
                if (array1.Length != array2.Length) return true; // Certainly not the same
                return array1.Where((t, arrayCounter) => t != array2[arrayCounter]).Any();
            }

            // This seems to be a little more complex
            try
            {
                // First of all, we check for nulls on one end
                var v1IsNull = value1 is DBNull;
                var v2IsNull = value2 is DBNull;
                if (v1IsNull && !v2IsNull || !v1IsNull && v2IsNull)
                {
                    // Note: I chose to return from here since setting
                    //       the return variable and waiting all the way
                    //       for the end of the method would have increased
                    //       the complexity of the method to a point where
                    //       I did not consider it helpful anymore.
                    return true;
                }
            }
            catch (InvalidCastException)
            {
                // This is odd. For some reason, we were not able to cast the value.
                // We inform the developer about the problem.
                fieldsDiffer = true;
            }
            return fieldsDiffer;
        }

        /// <summary>Dynamically retrieves a property value from the specified object</summary>
        /// <typeparam name="TResult">The interfaceType of the result.</typeparam>
        /// <param name="valueObject">The value object.</param>
        /// <param name="path">Name of the property.</param>
        /// <returns>Property value or default value</returns>
        /// <remarks>
        /// The property must be a readable instance property.
        /// This method can be called as an extension method.
        /// </remarks>
        /// <example>
        /// using EPS.Utilities;
        /// // more code
        /// var customer = this.GetCustomerObject();
        /// string name = customer.GetPropertyValue&lt;string&gt;("LastName");
        /// </example>
        public static TResult GetPropertyValue<TResult>(this object valueObject, string path)
        {
            // If this is a simple property name, we can simply get its value
            if (!path.Contains(".") && !path.Contains("[")) return GetSimplePropertyValue<TResult>(valueObject, path);

            // The path is a complex path syntax that first needs to be parsed before we can retrieve the property value
            try
            {
                object parentObject;
                var property = GetPropertyByPath(valueObject, path, out parentObject);
                if (property == null) return default(TResult);
                var propertyValue = property.GetValue(parentObject, null);
                return (TResult)propertyValue;
            }
            catch
            {
                return default(TResult);
            }
        }

        private static TResult GetSimplePropertyValue<TResult>(object valueObject, string propertyName)
        {
            try
            {
                var type = valueObject.GetType();
                var propertyInfo = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (propertyInfo != null) return (TResult)propertyInfo.GetValue(valueObject, null);
                return default(TResult);
            }
            catch
            {
                return default(TResult);
            }
        }

        /// <summary>
        /// Returns property information based on the provided path (path can be a simple property name or a more complex path)
        /// </summary>
        /// <param name="valueObject">The value object.</param>
        /// <param name="path">The path.</param>
        /// <param name="parentObject">The parent object.</param>
        /// <returns>PropertyInfo.</returns>
        public static PropertyInfo GetPropertyByPath(object valueObject, string path, out object parentObject)
        {
            if (valueObject == null)
            {
                parentObject = null;
                return null;
            }
            if (string.IsNullOrEmpty(path))
            {
                parentObject = null;
                return null;
            }

            var parts = path.Split('.');
            parentObject = valueObject;

            for (var propertyCounter = 0; propertyCounter < parts.Length; propertyCounter++)
            {
                var part = parts[propertyCounter];
                var valueObjectType = valueObject.GetType();
                if (!part.Contains("["))
                {
                    // This is a simple path expression
                    var propertyInfo = valueObjectType.GetProperty(part, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetProperty);
                    if (propertyInfo == null) return null;
                    if (propertyCounter == parts.Length - 1) return propertyInfo; // This is the last one (right-most one) in the path, so that is the one we are after
                    valueObject = propertyInfo.GetValue(valueObject, null);
                    parentObject = valueObject;
                }
                else
                {
                    // This is an indexed property
                    var partName = part.Substring(0, part.IndexOf("[", StringComparison.Ordinal));
                    var indexExpression = part.Substring(part.IndexOf("[", StringComparison.Ordinal) + 1);
                    indexExpression = indexExpression.Substring(0, indexExpression.IndexOf("]", StringComparison.Ordinal));
                    int index;
                    if (!int.TryParse(indexExpression, out index)) return null;

                    // We first get the main object property
                    var propertyInfo = valueObjectType.GetProperty(partName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetProperty);
                    if (propertyInfo == null) return null;
                    valueObject = propertyInfo.GetValue(valueObject, null);
                    if (valueObject == null) return null;
                    valueObjectType = valueObject.GetType();

                    // Now, we get the indexed property
                    var indexerProperties = valueObjectType.GetProperties().Where(p => p.Name == "Item").ToList();
                    PropertyInfo indexerPropertyInfo = null;
                    if (indexerProperties.Count < 1) return null;
                    if (indexerProperties.Count == 1) indexerPropertyInfo = indexerProperties[0];
                    else
                        foreach (var indexerProperty in indexerProperties)
                        {
                            var indexerParameters = indexerProperty.GetIndexParameters();
                            if (indexerParameters.Length != 1 || indexerParameters[0].ParameterType != typeof (int)) continue;
                            indexerPropertyInfo = indexerProperty;
                            break;
                        }
                    if (indexerPropertyInfo == null) return null;
                    if (propertyCounter == parts.Length - 1) return indexerPropertyInfo; // This is the last one (right-most one) in the path, so that is the one we are after
                    
                    valueObject = indexerPropertyInfo.GetValue(valueObject, new object[] {index});
                    parentObject = valueObject;
                }

                if (valueObject == null) break;
            }

            return null;
        }

        /// <summary>
        /// Dynamically retrieves a property value from the specified object
        /// </summary>
        /// <typeparam name="TValue">The interfaceType of the value that is to be set.</typeparam>
        /// <param name="valueObject">The value object.</param>
        /// <param name="path">Name of the property.</param>
        /// <param name="value">The value that is to be set.</param>
        /// <returns>True if the value was set successfully</returns>
        /// <remarks>
        /// The property must be a writable instance property.
        /// This method can be called as an extension method.
        /// </remarks>
        /// <example>
        /// using EPS.Utilities;
        /// // more code
        /// var customer = this.GetCustomerObject();
        /// customer.SetPropertyValue("LastName", "Smith");
        /// </example>
        public static bool SetPropertyValue<TValue>(this object valueObject, string path, TValue value)
        {
            // If this is a simple property name, we can simply get its value
            if (!path.Contains(".") && !path.Contains("[")) return SetSimplePropertyValue<TValue>(valueObject, path, value);

            // The path is a complex path syntax that first needs to be parsed before we can retrieve the property value
            try
            {
                object parentObject;
                var property = GetPropertyByPath(valueObject, path, out parentObject);
                if (property == null) return false;
                property.SetValue(parentObject, value, null);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool SetSimplePropertyValue<TValue>(this object valueObject, string propertyName, TValue value)
        {
            try
            {
                var type = valueObject.GetType();
                var propertyInfo = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (propertyInfo == null) return false;
                propertyInfo.SetValue(valueObject, value, null);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Dynamically invokes the specified method on the defined object
        /// </summary>
        /// <typeparam name="TResult">The expected return interfaceType for the method</typeparam>
        /// <param name="valueObject">The value object (object that contains the method).</param>
        /// <param name="methodName">Name of the method.</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns>The method's return value</returns>
        /// <remarks>
        /// The method must be an instance method
        /// This method can be called as an extension method.
        /// </remarks>
        /// <example>
        /// using EPS.Utilities;
        /// // more code
        /// var customer = this.GetCustomerObject();
        /// object[] parameters = { "John", "M.", "Smith" };
        /// string fullName = customer.InvokeMethod&lt;string&gt;("GetFullName", parameters);
        /// </example>
        public static TResult InvokeMethod<TResult>(this object valueObject, string methodName, object[] parameters)
        {
            try
            {
                var type = valueObject.GetType();
                var methodInfo = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (methodInfo != null) return (TResult)methodInfo.Invoke(valueObject, parameters);
                return default(TResult);
            }
            catch
            {
                return default(TResult);
            }
        }

        /// <summary>
        /// Returns all methods defined on an interface or interfaces it inherits from.
        /// </summary>
        /// <param name="interfaceType">The interface type.</param>
        /// <returns>IEnumerable&lt;MethodInfo&gt;.</returns>
        public static IEnumerable<MethodInfo> GetAllMethodsForInterface(Type interfaceType)
        {
            var methods = interfaceType.GetMethods(BindingFlags.Instance | BindingFlags.Public).ToList();
            GetMethodsForInheritedInterfaces(interfaceType, methods);
            return methods;
        }
        private static void GetMethodsForInheritedInterfaces(Type interfaceType, List<MethodInfo> existingMethods)
        {
            var interfaces = interfaceType.GetInterfaces();
            foreach (var inheritedInterface in interfaces)
            {
                existingMethods.AddRange(inheritedInterface.GetMethods(BindingFlags.Instance | BindingFlags.Public));
                GetMethodsForInheritedInterfaces(inheritedInterface, existingMethods);
            }
        }
    }

    /// <summary>
    /// Exception thrown whenever object instantiation fails.
    /// </summary>
    [Serializable]
    public class ObjectInstantiationException : Exception
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public ObjectInstantiationException() : base("Error instantiating object.") { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="message">Exception message</param>
        public ObjectInstantiationException(string message) : base(message) { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="message">Exception message</param>
        /// <param name="innerException">Inner exception</param>
        public ObjectInstantiationException(string message, Exception innerException) : base(message, innerException) { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="info">Serialization info</param>
        /// <param name="context">Streaming context</param>
        protected ObjectInstantiationException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}

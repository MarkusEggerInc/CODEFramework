using System;
using System.Collections.Generic;

namespace CODE.Framework.Core.Utilities
{
    /// <summary>
    /// This class provides various helper functions for dealing with enums
    /// </summary>
    public static class EnumHelper
    {
        /// <summary>Returns a list (enumerable) of information items for each value in the enum</summary>
        /// <typeparam name="T">Enum type</typeparam>
        /// <returns>Enumerable list of enum information items (wich can be very useful for binding for instance)</returns>
        public static IEnumerable<EnumInformation> GetEnumInformation<T>()
        {
            if (_knownEnumInformation.ContainsKey(typeof(T)))
                return _knownEnumInformation[typeof (T)];

            var result = new List<EnumInformation>();

            var values = Enum.GetValues(typeof (T));
            var names = Enum.GetNames(typeof (T));
            int counter = 0;
            foreach (var value in values)
            {
                var intVal = (int) value;
                result.Add(new EnumInformation(intVal, value, names[counter]));
                counter++;
            }

            _knownEnumInformation.Add(typeof (T), result);

            return result;
        }

        /// <summary>Internal cache to avoid having to re-discover enums all the time</summary>
        private static Dictionary<Type, List<EnumInformation>> _knownEnumInformation = new Dictionary<Type, List<EnumInformation>>();
    }

    /// <summary>This class represents meta information about an enum</summary>
    public class EnumInformation
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="value">Integer value</param>
        /// <param name="enumValue">Original enum value</param>
        /// <param name="name">Enum value name</param>
        public EnumInformation(int value, object enumValue, string name)
        {
            Value = value;
            EnumValue = enumValue;
            Name = name;
            DisplayText = StringHelper.SpaceCamelCase(Name.Replace('_', ' '));
        }

        /// <summary>Value as an integer</summary>
        public int Value { get; private set; }
        /// <summary>The selected enum value as an object</summary>
        public object EnumValue { get; private set; }
        /// <summary>The enum name spelled out as separate words</summary>
        public string DisplayText { get; set; }
        /// <summary>Enum value name</summary>
        public string Name { get; private set; }
    }
}

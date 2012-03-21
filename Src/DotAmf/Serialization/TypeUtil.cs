using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace DotAmf.Serialization
{
    /// <summary>
    /// Type utility.
    /// </summary>
    static internal class TypeUtil
    {
        /// <summary>
        /// Instantiate an object and populate it with provided property values.
        /// </summary>
        /// <param name="type">Object type.</param>
        /// <param name="properties">Object properties.</param>
        /// <returns>Object instance.</returns>
        static public object InstantiateObject(Type type, IEnumerable<KeyValuePair<string, object>> properties)
        {
            var instance = Activator.CreateInstance(type);

            foreach (var data in properties)
                SetPropertyOnObject(instance, data.Key, data.Value);

            return instance;
        }

        /// <summary>
        /// Set a property's value on an object.
        /// </summary>
        /// <param name="instance">Object to set property on.</param>
        /// <param name="propertyName">Property name.</param>
        /// <param name="value">Property value to set.</param>
        static public void SetPropertyOnObject(object instance, string propertyName, object value)
        {
            var type = instance.GetType();

            var validProperties = from property in type.GetProperties()
                                  where property.CanWrite
                                  select property;

            foreach (var property in validProperties)
            {
                var contractAttribute =
                    property.GetCustomAttributes(typeof(DataMemberAttribute), true).FirstOrDefault() as
                    DataMemberAttribute;

                if (contractAttribute == null) continue;

                //Property overrides its default name
                if (contractAttribute.Name == propertyName || property.Name == propertyName)
                {
                    property.SetValue(instance, value, null);
                    return;
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace DotAmf.Serialization
{
    /// <summary>
    /// Data contract utility.
    /// </summary>
    static internal class DataContractUtil
    {
        /// <summary>
        /// Get data contract type's alias.
        /// </summary>
        /// <param name="type">Data contract type.</param>
        /// <returns>Alias name.</returns>
        /// <exception cref="ArgumentException">Type is not a valid AMF data contract.</exception>
        static public string GetContractAlias(Type type)
        {
            if (type == null) throw new ArgumentNullException("type");

            //Look for a data contract attribute first
            var contractAttribute =
                    type.GetCustomAttributes(typeof(DataContractAttribute), false).FirstOrDefault() as
                    DataContractAttribute;

            if (contractAttribute != null)
            {
                return !string.IsNullOrEmpty(contractAttribute.Name)
                                ? contractAttribute.Name
                                : type.FullName ?? type.Name;
            }

            throw new ArgumentException(Errors.DataContractUtil_GetContractAlias_InvalidContract, "type");
        }

        /// <summary>
        /// Instantiate a data contract object and populate it with provided properties.
        /// </summary>
        /// <param name="type">Data contract type.</param>
        /// <param name="properties">Properties to use.</param>
        /// <returns>Data contract instance.</returns>
        static public object InstantiateContract(Type type, IEnumerable<KeyValuePair<string, object>> properties)
        {
            if (type == null) throw new ArgumentNullException("type");
            if (properties == null) throw new ArgumentNullException("properties");

            var instance = Activator.CreateInstance(type);

            var map = from data in GetContractProperties(type)
                      join prop in properties on data.Key equals prop.Key
                      select new { property = data.Value, value = prop.Value };

            foreach (var pair in map)
                pair.property.SetValue(instance, pair.value, null);

            return instance;
        }

        /// <summary>
        /// Get data contract object's properties.
        /// </summary>
        /// <param name="instance">Object instance.</param>
        /// <returns>A set of property name-value pairs.</returns>
        static public IEnumerable<KeyValuePair<string, object>> GetContractProperties(object instance)
        {
            if (instance == null) throw new ArgumentNullException("instance");

            return from data in GetContractProperties(instance.GetType())
                   select new KeyValuePair<string, object>(data.Key, data.Value.GetValue(instance, null));
        }

        /// <summary>
        /// Get properties of a data contract type.
        /// </summary>
        /// <param name="type">Data contract type.</param>
        /// <returns>A set of name-property pairs.</returns>
        static private IEnumerable<KeyValuePair<string, PropertyInfo>> GetContractProperties(Type type)
        {
            if (type == null) throw new ArgumentNullException("type");

            var validProperties = from property in type.GetProperties()
                                  where property.CanWrite && property.CanRead
                                  select property;

            foreach (var property in validProperties)
            {
                //Look for a data contract attribute first
                var contractAttribute =
                    property.GetCustomAttributes(typeof(DataMemberAttribute), true).FirstOrDefault() as
                    DataMemberAttribute;

                if (contractAttribute != null)
                {
                    var propertyName = !string.IsNullOrEmpty(contractAttribute.Name)
                                           ? contractAttribute.Name
                                           : property.Name;

                    yield return new KeyValuePair<string, PropertyInfo>(propertyName, property);
                    continue;
                }
            }
        }
    }
}

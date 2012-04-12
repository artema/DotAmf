using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using DotAmf.Data;

namespace DotAmf.Serialization
{
    /// <summary>
    /// Data contract helper.
    /// </summary>
    static internal class DataContractHelper
    {
        #region Constants
        /// <summary>
        /// Proxy type name prefix.
        /// </summary>
        private const string ProxyTypePrefix = "ProxyType";
        #endregion

        #region Public methods
        /// <summary>
        /// Create assembly type name for a proxy type alias.
        /// </summary>
        /// <param name="typeAlias">Type alias.</param>
        /// <returns>Type name, suitable for using as an assembly type name.</returns>
        static public string CreateProxyTypeName(string typeAlias)
        {
            if (typeAlias == null) throw new ArgumentNullException("typeAlias");
            return ProxyTypePrefix + typeAlias.GetHashCode();
        }

        /// <summary>
        /// Get data contract type's alias.
        /// </summary>
        /// <param name="type">Data contract type.</param>
        /// <returns>Alias name.</returns>
        /// <exception cref="ArgumentException">Type is not a valid data contract.</exception>
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

            throw new ArgumentException(string.Format(Errors.DataContractUtil_GetContractAlias_InvalidContract, type.FullName), "type");
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

            var propertyMap = from data in GetContractProperties(type)
                              join prop in properties on data.Key equals prop.Key
                              select new { property = data.Value, value = prop.Value };

            foreach (var pair in propertyMap)
                pair.property.SetValue(instance, pair.value, null);

            var fieldMap = from data in GetContractFields(type)
                           join prop in properties on data.Key equals prop.Key
                           select new { field = data.Value, value = prop.Value };

            foreach (var pair in fieldMap)
                pair.field.SetValue(instance, pair.value);

            return instance;
        }

        /// <summary>
        /// Get data contract object's properties.
        /// </summary>
        /// <param name="instance">Object instance.</param>
        /// <returns>A set of property name-value pairs.</returns>
        static public Dictionary<string, object> GetContractProperties(object instance)
        {
            if (instance == null) throw new ArgumentNullException("instance");

            var type = instance.GetType();

            var properties = from data in GetContractProperties(type)
                             select new KeyValuePair<string, object>(data.Key, data.Value.GetValue(instance, null));

            var fields = from data in GetContractFields(type)
                         select new KeyValuePair<string, object>(data.Key, data.Value.GetValue(instance));

            var contents = properties.Concat(fields);

            var map = new Dictionary<string, object>();

            foreach (var pair in contents)
                map[pair.Key] = pair.Value;

            return map;
        }

        /// <summary>
        /// Create dynamic untyped object.
        /// </summary>
        /// <param name="source">Source object's properties.</param>
        /// <returns></returns>
        static public AmfObject CreateDynamicObject(IDictionary<string, object> source)
        {
            if (source == null) throw new ArgumentNullException("source");

            var obj = new AmfObject
            {
                Properties = new Dictionary<string, object>(source),
                Traits = new AmfTypeTraits
                {
                    IsDynamic = true,
                    TypeName = AmfTypeTraits.BaseTypeAlias,
                    ClassMembers = new string[0]
                }
            };

            return obj;
        }

        /// <summary>
        /// Get contract members from a contract type.
        /// </summary>
        static public IEnumerable<Type> GetContractMembers(Type type)
        {
            if (type == null) throw new ArgumentNullException("type");


            var properties = from pair in GetContractProperties(type)
                             select pair.Value.PropertyType;

            var fields = from pair in GetContractFields(type)
                         select pair.Value.FieldType;

            return properties.Concat(fields).Distinct();
        }
        #endregion

        #region Private methods
        /// <summary>
        /// Get properties of data contract type.
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

        /// <summary>
        /// Get fields of data contract type.
        /// </summary>
        /// <param name="type">Data contract type.</param>
        /// <returns>A set of name-field pairs.</returns>
        static private IEnumerable<KeyValuePair<string, FieldInfo>> GetContractFields(Type type)
        {
            if (type == null) throw new ArgumentNullException("type");

            var validFields = from field in type.GetFields()
                              where field.IsPublic && !field.IsStatic
                              select field;

            foreach (var field in validFields)
            {
                //Look for a data contract attribute first
                var contractAttribute =
                    field.GetCustomAttributes(typeof(DataMemberAttribute), true).FirstOrDefault() as
                    DataMemberAttribute;

                if (contractAttribute != null)
                {
                    var propertyName = !string.IsNullOrEmpty(contractAttribute.Name)
                                           ? contractAttribute.Name
                                           : field.Name;

                    yield return new KeyValuePair<string, FieldInfo>(propertyName, field);
                    continue;
                }
            }
        }
        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace DotAmf.Serialization
{
    /// <summary>
    /// Data contract helper.
    /// </summary>
    public static class DataContractHelper
    {
        #region Public methods
        /// <summary>
        /// Check if type is a valid data contract.
        /// </summary>
        static public bool IsDataContract(Type type)
        {
            if (type == null) throw new ArgumentNullException("type");

            //Look for a data contract attribute
            var contractAttribute =
                    type.GetCustomAttributes(typeof(DataContractAttribute), false).FirstOrDefault() as
                    DataContractAttribute;

            return (contractAttribute != null);
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

            //Look for a data contract attribute
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

        /// <summary>
        /// Get properties of data contract type.
        /// </summary>
        /// <param name="type">Data contract type.</param>
        /// <returns>A set of name-property pairs.</returns>
        static public IEnumerable<KeyValuePair<string, PropertyInfo>> GetContractProperties(Type type)
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
        static public IEnumerable<KeyValuePair<string, FieldInfo>> GetContractFields(Type type)
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

        /// <summary>
        /// Check if type is a numeric type.
        /// </summary>
        static public bool IsNumericType(Type type)
        {
            bool isInteger;
            return IsNumericType(type, out isInteger);
        }

        /// <summary>
        /// Check if type is a numeric type.
        /// </summary>
        static public bool IsNumericType(Type type, out bool isInteger)
        {
            isInteger = false;

            if (type == null) return false;

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    isInteger = true;
                    return true;

                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    return true;

                case TypeCode.Object:
                {
                    if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                        return IsNumericType(Nullable.GetUnderlyingType(type), out isInteger);

                    return false;
                }
            }

            return false;
        }

        /// <summary>
        /// Convert a <c>DateTime</c> to a UNIX timestamp in milliseconds.
        /// </summary>
        static public double ConvertToTimestamp(DateTime value)
        {
            var origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);

            if (value.Kind != DateTimeKind.Utc)
                origin = origin.ToLocalTime();

            return (value - origin).TotalSeconds * 1000;
        }
        #endregion
    }
}

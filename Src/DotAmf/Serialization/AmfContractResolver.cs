using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace DotAmf.Serialization
{
    #region Interface declaration
    /// <summary>
    /// AMF data contract resolver.
    /// </summary>
    public interface IAmfContractResolver
    {
        /// <summary>
        /// Resolve a type by alias.
        /// </summary>
        /// <param name="typeAlias">Type's alias.</param>
        /// <returns><c>Type</c> object or <c>null</c> 
        /// if type with a given alias is not found.</returns>
        Type Resolve(string typeAlias);

        /// <summary>
        /// Get registered alias for a type.
        /// </summary>
        /// <param name="type">Type to check for an alias.</param>
        /// <returns>Alias <c>string</c> or <c>null</c> if type is not registered.</returns>
        string GetAlias(Type type);
    }
    #endregion

    #region Basic implementation
    /// <summary>
    /// AMF data contract resolver.
    /// </summary>
    sealed public class AmfContractResolver : IAmfContractResolver
    {
        #region .ctor
        /// <summary>
        /// Constructor.
        /// </summary>
        public AmfContractResolver()
        {
            _registry = new Dictionary<string, Type>();
        }
        #endregion

        #region Data
        /// <summary>
        /// Contracts registry.
        /// </summary>
        private readonly Dictionary<string, Type> _registry;
        #endregion

        #region Public methods
        /// <summary>
        /// Register data contract.
        /// </summary>
        /// <param name="type">Type to register.</param>
        public void AddContract(Type type)
        {
            if (type == null) throw new ArgumentNullException("type");

            AddContract(type, type.FullName);
        }

        /// <summary>
        /// Register data contract.
        /// </summary>
        /// <param name="type">Type to register.</param>
        /// <param name="alias">Data contract alias to use.</param>
        /// <exception cref="InvalidOperationException">Alias already taken by another type.</exception>
        public void AddContract(Type type, string alias)
        {
            if (type == null) throw new ArgumentNullException("type");
            if (string.IsNullOrEmpty(alias)) throw new ArgumentException(Errors.AmfContractResolver_InvalidAliasName, "alias");

            if (_registry.ContainsKey(alias))
                throw new InvalidOperationException(string.Format(Errors.AmfContractResolver_AliasCollision, alias));

            if (!TryToAddType(type, alias))
                throw new Exception(Errors.AmfContractResolver_TypeRegistrationError);
        }

        /// <summary>
        /// Register all available data contracts from a given assembly.
        /// </summary>
        /// <param name="assembly">Assembly to look for data contracts.</param>
        public void AddContract(Assembly assembly)
        {
            if (assembly == null) throw new ArgumentNullException("assembly");

            var validTypes = from type in assembly.GetTypes()
                             where type.IsClass && type.IsPublic && !type.IsGenericType
                             select type;

            foreach (var type in validTypes)
                TryToAddType(type);
        }
        #endregion

        #region Private methods
        /// <summary>
        /// Try to register a type as a data contract.
        /// </summary>
        /// <param name="type">Type to register.</param>
        /// <param name="alias">Optional alias to use.</param>
        /// <returns>Returns <c>true</c> if type has been registered sucessfully,
        /// otherwise returns <c>false</c>.</returns>
        private bool TryToAddType(Type type, string alias = null)
        {
            //Look for a data contract attribute first
            var contractAttribute =
                    type.GetCustomAttributes(typeof(DataContractAttribute), false).FirstOrDefault() as
                    DataContractAttribute;

            if (contractAttribute != null)
            {
                if (alias == null)
                    alias = !string.IsNullOrEmpty(contractAttribute.Name)
                                ? contractAttribute.Name
                                : type.FullName ?? type.Name;

                _registry[alias] = type;
                return true;
            }

            return false;
        }
        #endregion

        #region IAmfContractResolver implementation
        public Type Resolve(string typeAlias)
        {
            if (string.IsNullOrEmpty(typeAlias)) throw new ArgumentException();
            return _registry[typeAlias];
        }

        public string GetAlias(Type type)
        {
            return _registry
                .Where(line => line.Value == type)
                .Select(line => line.Key)
                .FirstOrDefault();
        }
        #endregion
    }
    #endregion
}

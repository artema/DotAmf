using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using System.Xml;
using DotAmf.Data;

namespace DotAmf.Serialization
{
    /// <summary>
    /// AMF data contract resolver.
    /// </summary>
    internal class AmfDataContractResolver : AmfDataContractDereferencer
    {
        #region .ctor
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="proxyContainer"><c>ModuleBuilder</c> for persisting proxy types.</param>
        public AmfDataContractResolver(ModuleBuilder proxyContainer)
        {
            _proxyContainer = proxyContainer;

            _contractsRegistry = new Dictionary<string, Type>();
            _proxyRegistry = new Dictionary<string, Type>();
        }
        #endregion

        #region Constants
        /// <summary>
        /// AMF proxy type name prefix.
        /// </summary>
        private const string ProxyTypeNamePrefix = "AmfProxy_";
        #endregion

        #region Data
        /// <summary>
        /// Proxy types container.
        /// </summary>
        private readonly ModuleBuilder _proxyContainer;

        /// <summary>
        /// Proxy registry.
        /// </summary>
        private readonly Dictionary<string, Type> _proxyRegistry;

        /// <summary>
        /// Contracts registry.
        /// </summary>
        private readonly Dictionary<string, Type> _contractsRegistry;
        #endregion

        #region DataContractResolver implementation
        public override Type ResolveName(string typeName, string typeNamespace, Type declaredType, DataContractResolver knownTypeResolver)
        {
            //An AMF type or type proxy
            if (typeNamespace == AmfSerializationContext.AmfNamespace)
            {
                string alias;
                Type type;

                //Check if type is a proxy
                if (IsAmfProxyTypeName(typeName, out alias))
                {
                    //Get contract type by alias name
                    type = ResolveContract(alias);
                    if (type != null) return type;
                }
                //Check if type is an AMF type
                else if (IsAmfTypeName(typeName, out type))
                {
                    return type;
                }
            }

            //Look for a known type
            var res = knownTypeResolver.ResolveName(typeName, typeNamespace, declaredType, null);
            return res;
        }

        public override bool TryResolveType(Type type, Type declaredType, DataContractResolver knownTypeResolver, out XmlDictionaryString typeName, out XmlDictionaryString typeNamespace)
        {
            //Type is a proxy
            if (type.Assembly.FullName == _proxyContainer.Assembly.FullName)
            {
                //Get type's alias
                var typeAlias = GetProxyAlias(type);

                //Prepare a type name for serialization
                //so that we won't have any name collisions
                var contractTypeName = BuildTypeName(typeAlias);

                typeName = XmlDictionary.Add(contractTypeName);
                typeNamespace = XmlDictionary.Add(AmfSerializationContext.AmfNamespace);
                return true;
            }

            string alias;

            //A generic AMF object
            if (IsAmfType(type, out alias))
            {
                typeName = XmlDictionary.Add(alias);
                typeNamespace = XmlDictionary.Add(AmfSerializationContext.AmfNamespace);
                return true;
            }

            //Look for a known type
            var res = knownTypeResolver.TryResolveType(type, declaredType, null, out typeName, out typeNamespace);
            return res;
        }
        #endregion

        #region Proxy objects
        /// <summary>
        /// Create an AMF proxy object.
        /// </summary>
        /// <param name="source">Source AMF object.</param>
        /// <returns>Proxy object.</returns>
        public object CreateProxyObject(AmfObject source)
        {
            //An externizable type
            if (source.Traits.IsExternalizable)
                return source;

            var typeName = source.Traits.TypeName;

            //A dynamic or untyped object. No proxy can be created.
            if (string.IsNullOrEmpty(typeName) || source.Traits.IsDynamic)
                return DataContractHelper.GetContractProperties(source);

            //If contract for the alias is not registered, 
            //then proxy type is not required at this point
            if (!_contractsRegistry.ContainsKey(typeName))
                return source;

            //Check if proxy already exists
            var proxy = _proxyRegistry.ContainsKey(typeName)
                ? _proxyRegistry[typeName]
                : DefineProxy(source);

            //Instantiate proxy and populate it with source object's values
            return DataContractHelper.InstantiateContract(proxy, source.Properties);
        }

        /// <summary>
        /// Define proxy type.
        /// </summary>
        /// <param name="source">Source AMF object.</param>
        /// <returns>New proxy type.</returns>
        private Type DefineProxy(AmfObject source)
        {
            var typeName = source.Traits.TypeName;

            if (_proxyRegistry.ContainsKey(typeName)) throw new InvalidOperationException(Errors.AmfDataContractResolver_ProxyTypeAlreadyExists);

            //Create proxy type
            var typeBuilder = _proxyContainer.DefineType(
                DataContractHelper.CreateProxyTypeName(typeName),
                TypeAttributes.Public);

            //Add data contract attribute
            typeBuilder.SetCustomAttribute(ProxyContractAttribute);

            //Create proxy data member fields
            foreach (var property in source.Keys)
            {
                var value = source[property];
                var propertyType = value != null
                                       ? value.GetType()
                                       : typeof(object);

                if (propertyType == typeof(object[]))
                    propertyType = typeof(object);

                var fieldBuilder = typeBuilder.DefineField(property, propertyType, FieldAttributes.Public);
                fieldBuilder.SetCustomAttribute(ProxyMemberAttribute);
            }

            //Instantiate and register proxy type
            var proxyType = typeBuilder.CreateType();
            _proxyRegistry[typeName] = proxyType;

            return proxyType;
        }

        /// <summary>
        /// Get proxy type's alias.
        /// </summary>
        /// <param name="proxyType">Proxy type to check.</param>
        /// <returns>Proxy type's alias.</returns>
        public string GetProxyAlias(Type proxyType)
        {
            return _proxyRegistry
                .Where(line => line.Value == proxyType)
                .Select(line => line.Key)
                .First();
        }
        #endregion

        #region Helper methods
        /// <summary>
        /// Check if a type name is an AMF proxy type name.
        /// </summary>
        /// <param name="typeName">Type name to check.</param>
        /// <param name="alias">Type's alias of <c>null</c> if type name is not an AMF type.</param>
        static private bool IsAmfProxyTypeName(string typeName, out string alias)
        {
            if (typeName.StartsWith(ProxyTypeNamePrefix))
            {
                alias = typeName.Substring(ProxyTypeNamePrefix.Length, typeName.Length - ProxyTypeNamePrefix.Length);
                return true;
            }

            alias = null;
            return false;
        }

        /// <summary>
        /// Build type name from type alias.
        /// </summary>
        /// <param name="aliasName">Type's alias.</param>
        /// <returns>Type name.</returns>
        static private string BuildTypeName(string aliasName)
        {
            return ProxyTypeNamePrefix + aliasName;
        }

        /// <summary>
        /// Default proxy data contract attribute.
        /// </summary>
        static private readonly CustomAttributeBuilder ProxyContractAttribute = CreateProxyContractAttribute();

        /// <summary>
        /// Create default proxy data contract attribute.
        /// </summary>
        /// <returns></returns>
        static private CustomAttributeBuilder CreateProxyContractAttribute()
        {
            return new CustomAttributeBuilder(
                typeof(DataContractAttribute).GetConstructor(Type.EmptyTypes),
                new object[0],
                    typeof(DataContractAttribute).GetProperties().Where(pr => pr.Name == "Namespace").ToArray(),
                    new object[] { AmfSerializationContext.AmfNamespace });
        }

        /// <summary>
        /// Default proxy data member attribute.
        /// </summary>
        static private readonly CustomAttributeBuilder ProxyMemberAttribute = CreateProxyMemberAttribute();

        /// <summary>
        /// Create default proxy data member attribute.
        /// </summary>
        /// <returns></returns>
        static private CustomAttributeBuilder CreateProxyMemberAttribute()
        {
            return new CustomAttributeBuilder(
                    typeof(DataMemberAttribute).GetConstructor(Type.EmptyTypes),
                    new object[0]);
        }
        #endregion

        #region Register contracts
        /// <summary>
        /// Register data contract.
        /// </summary>
        /// <param name="type">Type to register.</param>
        /// <exception cref="ArgumentException">Invalid alias name.</exception>
        /// <exception cref="InvalidOperationException">Type already registered.</exception>
        public void AddContract(Type type)
        {
            if (type == null) throw new ArgumentNullException("type");

            //Try to register the type
            if (!TryToAddType(type))
                throw new Exception(Errors.AmfContractResolver_TypeRegistrationError);
        }

        /// <summary>
        /// Register data contract under an alias.
        /// </summary>
        /// <param name="type">Type to register.</param>
        /// <param name="alias">Data contract alias to use.</param>
        /// <exception cref="ArgumentException">Invalid alias name.</exception>
        /// <exception cref="InvalidOperationException">Alias already taken by another type.</exception>
        public void AddContract(Type type, string alias)
        {
            if (type == null) throw new ArgumentNullException("type");
            if (string.IsNullOrEmpty(alias)) throw new ArgumentException(Errors.AmfContractResolver_InvalidAliasName, "alias");

            //Alias already registered
            if (_contractsRegistry.ContainsKey(alias))
                throw new InvalidOperationException(string.Format(Errors.AmfContractResolver_AliasCollision, alias));

            //Try to register the type
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

            //Scan the assebly for valid types
            var validTypes = from type in assembly.GetTypes()
                             where type.IsClass && type.IsPublic && !type.IsGenericType
                             select type;

            //Try to add all valid types
            foreach (var type in validTypes)
                TryToAddType(type);
        }

        /// <summary>
        /// Try to register a type as a data contract.
        /// </summary>
        /// <param name="type">Type to register.</param>
        /// <param name="alias">Optional alias to use.</param>
        /// <returns>Returns <c>true</c> if type has been registered sucessfully,
        /// otherwise returns <c>false</c>.</returns>
        private bool TryToAddType(Type type, string alias = null)
        {
            try
            {
                var defaultAlias = DataContractHelper.GetContractAlias(type);
                _contractsRegistry[alias ?? defaultAlias] = type;

                return true;
            }
            catch
            {
                //Type is not a valid data contract
                return false;
            }
        }
        #endregion

        #region Contract resolving
        /// <summary>
        /// Get data contract type's alias.
        /// </summary>
        /// <param name="contractType">Type to check.</param>
        /// <returns>Contract type's alias or <c>null</c> if contract is not registered.</returns>
        public string GetContractAlias(Type contractType)
        {
            return _contractsRegistry
                .Where(line => line.Value == contractType)
                .Select(line => line.Key)
                .FirstOrDefault();
        }

        /// <summary>
        /// Resolve contract type by an alias name.
        /// </summary>
        /// <param name="typeAlias">Contract's alias.</param>
        /// <returns>Contract type with a given alias or <c>null</c> if contract is not registered.</returns>
        private Type ResolveContract(string typeAlias)
        {
            if (string.IsNullOrEmpty(typeAlias) || !_contractsRegistry.ContainsKey(typeAlias)) return null;
            return _contractsRegistry[typeAlias];
        }
        #endregion

        #region AMF proxy surrogate
        /// <summary>
        /// AMF proxy surrogate.
        /// </summary>
        sealed public class ProxySurrogate : IDataContractSurrogate
        {
            #region .ctor
            public ProxySurrogate(Func<AmfObject, object> proxyFactoryMethod)
            {
                _proxyFactoryMethod = proxyFactoryMethod;
            }
            #endregion

            #region Data
            /// <summary>
            /// AMF proxy object factory method.
            /// </summary>
            private readonly Func<AmfObject, object> _proxyFactoryMethod;
            #endregion

            #region IDataContractSurrogate implementation
            public Type GetDataContractType(Type type) { return type; }

            public object GetDeserializedObject(object obj, Type targetType) { return obj; }

            public object GetObjectToSerialize(object obj, Type targetType)
            {
                return obj.GetType() == typeof(AmfObject)
                    ? _proxyFactoryMethod.Invoke((AmfObject)obj)
                    : obj;
            }

            #region Not supported methods
            public object GetCustomDataToExport(Type clrType, Type dataContractType)
            {
                throw new NotSupportedException();
            }

            public object GetCustomDataToExport(MemberInfo memberInfo, Type dataContractType)
            {
                throw new NotSupportedException();
            }

            public void GetKnownCustomDataTypes(System.Collections.ObjectModel.Collection<Type> customDataTypes)
            {
                throw new NotSupportedException();
            }

            public Type GetReferencedTypeOnImport(string typeName, string typeNamespace, object customData)
            {
                throw new NotSupportedException();
            }

            public CodeTypeDeclaration ProcessImportedType(CodeTypeDeclaration typeDeclaration, CodeCompileUnit compileUnit)
            {
                throw new NotSupportedException();
            }
            #endregion
            #endregion
        }
        #endregion
    }
}

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using DotAmf.Serialization;

namespace DotAmf.Data
{
    /// <summary>
    /// AMF serialization context.
    /// </summary>
    sealed public class AmfSerializationContext : IDisposable
    {
        #region .ctor
        /// <summary>
        /// Constructor.
        /// </summary>
        public AmfSerializationContext()
        {
            //Create proxy types assembly
            var myDomain = AppDomain.CurrentDomain;
            var myAsmName = new AssemblyName(ProxyAssembleName + GetHashCode());
            var myAssembly = myDomain.DefineDynamicAssembly(myAsmName, AssemblyBuilderAccess.RunAndCollect);
            _proxyContainer = myAssembly.DefineDynamicModule(myAsmName.Name);

            _contractResolver = new AmfDataContractResolver(_proxyContainer);
            _contractDereferencer = new AmfDataContractDereferencer();
        }
        #endregion

        #region Constants
        /// <summary>
        /// AMF namespace.
        /// </summary>
        public const string AmfNamespace = "http://dotamf.net/";

        /// <summary>
        /// Proxy types assemble name.
        /// </summary>
        private const string ProxyAssembleName = "AmfProxyContainer";
        #endregion

        #region Data
        /// <summary>
        /// Proxy types container.
        /// </summary>
        private readonly ModuleBuilder _proxyContainer;

        /// <summary>
        /// AMF data contract resolver.
        /// </summary>
        private readonly AmfDataContractResolver _contractResolver;

        /// <summary>
        /// AMF data contract dereferencer.
        /// </summary>
        private readonly AmfDataContractDereferencer _contractDereferencer;
        #endregion

        #region Properties
        /// <summary>
        /// Data contract resolver.
        /// </summary>
        public DataContractResolver ContractResolver { get { return _contractResolver; } }

        /// <summary>
        /// Data contract dereferencer.
        /// </summary>
        public DataContractResolver ContractDereferencer { get { return _contractDereferencer; } }
        #endregion

        #region Public methods
        /// <summary>
        /// Check if type is registered as a contract.
        /// </summary>
        /// <param name="type">Type to check.</param>
        public bool ContractRegistered(Type type)
        {
            return _contractResolver.ContractRegistered(type);
        }

        /// <summary>
        /// Register data contract.
        /// </summary>
        /// <param name="type">Type to register.</param>
        /// <exception cref="ArgumentException">Invalid alias name.</exception>
        /// <exception cref="InvalidOperationException">Type already registered.</exception>
        public void AddContract(Type type)
        {
            _contractResolver.AddContract(type);
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
            _contractResolver.AddContract(type, alias);
        }

        /// <summary>
        /// Register all available data contracts from a given assembly.
        /// </summary>
        /// <param name="assembly">Assembly to look for data contracts.</param>
        public void AddContract(Assembly assembly)
        {
            _contractResolver.AddContract(assembly);
        }
        #endregion

        #region Proxies and contracts
        /// <summary>
        /// Create a proxy for an AMF object.
        /// </summary>
        /// <param name="source">AMF object to proxy.</param>
        /// <returns>Object to use as a proxy.</returns>
        internal object CreateProxyObject(AmfObject source)
        {
            return _contractResolver.CreateProxyObject(source);
        }

        /// <summary>
        /// Create a dereferenced AMF object.
        /// </summary>
        /// <param name="source">Data contract object to dereference.</param>
        /// <returns>AMF object.</returns>
        internal object CreateDereferencedObject(object source)
        {
            return _contractDereferencer.CreateDereferencedObject(source);
        }

        /// <summary>
        /// Get data contract alias.
        /// </summary>
        /// <param name="type">Data contract type.</param>
        /// <returns>Data contract alias or <c>null</c> if contract is not registered.</returns>
        internal string GetContractAlias(Type type)
        {
            return _contractResolver.GetContractAlias(type);
        }
        #endregion

        #region IDisposable implementation
        public void Dispose()
        {
        }
        #endregion
    }
}

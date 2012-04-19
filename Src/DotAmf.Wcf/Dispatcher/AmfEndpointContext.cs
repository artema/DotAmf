using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel.Description;
using DotAmf.Data;
using DotAmf.Serialization;
using DotAmf.ServiceModel.Messaging;

namespace DotAmf.ServiceModel.Dispatcher
{
    /// <summary>
    /// AMF endpoint context.
    /// </summary>
    sealed internal class AmfEndpointContext : IDisposable
    {
        #region .ctor
        public AmfEndpointContext(ServiceEndpoint endpoint)
        {
            if (endpoint == null) throw new ArgumentNullException("endpoint");

            _contracts = new List<Type>();
            ResolveContracts(endpoint);

            ServiceEndpoint = endpoint;
            AmfSerializer = new DataContractAmfSerializer(typeof(AmfPacket), _contracts);
        }
        #endregion

        #region Properties
        /// <summary>
        /// Registered contracts.
        /// </summary>
        private readonly List<Type> _contracts;

        /// <summary>
        /// Related service endpoint.
        /// </summary>
        public ServiceEndpoint ServiceEndpoint { get; private set; }

        /// <summary>
        /// AMF serializer.
        /// </summary>
        public DataContractAmfSerializer AmfSerializer { get; private set; }
        #endregion

        #region Data contracts
        /// <summary>
        /// Resolve endpoint contracts.
        /// </summary>
        private void ResolveContracts(ServiceEndpoint endpoint)
        {
            //Add default contracts
            AddContract(typeof(AbstractMessage));
            AddContract(typeof(AcknowledgeMessage));
            AddContract(typeof(CommandMessage));
            AddContract(typeof(ErrorMessage));
            AddContract(typeof(RemotingMessage));

            //Add endpoint contract's types
            var types = new List<Type>();

            //Get return types and methods parameters
            foreach (var method in endpoint.Contract.Operations.Select(operation => operation.SyncMethod))
            {
                types.Add(method.ReturnType);
                types.AddRange(method.GetParameters().Select(param => param.ParameterType));
            }

            //Get operation fault contracts
            var faultTypes = from operation in endpoint.Contract.Operations
                             from fault in operation.Faults
                             select fault.DetailType;

            types.AddRange(faultTypes);

            //Handle complex types
            for (var i = 0; i < types.Count; i++)
            {
                var type = types[i];

                //Type is an array
                if (type.IsArray && type.HasElementType)
                    types[i] = type.GetElementType();
            }

            //Remove duplicates and invalid types
            var validtypes = types
                .Distinct()
                .Where(IsValidDataContract)
                .Where(type => !IsContractRegistered(type));

            //Register all valid types
            foreach (var type in validtypes)
                AddContract(type);
        }

        /// <summary>
        /// Check if type is a valid data contract.
        /// </summary>
        private static bool IsValidDataContract(Type type)
        {
            return DataContractHelper.IsDataContract(type);
        }

        /// <summary>
        /// Check if type is registered as a data contract.
        /// </summary>
        private bool IsContractRegistered(Type type)
        {
            if (type == null) throw new ArgumentNullException("type");

            return _contracts.Contains(type);
        }

        /// <summary>
        /// Register data contract.
        /// </summary>
        /// <param name="type">Type to register.</param>
        /// <exception cref="InvalidDataContractException">Invalid data contract.</exception>
        /// <exception cref="InvalidOperationException">Type already registered.</exception>
        private void AddContract(Type type)
        {
            if (type == null) throw new ArgumentNullException("type");

            if (IsContractRegistered(type))
                throw new InvalidOperationException("Type already registered.");

            if (!IsValidDataContract(type))
                throw new InvalidDataContractException(string.Format("Type '{0}' is not a valid data contract.", type.FullName));

            _contracts.Add(type);

            if (type.IsClass)
            {
                var memberTypes = ProcessClass(type);

                foreach (var subtype in memberTypes)
                {
                    if (IsContractRegistered(subtype) || !IsValidDataContract(subtype)) continue;
                    AddContract(subtype);
                }
            }
        }

        private IEnumerable<Type> ProcessClass(Type type)
        {
            if (type == null) throw new ArgumentNullException("type");
            if (!type.IsClass) throw new ArgumentException();

            var result = new List<Type>();

            var properties = from property in DataContractHelper.GetContractProperties(type)
                             select property.Value.PropertyType;

            result.AddRange(properties);

            var fields = from field in DataContractHelper.GetContractFields(type)
                         select field.Value.FieldType;

            result.AddRange(fields);

            return result;
        }
        #endregion

        #region IDisposable implementation
        public void Dispose()
        {
        }
        #endregion
    }
}

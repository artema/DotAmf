using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Description;
using DotAmf.Data;
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
            ServiceEndpoint = endpoint;
            AmfSerializationContext = new AmfSerializationContext();

            ResolveContracts(ServiceEndpoint, AmfSerializationContext);
        }
        #endregion

        #region Properties
        /// <summary>
        /// Serive endpoint.
        /// </summary>
        public ServiceEndpoint ServiceEndpoint { get; private set; }

        /// <summary>
        /// AMF serialization context.
        /// </summary>
        public AmfSerializationContext AmfSerializationContext { get; private set; }
        #endregion

        #region Private methods
        /// <summary>
        /// Resolve endpoint contracts.
        /// </summary>
        static private void ResolveContracts(ServiceEndpoint endpoint, AmfSerializationContext context)
        {
            //Add default contracts
            context.AddContract(typeof(AbstractMessage).Assembly);

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
                if(type.IsArray && type.HasElementType)
                    types[i] = type.GetElementType();
            }

            //Remove duplicates
            types = types.Distinct().ToList();

            //Try to register all types
            foreach (var type in types)
            {
                if (context.ContractRegistered(type)) continue;

                try
                {
                    context.AddContract(type);
                }
                catch
                {
                    //Unable to add a type
                    continue;
                }
            }
        }
        #endregion

        #region IDisposable implementation
        public void Dispose()
        {
            AmfSerializationContext.Dispose();
        }
        #endregion
    }
}

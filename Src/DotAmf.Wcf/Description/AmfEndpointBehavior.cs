using System.Linq;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using DotAmf.ServiceModel.Configuration;
using DotAmf.ServiceModel.Dispatcher;

namespace DotAmf.ServiceModel.Description
{
    /// <summary>
    /// Enables the AMF support for an endpoint.
    /// </summary>
    sealed public class AmfEndpointBehavior : IEndpointBehavior
    {
        #region Overriden methods
        /// <summary>
        /// Implements the <c>IEndpointBehavior.ApplyDispatchBehavior</c> method to support modification or extension of the client across an endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint that exposes the contract.</param>
        /// <param name="endpointDispatcher">The endpoint dispatcher to which the behavior is applied.</param>
        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
        {
            //Create endpoint capabilities descriptor
            var capabilities = new AmfEndpointCapabilities
            {
                MessagingVersion = 1,
                ExceptionDetailInFaults = endpointDispatcher.ChannelDispatcher.IncludeExceptionDetailInFaults
            };

            //Create endpoint context
            var endpointContext = new AmfEndpointContext(endpoint);
            
            //Create AMF message filter
            endpointDispatcher.ContractFilter = new AmfMessageFilter();

            //Create operation selector that will resolve messages' data contracts and route service methods for AMF requests.
            endpointDispatcher.DispatchRuntime.OperationSelector = new AmfDispatchOperationSelector(endpointContext);

            //Create message inspector that will dereference reply messages' data contracts
            endpointDispatcher.DispatchRuntime.MessageInspectors.Add(new AmfMessageInspector(endpointContext));

            //Create error handler
            endpointDispatcher.ChannelDispatcher.ErrorHandlers.Add(new AmfErrorHandler(capabilities));

            //Apply regular AMF operation behavior
            foreach (var descriptor in endpoint.Contract.Operations)
            {
                if (descriptor.Behaviors.OfType<AmfOperationBehavior>().FirstOrDefault() != null) continue;

                descriptor.Behaviors.Add(new AmfOperationBehavior());
            }

            //Command operation
            var commandOperation = new DispatchOperation(endpointDispatcher.DispatchRuntime,
                                                         AmfOperationKind.Command,
                                                         AmfOperationKind.Command,
                                                         null)
            {
                Invoker = new AmfCommandInvoker(capabilities),
                Formatter = new AmfCommandFormatter(),
            };
            endpointDispatcher.DispatchRuntime.Operations.Add(commandOperation);

            //Fault operation
            var faultOperation = new DispatchOperation(endpointDispatcher.DispatchRuntime,
                                                         AmfOperationKind.Fault,
                                                         AmfOperationKind.Fault,
                                                         null)
            {
                Invoker = new AmfFaultInvoker(capabilities),
                Formatter = new AmfGenericOperationFormatter()
            };
            endpointDispatcher.DispatchRuntime.Operations.Add(faultOperation);
        }

        public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
        {
        }

        public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
        }

        public void Validate(ServiceEndpoint endpoint)
        {
        }
        #endregion
    }
}

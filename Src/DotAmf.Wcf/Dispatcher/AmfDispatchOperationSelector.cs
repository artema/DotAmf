using System.Linq;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using DotAmf.Data;
using DotAmf.Serialization;
using DotAmf.ServiceModel.Channels;
using DotAmf.ServiceModel.Messaging;

namespace DotAmf.ServiceModel.Dispatcher
{
    /// <summary>
    /// The operation selector that supports the AMF operation invokation.
    /// </summary>
    sealed internal class AmfDispatchOperationSelector : IDispatchOperationSelector
    {
        #region .ctor
        public AmfDispatchOperationSelector(AmfEndpointContext context)
        {
            _context = context;
        }
        #endregion

        #region Data
        /// <summary>
        /// Endpoint context.
        /// </summary>
        private readonly AmfEndpointContext _context;
        #endregion

        #region IDispatchOperationSelector implementation
        /// <summary>
        /// Selects the service operation to call.
        /// </summary>
        /// <param name="message">The <c>Message</c> object sent to invoke a service operation.</param>
        /// <returns>The name of the service operation to call.</returns>
        public string SelectOperation(ref Message message)
        {
            //ToDo: this should be moved elsewhere
            if (message is AmfBatchMessage)
            {
                return AmfOperationKind.Batch;
            }

            var amfMessage = (AmfGenericMessage)message;

            //Resolve message contracts
            amfMessage = ResolveContracts(_context.AmfSerializationContext, amfMessage);

            //Check if it is a Flex message
            var arraybody = amfMessage.AmfMessage.Data as object[];

            //A Flex message's body is always an array
            if (arraybody != null)
            {
                //Proccess only the first message (could it be more in some cases?)
                var flexmessage = arraybody.OfType<AbstractMessage>().FirstOrDefault();

                //It is a Flex message after all
                if (flexmessage != null)
                {
                    amfMessage.AmfMessage.Data = flexmessage;
                    message = amfMessage;

                    var type = flexmessage.GetType();

                    //An RPC operation
                    if (type == typeof(RemotingMessage))
                    {
                        var operation = ((RemotingMessage)flexmessage).Operation;
                        return EndpointHasOperation(_context.ServiceEndpoint, operation) 
                            ? operation 
                            : null;
                    }

                    //A Flex command message
                    if (type == typeof(CommandMessage))
                    {
                        return AmfOperationKind.Command;
                    }
                }
            }

            //If it's not a Flex message, then do it the old way
            return EndpointHasOperation(_context.ServiceEndpoint, amfMessage.AmfMessage.Target) 
                ? amfMessage.AmfMessage.Target
                : null;
        }
        #endregion

        #region Private methods
        /// <summary>
        /// Resolve message's data contracts.
        /// </summary>
        /// <param name="context">AMF serialization context.</param>
        /// <param name="amfMessage">AMF message.</param>
        /// <returns></returns>
        static private AmfGenericMessage ResolveContracts(AmfSerializationContext context, AmfGenericMessage amfMessage)
        {
            var serializer = new DataContractAmfSerializer(context);
            amfMessage = (AmfGenericMessage)amfMessage.Clone();

            amfMessage.AmfMessage.Data = serializer.ResolveContracts(amfMessage.AmfMessage.Data);

            foreach (var key in amfMessage.AmfHeaders.Keys)
            {
                var header = amfMessage.AmfHeaders[key];
                header.Data = serializer.ResolveContracts(header.Data);
            }

            return amfMessage;
        }

        /// <summary>
        /// Check if an endpoint has the operation.
        /// </summary>
        /// <param name="endpoint">Endpoint.</param>
        /// <param name="operationName">Operation's name.</param>
        static private bool EndpointHasOperation(ServiceEndpoint endpoint, string operationName)
        {
            return endpoint.Contract.Operations.Where(op => op.Name == operationName).FirstOrDefault() != null;
        }
        #endregion
    }
}

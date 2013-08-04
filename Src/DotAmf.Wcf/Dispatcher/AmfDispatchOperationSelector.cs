// Copyright (c) 2012 Artem Abashev (http://abashev.me)
// All rights reserved.
// Licensed under the Microsoft Public License (Ms-PL)
// http://opensource.org/licenses/ms-pl.html

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
            AmfPacket packet;

            //Read AMF packet from the message
            try
            {
                packet = message.GetBody<AmfPacket>(_context.AmfSerializer);
            }
            finally
            {
                message.Close();
            }

            //Batch request
            if (packet.Messages.Count > 1)
            {
                message = new AmfBatchMessage(packet.Headers, packet.Messages);
                return AmfOperationKind.Batch;
            }

            //Regular request
            var amfMessage = new AmfGenericMessage(packet.Headers, packet.Messages[0]);
            message = amfMessage;

            //Check if it is a Flex message.
            //Due to the nature of NetConnection.call(), RPC arguments 
            //are sent in an array. But in case of AMFX, an array is not used
            //if there is only one argument.
            var arraybody = amfMessage.AmfMessage.Data as object[];
            AbstractMessage flexmessage;

            if (arraybody != null && arraybody.Length > 0)
                flexmessage = arraybody[0] as AbstractMessage;
            else
                flexmessage = amfMessage.AmfMessage.Data as AbstractMessage;

            //It is a Flex message after all
            if (flexmessage != null)
            {
                amfMessage.AmfMessage.Data = flexmessage;

                var type = flexmessage.GetType();

                //An RPC operation
                if (type == typeof(RemotingMessage))
                {
                    var operation = ((RemotingMessage)flexmessage).Operation;
                    return EndpointHasOperation(_context.ServiceEndpoint, operation) 
                        ? operation
                        : AmfOperationKind.Fault;
                }

                //A Flex command message
                if (type == typeof(CommandMessage))
                {
                    return AmfOperationKind.Command;
                }
            }

            //If it's not a Flex message, then do it the old way
            return EndpointHasOperation(_context.ServiceEndpoint, amfMessage.AmfMessage.Target) 
                ? amfMessage.AmfMessage.Target
                : AmfOperationKind.Fault;
        }
        #endregion

        #region Helper methods
        /// <summary>
        /// Check if the endpoint has the operation.
        /// </summary>
        /// <param name="endpoint">Endpoint to check.</param>
        /// <param name="operationName">Operation's name.</param>
        static private bool EndpointHasOperation(ServiceEndpoint endpoint, string operationName)
        {
            return endpoint.Contract.Operations.Where(op => op.Name == operationName).FirstOrDefault() != null;
        }
        #endregion
    }
}

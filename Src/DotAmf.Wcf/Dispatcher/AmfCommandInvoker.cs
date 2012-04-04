using System;
using System.Collections.Generic;
using DotAmf.ServiceModel.Channels;
using DotAmf.ServiceModel.Configuration;
using DotAmf.ServiceModel.Messaging;

namespace DotAmf.ServiceModel.Dispatcher
{
    /// <summary>
    /// AMF command invoker.
    /// </summary>
    /// <see cref="AmfCommandFormatter"/>
    internal class AmfCommandInvoker : AmfOperationInvoker
    {
        #region .ctor
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="capabilities">Endpoint capabilities.</param>
        public AmfCommandInvoker(AmfEndpointCapabilities capabilities)
        {
            _capabilities = capabilities;
        }
        #endregion

        #region Data
        /// <summary>
        /// Endpoint capabilities.
        /// </summary>
        private readonly AmfEndpointCapabilities _capabilities;
        #endregion

        #region IOperationInvoker implementation
        override public object[] AllocateInputs() { return new object[1]; } //Allocate memory for an AmfGenericMessage

        override public object Invoke(object instance, object[] inputs, out object[] outputs)
        {
            outputs = new object[0];

            var request = inputs[0] as AmfGenericMessage;
            if (request == null) throw new ArgumentException(Errors.AmfGenericOperationInvoker_Invoke_MessageNotFound, "inputs");

            return ProcessCommand(request);
        }
        #endregion

        #region Protected methods
        /// <summary>
        /// Process an AMF command request.
        /// </summary>
        /// <param name="request">Request message to process.</param>
        /// <returns>Service reply message.</returns>
        protected AmfGenericMessage ProcessCommand(AmfGenericMessage request)
        {
            var command = (CommandMessage)request.AmfMessage.Data;
            Func<AmfGenericMessage, CommandMessage, AmfGenericMessage> handler;

            switch (command.Operation)
            {
                case CommandMessageOperation.ClientPing:
                    handler = HandleClientPing;
                    break;

                default:
                    throw new NotSupportedException(string.Format(Errors.AmfCommandInvoker_ProcessCommand_OperationNotSupported, command.Operation));
            }

            return handler.Invoke(request, command);
        }
        #endregion

        #region Operations
        /// <summary>
        /// Handle command message: a clinet ping.
        /// </summary>
        private AmfGenericMessage HandleClientPing(AmfGenericMessage request, CommandMessage message)
        {
            var acknowledge = AmfOperationUtil.BuildAcknowledgeMessage(message);
            acknowledge.Headers = new Dictionary<string, object>
                                      {
                                          {CommandMessageHeader.MessagingVersion, _capabilities.MessagingVersion}
                                      };

            return AmfOperationUtil.BuildMessageReply(request, acknowledge);
        }
        #endregion
    }
}

using System;
using DotAmf.ServiceModel.Channels;
using DotAmf.ServiceModel.Messaging;

namespace DotAmf.ServiceModel.Dispatcher
{
    /// <summary>
    /// AMF command invoker.
    /// </summary>
    /// <see cref="AmfCommandFormatter"/>
    internal class AmfCommandInvoker : AmfOperationInvoker
    {
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
            if (request == null) throw new ArgumentNullException("request");

            var command = request.AmfMessage.Data as CommandMessage;
            if (command == null) throw new ArgumentException("Command not found.", "request");

            Func<AmfGenericMessage, CommandMessage, AmfGenericMessage> handler;

            switch(command.Operation)
            {
                case CommandMessageOperation.ClientPing:
                    handler = HandleClientPing;
                    break;

                default:
                    throw new NotSupportedException(string.Format("Operation '{0}' is not supported", command.Operation));
            }

            var reply = handler.Invoke(request, command);
            return reply;
        }
        #endregion

        #region Operations
        /// <summary>
        /// Handle command message: a clinet ping.
        /// </summary>
        private static AmfGenericMessage HandleClientPing(AmfGenericMessage request, CommandMessage message)
        {
            var acknowledge = AmfOperationUtil.BuildAcknowledgeMessage(message);
            return AmfOperationUtil.BuildMessageReply(request, acknowledge);
        }
        #endregion
    }
}

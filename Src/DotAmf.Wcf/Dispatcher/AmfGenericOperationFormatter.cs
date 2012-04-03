using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;
using DotAmf.Data;
using DotAmf.ServiceModel.Channels;
using DotAmf.ServiceModel.Messaging;

namespace DotAmf.ServiceModel.Dispatcher
{
    /// <summary>
    /// Generic AMF operation formatter.
    /// </summary>
    /// <see cref="AmfGenericOperationInvoker"/>
    internal class AmfGenericOperationFormatter : IDispatchMessageFormatter
    {
        #region IDispatchMessageFormatter Members
        /// <summary>
        /// Deserializes a message into an array of parameters.
        /// </summary>
        /// <param name="message">The incoming message.</param>
        /// <param name="parameters">The objects that are passed to the operation as parameters.</param>
        public virtual void DeserializeRequest(Message message, object[] parameters)
        {
            var request = (AmfGenericMessage)message;

            OperationContext.Current.IncomingMessageProperties[MessagingHeaders.InvokerMessageBody] = request.AmfMessage;
            OperationContext.Current.IncomingMessageProperties[MessagingHeaders.InvokerMessageHeaders] = request.AmfHeaders;

            var rpcMessage = request.AmfMessage.Data as RemotingMessage;

            if (rpcMessage != null)
                OperationContext.Current.IncomingMessageProperties[MessagingHeaders.RemotingMessage] = rpcMessage;

            //Fill parameters, allocated by AmfGenericOperationInvoker
            if (rpcMessage != null)
                parameters[0] = rpcMessage.Body as object[];
            else
                parameters[0] = request.AmfMessage.Data as object[] ?? new object[0];
        }

        /// <summary>
        /// Serializes a reply message from a specified message version, array of parameters, and a return value.
        /// </summary>
        /// <param name="messageVersion">The SOAP message version.</param>
        /// <param name="parameters">The out parameters.</param>
        /// <param name="result">The return value.</param>
        /// <returns>The serialized reply message.</returns>
        public virtual Message SerializeReply(MessageVersion messageVersion, object[] parameters, object result)
        {
            var requestMessage = (AmfMessage)OperationContext.Current.IncomingMessageProperties[MessagingHeaders.InvokerMessageBody];

            if (OperationContext.Current.IncomingMessageProperties.ContainsKey(MessagingHeaders.RemotingMessage))
            {
                var rpcMessage = (RemotingMessage)OperationContext.Current.IncomingMessageProperties[MessagingHeaders.RemotingMessage];
                var acknowledge = AmfOperationUtil.BuildAcknowledgeMessage(rpcMessage);
                acknowledge.Body = result;

                result = acknowledge;
            }

            var replyHeaders = new Dictionary<string, AmfHeader>();
            var replyMessage = new AmfMessage
                                   {
                                       Data = result,
                                       Target = AmfOperationUtil.CreateReplyTarget(requestMessage),
                                       Response = string.Empty
                                   };

            return new AmfGenericMessage(replyHeaders, replyMessage);
        }
        #endregion
    }
}

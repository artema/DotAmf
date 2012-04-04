using System;
using System.Collections.Generic;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;
using DotAmf.Data;
using DotAmf.ServiceModel.Channels;
using DotAmf.ServiceModel.Messaging;
using AmfMessageHeader = DotAmf.ServiceModel.Messaging.MessageHeader;

namespace DotAmf.ServiceModel.Dispatcher
{
    /// <summary>
    /// AMF error handler.
    /// </summary>
    sealed internal class AmfErrorHandler : IErrorHandler
    {
        #region IErrorHandler implementation
        public bool HandleError(Exception error)
        {
            return true;
        }

        public void ProvideFault(Exception error, MessageVersion version, ref Message fault)
        {
            //An AMF operation
            if (OperationContext.Current.IncomingMessageProperties.ContainsKey(MessagingHeaders.InvokerMessageBody))
            {
                var replyHeaders = new Dictionary<string, AmfHeader>();
                var replyMessage = new AmfMessage { Response = string.Empty };

                var requestMessage = (AmfMessage)OperationContext.Current.IncomingMessageProperties[MessagingHeaders.InvokerMessageBody];

                //An RPC operation
                if (OperationContext.Current.IncomingMessageProperties.ContainsKey(MessagingHeaders.RemotingMessage))
                {
                    var rpcMessage = (RemotingMessage)OperationContext.Current.IncomingMessageProperties[MessagingHeaders.RemotingMessage];
                    var acknowledge = AmfOperationUtil.BuildErrorMessage(rpcMessage);
                    if (acknowledge.Headers == null) acknowledge.Headers = new Dictionary<string, object>();

                    acknowledge.Headers[AmfMessageHeader.StatusCode] = (int)HttpStatusCode.BadRequest;

                    acknowledge.FaultCode = ErrorMessageFaultCode.DeliveryInDoubt;
                    acknowledge.FaultString = error.Message;
                    acknowledge.FaultDetail = error.StackTrace;

                    replyMessage.Target = AmfOperationUtil.CreateStatusReplyTarget(requestMessage);
                    replyMessage.Data = acknowledge;
                }

                fault = new AmfGenericMessage(replyHeaders, replyMessage);
            }
        }
        #endregion
    }
}

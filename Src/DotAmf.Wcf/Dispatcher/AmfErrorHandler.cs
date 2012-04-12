using System;
using System.Collections.Generic;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;
using DotAmf.Data;
using DotAmf.ServiceModel.Channels;
using DotAmf.ServiceModel.Configuration;
using DotAmf.ServiceModel.Faults;
using DotAmf.ServiceModel.Messaging;
using AmfMessageHeader = DotAmf.ServiceModel.Messaging.MessageHeader;

namespace DotAmf.ServiceModel.Dispatcher
{
    /// <summary>
    /// AMF error handler.
    /// </summary>
    sealed internal class AmfErrorHandler : IErrorHandler
    {
        #region .ctor
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="capabilities">Endpoint capabilities.</param>
        public AmfErrorHandler(AmfEndpointCapabilities capabilities)
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

        #region IErrorHandler implementation
        public bool HandleError(Exception error)
        {
            return true;
        }

        public void ProvideFault(Exception error, MessageVersion version, ref Message fault)
        {
            //An internal server error occured
            if (OperationContext.Current == null) return;

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

                    if (error is AmfOperationNotFoundException)
                        acknowledge.Headers[AmfMessageHeader.StatusCode] = (int)HttpStatusCode.NotFound;
                    else
                        acknowledge.Headers[AmfMessageHeader.StatusCode] = (int)HttpStatusCode.BadRequest;

                    acknowledge.FaultCode = ErrorMessageFaultCode.DeliveryInDoubt;

                    acknowledge.FaultString = error.Message;

                    if (_capabilities.ExceptionDetailInFaults)
                    {
                        acknowledge.FaultDetail = error.StackTrace;
                    }

                    //Get FaultException's detail object, if any
                    if(error is FaultException)
                    {
                        var type = error.GetType();

                        if (type.IsGenericType && type.GetGenericTypeDefinition().Equals(typeof(FaultException<>)))
                        {
                            acknowledge.ExtendedData = type.GetProperty("Detail").GetValue(error, null);
                        }
                    }

                    replyMessage.Target = AmfOperationUtil.CreateStatusReplyTarget(requestMessage);
                    replyMessage.Data = acknowledge;
                }

                fault = new AmfGenericMessage(replyHeaders, replyMessage);
            }
        }
        #endregion
    }
}

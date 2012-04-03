using System.Collections.Generic;
using System.IO;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;
using System.Xml;
using DotAmf.Data;
using DotAmf.Serialization;
using DotAmf.ServiceModel.Channels;
using DotAmf.ServiceModel.Messaging;

namespace DotAmf.ServiceModel.Dispatcher
{
    /// <summary>
    /// AMF message inspector.
    /// </summary>
    sealed internal class AmfMessageInspector : IDispatchMessageInspector
    {
        #region .ctor
        public AmfMessageInspector(AmfEndpointContext context)
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

        #region IDispatchMessageInspector implementation
        public object AfterReceiveRequest(ref Message request, IClientChannel channel, InstanceContext instanceContext)
        {
            //var message = (AmfGenericMessage)request;

            //OperationContext.Current.IncomingMessageProperties[MessagingHeaders.InvokerMessageBody] = message.AmfMessage;
            //OperationContext.Current.IncomingMessageProperties[MessagingHeaders.InvokerMessageHeaders] = message.AmfHeaders;

            //var rpcMessage = message.AmfMessage.Data as RemotingMessage;

            //if (rpcMessage != null)
            //{
            //    OperationContext.Current.IncomingMessageProperties[MessagingHeaders.RemotingMessage] = rpcMessage;

            //    request = Message.CreateMessage(MessageVersion.None, "lolo", ((object[])rpcMessage.Body)[0]);

            //    //FileStream stream = new FileStream(@"C:\Users\Artem\Desktop\1.xml", FileMode.Truncate);
            //    //XmlDictionaryWriter xdw = XmlDictionaryWriter.CreateTextWriter(stream);
            //    //request.WriteBodyContents(xdw);
            //    //xdw.Flush();
            //}

            return null;
        }

        public void BeforeSendReply(ref Message reply, object correlationState)
        {
            //var message = reply as AmfGenericMessage;

            //if (message == null)
            //{
            //    var result = reply.GetBody<object>();
            //    var requestMessage =
            //        (AmfMessage) OperationContext.Current.IncomingMessageProperties[MessagingHeaders.InvokerMessageBody];

            //    if (OperationContext.Current.IncomingMessageProperties.ContainsKey(MessagingHeaders.RemotingMessage))
            //    {
            //        var rpcMessage =
            //            (RemotingMessage)
            //            OperationContext.Current.IncomingMessageProperties[MessagingHeaders.RemotingMessage];
            //        var acknowledge = AmfOperationUtil.BuildAcknowledgeMessage(rpcMessage);
            //        acknowledge.Body = result;

            //        result = acknowledge;
            //    }

            //    var replyHeaders = new Dictionary<string, AmfHeader>();
            //    var replyMessage = new AmfMessage
            //                           {
            //                               Data = result,
            //                               Target = AmfOperationUtil.CreateReplyTarget(requestMessage),
            //                               Response = string.Empty
            //                           };

            //    reply = new AmfGenericMessage(replyHeaders, replyMessage);
            //}

            DereferenceContracts(_context.AmfSerializationContext, ref reply);
        }
        #endregion

        #region Private methods
        /// <summary>
        /// Dereference message's data contracts.
        /// </summary>
        /// <param name="context">AMF serialization context.</param>
        /// <param name="message">AMF message.</param>
        /// <returns></returns>
        static private void DereferenceContracts(AmfSerializationContext context, ref Message message)
        {
            var amfMessage = (AmfGenericMessage) message;

            var serializer = new DataContractAmfSerializer(context);
            amfMessage.AmfMessage.Data = serializer.DereferenceContracts(amfMessage.AmfMessage.Data);

            foreach (var key in amfMessage.AmfHeaders.Keys)
            {
                var header = amfMessage.AmfHeaders[key];
                header.Data = serializer.DereferenceContracts(header.Data);
            }

            message = amfMessage;
        }
        #endregion
    }
}

using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;
using DotAmf.Data;
using DotAmf.Serialization;
using DotAmf.ServiceModel.Channels;

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
            return null;
        }

        public void BeforeSendReply(ref Message reply, object correlationState)
        {
            if(reply is AmfGenericMessage)
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

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
            //if(reply is AmfGenericMessage)
            //    DereferenceContracts(_context.SerializationContext, ref reply);
        }
        #endregion
    }
}

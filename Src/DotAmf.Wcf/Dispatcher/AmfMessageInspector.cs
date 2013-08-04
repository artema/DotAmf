// Copyright (c) 2012 Artem Abashev (http://abashev.me)
// All rights reserved.
// Licensed under the Microsoft Public License (Ms-PL)
// http://opensource.org/licenses/ms-pl.html

using System;
using System.IO;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;
using DotAmf.Data;
using DotAmf.IO;
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
            var message = reply as AmfGenericMessage;

            if(message == null)
                throw new InvalidOperationException("AmfGenericMessage is expected.");

            var packet = new AmfPacket();

            foreach (var header in message.AmfHeaders)
                packet.Headers[header.Key] = header.Value;

            packet.Messages.Add(message.AmfMessage);

            var ms = new MemoryStream();

            try
            {
                //Serialize packet into AMFX data
                var output = AmfxWriter.Create(ms);
                _context.AmfSerializer.WriteObject(output, packet);
                output.Flush();
                ms.Seek(0, SeekOrigin.Begin);

                reply = Message.CreateMessage(MessageVersion.None, null, AmfxReader.Create(ms, true));
            }
            catch
            {
                ms.Dispose();
                throw;
            }
        }
        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Channels;
using DotAmf.ServiceModel.Channels;

namespace DotAmf.ServiceModel.Dispatcher
{
    /// <summary>
    /// Batch AMF operation formatter.
    /// </summary>
    /// <see cref="AmfBatchOperationInvoker"/>
    sealed internal class AmfBatchOperationFormatter : AmfGenericOperationFormatter
    {
        #region IDispatchMessageFormatter Members
        public override void DeserializeRequest(Message message, object[] parameters)
        {
            var msg = message as AmfBatchMessage;
            if (msg == null) throw new ArgumentException("AmfBatchMessage is expected", "message");

            //parameters[0] = msg.ToList().Select(PrepareRequest).ToList(); //Use space allocated for one IEnumerable<AmfGenericMessage>
        }

        public override Message SerializeReply(MessageVersion messageVersion, object[] parameters, object result)
        {
            var msgs = result as IEnumerable<AmfGenericMessage>;
            if (msgs == null) throw new ArgumentException("IEnumerable<AmfGenericMessage> is expected", "result");

            //var replies = msgs.Select(PrepareReply); //Prepare replies
            //var headers = replies.First().AmfHeaders; //Grab the first message and use its headers for the whole batch
            //var messages = replies.Select(raw => raw.AmfMessage).ToList();

            //return new AmfBatchMessage(headers, messages);
            return null;
        }
        #endregion
    }
}

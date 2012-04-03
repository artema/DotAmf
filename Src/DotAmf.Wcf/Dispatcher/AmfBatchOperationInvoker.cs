using System;
using System.Collections.Generic;
using DotAmf.ServiceModel.Channels;

namespace DotAmf.ServiceModel.Dispatcher
{
    /// <summary>
    /// Batch AMF operation invoker.
    /// </summary>
    /// <see cref="AmfBatchOperationFormatter"/>
    sealed internal class AmfBatchOperationInvoker : AmfOperationInvoker
    {
        #region IOperationInvoker implementation
        override public object[] AllocateInputs() { return new object[1]; } //Allocate memory for an IEnumerable<AmfGenericMessage>
        
        override public object Invoke(object instance, object[] inputs, out object[] outputs)
        {
            outputs = new object[0];

            var requests = inputs[0] as IEnumerable<AmfGenericMessage>;
            if (requests == null) throw new ArgumentException("IEnumerable<AmfGenericMessage> was expected at inputs[0]", "inputs");

            var replies = new List<AmfGenericMessage>();

            foreach (var request in requests)
            {
                //var reply = ProcessRequest(request);
                //replies.Add(reply);
            }

            return replies;
        }
        #endregion
    }
}

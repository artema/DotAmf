using System;
using System.ServiceModel;
using System.ServiceModel.Dispatcher;
using DotAmf.ServiceModel.Messaging;

namespace DotAmf.ServiceModel.Dispatcher
{
    /// <summary>
    /// Generic AMF operation invoker.
    /// </summary>
    /// <see cref="AmfGenericOperationFormatter"/>
    sealed internal class AmfGenericOperationInvoker : AmfOperationInvoker
    {
        #region .ctor
        public AmfGenericOperationInvoker(IOperationInvoker invoker)
        {
            if (invoker == null) throw new ArgumentNullException("invoker");
            _invoker = invoker;
        }
        #endregion

        #region Data
        /// <summary>
        /// Operation invoker.
        /// </summary>
        private readonly IOperationInvoker _invoker;
        #endregion

        #region IOperationInvoker implementation
        override public object[] AllocateInputs() { return _invoker.AllocateInputs(); } //An object[] is expected at [0]

        override public object Invoke(object instance, object[] inputs, out object[] outputs)
        {
            //An AMF operation
            if (OperationContext.Current.IncomingMessageProperties.ContainsKey(MessagingHeaders.InvokerMessageBody))
            {
                var parameters = inputs[0] as object[];

                if (parameters == null) throw new ArgumentException("Input parameters not found.", "inputs");

                return _invoker.Invoke(instance, parameters, out outputs);
            }

            //A regular operation
            return _invoker.Invoke(instance, inputs, out outputs);
        }
        #endregion
    }
}

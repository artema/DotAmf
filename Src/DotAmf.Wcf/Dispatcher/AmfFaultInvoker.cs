// Copyright (c) 2012 Artem Abashev (http://abashev.me)
// All rights reserved.
// Licensed under the Microsoft Public License (Ms-PL)
// http://opensource.org/licenses/ms-pl.html

using System.ServiceModel;
using DotAmf.ServiceModel.Configuration;
using DotAmf.ServiceModel.Faults;
using DotAmf.ServiceModel.Messaging;

namespace DotAmf.ServiceModel.Dispatcher
{
    /// <summary>
    /// AMF fault invoker.
    /// </summary>
    internal class AmfFaultInvoker : AmfOperationInvoker
    {
        #region .ctor
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="capabilities">Endpoint capabilities.</param>
        public AmfFaultInvoker(AmfEndpointCapabilities capabilities)
            : base(capabilities)
        {
        }
        #endregion

        #region IOperationInvoker implementation
        override public object[] AllocateInputs() { return new object[1]; }

        override public object Invoke(object instance, object[] inputs, out object[] outputs)
        {
            outputs = new object[0];

            var operationName = string.Empty;

            if(OperationContext.Current.IncomingMessageProperties.ContainsKey(MessagingHeaders.RemotingMessage))
            {
                var operation = (RemotingMessage)OperationContext.Current.IncomingMessageProperties[MessagingHeaders.RemotingMessage];
                operationName = operation.Operation;
            }

            throw new AmfOperationNotFoundException(string.Format(Errors.AmfFaultInvoker_Invoke_OperationNotFound, operationName), operationName);
        }
        #endregion
    }
}

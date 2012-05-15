// Copyright (c) 2012 Artem Abashev (http://abashev.me)
// All rights reserved.
// Licensed under the Microsoft Public License (Ms-PL)
// http://opensource.org/licenses/ms-pl.html

using System;
using System.ServiceModel.Dispatcher;
using DotAmf.ServiceModel.Configuration;

namespace DotAmf.ServiceModel.Dispatcher
{
    /// <summary>
    /// Abstract AMF operation invoker.
    /// </summary>
    abstract internal class AmfOperationInvoker : IOperationInvoker
    {
        #region .ctor
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="capabilities">Endpoint capabilities.</param>
        protected AmfOperationInvoker(AmfEndpointCapabilities capabilities)
        {
            Capabilities = capabilities;
        }
        #endregion

        #region Data
        /// <summary>
        /// Endpoint capabilities.
        /// </summary>
        protected readonly AmfEndpointCapabilities Capabilities;
        #endregion

        #region IOperationInvoker implementation
        /// <summary>
        /// Returns an <c>System.Array</c> of parameter objects.
        /// </summary>
        /// <returns>The parameters that are to be used as arguments to the operation.</returns>
        public abstract object[] AllocateInputs();

        /// <summary>
        /// Returns an object and a set of output objects from an instance and set of input objects.
        /// </summary>
        /// <param name="instance">The object to be invoked.</param>
        /// <param name="inputs">The inputs to the method.</param>
        /// <param name="outputs">The outputs from the method.</param>
        /// <returns>The return value.</returns>
        public abstract object Invoke(object instance, object[] inputs, out object[] outputs);

        /// <summary>
        /// <c>true</c> if the dispatcher invokes the synchronous operation; otherwise, <c>false</c>.
        /// </summary>
        /// <remarks>Only synchronous AMF operations are supported.</remarks>
        public bool IsSynchronous { get { return true; } }

        public IAsyncResult InvokeBegin(object instance, object[] inputs, AsyncCallback callback, object state){ throw new NotSupportedException(); }

        public object InvokeEnd(object instance, out object[] outputs, IAsyncResult result){ throw new NotSupportedException(); }
        #endregion
    }
}

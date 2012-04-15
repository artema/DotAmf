using System;
using System.Collections.Generic;
using DotAmf.Data;

namespace DotAmf.ServiceModel.Channels
{
    /// <summary>
    /// Generic AMF message.
    /// </summary>
    internal class AmfGenericMessage : AmfMessageBase
    {
        #region .ctor
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="headers">AMF headers.</param>
        /// <param name="message">AMF message.</param>
        public AmfGenericMessage(IDictionary<string, AmfHeader> headers, AmfMessage message)
            : base(headers)
        {
            if (message == null) throw new ArgumentNullException("message");
            _amfMessage = message;
        }
        #endregion

        #region Data
        /// <summary>
        /// AMF message.
        /// </summary>
        private readonly AmfMessage _amfMessage;
        #endregion

        #region Properties
        /// <summary>
        /// AMF message.
        /// </summary>
        public AmfMessage AmfMessage { get { return _amfMessage; } }
        #endregion
    }
}

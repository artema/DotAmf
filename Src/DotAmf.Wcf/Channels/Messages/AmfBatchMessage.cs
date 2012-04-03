using System;
using System.Collections.Generic;
using System.Linq;
using DotAmf.Data;

namespace DotAmf.ServiceModel.Channels
{
    /// <summary>
    /// Batch AMF message.
    /// </summary>
    sealed internal class AmfBatchMessage : AmfMessageBase
    {
        #region .ctor
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="headers">AMF headers.</param>
        /// <param name="messages">AMF messages.</param>
        public AmfBatchMessage(IDictionary<string, AmfHeader> headers, IEnumerable<AmfMessage> messages)
            : base(headers)
        {
            if (messages == null) throw new ArgumentNullException("messages");
            _amfMessages = messages;

            //AMF packet contains multimple messages, so we will need batching
            Properties.AllowOutputBatching = true;
        }
        #endregion

        #region Data
        /// <summary>
        /// AMF messages.
        /// </summary>
        private readonly IEnumerable<AmfMessage> _amfMessages;
        #endregion

        #region Properties
        /// <summary>
        /// AMF messages.
        /// </summary>
        public IEnumerable<AmfMessage> AmfMessages { get { return _amfMessages; } }
        #endregion

        #region Public methods
        /// <summary>
        /// Get a list of contained AMF messages.
        /// </summary>
        public IEnumerable<AmfGenericMessage> ToList()
        {
            return AmfMessages.Select(msg => new AmfGenericMessage(AmfHeaders, msg));
        }
        #endregion

        #region IClonable implementation
        override public object Clone()
        {
            return new AmfBatchMessage(AmfHeaders, AmfMessages);
        }
        #endregion
    }
}

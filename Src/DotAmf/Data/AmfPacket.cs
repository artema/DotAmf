using System.Collections.Generic;

namespace DotAmf.Data
{
    /// <summary>
    /// AMF packet.
    /// </summary>
    sealed public class AmfPacket
    {
        #region .ctor
        public AmfPacket()
        {
            Headers = new Dictionary<string, AmfHeader>();
            Messages = new List<AmfMessage>();
        }
        #endregion

        /// <summary>
        /// Packet headers.
        /// </summary>
        public IDictionary<string,AmfHeader> Headers { get; private set; }

        /// <summary>
        /// Packet messages.
        /// </summary>
        public IList<AmfMessage> Messages { get; private set; }
    }
}

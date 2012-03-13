using System.Collections.Generic;

namespace DotAmf.Data
{
    /// <summary>
    /// AMF packet.
    /// </summary>
    sealed public class AmfPacket
    {
        #region .ctor
        public AmfPacket(AmfVersion version)
        {
            Version = version;

            Headers = new List<AmfHeader>();
            Messages = new List<AmfMessage>();
        }
        #endregion

        /// <summary>
        /// Message version.
        /// </summary>
        public AmfVersion Version { get; private set; }

        /// <summary>
        /// Packet headers.
        /// </summary>
        public IList<AmfHeader> Headers { get; private set; }

        /// <summary>
        /// Packet messages.
        /// </summary>
        public IList<AmfMessage> Messages { get; private set; }
    }
}

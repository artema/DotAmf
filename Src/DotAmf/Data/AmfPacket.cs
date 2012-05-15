// Copyright (c) 2012 Artem Abashev (http://abashev.me)
// All rights reserved.
// Licensed under the Microsoft Public License (Ms-PL)
// http://opensource.org/licenses/ms-pl.html

using System.Collections.Generic;
using System.Runtime.Serialization;

namespace DotAmf.Data
{
    /// <summary>
    /// AMF packet.
    /// </summary>
    [DataContract(Name = "#AmfPacket", Namespace = "http://dotamf.net/")]
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

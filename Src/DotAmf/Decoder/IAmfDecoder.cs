using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using DotAmf.Data;

namespace DotAmf.Decoder
{
    /// <summary>
    /// AMF decoder.
    /// </summary>
    public interface IAmfDecoder
    {
        /// <summary>
        /// Read a value.
        /// </summary>
        /// <exception cref="NotSupportedException">AMF type is not supported.</exception>
        /// <exception cref="FormatException">Unknown data format.</exception>
        /// <exception cref="SerializationException">Error during deserialization.</exception>
        /// <exception cref="InvalidOperationException">Invalid AMF context.</exception>
        object ReadValue();

        /// <summary>
        /// Read AMF packet headers.
        /// </summary>
        /// <returns>Lazy headers reader.</returns>
        /// <exception cref="FormatException">Data has unknown format.</exception>
        IEnumerable<AmfHeader> ReadPacketHeaders();

        /// <summary>
        /// Read AMF packet messages.
        /// </summary>
        /// <returns>Lazy messages reader.</returns>
        /// <exception cref="FormatException">Data has unknown format.</exception>
        IEnumerable<AmfMessage> ReadPacketMessages();

        /// <summary>
        /// Clear stored references.
        /// </summary>
        void ClearReferences();

        /// <summary>
        /// AMF encoding context switch event.
        /// </summary>
        event EncodingContextSwitch ContextSwitch;
    }
}

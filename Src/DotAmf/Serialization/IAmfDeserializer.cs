using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using DotAmf.Data;

namespace DotAmf.Serialization
{
    /// <summary>
    /// AMF deserializer.
    /// </summary>
    public interface IAmfDeserializer
    {
        /// <summary>
        /// Read an AMF header.
        /// </summary>
        /// <remarks>
        /// Reader's current position must be set on header's 0 position.
        /// </remarks>
        AmfHeader ReadHeader();

        /// <summary>
        /// Read an AMF message.
        /// </summary>
        /// <remarks>
        /// Reader's current position must be set on message's 0 position.
        /// </remarks>
        AmfMessage ReadMessage();

        /// <summary>
        /// Read a value.
        /// </summary>
        /// <remarks>
        /// Reader's current position must be set on a value type marker.
        /// </remarks>
        /// <exception cref="NotSupportedException">AMF type is not supported.</exception>
        /// <exception cref="FormatException">Unknown data format.</exception>
        /// <exception cref="SerializationException">Error during deserialization.</exception>
        /// <exception cref="InvalidOperationException">Invalid AMF context.</exception>
        object ReadValue();

        /// <summary>
        /// Clear stored references.
        /// </summary>
        void ClearReferences();

        /// <summary>
        /// AMF context switch event.
        /// </summary>
        event ContextSwitch ContextSwitch;

        /// <summary>
        /// Current AMF context.
        /// </summary>
        AmfVersion Context { get; }
    }
}

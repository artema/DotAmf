using System;
using System.Runtime.Serialization;

namespace DotAmf.Serialization
{
    /// <summary>
    /// AMF deserializer.
    /// </summary>
    public interface IAmfDeserializer
    {
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
        /// AMF serialization context switch event.
        /// </summary>
        event ContextSwitch ContextSwitch;

        /// <summary>
        /// Current AMF serialization context.
        /// </summary>
        AmfSerializationContext Context { get; }
    }
}

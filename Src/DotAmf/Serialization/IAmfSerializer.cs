using System.Runtime.Serialization;
using DotAmf.Data;

namespace DotAmf.Serialization
{
    /// <summary>
    /// AMF serializer.
    /// </summary>
    public interface IAmfSerializer
    {
        /// <summary>
        /// Write a value.
        /// </summary>
        /// <param name="value">Value to write.</param>
        /// <exception cref="SerializationException">Error during serialization.</exception>
        void WriteValue(object value);

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

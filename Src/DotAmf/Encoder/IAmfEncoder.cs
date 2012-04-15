using System.Runtime.Serialization;
using DotAmf.Data;

namespace DotAmf.Encoder
{
    /// <summary>
    /// AMF encoder.
    /// </summary>
    public interface IAmfEncoder
    {
        /// <summary>
        /// Write AMF packet headers.
        /// </summary>
        /// <param name="header">Header data.</param>
        void WritePacketHeader(AmfHeader header);

        /// <summary>
        /// Write AMF packet body.
        /// </summary>
        /// <param name="message">Message body.</param>
        void WritePacketBody(AmfMessage message);

        /// <summary>
        /// Write a value.
        /// </summary>
        /// <param name="value">Value to write.</param>
        /// <exception cref="SerializationException">Error during serialization.</exception>
        void WriteValue(object value);
    }
}

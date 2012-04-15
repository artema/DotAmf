using System;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;
using DotAmf.Data;

namespace DotAmf.Decoder
{
    /// <summary>
    /// AMF decoder.
    /// </summary>
    public interface IAmfDecoder
    {
        /// <summary>
        /// Decode data to AMFX format.
        /// </summary>
        /// <param name="stream">AMF stream.</param>
        /// <param name="output">AMFX output writer.</param>
        /// <exception cref="NotSupportedException">AMF type is not supported.</exception>
        /// <exception cref="FormatException">Unknown data format.</exception>
        /// <exception cref="SerializationException">Error during deserialization.</exception>
        /// <exception cref="InvalidOperationException">Invalid AMF context.</exception>
        void Decode(Stream stream, XmlWriter output);

        /// <summary>
        /// Read an AMF packet header descriptor.
        /// </summary>
        /// <exception cref="FormatException">Data has unknown format.</exception>
        AmfHeaderDescriptor ReadPacketHeader(Stream stream);

        /// <summary>
        /// Read AMF packet body descriptor.
        /// </summary>
        /// <exception cref="FormatException">Data has unknown format.</exception>
        AmfMessageDescriptor ReadPacketBody(Stream stream);
    }
}

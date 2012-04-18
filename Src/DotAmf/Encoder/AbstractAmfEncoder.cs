using System;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;
using DotAmf.Data;
using DotAmf.IO;

namespace DotAmf.Encoder
{
    /// <summary>
    /// Abstract AMF encoder.
    /// </summary>
    abstract internal class AbstractAmfEncoder : IAmfEncoder
    {
        #region .ctor
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="encodingOptions">AMF encoding options.</param>
        protected AbstractAmfEncoder(AmfEncodingOptions encodingOptions)
        {
            EncodingOptions = encodingOptions;
        }
        #endregion

        #region Data
        /// <summary>
        /// AMF encoding options.
        /// </summary>
        protected AmfEncodingOptions EncodingOptions { get; private set; }
        #endregion

        #region IAmfSerializer implementation
        public abstract void Encode(Stream stream, XmlReader input);

        public abstract void WritePacketHeader(Stream stream, AmfHeaderDescriptor descriptor);

        public abstract void WritePacketBody(Stream stream, AmfMessageDescriptor descriptor);
        #endregion

        #region Proptected methods
        /// <summary>
        /// Write AMF value from the current position.
        /// </summary>
        /// <param name="context">AMF decoding context.</param>
        /// <param name="input">AMFX input reader.</param>
        /// <param name="writer">AMF stream writer.</param>
        /// <exception cref="NotSupportedException">AMF type is not supported.</exception>
        /// <exception cref="FormatException">Invalid data format.</exception>
        /// <exception cref="SerializationException">Error during serialization.</exception>
        protected abstract void WriteAmfValue(AmfContext context, XmlReader input, AmfStreamWriter writer);

        /// <summary>
        /// Create default AMF decoding context.
        /// </summary>
        protected AmfContext CreateDefaultContext()
        {
            //In mixed context enviroinments, 
            //AMF0 is always used by default
            var amfVersion = EncodingOptions.UseContextSwitch
                ? AmfVersion.Amf0
                : EncodingOptions.AmfVersion;

            return new AmfContext(amfVersion);
        }
        #endregion
    }
}

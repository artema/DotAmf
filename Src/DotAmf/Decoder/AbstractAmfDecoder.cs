using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;
using DotAmf.Data;

namespace DotAmf.Decoder
{
    /// <summary>
    /// Abstract AMF decoder.
    /// </summary>
    abstract class AbstractAmfDecoder : IAmfDecoder
    {
        #region .ctor
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="decodingOptions">AMF decoding options.</param>
        protected AbstractAmfDecoder(AmfEncodingOptions decodingOptions)
        {
            DecodingOptions = decodingOptions;
        }
        #endregion

        #region Data
        /// <summary>
        /// AMF decoding options.
        /// </summary>
        protected AmfEncodingOptions DecodingOptions { get; private set; }
        #endregion

        #region IAmfDeserializer implementation
        public abstract AmfHeaderDescriptor ReadPacketHeader(Stream stream);

        public abstract AmfMessageDescriptor ReadPacketBody(Stream stream);

        public abstract void Decode(Stream stream, XmlWriter output);
        #endregion

        #region Proptected methods
        /// <summary>
        /// Read AMF value from the current position.
        /// </summary>
        /// <param name="context">AMF decoding context.</param>
        /// <param name="output">AMFX output writer.</param>
        /// <exception cref="NotSupportedException">AMF type is not supported.</exception>
        /// <exception cref="FormatException">Invalid data format.</exception>
        /// <exception cref="SerializationException">Error during deserialization.</exception>
        protected abstract void ReadAmfValue(AmfEncodingContext context, XmlWriter output = null);

        /// <summary>
        /// Create default AMF decoding context.
        /// </summary>
        protected AmfEncodingContext CreateDefaultContext(BinaryReader reader)
        {
            //In mixed context enviroinments, 
            //AMF0 is always used by default
            var amfVersion = DecodingOptions.UseContextSwitch
                ? AmfVersion.Amf0
                : DecodingOptions.AmfVersion;

            return new AmfEncodingContext(reader, amfVersion);
        }
        #endregion

        #region Helper classes
        /// <summary>
        /// AMF encoding context.
        /// </summary>
        sealed protected class AmfEncodingContext
        {
            #region .ctor
            public AmfEncodingContext(BinaryReader reader, AmfVersion version)
            {
                Reader = reader;

                AmfVersion = version;

                StringReferences = new List<string>();
                TraitsReferences = new List<AmfTypeTraits>();
            }
            #endregion

            #region Properties
            /// <summary>
            /// Stream reader.
            /// </summary>
            public BinaryReader Reader { get; private set; }

            /// <summary>
            /// AMF version.
            /// </summary>
            public AmfVersion AmfVersion { get; private set; }

            /// <summary>
            /// Reference count.
            /// </summary>
            public uint References { get; private set; }

            /// <summary>
            /// String references.
            /// </summary>
            public IList<string> StringReferences { get; private set; }

            /// <summary>
            /// Traits references.
            /// </summary>
            public IList<AmfTypeTraits> TraitsReferences { get; private set; }
            #endregion

            #region Public methods
            /// <summary>
            /// Increment the reference counter.
            /// </summary>
            public void CountReference()
            {
                References++;
            }

            /// <summary>
            /// Reset reference counter.
            /// </summary>
            public void ResetReferences()
            {
                References = 0;
                StringReferences.Clear();
                TraitsReferences.Clear();
            }
            #endregion
        }
        #endregion
    }
}

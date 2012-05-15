// Copyright (c) 2012 Artem Abashev (http://abashev.me)
// All rights reserved.
// Licensed under the Microsoft Public License (Ms-PL)
// http://opensource.org/licenses/ms-pl.html

using System;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;
using DotAmf.Data;
using DotAmf.IO;

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
        /// <param name="context">AMF context.</param>
        /// <param name="reader">AMF stream reader.</param>
        /// <param name="output">AMFX output writer.</param>
        /// <exception cref="NotSupportedException">AMF type is not supported.</exception>
        /// <exception cref="FormatException">Invalid data format.</exception>
        /// <exception cref="SerializationException">Error during deserialization.</exception>
        protected abstract void ReadAmfValue(AmfContext context, AmfStreamReader reader, XmlWriter output = null);

        /// <summary>
        /// Create default AMF decoding context.
        /// </summary>
        protected AmfContext CreateDefaultContext()
        {
            //In mixed context enviroinments, 
            //AMF0 is always used by default
            var amfVersion = DecodingOptions.UseContextSwitch
                ? AmfVersion.Amf0
                : DecodingOptions.AmfVersion;

            return new AmfContext(amfVersion);
        }
        #endregion
    }
}

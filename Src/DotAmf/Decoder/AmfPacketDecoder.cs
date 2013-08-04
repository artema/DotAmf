// Copyright (c) 2012 Artem Abashev (http://abashev.me)
// All rights reserved.
// Licensed under the Microsoft Public License (Ms-PL)
// http://opensource.org/licenses/ms-pl.html

using System;
using System.IO;
using System.Xml;
using DotAmf.Data;
using DotAmf.IO;

namespace DotAmf.Decoder
{
    /// <summary>
    /// AMF packet decoder.
    /// </summary>
    sealed class AmfPacketDecoder
    {
        #region .ctor
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="options">Encoding options.</param>
        public AmfPacketDecoder(AmfEncodingOptions options)
        {
            if (!options.UseContextSwitch)
                throw new ArgumentException(Errors.AmfPacketReader_AmfPacketReader_ContextSwitchRequired, "options");

            _options = options;
        }
        #endregion

        #region Data
        /// <summary>
        /// AMF encoding options.
        /// </summary>
        private readonly AmfEncodingOptions _options;
        #endregion

        #region Public methods
        /// <summary>
        /// Decode an AMF packet into an AMFX format.
        /// </summary>
        /// <exception cref="InvalidDataException">Error during decoding.</exception>
        public void Decode(Stream stream, XmlWriter output)
        {
            if (stream == null) throw new ArgumentNullException("stream");
            if (!stream.CanRead) throw new ArgumentException(Errors.AmfPacketReader_Read_StreamClosed, "stream");
            if (output == null) throw new ArgumentNullException("output");

            try
            {
                var amfStreamReader = new AmfStreamReader(stream);

                var version = ReadPacketVersion(amfStreamReader);
                var decoder = CreateDecoder(version, _options);

                output.WriteStartDocument();
                output.WriteStartElement(AmfxContent.AmfxDocument, AmfxContent.Namespace);
                output.WriteAttributeString(AmfxContent.VersionAttribute, version.ToAmfxName());
                output.Flush();

                //Read headers
                var headerCount = ReadDataCount(amfStreamReader);

                for (var i = 0; i < headerCount; i++)
                {
                    var header = decoder.ReadPacketHeader(stream);

                    output.WriteStartElement(AmfxContent.PacketHeader);
                    output.WriteAttributeString(AmfxContent.PacketHeaderName, header.Name);
                    output.WriteAttributeString(AmfxContent.PacketHeaderMustUnderstand, header.MustUnderstand.ToString());
                    decoder.Decode(stream, output);
                    output.WriteEndElement();
                    output.Flush();
                }

                //Read messages
                var messageCount = ReadDataCount(amfStreamReader);

                for (var i = 0; i < messageCount; i++)
                {
                    var body = decoder.ReadPacketBody(stream);

                    output.WriteStartElement(AmfxContent.PacketBody);
                    output.WriteAttributeString(AmfxContent.PacketBodyTarget, body.Target);
                    output.WriteAttributeString(AmfxContent.PacketBodyResponse, body.Response);
                    decoder.Decode(stream, output);
                    output.WriteEndElement();
                    output.Flush();
                }

                output.WriteEndElement();
                output.WriteEndDocument();
                output.Flush();
            }
            catch (Exception e)
            {
                output.Flush();
                throw new InvalidDataException(Errors.AmfPacketReader_DecodingError, e);
            }
        }
        #endregion

        #region Private methods
        /// <summary>
        /// Read AMF packet version.
        /// </summary>
        /// <exception cref="FormatException">Data has unknown format.</exception>
        private static AmfVersion ReadPacketVersion(AmfStreamReader reader)
        {
            try
            {
                //First two bytes contain message version number
                return (AmfVersion)reader.ReadUInt16();
            }
            catch (Exception e)
            {
                throw new FormatException(Errors.AmfPacketReader_ReadPacketVersion_VersionReadError, e);
            }
        }

        /// <summary>
        /// Read number of following headers/messages.
        /// </summary>
        private static uint ReadDataCount(AmfStreamReader reader)
        {
            return reader.ReadUInt16();
        }

        /// <summary>
        /// Create AMF decoder.
        /// </summary>
        /// <param name="version">AMF packet version.</param>
        /// <param name="options">Encoding options.</param>
        private static IAmfDecoder CreateDecoder(AmfVersion version, AmfEncodingOptions options)
        {
            switch (version)
            {
                case AmfVersion.Amf0:
                    return new Amf0Decoder(options);

                case AmfVersion.Amf3:
                    return new Amf3Decoder(options);

                default:
                    throw new NotSupportedException();
            }
        }
        #endregion
    }
}

using System;
using System.IO;
using System.Xml;
using DotAmf.Data;
using DotAmf.IO;

namespace DotAmf.Encoder
{
    /// <summary>
    /// AMF packet writer.
    /// </summary>
    sealed public class AmfPacketEncoder
    {
        #region .ctor
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="options">Encoding options.</param>
        public AmfPacketEncoder(AmfEncodingOptions options)
        {
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
        /// Encode an AMF packet from an AMFX format.
        /// </summary>
        /// <exception cref="InvalidDataException">Error during encoding.</exception>
        public void Encode(Stream stream, XmlReader input)
        {
            if (stream == null) throw new ArgumentNullException("stream");
            if (!stream.CanWrite) throw new ArgumentException(Errors.AmfPacketWriter_Write_StreamNotWriteable, "stream");
            if (input == null) throw new ArgumentNullException("input");
            
            try
            {
                #region Encode data
                var encoder = CreateEncoder(_options);
                var amfStreamWriter = new AmfStreamWriter(stream);

                WriteAmfVersion(amfStreamWriter, _options.AmfVersion);

                input.MoveToContent();

                var headerCount = Convert.ToInt32(input.GetAttribute(AmfxContent.PacketHeaderCount));
                var bodyCount = Convert.ToInt32(input.GetAttribute(AmfxContent.PacketBodyCount));

                while (input.Read())
                {
                    if (input.NodeType != XmlNodeType.Element) continue;

                    if (headerCount != -1)
                    {
                        WriteHeaderCount(amfStreamWriter, headerCount);
                        headerCount = -1;
                    }

                    #region Read packet header
                    if (input.Name == AmfxContent.PacketHeader)
                    {
                        var header = new AmfHeaderDescriptor();
                        var headerreader = input.ReadSubtree();
                        headerreader.MoveToContent();

                        header.Name = headerreader.GetAttribute(AmfxContent.PacketHeaderName);
                        header.MustUnderstand = (headerreader.GetAttribute(AmfxContent.PacketHeaderMustUnderstand) == AmfxContent.True);
                        encoder.WritePacketHeader(stream, header);

                        while (headerreader.Read())
                        {
                            //Skip until header content is found, if any
                            if (headerreader.NodeType != XmlNodeType.Element || headerreader.Name == AmfxContent.PacketHeader)
                                continue;

                            encoder.Encode(stream, headerreader);
                            break;
                        }

                        headerreader.Close();
                        continue;
                    }
                    #endregion

                    if (bodyCount != -1)
                    {
                        WriteMessageCount(amfStreamWriter, bodyCount);
                        bodyCount = -1;
                    }

                    #region Read packet body
                    if (input.Name == AmfxContent.PacketBody)
                    {
                        var message = new AmfMessageDescriptor();
                        var bodyreader = input.ReadSubtree();
                        bodyreader.MoveToContent();

                        message.Target = bodyreader.GetAttribute(AmfxContent.PacketBodyTarget);
                        message.Response = bodyreader.GetAttribute(AmfxContent.PacketBodyResponse);
                        encoder.WritePacketBody(stream, message);

                        while (bodyreader.Read())
                        {
                            //Skip until body content is found, if any
                            if (bodyreader.NodeType != XmlNodeType.Element || bodyreader.Name == AmfxContent.PacketBody)
                                continue;
                            
                            encoder.Encode(stream, bodyreader);
                            break;
                        }

                        bodyreader.Close();
                        continue;
                    }
                    #endregion
                }
                #endregion
            }
            catch (Exception e)
            {
                throw new InvalidDataException(Errors.AmfPacketReader_DecodingError, e);
            }
        }
        #endregion

        #region Private methods
        /// <summary>
        /// Create an AMF encoder.
        /// </summary>
        /// <param name="options">Encoding options.</param>
        static private IAmfEncoder CreateEncoder(AmfEncodingOptions options)
        {
            switch (options.AmfVersion)
            {
                case AmfVersion.Amf0:
                    return new Amf0Encoder(options);

                case AmfVersion.Amf3:
                    return new Amf3Encoder(options);

                default:
                    throw new NotSupportedException();
            }
        }
        #endregion

        #region Write methods
        /// <summary>
        /// Write AMF message version.
        /// </summary>
        static private void WriteAmfVersion(AmfStreamWriter writer, AmfVersion version)
        {
            writer.Write((ushort)version);
        }

        /// <summary>
        /// Write AMF message headers count.
        /// </summary>
        static private void WriteHeaderCount(AmfStreamWriter writer, int count)
        {
            writer.Write((ushort)count);
        }

        /// <summary>
        /// Write AMF message bodies count.
        /// </summary>
        static private void WriteMessageCount(AmfStreamWriter writer, int count)
        {
            writer.Write((ushort)count);
        }
        #endregion
    }
}

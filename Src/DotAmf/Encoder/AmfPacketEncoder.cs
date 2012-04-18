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

            var headerstream = new MemoryStream();
            var bodystream = new MemoryStream();
            var headercount = 0;
            var bodycount = 0;

            try
            {
                #region Encode data
                var encoder = CreateEncoder(_options);

                input.MoveToContent();

                while (input.Read())
                {
                    if (input.NodeType != XmlNodeType.Element) continue;

                    #region Read packet header
                    if (input.Name == AmfxContent.PacketHeader)
                    {
                        headercount++;

                        var header = new AmfHeaderDescriptor();
                        var headerreader = input.ReadSubtree();
                        headerreader.MoveToContent();

                        header.Name = headerreader.GetAttribute(AmfxContent.PacketHeaderName);
                        header.MustUnderstand = (headerreader.GetAttribute(AmfxContent.PacketHeaderMustUnderstand) == AmfxContent.True);
                        encoder.WritePacketHeader(headerstream, header);

                        while (headerreader.Read())
                        {
                            //Skip until header content is found, if any
                            if (headerreader.NodeType != XmlNodeType.Element || headerreader.Name == AmfxContent.PacketHeader)
                                continue;
                            
                            encoder.Encode(headerstream, headerreader);
                            break;
                        }

                        headerreader.Close();
                        continue;
                    }
                    #endregion

                    #region Read packet body
                    if (input.Name == AmfxContent.PacketBody)
                    {
                        bodycount++;

                        var message = new AmfMessageDescriptor();
                        var bodyreader = input.ReadSubtree();
                        bodyreader.MoveToContent();

                        message.Target = bodyreader.GetAttribute(AmfxContent.PacketBodyTarget);
                        message.Response = bodyreader.GetAttribute(AmfxContent.PacketBodyResponse);
                        encoder.WritePacketBody(bodystream, message);

                        while (bodyreader.Read())
                        {
                            //Skip until body content is found, if any
                            if (bodyreader.NodeType != XmlNodeType.Element || bodyreader.Name == AmfxContent.PacketBody)
                                continue;
                            
                            encoder.Encode(bodystream, bodyreader);
                            break;
                        }

                        bodyreader.Close();
                        continue;
                    }
                    #endregion
                }
                #endregion

                #region Write data
                var amfStreamWriter = new AmfStreamWriter(stream);

                WriteAmfVersion(amfStreamWriter, _options.AmfVersion);

                WriteHeaderCount(amfStreamWriter, headercount);

                if(headercount > 0)
                {
                    headerstream.Seek(0, SeekOrigin.Begin);
                    headerstream.CopyTo(stream);
                }

                WriteMessageCount(amfStreamWriter, bodycount);

                if (bodycount > 0)
                {
                    bodystream.Seek(0, SeekOrigin.Begin);
                    bodystream.CopyTo(stream);
                }
                #endregion
            }
            catch (Exception e)
            {
                throw new InvalidDataException(Errors.AmfPacketReader_DecodingError, e);
            }
            finally
            {
                headerstream.Dispose();
                bodystream.Dispose();
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

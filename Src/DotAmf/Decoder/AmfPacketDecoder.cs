using System;
using System.IO;
using DotAmf.Data;
using DotAmf.IO;

namespace DotAmf.Decoder
{
    /// <summary>
    /// AMF packet decoder.
    /// </summary>
    sealed public class AmfPacketDecoder
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
        /// Read an AMF packet.
        /// </summary>
        /// <exception cref="InvalidDataException">Error during decoding.</exception>
        public AmfPacket Read(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException("stream");
            if (!stream.CanRead) throw new ArgumentException(Errors.AmfPacketReader_Read_StreamClosed, "stream");

            using (var reader = new AmfStreamReader(stream))
            {
                IAmfDecoder decoder = null;

                try
                {
                    var version = ReadPacketVersion(reader);
                    var packet = new AmfPacket();

                    decoder = CreateDecoder(version, reader, _options);
                    decoder.ContextSwitch += OnContextSwitch;

                    //Read headers
                    foreach (var header in decoder.ReadPacketHeaders())
                    {
                        decoder.ClearReferences();
                        packet.Headers[header.Name] = header;
                    }

                    //Read messages
                    foreach (var message in decoder.ReadPacketMessages())
                    {
                        decoder.ClearReferences();
                        packet.Messages.Add(message);
                    }

                    return packet;
                }
                catch (Exception e)
                {
                    throw new InvalidDataException(Errors.AmfPacketReader_DecodingError, e);
                }
                finally
                {
                    if (decoder != null)
                    {
                        decoder.ContextSwitch -= OnContextSwitch;
                        decoder.ClearReferences();
                    }
                }
            }
        }
        #endregion

        #region Private methods
        /// <summary>
        /// Read AMF packet version.
        /// </summary>
        /// <exception cref="FormatException">Data has unknown format.</exception>
        static private AmfVersion ReadPacketVersion(AmfStreamReader reader)
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
        /// Create AMF decoder.
        /// </summary>
        /// <param name="version">AMF packet version.</param>
        /// <param name="reader">AMF stream reader.</param>
        /// <param name="options">Encoding options.</param>
        static private IAmfDecoder CreateDecoder(AmfVersion version, BinaryReader reader, AmfEncodingOptions options)
        {
            switch (version)
            {
                case AmfVersion.Amf0:
                    return new Amf0Decoder(reader, options);

                case AmfVersion.Amf3:
                    return new Amf3Decoder(reader, options);

                default:
                    throw new NotSupportedException();
            }
        }

        /// <summary>
        /// AMF context switch event hanlder.
        /// </summary>
        static private void OnContextSwitch(object sender, EncodingContextSwitchEventArgs e)
        {
            var decoder = (IAmfDecoder) sender;
            decoder.ClearReferences();
        }
        #endregion
    }
}

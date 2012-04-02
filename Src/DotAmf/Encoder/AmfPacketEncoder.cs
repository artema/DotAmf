using System;
using System.IO;
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
        /// Write an AMF packet.
        /// </summary>
        /// <exception cref="InvalidDataException">Error during encoding.</exception>
        public void Write(Stream stream, AmfPacket packet)
        {
            if (stream == null) throw new ArgumentNullException("stream");
            if (!stream.CanWrite)
                throw new ArgumentException(Errors.AmfPacketWriter_Write_StreamNotWriteable, "stream");

            if(packet == null) throw new ArgumentNullException("packet");
            if (packet.Messages.Count == 0) 
                throw new ArgumentException(Errors.AmfPacketWriter_Write_PacketEmpty, "packet");

            using(var writer = new AmfStreamWriter(stream))
            {
                var encoder = CreateEncoder(writer, _options);
                encoder.ContextSwitch += OnContextSwitch;

                try
                {
                    WriteAmfVersion(writer, _options.AmfVersion);
                    WriteHeaderCount(writer, packet.Headers.Count);

                    foreach (var pair in packet.Headers)
                    {
                        encoder.ClearReferences();
                        encoder.WritePacketHeader(pair.Value);
                    }

                    WriteMessageCount(writer, packet.Messages.Count);

                    foreach (var message in packet.Messages)
                    {
                        encoder.ClearReferences();
                        encoder.WritePacketBody(message);
                    }
                }
                catch (Exception e)
                {
                    throw new InvalidDataException(Errors.AmfPacketEncoder_EncodingError, e);
                }
                finally
                {
                    encoder.ContextSwitch -= OnContextSwitch;
                    encoder.ClearReferences();
                }
            }
        }
        #endregion

        #region Private methods
        /// <summary>
        /// Create AMF encoder.
        /// </summary>
        /// <param name="writer">AMF stream writer.</param>
        /// <param name="options">Encoding options.</param>
        static private IAmfEncoder CreateEncoder(BinaryWriter writer, AmfEncodingOptions options)
        {
            switch (options.AmfVersion)
            {
                case AmfVersion.Amf0:
                    return new Amf0Encoder(writer, options);

                case AmfVersion.Amf3:
                    return new Amf3Encoder(writer, options);

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

        /// <summary>
        /// Context switch event handler.
        /// </summary>
        static private void OnContextSwitch(object sender, EncodingContextSwitchEventArgs e)
        {
            var encoder = (IAmfEncoder)sender;
            encoder.ClearReferences();
        }
        #endregion
    }
}

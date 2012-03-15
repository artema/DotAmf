using System;
using System.IO;
using System.Runtime.Serialization;
using DotAmf.Data;
using DotAmf.IO;

namespace DotAmf.Serialization
{
    /// <summary>
    /// AMF packet writer.
    /// </summary>
    sealed public class AmfPacketWriter : IDisposable
    {
        #region .ctor
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="stream">AMF output stream.</param>
        public AmfPacketWriter(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException("stream");

            _writer = new AmfStreamWriter(stream);
        }
        #endregion

        #region Constants
        /// <summary>
        /// <c>Null</c> value.
        /// </summary>
        private const string Null =  "null";
        #endregion

        #region Data
        /// <summary>
        /// Stream writer.
        /// </summary>
        private readonly BinaryWriter _writer;

        /// <summary>
        /// AMF packet serializer.
        /// </summary>
        private IAmfSerializer _packetSerializer;

        /// <summary>
        /// AMF data serializer.
        /// </summary>
        private IAmfSerializer _dataSerializer;
        #endregion

        #region Public methods
        /// <summary>
        /// Write an AMF packet.
        /// </summary>
        /// <exception cref="SerializationException">Error during serialization.</exception>
        public void Write(AmfPacket packet)
        {
            if (packet == null) throw new ArgumentNullException("packet");
            if (packet.Messages.Count == 0) throw new ArgumentException("Packet contains no messages.", "packet");

            try
            {
                _packetSerializer = CreateSerializer(packet.Version);

                _dataSerializer = packet.Version != AmfVersion.Amf0
                                      ? CreateSerializer(packet.Version)
                                      : _packetSerializer;

                var headerCount = (uint) packet.Headers.Count;
                WriteHeaderCount(headerCount);

                for (var i = 0; i < headerCount; i++)
                    WriteHeader(packet.Headers[i], packet.Version);

                var messageCount = (uint)packet.Messages.Count;
                WriteMessageCount(messageCount);

                for (var i = 0; i < messageCount; i++)
                    WriteMessage(packet.Messages[i], packet.Version);

            }
            catch (Exception e)
            {
                throw new SerializationException("Error during serialization. Check inner exception for details.", e);
            }
            finally
            {
                _dataSerializer.ClearReferences();
                _dataSerializer = null;
                _packetSerializer = null;
            }
        }
        #endregion

        #region Private methods
        /// <summary>
        /// Create AMF serializer.
        /// </summary>
        /// <param name="version">AMF version.</param>
        private IAmfSerializer CreateSerializer(AmfVersion version)
        {
            switch (version)
            {
                case AmfVersion.Amf0:
                    return new Amf0Serializer(_writer);

                case AmfVersion.Amf3:
                    return new Amf3Serializer(_writer);

                default:
                    throw new NotSupportedException("Serializer for AMF type '" + version + "' is not implemented.");
            }
        }
        #endregion

        #region Write methods
        private void WriteHeaderCount(uint count)
        {
            _writer.Write(count);
        }

        private void WriteMessageCount(uint count)
        {
            _writer.Write(count);
        }

        private void WriteHeader(AmfHeader header, AmfVersion version)
        {
            _dataSerializer.ClearReferences();

            //Write header metadata
            _packetSerializer.WriteValue(header.Name);
            _packetSerializer.WriteValue(header.MustUnderstand);

            //Write header length
            _writer.Write(-1);
            
            var contentLength = WriteValue(header.Data, version);

            if(_writer.BaseStream.CanSeek)
            {
                _writer.Seek(-1 * (contentLength + 1), SeekOrigin.Current);
                _writer.Write(contentLength);
                _writer.Seek(0, SeekOrigin.Current);
            }
        }

        private void WriteMessage(AmfMessage message, AmfVersion version)
        {
            _dataSerializer.ClearReferences();

            //Write message metadata
            _packetSerializer.WriteValue(message.Target ?? Null);
            _packetSerializer.WriteValue(message.Response ?? Null);

            //Write message length
            _writer.Write(-1);

            var contentLength = WriteValue(message.Data, version);

            if (_writer.BaseStream.CanSeek)
            {
                _writer.Seek(-1 * (contentLength + 1), SeekOrigin.Current);
                _writer.Write(contentLength);
                _writer.Seek(0, SeekOrigin.Current);
            }
        }

        private int WriteValue(object value, AmfVersion version)
        {
            int length = 0;

            //Write AMF+ marker);
            if (version == AmfVersion.Amf3)
            {
                _writer.Write((byte) Amf0TypeMarker.AvmPlusObject);
                length += 1;
            }

            //Write value
            length += _dataSerializer.WriteValue(value);

            return length;
        }
        #endregion

        #region IDispose implementation
        public void Dispose()
        {
            _writer.Dispose();
            _packetSerializer = null;
            _dataSerializer = null;
        }
        #endregion
    }
}

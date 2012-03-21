using System;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
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
        /// <param name="context">Serialization context.</param>
        public AmfPacketWriter(Stream stream, AmfSerializationContext context)
        {
            if (stream == null) throw new ArgumentNullException("stream");

            _writer = new AmfStreamWriter(stream);
            _baseContext = context;
        }
        #endregion

        #region Constants
        /// <summary>
        /// <c>Null</c> value.
        /// </summary>
        private const string Null = "null";
        #endregion

        #region Data
        /// <summary>
        /// Stream writer.
        /// </summary>
        private readonly BinaryWriter _writer;

        /// <summary>
        /// Base serialization context.
        /// </summary>
        private readonly AmfSerializationContext _baseContext;

        /// <summary>
        /// AMF packet serializer.
        /// </summary>
        private Amf0Serializer _packetSerializer;

        /// <summary>
        /// AMF data serializer.
        /// </summary>
        private Amf0Serializer _dataSerializer;
        #endregion

        #region Public methods
        /// <summary>
        /// Write an AMF packet.
        /// </summary>
        /// <exception cref="SerializationException">Error during serialization.</exception>
        public void Write(AmfPacket packet, AmfVersion version)
        {
            if (packet == null) throw new ArgumentNullException("packet");
            if (packet.Messages.Count == 0) throw new ArgumentException("Packet contains no messages.", "packet");

            try
            {
                _packetSerializer = CreateSerializer(AmfVersion.Amf0);
                _dataSerializer = version != AmfVersion.Amf0
                                      ? CreateSerializer(version)
                                      : _packetSerializer;

                if (version != AmfVersion.Amf0)
                    _dataSerializer.ContextSwitch += OnContextSwitch;

                WriteAmfVersion(version);

                var headerCount = (ushort)packet.Headers.Count;
                WriteHeaderCount(headerCount);

                foreach (var pair in packet.Headers)
                    WriteHeader(pair.Value);

                var messageCount = (ushort)packet.Messages.Count;
                WriteMessageCount(messageCount);

                for (var i = 0; i < messageCount; i++)
                    WriteMessage(packet.Messages[i]);

            }
            catch (Exception e)
            {
                throw new SerializationException("Error during serialization. Check inner exception for details.", e);
            }
            finally
            {
                if (version != AmfVersion.Amf0)
                    _dataSerializer.ContextSwitch -= OnContextSwitch;

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
        private Amf0Serializer CreateSerializer(AmfVersion version)
        {
            var context = _baseContext;
            context.AmfVersion = version;

            switch (version)
            {
                case AmfVersion.Amf0:
                    return new Amf0Serializer(_writer, context);

                //By default, all AMF packets are encoded in AMF0.
                case AmfVersion.Amf3:
                    context.AmfVersion = AmfVersion.Amf0;
                    return new Amf3Serializer(_writer, context);

                default:
                    throw new NotSupportedException("Serializer for AMF type '" + version + "' is not implemented.");
            }
        }
        #endregion

        #region Write methods
        private void WriteAmfVersion(AmfVersion version)
        {
            _writer.Write((ushort)version);
        }

        private void WriteHeaderCount(ushort count)
        {
            _writer.Write(count);
        }

        private void WriteMessageCount(ushort count)
        {
            _writer.Write(count);
        }

        private void WriteHeader(AmfHeader header)
        {
            _dataSerializer.ClearReferences();

            //Write header metadata
            WriteUtfShort(header.Name);
            _writer.Write((byte)(header.MustUnderstand ? 0 : 1));
            _packetSerializer.WriteValue(header.MustUnderstand);

            //Write header length
            _writer.Write(-1);

            _dataSerializer.WriteValue(header.Data);
        }

        private void WriteMessage(AmfMessage message)
        {
            _dataSerializer.ClearReferences();

            //Write message metadata
            WriteUtfShort(message.Target ?? Null);
            WriteUtfShort(message.Response ?? Null);

            //Write message length
            _writer.Write(-1);

            _dataSerializer.WriteValue(message.Data);
        }

        private void WriteUtfShort(string text)
        {
            var data = Encoding.UTF8.GetBytes(text);
            _writer.Write((ushort)data.Length);
            _writer.Write(data);
        }

        /// <summary>
        /// Context switch event handler.
        /// </summary>
        private void OnContextSwitch(object sender, ContextSwitchEventArgs e)
        {
            _dataSerializer.ClearReferences();

            if(e.Context.AmfVersion != AmfVersion.Amf0)
                _dataSerializer.Write(Amf0TypeMarker.AvmPlusObject);
        }
        #endregion

        #region IDispose implementation
        public void Dispose()
        {
            _writer.Flush();
            _writer.Dispose();
            _packetSerializer = null;
            _dataSerializer = null;
        }
        #endregion
    }
}

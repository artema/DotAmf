using System;
using System.IO;
using System.Runtime.Serialization;
using DotAmf.Data;
using DotAmf.IO;

namespace DotAmf.Serialization
{
    /// <summary>
    /// AMF reader.
    /// </summary>
    sealed public class AmfReader : IDisposable
    {
        #region .ctor
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="stream">AMF0 data stream.</param>
        public AmfReader(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException("stream");

            _reader = new AmfStreamReader(stream);
        }
        #endregion

        #region Data
        /// <summary>
        /// Stream reader.
        /// </summary>
        private readonly AmfStreamReader _reader;

        /// <summary>
        /// Current AMF deserializer.
        /// </summary>
        private IAmfDeserializer _deserializer;
        #endregion

        #region Public methods
        /// <summary>
        /// Read an AMF packet.
        /// </summary>
        /// <remarks>
        /// Type declaration:
        /// <c>amf-packet = version header-count *(header-type) message-count *(message-type)</c>
        /// </remarks>
        /// <exception cref="SerializationException">Error during deserialization.</exception>
        public AmfPacket Read()
        {
            try
            {
                var version = ReadAmfVersion();
                var packet = new AmfPacket(version);

                _deserializer = CreateDeserializer(version);
                _deserializer.ContextSwitch += OnContextSwitch;

                //Read headers count
                var headerCount = ReadHeaderCount();

                for (var i = 0; i < headerCount; i++)
                {
                    _deserializer.ClearReferences();

                    var header = _deserializer.ReadHeader();
                    packet.Headers.Add(header);
                }

                //Read messages count
                var messageCount = ReadMessageCount();

                for (var i = 0; i < messageCount; i++)
                {
                    _deserializer.ClearReferences();

                    var message = _deserializer.ReadMessage();
                    packet.Messages.Add(message);
                }

                return packet;
            }
            catch (Exception e)
            {
                throw new SerializationException("Error during deserialization. Check inner exception for details.", e);
            }
            finally
            {
                _deserializer.ClearReferences();
                _deserializer.ContextSwitch -= OnContextSwitch;
                _deserializer = null;
            }
        }
        #endregion

        #region Private methods
        /// <summary>
        /// Read AMF version.
        /// </summary>
        /// <remarks>
        /// After this operation, stream's read position will be on an AMF headers section.
        /// </remarks>
        /// <exception cref="FormatException">Data has unknown format.</exception>
        private AmfVersion ReadAmfVersion()
        {
            try
            {
                //First two bytes contain message version number
                return (AmfVersion) _reader.ReadUInt16();
            }
            catch
            {
                throw new FormatException("Unable to read AMF version. Data has unknown format.");
            }
        }

        private ushort ReadHeaderCount()
        {
            //Up to 65535 headers are possible
            return _reader.ReadUInt16();
        }

        private ushort ReadMessageCount()
        {
            //Up to 65535 messages are possible
            return _reader.ReadUInt16();
        }

        /// <summary>
        /// Create AMF deserializer.
        /// </summary>
        /// <param name="version">AMF version.</param>
        private IAmfDeserializer CreateDeserializer(AmfVersion version)
        {
            switch (version)
            {
                case AmfVersion.Amf0:
                    return new Amf0Deserializer(_reader);

                case AmfVersion.Amf3:
                    return new Amf3Deserializer(_reader);

                default:
                    throw new NotSupportedException("Deserializer for AMF type '" + version + "' is not implemented.");
            }
        }

        /// <summary>
        /// Context switch event handler.
        /// </summary>
        private void OnContextSwitch(object sender, ContextSwitchEventArgs e)
        {
            _deserializer.Context = CreateDeserializer(e.ContextVersion);
        }
        #endregion

        #region IDispose implementation
        public void Dispose()
        {
            _reader.Dispose();
        }
        #endregion
    }
}

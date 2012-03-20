using System;
using System.IO;
using System.Runtime.Serialization;
using DotAmf.Data;
using DotAmf.IO;

namespace DotAmf.Serialization
{
    /// <summary>
    /// AMF packet reader.
    /// </summary>
    sealed public class AmfPacketReader : IDisposable
    {
        #region .ctor
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="stream">AMF input stream.</param>
        public AmfPacketReader(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException("stream");

            _reader = new AmfStreamReader(stream);
        }
        #endregion

        #region Data
        /// <summary>
        /// Stream reader.
        /// </summary>
        private readonly BinaryReader _reader;

        /// <summary>
        /// Current AMF deserializer.
        /// </summary>
        private Amf0Deserializer _deserializer;
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
                var packet = new AmfPacket();

                _deserializer = CreateDeserializer(version);
                _deserializer.ContextSwitch += OnContextSwitch;

                //Read headers count
                var headerCount = _reader.ReadUInt16();

                for (var i = 0; i < headerCount; i++)
                {
                    _deserializer.ClearReferences();

                    var header = ReadHeader();
                    packet.Headers[header.Name] = header;
                }

                //Read messages count
                var messageCount = _reader.ReadUInt16();

                for (var i = 0; i < messageCount; i++)
                {
                    _deserializer.ClearReferences();

                    var message = ReadMessage();
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
                return (AmfVersion)_reader.ReadUInt16();
            }
            catch
            {
                throw new FormatException("Unable to read AMF version. Data has unknown format.");
            }
        }

        /// <summary>
        /// Read an AMF header.
        /// </summary>
        /// <remarks>
        /// Reader's current position must be set on header's 0 position.
        /// </remarks>
        private AmfHeader ReadHeader()
        {
            var header = new AmfHeader();
            header.Name = (string)_deserializer.ReadValue(Amf0TypeMarker.String);
            header.MustUnderstand = (bool)_deserializer.ReadValue(Amf0TypeMarker.Boolean);

            //Value contains header's length
            _reader.ReadInt32();

            header.Data = _deserializer.ReadValue();

            return header;
        }

        /// <summary>
        /// Read an AMF message.
        /// </summary>
        /// <remarks>
        /// Reader's current position must be set on message's 0 position.
        /// </remarks>
        private AmfMessage ReadMessage()
        {
            var message = new AmfMessage();
            message.Target = (string)_deserializer.ReadValue(Amf0TypeMarker.String);
            message.Response = (string)_deserializer.ReadValue(Amf0TypeMarker.String);

            //Value contains message's length
            _reader.ReadInt32();

            message.Data = _deserializer.ReadValue();

            return message;
        }

        /// <summary>
        /// Create AMF deserializer.
        /// </summary>
        /// <param name="version">AMF version.</param>
        private Amf0Deserializer CreateDeserializer(AmfVersion version)
        {
            var context = new AmfSerializationContext
                              {
                                  AmfVersion = version
                              };

            switch (version)
            {
                case AmfVersion.Amf0:
                    return new Amf0Deserializer(_reader, context);

                //By default, all AMF packets are encoded in AMF0.
                //A type marker 0x11 is used to temporary switch
                //to AMF3 context for the next encoded value.
                case AmfVersion.Amf3:
                    context.AmfVersion = AmfVersion.Amf0;
                    return new Amf3Deserializer(_reader, context);

                default:
                    throw new NotSupportedException("Deserializer for AMF type '" + version + "' is not implemented.");
            }
        }

        /// <summary>
        /// Context switch event handler.
        /// </summary>
        private void OnContextSwitch(object sender, ContextSwitchEventArgs e)
        {
            _deserializer.ClearReferences();
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

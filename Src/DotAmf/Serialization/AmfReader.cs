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
        #endregion

        #region Public methods
        /// <summary>
        /// Read an AMF packet.
        /// </summary>
        /// <remarks>
        /// Type declaration:
        /// <c>amf-packet = version header-count *(header-type) message-count *(message-type)</c>
        /// </remarks>
        /// <exception cref="FormatException">Data has invalid format.</exception>
        /// <exception cref="NotSupportedException">Unsupported AMF format.</exception>
        /// <exception cref="SerializationException">Error during deserialization.</exception>
        public AmfPacket Read()
        {
            var version = ReadAmfVersion();
            var packet = new AmfPacket(version);
            var deserializer = CreateDeserializer(version);

            try
            {
                //Read headers count
                var headerCount = deserializer.ReadHeaderCount();

                for (var i = 0; i < headerCount; i++)
                {
                    deserializer.ClearReferences();

                    var header = deserializer.ReadNextHeader();
                    packet.Headers.Add(header);
                }

                //Read messages count
                var messageCount = deserializer.ReadMessageCount();

                for (var i = 0; i < messageCount; i++)
                {
                    deserializer.ClearReferences();

                    var message = deserializer.ReadNextMessage();
                    packet.Messages.Add(message);
                }

                deserializer.ClearReferences();
            }
            catch (Exception e)
            {
                throw new SerializationException("Error during deserialization.", e);
            }

            return packet;
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
            //First two bytes contain message version number
            var versionMarker = _reader.ReadUInt16();

            switch (versionMarker)
            {
                case 0:
                    return AmfVersion.Amf0;

                case 3:
                    return AmfVersion.Amf3;

                default:
                    throw new FormatException("Data has unknown format.");
            }
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
        #endregion

        #region IDispose implementation
        public void Dispose()
        {
            _reader.Dispose();
        }
        #endregion
    }
}

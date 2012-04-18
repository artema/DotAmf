using System;
using System.IO;
using System.ServiceModel.Channels;
using System.Xml;
using DotAmf.Data;
using DotAmf.IO;
using DotAmf.Serialization;

namespace DotAmf.ServiceModel.Channels
{
    /// <summary>
    /// AMF message encoder/decoder.
    /// </summary>
    /// <remarks>The encoder is the component that is used to write 
    /// messages to a stream and to read messages from a stream.</remarks>
    sealed internal class AmfEncoder : MessageEncoder
    {
        #region .ctor
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="encodingOptions">AMF encoding options.</param>
        public AmfEncoder(AmfEncodingOptions encodingOptions)
        {
            //Construct the default content type string
            _contentType = string.Format(
                "{0}; charset={1}",
                AmfEncoderFactory.DefaultAmfMediaType,
                AmfEncoderFactory.DefaultAmfCharSet
            );

            _serializer = new DataContractAmfSerializer(typeof(AmfPacket), encodingOptions);
        }
        #endregion

        #region Constants
        /// <summary>
        /// Default web content type.
        /// </summary>
        private const string WebContentType = "text/html";
        #endregion

        #region Data
        /// <summary>
        /// The default content type.
        /// </summary>
        private readonly string _contentType;

        /// <summary>
        /// AMF packet serializer.
        /// </summary>
        private readonly DataContractAmfSerializer _serializer;
        #endregion

        #region Properties
        /// <summary>
        /// The content type that is supported by the message encoder.
        /// </summary>
        /// <remarks>By default, this one is <c>application/x-amf; charset=utf-8</c>.</remarks>
        public override string ContentType { get { return _contentType; } }

        /// <summary>
        /// The media type (MIME) that is supported by the message encoder.
        /// </summary>
        /// <remarks>By default, this one is <c>application/x-amf</c>.</remarks>
        public override string MediaType { get { return AmfEncoderFactory.DefaultAmfMediaType; } }

        /// <summary>
        /// The MessageVersion that is used by the encoder.
        /// </summary>
        /// <remarks>By default, this one is <c>MessageVersion.None</c> since we don't need any SOAP/WS-* support.</remarks>
        public override MessageVersion MessageVersion { get { return MessageVersion.None; } }
        #endregion

        #region Abstract methods implementation
        /// <summary>
        /// Returns a value that indicates whether a specified message-level content-type value is supported by the message encoder.
        /// </summary>
        /// <param name="contentType">The message-level content-type being tested.</param>
        /// <returns><c>true</c> if the message-level content-type specified is supported; otherwise <c>false</c>.</returns>
        public override bool IsContentTypeSupported(string contentType)
        {
            //A perfect match
            if (contentType == ContentType || contentType == WebContentType)
                return true;

            //Not a perfect match, but still OK
            if (MediaType != null && contentType.Length == MediaType.Length && contentType.Equals(MediaType, StringComparison.OrdinalIgnoreCase))
                return true;

            //Something like "application/x-amf; charset=KOI8-R" will be rejected,
            //but it is probably OK since AMF must be encoded in UTF-8.

            return base.IsContentTypeSupported(contentType);
        }

        /// <summary>
        /// Reads a message from a specified buffer.
        /// </summary>
        /// <param name="buffer">Buffer from which the message is deserialized.</param>
        /// <param name="bufferManager">BufferManager that manages the buffer from which the message is deserialized.</param>
        /// <param name="contentType">The Multipurpose Internet Mail Extensions (MIME) message-level content-type.</param>
        /// <returns></returns>
        public override Message ReadMessage(ArraySegment<byte> buffer, BufferManager bufferManager, string contentType)
        {
            //Just write the message to memory, then deserialize it
            var msgContents = new byte[buffer.Count];
            Array.Copy(buffer.Array, buffer.Offset, msgContents, 0, msgContents.Length);
            bufferManager.ReturnBuffer(buffer.Array);

            using (var stream = new MemoryStream(msgContents))
            {
                return ReadMessage(stream, int.MaxValue, contentType);
            }
        }

        /// <summary>
        /// Writes a message of less than a specified size to a byte array buffer at the specified offset.
        /// </summary>
        /// <param name="message">The Message to write to the message buffer.</param>
        /// <param name="maxMessageSize">The maximum message size that can be written.</param>
        /// <param name="bufferManager">The BufferManager that manages the buffer to which the message is written.</param>
        /// <param name="messageOffset">The offset of the segment that begins from the start of the byte array that provides the buffer.</param>
        /// <returns>The buffer to which the message is serialized.</returns>
        public override ArraySegment<byte> WriteMessage(Message message, int maxMessageSize, BufferManager bufferManager, int messageOffset)
        {
            byte[] messageBytes;
            int messageLength;

            using (var ms = new MemoryStream())
            {
                WriteMessage(message, ms);

                messageBytes = ms.ToArray();
                messageLength = messageBytes.Length;
            }

            var totalBytes = bufferManager.TakeBuffer(messageLength + messageOffset);
            Array.Copy(messageBytes, 0, totalBytes, messageOffset, messageLength);

            return new ArraySegment<byte>(totalBytes, messageOffset, messageLength);
        }
        #endregion

        #region Read/write
        /// <summary>
        /// Reads a message from a specified stream.
        /// </summary>
        /// <remarks>An actual deserialization is performed rigth here.</remarks>
        /// <param name="stream">Stream object from which the message is read.</param>
        /// <param name="maxSizeOfHeaders">The maximum size of the headers that can be read from the message.</param>
        /// <param name="contentType">The Multipurpose Internet Mail Extensions (MIME) message-level content-type.</param>
        /// <returns>The Message that is read from the stream specified.</returns>
        public override Message ReadMessage(Stream stream, int maxSizeOfHeaders, string contentType)
        {
            var ms = new MemoryStream();

            try
            {
                //Decode binary AMF packet into AMFX data
                var output = AmfxWriter.Create(ms);
                _serializer.ReadObject(stream, output);
                output.Flush();
                ms.Seek(0, SeekOrigin.Begin);
                
                //Write AMFX message
                return Message.CreateMessage(MessageVersion.None, null, AmfxReader.Create(ms, true));
            }
            catch
            {
                ms.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Writes a message to a specified stream.
        /// </summary>
        /// <remarks>An actual serialization is performed rigth here.</remarks>
        /// <param name="message">The Message to write to the stream.</param>
        /// <param name="stream">The Stream object to which the message is written.</param>
        public override void WriteMessage(Message message, Stream stream)
        {
            try
            {
                using(var ms = new MemoryStream())
                {
                    //Read AMFX message
                    var writer = XmlDictionaryWriter.CreateDictionaryWriter(AmfxWriter.Create(ms));
                    message.WriteBodyContents(writer);
                    writer.Flush();
                    ms.Seek(0, SeekOrigin.Begin);

                    //Encode binary AMF packet from AMFX data
                    _serializer.WriteObject(stream, AmfxReader.Create(ms));
                }
            }
            finally
            {
                message.Close();
            }
        }
        #endregion
    }
}

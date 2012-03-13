using DotAmf.Data;

namespace DotAmf.Serialization
{
    /// <summary>
    /// AMF deserializer.
    /// </summary>
    public interface IAmfDeserializer
    {
        /// <summary>
        /// Clear stored object references.
        /// </summary>
        void ClearReferences();

        /// <summary>
        /// Read next AMF header.
        /// </summary>
        AmfHeader ReadNextHeader();

        /// <summary>
        /// Read next AMF message.
        /// </summary>
        AmfMessage ReadNextMessage();

        /// <summary>
        /// Read header count.
        /// </summary>
        ushort ReadHeaderCount();

        /// <summary>
        /// Read message count.
        /// </summary>
        ushort ReadMessageCount();
    }
}

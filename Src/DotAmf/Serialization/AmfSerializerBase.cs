using System;
using System.IO;

namespace DotAmf.Serialization
{
    /// <summary>
    /// Abstract AMF serializer.
    /// </summary>
    abstract public class AmfSerializerBase : IAmfSerializer
    {
        #region .ctor
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="writer">AMF stream writer.</param>
        protected AmfSerializerBase(BinaryWriter writer)
        {
            if (writer == null) throw new ArgumentNullException("writer");
            _writer = writer;
        }
        #endregion

        #region Data
        /// <summary>
        /// AMF stream writer.
        /// </summary>
        private readonly BinaryWriter _writer;
        #endregion

        #region Properties
        /// <summary>
        /// Stream writer.
        /// </summary>
        protected BinaryWriter Writer { get { return _writer; } }
        #endregion

        #region IAmfSerializer implementation
        public void ClearReferences()
        {
            throw new NotImplementedException();
        }

        public abstract int WriteValue(object value);
        #endregion
    }
}

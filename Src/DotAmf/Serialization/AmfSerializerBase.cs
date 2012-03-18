using System;
using System.Collections.Generic;
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

            _references = new List<object>();
        }
        #endregion

        #region Data
        /// <summary>
        /// AMF stream writer.
        /// </summary>
        private readonly BinaryWriter _writer;

        /// <summary>
        /// References.
        /// </summary>
        private readonly IList<object> _references;
        #endregion

        #region Properties
        /// <summary>
        /// Stream writer.
        /// </summary>
        protected BinaryWriter Writer { get { return _writer; } }

        /// <summary>
        /// References.
        /// </summary>
        protected IList<object> References { get { return _references; } }
        #endregion

        #region IAmfSerializer implementation
        public virtual void ClearReferences()
        {
            _references.Clear();
        }

        public abstract int WriteValue(object value);
        #endregion

        #region Protected methods
        /// <summary>
        /// Save object to a list of object references.
        /// </summary>
        /// <param name="value">Object to save or <c>null</c></param>
        protected void SaveReference(object value)
        {
            References.Add(value);
        }
        #endregion
    }
}

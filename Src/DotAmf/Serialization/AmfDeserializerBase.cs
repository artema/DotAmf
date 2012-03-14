using System;
using System.Collections.Generic;
using DotAmf.Data;
using DotAmf.IO;

namespace DotAmf.Serialization
{
    /// <summary>
    /// Abstract AMF deserializer.
    /// </summary>
    abstract public class AmfDeserializerBase : IAmfDeserializer
    {
        #region .ctor
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="reader">AMF stream reader.</param>
        /// <param name="references">Object references to use.</param>
        protected AmfDeserializerBase(AmfStreamReader reader, IList<object> references)
        {
            if (reader == null) throw new ArgumentNullException("reader");
            _reader = reader;

            _references = references ?? new List<object>();
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="reader">AMF stream reader.</param>
        protected AmfDeserializerBase(AmfStreamReader reader)
            : this(reader, null)
        {
        }
        #endregion

        #region Data
        /// <summary>
        /// AMF stream reader.
        /// </summary>
        private readonly AmfStreamReader _reader;

        /// <summary>
        /// References.
        /// </summary>
        private readonly IList<object> _references;
        #endregion

        #region Properties
        /// <summary>
        /// Stream reader.
        /// </summary>
        protected AmfStreamReader Reader { get { return _reader; } }

        /// <summary>
        /// References.
        /// </summary>
        protected IList<object> References { get { return _references; } }
        #endregion

        #region IAmfDeserializer implementation
        public event ContextSwitch ContextSwitch;

        public abstract AmfHeader ReadHeader();

        public abstract AmfMessage ReadMessage();

        public abstract object ReadValue();

        public virtual void ClearReferences()
        {
            _references.Clear();
        }

        public IAmfDeserializer Context { set; protected get; }
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

        /// <summary>
        /// Switch AMF context.
        /// </summary>
        protected void SwitchContext(AmfVersion contextVersion)
        {
            OnContextSwitchEvent(new ContextSwitchEventArgs(contextVersion, References));
        }
        #endregion

        #region Event invokers
        private void OnContextSwitchEvent(ContextSwitchEventArgs e)
        {
            if (ContextSwitch != null) ContextSwitch(this, e);
        }
        #endregion
    }
}

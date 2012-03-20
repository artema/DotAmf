using System;
using System.Collections.Generic;
using System.IO;
using DotAmf.Data;

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
        /// <param name="context">AMF serialization context.</param>
        protected AmfDeserializerBase(BinaryReader reader, AmfSerializationContext context)
        {
            if (reader == null) throw new ArgumentNullException("reader");
            _reader = reader;

            _context = context;

            _references = new List<object>();
        }
        #endregion

        #region Data
        /// <summary>
        /// AMF stream reader.
        /// </summary>
        private readonly BinaryReader _reader;

        /// <summary>
        /// References.
        /// </summary>
        private readonly IList<object> _references;

        /// <summary>
        /// AMF serialization context.
        /// </summary>
        private AmfSerializationContext _context;
        #endregion

        #region Properties
        /// <summary>
        /// Stream reader.
        /// </summary>
        protected BinaryReader Reader { get { return _reader; } }

        /// <summary>
        /// References.
        /// </summary>
        protected IList<object> References { get { return _references; } }
        #endregion

        #region IAmfDeserializer implementation
        public event ContextSwitch ContextSwitch;

        public abstract object ReadValue();

        public virtual void ClearReferences()
        {
            _references.Clear();
        }

        public AmfSerializationContext Context { get { return _context; } }
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
        /// Gets or sets current AMF version.
        /// </summary>
        protected AmfVersion CurrentAmfVersion
        {
            get { return _context.AmfVersion; }
            set
            {
                if(_context.AmfVersion == value) return;
                
                _context.AmfVersion = value;
                OnContextSwitchEvent(new ContextSwitchEventArgs(_context));
            }
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

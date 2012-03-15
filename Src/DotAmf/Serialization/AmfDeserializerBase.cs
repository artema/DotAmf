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
        /// <param name="initialContext">Initial AMF initialContext.</param>
        protected AmfDeserializerBase(AmfStreamReader reader, AmfVersion initialContext)
        {
            if (reader == null) throw new ArgumentNullException("reader");
            _reader = reader;

            _context = initialContext;

            _references = new List<object>();
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

        /// <summary>
        /// Current AMF context.
        /// </summary>
        private AmfVersion _context;
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

        public AmfVersion Context
        {
            protected set
            {
                if (value != _context)
                {
                    _context = value;
                    OnContextSwitchEvent(new ContextSwitchEventArgs(value));
                }
                else
                    _context = value;
            }
            get { return _context; }
        }
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

        #region Event invokers
        private void OnContextSwitchEvent(ContextSwitchEventArgs e)
        {
            if (ContextSwitch != null) ContextSwitch(this, e);
        }
        #endregion
    }
}

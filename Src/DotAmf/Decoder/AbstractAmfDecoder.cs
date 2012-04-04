using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using DotAmf.Data;

namespace DotAmf.Decoder
{
    /// <summary>
    /// Abstract AMF decoder.
    /// </summary>
    abstract internal class AbstractAmfDecoder : IAmfDecoder
    {
        #region .ctor
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="reader">AMF stream reader.</param>
        /// <param name="options">AMF serialization options.</param>
        protected AbstractAmfDecoder(BinaryReader reader, AmfEncodingOptions options)
        {
            if (reader == null) throw new ArgumentNullException("reader");
            _reader = reader;

            _options = options;

            //In mixed context enviroinments, 
            //AMF0 is always used by default
            _currentAmfVersion = _options.UseContextSwitch 
                ? AmfVersion.Amf0
                : _options.AmfVersion;

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
        /// AMF serialization options.
        /// </summary>
        private readonly AmfEncodingOptions _options;

        /// <summary>
        /// Current AMF version.
        /// </summary>
        private AmfVersion _currentAmfVersion;
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

        /// <summary>
        /// Gets or sets current AMF version.
        /// </summary>
        protected AmfVersion CurrentAmfVersion
        {
            get { return _currentAmfVersion; }
            set
            {
                if (_currentAmfVersion == value) return;
                _currentAmfVersion = value;

                #if DEBUG
                Debug.WriteLine(string.Format(Errors.AbstractAmfDecoder_CurrentAmfVersion_Debug, _currentAmfVersion));
                #endif

                OnContextSwitchEvent(new EncodingContextSwitchEventArgs(_currentAmfVersion));
            }
        }
        #endregion

        #region IAmfDeserializer implementation
        public abstract IEnumerable<AmfHeader> ReadPacketHeaders();

        public abstract IEnumerable<AmfMessage> ReadPacketMessages();

        public abstract object ReadValue();

        public virtual void ClearReferences()
        {
            References.Clear();
        }

        public event EncodingContextSwitch ContextSwitch;
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
        private void OnContextSwitchEvent(EncodingContextSwitchEventArgs e)
        {
            if (ContextSwitch != null) ContextSwitch(this, e);
        }
        #endregion
    }
}

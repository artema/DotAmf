using System;
using System.Collections.Generic;
using System.IO;
using DotAmf.Data;

namespace DotAmf.Encoder
{
    /// <summary>
    /// Abstract AMF encoder.
    /// </summary>
    abstract internal class AbstractAmfEncoder : IAmfEncoder
    {
        #region .ctor
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="writer">AMF stream writer.</param>
        /// <param name="options">AMF encoding options.</param>
        protected AbstractAmfEncoder(BinaryWriter writer, AmfEncodingOptions options)
        {
            if (writer == null) throw new ArgumentNullException("writer");
            _writer = writer;

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
        /// AMF stream writer.
        /// </summary>
        private readonly BinaryWriter _writer;

        /// <summary>
        /// Object references.
        /// </summary>
        private readonly List<object> _references;

        /// <summary>
        /// AMF encoding options.
        /// </summary>
        private readonly AmfEncodingOptions _options;

        /// <summary>
        /// Current AMF version.
        /// </summary>
        private AmfVersion _currentAmfVersion;
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

                ClearReferences();
            }
        }
        #endregion

        #region IAmfSerializer implementation
        public virtual void ClearReferences()
        {
            References.Clear();
        }

        public abstract void WritePacketHeader(AmfHeader header);

        public abstract void WritePacketBody(AmfMessage message);

        public abstract void WriteValue(object value);
        #endregion

        #region Protected methods
        /// <summary>
        /// Save an object to a list of object references.
        /// </summary>
        /// <param name="item">Object to save.</param>
        /// <returns><c>null</c> if the item was added to the reference list,
        /// or a position in reference list if the item has already been added.</returns>
        protected int? SaveReference(object item)
        {
            var index = References.IndexOf(item);
            if (index != -1) return index;

            References.Add(item);
            return null;
        }
        #endregion
        
        #region Helper methods
        /// <summary>
        /// Convert a <c>DateTime</c> to a UNIX timestamp in milliseconds.
        /// </summary>
        protected static double ConvertToTimestamp(DateTime value)
        {
            var origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);

            if (value.Kind != DateTimeKind.Utc)
                origin = origin.ToLocalTime();

            return (value - origin).TotalSeconds * 1000;
        }
        #endregion
    }
}

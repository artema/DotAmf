using System;
using System.Collections.Generic;
using System.IO;
using DotAmf.Data;

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
        /// <param name="context">AMF serialization context.</param>
        protected AmfSerializerBase(BinaryWriter writer, AmfSerializationContext context)
        {
            if (writer == null) throw new ArgumentNullException("writer");
            _writer = writer;

            _context = context;

            _objectReferences = new List<object>();
        }
        #endregion

        #region Data
        /// <summary>
        /// AMF stream writer.
        /// </summary>
        private readonly BinaryWriter _writer;

        /// <summary>
        /// AMF serialization context.
        /// </summary>
        private AmfSerializationContext _context;

        /// <summary>
        /// Objects references.
        /// </summary>
        private readonly List<object> _objectReferences;
        #endregion

        #region Properties
        /// <summary>
        /// Stream writer.
        /// </summary>
        protected BinaryWriter Writer { get { return _writer; } }
        #endregion

        #region IAmfSerializer implementation
        public event ContextSwitch ContextSwitch;

        public virtual void ClearReferences()
        {
            _objectReferences.Clear();
        }

        public AmfSerializationContext Context { get { return _context; } }

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
            var index = _objectReferences.IndexOf(item);
            if (index != -1) return index;

            _objectReferences.Add(item);
            return null;
        }

        /// <summary>
        /// Gets or sets current AMF version.
        /// </summary>
        protected AmfVersion CurrentAmfVersion
        {
            get { return _context.AmfVersion; }
            set
            {
                if (_context.AmfVersion == value) return;

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
        
        #region Helper methods
        /// <summary>
        /// Check if type is a numeric type.
        /// </summary>
        protected static bool IsNumericType(Type type, out bool isInteger)
        {
            isInteger = false;

            if (type == null) return false;

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    {
                        isInteger = true;
                        return true;
                    }

                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    {
                        return true;
                    }

                case TypeCode.Object:
                    {
                        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                            return IsNumericType(Nullable.GetUnderlyingType(type), out isInteger);

                        return false;
                    }
            }

            return false;
        }

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

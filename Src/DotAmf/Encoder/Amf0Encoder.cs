using System;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using DotAmf.Data;

namespace DotAmf.Encoder
{
    /// <summary>
    /// AMF0 encoder.
    /// </summary>
    internal class Amf0Encoder : AbstractAmfEncoder
    {
        #region .ctor
        public Amf0Encoder(BinaryWriter writer, AmfEncodingOptions options)
            : base(writer, options)
        {
        }
        #endregion

        #region Constants
        /// <summary>
        /// Maximum number of byte a short string can contain.
        /// </summary>
        private const uint ShortStringLimit = 65535;

        /// <summary>
        /// <c>Null</c> value.
        /// </summary>
        private const string Null = "null";
        #endregion

        #region IAmfSerializer implementation
        public override void WriteValue(object value)
        {
            //A null value
            if (value == null)
            {
                WriteNull();
                return;
            }

            var type = value.GetType();

            //A primitive value
            if (type.IsValueType || type.IsEnum || type == typeof(string))
            {
                WritePrimitive(value);
                return;
            }

            //ToDo: write object

            throw new SerializationException("Invalid type: " + type.FullName);
        }

        public override void WritePacketHeader(AmfHeader header)
        {
            WriteUtf8(header.Name);
            Writer.Write((byte)(header.MustUnderstand ? 0 : 1));
            Writer.Write(-1); //Header's length

            WriteValue(header.Data);
        }

        public override void WritePacketBody(AmfMessage message)
        {
            WriteUtf8(message.Target ?? Null);
            WriteUtf8(message.Response ?? Null);
            Writer.Write(-1); //Message's length

            WriteValue(message.Data);
        }
        #endregion

        #region Special values
        /// <summary>
        /// Write a <c>null</c>.
        /// </summary>
        private void WriteNull()
        {
            Write(Amf0TypeMarker.Null);
        }

        /// <summary>
        /// Write a primitive value.
        /// </summary>
        private void WritePrimitive(object value)
        {
            var type = value.GetType();

            //A boolean value
            if (type == typeof(bool))
            {
                Write((bool)value);
                return;
            }

            //A string
            if (type == typeof(string))
            {
                Write((string)value);
                return;
            }

            //A date/time value
            if (type == typeof(DateTime))
            {
                Write((DateTime)value);
                return;
            }

            throw new SerializationException("Invalid type: " + type.FullName);
        }
        #endregion

        #region Serialization methods
        /// <summary>
        /// Write an AMF0 type marker.
        /// </summary>
        public void Write(Amf0TypeMarker marker)
        {
            Writer.Write((byte)marker);
        }

        /// <summary>
        /// Write a boolean value.
        /// </summary>
        private void Write(bool value)
        {
            Writer.Write(value);
        }

        /// <summary>
        /// Write a <c>DateTime</c>.
        /// </summary>
        private void Write(DateTime value)
        {
            Write(Amf0TypeMarker.Date);

            Writer.Write(ConvertToTimestamp(value));

            //Value indicates a timezone, but it should not be used
            Writer.Write((short)0);
        }

        /// <summary>
        /// Write a string.
        /// </summary>
        private void Write(string value)
        {
            if (value == null) value = string.Empty;

            var data = Encoding.UTF8.GetBytes(value);

            if(data.Length < ShortStringLimit)
            {
                Write(Amf0TypeMarker.String);
                Writer.Write((ushort)data.Length);
            }
            else
            {
                Write(Amf0TypeMarker.LongString);
                Writer.Write((uint)data.Length);
            }

            Writer.Write(data);
        }

        /// <summary>
        /// Write a string.
        /// </summary>
        private void WriteUtf8(string value)
        {
            if (value == null) value = string.Empty;

            var data = Encoding.UTF8.GetBytes(value);

            Writer.Write((ushort)data.Length);
            Writer.Write(data);
        }

        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml;
using System.Text;
using DotAmf.Data;
using DotAmf.IO;

namespace DotAmf.Serialization
{
    /// <summary>
    /// AMF0 deserializer.
    /// </summary>
    public class Amf0Deserializer : IAmfDeserializer
    {
        #region .ctor
        public Amf0Deserializer(AmfStreamReader reader)
        {
            if (reader == null) throw new ArgumentNullException("reader");
            _reader = reader;

            _references = new List<object>();
        }
        #endregion

        #region Data
        /// <summary>
        /// AMF stream reader.
        /// </summary>
        private readonly AmfStreamReader _reader;

        /// <summary>
        /// Object references.
        /// </summary>
        private readonly IList<object> _references;
        #endregion

        #region Properties
        /// <summary>
        /// AMF stream reader.
        /// </summary>
        protected AmfStreamReader Reader { get { return _reader; } }

        /// <summary>
        /// Object references.
        /// </summary>
        protected IList<object> References { get { return _references; } }
        #endregion

        #region IAmfDeserializer implementation
        public virtual void ClearReferences()
        {
            _references.Clear();
        }

        public virtual AmfHeader ReadNextHeader()
        {
            var header = new AmfHeader();
            header.Name = ReadString();
            header.MustUnderstand = Reader.ReadBoolean();

            //Value contains header's length
            Reader.ReadInt32();

            header.Data = ReadNextValue();

            return header;
        }

        public virtual AmfMessage ReadNextMessage()
        {
            var message = new AmfMessage();
            message.Target = ReadString();
            message.Response = ReadString();

            //Value contains message's length
            Reader.ReadInt32();

            message.Data = ReadNextValue();

            return message;
        }

        public virtual ushort ReadHeaderCount()
        {
            //Up to 65535 headers are possible
            return Reader.ReadUInt16();
        }

        public virtual ushort ReadMessageCount()
        {
            //Up to 65535 messages are possible
            return Reader.ReadUInt16();
        }
        #endregion

        #region Deserialization methods
        /// <summary>
        /// Read next value.
        /// </summary>
        /// <remarks>
        /// Current reader position must be on a value type marker.
        /// </remarks>
        /// <exception cref="NotSupportedException">AMF type is not supported.</exception>
        /// <exception cref="ArgumentException">Unknown type.</exception>
        /// <exception cref="SerializationException">Error during deserialization.</exception>
        protected virtual object ReadNextValue()
        {
            Amf0TypeMarker type;

            try
            {
                //Read a type marker byte
                type = (Amf0TypeMarker)Reader.ReadByte();
            }
            catch (Exception e)
            {
                throw new FormatException("Value type marker not found.", e);
            }

            return ReadValue(type);
        }

        /// <summary>
        /// Read a value of a given type from current position.
        /// </summary>
        /// <remarks>
        /// Current reader position must be just after a value type marker of a type to read.
        /// </remarks>
        /// <param name="type">Type of the value to read.</param>
        /// <exception cref="NotSupportedException">AMF type is not supported.</exception>
        /// <exception cref="ArgumentException">Unknown type.</exception>
        /// <exception cref="SerializationException">Error during deserialization.</exception>
        private object ReadValue(Amf0TypeMarker type)
        {
            switch (type)
            {
                case Amf0TypeMarker.Null:
                case Amf0TypeMarker.Undefined:
                    return null;

                case Amf0TypeMarker.Boolean:
                    return Reader.ReadBoolean();

                case Amf0TypeMarker.Number:
                    return Reader.ReadDouble();

                case Amf0TypeMarker.String:
                    return ReadString();

                case Amf0TypeMarker.LongString:
                    return ReadLongString();

                case Amf0TypeMarker.Date:
                    return ReadDate();

                case Amf0TypeMarker.XmlDocument:
                    return ReadXml();

                case Amf0TypeMarker.Reference:
                    return ReadReference();

                case Amf0TypeMarker.Object:
                    return ReadObject();

                case Amf0TypeMarker.TypedObject:
                    return ReadTypedObject();

                case Amf0TypeMarker.EcmaArray:
                    return ReadEcmaArray();

                case Amf0TypeMarker.StrictArray:
                    return ReadStrictArray();

                case Amf0TypeMarker.MovieClip:
                case Amf0TypeMarker.RecordSet:
                case Amf0TypeMarker.Unsupported:
                    throw new NotSupportedException("Type '" + type + "' is not supported.");

                case Amf0TypeMarker.AvmPlusObject:
                    throw new NotSupportedException("AVM+ types are not supported in this deserializer.");

                default:
                    throw new ArgumentException("type");
            }
        }

        /// <summary>
        /// Save object to a list of references.
        /// </summary>
        /// <param name="value">Object to save or <c>null</c></param>
        protected void SaveReference(object value)
        {
            References.Add(value);
        }

        /// <summary>
        /// Read an object reference.
        /// </summary>
        /// <remarks>
        /// Type declaration:
        /// <c>reference-type = reference-marker U16</c>
        /// </remarks>
        private object ReadReference()
        {
            var index = Reader.ReadUInt16();

            if (References.Count < index)
                throw new SerializationException("Referenced object #'" + index + "' not found.");

            return References[index];
        }

        /// <summary>
        /// Read a string.
        /// </summary>
        /// <remarks>
        /// Type declaration:
        /// <c>string-type = string-marker UTF-8</c>
        /// </remarks>
        private string ReadString()
        {
            //First 16 bits represents string's (UTF-8) length in bytes
            var length = Reader.ReadUInt16();
            return ReadString(length);
        }

        /// <summary>
        /// Read a long string.
        /// </summary>
        /// <remarks>
        /// Type declaration:
        /// <c>long-string-type = long-string-marker UTF-8-long</c>
        /// </remarks>
        private string ReadLongString()
        {
            //First 32 bits represents long string's (UTF-8-long) length in bytes
            var length = Reader.ReadInt32();
            return ReadString(length);
        }

        /// <summary>
        /// Read a specified number of bytes of a string.
        /// </summary>
        /// <param name="length">Number of bytes to read.</param>
        private string ReadString(int length)
        {
            if (length < 0) throw new ArgumentException("Length cannot be negative.", "length");

            //Make sure that a null is never returned
            if (length == 0) return string.Empty;

            var data = Reader.ReadBytes(length);

            //All strings are encoded in UTF-8
            return new UTF8Encoding().GetString(data);
        }

        /// <summary>
        /// Read an XML document.
        /// </summary>
        /// <remarks>
        /// Type declaration:
        /// <c>xml-document-type = xml-document-marker UTF-8-long</c>
        /// </remarks>
        private XmlDocument ReadXml()
        {
            try
            {
                //XML is stored as a long string
                var data = ReadLongString();

                var xml = new XmlDocument();
                xml.LoadXml(data);

                return xml;
            }
            catch (Exception e)
            {
                throw new SerializationException("Error during XML deserialization.", e);
            }
        }

        /// <summary>
        /// Read a date.
        /// </summary>
        /// <remarks>
        /// Type declaration:
        /// <c>time-zone = S16
        /// date-type = date-marker DOUBLE time-zone</c>
        /// </remarks>
        private DateTime ReadDate()
        {
            //Dates are represented as an Unix time stamp, but in milliseconds
            var milliseconds = Reader.ReadDouble();

            //Value indicates a timezone, but it should not be used
            Reader.ReadInt16();

            var origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

            return origin.AddMilliseconds(milliseconds);
        }

        /// <summary>
        /// Read object properties map.
        /// </summary>
        /// <remarks>
        /// Type declaration:
        /// <c>object-property = (UTF-8 value-type) | (UTF-8-empty object-end-marker)</c>
        /// </remarks>
        /// <exception cref="SerializationException"></exception>
        private Dictionary<string, object> ReadPropertiesMap()
        {
            try
            {
                var result = new Dictionary<string, object>();
                var property = ReadString(); //Read first property's name

                //An empty property name indicates that object's declaration ends here
                while (property != string.Empty)
                {
                    result[property] = ReadNextValue();

                    property = ReadString();
                }

                //Last byte is always an "ObjectEnd" marker
                var marker = (Amf0TypeMarker)Reader.ReadByte();

                //Something goes wrong
                if (marker != Amf0TypeMarker.ObjectEnd)
                    throw new FormatException("Unexpected object end.");

                return result;
            }
            catch (Exception e)
            {
                throw new SerializationException("Unable to deserialize properties map.", e);
            }
        }

        /// <summary>
        /// Read an anonymous object.
        /// </summary>
        /// <remarks>
        /// Type declaration:
        /// <c>object-property = (UTF-8 value-type) | (UTF-8-empty object-end-marker)
        /// anonymous-object-type = object-marker *(object-property)</c>
        /// </remarks>
        private AnonymousObject ReadObject()
        {
            var result = new AnonymousObject();
            SaveReference(result); //Save reference to this object

            var properties = ReadPropertiesMap();
            result.ReplaceProperties(properties);

            return result;
        }

        /// <summary>
        /// Read a strongly-typed object.
        /// </summary>
        /// <remarks>
        /// Type declaration:
        /// <c>class-name = UTF-8
        /// object-type = object-marker class-name *(object-property)</c>
        /// </remarks>
        private TypedObject ReadTypedObject()
        {
            TypedObject result;

            try
            {
                var className = ReadString();
                result = new TypedObject(className);
            }
            finally
            {
                //Make sure that the reference map will not get broken
                SaveReference(null);
            }

            SaveReference(result); //Save reference to this object

            var properties = ReadPropertiesMap();
            result.ReplaceProperties(properties);

            return result;
        }

        /// <summary>
        /// Read an ECMA array.
        /// </summary>
        /// <remarks>
        /// Type declaration:
        /// <c>associative-count = U32
        /// ecma-array-type = associative-count *(object-property)</c>
        /// </remarks>
        private AnonymousObject ReadEcmaArray()
        {
            var result = new AnonymousObject();
            SaveReference(result); //Save reference to this object

            var length = Reader.ReadUInt32();
            var properties = ReadPropertiesMap();

            if (properties.Count != length)
                throw new SerializationException("Invalid array length.");

            result.ReplaceProperties(properties);

            return result;
        }

        /// <summary>
        /// Read a strict array.
        /// </summary>
        /// <remarks>
        /// Type declaration:
        /// <c>array-count = U32
        /// strict-array-type = array-count *(value-type)</c>
        /// </remarks>
        private StrictArray ReadStrictArray()
        {
            var result = new StrictArray();
            SaveReference(result); //Save reference to this object

            var length = Reader.ReadUInt32();
            Exception error = null;

            for (var i = 0; i < length; i++)
            {
                try
                {
                    var value = ReadNextValue();
                    result.Add(value);
                }
                catch(Exception e)
                {
                    //We can't afford any errors here since it will break our reference map
                    error = e;
                    continue;
                }
            }

            if (error != null)
            {
                //Indicate a serialization error
                throw new SerializationException("Error during array deserialization.", error);
            }

            return result;
        }
        #endregion
    }
}

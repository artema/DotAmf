using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;
using System.Text;
using DotAmf.Data;

namespace DotAmf.Serialization
{
    /// <summary>
    /// AMF0 deserializer.
    /// </summary>
    public class Amf0Deserializer : AmfDeserializerBase
    {
        #region .ctor
        public Amf0Deserializer(BinaryReader reader, AmfSerializationContext context)
            : base(reader, context)
        {
        }
        #endregion

        #region IAmfDeserializer implementation
        public override object ReadValue()
        {
            if(CurrentAmfVersion != AmfVersion.Amf0)
                throw new InvalidOperationException("Invalid AMF version: " + CurrentAmfVersion);

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
        #endregion

        #region Deserialization methods
        /// <summary>
        /// Read a value of a given type from current reader's position.
        /// </summary>
        /// <remarks>
        /// Current reader position must be just after a value type marker of a type to read.
        /// </remarks>
        /// <param name="type">Type of the value to read.</param>
        /// <exception cref="NotSupportedException">AMF type is not supported.</exception>
        /// <exception cref="FormatException">Unknown data format.</exception>
        /// <exception cref="SerializationException">Error during deserialization.</exception>
        public object ReadValue(Amf0TypeMarker type)
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
                    return ReadAmvPlusValue();

                default:
                    throw new ArgumentException("type");
            }
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
            var length = Reader.ReadUInt32();
            return ReadString(length);
        }

        /// <summary>
        /// Read a specified number of bytes of a string.
        /// </summary>
        /// <param name="length">Number of bytes to read.</param>
        private string ReadString(uint length)
        {
            //Make sure that a null is never returned
            if (length == 0) return string.Empty;

            var data = Reader.ReadBytes((int)length);
            
            //All strings are encoded in UTF-8)
            return Encoding.UTF8.GetString(data);
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
        private IDictionary<string, object> ReadPropertiesMap()
        {
            try
            {
                var result = new Dictionary<string, object>();
                var property = ReadString(); //Read first property's name

                //An empty property name indicates that object's declaration ends here
                while (property != string.Empty)
                {
                    result[property] = ReadValue();

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
        private AmfObject ReadObject()
        {
            var result = new AmfObject();
            SaveReference(result); //Save reference to this object

            var properties = ReadPropertiesMap();

            foreach (var pair in properties)
                result[pair.Key] = pair.Value;

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
        private AmfObject ReadTypedObject()
        {
            AmfObject result;

            try
            {
                var typeName = ReadString();
                result = new AmfObject(typeName);
            }
            finally
            {
                //Make sure that the reference map will not get broken
                SaveReference(null);
            }

            SaveReference(result); //Save reference to this object

            var properties = ReadPropertiesMap();

            foreach (var pair in properties)
                result[pair.Key] = pair.Value;

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
        private AmfObject ReadEcmaArray()
        {
            var result = new AmfObject();
            SaveReference(result); //Save reference to this object

            var length = Reader.ReadUInt32();
            var properties = ReadPropertiesMap();

            if (properties.Count != length)
                throw new SerializationException("Invalid array length.");

            foreach (var pair in properties)
                result[pair.Key] = pair.Value;

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
        private object[] ReadStrictArray()
        {
            var length = Reader.ReadUInt32();

            var result = new object[length];
            SaveReference(result); //Save reference to this object

            Exception error = null;

            for (var i = 0; i < length; i++)
            {
                try
                {
                    var value = ReadValue();
                    result[i] = value;
                }
                catch (Exception e)
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

        /// <summary>
        /// Read AMV+ value.
        /// </summary>
        private object ReadAmvPlusValue()
        {
            //Set new AMF version only for the next value, then switch back
            var oldVersion = CurrentAmfVersion;
            CurrentAmfVersion = AmfVersion.Amf3;

            try
            {
                return ReadValue();
            }
            finally
            {
                CurrentAmfVersion = oldVersion;
            }
        }
        #endregion
    }
}

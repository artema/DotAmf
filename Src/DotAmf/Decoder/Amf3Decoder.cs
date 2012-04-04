using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using DotAmf.Data;

namespace DotAmf.Decoder
{
    /// <summary>
    /// AMF3 decoder.
    /// </summary>
    sealed internal class Amf3Decoder : Amf0Decoder
    {
        #region .ctor
        public Amf3Decoder(BinaryReader reader, AmfEncodingOptions options)
            : base(reader, options)
        {
            _stringReferences = new List<string>();
            _traitReferences = new List<AmfTypeTraits>();
        }
        #endregion

        #region Data
        /// <summary>
        /// Strings references.
        /// </summary>
        private readonly IList<string> _stringReferences;

        /// <summary>
        /// Object traits references.
        /// </summary>
        private readonly IList<AmfTypeTraits> _traitReferences;
        #endregion

        #region References
        /// <summary>
        /// Save string to a list of string references.
        /// </summary>
        /// <param name="value">String to save or <c>null</c></param>
        private void SaveReference(string value)
        {
            _stringReferences.Add(value);
        }

        /// <summary>
        /// Save object traits to a list of traits references.
        /// </summary>
        /// <param name="value">Traits to save or <c>null</c></param>
        private void SaveReference(AmfTypeTraits value)
        {
            _traitReferences.Add(value);
        }

        /// <summary>
        /// Read an object reference.
        /// </summary>
        /// <param name="value">Uint29 value received when trying to read a reference.</param>
        /// <returns>Referenced object or <c>null</c> if value does not contain a reference.</returns>
        /// <exception cref="SerializationException">Invalid object reference.</exception>
        private object ReadReference(out int value)
        {
            value = ReadUint29();

            if ((value & 0x1) == 0)
            {
                var index = value >> 1;

                try
                {
                    return References[index];
                }
                catch
                {
                    throw new SerializationException("Invalid object reference. No object found at position " + index);
                }
            }

            return null;
        }
        #endregion

        #region IAmfDecoder implementation
        override public void ClearReferences()
        {
            base.ClearReferences();

            _stringReferences.Clear();
            _traitReferences.Clear();
        }

        override public object ReadValue()
        {
            //Work in a legacy context
            if (CurrentAmfVersion == AmfVersion.Amf0)
                return base.ReadValue();

            Amf3TypeMarker type;

            try
            {
                //Read a type marker byte
                type = (Amf3TypeMarker)Reader.ReadByte();
            }
            catch (Exception e)
            {
                #if DEBUG
                Debug.WriteLine(string.Format(Errors.Amf3Decoder_ReadValue_InvalidMarker_, Reader.BaseStream.Position));
                #endif

                throw new FormatException(string.Format(Errors.Amf3Decoder_ReadValue_TypeMarkerNotFound, Reader.BaseStream.Position), e);
            }

            var value = ReadValue(type);

            #if DEBUG
            Debug.WriteLine(string.Format(Errors.Amf3Decoder_ReadValue_End, type, Reader.BaseStream.Position));
            #endif

            return value;
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
        public object ReadValue(Amf3TypeMarker type)
        {
            #if DEBUG
            Debug.WriteLine(string.Format(Errors.Amf3Decoder_ReadValue_Debug, type, Reader.BaseStream.Position));
            #endif

            switch (type)
            {
                case Amf3TypeMarker.Null:
                case Amf3TypeMarker.Undefined:
                    return null;

                case Amf3TypeMarker.False:
                    return false;

                case Amf3TypeMarker.True:
                    return true;

                case Amf3TypeMarker.Integer:
                    return ReadUint29();

                case Amf3TypeMarker.Double:
                    return Reader.ReadDouble();

                case Amf3TypeMarker.String:
                    return ReadString();

                case Amf3TypeMarker.Date:
                    return ReadDate();

                case Amf3TypeMarker.ByteArray:
                    return ReadByteArray();

                case Amf3TypeMarker.Xml:
                case Amf3TypeMarker.XmlDocument:
                    return ReadXml();

                case Amf3TypeMarker.Array:
                    return ReadArray();

                case Amf3TypeMarker.Object:
                    return ReadObject();

                default:
                    throw new NotSupportedException("Type '" + type + "' is not supported.");
            }
        }

        /// <summary>
        /// Read a 29-bit unsigned integer.
        /// </summary>
        /// <remarks>
        /// Up to 4 bytes are required to hold the value however the high bit 
        /// of the first 3 bytes are used as flags to determine 
        /// whether the next byte is part of the integer.
        /// <c>
        /// 0x00000000 - 0x0000007F : 0xxxxxxx
        /// 0x00000080 - 0x00003FFF : 1xxxxxxx 0xxxxxxx
        /// 0x00004000 - 0x001FFFFF : 1xxxxxxx 1xxxxxxx 0xxxxxxx
        /// 0x00200000 - 0x3FFFFFFF : 1xxxxxxx 1xxxxxxx 1xxxxxxx xxxxxxxx
        /// 0x40000000 - 0xFFFFFFFF : throw range exception
        /// </c>
        /// </remarks>
        private int ReadUint29()
        {
            const byte mask = 0x7F; //0111 1111
            var octet = Reader.ReadByte() & 0xFF;

            //0xxxxxxx
            if (octet < 128) return octet;

            var result = (octet & mask) << 7;
            octet = Reader.ReadByte() & 0xFF;

            //1xxxxxxx 0xxxxxxx
            if (octet < 128) return (result | octet);

            result = (result | (octet & mask)) << 7;
            octet = Reader.ReadByte() & 0xFF;

            //1xxxxxxx 1xxxxxxx 0xxxxxxx
            if (octet < 128) return (result | octet);

            result = (result | (octet & mask)) << 8;
            octet = Reader.ReadByte() & 0xFF;

            //1xxxxxxx 1xxxxxxx 1xxxxxxx xxxxxxxx
            return (result | octet);
        }

        /// <summary>
        /// Read a string.
        /// </summary>
        /// <remarks>
        /// Type declaration:
        /// <c>U29S-ref = U29 (The first (low) bit is a flag with value 0. The remaining 1 to 28
        /// significant bits are used to encode a string reference table index (an integer)).
        /// U29S-value = U29 (The first (low) bit is a flag with value 1. The remaining 1 to 28 significant 
        /// bits are used to encode the byte-length of the UTF-8 encoded representation of the string).
        /// UTF-8-empty = 0x01 (The UTF-8-vr empty string which is never sent by reference).
        /// UTF-8-vr = U29S-ref | (U29S-value *(UTF8-char))
        /// string-type = string-marker UTF-8-vr</c>
        /// </remarks>
        private string ReadString()
        {
            var reference = ReadUint29();

            //Read string by reference
            if ((reference & 0x1) == 0) return _stringReferences[(reference >> 1)];

            //Get string length
            var length = (reference >> 1);
            var str = ReadString(length);

            SaveReference(str);
            return str;
        }

        /// <summary>
        /// Read a specified number of bytes of a string.
        /// </summary>
        /// <param name="length">Number of bytes to read.</param>
        private string ReadString(int length)
        {
            if (length < 0) throw new ArgumentException(Errors.Amf3Deserializer_ReadString_NegativeLength, "length");

            //Make sure that a null is never returned
            if (length == 0) return string.Empty;

            var data = Reader.ReadBytes(length);

            //All strings are encoded in UTF-8
            return Encoding.UTF8.GetString(data);
        }

        /// <summary>
        /// Read a date.
        /// </summary>
        /// <remarks>
        /// Type declaration:
        /// <c>U29D-value = U29 (The first (low) bit is a flag with value 1.
        /// The remaining bits are not used).
        /// date-time = DOUBLE (A 64-bit integer value transported as a double).
        /// date-type = date-marker (U29O-ref | (U29D-value date-time))</c>
        /// </remarks>
        private DateTime ReadDate()
        {
            int reference;
            object cache;

            if ((cache = ReadReference(out reference)) != null) return (DateTime)cache;

            //Dates are represented as an Unix time stamp, but in milliseconds
            var milliseconds = Reader.ReadDouble();

            var origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            var date = origin.AddMilliseconds(milliseconds);

            SaveReference(date);
            return date;
        }

        /// <summary>
        /// Read a byte array.
        /// </summary>
        /// <remarks>
        /// Type declaration:
        /// <c>U29B-value = U29 (The first (low) bit is a flag with value 1.
        /// The remaining 1 to 28 significant bits are used to encode the
        /// byte-length of the ByteArray).
        /// bytearray-type = bytearray-marker (U29O-ref | U29B-value *(U8))</c>
        /// </remarks>
        private byte[] ReadByteArray()
        {
            int reference;
            object cache;

            if ((cache = ReadReference(out reference)) != null) return (byte[])cache;

            //Get array length
            var length = (reference >> 1);

            var data = length == 0 ? new byte[] { } : Reader.ReadBytes(length);

            SaveReference(data);
            return data;
        }

        /// <summary>
        /// Read an XML document.
        /// </summary>
        /// <remarks>
        /// Type declaration:
        /// <c>U29X-value = U29 (The first (low) bit is a flag with value 1.
        /// The remaining 1 to 28 significant bits are used to encode the byte-length
        /// of the UTF-8 encoded representation of the XML or XMLDocument). 
        /// xml-doc-type = xml-doc-marker (U29O-ref | (U29X-value *(UTF8-char)))</c>
        /// </remarks>
        private XmlDocument ReadXml()
        {
            int reference;
            object cache;

            if ((cache = ReadReference(out reference)) != null) return (XmlDocument)cache;

            //Get XML string length
            var length = (reference >> 1);
            var rawData = ReadString(length);

            var xml = new XmlDocument();

            try
            {
                xml.LoadXml(rawData);
            }
            catch (Exception e)
            {
                throw new SerializationException("Error during XML deserialization.", e);
            }

            SaveReference(xml);
            return xml;
        }

        /// <summary>
        /// Read an array.
        /// </summary>
        /// <remarks>
        /// Type declaration:
        /// <c>U29A-value = U29 (The first (low) bit is a flag with value 1.
        /// The remaining 1 to 28 significant bits are used to encode 
        /// the count of the dense portion of the Array).
        /// assoc-value = UTF-8-vr value-type
        /// array-type = array-marker (U29O-ref | 
        /// (U29A-value (UTF-8-empty | *(assoc-value) UTF-8-empty) *(value-type)))</c>
        /// </remarks>
        private object ReadArray()
        {
            int reference;
            object cache;

            if ((cache = ReadReference(out reference)) != null) return cache;

            var key = ReadString();

            //ECMA array
            if (key != string.Empty)
            {
                //Create a dynamic object
                var hashmap = new AmfObject
                                  {
                                      Properties = new Dictionary<string, object>(),
                                      Traits = new AmfTypeTraits { IsDynamic = true }
                                  };

                //Read associative values
                do
                {
                    hashmap.Properties[key] = ReadValue();
                    key = ReadString();
                } while (key != string.Empty);

                var length = reference >> 1;

                //Read array values
                for (var i = 0; i < length; i++)
                    hashmap.Properties[i.ToString()] = ReadValue();

                SaveReference(hashmap);
                return hashmap;
            }
            //Regular array
            else
            {
                var length = reference >> 1;
                var array = new object[length];

                //Read array values
                for (var i = 0; i < length; i++)
                    array[i] = ReadValue();

                SaveReference(array);
                return array;
            }
        }

        /// <summary>
        /// Read an object.
        /// </summary>
        private object ReadObject()
        {
            var reference = ReadUint29();
            AmfTypeTraits traits;

            //Get traits object by reference
            if ((reference & 0x3) == 1)
            {
                traits = _traitReferences[(reference >> 2)];
            }
            //Read object's traits
            else
            {
                var isExternalizable = ((reference & 0x4) == 4);
                var isDynamic = ((reference & 0x8) == 8);
                var typeName = ReadString();
                var count = (reference >> 4);
                var classMembers = new string[count];

                for (var i = 0; i < count; i++)
                    classMembers[i] = ReadString();

                traits = new AmfTypeTraits
                             {
                                 IsDynamic = isDynamic,
                                 IsExternalizable = isExternalizable,
                                 TypeName = typeName,

                                 //No property names are included for types
                                 //that are externizable or dynamic
                                 ClassMembers = isDynamic || isExternalizable
                                    ? new string[0]
                                    : classMembers
                             };

                SaveReference(traits);
            }

            #if DEBUG
            Debug.WriteLine(string.Format(Errors.Amf3Decoder_ReadObject_Debug_Name, traits.TypeName));

            if (traits.IsDynamic)
                Debug.WriteLine(Errors.Amf3Decoder_ReadObject_Debug_Dynamic);
            else if (traits.IsExternalizable)
                Debug.WriteLine(Errors.Amf3Decoder_ReadObject_Debug_Externizable);
            else
                Debug.WriteLine(string.Format(Errors.Amf3Decoder_ReadObject_Debug_Members, traits.ClassMembers.Length));
            #endif

            var content = new Dictionary<string, object>();

            #if DEBUG
            var memberPosition = 0;
            #endif

            //Read object's properties
            foreach (var classMember in traits.ClassMembers)
            {
                #if DEBUG
                Debug.WriteLine(string.Format(Errors.Amf3Decoder_ReadObject_Debug_ReadingField, memberPosition, classMember));
                memberPosition++;
                #endif

                content[classMember] = ReadValue();
            }

            //Read dynamic properties too
            if (traits.IsDynamic)
            {
                #if DEBUG
                Debug.WriteLine(Errors.Amf3Decoder_ReadObject_Debug_ReadingDynamic);
                #endif

                var key = ReadString();

                while (key != string.Empty)
                {
                    #if DEBUG
                    Debug.WriteLine(string.Format(Errors.Amf3Decoder_ReadObject_Debug_ReadingDynamicField, key));
                    #endif

                    var value = ReadValue();
                    content[key] = value;
                    key = ReadString();
                }

                #if DEBUG
                Debug.WriteLine(Errors.Amf3Decoder_ReadObject_Debug_DynamicEnd);
                #endif
            }

            var result = new AmfObject { Traits = traits, Properties = content };
            SaveReference(result);

            #if DEBUG
            Debug.WriteLine(Errors.Amf3Decoder_ReadObject_Debug_End);
            #endif

            return result;
        }
        #endregion
    }
}

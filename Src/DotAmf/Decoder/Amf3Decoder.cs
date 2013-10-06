// Copyright (c) 2012 Artem Abashev (http://abashev.me)
// All rights reserved.
// Licensed under the Microsoft Public License (Ms-PL)
// http://opensource.org/licenses/ms-pl.html

using System;
using System.Collections.Generic;
#if DEBUG
using System.Diagnostics;
#endif
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using DotAmf.Data;
using DotAmf.IO;

namespace DotAmf.Decoder
{
    /// <summary>
    /// AMF3 decoder.
    /// </summary>
    class Amf3Decoder : Amf0Decoder
    {
        #region .ctor
        public Amf3Decoder(AmfEncodingOptions options)
            : base(options)
        {
        }
        #endregion

        #region Constants
        /// <summary>
        /// The minimum value for an integer that will avoid
        /// promotion to an ActionScript's <c>Number</c> type.
        /// </summary>
        private const int MinInt29Value = -268435456;

        /// <summary>
        /// The maximum value for an integer that will avoid
        /// promotion to an ActionScript's <c>Number</c> type.
        /// </summary>
        private const int MaxInt29Value = 268435455;

        /// <summary>
        /// The minimum value for a double that should be handled.
        /// </summary>
        private const double MinDoublePrecision = 0.00000001;
        #endregion

        #region IAmfDecoder implementation
        override public void Decode(Stream stream, XmlWriter output)
        {
            var reader = new AmfStreamReader(stream);
            var context = CreateDefaultContext();
            ReadAmfValue(context, reader, output);
        }
        #endregion

        #region References
        /// <summary>
        /// Read an object reference.
        /// </summary>
        /// <param name="context">AMF decoding context.</param>
        /// <param name="reader">AMF reader.</param>
        /// <param name="index">Reference index.</param>
        /// <param name="reference">Reference value.</param>
        /// <returns>Referenced object or <c>null</c> if value does not contain a reference.</returns>
        /// <exception cref="SerializationException">Invalid reference.</exception>
        private static bool ReadReference(AmfContext context, AmfStreamReader reader, out int index, out int reference)
        {
            reference = ReadUint29(reader);

            //The first bit is a flag with value 0 to imply that this is not an instance but a reference
            if ((reference & 0x1) == 0)
            {
                //The remaining 1 to 28 significant bits are used to encode an object reference index
                index = reference >> 1;

                if (context.References.Count <= index)
                    throw new SerializationException("Invalid reference index: " + index);

                return true;
            }

            index = -1;
            return false;
        }

        /// <summary>
        /// Read a string reference.
        /// </summary>
        /// <param name="context">AMF decoding context.</param>
        /// <param name="reader">AMF reader.</param>
        /// <param name="index">Reference index.</param>
        /// <param name="reference">Reference value.</param>
        /// <returns>Referenced string or <c>null</c> if value does not contain a reference.</returns>
        /// <exception cref="SerializationException">Invalid reference.</exception>
        private static string ReadStringReference(AmfContext context, AmfStreamReader reader, out int index, out int reference)
        {
            reference = ReadUint29(reader);

            //The first bit is a flag with value 0
            if ((reference & 0x1) == 0)
            {
                //The remaining 1 to 28 significant bits are used to encode a string reference table index
                index = reference >> 1;

                if (context.StringReferences.Count <= index)
                    throw new SerializationException("Invalid reference index: " + index);

                return context.StringReferences[index];
            }

            index = -1;
            return null;
        }

        /// <summary>
        /// Read a traits reference.
        /// </summary>
        /// <param name="context">AMF decoding context.</param>
        /// <param name="reader">AMF reader.</param>
        /// <param name="index">Reference index.</param>
        /// <param name="reference">Reference value.</param>
        /// <returns>Referenced traits object or <c>null</c> if value does not contain a reference.</returns>
        /// <exception cref="SerializationException">Invalid reference.</exception>
        private static AmfTypeTraits ReadTraitsReference(AmfContext context, AmfStreamReader reader, out int index, out int reference)
        {
            reference = ReadUint29(reader);

            //The first bit is a flag with value 1. The second bit is a flag with value 0 to imply 
            //that this objects traits are being sent by reference
            if ((reference & 0x3) == 1) //x01 & x11 == x01
            {
                //The remaining 1 to 27 significant bits are used to encode a trait reference index
                index = reference >> 2;

                if (context.TraitsReferences.Count <= index)
                    throw new SerializationException("Invalid reference index: " + index);

                return context.TraitsReferences[index];
            }

            index = -1;
            return null;
        }
        #endregion

        #region Helper methods
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
        private static int ReadUint29(AmfStreamReader reader)
        {
            const byte mask = 0x7F; //0111 1111
            var octet = reader.ReadByte() & 0xFF;

            //0xxxxxxx
            if (octet < 128) return octet;

            var result = (octet & mask) << 7;
            octet = reader.ReadByte() & 0xFF;

            //1xxxxxxx 0xxxxxxx
            if (octet < 128) return (result | octet);

            result = (result | (octet & mask)) << 7;
            octet = reader.ReadByte() & 0xFF;

            //1xxxxxxx 1xxxxxxx 0xxxxxxx
            if (octet < 128) return (result | octet);

            result = (result | (octet & mask)) << 8;
            octet = reader.ReadByte() & 0xFF;

            //1xxxxxxx 1xxxxxxx 1xxxxxxx xxxxxxxx
            return (result | octet);
        }

        /// <summary>
        /// Read a specified number of bytes of a string.
        /// </summary>
        /// <param name="reader">AMF reader.</param>
        /// <param name="length">Number of bytes to read.</param>
        private static string ReadUtf8(AmfStreamReader reader, int length)
        {
            if (length < 0) throw new ArgumentException(Errors.Amf3Deserializer_ReadString_NegativeLength, "length");

            //Make sure that a null is never returned
            if (length == 0) return string.Empty;

            var data = reader.ReadBytes(length);

            //All strings are encoded in UTF-8
            return Encoding.UTF8.GetString(data);
        }

        /// <summary>
        /// Write an empty element of a given name.
        /// </summary>
        private static void WriteEmptyElement(string elementName, XmlWriter output)
        {
            if (output != null)
            {
                output.WriteStartElement(elementName);
                output.WriteEndElement();
            }
        }

        /// <summary>
        /// Write a reference.
        /// </summary>
        private static void WriteReference(int index, XmlWriter output)
        {
            if (output != null)
            {
                output.WriteStartElement(AmfxContent.Reference);
                output.WriteAttributeString(AmfxContent.ReferenceId, index.ToString());
                output.WriteEndElement();
            }
        }
        #endregion

        #region Deserialization methods
        protected override void ReadAmfValue(AmfContext context, AmfStreamReader reader, XmlWriter output = null)
        {
            //Work in a legacy context
            if (context.AmfVersion == AmfVersion.Amf0)
            {
                base.ReadAmfValue(context, reader, output);
                return;
            }

            Amf3TypeMarker dataType;

            try
            {
                //Read a type marker byte
                dataType = (Amf3TypeMarker)reader.ReadByte();
            }
            catch (Exception e)
            {
                #if DEBUG
                Debug.WriteLine(string.Format(Errors.Amf3Decoder_ReadValue_InvalidMarker, reader.BaseStream.Position));
                #endif

                throw new FormatException(string.Format(Errors.Amf3Decoder_ReadValue_TypeMarkerNotFound, reader.BaseStream.Position), e);
            }

            ReadValue(context, reader, dataType, output);

            #if DEBUG
            Debug.WriteLine(string.Format(Errors.Amf3Decoder_ReadValue_End, dataType, reader.BaseStream.Position));
            #endif
        }

        /// <summary>
        /// Read a value of a given type from current reader's position.
        /// </summary>
        /// <remarks>
        /// Current reader position must be just after a value type marker of a type to read.
        /// </remarks>
        /// <param name="context">AMF decoding context.</param>
        /// <param name="reader">AMF reader.</param>
        /// <param name="type">Type of the value to read.</param>
        /// <param name="output">AMFX output.</param>
        /// <exception cref="NotSupportedException">AMF type is not supported.</exception>
        /// <exception cref="FormatException">Unknown data format.</exception>
        /// <exception cref="SerializationException">Error during deserialization.</exception>
        private void ReadValue(AmfContext context, AmfStreamReader reader, Amf3TypeMarker type, XmlWriter output = null)
        {
            #if DEBUG
            Debug.WriteLine(string.Format(Errors.Amf3Decoder_ReadValue_Debug, type, reader.BaseStream.Position));
            #endif

            switch (type)
            {
                case Amf3TypeMarker.Null:
                case Amf3TypeMarker.Undefined:
                    WriteEmptyElement(type.ToAmfxName(), output);
                    break;

                case Amf3TypeMarker.False:
                    WriteEmptyElement(type.ToAmfxName(), output);
                    break;

                case Amf3TypeMarker.True:
                    WriteEmptyElement(type.ToAmfxName(), output);
                    break;

                case Amf3TypeMarker.Integer:
                    ReadInteger(reader, output);
                    break;

                case Amf3TypeMarker.Double:
                    ReadDouble(reader, output);
                    break;

                case Amf3TypeMarker.String:
                    ReadString(context, reader, output);
                    break;

                case Amf3TypeMarker.Date:
                    ReadDate(context, reader, output);
                    break;

                case Amf3TypeMarker.ByteArray:
                    ReadByteArray(context, reader, output);
                    break;

                case Amf3TypeMarker.Xml:
                case Amf3TypeMarker.XmlDocument:
                    ReadXml(context, reader, output);
                    break;

                case Amf3TypeMarker.Array:
                    ReadArray(context, reader, output);
                    break;

                case Amf3TypeMarker.Object:
                    ReadObject(context, reader, output);
                    break;

                default:
                    throw new NotSupportedException("Type '" + type + "' is not supported.");
            }

            if (output != null) output.Flush();
        }

        #region Primitive types
        /// <summary>
        /// Read an integer.
        /// </summary>
        private static void ReadInteger(AmfStreamReader reader, XmlWriter output = null)
        {
            var value = ReadUint29(reader);

            const int mask = 1 << 28; //Integer sign mask
            value = -(value & mask) | value;

            if (output != null)
            {
                output.WriteStartElement(AmfxContent.Integer);
                output.WriteValue(value);
                output.WriteEndElement();
            }
        }

        /// <summary>
        /// Read a double.
        /// </summary>
        private static void ReadDouble(AmfStreamReader reader, XmlWriter output = null)
        {
            var value = reader.ReadDouble();

            if (output != null)
            {
                if (value <= MinInt29Value || value >= MaxInt29Value && Math.Abs(value - Math.Round(value)) < MinDoublePrecision)
                {
                    var integer = Convert.ToInt32(value);
                    output.WriteStartElement(AmfxContent.Integer);
                    output.WriteValue(integer);
                }
                else
                {
                    output.WriteStartElement(AmfxContent.Double);
                    output.WriteValue(value);
                }

                output.WriteEndElement();
            }
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
        private static string ReadString(AmfContext context, AmfStreamReader reader, XmlWriter output = null)
        {
            int index, reference;
            string cache;

            if ((cache = ReadStringReference(context, reader, out index, out reference)) != null)
            {
                if (output != null)
                {
                    output.WriteStartElement(AmfxContent.String);
                    output.WriteAttributeString(AmfxContent.StringId, index.ToString());
                    output.WriteEndElement();
                }

                return cache;
            }

            //Get string length
            var length = (reference >> 1);
            var value = ReadUtf8(reader, length);

            if (value != string.Empty)
                context.StringReferences.Add(value);

            if (output != null)
            {
                output.WriteStartElement(AmfxContent.String);
                output.WriteValue(value);
                output.WriteEndElement();
            }

            return value;
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
        private static void ReadDate(AmfContext context, AmfStreamReader reader, XmlWriter output = null)
        {
            int index, reference;

            if (ReadReference(context, reader, out index, out reference))
            {
                if (output != null) WriteReference(index, output);
                return;
            }

            context.References.Track();

            //Dates are represented as an Unix time stamp, but in milliseconds
            var milliseconds = reader.ReadDouble();
            var value = milliseconds.ToString();

            if (output != null)
            {
                output.WriteStartElement(AmfxContent.Date);
                output.WriteValue(value);
                output.WriteEndElement();
            }
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
        private static void ReadByteArray(AmfContext context, AmfStreamReader reader, XmlWriter output = null)
        {
            int index, reference;

            if (ReadReference(context, reader, out index, out reference))
            {
                if (output != null) WriteReference(index, output);
                return;
            }

            context.References.Track();

            //Get array length
            var length = (reference >> 1);
            var data = length == 0 ? new byte[] { } : reader.ReadBytes(length);
            var value = Convert.ToBase64String(data);

            if (output != null)
            {
                output.WriteStartElement(AmfxContent.ByteArray);
                output.WriteValue(value);
                output.WriteEndElement();
            }
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
        private static void ReadXml(AmfContext context, AmfStreamReader reader, XmlWriter output = null)
        {
            int index, reference;

            if (ReadReference(context, reader, out index, out reference))
            {
                if (output != null) WriteReference(index, output);
                return;
            }

            context.References.Track();

            //Get XML string length
            var length = (reference >> 1);
            var value = ReadUtf8(reader, length);

            if (output != null)
            {
                output.WriteStartElement(AmfxContent.Xml);
                output.WriteValue(value);
                output.WriteEndElement();
            }
        }
        #endregion

        #region Complex types
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
        private void ReadArray(AmfContext context, AmfStreamReader reader, XmlWriter output = null)
        {
            int index, reference;

            if (ReadReference(context, reader, out index, out reference))
            {
                if (output != null) WriteReference(index, output);
                return;
            }

            context.References.Track();

            var length = reference >> 1;
            var key = ReadString(context, reader);

            if (output != null)
            {
                output.WriteStartElement(AmfxContent.Array);
                output.WriteAttributeString(AmfxContent.ArrayLength, length.ToString());
            }

            //ECMA array
            if (key != string.Empty)
            {
                if (output != null) output.WriteAttributeString(AmfxContent.ArrayEcma, AmfxContent.True);

                //Read associative values
                do
                {
                    if (output != null)
                    {
                        output.WriteStartElement(AmfxContent.ArrayItem);
                        output.WriteAttributeString(AmfxContent.ArrayKey, key);
                    }

                    ReadAmfValue(context, reader, output);
                    key = ReadString(context, reader);

                    if (output != null) output.WriteEndElement();
                }
                while (key != string.Empty);

                //Read array values
                for (var i = 0; i < length; i++)
                    ReadAmfValue(context, reader, output);
            }
            //Regular array
            else
            {
                //Read array values
                for (var i = 0; i < length; i++)
                    ReadAmfValue(context, reader, output);
            }

            if (output != null) output.WriteEndElement();
        }

        /// <summary>
        /// Read an object.
        /// </summary>
        private void ReadObject(AmfContext context, AmfStreamReader reader, XmlWriter output = null)
        {
            var reference = reader.PeekChar();

            if ((reference & 0x1) == 0)
            {
                var index = reference >> 1;
                if (output != null) WriteReference(index, output);
                return;
            }

            context.References.Track();

            int traitsindex, traitsreference;
            AmfTypeTraits traits;

            #region Read object's traits
            if ((traits = ReadTraitsReference(context, reader, out traitsindex, out traitsreference)) == null)
            {
                var isExternalizable = ((traitsreference & 0x4) == 4);
                var isDynamic = ((traitsreference & 0x8) == 8);
                var typeName = ReadString(context, reader);
                var count = (traitsreference >> 4);
                var classMembers = new string[count];

                for (var i = 0; i < count; i++)
                    classMembers[i] = ReadString(context, reader);

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

                context.TraitsReferences.Add(traits);
            }
            #endregion

            #if DEBUG
            Debug.WriteLine(string.Format(Errors.Amf3Decoder_ReadObject_Debug_Name, traits.TypeName));

            if (traits.IsDynamic)
                Debug.WriteLine(Errors.Amf3Decoder_ReadObject_Debug_Dynamic);
            else if (traits.IsExternalizable)
                Debug.WriteLine(Errors.Amf3Decoder_ReadObject_Debug_Externizable);
            else
                Debug.WriteLine(string.Format(Errors.Amf3Decoder_ReadObject_Debug_Members, traits.ClassMembers.Length));
            #endif

            

            using (var ms = new MemoryStream())
            {
                #region Reading object members
                var members = new List<string>();

                var buffer = new XmlTextWriter(ms, Encoding.UTF8);
                buffer.WriteStartElement("buffer");
                buffer.WriteAttributeString("xmlns", AmfxContent.Namespace);

                #if DEBUG
                var memberPosition = 0;
                #endif

                //Read object's properties
                foreach (var classMember in traits.ClassMembers)
                {
                    #if DEBUG
                    Debug.WriteLine(string.Format(Errors.Amf3Decoder_ReadObject_Debug_ReadingField, memberPosition,
                                                    classMember));
                    memberPosition++;
                    #endif

                    ReadAmfValue(context, reader, buffer);
                    members.Add(classMember);
                }

                //Read dynamic properties too
                if (traits.IsDynamic)
                {
                    #if DEBUG
                    Debug.WriteLine(Errors.Amf3Decoder_ReadObject_Debug_ReadingDynamic);
                    #endif

                    var key = ReadString(context, reader);

                    while (key != string.Empty)
                    {
                        #if DEBUG
                        Debug.WriteLine(string.Format(Errors.Amf3Decoder_ReadObject_Debug_ReadingDynamicField, key));
                        #endif

                        ReadAmfValue(context, reader, buffer);
                        members.Add(key);

                        key = ReadString(context, reader);
                    }

                    #if DEBUG
                    Debug.WriteLine(Errors.Amf3Decoder_ReadObject_Debug_DynamicEnd);
                    #endif
                }

                buffer.WriteEndElement();
                buffer.Flush();
                #endregion

                #if DEBUG
                Debug.WriteLine(Errors.Amf3Decoder_ReadObject_Debug_End);
                #endif

                #region Writing member values
                if (output != null)
                {
                    output.WriteStartElement(AmfxContent.Object);

                    if (traits.TypeName != AmfTypeTraits.BaseTypeAlias)
                        output.WriteAttributeString(AmfxContent.ObjectType, traits.TypeName);

                    output.WriteStartElement(AmfxContent.Traits);

                    //Regular traits object
                    //Always write traits for dynamic objects
                    if (traitsindex == -1 || traits.IsDynamic)
                    {
                        if (traits.IsExternalizable)
                        {
                            output.WriteAttributeString(AmfxContent.TraitsExternizable, AmfxContent.True);
                        }
                        else
                        {
                            //Write object members
                            foreach (var classMember in members)
                            {
                                output.WriteStartElement(AmfxContent.String);
                                output.WriteValue(classMember);
                                output.WriteEndElement();
                            }
                        }
                    }
                    //Traits object reference
                    else
                    {
                        output.WriteAttributeString(AmfxContent.TraitsId, traitsindex.ToString());
                    }

                    output.WriteEndElement(); //End of traits

                    //Write object members
                    if (members.Count > 0)
                    {
                        ms.Position = 0;
                        var bufferreader = new XmlTextReader(ms);
                        bufferreader.Read();
                        bufferreader.ReadStartElement();

                        while (bufferreader.Depth >= 1)
                            output.WriteNode(bufferreader, false);
                    }

                    output.WriteEndElement(); //End of object
                }
                #endregion
            }
        }
        #endregion
        #endregion
    }
}

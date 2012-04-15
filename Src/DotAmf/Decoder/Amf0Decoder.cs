using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;
using System.Text;
using DotAmf.Data;
using DotAmf.IO;

namespace DotAmf.Decoder
{
    /// <summary>
    /// AMF0 decoder.
    /// </summary>
    class Amf0Decoder : AbstractAmfDecoder
    {
        #region .ctor
        public Amf0Decoder(AmfEncodingOptions options)
            : base(options)
        {
        }
        #endregion

        #region IAmfDecoder implementation
        override public void Decode(Stream stream, XmlWriter output)
        {
            var reader = new AmfStreamReader(stream);
            var context = CreateDefaultContext(reader);
            ReadAmfValue(context, output);
        }

        sealed public override AmfHeaderDescriptor ReadPacketHeader(Stream stream)
        {
            var reader = new AmfStreamReader(stream);
            var context = CreateDefaultContext(reader);

            try
            {
                var descriptor = new AmfHeaderDescriptor
                                     {
                                         Name = ReadString(context),
                                         MustUnderstand = context.Reader.ReadBoolean()
                                     };

                context.Reader.ReadInt32(); //Header length

                context.ResetReferences();
                return descriptor;
            }
            catch (Exception e)
            {
                throw new FormatException(Errors.Amf0Deserializer_ReadPacketHeaders_InvalidFormat, e);
            }
        }

        sealed public override AmfMessageDescriptor ReadPacketBody(Stream stream)
        {
            var reader = new AmfStreamReader(stream);
            var context = CreateDefaultContext(reader);

            try
            {
                var descriptor = new AmfMessageDescriptor
                                     {
                                         Target = ReadString(context),
                                         Response = ReadString(context)
                                     };

                context.Reader.ReadInt32(); //Message length

                context.ResetReferences();
                return descriptor;
            }
            catch (Exception e)
            {
                throw new FormatException(Errors.Amf0Deserializer_ReadPacketMessages_InvalidFormat, e);
            }
        }
        #endregion

        #region Helper methods
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

        /// <summary>
        /// Read a specified number of bytes of a string.
        /// </summary>
        /// <param name="context">AMF decoding context.</param>
        /// <param name="length">Number of bytes to read.</param>
        private static string ReadUtf8(AmfEncodingContext context, uint length)
        {
            //Make sure that a null is never returned
            if (length == 0) return string.Empty;

            var data = context.Reader.ReadBytes((int)length);

            //All strings are encoded in UTF-8)
            return Encoding.UTF8.GetString(data);
        }
        #endregion

        #region Deserialization methods
        protected override void ReadAmfValue(AmfEncodingContext context, XmlWriter output = null)
        {
            if (context.AmfVersion != AmfVersion.Amf0)
                throw new InvalidOperationException(string.Format(Errors.Amf0Decoder_ReadAmfValue_AmfVersionNotSupported, context.AmfVersion));

            Amf0TypeMarker dataType;

            try
            {
                //Read a type marker byte
                dataType = (Amf0TypeMarker)context.Reader.ReadByte();
            }
            catch (Exception e)
            {
                throw new FormatException(Errors.Amf0Decoder_ReadAmfValue_TypeMarkerMissing, e);
            }

            //Special case
            if(dataType == Amf0TypeMarker.AvmPlusObject)
            {
                var newContext = new AmfEncodingContext(context.Reader, AmfVersion.Amf3);
                ReadAmfValue(newContext, output);
                return;
            }

            ReadValue(context, dataType, output);
        }

        /// <summary>
        /// Read a value of a given type from current reader's position.
        /// </summary>
        /// <remarks>
        /// Current reader position must be just after a value type marker of a type to read.
        /// </remarks>
        /// <param name="context">AMF decoding context.</param>
        /// <param name="type">Type of the value to read.</param>
        /// <param name="output">AMFX output.</param>
        /// <exception cref="NotSupportedException">AMF type is not supported.</exception>
        /// <exception cref="FormatException">Unknown data format.</exception>
        /// <exception cref="SerializationException">Error during deserialization.</exception>
        private void ReadValue(AmfEncodingContext context, Amf0TypeMarker type, XmlWriter output = null)
        {
            switch (type)
            {
                case Amf0TypeMarker.Null:
                case Amf0TypeMarker.Undefined:
                    break;

                case Amf0TypeMarker.Boolean:
                    context.Reader.ReadBoolean();
                    break;

                case Amf0TypeMarker.Number:
                    context.Reader.ReadDouble();
                    break;

                case Amf0TypeMarker.String:
                    ReadString(context, output);
                    break;

                case Amf0TypeMarker.LongString:
                    ReadLongString(context, output);
                    break;

                case Amf0TypeMarker.Date:
                    ReadDate(context, output);
                    break;

                case Amf0TypeMarker.XmlDocument:
                    ReadXml(context, output);
                    break;

                case Amf0TypeMarker.Reference:
                    ReadReference(context, output);
                    break;

                case Amf0TypeMarker.Object:
                    ReadObject(context, output);
                    break;

                case Amf0TypeMarker.TypedObject:
                    ReadObject(context, output, true);
                    break;

                case Amf0TypeMarker.EcmaArray:
                    ReadEcmaArray(context, output);
                    break;

                case Amf0TypeMarker.StrictArray:
                    ReadStrictArray(context, output);
                    break;

                case Amf0TypeMarker.MovieClip:
                case Amf0TypeMarker.RecordSet:
                case Amf0TypeMarker.Unsupported:
                    throw new NotSupportedException(string.Format(Errors.Amf0Deserializer_ReadValue_UnsupportedType, type));

                default:
                    throw new FormatException(string.Format(Errors.Amf0Decoder_ReadValue_UnknownType, (byte)type));
            }
        }

        #region Primitive types
        /// <summary>
        /// Read an object reference.
        /// </summary>
        /// <remarks>
        /// Type declaration:
        /// <c>reference-type = reference-marker U16</c>
        /// </remarks>
        private static void ReadReference(AmfEncodingContext context, XmlWriter output = null)
        {
            var index = context.Reader.ReadUInt16();

            if (context.References <= index)
                throw new SerializationException(string.Format(Errors.Amf0Decoder_ReadReference_BadIndex, index));

            WriteReference(index, output);
        }

        /// <summary>
        /// Read a string.
        /// </summary>
        /// <remarks>
        /// Type declaration:
        /// <c>string-type = string-marker UTF-8</c>
        /// </remarks>
        private static string ReadString(AmfEncodingContext context, XmlWriter output = null)
        {
            //First 16 bits represents string's (UTF-8) length in bytes
            var length = context.Reader.ReadUInt16();
            var value = ReadUtf8(context, length);

            if (output != null)
            {
                output.WriteStartElement(AmfxContent.String);
                output.WriteValue(value);
                output.WriteEndElement();
            }

            return value;
        }

        /// <summary>
        /// Read a long string.
        /// </summary>
        /// <remarks>
        /// Type declaration:
        /// <c>long-string-type = long-string-marker UTF-8-long</c>
        /// </remarks>
        private static string ReadLongString(AmfEncodingContext context, XmlWriter output = null)
        {
            //First 32 bits represents long string's (UTF-8-long) length in bytes
            var length = context.Reader.ReadUInt32();
            var value =  ReadUtf8(context, length);

            if (output != null)
            {
                output.WriteStartElement(AmfxContent.String);
                output.WriteValue(value);
                output.WriteEndElement();
            }

            return value;
        }

        /// <summary>
        /// Read an XML document.
        /// </summary>
        /// <remarks>
        /// Type declaration:
        /// <c>xml-document-type = xml-document-marker UTF-8-long</c>
        /// </remarks>
        private static void ReadXml(AmfEncodingContext context, XmlWriter output = null)
        {
            var data = ReadLongString(context); //XML is stored as a long string

            if (output != null)
            {
                var rawData = Encoding.UTF8.GetBytes(data);
                var value = Convert.ToBase64String(rawData);
                output.WriteStartElement(AmfxContent.Xml);
                output.WriteValue(value);
                output.WriteEndElement();
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
        private static void ReadDate(AmfEncodingContext context, XmlWriter output = null)
        {
            //Dates are represented as an Unix time stamp, but in milliseconds
            var milliseconds = context.Reader.ReadDouble();

            //Value indicates a timezone, but it should not be used
            context.Reader.ReadInt16();

            if (output != null)
            {
                output.WriteStartElement(AmfxContent.Date);
                var result = milliseconds.ToString();
                output.WriteValue(result);
                output.WriteEndElement();
            }
        }
        #endregion

        #region Complex types
        /// <summary>
        /// Read object properties map.
        /// </summary>
        /// <remarks>
        /// Type declaration:
        /// <c>object-property = (UTF-8 value-type) | (UTF-8-empty object-end-marker)</c>
        /// </remarks>
        /// <exception cref="SerializationException"></exception>
        private IList<string> ReadPropertiesMap(AmfEncodingContext context, XmlWriter output = null)
        {
            try
            {
                var result = new List<string>();
                var property = ReadString(context); //Read first property's name

                //An empty property name indicates that object's declaration ends here
                while (property != string.Empty)
                {
                    result.Add(property);

                    ReadAmfValue(context, output);
                    property = ReadString(context);
                }

                //Last byte is always an "ObjectEnd" marker
                var marker = (Amf0TypeMarker)context.Reader.ReadByte();

                //Something goes wrong
                if (marker != Amf0TypeMarker.ObjectEnd)
                    throw new FormatException(Errors.Amf0Decoder_ReadPropertiesMap_UnexpectedObjectEnd);

                return result;
            }
            catch (Exception e)
            {
                throw new SerializationException(Errors.Amf0Decoder_ReadPropertiesMap_UnableToDeserialize, e);
            }
        }

        /// <summary>
        /// Read an object.
        /// </summary>
        /// <remarks>
        /// Type declaration:
        /// <c>object-property = (UTF-8 value-type) | (UTF-8-empty object-end-marker)
        /// anonymous-object-type = object-marker *(object-property)</c>
        /// </remarks>
        private void ReadObject(AmfEncodingContext context, XmlWriter output = null, bool isTyped=false)
        {
            context.CountReference();

            if (output != null)
            {
                output.WriteStartElement(AmfxContent.Object);

                //Read properties
                using (var ms = new MemoryStream())
                {
                    var buffer = new XmlTextWriter(ms, Encoding.UTF8);
                    buffer.WriteStartElement("buffer");
                    buffer.WriteAttributeString("xmlns", AmfxContent.Namespace);

                    var members = ReadPropertiesMap(context);

                    buffer.WriteEndElement();
                    buffer.Flush();

                    if (isTyped)
                    {
                        var typeName = ReadString(context);
                        output.WriteAttributeString(AmfxContent.ObjectType, typeName);
                    }

                    output.WriteStartElement(AmfxContent.Traits);

                    //Write traits
                    foreach (var classMember in members)
                    {
                        output.WriteStartElement(AmfxContent.String);
                        output.WriteValue(classMember);
                        output.WriteEndElement();
                    }

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

                    buffer.WriteEndElement(); //End of traits
                }

                output.WriteEndElement(); //End of object
            }
            else
            {
                //Just read the object's properties
                ReadPropertiesMap(context);
                if(isTyped) ReadString(context);
            }
        }

        /// <summary>
        /// Read an ECMA array.
        /// </summary>
        /// <remarks>
        /// Type declaration:
        /// <c>associative-count = U32
        /// ecma-array-type = associative-count *(object-property)</c>
        /// </remarks>
        private void ReadEcmaArray(AmfEncodingContext context, XmlWriter output = null)
        {
            context.CountReference();

            context.Reader.ReadUInt32(); //Read properties count
            ReadObject(context, output);
        }

        /// <summary>
        /// Read a strict array.
        /// </summary>
        /// <remarks>
        /// Type declaration:
        /// <c>array-count = U32
        /// strict-array-type = array-count *(value-type)</c>
        /// </remarks>
        private void ReadStrictArray(AmfEncodingContext context, XmlWriter output = null)
        {
            context.CountReference();

            var length = context.Reader.ReadUInt32();

            if (output != null)
            {
                output.WriteStartElement(AmfxContent.Array);
                output.WriteAttributeString(AmfxContent.ArrayLength, length.ToString());
            }

            for (var i = 0; i < length; i++)
                ReadAmfValue(context, output);

            if (output != null) output.WriteEndElement();
        }
        #endregion
        #endregion
    }
}

// Copyright (c) 2012 Artem Abashev (http://abashev.me)
// All rights reserved.
// Licensed under the Microsoft Public License (Ms-PL)
// http://opensource.org/licenses/ms-pl.html

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using DotAmf.Data;
using DotAmf.IO;

namespace DotAmf.Encoder
{
    /// <summary>
    /// AMF0 encoder.
    /// </summary>
    class Amf0Encoder : AbstractAmfEncoder
    {
        #region .ctor
        public Amf0Encoder(AmfEncodingOptions encodingOptions)
            : base(encodingOptions)
        {
        }
        #endregion

        #region Constants
        /// <summary>
        /// Maximum number of byte a short string can contain.
        /// </summary>
        private const uint ShortStringLimit = 65535;
        #endregion

        #region IAmfSerializer implementation
        public override void Encode(Stream stream, XmlReader input)
        {
            var writer = new AmfStreamWriter(stream);
            var context = CreateDefaultContext();
            WriteAmfValue(context, input, writer);
        }

        public override void WritePacketHeader(Stream stream, AmfHeaderDescriptor descriptor)
        {
            var writer = new AmfStreamWriter(stream);

            WriteUtf8(writer, descriptor.Name);
            writer.Write((byte)(descriptor.MustUnderstand ? 1 : 0));
            writer.Write(-1);
        }

        public override void WritePacketBody(Stream stream, AmfMessageDescriptor descriptor)
        {
            var writer = new AmfStreamWriter(stream);

            WriteUtf8(writer, descriptor.Target);
            WriteUtf8(writer, descriptor.Response);
            writer.Write(-1);
        }
        #endregion

        #region Serialization methods
        protected override void WriteAmfValue(AmfContext context, XmlReader input, AmfStreamWriter writer)
        {
            if (context.AmfVersion != AmfVersion.Amf0)
                throw new InvalidOperationException(string.Format(Errors.Amf0Decoder_ReadAmfValue_AmfVersionNotSupported, context.AmfVersion));

            if (input == null) throw new ArgumentNullException("input");
            if (context == null) throw new ArgumentNullException("context");
            if (input.NodeType != XmlNodeType.Element) throw new XmlException(string.Format("Element node expected, {0} found.", input.NodeType));

            #region Primitive values
            switch (input.Name)
            {
                case AmfxContent.Null:
                    WriteNull(writer);
                    return;

                case AmfxContent.True:
                    WriteBoolean(writer, true);
                    return;

                case AmfxContent.False:
                    WriteBoolean(writer, false);
                    return;
            }
            #endregion

            #region Complex values
            var reader = input.ReadSubtree();
            reader.MoveToContent();

            switch (reader.Name)
            {
                case AmfxContent.Integer:
                case AmfxContent.Double:
                    WriteNumber(writer, reader);
                    break;

                case AmfxContent.String:
                    WriteString(writer, reader);
                    break;

                case AmfxContent.Date:
                    WriteDate(writer, reader);
                    break;

                case AmfxContent.Xml:
                    WriteXml(writer, reader);
                    break;

                case AmfxContent.Reference:
                    WriteReference(context, writer, reader);
                    break;

                case AmfxContent.Array:
                    WriteArray(context, writer, reader);
                    break;

                case AmfxContent.Object:
                    WriteObject(context, writer, reader);
                    break;

                default:
                    throw new NotSupportedException("Unexpected AMFX type: " + reader.Name);
            }

            reader.Close();
            #endregion
        }

        #region Primitive values
        /// <summary>
        /// Write an AMF0 type marker.
        /// </summary>
        private static void WriteTypeMarker(AmfStreamWriter writer, Amf0TypeMarker marker)
        {
            writer.Write((byte)marker);
        }

        /// <summary>
        /// Write a <c>null</c>.
        /// </summary>
        private static void WriteNull(AmfStreamWriter writer)
        {
            WriteTypeMarker(writer, Amf0TypeMarker.Null);
        }

        /// <summary>
        /// Write a boolean value.
        /// </summary>
        private static void WriteBoolean(AmfStreamWriter writer, bool value)
        {
            writer.Write(value);
        }
        #endregion

        #region Value types
        /// <summary>
        /// Write a number.
        /// </summary>
        private static void WriteNumber(AmfStreamWriter writer, XmlReader input)
        {
            WriteTypeMarker(writer, Amf0TypeMarker.Number);

            var value = Convert.ToDouble(input.ReadString());
            writer.Write(value);
        }

        /// <summary>
        /// Write a string.
        /// </summary>
        private static void WriteString(AmfStreamWriter writer, XmlReader input)
        {
            var value = input.IsEmptyElement 
                ? string.Empty 
                : input.ReadString();

            WriteString(writer, value);
        }

        /// <summary>
        /// Write a string.
        /// </summary>
        private static void WriteString(AmfStreamWriter writer, string value)
        {
            var data = Encoding.UTF8.GetBytes(value);

            WriteTypeMarker(writer, data.Length < ShortStringLimit 
                ? Amf0TypeMarker.String 
                : Amf0TypeMarker.LongString);

            WriteUtf8(writer, data);
        }

        /// <summary>
        /// Write UTF8 string.
        /// </summary>
        private static void WriteUtf8(AmfStreamWriter writer, string value)
        {
            var data = Encoding.UTF8.GetBytes(value);
            WriteUtf8(writer, data);
        }

        /// <summary>
        /// Write UTF8 data.
        /// </summary>
        private static void WriteUtf8(AmfStreamWriter writer, byte[] data)
        {
            if (data.Length < ShortStringLimit)
                writer.Write((ushort)data.Length);
            else
                writer.Write((uint)data.Length);

            writer.Write(data);
        }

        /// <summary>
        /// Write a reference value.
        /// </summary>
        private static void WriteReference(AmfContext context, AmfStreamWriter writer, XmlReader input)
        {
            var index = Convert.ToInt32(input.GetAttribute(AmfxContent.ReferenceId));
            var proxy = context.References[index];

            switch(proxy.AmfxType)
            {
                case AmfxContent.Array:
                case AmfxContent.Object:
                    WriteTypeMarker(writer, Amf0TypeMarker.Reference);
                    writer.Write((ushort)index);
                    break;

                default:
                    throw new InvalidOperationException(string.Format("AMFX type '{0}' cannot be send by reference.", proxy.AmfxType));
            }
        }

        /// <summary>
        /// Write a date.
        /// </summary>
        private static void WriteDate(AmfStreamWriter writer, XmlReader input)
        {
            var value = Convert.ToDouble(input.ReadString());
            WriteDate(writer, value);
        }

        /// <summary>
        /// Write a date.
        /// </summary>
        private static void WriteDate(AmfStreamWriter writer, double value)
        {
            WriteTypeMarker(writer, Amf0TypeMarker.Date);
            writer.Write(value);
            writer.Write((short)0); //Timezone (not used)
        }

        /// <summary>
        /// Write an XML.
        /// </summary>
        private static void WriteXml(AmfStreamWriter writer, XmlReader input)
        {
            var value = input.ReadString();
            var data = Encoding.UTF8.GetBytes(value);
            WriteXml(writer, data);
        }

        /// <summary>
        /// Write an XML.
        /// </summary>
        private static void WriteXml(AmfStreamWriter writer, byte[] value)
        {
            WriteTypeMarker(writer, Amf0TypeMarker.XmlDocument);
            writer.Write((uint)value.Length);
            writer.Write(value);
        }
        #endregion

        #region Complex types
        /// <summary>
        /// Write an array.
        /// </summary>
        private void WriteArray(AmfContext context, AmfStreamWriter writer, XmlReader input)
        {
            context.References.Add(new AmfReference {AmfxType = AmfxContent.Array});

            var length = Convert.ToUInt32(input.GetAttribute(AmfxContent.ArrayLength));
            writer.Write(length);

            if (length == 0) return;

            input.MoveToContent();

            while (input.Read())
            {
                if (input.NodeType != XmlNodeType.Element)
                    continue;

                for (var i = 0; i < length; i++)
                {
                    var itemreader = input.ReadSubtree();
                    itemreader.MoveToContent();
                    WriteAmfValue(context, itemreader, writer);
                    itemreader.Close();
                }
            }
        }

        /// <summary>
        /// Write an object.
        /// </summary>
        private void WriteObject(AmfContext context, AmfStreamWriter writer, XmlReader input)
        {
            context.References.Add(new AmfReference { AmfxType = AmfxContent.Object });

            WriteTypeMarker(writer, Amf0TypeMarker.Object);
            
            var typeName = string.Empty;

            if (input.HasAttributes)
                typeName = input.GetAttribute(AmfxContent.ObjectType);

            #region Read traits
            var traits = new AmfTypeTraits { TypeName = typeName };

            while (input.Read())
            {
                if (input.NodeType != XmlNodeType.Element && input.Name != AmfxContent.Traits) continue;

                if (!input.IsEmptyElement)
                {
                    var traitsReader = input.ReadSubtree();
                    traitsReader.MoveToContent();
                    traitsReader.ReadStartElement();

                    var members = new List<string>();

                    while (input.NodeType != XmlNodeType.EndElement)
                        members.Add(traitsReader.ReadElementContentAsString());

                    traits.ClassMembers = members.ToArray();
                    traitsReader.Close();
                }

                break;
            }
            #endregion

            #region Type name
            //Untyped object
            if(string.IsNullOrEmpty(traits.TypeName))
            {
                WriteTypeMarker(writer, Amf0TypeMarker.Object);
            }
            //Strongly-typed object
            else
            {
                WriteTypeMarker(writer, Amf0TypeMarker.TypedObject);
                var typeNameData = Encoding.UTF8.GetBytes(traits.TypeName);
                writer.Write((ushort)typeNameData.Length);
                writer.Write(typeNameData);
            }
            #endregion

            #region Write members
            var i = 0;

            while (input.Read())
            {
                if (input.NodeType != XmlNodeType.Element) continue;

                var memberName = traits.ClassMembers[i];
                var memberReader = input.ReadSubtree();
                memberReader.MoveToContent();

                WriteUtf8(writer, memberName);
                WriteAmfValue(context, memberReader, writer);

                memberReader.Close();
                i++;
            }
            #endregion

            WriteUtf8(writer, string.Empty);
            WriteTypeMarker(writer, Amf0TypeMarker.ObjectEnd);
        }
        #endregion
        #endregion
    }
}

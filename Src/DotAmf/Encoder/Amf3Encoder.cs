using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using DotAmf.Data;
using DotAmf.IO;

namespace DotAmf.Encoder
{
    /// <summary>
    /// AMF3 encoder.
    /// </summary>
    class Amf3Encoder : Amf0Encoder
    {
        #region .ctor
        public Amf3Encoder(AmfEncodingOptions encodingOptions)
            : base(encodingOptions)
        {
        }
        #endregion

        #region Constants
        /// <summary>
        /// A bit mask to truncate a value to <c>UInt29</c>.
        /// </summary>
        private const int UInt29Mask = 0x1FFFFFFF;

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
        #endregion

        #region IAmfSerializer implementation
        public override void Encode(Stream stream, XmlReader input)
        {
            var writer = new AmfStreamWriter(stream);
            var context = CreateDefaultContext();
            WriteAmfValue(context, input, writer);
        }
        #endregion

        #region Helper methods
        /// <summary>
        /// Write an AMF0 type marker.
        /// </summary>
        private static void WriteTypeMarker(AmfStreamWriter writer, Amf3TypeMarker marker)
        {
            writer.Write((byte)marker);
        }

        /// <summary>
        /// Write an 29-bit unsigned integer.
        /// </summary>
        private static void WriteUInt29(AmfStreamWriter writer, int value)
        {
            //< 128:
            //0x00000000 - 0x0000007F
            if (value < 0x80)
            {
                writer.Write((byte)value);                      //0xxxxxxx
            }
            //< 16,384:
            //0x00000080 - 0x00003FFF
            else if (value < 0x4000)
            {
                writer.Write((byte)(value >> 7 & 0x7F | 0x80));   //1xxxxxxx
                writer.Write((byte)(value & 0x7F));               //xxxxxxxx
            }
            //< 2,097,152:
            //0x00004000 - 0x001FFFFF
            else if (value < 0x200000)
            {
                writer.Write((byte)(value >> 14 & 0x7F | 0x80));  //1xxxxxxx
                writer.Write((byte)(value >> 7 & 0x7F | 0x80));   //1xxxxxxx
                writer.Write((byte)(value & 0x7F));               //xxxxxxxx
            }
            //0x00200000 - 0x3FFFFFFF
            else if (value < 0x40000000)
            {
                writer.Write((byte)(value >> 22 & 0x7F | 0x80));  //1xxxxxxx
                writer.Write((byte)(value >> 15 & 0x7F | 0x80));  //1xxxxxxx
                writer.Write((byte)(value >> 8 & 0x7F | 0x80));   //1xxxxxxx
                writer.Write((byte)(value & 0xFF));               //xxxxxxxx
            }
            //0x40000000 - 0xFFFFFFFF, out of range
            else
            {
                throw new IndexOutOfRangeException("Integer is out of range: " + value);
            }
        }

        /// <summary>
        /// Write a reference.
        /// </summary>
        private static void WriteReference(AmfStreamWriter writer, int reference)
        {
            reference &= UInt29Mask; //Truncate value to UInt29

            //The first bit is a flag (representing whether an instance follows)
            //with value 0 to imply that this is not an instance but a reference.
            //The remaining 1 to 28 significant bits are used to encode a reference index.
            var flag = reference << 1;

            WriteUInt29(writer, flag);
        }

        /// <summary>
        /// Write UTF-8 string.
        /// </summary>
        private static void WriteUtf8(AmfContext context, AmfStreamWriter writer, string value)
        {
            if (value == null) value = string.Empty;

            //A special case
            if (value == string.Empty)
            {
                writer.Write((byte)0x01);
                return;
            }

            var index = context.StringReferences.IndexOf(value);

            if(index != -1)
            {
                WriteReference(writer, index);
                return;
            }

            context.StringReferences.Add(value);

            var decoded = Encoding.UTF8.GetBytes(value);

            WriteUtf8(writer, decoded);
        }

        /// <summary>
        /// Write UTF-8 data.
        /// </summary>
        private static void WriteUtf8(AmfStreamWriter writer, byte[] data)
        {
            //The first bit is a flag with value 1.
            //The remaining 1 to 28 significant bits are used 
            //to encode the byte-length of the data
            var flag = (data.Length << 1) | 0x1;

            WriteUInt29(writer, flag);
            writer.Write(data);
        }
        #endregion

        #region Serialization methods
        protected override void WriteAmfValue(AmfContext context, XmlReader input, AmfStreamWriter writer)
        {
            if (input == null) throw new ArgumentNullException("input");
            if (context == null) throw new ArgumentNullException("context");
            if (input.NodeType != XmlNodeType.Element) throw new XmlException(string.Format("Element node expected, {0} found.", input.NodeType));

            if (context.AmfVersion != AmfVersion.Amf3)
            {
                context = new AmfContext(AmfVersion.Amf3);
                writer.Write((byte)Amf0TypeMarker.AvmPlusObject);
            }

            #region Primitive values
            switch (input.Name)
            {
                case AmfxContent.Null:
                    WriteTypeMarker(writer, Amf3TypeMarker.Null);
                    return;

                case AmfxContent.True:
                    WriteTypeMarker(writer, Amf3TypeMarker.True);
                    return;

                case AmfxContent.False:
                    WriteTypeMarker(writer, Amf3TypeMarker.False);
                    return;
            }
            #endregion

            #region Complex values
            switch (input.Name)
            {
                case AmfxContent.Integer:
                    WriteInteger(writer, input);
                    break;

                case AmfxContent.Double:
                    WriteDouble(writer, input);
                    break;

                case AmfxContent.String:
                    WriteString(context, writer, input);
                    break;

                case AmfxContent.Reference:
                    WriteReference(context, writer, input);
                    break;

                case AmfxContent.Date:
                    WriteDate(context, writer, input);
                    break;

                case AmfxContent.Xml:
                    WriteXml(context, writer, input);
                    break;

                case AmfxContent.Array:
                    WriteArray(context, writer, input);
                    break;

                case AmfxContent.ByteArray:
                    WriteByteArray(context, writer, input);
                    break;

                case AmfxContent.Object:
                    WriteObject(context, writer, input);
                    break;

                default:
                    throw new NotSupportedException("Unexpected AMFX type: " + input.Name);
            }
            #endregion
        }

        #region Value types
        /// <summary>
        /// Write an integer.
        /// </summary>
        private static void WriteReference(AmfContext context, AmfStreamWriter writer, XmlReader input)
        {
            var index = Convert.ToInt32(input.GetAttribute(AmfxContent.ReferenceId));
            var proxy = context.References[index];

            switch(proxy.AmfxType)
            {
                case AmfxContent.Date:
                    WriteTypeMarker(writer, Amf3TypeMarker.Date);
                    break;

                case AmfxContent.Xml:
                    WriteTypeMarker(writer, Amf3TypeMarker.Xml);
                    break;

                case AmfxContent.Array:
                    WriteTypeMarker(writer, Amf3TypeMarker.Array);
                    break;

                case AmfxContent.ByteArray:
                    WriteTypeMarker(writer, Amf3TypeMarker.ByteArray);
                    break;

                case AmfxContent.Object:
                    WriteTypeMarker(writer, Amf3TypeMarker.Object);
                    break;

                default:
                    throw new InvalidOperationException(string.Format("AMFX type '{0}' cannot be send by reference.", proxy.AmfxType));
            }

            WriteReference(writer, index);
        }

        /// <summary>
        /// Write an integer.
        /// </summary>
        private static void WriteInteger(AmfStreamWriter writer, XmlReader input)
        {
            var text = input.ReadString();
            var value = Convert.ToInt64(text);

            //Check if the value fits the Int29 span
            if (value >= MinInt29Value && value <= MaxInt29Value)
            {
                //It should be safe to cast it there
                var integer = UInt29Mask & (int)value; //Truncate the value

                WriteTypeMarker(writer, Amf3TypeMarker.Integer);
                WriteUInt29(writer, integer);
            }
            //Promote the value to a double
            else
            {
                WriteTypeMarker(writer, Amf3TypeMarker.Double);
                writer.Write(Convert.ToDouble(value));
            }
        }

        /// <summary>
        /// Write a double.
        /// </summary>
        private static void WriteDouble(AmfStreamWriter writer, XmlReader input)
        {
            var value = Convert.ToDouble(input.ReadString());

            WriteTypeMarker(writer, Amf3TypeMarker.Double);
            writer.Write(value);
        }

        /// <summary>
        /// Write a string.
        /// </summary>
        private static void WriteString(AmfContext context, AmfStreamWriter writer, XmlReader input)
        {
            WriteTypeMarker(writer, Amf3TypeMarker.String);

            string value;

            if (input.IsEmptyElement)
            {
                if (input.AttributeCount > 0)
                {
                    var index = Convert.ToInt32(input.GetAttribute(AmfxContent.StringId));
                    WriteReference(writer, index);
                    return;
                }

                value = string.Empty;
            }
            else
                value = input.ReadString();

            WriteUtf8(context, writer, value);
        }

        /// <summary>
        /// Write a date.
        /// </summary>
        private static void WriteDate(AmfContext context, AmfStreamWriter writer, XmlReader input)
        {
            context.References.Add(new AmfReference { AmfxType = AmfxContent.Date });
            WriteTypeMarker(writer, Amf3TypeMarker.Date);

            var milliseconds = Convert.ToDouble(input.ReadString());

            //The first bit is a flag with value 1.
            //The remaining bits are not used.
            WriteUInt29(writer, 0 | 0x1);

            writer.Write(milliseconds);
        }

        /// <summary>
        /// Write an XML.
        /// </summary>
        private static void WriteXml(AmfContext context, AmfStreamWriter writer, XmlReader input)
        {
            context.References.Add(new AmfReference {AmfxType = AmfxContent.Xml});
            WriteTypeMarker(writer, Amf3TypeMarker.Xml);

            var encoded = input.ReadString();
            var decoded = Convert.FromBase64String(encoded);

            WriteUtf8(writer, decoded);
        }

        /// <summary>
        /// Write a byte array.
        /// </summary>
        private static void WriteByteArray(AmfContext context, AmfStreamWriter writer, XmlReader input)
        {
            context.References.Add(new AmfReference { AmfxType = AmfxContent.ByteArray });
            WriteTypeMarker(writer, Amf3TypeMarker.ByteArray);

            var encoded = input.ReadString();
            var bytes = Convert.FromBase64String(encoded);

            //The first bit is a flag with value 1.
            //The remaining 1 to 28 significant bits are used
            //to encode the byte-length of the data
            var flag = (bytes.Length << 1) | 0x1;
            WriteUInt29(writer, flag);

            writer.Write(bytes);
        }
        #endregion

        #region Complex types
        /// <summary>
        /// Write an array.
        /// </summary>
        private void WriteArray(AmfContext context, AmfStreamWriter writer, XmlReader input)
        {
            context.References.Add(new AmfReference { AmfxType = AmfxContent.Array });
            WriteTypeMarker(writer, Amf3TypeMarker.Array);

            var length = Convert.ToInt32(input.GetAttribute(AmfxContent.ArrayLength));

            input.Read();

            //The first bit is a flag with value 1.
            //The remaining 1 to 28 significant bits 
            //are used to encode the count of the dense 
            //portion of the Array.
            var size = (length << 1) | 0x1;
            WriteUInt29(writer, size);

            WriteUtf8(context, writer, string.Empty); //No associative values

            if (length == 0) return;

            for (var i = 0; i < length; i++)
            {
                WriteAmfValue(context, input, writer);
                input.Read();
            }
        }

        /// <summary>
        /// Write an object.
        /// </summary>
        private void WriteObject(AmfContext context, AmfStreamWriter writer, XmlReader input)
        {
            context.References.Add(new AmfReference { AmfxType = AmfxContent.Object });
            WriteTypeMarker(writer, Amf3TypeMarker.Object);

            AmfTypeTraits traits;
            var typeName = string.Empty;

            if (input.HasAttributes)
                typeName = input.GetAttribute(AmfxContent.ObjectType);

            #region Write traits
            input.Read();

            //Send traits by value
            if (!input.IsEmptyElement)
            {
                traits = new AmfTypeTraits { TypeName = typeName };
                context.TraitsReferences.Add(traits);

                var traitsReader = input.ReadSubtree();
                traitsReader.MoveToContent();
                traitsReader.ReadStartElement();

                var members = new List<string>();

                while (input.NodeType != XmlNodeType.EndElement)
                    members.Add(traitsReader.ReadElementContentAsString());

                traits.ClassMembers = members.ToArray();
                traitsReader.Close();

                //The first bit is a flag with value 1. 
                //The second bit is a flag with value 1.
                //The third bit is a flag with value 0. 
                var flag = 0x3; //00000011

                if (traits.IsExternalizable) flag |= 0x4; //00000111

                //The fourth bit is a flag specifying whether the type is dynamic.
                //A value of 0 implies not dynamic, a value of 1 implies dynamic.
                if (traits.IsDynamic) flag |= 0x8; //00001011

                //The remaining 1 to 25 significant bits are used to encode the number 
                //of sealed traits member names that follow after the class name.
                var count = traits.ClassMembers.Count();
                flag |= count << 4;

                WriteUInt29(writer, flag);

                WriteUtf8(context, writer, traits.TypeName);

                //Write member names
                foreach (var member in traits.ClassMembers)
                    WriteUtf8(context, writer, member);
            }
            //Send traits by reference
            else
            {
                var index = Convert.ToInt32(input.GetAttribute(AmfxContent.TraitsId));
                traits = context.TraitsReferences[index];

                var flag = index & UInt29Mask; //Truncate value to UInt29

                //The first bit is a flag with value 1.
                //The second bit is a flag (representing whether a trait
                //reference follows) with value 0 to imply that this objects
                //traits are being sent by reference. The remaining 1 to 27 
                //significant bits are used to encode a trait reference index.
                flag = (flag << 2) | 0x1;
                WriteUInt29(writer, flag);
            }

            input.Read();
            #endregion

            #region Write members
            for (var i = 0; i < traits.ClassMembers.Length; i++)
            {
                WriteAmfValue(context, input, writer);
                input.Read();
            }
            #endregion
        }
        #endregion
        #endregion
    }
}

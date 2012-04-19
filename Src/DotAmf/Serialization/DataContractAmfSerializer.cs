using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using DotAmf.Data;
using DotAmf.Decoder;
using DotAmf.Encoder;
using DotAmf.IO;

namespace DotAmf.Serialization
{
    /// <summary>
    /// Serializes objects to the Action Message Format (AMF) and deserializes AMF data to objects.
    /// </summary>
    /// <remarks>
    /// This class is thread-safe and is expensive to instantiate.
    /// </remarks>
    sealed public class DataContractAmfSerializer : XmlObjectSerializer
    {
        #region .ctor
        /// <summary>
        /// Initializes a new instance of the DataContractAmfSerializer class to serialize or deserialize an object of the specified type.
        /// </summary>
        /// <param name="type">A Type that specifies the type of the instances that is serialized or deserialized.</param>
        /// <exception cref="InvalidDataContractException">At least one of the types being serialized or deserialized does not conform to data contract rules.
        /// For example, the DataContractAttribute attribute has not been applied to the type.</exception>
        public DataContractAmfSerializer(Type type)
            : this(type, new List<Type>(), CreateDefaultOptions())
        {
        }

        /// <summary>
        /// Initializes a new instance of the DataContractAmfSerializer class to serialize or deserialize an object of the specified type.
        /// </summary>
        /// <param name="type">A Type that specifies the type of the instances that is serialized or deserialized.</param>
        /// <param name="encodingOptions">AMF encoding options.</param>
        /// <exception cref="InvalidDataContractException">At least one of the types being serialized or deserialized does not conform to data contract rules.
        /// For example, the DataContractAttribute attribute has not been applied to the type.</exception>
        public DataContractAmfSerializer(Type type, AmfEncodingOptions encodingOptions)
            : this(type, new List<Type>(), encodingOptions)
        {
        }

        /// <summary>
        /// Initializes a new instance of the DataContractAmfSerializer class to serialize or deserialize an object of the specified type, 
        /// and a collection of known types that may be present in the object graph.
        /// </summary>
        /// <param name="type">A Type that specifies the type of the instances that is serialized or deserialized.</param>
        /// <param name="knownTypes">An IEnumerable of Type that contains the types that may be present in the object graph.</param>
        /// <exception cref="InvalidDataContractException">At least one of the types being serialized or deserialized does not conform to data contract rules.
        /// For example, the DataContractAttribute attribute has not been applied to the type.</exception>
        public DataContractAmfSerializer(Type type, IEnumerable<Type> knownTypes)
            : this(type, knownTypes, CreateDefaultOptions())
        {
        }

        /// <summary>
        /// Initializes a new instance of the DataContractAmfSerializer class to serialize or deserialize an object of the specified type, 
        /// and a collection of known types that may be present in the object graph.
        /// </summary>
        /// <param name="type">A Type that specifies the type of the instances that is serialized or deserialized.</param>
        /// <param name="knownTypes">An IEnumerable of Type that contains the types that may be present in the object graph.</param>
        /// <param name="encodingOptions">AMF encoding options.</param>
        /// <exception cref="InvalidDataContractException">At least one of the types being serialized or deserialized does not conform to data contract rules.
        /// For example, the DataContractAttribute attribute has not been applied to the type.</exception>
        public DataContractAmfSerializer(Type type, IEnumerable<Type> knownTypes, AmfEncodingOptions encodingOptions)
        {
            if (type == null) throw new ArgumentNullException("type");
            _type = PrepareDataContract(type);

            if (knownTypes == null) throw new ArgumentNullException("knownTypes");
            _knownTypes = PrepareDataContracts(knownTypes);
            _knownTypes[_type.Key] = _type.Value;

            _encodingOptions = encodingOptions;
        }
        #endregion

        #region Data
        /// <summary>
        /// A Type that specifies the type of the instances that is serialized or deserialized.
        /// </summary>
        private readonly KeyValuePair<string, DataContractDescriptor> _type;

        /// <summary>
        /// Contains the types that may be present in the object graph.
        /// </summary>
        private readonly Dictionary<string, DataContractDescriptor> _knownTypes;

        /// <summary>
        /// AMF encoding options.
        /// </summary>
        private readonly AmfEncodingOptions _encodingOptions;
        #endregion

        #region Read methods
        /// <summary>
        /// Reads a document stream in the AMF (Action Message Format) format and returns the deserialized object.
        /// </summary>
        /// <param name="stream">The Stream to be read.</param>
        /// <returns>The deserialized object.</returns>
        /// <exception cref="SerializationException">Unable to resolve data contracts.</exception>
        public override object ReadObject(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException("stream");
            if (!stream.CanRead) throw new ArgumentException(Errors.DataContractAmfSerializer_ReadObject_InvalidStream, "stream");

            using (var buffer = new MemoryStream())
            {
                ReadObject(stream, AmfxWriter.Create(buffer));

                buffer.Position = 0;
                return ReadObject(new XmlTextReader(buffer));
            }
        }

        /// <summary>
        /// Reads a document stream in the AMF (Action Message Format) format and writes
        /// it in the AMFX (Action Message Format in XML) format,
        /// </summary>
        /// <param name="stream">The Stream to be read.</param>
        /// <param name="output">AMFX writer.</param>
        /// <returns>Object graph.</returns>
        /// <exception cref="SerializationException">Unable to resolve data contracts.</exception>
        public void ReadObject(Stream stream, XmlWriter output)
        {
            //Decode AMF packet
            if (_type.Value.AmfxType == AmfxContent.AmfxDocument)
            {
                var decoder = new AmfPacketDecoder(_encodingOptions);
                decoder.Decode(stream, output);
            }
            //Decode generic AMF data
            else
            {
                var decoder = CreateDecoder(_encodingOptions);
                decoder.Decode(stream, output);
            }
        }

        /// <summary>
        /// Reads the XML document mapped from AMFX (Action Message Format in XML) 
        /// with an XmlDictionaryReader and returns the deserialized object.
        /// </summary>
        /// <param name="reader">XML reader.</param>
        /// <returns>Object graph.</returns>
        /// <exception cref="SerializationException">Unable to resolve data contracts.</exception>
        public override object ReadObject(XmlDictionaryReader reader)
        {
            return ReadObject((XmlReader)reader);
        }

        /// <summary>
        /// Reads the AMFX document mapped from AMF with an XmlDictionaryReader and returns the deserialized object;
        /// it also enables you to specify whether the serializer should verify that it is positioned on an appropriate
        /// element before attempting to deserialize.
        /// </summary>
        /// <param name="reader">An XmlDictionaryReader used to read the AMFX document mapped from AMF.</param>
        /// <param name="verifyObjectName"><c>true</c> to check whether the enclosing XML element name and namespace 
        /// correspond to the expected name and namespace; otherwise, <c>false</c> to skip the verification.</param>
        /// <exception cref="SerializationException">Unable to resolve data contracts.</exception>
        public override object ReadObject(XmlDictionaryReader reader, bool verifyObjectName)
        {
            return ReadObject((XmlReader)reader, verifyObjectName);
        }

        /// <summary>
        /// Reads the XML document mapped from AMFX (Action Message Format in XML) 
        /// with an XmlReader and returns the deserialized object.
        /// </summary>
        /// <param name="reader">XML reader.</param>
        /// <returns>Object graph.</returns>
        /// <exception cref="SerializationException">Unable to resolve data contracts.</exception>
        public override object ReadObject(XmlReader reader)
        {
            if (reader == null) throw new ArgumentNullException("reader");

            var context = new SerializationContext(_encodingOptions.AmfVersion) { KnownTypes = _knownTypes };

            return _type.Value.AmfxType == AmfxContent.AmfxDocument
                ? DeserializePacket(reader, context) //Special case
                : Deserialize(reader, context);
        }

        /// <summary>
        /// Reads the AMFX document mapped from AMF with an XmlReader and returns the deserialized object;
        /// it also enables you to specify whether the serializer should verify that it is positioned on an appropriate
        /// element before attempting to deserialize.
        /// </summary>
        /// <param name="reader">An XmlReader used to read the AMFX document mapped from AMF.</param>
        /// <param name="verifyObjectName"><c>true</c> to check whether the enclosing XML element name and namespace 
        /// correspond to the expected name and namespace; otherwise, <c>false</c> to skip the verification.</param>
        /// <exception cref="SerializationException">Unable to resolve data contracts.</exception>
        public override object ReadObject(XmlReader reader, bool verifyObjectName)
        {
            if (reader == null) throw new ArgumentNullException("reader");

            return verifyObjectName && !IsStartObject(reader)
                ? null
                : ReadObject(reader);
        }

        /// <summary>
        /// Gets a value that specifies whether the XmlDictionaryReader is positioned over 
        /// an AMFX element that represents an object the serializer can deserialize from.
        /// </summary>
        /// <param name="reader">The XmlDictionaryReader used to read the AMFX stream mapped from AMF.</param>
        /// <returns><c>true</c> if the reader is positioned correctly; otherwise, <c>false</c>.</returns>
        public override bool IsStartObject(XmlDictionaryReader reader)
        {
            if (reader == null) throw new ArgumentNullException("reader");
            return IsStartObject((XmlReader)reader);
        }

        /// <summary>
        /// Gets a value that specifies whether the XmlReader is positioned over 
        /// an AMFX element that represents an object the serializer can deserialize from.
        /// </summary>
        /// <param name="reader">The XmlReader used to read the AMFX stream mapped from AMF.</param>
        /// <returns><c>true</c> if the reader is positioned correctly; otherwise, <c>false</c>.</returns>
        public override bool IsStartObject(XmlReader reader)
        {
            if (reader == null) throw new ArgumentNullException("reader");
            return reader.NodeType == XmlNodeType.Element && reader.Name == _type.Value.AmfxType;
        }
        #endregion

        #region Write methods
        /// <summary>
        /// Serializes a specified object to Action Message Format (AMF) data and writes the resulting AMF to a stream.
        /// </summary>
        /// <param name="stream">The Stream that is written to.</param>
        /// <param name="graph">The object that contains the data to write to the stream.</param>
        /// <exception cref="SerializationException">Unable to resolve data contracts.</exception>
        /// <exception cref="InvalidDataException">Error during encoding.</exception>
        public override void WriteObject(Stream stream, object graph)
        {
            if (stream == null) throw new ArgumentNullException("stream");
            if (!stream.CanWrite) throw new ArgumentException(Errors.DataContractAmfSerializer_WriteObject_InvalidStream, "stream");

            using (var buffer = new MemoryStream())
            {
                WriteObject(AmfxWriter.Create(buffer), graph);

                buffer.Position = 0;
                WriteObject(stream, AmfxReader.Create(buffer));
            }
        }

        /// <summary>
        /// Reads a specified Action Message Format in XML (AMFX) data and writes the resulting AMF data to a stream.
        /// </summary>
        /// <param name="stream">The Stream to be written.</param>
        /// <param name="input">AMFX reader.</param>
        /// <exception cref="SerializationException">Unable to resolve data contracts.</exception>
        /// <exception cref="InvalidDataException">Error during encoding.</exception>
        public void WriteObject(Stream stream, XmlReader input)
        {
            //Encode AMF packet
            if (_type.Value.AmfxType == AmfxContent.AmfxDocument)
            {
                var encoder = new AmfPacketEncoder(_encodingOptions);
                encoder.Encode(stream, input);
            }
            //Encode generic AMF data
            else
            {
                var encoder = CreateEncoder(_encodingOptions);
                encoder.Encode(stream, input);
            }
        }

        /// <summary>
        /// Serializes an object to XML that may be mapped to Action Message Format in XML (AMFX). 
        /// Writes all the object data, including the starting XML element, content, 
        /// and closing element, with an XmlWriter. 
        /// </summary>
        /// <param name="writer">XML writer.</param>
        /// <param name="graph">Object graph.</param>
        /// <exception cref="SerializationException">Unable to serialize data contracts.</exception>
        public override void WriteObject(XmlWriter writer, object graph)
        {
            var context = new SerializationContext(_encodingOptions.AmfVersion) { KnownTypes = _knownTypes };

            if (_type.Value.AmfxType == AmfxContent.AmfxDocument)
                SerializePacket(writer, graph, context); //Special case
            else
                Serialize(writer, graph, context);
        }

        /// <summary>
        /// Writes the XML content that can be mapped to Action Message Format (AMF) using an XmlDictionaryWriter.
        /// </summary>
        /// <param name="writer">The XmlDictionaryWriter used to write to.</param>
        /// <param name="graph">The object to write.</param>
        public override void WriteObjectContent(XmlDictionaryWriter writer, object graph)
        {
            WriteObjectContent((XmlWriter)writer, graph);
        }

        /// <summary>
        /// Writes the XML content that can be mapped to Action Message Format (AMF) using an XmlWriter.
        /// </summary>
        /// <param name="writer">The XmlWriter used to write to.</param>
        /// <param name="graph">The object to write.</param>
        public override void WriteObjectContent(XmlWriter writer, object graph)
        {
            WriteObject(writer, graph);
        }

        #region Obsolete methods
        public override void WriteStartObject(XmlDictionaryWriter writer, object graph)
        {
        }

        public override void WriteStartObject(XmlWriter writer, object graph)
        {
        }

        public override void WriteEndObject(XmlDictionaryWriter writer)
        {
        }

        public override void WriteEndObject(XmlWriter writer)
        {
        }
        #endregion
        #endregion

        #region Deserialization
        /// <summary>
        /// Deserialize an AMFX packet.
        /// </summary>
        /// <exception cref="SerializationException">Error during deserialization.</exception>
        private static object DeserializePacket(XmlReader reader, SerializationContext context)
        {
            if (reader == null) throw new ArgumentNullException("reader");
            if (context == null) throw new ArgumentNullException("context");

            var packet = new AmfPacket();

            while (reader.Read())
            {
                if (reader.NodeType != XmlNodeType.Element) continue;

                #region Read packet header
                if (reader.Name == AmfxContent.PacketHeader)
                {
                    var header = new AmfHeader();
                    var headerreader = reader.ReadSubtree();
                    headerreader.MoveToContent();

                    header.Name = headerreader.GetAttribute(AmfxContent.PacketHeaderName);
                    header.MustUnderstand = (headerreader.GetAttribute(AmfxContent.PacketHeaderMustUnderstand) == AmfxContent.True);

                    while (headerreader.Read())
                    {
                        //Skip until header content is found, if any
                        if (headerreader.NodeType != XmlNodeType.Element || headerreader.Name == AmfxContent.PacketHeader)
                            continue;

                        header.Data = Deserialize(headerreader, context);
                        break;
                    }

                    packet.Headers[header.Name] = header;
                    headerreader.Close();
                    continue;
                }
                #endregion

                #region Read packet body
                if (reader.Name == AmfxContent.PacketBody)
                {
                    var message = new AmfMessage();
                    var bodyreader = reader.ReadSubtree();
                    bodyreader.MoveToContent();

                    message.Target = bodyreader.GetAttribute(AmfxContent.PacketBodyTarget);
                    message.Response = bodyreader.GetAttribute(AmfxContent.PacketBodyResponse);

                    while (bodyreader.Read())
                    {
                        //Skip until body content is found, if any
                        if (bodyreader.NodeType != XmlNodeType.Element || bodyreader.Name == AmfxContent.PacketBody)
                            continue;

                        message.Data = Deserialize(bodyreader, context);
                        break;
                    }

                    packet.Messages.Add(message);
                    bodyreader.Close();
                    continue;
                }
                #endregion
            }

            return packet;
        }

        /// <summary>
        /// Deserialize AMFX data.
        /// </summary>
        /// <exception cref="SerializationException">Error during deserialization.</exception>
        private static object Deserialize(XmlReader reader, SerializationContext context)
        {
            if (reader == null) throw new ArgumentNullException("reader");
            if (context == null) throw new ArgumentNullException("context");
            if (reader.NodeType != XmlNodeType.Element) throw new XmlException(string.Format("Element node expected, {0} found.", reader.NodeType));

            #region Primitive values
            switch (reader.Name)
            {
                case AmfxContent.Null:
                    return null;

                case AmfxContent.True:
                    return true;

                case AmfxContent.False:
                    return false;
            }
            #endregion

            #region Complex values
            var nodereader = reader.ReadSubtree();
            nodereader.MoveToContent();

            object value;

            switch (nodereader.Name)
            {
                case AmfxContent.Integer:
                    value = ReadInteger(nodereader);
                    break;

                case AmfxContent.Double:
                    value = ReadDouble(nodereader);
                    break;

                case AmfxContent.String:
                    value = ReadString(nodereader, context);
                    break;

                case AmfxContent.Array:
                    value = ReadArray(nodereader, context);
                    break;

                case AmfxContent.ByteArray:
                    value = ReadByteArray(nodereader, context);
                    break;

                case AmfxContent.Date:
                    value = ReadDate(nodereader, context);
                    break;

                case AmfxContent.Xml:
                    value = ReadXml(nodereader, context);
                    break;

                case AmfxContent.Object:
                    value = ReadObject(nodereader, context);
                    break;

                case AmfxContent.Reference:
                    value = ReadReference(nodereader, context);
                    break;

                default:
                    throw new NotSupportedException("Unexpected AMFX type: " + nodereader.Name);
            }

            nodereader.Close();
            return value;
            #endregion
        }

        private static int ReadInteger(XmlReader reader)
        {
            return Convert.ToInt32(reader.ReadString());
        }

        private static double ReadDouble(XmlReader reader)
        {
            return Convert.ToDouble(reader.ReadString());
        }

        private static string ReadString(XmlReader reader, SerializationContext context)
        {
            if (reader.IsEmptyElement)
            {
                if (reader.AttributeCount > 0)
                {
                    var index = Convert.ToInt32(reader.GetAttribute(AmfxContent.StringId));
                    return context.StringReferences[index];
                }

                return string.Empty;
            }

            var text = reader.ReadString();
            context.StringReferences.Add(text);

            return text;
        }

        private static DateTime ReadDate(XmlReader reader, SerializationContext context)
        {
            var milliseconds = Convert.ToInt64(reader.ReadString());
            var origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            var offset = TimeSpan.FromMilliseconds(milliseconds);
            var result = origin + offset;

            context.References.Add(new AmfReference(result, AmfxContent.Date));

            return result;
        }

        private static XmlDocument ReadXml(XmlReader reader, SerializationContext context)
        {
            var encoded = reader.ReadString();
            var decoded = Convert.FromBase64String(encoded);
            var text = Encoding.UTF8.GetString(decoded);
            var result = new XmlDocument();
            result.LoadXml(text);

            context.References.Add(new AmfReference(result, AmfxContent.Xml));

            return result;
        }

        private static object ReadReference(XmlReader reader, SerializationContext context)
        {
            var index = Convert.ToInt32(reader.GetAttribute(AmfxContent.ReferenceId));
            return context.References[index];
        }

        private static object[] ReadArray(XmlReader reader, SerializationContext context)
        {
            var length = Convert.ToInt32(reader.GetAttribute(AmfxContent.ArrayLength));

            var result = new object[length];
            context.References.Add(new AmfReference(result, AmfxContent.Array));

            if (length == 0) return result;

            reader.MoveToContent();

            while (reader.Read())
            {
                if (reader.NodeType != XmlNodeType.Element)
                    continue;

                for (var i = 0; i < length; i++)
                {
                    var itemreader = reader.ReadSubtree();
                    itemreader.MoveToContent();
                    result[i] = Deserialize(itemreader, context);
                    itemreader.Close();
                    reader.Read();
                }
            }

            return result;
        }

        private static byte[] ReadByteArray(XmlReader reader, SerializationContext context)
        {
            var encoded = reader.ReadString();
            var bytes = Convert.FromBase64String(encoded);

            context.References.Add(new AmfReference(bytes, AmfxContent.ByteArray));

            return bytes;
        }

        private static object ReadObject(XmlReader reader, SerializationContext context)
        {
            var properties = new Dictionary<string, object>();

            var proxy = new object();
            context.References.Add(new AmfReference(proxy, AmfxContent.Object));

            AmfTypeTraits traits = null;
            var typeName = string.Empty;

            if (reader.HasAttributes)
                typeName = reader.GetAttribute(AmfxContent.ObjectType);

            #region Read traits
            while (reader.Read())
            {
                if (reader.NodeType != XmlNodeType.Element && reader.Name != AmfxContent.Traits) continue;

                if (!reader.IsEmptyElement)
                {
                    traits = new AmfTypeTraits { TypeName = typeName };
                    context.TraitsReferences.Add(traits);

                    var traitsReader = reader.ReadSubtree();
                    traitsReader.MoveToContent();
                    traitsReader.ReadStartElement();

                    var members = new List<string>();

                    while (reader.NodeType != XmlNodeType.EndElement)
                        members.Add(traitsReader.ReadElementContentAsString());

                    traits.ClassMembers = members.ToArray();
                    traitsReader.Close();
                }
                else
                {
                    var index = Convert.ToInt32(reader.GetAttribute(AmfxContent.TraitsId));
                    traits = context.TraitsReferences[index];
                }

                break;
            }

            if (traits == null) throw new SerializationException("Object traits not found.");
            #endregion

            #region Read members
            var i = 0;

            while (reader.Read())
            {
                if (reader.NodeType != XmlNodeType.Element) continue;

                var memberName = traits.ClassMembers[i];
                var memberReader = reader.ReadSubtree();
                memberReader.MoveToContent();
                var memberValue = Deserialize(memberReader, context);
                memberReader.Close();

                properties[memberName] = memberValue;
                i++;
            }
            #endregion

            #region Instantiate type
            object result;
            var objectindex = context.References.IndexOf(proxy);
            context.References.RemoveAt(objectindex);

            if (!string.IsNullOrEmpty(traits.TypeName))
            {
                if (!context.KnownTypes.ContainsKey(traits.TypeName))
                    throw new SerializationException(string.Format("Unable to find data contract for type alias '{0}'.", traits.TypeName));

                var typeDescriptor = context.KnownTypes[traits.TypeName];

                result = DataContractHelper.InstantiateContract(typeDescriptor.Type, properties, typeDescriptor.PropertyMap, typeDescriptor.FieldMap);
            }
            else
                result = properties;

            context.References.Insert(objectindex, new AmfReference(result));

            return result;
            #endregion
        }
        #endregion

        #region Serialization
        /// <summary>
        /// Serialize an AMFX packet.
        /// </summary>
        /// <exception cref="SerializationException">Error during serialization.</exception>
        private static void SerializePacket(XmlWriter writer, object graph, SerializationContext context)
        {
            if (writer == null) throw new ArgumentNullException("writer");
            if (context == null) throw new ArgumentNullException("context");
            if (graph == null) throw new ArgumentNullException("graph");

            var packet = graph as AmfPacket;
            if (packet == null) throw new SerializationException("Object is not an AmfPacket");

            writer.WriteStartDocument();
            writer.WriteStartElement(AmfxContent.AmfxDocument, AmfxContent.Namespace);
            writer.WriteAttributeString(AmfxContent.VersionAttribute, context.AmfVersion.ToAmfxName());

            //Write headers
            foreach (var header in packet.Headers.Values)
            {
                writer.WriteStartElement(AmfxContent.PacketHeader);
                writer.WriteAttributeString(AmfxContent.PacketHeaderName, header.Name);
                writer.WriteAttributeString(AmfxContent.PacketHeaderMustUnderstand, header.MustUnderstand.ToString());
                Serialize(writer, header.Data, context);
                writer.WriteEndElement();

                context.ResetReferences();
            }

            //Write bodies
            foreach (var body in packet.Messages)
            {
                writer.WriteStartElement(AmfxContent.PacketBody);
                writer.WriteAttributeString(AmfxContent.PacketBodyTarget, body.Target);
                writer.WriteAttributeString(AmfxContent.PacketBodyResponse, body.Response);
                Serialize(writer, body.Data, context);
                writer.WriteEndElement();

                context.ResetReferences();
            }

            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        /// <summary>
        /// Serialize AMFX data.
        /// </summary>
        /// <exception cref="SerializationException">Error during serialization.</exception>
        private static void Serialize(XmlWriter writer, object value, SerializationContext context)
        {
            if (writer == null) throw new ArgumentNullException("writer");
            if (context == null) throw new ArgumentNullException("context");

            //A null value
            if (value == null)
            {
                WriteEmptyElement(writer, AmfxContent.Null);
                return;
            }

            bool isDataContract;
            var type = value.GetType();
            var amfxtype = GetAmfxType(type, out isDataContract);

            //A data contract type
            if (amfxtype == AmfxContent.Object)
            {
                if (isDataContract)
                {
                    var alias = (from pair in context.KnownTypes
                                 where pair.Value.Type == type
                                 select pair.Key).FirstOrDefault();

                    if (alias == null)
                    {
                        throw new SerializationException(
                            string.Format(
                                "Unable to resolve type '{0}'. Check if type was registered within the serializer.",
                                type.FullName));
                    }
                }

                WriteDataContract(writer, value, type, context, isDataContract);
                return;
            }

            //Handle enums
            if (type.IsEnum)
            {
                WriteElement(writer, amfxtype, Convert.ToInt32(value).ToString());
                return;
            }

            switch (amfxtype)
            {
                case AmfxContent.Boolean:
                    WriteEmptyElement(writer, (bool)value ? AmfxContent.True : AmfxContent.False);
                    break;

                case AmfxContent.Integer:
                case AmfxContent.Double:
                    WriteElement(writer, amfxtype, value.ToString());
                    break;

                case AmfxContent.String:
                    WriteString(writer, value.ToString(), context);
                    break;

                case AmfxContent.Date:
                    WriteDate(writer, (DateTime)value, context);
                    break;

                case AmfxContent.ByteArray:
                    WriteByteArray(writer, (byte[])value, context);
                    break;

                case AmfxContent.Xml:
                    WriteXml(writer, (XmlDocument)value, context);
                    break;

                case AmfxContent.Array:
                    WriteArray(writer, (object[])value, context);
                    break;

                default:
                    throw new SerializationException(string.Format("Unable to serialize type '{0}'", type.FullName));
            }
        }

        /// <summary>
        /// Write an empty element.
        /// </summary>
        private static void WriteEmptyElement(XmlWriter writer, string elementName, IEnumerable<KeyValuePair<string, string>> attributes = null)
        {
            writer.WriteStartElement(elementName);

            if (attributes != null)
            {
                foreach (var pair in attributes)
                    writer.WriteAttributeString(pair.Key, pair.Value);
            }

            writer.WriteEndElement();
        }

        /// <summary>
        /// Write an element.
        /// </summary>
        private static void WriteElement(XmlWriter writer, string elementName, string value, IEnumerable<KeyValuePair<string, string>> attributes = null)
        {
            writer.WriteStartElement(elementName);

            if (attributes != null)
            {
                foreach (var pair in attributes)
                    writer.WriteAttributeString(pair.Key, pair.Value);
            }

            writer.WriteValue(value);
            writer.WriteEndElement();
        }

        /// <summary>
        /// Write a string.
        /// </summary>
        private static void WriteString(XmlWriter writer, string value, SerializationContext context)
        {
            int index;

            //Write a string
            if (value == string.Empty || context.AmfVersion != AmfVersion.Amf3 || (index = context.StringReferences.IndexOf(value)) == -1)
            {
                if (value != string.Empty)
                    context.StringReferences.Add(value);

                WriteElement(writer, AmfxContent.String, value);
            }
            //Write a string reference. Only in AMF+
            else
            {
                var attributes = new Dictionary<string, string> { { AmfxContent.StringId, index.ToString() } };
                WriteEmptyElement(writer, AmfxContent.String, attributes);
            }
        }

        /// <summary>
        /// Write a date.
        /// </summary>
        private static void WriteDate(XmlWriter writer, DateTime value, SerializationContext context)
        {
            int index;

            //Write a date
            if (context.AmfVersion != AmfVersion.Amf3 || (index = context.References.IndexOf(value)) == -1)
            {
                var timestamp = DataContractHelper.ConvertToTimestamp(value);
                context.References.Add(new AmfReference(value));
                WriteElement(writer, AmfxContent.Date, timestamp.ToString());
            }
            //Write a date reference. Only in AMF+
            else
            {
                var attributes = new Dictionary<string, string> { { AmfxContent.ReferenceId, index.ToString() } };
                WriteEmptyElement(writer, AmfxContent.Reference, attributes);
            }
        }

        /// <summary>
        /// Write an XML.
        /// </summary>
        private static void WriteXml(XmlWriter writer, XmlDocument value, SerializationContext context)
        {
            int index;

            //Write an XML
            if (context.AmfVersion != AmfVersion.Amf3 || (index = context.References.IndexOf(value)) == -1)
            {
                context.References.Add(new AmfReference(value));
                WriteElement(writer, AmfxContent.Xml, value.ToString());
            }
            //Write an XML reference. Only in AMF+
            else
            {
                var attributes = new Dictionary<string, string> { { AmfxContent.ReferenceId, index.ToString() } };
                WriteEmptyElement(writer, AmfxContent.Reference, attributes);
            }
        }

        /// <summary>
        /// Write a byte array.
        /// </summary>
        private static void WriteByteArray(XmlWriter writer, byte[] value, SerializationContext context)
        {
            int index;

            //Write a byte array
            if (context.AmfVersion != AmfVersion.Amf3 || (index = context.References.IndexOf(value)) == -1)
            {
                var data = Convert.ToBase64String(value);

                context.References.Add(new AmfReference(value));
                WriteElement(writer, AmfxContent.ByteArray, data);
            }
            //Write a byte array reference. Only in AMF+
            else
            {
                var attributes = new Dictionary<string, string> { { AmfxContent.ReferenceId, index.ToString() } };
                WriteEmptyElement(writer, AmfxContent.Reference, attributes);
            }
        }

        /// <summary>
        /// Write an array.
        /// </summary>
        private static void WriteArray(XmlWriter writer, object[] value, SerializationContext context)
        {
            var index = context.References.IndexOf(value);

            //Write an array
            if (index == -1)
            {
                context.References.Add(new AmfReference(value));

                writer.WriteStartElement(AmfxContent.Array);
                writer.WriteAttributeString(AmfxContent.ArrayLength, value.Length.ToString());

                foreach (var item in value)
                    Serialize(writer, item, context);

                writer.WriteEndElement();
            }
            //Write an array reference. Only in AMF+
            else
            {
                var attributes = new Dictionary<string, string> { { AmfxContent.ReferenceId, index.ToString() } };
                WriteEmptyElement(writer, AmfxContent.Reference, attributes);
            }
        }

        /// <summary>
        /// Write an object.
        /// </summary>
        private static void WriteDataContract(XmlWriter writer, object graph, Type type, SerializationContext context, bool isDataContract)
        {
            var index = context.References.IndexOf(graph);

            //Write object reference
            if (index != -1)
            {
                var attributes = new Dictionary<string, string> { { AmfxContent.ReferenceId, index.ToString() } };
                WriteEmptyElement(writer, AmfxContent.Reference, attributes);
                return;
            }

            Dictionary<string, object> properties;

            if (isDataContract)
            {
                var descriptor = (from pair in context.KnownTypes
                             where pair.Value.Type == type
                             select pair.Value).FirstOrDefault();

                if (descriptor == null)
                {
                    throw new SerializationException(
                        string.Format(
                            "Unable to resolve type '{0}'. Check if type was registered within the serializer.",
                            type.FullName));
                }

                var alias = descriptor.Alias;
                var traitsindex = context.TraitsIndex(alias);

                writer.WriteStartElement(AmfxContent.Object);

                if (!string.IsNullOrEmpty(alias))
                {
                    writer.WriteAttributeString(AmfxContent.ObjectType, alias);

                    if (traitsindex == -1)
                    {
                        var typeNameIndex = context.StringReferences.IndexOf(alias);
                        if (typeNameIndex == -1) context.StringReferences.Add(alias);
                    }
                }

                properties = DataContractHelper.GetContractProperties(graph, descriptor.PropertyMap, descriptor.FieldMap);

                //Write traits by reference
                if (context.AmfVersion == AmfVersion.Amf3 && traitsindex != -1)
                {
                    var attributes = new Dictionary<string, string> { { AmfxContent.TraitsId, traitsindex.ToString() } };
                    WriteEmptyElement(writer, AmfxContent.Traits, attributes);
                }
                //Write traits
                else
                {
                    var traits = new AmfTypeTraits { TypeName = alias, ClassMembers = properties.Keys.ToArray() };
                    context.TraitsReferences.Add(traits);

                    writer.WriteStartElement(AmfxContent.Traits);

                    foreach (var propertyName in properties.Keys)
                    {
                        WriteElement(writer, AmfxContent.String, propertyName);

                        var memberNameIndex = context.StringReferences.IndexOf(propertyName);
                        if (memberNameIndex == -1) context.StringReferences.Add(propertyName);
                    }

                    writer.WriteEndElement(); //End of traits
                }
            }
            else
            {
                var map = (IDictionary)graph;
                properties = new Dictionary<string, object>();

                foreach (var key in map.Keys)
                    properties[key.ToString()] = map[key];

                writer.WriteStartElement(AmfxContent.Object);

                writer.WriteStartElement(AmfxContent.Traits);

                foreach (var propertyName in properties.Keys)
                {
                    WriteElement(writer, AmfxContent.String, propertyName);
                    var memberNameIndex = context.StringReferences.IndexOf(propertyName);
                    if (memberNameIndex == -1) context.StringReferences.Add(propertyName);
                }

                writer.WriteEndElement(); //End of traits
            }

            foreach (var value in properties.Values)
                Serialize(writer, value, context);

            writer.WriteEndElement(); //End of object
        }
        #endregion

        #region Helper methods
        /// <summary>
        /// Create default AMF encoding options.
        /// </summary>
        static private AmfEncodingOptions CreateDefaultOptions()
        {
            return new AmfEncodingOptions
                       {
                           AmfVersion = AmfVersion.Amf3,
                           UseContextSwitch = false
                       };
        }

        /// <summary>
        /// Create AMF encoder.
        /// </summary>
        /// <param name="encodingOptions">AMF encoding options.</param>
        static private IAmfEncoder CreateEncoder(AmfEncodingOptions encodingOptions)
        {
            switch (encodingOptions.AmfVersion)
            {
                case AmfVersion.Amf0:
                    return new Amf0Encoder(encodingOptions);

                case AmfVersion.Amf3:
                    return new Amf3Encoder(encodingOptions);

                default:
                    throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Create AMF decoder.
        /// </summary>
        /// <param name="encodingOptions">AMF decoding options.</param>
        static private IAmfDecoder CreateDecoder(AmfEncodingOptions encodingOptions)
        {
            switch (encodingOptions.AmfVersion)
            {
                case AmfVersion.Amf0:
                    return new Amf0Decoder(encodingOptions);

                case AmfVersion.Amf3:
                    return new Amf3Decoder(encodingOptions);

                default:
                    throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Prepare a data contract.
        /// </summary>
        /// <param name="type">The type to prepare.</param>
        /// <returns>An alias-contract pair.</returns>
        /// <exception cref="InvalidDataContractException">Type does not conform to data contract rules.
        /// For example, the DataContractAttribute attribute has not been applied to the type.</exception>
        static private KeyValuePair<string, DataContractDescriptor> PrepareDataContract(Type type)
        {
            if (type == null) throw new ArgumentNullException("type");

            try
            {
                bool isDataContract;
                var amfxType = GetAmfxType(type, out isDataContract);
                var alias = isDataContract
                    ? DataContractHelper.GetContractAlias(type)
                    : type.FullName;

                var descriptor = new DataContractDescriptor
                {
                    Alias = alias,
                    Type = type,
                    IsPrimitive = !isDataContract,
                    AmfxType = amfxType
                };

                if (isDataContract)
                {
                    descriptor.FieldMap = DataContractHelper.GetContractFields(type);
                    descriptor.PropertyMap = DataContractHelper.GetContractProperties(type);
                }

                return new KeyValuePair<string, DataContractDescriptor>(alias, descriptor);
            }
            catch (Exception e)
            {
                throw new InvalidDataContractException(string.Format("Type '{0}' is not a valid data contract.", type.FullName), e);
            }
        }

        /// <summary>
        /// Prepare data contracts.
        /// </summary>
        /// <param name="knownTypes">The types that may be present in the object graph.</param>
        /// <returns>An alias-contract dictionary object.</returns>
        /// <exception cref="InvalidDataContractException">At least one of the types does not conform to data contract rules.
        /// For example, the DataContractAttribute attribute has not been applied to the type.</exception>
        static private Dictionary<string, DataContractDescriptor> PrepareDataContracts(IEnumerable<Type> knownTypes)
        {
            if (knownTypes == null) throw new ArgumentNullException("knownTypes");

            return knownTypes.Select(PrepareDataContract).ToDictionary(pair => pair.Key, pair => pair.Value);
        }

        /// <summary>
        /// Get AMFX type for a CLR type.
        /// </summary>
        static private string GetAmfxType(Type type, out bool isDataContract)
        {
            if (type == null) throw new ArgumentNullException("type");
            isDataContract = false;

            //A boolean value
            if (type == typeof(bool))
                return AmfxContent.Boolean;

            //A string
            if (type == typeof(string))
                return AmfxContent.String;

            //A date
            if (type == typeof(DateTime))
                return AmfxContent.Date;

            //Check if type is a number
            bool isInteger;
            if (DataContractHelper.IsNumericType(type, out isInteger))
                return isInteger ? AmfxContent.Integer : AmfxContent.Double;

            //An array
            if (type.IsArray)
            {
                return type == typeof(byte[])
                    ? AmfxContent.ByteArray
                    : AmfxContent.Array;
            }

            //An enumeration
            if (type.IsEnum)
                return AmfxContent.Integer;

            //An XML document
            if (type == typeof(XmlDocument))
                return AmfxContent.Xml;

            //A guid
            if (type == typeof(Guid))
                return AmfxContent.String;

            //A special case
            if (type == typeof(AmfPacket))
            {
                isDataContract = true;
                return AmfxContent.AmfxDocument;
            }

            //A dictionary
            if (type.IsGenericType && typeof(IDictionary).IsAssignableFrom(type))
                return AmfxContent.Object;

            //Probably a data contract
            isDataContract = true;
            return AmfxContent.Object;
        }
        #endregion

        #region Helper classes
        /// <summary>
        /// Data contract descriptor.
        /// </summary>
        sealed private class DataContractDescriptor
        {
            /// <summary>
            /// Data contract type's alias.
            /// </summary>
            public string Alias { get; set; }

            /// <summary>
            /// Data contract type type.
            /// </summary>
            public Type Type { get; set; }

            /// <summary>
            /// Type is a primitive type.
            /// </summary>
            public bool IsPrimitive { get; set; }

            /// <summary>
            /// Data contract type's AMFX type.
            /// </summary>
            public string AmfxType { get; set; }

            /// <summary>
            /// Data contract property map.
            /// </summary>
            public IEnumerable<KeyValuePair<string, PropertyInfo>> PropertyMap { get; set; }

            /// <summary>
            /// Data contract field map.
            /// </summary>
            public IEnumerable<KeyValuePair<string, FieldInfo>> FieldMap { get; set; }
        }

        /// <summary>
        /// AMFX serialization context.
        /// </summary>
        sealed private class SerializationContext : AmfContext
        {
            #region .ctor
            public SerializationContext(AmfVersion version)
                : base(version)
            {
            }
            #endregion

            #region Properties
            /// <summary>
            /// Contains the types that may be present in the object graph.
            /// </summary>
            public Dictionary<string, DataContractDescriptor> KnownTypes { get; set; }
            #endregion
        }
        #endregion
    }
}

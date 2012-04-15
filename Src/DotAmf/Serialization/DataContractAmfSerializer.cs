using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using DotAmf.Data;
using DotAmf.Decoder;

namespace DotAmf.Serialization
{
    /// <summary>
    /// Serializes objects to the Action Message Format (AMF) and deserializes AMF data to objects.
    /// </summary>
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
                var amfxwriter = new XmlTextWriter(buffer, Encoding.UTF8);

                ReadObject(stream, amfxwriter);

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
            if (_type.Value.AmfxType == AmfxContent.AmfxRoot)
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

            var context = new SerializationContext { KnownTypes = _knownTypes };

            return _type.Value.AmfxType == AmfxContent.AmfxRoot
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
        public override void WriteObject(Stream stream, object graph)
        {
            if (stream == null) throw new ArgumentNullException("stream");
            if (!stream.CanWrite) throw new ArgumentException(Errors.DataContractAmfSerializer_WriteObject_InvalidStream, "stream");

            throw new NotSupportedException();
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
            throw new NotSupportedException();
        }

        public override void WriteStartObject(XmlDictionaryWriter writer, object graph)
        {
            throw new NotSupportedException();
        }

        public override void WriteObjectContent(XmlDictionaryWriter writer, object graph)
        {
            throw new NotSupportedException();
        }

        public override void WriteEndObject(XmlDictionaryWriter writer)
        {
            throw new NotSupportedException();
        }
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
                        if (reader.NodeType != XmlNodeType.Element || reader.Name == AmfxContent.PacketHeader)
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
                        if (reader.NodeType != XmlNodeType.Element || reader.Name == AmfxContent.PacketBody)
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
            if(reader.IsEmptyElement)
            {
                if(reader.AttributeCount > 0)
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
            var reference = new ObjectProxy();
            context.References.Add(reference);

            var milliseconds = Convert.ToInt64(reader.ReadString());
            var origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            var offset = TimeSpan.FromMilliseconds(milliseconds);
            var result = origin + offset;

            reference.Reference = result;

            return result;
        }

        private static XmlDocument ReadXml(XmlReader reader, SerializationContext context)
        {
            var reference = new ObjectProxy();
            context.References.Add(reference);

            var encoded = reader.ReadString();
            var decoded = Convert.FromBase64String(encoded);
            var text = Encoding.UTF8.GetString(decoded);
            var result = new XmlDocument();
            result.LoadXml(text);

            reference.Reference = result;

            return result;
        }

        private static object ReadReference(XmlReader reader, SerializationContext context)
        {
            var index = Convert.ToInt32(reader.GetAttribute(AmfxContent.ReferenceId));
            return context.References[index].Reference;
        }

        private static object[] ReadArray(XmlReader reader, SerializationContext context)
        {
            var length = Convert.ToInt32(reader.GetAttribute(AmfxContent.ArrayLength));

            var result = new object[length];
            context.References.Add(new ObjectProxy(result));

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
                }
            }

            return result;
        }

        private static object ReadObject(XmlReader reader, SerializationContext context)
        {
            var properties = new Dictionary<string, object>();

            var proxy = new ObjectProxy();
            context.References.Add(proxy);

            AmfTypeTraits traits = null;
            var typeName = string.Empty;

            if(reader.HasAttributes)
                typeName = reader.GetAttribute(AmfxContent.ObjectType);

            #region Read traits
            while (reader.Read())
            {
                if (reader.NodeType != XmlNodeType.Element && reader.Name != AmfxContent.Traits) continue;

                if (!reader.IsEmptyElement)
                {
                    traits = new AmfTypeTraits{ TypeName = typeName };
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

            if(traits == null) throw new SerializationException("Object traits not found.");
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
            if (!string.IsNullOrEmpty(traits.TypeName))
            {
                if (!context.KnownTypes.ContainsKey(traits.TypeName))
                    throw new SerializationException(string.Format("Unable to find data contract for type alias '{0}'.", traits.TypeName));

                var typeDescriptor = context.KnownTypes[traits.TypeName];

                proxy.Reference = DataContractHelper.InstantiateContract(typeDescriptor.Type, properties);
            }
            else
                proxy.Reference = properties;
            #endregion

            return proxy.Reference;
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
                var alias = DataContractHelper.GetContractAlias(type);
                var descriptor = new DataContractDescriptor
                                     {
                                         Type = type,
                                         AmfxType = GetAmfxType(type)
                                     };

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
        static private string GetAmfxType(Type type)
        {
            if (type == null) throw new ArgumentNullException("type");

            //A boolean value
            if (type == typeof(bool))
                return AmfxContent.Boolean;

            //A string
            if (type == typeof(string))
                return AmfxContent.String;

            //A string
            if (type == typeof(DateTime))
                return AmfxContent.Date;

            //Check if type is a number
            bool isInteger;
            if (DataContractHelper.IsNumericType(type, out isInteger))
                return isInteger ? AmfxContent.Integer : AmfxContent.Double;

            //An array
            if (type.IsArray)
                return AmfxContent.Array;

            //An enumeration
            if (type.IsEnum)
                return AmfxContent.Integer;

            //An XML document
            if (type == typeof(XmlDocument))
                return AmfxContent.Xml;

            //A special case
            if (type == typeof(AmfPacket))
                return AmfxContent.AmfxRoot;

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
            /// Data contract type type.
            /// </summary>
            public Type Type { get; set; }

            /// <summary>
            /// Data contract type's AMFX type.
            /// </summary>
            public string AmfxType { get; set; }
        }

        /// <summary>
        /// Serialized object proxy.
        /// </summary>
        sealed private class ObjectProxy
        {
            #region .ctor
            public ObjectProxy()
            {}

            public ObjectProxy(object reference)
            {
                Reference = reference;
            }
            #endregion

            /// <summary>
            /// Actual object reference.
            /// </summary>
            public object Reference { get; set; }
        }

        /// <summary>
        /// AMFX serialization context.
        /// </summary>
        sealed private class SerializationContext
        {
            #region .ctor
            public SerializationContext()
            {
                References = new List<ObjectProxy>();
                StringReferences = new List<string>();
                TraitsReferences = new List<AmfTypeTraits>();
            }
            #endregion

            #region Properties
            /// <summary>
            /// Contains the types that may be present in the object graph.
            /// </summary>
            public Dictionary<string, DataContractDescriptor> KnownTypes { get; set; }

            /// <summary>
            /// Object references.
            /// </summary>
            public IList<ObjectProxy> References { get; private set; }

            /// <summary>
            /// String references.
            /// </summary>
            public IList<string> StringReferences { get; private set; }

            /// <summary>
            /// Traits references.
            /// </summary>
            public IList<AmfTypeTraits> TraitsReferences { get; private set; }
            #endregion
        }
        #endregion
    }
}

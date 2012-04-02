using System;
using System.IO;
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
    sealed public class DataContractAmfSerializer : XmlObjectSerializer
    {
        #region .ctor
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="context">AMF serialization context.</param>
        /// <param name="encodingOptions">AMF encoding options.</param>
        public DataContractAmfSerializer(AmfSerializationContext context, AmfEncodingOptions encodingOptions)
        {
            if (context == null) throw new ArgumentNullException("context");
            _contractResolver = CreateContractResolver(context);
            _contractDereferencer = CreateContractDereferencer(context);

            _encodingOptions = encodingOptions;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="context">AMF serialization context.</param>
        public DataContractAmfSerializer(AmfSerializationContext context)
            : this(context, new AmfEncodingOptions { AmfVersion = AmfVersion.Amf3 })
        {
        }
        #endregion

        #region Data
        /// <summary>
        /// Data contract resolver.
        /// </summary>
        private readonly DataContractSerializer _contractResolver;

        /// <summary>
        /// Data contract dereferencer.
        /// </summary>
        private readonly DataContractSerializer _contractDereferencer;

        /// <summary>
        /// Encoding options.
        /// </summary>
        private readonly AmfEncodingOptions _encodingOptions;
        #endregion

        #region Public methods
        /// <summary>
        /// Reads a document stream in the AMF (Action Message Format) format 
        /// and returns the deserialized object.
        /// </summary>
        /// <remarks>
        /// Typed AMF object with no matching data contracts will be converted to
        /// <c>IDictionary&lt;string,object&gt;</c> objects. You can resolve data contracts
        /// for these object later by calling the <c>ResolveContracts</c> method 
        /// on the object graph within an appropriate AMF serialization context.
        /// </remarks>
        /// <param name="stream">AMF data stream.</param>
        /// <returns>Object graph.</returns>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="ArgumentException">Stream does not allow reading or seeking.</exception>
        /// <exception cref="IOException">Error reading AMF data.</exception>
        /// <exception cref="SerializationException">Unable to deserialize data contracts.</exception>
        public override object ReadObject(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException("stream");
            if (!stream.CanRead) throw new ArgumentException(Errors.DataContractAmfSerializer_ReadObject_InvalidStream, "stream");

            object graph;

            try
            {
                //Read raw AMF data into an object graph
                using (var reader = new AmfStreamReader(stream))
                {
                    var decoder = CreateDecoder(reader, _encodingOptions);

                    try
                    {
                        decoder.ContextSwitch += OnDecoderContextSwitch;
                        graph = decoder.ReadValue();
                    }
                    finally
                    {
                        decoder.ContextSwitch -= OnDecoderContextSwitch;
                    }
                }
            }
            catch (Exception e)
            {
                throw new IOException(Errors.DataContractAmfSerializer_ReadObject_ErrorReadingAmf, e);
            }

            return ResolveContracts(graph);
        }

        /// <summary>
        /// Serializes a specified object to AMF (Action Message Format) data 
        /// and writes the resulting AMF to a stream.
        /// </summary>
        /// <param name="stream">Stream to write AMF data.</param>
        /// <param name="graph">Object graph.</param>
        /// <exception cref="ArgumentNullException">No stream provided.</exception>
        /// <exception cref="ArgumentException">Stream does not allow writing.</exception>
        public override void WriteObject(Stream stream, object graph)
        {
            if (stream == null) throw new ArgumentNullException("stream");

            //Check if stream is valid
            if (!stream.CanWrite)
                throw new ArgumentException(Errors.DataContractAmfSerializer_WriteObject_InvalidStream, "stream");

            try
            {
                using (var writer = new AmfStreamWriter(stream))
                {
                    var encoder = CreateEncoder(writer, _encodingOptions);

                    try
                    {
                        encoder.ContextSwitch += OnEncoderContextSwitch;
                        encoder.WriteValue(graph);
                    }
                    finally
                    {
                        encoder.ContextSwitch -= OnEncoderContextSwitch;
                    }
                }
            }
            catch (Exception e)
            {
                throw new IOException(Errors.DataContractAmfSerializer_WriteObject_ErrorWritingAmf, e);
            }
        }

        /// <summary>
        /// Reads the XML document mapped from AMF (Action Message Format) 
        /// with an XmlReader and returns the deserialized object.
        /// </summary>
        /// <param name="reader">XML reader.</param>
        /// <returns>Object graph.</returns>
        /// <exception cref="SerializationException">Unable to deserialize data contracts.</exception>
        public override object ReadObject(XmlReader reader)
        {
            return ReadObject(reader, _contractResolver);
        }

        /// <summary>
        /// Serializes an object to XML that may be mapped to Action Message Format (AMF). 
        /// Writes all the object data, including the starting XML element, content, 
        /// and closing element, with an XmlWriter. 
        /// </summary>
        /// <param name="writer">XML writer.</param>
        /// <param name="graph">Object graph.</param>
        /// <exception cref="SerializationException">Unable to serialize data contracts.</exception>
        public override void WriteObject(XmlWriter writer, object graph)
        {
            WriteObject(writer, graph, _contractResolver);
        }

        /// <summary>
        /// Resolve data contracts in untyped or partly unresolved objects graph.
        /// </summary>
        /// <param name="graph">Objects graph.</param>
        /// <exception cref="SerializationException">Unable to resolve data contracts.</exception>
        public object ResolveContracts(object graph)
        {
            if (graph == null) return null;

            //Nothing to resolve here
            if (graph.GetType().IsValueType) return graph;

            object result;

            try
            {
                using (var ms = new MemoryStream())
                {
                    //Serialize AMF objects into proxy contracts
                    var writer = new XmlTextWriter(ms, Encoding.UTF8);
                    WriteObject(writer, graph, _contractResolver);
                    writer.Flush();

                    //Then try to deserialize using current contracts
                    ms.Position = 0;

                    //Custom XML reader is required in order to bypass
                    //XML namespace constraints during deserialization
                    var reader = new AmfXmlTextReader(ms);
                    result = ReadObject(reader, _contractResolver);
                    reader.Close();
                }
            }
            catch (Exception e)
            {
                throw new SerializationException(Errors.DataContractAmfSerializer_ReadObject_ContractsError, e);
            }

            return result;
        }

        /// <summary>
        /// Dereference data contracts into untyped objects graph.
        /// </summary>
        /// <param name="graph">Objects graph.</param>
        /// <exception cref="SerializationException">Unable to dereference data contracts.</exception>
        public object DereferenceContracts(object graph)
        {
            if (graph == null) return null;

            //Nothing to dereference here
            if (graph.GetType().IsValueType) return graph;

            object result;

            try
            {
                using (var ms = new MemoryStream())
                {
                    //Serialize AMF objects into proxy contracts
                    var writer = new XmlTextWriter(ms, Encoding.UTF8);
                    WriteObject(writer, graph, _contractDereferencer);
                    writer.Flush();

                    //Then try to deserialize using current contracts
                    ms.Position = 0;

                    //Custom XML reader is required in order to bypass
                    //XML namespace constraints during deserialization
                    var reader = new AmfXmlTextReader(ms);
                    result = ReadObject(reader, _contractDereferencer);
                    reader.Close();
                }
            }
            catch (Exception e)
            {
                throw new SerializationException(Errors.DataContractAmfSerializer_ReadObject_ContractsError, e);
            }

            return result;
        }
        #endregion

        #region Private methods
        static private object ReadObject(XmlReader reader, DataContractSerializer serializer)
        {
            try
            {
                return serializer.ReadObject(reader);
            }
            catch (Exception e)
            {
                throw new SerializationException(Errors.DataContractAmfSerializer_ReadObject_ContractsError, e);
            }
        }

        static private void WriteObject(XmlWriter writer, object graph, DataContractSerializer serializer)
        {
            try
            {
                serializer.WriteObject(writer, graph);
            }
            catch (Exception e)
            {
                throw new SerializationException(Errors.DataContractAmfSerializer_WriteObject_ContractsError, e);
            }
        }
        #endregion

        #region Abstract methods implementation
        public override bool IsStartObject(XmlDictionaryReader reader)
        {
            throw new NotSupportedException();
        }

        public override object ReadObject(XmlDictionaryReader reader, bool verifyObjectName)
        {
            throw new NotSupportedException();
        }

        public override void WriteEndObject(XmlDictionaryWriter writer)
        {
            throw new NotSupportedException();
        }

        public override void WriteObjectContent(XmlDictionaryWriter writer, object graph)
        {
            throw new NotSupportedException();
        }

        public override void WriteStartObject(XmlDictionaryWriter writer, object graph)
        {
            throw new NotSupportedException();
        }
        #endregion

        #region Factories
        /// <summary>
        /// Create data contract resolver.
        /// </summary>
        /// <param name="context">Serialization context.</param>
        static private DataContractSerializer CreateContractResolver(AmfSerializationContext context)
        {
            return new DataContractSerializer(
                typeof(object),
                AmfDataContractDereferencer.KnownTypes,
                int.MaxValue,
                false,
                true,
                new AmfDataContractResolver.ProxySurrogate(context.CreateProxyObject),
                context.ContractResolver
            );
        }

        /// <summary>
        /// Create data contract dereferencer.
        /// </summary>
        /// <param name="context">Serialization context.</param>
        static private DataContractSerializer CreateContractDereferencer(AmfSerializationContext context)
        {
            return new DataContractSerializer(
                typeof(object),
                AmfDataContractDereferencer.KnownTypes,
                int.MaxValue,
                false,
                true,
                new AmfDataContractDereferencer.DereferencedSurrogate(context.CreateDereferencedObject),
                context.ContractDereferencer
            );
        }

        /// <summary>
        /// Create AMF decoder.
        /// </summary>
        static private IAmfDecoder CreateDecoder(BinaryReader reader, AmfEncodingOptions encodingOptions)
        {
            switch (encodingOptions.AmfVersion)
            {
                case AmfVersion.Amf0:
                    return new Amf0Decoder(reader, encodingOptions);

                case AmfVersion.Amf3:
                    return new Amf3Decoder(reader, encodingOptions);

                default:
                    throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Create AMF encoder.
        /// </summary>
        static private IAmfEncoder CreateEncoder(BinaryWriter writer, AmfEncodingOptions encodingOptions)
        {
            switch (encodingOptions.AmfVersion)
            {
                case AmfVersion.Amf0:
                    return new Amf0Encoder(writer, encodingOptions);

                case AmfVersion.Amf3:
                    return new Amf3Encoder(writer, encodingOptions);

                default:
                    throw new NotSupportedException();
            }
        }
        #endregion

        #region Event handlers
        static private void OnDecoderContextSwitch(object sender, EncodingContextSwitchEventArgs e)
        {
            var decoder = (IAmfDecoder)sender;
            decoder.ClearReferences();
        }

        static private void OnEncoderContextSwitch(object sender, EncodingContextSwitchEventArgs e)
        {
            var encoder = (IAmfEncoder)sender;
            encoder.ClearReferences();
        }
        #endregion

        #region AMF XML reader
        /// <summary>
        /// AMF XML reader.
        /// </summary>
        sealed private class AmfXmlTextReader : XmlTextReader
        {
            #region .ctor
            public AmfXmlTextReader(Stream stream)
                : base(stream)
            { }
            #endregion

            #region Overriden methods
            public override bool IsStartElement(string localname, string ns)
            {
                var nodeType = MoveToContent();

                //Check if node type is valid
                if (nodeType != XmlNodeType.Element) return false;

                //Don't perform namespace checks for AMF types
                if (NamespaceURI == AmfSerializationContext.AmfNamespace)
                    return (LocalName == localname);

                //Do the regular routine for all other types
                return (LocalName == localname && NamespaceURI == ns);
            }
            #endregion
        }
        #endregion
    }
}

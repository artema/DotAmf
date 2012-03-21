using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace DotAmf.Data
{
    /// <summary>
    /// AMF object.
    /// </summary>
    [Serializable]
    [XmlRoot("AmfObject")]
    public class AmfObject : ISerializable, IDictionary<string, object>, IXmlSerializable
    {
        #region .ctor
        /// <summary>
        /// Constructs a newtyped  AMF object.
        /// </summary>
        /// <param name="typeName">Object's type.</param>
        public AmfObject(string typeName)
            : this(typeName, new Dictionary<string, object>())
        {
        }

        /// <summary>
        /// Constructs an anonymous object.
        /// </summary>
        public AmfObject()
            : this(string.Empty)
        { }

        /// <summary>
        /// Constructs a newtyped  AMF object with provided properties collection.
        /// </summary>
        /// <param name="typeName">Object's type.</param>
        /// <param name="properties">Collection to use for storing object's properties.</param>
        public AmfObject(string typeName, IDictionary<string, object> properties)
        {
            if (typeName == null) throw new ArgumentNullException("typeName");
            if (typeName == BaseTypeName) typeName = string.Empty;
            TypeName = typeName;

            if (properties == null) throw new ArgumentNullException("properties");
            _properties = properties;
        }
        #endregion

        #region Constants
        /// <summary>
        /// Base type name.
        /// </summary>
        private const string BaseTypeName = "Object";
        #endregion

        #region Properties
        /// <summary>
        /// Object's properties.
        /// </summary>
        public IDictionary<string, object> Properties { get { return _properties; } }

        /// <summary>
        /// Type name.
        /// </summary>
        public string TypeName { get; private set; }
        #endregion

        #region Data
        /// <summary>
        /// Object's properties.
        /// </summary>
        private readonly IDictionary<string, object> _properties;
        #endregion

        #region Indexer
        /// <summary>
        /// Get or set object's property by name.
        /// </summary>
        /// <param name="key">Property name.</param>
        /// <remarks>Changing this property is effectively
        /// the same as changing the <c>Properties</c> collection.</remarks>
        public object this[string key]
        {
            get { return _properties[key]; }
            set { _properties[key] = value; }
        }
        #endregion

        #region IEnumerable implementation
        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return Properties.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        #endregion

        #region ISerializable implementation
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            foreach (var pair in Properties)
                info.AddValue(pair.Key, pair.Value);

            info.FullTypeName = TypeName != string.Empty
                ? TypeName
                : BaseTypeName;
        }
        #endregion

        #region IXmlSerializable implementation
        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            var keySerializer = new XmlSerializer(typeof(string));
            var valueSerializer = new XmlSerializer(typeof(object));
            var wasEmpty = reader.IsEmptyElement;
            reader.Read();

            if (wasEmpty) return;

            while (reader.NodeType != XmlNodeType.EndElement)
            {
                reader.ReadStartElement("item");
                reader.ReadStartElement("key");

                var key = (string)keySerializer.Deserialize(reader);
                reader.ReadEndElement();
                reader.ReadStartElement("value");

                var value = valueSerializer.Deserialize(reader);
                reader.ReadEndElement();

                Properties.Add(key, value);
                reader.ReadEndElement();
                reader.MoveToContent();
            }

            reader.ReadEndElement();
        }

        public void WriteXml(XmlWriter writer)
        {
            var keySerializer = new XmlSerializer(typeof(string));
            var valueSerializer = new XmlSerializer(typeof(object));

            foreach (var key in Properties.Keys)
            {
                writer.WriteStartElement("item");
                writer.WriteStartElement("key");
                keySerializer.Serialize(writer, key);
                writer.WriteEndElement();
                writer.WriteStartElement("value");

                var value = Properties[key];
                valueSerializer.Serialize(writer, value);
                writer.WriteEndElement();
                writer.WriteEndElement();
            }
        }
        #endregion

        #region IDictionary implementation
        public void Add(string key, object value)
        {
            Properties.Add(key, value);
        }

        public bool ContainsKey(string key)
        {
            return Properties.ContainsKey(key);
        }

        public ICollection<string> Keys
        {
            get { return Properties.Keys; }
        }

        public bool Remove(string key)
        {
            return Properties.Remove(key);
        }

        public bool TryGetValue(string key, out object value)
        {
            return Properties.TryGetValue(key, out value);
        }

        public ICollection<object> Values
        {
            get { return Properties.Values; }
        }

        public void Add(KeyValuePair<string, object> item)
        {
            Properties.Add(item);
        }

        public void Clear()
        {
            Properties.Clear();
        }

        public bool Contains(KeyValuePair<string, object> item)
        {
            return Properties.Contains(item);
        }

        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            Properties.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return Properties.Count; }
        }

        public bool IsReadOnly
        {
            get { return Properties.IsReadOnly; }
        }

        public bool Remove(KeyValuePair<string, object> item)
        {
            return Properties.Remove(item);
        }
        #endregion
    }

    /// <summary>
    /// AMF+ object.
    /// </summary>
    [Serializable]
    [XmlRoot("AmfPlusObject")]
    sealed public class AmfPlusObject : AmfObject
    {
        #region .ctor
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="traits">Type traits.</param>
        public AmfPlusObject(AmfTypeTraits traits)
            : base(traits.TypeName, BuildDictionary(traits))
        {
            if (traits == null) throw new ArgumentNullException("traits");
            Traits = traits;
        }

        /// <summary>
        /// Constructs an anonymous object.
        /// </summary>
        public AmfPlusObject()
            : this(new AmfTypeTraits())
        {
        }

        /// <summary>
        /// Constructs a newtyped  AMF+ object with provided properties collection.
        /// </summary>
        /// <param name="typeName">Object's type.</param>
        /// <param name="properties">Properties to use.</param>
        public AmfPlusObject(string typeName, IDictionary<string, object> properties)
            : base(typeName, properties)
        {
            Traits = new AmfTypeTraits(typeName, properties.Select(pair => pair.Key));
        }
        #endregion

        #region Properties
        /// <summary>
        /// Type traits.
        /// </summary>
        public AmfTypeTraits Traits { get; private set; }
        #endregion

        #region Private methods
        /// <summary>
        /// Build a dictionary that will store object's properties.
        /// </summary>
        static private IDictionary<string, object> BuildDictionary(AmfTypeTraits traits)
        {
            if (traits == null) throw new ArgumentNullException("traits");

            //No need to use any constraints
            if (traits.IsDynamic) return new Dictionary<string, object>();

            //Externizable objects have no properties
            if (traits.IsExternalizable) return new ConstrainedDictionary(new string[0]);

            return new ConstrainedDictionary(traits.ClassMembers);
        }
        #endregion
    }

    /// <summary>
    /// AMF+ type's traits.
    /// </summary>
    [Serializable]
    sealed public class AmfTypeTraits : ISerializable
    {
        #region .ctor
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="typeName">Fully qualified type name.</param>
        /// <param name="classMembers">A list of type members.</param>
        /// <param name="isDynamic">Type if dynamic.</param>
        public AmfTypeTraits(string typeName, IEnumerable<string> classMembers, bool isDynamic = false)
        {
            if (typeName == null) throw new ArgumentNullException("typeName");
            TypeName = typeName;

            if (classMembers == null) throw new ArgumentNullException("classMembers");
            ClassMembers = classMembers;

            IsExternalizable = false;
            IsDynamic = isDynamic;
        }

        /// <summary>
        /// Constructs a trait object for an externizable type.
        /// </summary>
        /// <param name="typeName">Fully qualified type name.</param>
        /// <param name="isDynamic">Type if dynamic.</param>
        public AmfTypeTraits(string typeName, bool isDynamic = false)
            : this(typeName, new HashSet<string>())
        {
            IsExternalizable = true;
            IsDynamic = isDynamic;
        }

        /// <summary>
        /// Constructs a trait object for an anonymous type.
        /// </summary>
        public AmfTypeTraits()
            : this(string.Empty, true)
        {
            IsExternalizable = false;
        }
        #endregion

        #region Properties
        /// <summary>
        /// Fully qualified type name.
        /// </summary>
        public string TypeName { get; private set; }

        /// <summary>
        /// A list of type members.
        /// </summary>
        public IEnumerable<string> ClassMembers { get; private set; }

        /// <summary>
        /// Type is externalizable.
        /// </summary>
        public bool IsExternalizable { get; private set; }

        /// <summary>
        /// Type is dynamic.
        /// </summary>
        public bool IsDynamic { get; private set; }
        #endregion

        #region ISerializable implementation
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("TypeName", TypeName);
            info.AddValue("IsExternalizable", IsExternalizable);
            info.AddValue("IsDynamic", IsDynamic);
            info.AddValue("ClassMembers", string.Join(", ", ClassMembers.ToArray()));
        }
        #endregion
    }

    #region IExternizable
    /// <summary>
    /// Provides control over serialization of a type 
    /// as it is encoded into a data stream.
    /// </summary>
    public interface IExternalizable
    {
        /// <summary>
        /// Type name.
        /// </summary>
        string TypeName { get; }

        /// <summary>
        /// Decode itself from a data stream.
        /// </summary>
        void ReadExternal(Stream input);

        /// <summary>
        /// Encode itself for a data stream.
        /// </summary>
        /// <returns></returns>
        void WriteExternal(Stream ouput);
    }
    #endregion
}

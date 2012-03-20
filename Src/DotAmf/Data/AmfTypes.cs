using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;

namespace DotAmf.Data
{
    /// <summary>
    /// AMF object.
    /// </summary>
    [Serializable]
    public class AmfObject : ISerializable, IEnumerable<KeyValuePair<string, object>>
    {
        #region .ctor
        /// <summary>
        /// Constructs a newtyped  AMF object.
        /// </summary>
        /// <param name="typeName">Object's type.</param>
        public AmfObject(string typeName)
        {
            if (typeName == null) throw new ArgumentNullException("typeName");
            if (typeName == BaseTypeName) typeName = string.Empty;
            TypeName = typeName;

            _properties = new Dictionary<string, object>();
        }

        /// <summary>
        /// Constructs an anonymous object.
        /// </summary>
        public AmfObject()
            : this(string.Empty)
        { }
        #endregion

        #region Constants
        /// <summary>
        /// Base type name.
        /// </summary>
        private const string BaseTypeName = "Object";
        #endregion

        #region Properties
        /// <summary>
        /// Type name.
        /// </summary>
        public string TypeName { get; private set; }
        #endregion

        #region Data
        /// <summary>
        /// Object's properties.
        /// </summary>
        private readonly Dictionary<string, object> _properties;
        #endregion

        #region ISerializable implementation
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            foreach (var pair in this)
                info.AddValue(pair.Key, pair.Value);

            info.FullTypeName = TypeName != string.Empty
                ? TypeName
                : BaseTypeName;
        }
        #endregion

        #region Indexer
        /// <summary>
        /// Get or set object's property by name.
        /// </summary>
        /// <param name="key">Property name.</param>
        public virtual object this[string key]
        {
            get { return _properties[key]; }
            set { _properties[key] = value; }
        }
        #endregion

        #region IEnumerable implementation
        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return _properties.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        #endregion
    }

    /// <summary>
    /// AMF+ object.
    /// </summary>
    [Serializable]
    sealed public class AmfPlusObject : AmfObject
    {
        #region .ctor
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="traits">Type traits.</param>
        public AmfPlusObject(AmfTypeTraits traits)
            : base(traits.TypeName)
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
        #endregion

        #region Properties
        /// <summary>
        /// Type traits.
        /// </summary>
        public AmfTypeTraits Traits { get; private set; }
        #endregion

        #region Indexer
        public override object this[string key]
        {
            get { return base[key]; }
            set
            {
                if (Traits.IsExternalizable)
                    throw new InvalidOperationException(Errors.SettingPropertyOnExternizableType);

                if (!Traits.IsDynamic && !Traits.ClassMembers.Contains(key))
                    throw new ArgumentException(string.Format(Errors.SettingMissingProperty, key));

                base[key] = value;
            }
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
}

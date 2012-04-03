using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;

namespace DotAmf.Data
{
    /// <summary>
    /// AMF object.
    /// </summary>
    [DataContract(Namespace = "http://dotamf.net/")]
    sealed public class AmfObject : IDictionary<string,object>
    {
        #region Constants
        /// <summary>
        /// Externizable data property name.
        /// </summary>
        public const string ExternizableProperty = "$IExternalizable";
        #endregion

        #region Properties
        /// <summary>
        /// Object's properties.
        /// </summary>
        [DataMember]
        public Dictionary<string, object> Properties { get; set; }

        /// <summary>
        /// Type traits.
        /// </summary>
        [DataMember]
        public AmfTypeTraits Traits { get; set; }
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
            Properties.Add(item.Key, item.Value);
        }

        public void Clear()
        {
            Properties.Clear();
        }

        public bool Contains(KeyValuePair<string, object> item)
        {
            return Properties.ContainsValue(item.Value);
        }

        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            throw new NotSupportedException();
        }

        public int Count
        {
            get { return Properties.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(KeyValuePair<string, object> item)
        {
            return Properties.Remove(item.Key);
        }
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
            get { return Properties[key]; }
            set { Properties[key] = value; }
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
    }

    /// <summary>
    /// AMF+ type's traits.
    /// </summary>
    [DataContract(Namespace = "http://dotamf.net/")]
    sealed public class AmfTypeTraits
    {
        #region Constants
        /// <summary>
        /// Base type alias.
        /// </summary>
        public const string BaseTypeAlias = "";
        #endregion

        #region Properties
        /// <summary>
        /// Fully qualified type name.
        /// </summary>
        [DataMember]
        public string TypeName { get; set; }

        /// <summary>
        /// A list of type members.
        /// </summary>
        [DataMember]
        public string[] ClassMembers { get; set; }

        /// <summary>
        /// Type is externalizable.
        /// </summary>
        [DataMember]
        public bool IsExternalizable { get; set; }

        /// <summary>
        /// Type is dynamic.
        /// </summary>
        [DataMember]
        public bool IsDynamic { get; set; }
        #endregion
    }

    /// <summary>
    /// AMF externalizable data.
    /// </summary>
    [DataContract(Namespace = "http://dotamf.net/")]
    sealed public class AmfExternalizable
    {
        #region Properties
        /// <summary>
        /// Type name.
        /// </summary>
        public string TypeName { get; set; }

        /// <summary>
        /// Data.
        /// </summary>
        [DataMember]
        public byte[] Data { get; set; }
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

using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;

namespace DotAmf.Data
{
    /// <summary>
    /// AMF object.
    /// </summary>
    [DataContract(Namespace = "http://dotamf.net/")]
    sealed public class AmfObject : Dictionary<string,object>
    {
        #region .ctor
        public AmfObject()
        {}

        public AmfObject(IDictionary<string,object> dictionary)
            : base(dictionary)
        {}
        #endregion
        
        #region Constants
        /// <summary>
        /// Externizable data property name.
        /// </summary>
        public const string ExternizableProperty = "$IExternalizable";
        #endregion

        #region Properties
        /// <summary>
        /// Type traits.
        /// </summary>
        [DataMember]
        public AmfTypeTraits Traits { get; set; }
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

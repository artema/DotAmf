using System;
using System.Collections.Generic;

namespace DotAmf.Data
{
    /// <summary>
    /// AMF object.
    /// </summary>
    public class AmfObject : Dictionary<string,object>
    {
        #region .ctor
        /// <summary>
        /// Constructor.
        /// </summary>
        public AmfObject()
        {}

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="properties">Object's properties map.</param>
        public AmfObject(IDictionary<string,object> properties)
        {
            if (properties == null) throw new ArgumentNullException("properties");

            foreach (var pair in properties)
                this[pair.Key] = pair.Value;
        }
        #endregion

        /// <summary>
        /// Replace object's properties.
        /// </summary>
        /// <param name="newPoperties">New properties.</param>
        public void ReplaceProperties(Dictionary<string,object> newPoperties)
        {
            if (newPoperties == null) throw new ArgumentNullException("newPoperties");

            Clear();

            foreach (var pair in newPoperties)
                this[pair.Key] = pair.Value;
        }

        /// <summary>
        /// Type traits.
        /// </summary>
        public AmfTypeTraits Traits { get; set; }
    }

    /// <summary>
    /// Strongly-typed object.
    /// </summary>
    sealed public class TypedAmfObject : AmfObject
    {
        #region .ctor
        public TypedAmfObject(string className)
        {
            if (string.IsNullOrEmpty(className)) throw new ArgumentException("className");
            ClassName = className;
        }

        public TypedAmfObject(string className, Dictionary<string, object> properties)
            : base(properties)
        {
            if (string.IsNullOrEmpty(className)) throw new ArgumentException("className");
            ClassName = className;
        }
        #endregion

        /// <summary>
        /// Class name.
        /// </summary>
        public string ClassName { get; private set; }
    }

    /// <summary>
    /// AMF type's traits.
    /// </summary>
    sealed public class AmfTypeTraits
    {
        #region .ctor
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="typeName">Fully qualified type name.</param>
        /// <param name="classMembers">A list of type members.</param>
        public AmfTypeTraits(string typeName, IEnumerable<string> classMembers)
        {
            TypeName = typeName ?? string.Empty;

            if (classMembers == null) throw new ArgumentNullException("classMembers");
            ClassMembers = classMembers;
        }

        /// <summary>
        /// Constructs an object with no class members.
        /// </summary>
        /// <param name="typeName">Fully qualified type name.</param>
        public AmfTypeTraits(string typeName)
            : this(typeName, new List<string>())
        {
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
        public bool IsExternalizable { get; set; }

        /// <summary>
        /// Type is dynamic.
        /// </summary>
        public bool IsDynamic { get; set; }
        #endregion
    }

    /// <summary>
    /// ECMA array.
    /// </summary>
    sealed public class AmfEcmaArray : List<object>
    {
        #region .ctor
        /// <summary>
        /// Constructor.
        /// </summary>
        public AmfEcmaArray()
        {
            AssociativeValues = new AmfObject();
        }
        #endregion

        /// <summary>
        /// Array's associative values.
        /// </summary>
        public AmfObject AssociativeValues { get; private set; }
    }
}

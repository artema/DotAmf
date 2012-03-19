using System;
using System.Collections.Generic;

namespace DotAmf.Data
{
    /// <summary>
    /// AMF object.
    /// </summary>
    [Serializable]
    public class AmfObject : Dictionary<string, object>
    {
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

        /// <summary>
        /// Type traits.
        /// </summary>
        public AmfTypeTraits Traits { get; private set; }
    }

    /// <summary>
    /// Strongly-typed AMF object.
    /// </summary>
    /// <remarks><c>TypedAmfObject</c> should be used only in AMF0 content.
    /// For AMF+ context use <c>AmfPlusObject</c>.</remarks>
    /// <see cref="AmfPlusObject"/>
    [Serializable]
    sealed public class TypedAmfObject : AmfObject
    {
        #region .ctor
        public TypedAmfObject(string className)
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
    /// AMF+ type's traits.
    /// </summary>
    [Serializable]
    sealed public class AmfTypeTraits
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
            if (typeName == null) throw new ArgumentException("Invalid type name.", "typeName");
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
            : this(typeName, new List<string>())
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
    }
}

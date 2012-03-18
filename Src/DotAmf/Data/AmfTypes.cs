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
            if (string.IsNullOrEmpty(typeName)) throw new ArgumentException("Invalid type name.", "typeName");
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
            : this("Object", true)
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

    /// <summary>
    /// AMF+ array.
    /// </summary>
    /// <remarks><c>AmfPlusArray</c> should be used only in AMF+ content.
    /// For AMF0 context use any <c>IEnumerable</c> of <c>object</c>.</remarks>
    [Serializable]
    sealed public class AmfPlusArray : AmfObject, IList<object>
    {
        #region .ctor
        /// <summary>
        /// Constructor.
        /// </summary>
        public AmfPlusArray()
        {
            _list = new List<object>();
        }
        #endregion

        #region Data
        /// <summary>
        /// Wrapped list.
        /// </summary>
        private readonly List<object> _list;
        #endregion

        #region IList implementation
        public int IndexOf(object item)
        {
            return _list.IndexOf(item);
        }

        public void Insert(int index, object item)
        {
            _list.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            _list.RemoveAt(index);
        }

        public object this[int index]
        {
            get { return _list[index]; }
            set { _list[index] = value; }
        }

        public void Add(object item)
        {
            _list.Add(item);
        }

        public bool Contains(object item)
        {
            return _list.Contains(item);
        }

        public void CopyTo(object[] array, int arrayIndex)
        {
            _list.CopyTo(array, arrayIndex);
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(object item)
        {
            return _list.Remove(item);
        }

        public new IEnumerator<object> GetEnumerator()
        {
            return _list.GetEnumerator();
        }
        #endregion
    }
}

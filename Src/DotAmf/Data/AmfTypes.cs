using System;
using System.Collections.Generic;

namespace DotAmf.Data
{
    /// <summary>
    /// Anonymous object.
    /// </summary>
    public class AnonymousObject : Dictionary<string,object>
    {
        #region .ctor
        public AnonymousObject()
        {}

        public AnonymousObject(Dictionary<string,object> properties)
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

        public Traits Traits { get; set; }
    }

    /// <summary>
    /// Strongly-typed object.
    /// </summary>
    sealed public class TypedObject : AnonymousObject
    {
        #region .ctor
        public TypedObject(string className)
        {
            if (string.IsNullOrEmpty(className)) throw new ArgumentException("className");
            ClassName = className;
        }

        public TypedObject(string className, Dictionary<string, object> properties)
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
    /// Object's traits.
    /// </summary>
    sealed public class Traits
    {
        public string Type { get; set; }

        public int Count { get; set; }

        public bool IsExternalizable { get; set; }

        public bool IsDynamic { get; set; }

        public IList<string> ClassMembers { get; set; }
    }

    /// <summary>
    /// Associative array.
    /// </summary>
    sealed public class AssociativeArray : List<object>
    {
        #region .ctor
        public AssociativeArray()
        {
            AssociativeValues = new AnonymousObject();
        }
        #endregion

        /// <summary>
        /// Array's associative values.
        /// </summary>
        public AnonymousObject AssociativeValues { get; private set; }
    }
}

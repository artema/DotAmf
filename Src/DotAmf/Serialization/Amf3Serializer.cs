using System;
using System.Collections.Generic;
using System.IO;
using DotAmf.Data;

namespace DotAmf.Serialization
{
    /// <summary>
    /// AMF3 serializer.
    /// </summary>
    public class Amf3Serializer : Amf0Serializer
    {
        #region .ctor
        public Amf3Serializer(BinaryWriter writer)
            : base(writer)
        {
            _stringReferences = new List<string>();
            _traitReferences = new List<AmfTypeTraits>();
        }
        #endregion

        #region Data
        /// <summary>
        /// Strings references.
        /// </summary>
        private readonly IList<string> _stringReferences;

        /// <summary>
        /// Object traits references.
        /// </summary>
        private readonly IList<AmfTypeTraits> _traitReferences;
        #endregion

        #region References
        /// <summary>
        /// Save string to a list of string references.
        /// </summary>
        /// <param name="value">String to save or <c>null</c></param>
        private void SaveReference(string value)
        {
            _stringReferences.Add(value);
        }

        /// <summary>
        /// Save object traits to a list of traits references.
        /// </summary>
        /// <param name="value">Traits to save or <c>null</c></param>
        private void SaveReference(AmfTypeTraits value)
        {
            _traitReferences.Add(value);
        }
        #endregion

        #region IAmfSerializer implementation
        override public void ClearReferences()
        {
            base.ClearReferences();

            _stringReferences.Clear();
            _traitReferences.Clear();
        }

        public override int WriteValue(object value)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}

using System.Collections.Generic;

namespace DotAmf.Data
{
    /// <summary>
    /// AMF context.
    /// </summary>
    class AmfContext
    {
        #region .ctor
        public AmfContext(AmfVersion version)
        {
            AmfVersion = version;

            References = new List<AmfReference>();
            StringReferences = new List<string>();
            TraitsReferences = new List<AmfTypeTraits>();
        }
        #endregion

        #region Properties
        /// <summary>
        /// AMF version.
        /// </summary>
        public AmfVersion AmfVersion { get; private set; }

        /// <summary>
        /// Object references.
        /// </summary>
        public List<AmfReference> References { get; private set; }

        /// <summary>
        /// String references.
        /// </summary>
        public List<string> StringReferences { get; private set; }

        /// <summary>
        /// Traits references.
        /// </summary>
        public List<AmfTypeTraits> TraitsReferences { get; private set; }
        #endregion

        #region Public methods
        /// <summary>
        /// Get a traits index.
        /// </summary>
        public int TraitsIndex(string alias)
        {
            for (var i = 0; i < TraitsReferences.Count; i++)
            {
                if (TraitsReferences[i].TypeName == alias)
                    return i;
            }

            return -1;
        }

        /// <summary>
        /// Reset reference counter.
        /// </summary>
        public void ResetReferences()
        {
            References.Clear();
            StringReferences.Clear();
            TraitsReferences.Clear();
        }
        #endregion
    }

    /// <summary>
    /// AMF reference.
    /// </summary>
    struct AmfReference
    {
        #region Properties
        /// <summary>
        /// Object reference.
        /// </summary>
        public object Reference;

        /// <summary>
        /// AMFX type name.
        /// </summary>
        public string AmfxType;
        #endregion
    }

    #region Extension
    /// <summary>
    /// AMF encoding context extension.
    /// </summary>
    static class AmfEncodingContextExtension
    {
        /// <summary>
        /// Get reference index.
        /// </summary>
        static public int IndexOf(this List<AmfReference> list, object reference)
        {
            if (reference == null) return -1;

            for (var i = 0; i < list.Count; i++)
            {
                var proxy = list[i];

                if (proxy.Reference == null) continue;
                if (list[i].Reference == reference) return i;
            }

            return -1;
        }

        /// <summary>
        /// Track a reference.
        /// </summary>
        static public void Track(this IList<AmfReference> list)
        {
            list.Add(default(AmfReference));
        }
    }
    #endregion
}

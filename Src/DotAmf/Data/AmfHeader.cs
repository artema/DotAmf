using System;

namespace DotAmf.Data
{
    /// <summary>
    /// AMF header.
    /// </summary>
    [Serializable]
    sealed public class AmfHeader
    {
        /// <summary>
        /// A remote operation or method to be invoked by this header.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Client must understand this header or handle an error if he can't.
        /// </summary>
        public bool MustUnderstand { get; set; }

        /// <summary>
        /// A data associated with the header.
        /// </summary>
        public object Data { get; set; }
    }
}

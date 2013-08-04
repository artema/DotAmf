// Copyright (c) 2012 Artem Abashev (http://abashev.me)
// All rights reserved.
// Licensed under the Microsoft Public License (Ms-PL)
// http://opensource.org/licenses/ms-pl.html

namespace DotAmf.Data
{
    /// <summary>
    /// AMF header.
    /// </summary>
    sealed public class AmfHeader
    {
        #region .ctor
        public AmfHeader()
        {}

        public AmfHeader(AmfHeaderDescriptor descriptor)
        {
            Name = descriptor.Name;
            MustUnderstand = descriptor.MustUnderstand;
        }
        #endregion

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

    /// <summary>
    /// AMF header descriptor.
    /// </summary>
    public struct AmfHeaderDescriptor
    {
        /// <summary>
        /// A remote operation or method to be invoked by this header.
        /// </summary>
        public string Name;

        /// <summary>
        /// Client must understand this header or handle an error if he can't.
        /// </summary>
        public bool MustUnderstand;
    }
}

// Copyright (c) 2012 Artem Abashev (http://abashev.me)
// All rights reserved.
// Licensed under the Microsoft Public License (Ms-PL)
// http://opensource.org/licenses/ms-pl.html

namespace DotAmf.Data
{
    /// <summary>
    /// AMF serialization options.
    /// </summary>
    public struct AmfEncodingOptions
    {
        /// <summary>
        /// AMF version.
        /// </summary>
        public AmfVersion AmfVersion;

        /// <summary>
        /// Use AMF context switch.
        /// </summary>
        public bool UseContextSwitch;
    }
}

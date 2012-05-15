// Copyright (c) 2012 Artem Abashev (http://abashev.me)
// All rights reserved.
// Licensed under the Microsoft Public License (Ms-PL)
// http://opensource.org/licenses/ms-pl.html

using System;

namespace DotAmf.Data
{
    /// <summary>
    /// AMF packet version.
    /// </summary>
    public enum AmfVersion : ushort
    {
        /// <summary>
        /// AMF0.
        /// </summary>
        Amf0 = 0,

        /// <summary>
        /// AMF3.
        /// </summary>
        Amf3 = 3
    }

    #region Extension
    static internal class AmfVersionExtension
    {
        /// <summary>
        /// Convert version enumeration value to an AMFX value.
        /// </summary>
        static public string ToAmfxName(this AmfVersion value)
        {
            switch(value)
            {
                case AmfVersion.Amf0:
                    return AmfxContent.VersionAmf0;

                case AmfVersion.Amf3:
                    return AmfxContent.VersionAmf3;

                default:
                    throw new NotSupportedException("Version '" + value + "' is not supported.");
            }
        }
    }
    #endregion
}

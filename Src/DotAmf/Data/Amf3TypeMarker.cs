// Copyright (c) 2012 Artem Abashev (http://abashev.me)
// All rights reserved.
// Licensed under the Microsoft Public License (Ms-PL)
// http://opensource.org/licenses/ms-pl.html

using System;

namespace DotAmf.Data
{
    /// <summary>
    /// AMF3 data type marker.
    /// </summary>
    internal enum Amf3TypeMarker : byte
    {
        /// <summary>
        /// An undefined value.
        /// </summary>
        Undefined = 0x00,

        /// <summary>
        /// Null value.
        /// </summary>
        Null = 0x01,

        /// <summary>
        /// Boolean false value.
        /// </summary>
        False = 0x02,

        /// <summary>
        /// Boolean true value.
        /// </summary>
        True = 0x03,

        /// <summary>
        /// A variable length unsigned 29-bit integer.
        /// </summary>
        Integer = 0x04,

        /// <summary>
        /// An 8 byte double precision floating point value.
        /// </summary>
        Double = 0x05,

        /// <summary>
        /// A UTF-8 string or string reference.
        /// </summary>
        String = 0x06,

        /// <summary>
        /// An XML document.
        /// </summary>
        XmlDocument = 0x07,

        /// <summary>
        /// A date/time value.
        /// </summary>
        Date = 0x08,

        /// <summary>
        /// An array.
        /// </summary>
        Array = 0x09,

        /// <summary>
        /// An object.
        /// </summary>
        Object = 0x0A,

        /// <summary>
        /// An XML value.
        /// </summary>
        Xml = 0x0B,

        /// <summary>
        /// An array of bytes.
        /// </summary>
        ByteArray = 0x0C
    }

    #region Extension
    static internal class Amf3TypeMarkerExtension
    {
        /// <summary>
        /// Convert type marker to an AMFX type name.
        /// </summary>
        static public string ToAmfxName(this Amf3TypeMarker value)
        {
            switch (value)
            {
                case Amf3TypeMarker.Null:
                case Amf3TypeMarker.Undefined:
                    return AmfxContent.Null;

                case Amf3TypeMarker.False:
                    return AmfxContent.False;

                case Amf3TypeMarker.True:
                    return AmfxContent.True;

                case Amf3TypeMarker.Integer:
                    return AmfxContent.Integer;

                case Amf3TypeMarker.Double:
                    return AmfxContent.Double;

                case Amf3TypeMarker.String:
                    return AmfxContent.String;

                case Amf3TypeMarker.Date:
                    return AmfxContent.Date;

                case Amf3TypeMarker.ByteArray:
                    return AmfxContent.ByteArray;

                case Amf3TypeMarker.Xml:
                case Amf3TypeMarker.XmlDocument:
                    return AmfxContent.Xml;

                case Amf3TypeMarker.Array:
                    return AmfxContent.Array;

                case Amf3TypeMarker.Object:
                    return AmfxContent.Object;

                default:
                    throw new NotSupportedException("Type '" + value + "' is not supported.");
            }
        }
    }
    #endregion
}

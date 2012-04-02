namespace DotAmf.Data
{
    /// <summary>
    /// AMF0 data type marker.
    /// </summary>
    internal enum Amf0TypeMarker : byte
    {
        /// <summary>
        /// A 8 byte double precision floating point value.
        /// </summary>
        Number = 0x00,

        /// <summary>
        /// Boolean value.
        /// </summary>
        Boolean = 0x01,

        /// <summary>
        /// An UTF-8 string up to 65535 bytes.
        /// </summary>
        String = 0x02,

        /// <summary>
        /// An anonymous object.
        /// </summary>
        Object = 0x03,

        /// <summary>
        /// A movie clip. Not supported. Reserved for future use.
        /// </summary>
        MovieClip = 0x04,

        /// <summary>
        /// Null value.
        /// </summary>
        Null = 0x05,

        /// <summary>
        /// An undefined value.
        /// </summary>
        Undefined = 0x06,

        /// <summary>
        /// A reference to a complex object.
        /// </summary>
        Reference = 0x07,

        /// <summary>
        /// An associative array with string indices.
        /// </summary>
        EcmaArray = 0x08,

        /// <summary>
        /// A marker that signals the end of a set of object properties.
        /// </summary>
        ObjectEnd = 0x09,

        /// <summary>
        /// A regular array with integer indices.
        /// </summary>
        StrictArray = 0x0A,

        /// <summary>
        /// A date/time value.
        /// </summary>
        Date = 0x0B,

        /// <summary>
        /// An UTF-8 string that occupies more than 65535 bytes.
        /// </summary>
        LongString = 0x0C,

        /// <summary>
        /// Unsopported type marker.
        /// </summary>
        Unsupported = 0x0D,

        /// <summary>
        /// Record set. Not supported. Reserved for future use.
        /// </summary>
        RecordSet = 0x0E,

        /// <summary>
        /// An XML document.
        /// </summary>
        XmlDocument = 0x0F,

        /// <summary>
        /// A strongly typed object.
        /// </summary>
        TypedObject = 0x10,

        /// <summary>
        /// A marker that signifies that the following Object is formatted in AMF3.
        /// </summary>
        AvmPlusObject = 0x11
    }
}

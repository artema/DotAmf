namespace DotAmf.Data
{
    /// <summary>
    /// AMF3 data type marker.
    /// </summary>
    public enum Amf3TypeMarker : byte
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
}

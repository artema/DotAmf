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

namespace DotAmf.ServiceModel.Configuration
{
    /// <summary>
    /// AMF endpoint capabilities.
    /// </summary>
    internal struct AmfEndpointCapabilities
    {
        /// <summary>
        /// Messaging version.
        /// </summary>
        public uint MessagingVersion;

        /// <summary>
        /// Include exception details in faults.
        /// </summary>
        public bool ExceptionDetailInFaults;
    }
}

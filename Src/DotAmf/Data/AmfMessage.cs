namespace DotAmf.Data
{
    /// <summary>
    /// AMF message.
    /// </summary>
    sealed public class AmfMessage
    {
        #region .ctor
        public AmfMessage()
        {}

        public AmfMessage(AmfMessageDescriptor descriptor)
        {
            Target = descriptor.Target;
            Response = descriptor.Response;
        }
        #endregion

        /// <summary>
        /// An operation, function, or method is to be remotely invoked.
        /// </summary>
        public string Target { get; set; }

        /// <summary>
        /// A method on the local client that should be invoked to handle the response.
        /// </summary>
        public string Response { get; set; }

        /// <summary>
        /// A data associated with the operation.
        /// </summary>
        public object Data { get; set; }
    }

    /// <summary>
    /// AMF message descriptor.
    /// </summary>
    public struct AmfMessageDescriptor
    {
        /// <summary>
        /// An operation, function, or method is to be remotely invoked.
        /// </summary>
        public string Target;

        /// <summary>
        /// A method on the local client that should be invoked to handle the response.
        /// </summary>
        public string Response;
    }
}

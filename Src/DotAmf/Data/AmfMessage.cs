using System;

namespace DotAmf.Data
{
    /// <summary>
    /// AMF message.
    /// </summary>
    [Serializable]
    sealed public class AmfMessage : ICloneable
    {
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

        #region IClonable implementation
        public object Clone()
        {
            return new AmfMessage
                       {
                           Target = Target,
                           Response = Response,
                           Data = Data
                       };
        }
        #endregion
    }
}

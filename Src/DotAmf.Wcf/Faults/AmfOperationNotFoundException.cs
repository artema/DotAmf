using System;

namespace DotAmf.ServiceModel.Faults
{
    /// <summary>
    /// AMF operation not found.
    /// </summary>
    class AmfOperationNotFoundException : Exception
    {
        #region .ctor
        public AmfOperationNotFoundException(string operationName)
            :this(null, operationName)
        {
        }

        public AmfOperationNotFoundException(string message, string operationName)
            : base(message)
        {
            if (string.IsNullOrEmpty(operationName)) throw new ArgumentException("Invalid operation name.", "operationName");
            OperationName = operationName;
        }
        #endregion

        /// <summary>
        /// Operation name.
        /// </summary>
        public string OperationName { get; set; }
    }
}

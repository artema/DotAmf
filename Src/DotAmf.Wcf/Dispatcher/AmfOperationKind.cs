namespace DotAmf.ServiceModel.Dispatcher
{
    /// <summary>
    /// AMF operation kinds.
    /// </summary>
    static internal class AmfOperationKind
    {
        /// <summary>
        /// Batch operation.
        /// </summary>
        public const string Batch = "@AmfBatchOperation";

        /// <summary>
        /// Command.
        /// </summary>
        public const string Command = "@AmfCommand";

        /// <summary>
        /// Fault.
        /// </summary>
        public const string Fault = "@AmfFault";
    }
}

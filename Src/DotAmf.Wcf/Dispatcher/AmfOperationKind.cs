// Copyright (c) 2012 Artem Abashev (http://abashev.me)
// All rights reserved.
// Licensed under the Microsoft Public License (Ms-PL)
// http://opensource.org/licenses/ms-pl.html

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

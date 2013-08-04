// Copyright (c) 2012 Artem Abashev (http://abashev.me)
// All rights reserved.
// Licensed under the Microsoft Public License (Ms-PL)
// http://opensource.org/licenses/ms-pl.html

using System.Collections.Generic;
using System.Runtime.Serialization;

namespace DotAmf.ServiceModel.Messaging
{
    /// <summary>
    /// The ErrorMessage class is used to report errors within the messaging system. 
    /// An error message only occurs in response to a message sent within the system. 
    /// </summary>
    [DataContract(Name = "flex.messaging.messages.ErrorMessage")]
    sealed public class ErrorMessage : AcknowledgeMessage
    {
        /// <summary>
        /// The fault code for the error.
        /// This value typically follows the convention of
        /// "[outer_context].[inner_context].[issue]".
        /// For example: "Channel.Connect.Failed", "Server.Call.Failed", etc.
        /// </summary>
        [DataMember(Name = "faultCode")]
        public string FaultCode { get; set; }

        /// <summary>
        /// A simple description of the error.
        /// </summary>
        [DataMember(Name = "faultString")]
        public string FaultString { get; set; }

        /// <summary>
        /// Detailed description of what caused the error.
        /// This is typically a stack trace from the remote destination.
        /// </summary>
        [DataMember(Name = "faultDetail")]
        public string FaultDetail { get; set; }

        /// <summary>
        /// Should a root cause exist for the error, this property contains those details.
        /// This may be an ErrorMessage, a NetStatusEvent info Object, or an underlying
        /// Flash error event: ErrorEvent, IOErrorEvent, or SecurityErrorEvent.
        /// </summary>
        [DataMember(Name = "rootCause")]
        public object RootCause { get; set; }

        /// <summary>
        /// Extended data that the remote destination has chosen to associate
        /// with this error to facilitate custom error processing on the client.
        /// </summary>
        [DataMember(Name = "extendedData")]
        public object ExtendedData { get; set; }
    }

    /// <summary>
    /// Error message fault codes.
    /// </summary>
    static public class ErrorMessageFaultCode
    {
        /// <summary>
        /// If a message may not have been delivered, the <c>faultCode</c> will
        /// contain this constant. 
        /// </summary>
        public const string DeliveryInDoubt = "Client.Error.DeliveryInDoubt";

        /// <summary>
        /// Header name for the retryable hint header.
        /// This is used to indicate that the operation that generated the error
        /// may be retryable rather than fatal.
        /// </summary>
        public const string RetryableErrorHint = "DSRetryableErrorHint";
    }
}

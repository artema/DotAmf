// Copyright (c) 2012 Artem Abashev (http://abashev.me)
// All rights reserved.
// Licensed under the Microsoft Public License (Ms-PL)
// http://opensource.org/licenses/ms-pl.html

namespace DotAmf.ServiceModel.Messaging
{
    /// <summary>
    /// AMF messaging headers.
    /// </summary>
    static public class MessagingHeaders
    {
        /// <summary>
        /// AMF message's body that invoked the current operation.
        /// </summary>
        public const string InvokerMessageBody = "AmfMessageBody";

        /// <summary>
        /// AMF message's headers that invoked the current operation.
        /// </summary>
        public const string InvokerMessageHeaders = "AmfMessageHeaders";

        /// <summary>
        /// A header that contains a <c>RemotingMessage</c> that invoked current RPC operation.
        /// </summary>
        /// <remarks>Available only during RPC calls.</remarks>
        public const string RemotingMessage = "AmfRemotingMessage";
    }
}

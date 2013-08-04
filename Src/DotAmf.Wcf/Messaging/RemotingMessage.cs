// Copyright (c) 2012 Artem Abashev (http://abashev.me)
// All rights reserved.
// Licensed under the Microsoft Public License (Ms-PL)
// http://opensource.org/licenses/ms-pl.html

using System.Runtime.Serialization;

namespace DotAmf.ServiceModel.Messaging
{
    /// <summary>
    /// RemotingMessages are used to send RPC requests to a remote endpoint. 
    /// These messages use the operation property to specify which method to call 
    /// on the remote object. The destination property indicates what object/service 
    /// should be used. 
    /// </summary>
    [DataContract(Name = "flex.messaging.messages.RemotingMessage")]
    sealed public class RemotingMessage : AbstractMessage
    {
        /// <summary>
        /// Provides access to the name of the remote method/operation 
        /// that should be called.
        /// </summary>
        [DataMember(Name = "operation")]
        public string Operation { get; set; }
    }
}

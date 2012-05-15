// Copyright (c) 2012 Artem Abashev (http://abashev.me)
// All rights reserved.
// Licensed under the Microsoft Public License (Ms-PL)
// http://opensource.org/licenses/ms-pl.html

using System.Runtime.Serialization;

namespace DotAmf.ServiceModel.Messaging
{
    /// <summary>
    /// The CommandMessage class provides a mechanism for sending commands to the server infrastructure, 
    /// such as commands related to publish/subscribe messaging scenarios, ping operations, and cluster operations. 
    /// </summary>
    [DataContract(Name = "flex.messaging.messages.CommandMessage")]
    sealed public class CommandMessage : AbstractMessage
    {
        /// <summary>
        /// Provides access to the operation/command for the CommandMessage.
        /// </summary>
        [DataMember(Name = "operation")]
        public CommandMessageOperation Operation { get; set; }
    }

    /// <summary>
    /// Command message header names.
    /// </summary>
    static public class CommandMessageHeader
    {
        /// <summary>
        /// Endpoints can imply what features they support by reporting the
        /// latest version of messaging they are capable of during the handshake of
        /// the initial ping CommandMessage.
        /// </summary>
        public const string MessagingVersion = "DSMessagingVersion";

        /// <summary>
        /// Subscribe commands issued by a Consumer pass the Consumer's <code>selector</code>
        /// expression in this header.
        /// </summary>
        public const string Selector = "DSSelector";

        /// <summary>
        /// Durable JMS subscriptions are preserved when an unsubscribe message
        /// has this parameter set to true in its header.
        /// </summary>
        public const string PreserveDurable = "DSPreserveDurable";

        /// <summary>
        /// Header to indicate that the Channel needs the configuration from the server.
        /// </summary>
        public const string NeedsConfig = "DSNeedsConfig";

        /// <summary>
        /// Header used in a MULTI_SUBSCRIBE message to specify an Array of subtopic/selector
        /// pairs to add to the existing set of subscriptions.
        /// </summary>
        public const string AddSubscriptions = "DSAddSub";

        /// <summary>
        /// Like the <c>AddSub</c>, but specifies the subtopic/selector array of to remove.
        /// </summary>
        public const string RemSubscriptions = "DSRemSub";

        /// <summary>
        /// Header to drive an idle wait time before the next client poll request.
        /// </summary>
        public const string PollWait = "DSPollWait";

        /// <summary>
        /// Header to suppress poll response processing. If a client has a long-poll
        /// parked on the server and issues another poll, the response to this subsequent poll
        /// should be tagged with this header in which case the response is treated as a
        /// no-op and the next poll will not be scheduled. Without this, a subsequent poll
        /// will put the channel and endpoint into a busy polling cycle.
        /// </summary>
        public const string NoOpPoll = "DSNoOpPoll";

        /// <summary>
        /// Header to specify which character set encoding was used while encoding login credentials. 
        /// </summary>
        public const string CredentialsCharset = "DSCredentialsCharset";

        /// <summary>
        /// Header to indicate the maximum number of messages a Consumer wants to receive per second.
        /// </summary>
        public const string MaxFrequency = "DSMaxFrequency";

        /// <summary>
        /// Header that indicates the message is a heartbeat.
        /// </summary>
        public const string Heartbeat = "DS<3";
    }

    /// <summary>
    /// Command message operation.
    /// </summary>
    [DataContract(Name = "flex.messaging.messages.CommandMessageOperation")]
    public enum CommandMessageOperation
    {
        /// <summary>
        /// This operation is used to subscribe to a remote destination.
        /// </summary>
        [EnumMember]
        Subscribe = 0,

        /// <summary>
        /// This operation is used to unsubscribe from a remote destination.
        /// </summary>
        [EnumMember]
        Unsubscribe = 1,

        /// <summary>
        /// This operation is used to poll a remote destination 
        /// for pending, undelivered messages.
        /// </summary>
        [EnumMember]
        Poll = 2,

        /// <summary>
        /// This operation is used by a remote destination to sync missed 
        /// or cached messages back to a client as a result of a client issued poll command.
        /// </summary>
        [EnumMember]
        ClientSync = 4,

        /// <summary>
        /// This operation is used to test connectivity 
        /// over the current channel to the remote endpoint.
        /// </summary>
        [EnumMember]
        ClientPing = 5,

        /// <summary>
        /// This operation is used to request a list of failover endpoint URIs 
        /// for the remote destination based on cluster membership.
        /// </summary>
        [EnumMember]
        ClusterRequest = 7,

        /// <summary>
        /// This operation is used to send credentials to the endpoint 
        /// so that the user can be logged in over the current channel.
        /// </summary>
        [EnumMember]
        Login = 8,

        /// <summary>
        /// This operation is used to log the user out of the current channel,
        /// and will invalidate the server session if the channel is HTTP based.
        /// </summary>
        [EnumMember]
        Logout = 9,

        /// <summary>
        /// This operation is used to indicate that the client's subscription 
        /// with a remote destination has timed out.
        /// </summary>
        [EnumMember]
        SubscriptionInvalidate = 10,

        /// <summary>
        /// Used by the MultiTopicConsumer to subscribe/unsubscribe 
        /// for more than one topic in the same message.
        /// </summary>
        [EnumMember]
        MultiSubscribe = 11,

        /// <summary>
        /// This operation is used to indicate that a channel has disconnected.
        /// </summary>
        [EnumMember]
        Disconnect = 12,

        /// <summary>
        /// This operation is used to trigger a ChannelSet to connect.
        /// </summary>
        [EnumMember]
        TriggerConnect = 13,

        /// <summary>
        /// Unknown operation.
        /// </summary>
        [EnumMember]
        Unknown = 1000
    }
}

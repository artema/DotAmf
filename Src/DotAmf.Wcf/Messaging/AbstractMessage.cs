// Copyright (c) 2012 Artem Abashev (http://abashev.me)
// All rights reserved.
// Licensed under the Microsoft Public License (Ms-PL)
// http://opensource.org/licenses/ms-pl.html

using System.Collections.Generic;
using System.Runtime.Serialization;

namespace DotAmf.ServiceModel.Messaging
{
    /// <summary>
    /// Abstract base class for all messages. Messages have two customizable sections; headers and body. 
    /// The headers property provides access to specialized meta information for a specific message instance. 
    /// The headers property is an associative array with the specific header name as the key. 
    /// The body of a message contains the instance specific data that needs to be delivered 
    /// and processed by the remote destination. The body is an object and is the payload for a message. 
    /// </summary>
    [DataContract(Name = "flex.messaging.messages.AbstractMessage")]
    abstract public class AbstractMessage
    {
        /// <summary>
        /// The body of a message contains the specific data 
        /// that needs to be delivered to the remote destination.
        /// </summary>
        [DataMember(Name = "body")]
        public object Body { get; set; }

        /// <summary>
        /// The headers of a message are an associative array 
        /// where the key is the header name and the value is the header value. 
        /// This property provides access to the specialized meta information 
        /// for the specific message instance. Core header names begin with a 'DS' prefix. 
        /// Custom header names should start with a unique prefix to avoid name collisions. 
        /// </summary>
        [DataMember(Name = "headers")]
        public IDictionary<string, object> Headers { get; set; }

        /// <summary>
        /// The clientId indicates which MessageAgent sent the message.
        /// </summary>
        [DataMember(Name = "clientId")]
        public string ClientId { get; set; }

        /// <summary>
        /// Provides access to the correlation id of the message.
        /// </summary>
        [DataMember(Name = "correlationId")]
        public string CorrelationId { get; set; }

        /// <summary>
        /// The message destination.
        /// </summary>
        [DataMember(Name = "destination")]
        public string Destination { get; set; }

        /// <summary>
        /// The unique id for the message.
        /// </summary>
        [DataMember(Name = "messageId")]
        public string MessageId { get; set; }

        /// <summary>
        /// The time to live value of a message indicates how long the message 
        /// should be considered valid and deliverable.
        /// </summary>
        [DataMember(Name = "timeToLive")]
        public double TimeToLive { get; set; }

        /// <summary>
        /// Provides access to the time stamp for the message.
        /// </summary>
        [DataMember(Name = "timestamp")]
        public double Timestamp { get; set; }
    }

    /// <summary>
    /// Message header names.
    /// </summary>
    static public class MessageHeader
    {
        /// <summary>
        /// Messages pushed from the server may arrive in a batch, with messages in the
        /// batch potentially targeted to different Consumer instances. 
        /// Each message will contain this header identifying the Consumer instance that 
        /// will receive the message.
        /// </summary>
        public const string DestinationClientId = "DSDstClientId";

        /// <summary>
        /// Messages are tagged with the endpoint id for the Channel they are sent over.
        /// Channels set this value automatically when they send a message.
        /// </summary>
        public const string Endpoint = "DSEndpoint";

        /// <summary>
        /// This header is used to transport the global FlexClient Id value in outbound 
        /// messages once it has been assigned by the server.
        /// </summary>
        public const string FlexClientId = "DSId";

        /// <summary>
        /// Messages sent by a MessageAgent can have a priority header with a 0-9
        /// numerical value (0 being lowest) and the server can choose to use this
        /// numerical value to prioritize messages to clients. 
        /// </summary>
        public const string Priority = "DSPriority";

        /// <summary>
        /// Messages that need to set remote credentials for a destination
        /// carry the Base64 encoded credentials in this header.  
        /// </summary>
        public const string RemoteCredentials = "DSRemoteCredentials";

        /// <summary>
        /// Messages that need to set remote credentials for a destination
        /// may also need to report the character-set encoding that was used to
        /// create the credentials String using this header.  
        /// </summary>
        public const string RemoteCredentialsCharset = "DSRemoteCredentialsCharset";

        /// <summary>
        /// Messages sent with a defined request timeout use this header. 
        /// The request timeout value is set on outbound messages by services or 
        /// channels and the value controls how long the corresponding MessageResponder 
        /// will wait for an acknowledgement, result or fault response for the message
        /// before timing out the request.
        /// </summary>
        public const string RequestTimeout = "DSRequestTimeout";

        /// <summary>
        /// A status code can provide context about the nature of a response
        /// message. For example, messages received from an HTTP based channel may
        /// need to report the HTTP response status code (if available).
        /// </summary>
        public const string StatusCode = "DSStatusCode";

        /// <summary>
        /// Header name for the error hint header.
        /// </summary>
        public const string ErrorHint = "DSErrorHint";

        /// <summary>
        /// Messages sent by a MessageAgent with a defined subtopic property 
        /// indicate their target subtopic in this header.
        /// </summary>
        public const string Subtopic = "DSSubtopic";
    }
}

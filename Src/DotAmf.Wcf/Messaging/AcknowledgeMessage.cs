// Copyright (c) 2012 Artem Abashev (http://abashev.me)
// All rights reserved.
// Licensed under the Microsoft Public License (Ms-PL)
// http://opensource.org/licenses/ms-pl.html

using System.Runtime.Serialization;

namespace DotAmf.ServiceModel.Messaging
{
    /// <summary>
    /// An AcknowledgeMessage acknowledges the receipt of a message that was sent previously. 
    /// Every message sent within the messaging system must receive an acknowledgement. 
    /// </summary>
    [DataContract(Name = "flex.messaging.messages.AcknowledgeMessage")]
    public class AcknowledgeMessage : AbstractMessage
    {
    }
}

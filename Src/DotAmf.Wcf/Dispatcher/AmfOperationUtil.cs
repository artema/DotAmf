using System;
using System.Collections.Generic;
using DotAmf.Data;
using DotAmf.ServiceModel.Channels;
using DotAmf.ServiceModel.Messaging;

namespace DotAmf.ServiceModel.Dispatcher
{
    /// <summary>
    /// AMF operations utility.
    /// </summary>
    static internal class AmfOperationUtil
    {
        #region Constants
        /// <summary>
        /// Result operation's target template.
        /// </summary>
        private const string OperationResultTarget = "{0}/onResult";

        /// <summary>
        /// Fault operation's target template.
        /// </summary>
        private const string OperationFaultTarget = "{0}/onStatus";
        #endregion

        #region Flex operations
        /// <summary>
        /// Build a message reply.
        /// </summary>
        /// <param name="request">Request message.</param>
        /// <param name="body">Reply message's body.</param>
        static public AmfGenericMessage BuildMessageReply(AmfGenericMessage request, object body)
        {
            var replyHeaders = new Dictionary<string, AmfHeader>();
            var replyMessage = new AmfMessage
            {
                Target = CreateResultReplyTarget(request.AmfMessage),
                Response = string.Empty,
                Data = body
            };

            return new AmfGenericMessage(replyHeaders, replyMessage);
        }

        /// <summary>
        /// Build an acknowledge message.
        /// </summary>
        /// <param name="message">Incoming message.</param>
        static public AcknowledgeMessage BuildAcknowledgeMessage(AbstractMessage message)
        {
            return new AcknowledgeMessage
                       {
                           MessageId = GenerateUuid(),
                           CorrelationId = message.MessageId,
                           Timestamp = GenerateTimestamp()
                       };
        }

        /// <summary>
        /// Build an error message.
        /// </summary>
        /// <param name="message">Incoming message.</param>
        static public ErrorMessage BuildErrorMessage(AbstractMessage message)
        {
            return new ErrorMessage
            {
                MessageId = GenerateUuid(),
                CorrelationId = message.MessageId,
                Timestamp = GenerateTimestamp()
            };
        }

        /// <summary>
        /// Generate a unique ID.
        /// </summary>
        static public string GenerateUuid()
        {
            return Guid.NewGuid().ToString().ToUpper();
        }

        /// <summary>
        /// Generate current timestamp.
        /// </summary>
        static public double GenerateTimestamp()
        {
            var span = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));
            return span.TotalSeconds;
        }
        #endregion

        #region Helper methods
        /// <summary>
        /// Create a result message's reply target.
        /// </summary>
        /// <param name="requestMessage">Request message.</param>
        static public string CreateResultReplyTarget(AmfMessage requestMessage)
        {
            return string.Format(OperationResultTarget, requestMessage.Response ?? string.Empty);
        }

        /// <summary>
        /// Create a status message's reply target.
        /// </summary>
        /// <param name="requestMessage">Request message.</param>
        static public string CreateStatusReplyTarget(AmfMessage requestMessage)
        {
            return string.Format(OperationFaultTarget, requestMessage.Response ?? string.Empty);
        }
        #endregion
    }
}

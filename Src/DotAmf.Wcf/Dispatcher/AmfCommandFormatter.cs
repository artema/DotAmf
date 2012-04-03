using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;
using DotAmf.ServiceModel.Channels;

namespace DotAmf.ServiceModel.Dispatcher
{
    /// <summary>
    /// AMD command formatter.
    /// </summary>
    /// <see cref="AmfCommandInvoker"/>
    internal class AmfCommandFormatter : IDispatchMessageFormatter
    {
        #region IDispatchMessageFormatter Members
        /// <summary>
        /// Deserializes a message into an array of parameters.
        /// </summary>
        /// <param name="message">The incoming message.</param>
        /// <param name="parameters">The objects that are passed to the operation as parameters.</param>
        public virtual void DeserializeRequest(Message message, object[] parameters)
        {
            var request = (AmfGenericMessage)message;
            parameters[0] = new AmfGenericMessage(request.AmfHeaders, request.AmfMessage); //Use space allocated for one AmfGenericMessage
        }

        /// <summary>
        /// Serializes a reply message from a specified message version, array of parameters, and a return value.
        /// </summary>
        /// <param name="messageVersion">The SOAP message version.</param>
        /// <param name="parameters">The out parameters.</param>
        /// <param name="result">The return value.</param>
        /// <returns>The serialized reply message.</returns>
        public virtual Message SerializeReply(MessageVersion messageVersion, object[] parameters, object result)
        {
            var reply = (AmfGenericMessage)result;
            return new AmfGenericMessage(reply.AmfHeaders, reply.AmfMessage);
        }
        #endregion
    }
}

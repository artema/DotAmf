using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;
using DotAmf.ServiceModel.Channels;

namespace DotAmf.ServiceModel.Dispatcher
{
    /// <summary>
    /// AMF message filter.
    /// </summary>
    sealed internal class AmfMessageFilter : MessageFilter
    {
        #region Abstract methods implementation
        public override bool Match(MessageBuffer buffer)
        {
            return Match(buffer.CreateMessage());
        }

        public override bool Match(Message message)
        {
            return message is AmfMessageBase;
        }
        #endregion
    }
}

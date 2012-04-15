using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;

namespace DotAmf.ServiceModel.Dispatcher
{
    /// <summary>
    /// AMF message filter.
    /// </summary>
    sealed internal class AmfMessageFilter : MessageFilter
    {
        //All messages will be filtered in AmfDispatchOperationSelector
        //since there is no way to check if message is valid before
        //deserializing it.

        #region Abstract methods implementation
        public override bool Match(MessageBuffer buffer)
        {
            return true;
        }

        public override bool Match(Message message)
        {
            return true;
        }
        #endregion
    }
}

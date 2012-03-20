using System;

namespace DotAmf.Serialization
{
    /// <summary>
    /// Context switch event arguments.
    /// </summary>
    sealed public class ContextSwitchEventArgs : EventArgs
    {
        public ContextSwitchEventArgs(AmfSerializationContext context)
        {
            Context = context;
        }

        /// <summary>
        /// New AMF context.
        /// </summary>
        public AmfSerializationContext Context { get; private set; }
    }

    /// <summary>
    /// Context switch event handler.
    /// </summary>
    public delegate void ContextSwitch(object sender, ContextSwitchEventArgs e);
}

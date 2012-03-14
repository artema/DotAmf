using System;
using DotAmf.Data;

namespace DotAmf.Serialization
{
    /// <summary>
    /// Context switch event arguments.
    /// </summary>
    sealed public class ContextSwitchEventArgs : EventArgs
    {
        public ContextSwitchEventArgs(AmfVersion contextVersion)
        {
            ContextVersion = contextVersion;
        }

        /// <summary>
        /// AMF version to switch to.
        /// </summary>
        public AmfVersion ContextVersion { get; private set; }
    }

    /// <summary>
    /// Context switch event handler.
    /// </summary>
    public delegate void ContextSwitch(object sender, ContextSwitchEventArgs e);
}

using System;

namespace DotAmf.Data
{
    /// <summary>
    /// AMF encoding context switch event arguments.
    /// </summary>
    sealed public class EncodingContextSwitchEventArgs : EventArgs
    {
        public EncodingContextSwitchEventArgs(AmfVersion amfVersion)
        {
            AmfVersion = amfVersion;
        }

        /// <summary>
        /// New AMF encoding context version.
        /// </summary>
        public AmfVersion AmfVersion { get; private set; }
    }

    /// <summary>
    /// AMF encoding context switch event handler.
    /// </summary>
    public delegate void EncodingContextSwitch(object sender, EncodingContextSwitchEventArgs e);
}

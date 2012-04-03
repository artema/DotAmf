using System;
using System.ServiceModel.Channels;
using DotAmf.Data;

namespace DotAmf.ServiceModel.Channels
{
    /// <summary>
    /// The binding element that sets the message version used to encode messages to AMF.
    /// </summary>
    sealed public class AmfEncodingBindingElement : MessageEncodingBindingElement
    {
        #region .ctor
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="encodingOptions">AMF encoding options.</param>
        public AmfEncodingBindingElement(AmfEncodingOptions encodingOptions)
        {
            _encodingOptions = encodingOptions;
        }
        #endregion

        #region Data
        /// <summary>
        /// AMF encoding options.
        /// </summary>
        private readonly AmfEncodingOptions _encodingOptions;
        #endregion

        #region Overriden methods
        /// <summary>
        /// Creates a factory for producing message encoders.
        /// </summary>
        /// <returns>The <c>MessageEncoderFactory</c> used to produce message encoders.</returns>
        public override MessageEncoderFactory CreateMessageEncoderFactory() { return new AmfEncoderFactory(_encodingOptions); }

        /// <summary>
        /// Gets or sets the message version that can be handled by the message encoders 
        /// produced by the message encoder factory.
        /// </summary>
        /// <remarks>Always set to <c>None</c> to make sure that no wrapping is applied.</remarks>
        public override MessageVersion MessageVersion { get { return MessageVersion.None; } set { throw new NotSupportedException(); } }

        /// <summary>
        /// Returns a copy of the binding element object.
        /// </summary>
        /// <returns>A BindingElement object that is a deep clone of the original.</returns>
        public override BindingElement Clone() { return new AmfEncodingBindingElement(_encodingOptions); }
        #endregion

        #region Setup
        public override IChannelFactory<TChannel> BuildChannelFactory<TChannel>(BindingContext context)
        {
            context.BindingParameters.Add(this);
            return base.BuildChannelFactory<TChannel>(context);
        }

        public override IChannelListener<TChannel> BuildChannelListener<TChannel>(BindingContext context)
        {
            context.BindingParameters.Add(this);
            return base.BuildChannelListener<TChannel>(context);
        }
        #endregion
    }
}

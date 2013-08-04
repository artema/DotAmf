// Copyright (c) 2012 Artem Abashev (http://abashev.me)
// All rights reserved.
// Licensed under the Microsoft Public License (Ms-PL)
// http://opensource.org/licenses/ms-pl.html

using System;
using System.ServiceModel.Channels;
using DotAmf.Data;

namespace DotAmf.ServiceModel.Channels
{
    /// <summary>
    /// AMF message encoder factory.
    /// </summary>
    /// <remarks>The factory for producing message encoders that can read messages 
    /// from a stream and write them to a stream for various types of message encoding.</remarks>
    sealed internal class AmfEncoderFactory : MessageEncoderFactory
    {
        #region .ctor
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="encodingOptions">AMF encoding options.</param>
        public AmfEncoderFactory(AmfEncodingOptions encodingOptions)
        {
            //Create an encoder instance for future use
            _encoder = new AmfEncoder(encodingOptions);
        }
        #endregion

        #region Constants
        /// <summary>
        /// The default AMF media type (MIME).
        /// </summary>
        public const string DefaultAmfMediaType = "application/x-amf";

        /// <summary>
        /// The default AMF character set.
        /// </summary>
        public const string DefaultAmfCharSet = "utf-8";
        #endregion

        #region Data
        /// <summary>
        /// Message encoder instance.
        /// </summary>
        /// <remarks>A message encoder should support concurrent calls, 
        /// so it is safe to have only a single instance of it.</remarks>
        private readonly AmfEncoder _encoder;
        #endregion

        #region Overriden methods
        public override MessageEncoder CreateSessionEncoder() { throw new NotSupportedException(); }

        /// <summary>
        /// Gets the message encoder that is produced by the factory.
        /// </summary>
        public override MessageEncoder Encoder { get { return _encoder; } } //And you call that a factory?

        /// <summary>
        /// Gets the message version that is used by the encoders produced by the factory to encode messages.
        /// </summary>
        /// <remarks>By default, this one is <c>MessageVersion.None</c> since we don't need any SOAP/WS-* support.</remarks>
        public override MessageVersion MessageVersion { get { return MessageVersion.None; } }
        #endregion
    }
}

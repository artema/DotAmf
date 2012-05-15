// Copyright (c) 2012 Artem Abashev (http://abashev.me)
// All rights reserved.
// Licensed under the Microsoft Public License (Ms-PL)
// http://opensource.org/licenses/ms-pl.html

using System;
using System.Collections.Generic;
using System.ServiceModel.Channels;
using System.Xml;
using DotAmf.Data;

namespace DotAmf.ServiceModel.Channels
{
    /// <summary>
    /// Abstract AMF message.
    /// </summary>
    abstract internal class AmfMessageBase : Message
    {
        #region .ctor
        /// <summary>
        /// Constructor.
        /// </summary>
        protected AmfMessageBase()
        {
            _properties = new MessageProperties();

            //Make sure that there is no wrapping applied to this message
            _headers = new MessageHeaders(MessageVersion.None);
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        protected AmfMessageBase(IDictionary<string, AmfHeader> headers)
            : this()
        {
            if (headers == null) throw new ArgumentNullException("headers");
            _amfHeaders = headers;
        }
        #endregion

        #region Data
        /// <summary>
        /// Message headers.
        /// </summary>
        private readonly MessageHeaders _headers;

        /// <summary>
        /// Message properties.
        /// </summary>
        private readonly MessageProperties _properties;

        /// <summary>
        /// AMF headers.
        /// </summary>
        private readonly IDictionary<string,AmfHeader> _amfHeaders;
        #endregion

        #region Properties
        /// <summary>
        /// AMF headers.
        /// </summary>
        public IDictionary<string, AmfHeader> AmfHeaders { get { return _amfHeaders; } }
        #endregion

        #region Overriden methods
        /// <summary>
        /// Gets the headers of the message.
        /// </summary>
        sealed public override MessageHeaders Headers { get { return _headers; } }

        /// <summary>
        /// Gets a set of processing-level annotations to the message.
        /// </summary>
        sealed public override MessageProperties Properties { get { return _properties; } }

        /// <summary>
        /// Gets the SOAP version of the message.
        /// </summary>
        /// <remarks>To make sure that there won't be any SOAP, this one always returns <c>None</c>.</remarks>
        sealed public override MessageVersion Version { get { return MessageVersion.None; } }

        /// <summary>
        /// Called when the message body is written to an XML file.
        /// </summary>
        /// <param name="writer">A XmlDictionaryWriter that is used to write this message body to an XML file.</param>
        sealed protected override void OnWriteBodyContents(XmlDictionaryWriter writer)
        {
            //We don't need this at all
        }
        #endregion
    }
}

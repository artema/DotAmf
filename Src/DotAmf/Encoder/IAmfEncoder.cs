// Copyright (c) 2012 Artem Abashev (http://abashev.me)
// All rights reserved.
// Licensed under the Microsoft Public License (Ms-PL)
// http://opensource.org/licenses/ms-pl.html

using System;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;
using DotAmf.Data;

namespace DotAmf.Encoder
{
    /// <summary>
    /// AMF encoder.
    /// </summary>
    public interface IAmfEncoder
    {
        /// <summary>
        /// Encode data from AMFX format.
        /// </summary>
        /// <param name="stream">AMF stream.</param>
        /// <param name="input">AMFX input reader.</param>
        /// <exception cref="NotSupportedException">AMF type is not supported.</exception>
        /// <exception cref="SerializationException">Error during serialization.</exception>
        /// <exception cref="InvalidOperationException">Invalid AMF context.</exception>
        void Encode(Stream stream, XmlReader input);

        /// <summary>
        /// Write an AMF packet header descriptor.
        /// </summary>
        void WritePacketHeader(Stream stream, AmfHeaderDescriptor descriptor);

        /// <summary>
        /// Write AMF packet body descriptor.
        /// </summary>
        void WritePacketBody(Stream stream, AmfMessageDescriptor descriptor);
    }
}

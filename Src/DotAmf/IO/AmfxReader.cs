// Copyright (c) 2012 Artem Abashev (http://abashev.me)
// All rights reserved.
// Licensed under the Microsoft Public License (Ms-PL)
// http://opensource.org/licenses/ms-pl.html

using System;
using System.IO;
using System.Xml;
using System.Xml.Schema;

namespace DotAmf.IO
{
    /// <summary>
    /// AMFX reader.
    /// </summary>
    abstract public class AmfxReader
    {
        /// <summary>
        /// Create an AMFX reader for the stream.
        /// </summary>
        public static XmlReader Create(Stream stream, bool handleDispose=false)
        {
            if (stream == null) throw new ArgumentNullException("stream");

            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Ignore,
                IgnoreProcessingInstructions = true,
                CloseInput = handleDispose,
                CheckCharacters = false,
                IgnoreComments = true,
                IgnoreWhitespace = true
            };

            return XmlReader.Create(stream, settings);
        }
    }
}

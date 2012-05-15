// Copyright (c) 2012 Artem Abashev (http://abashev.me)
// All rights reserved.
// Licensed under the Microsoft Public License (Ms-PL)
// http://opensource.org/licenses/ms-pl.html

using System;
using System.IO;
using System.Text;
using System.Xml;

namespace DotAmf.IO
{
    /// <summary>
    /// AMFX writer.
    /// </summary>
    abstract public class AmfxWriter
    {
        /// <summary>
        /// Create an AMFX writer for the stream.
        /// </summary>
        public static XmlWriter Create(Stream stream, bool handleDispose = false)
        {
            if (stream == null) throw new ArgumentNullException("stream");

            var settings = new XmlWriterSettings
            {
                CheckCharacters = false,
                Encoding = Encoding.UTF8,
                CloseOutput = handleDispose,
                NewLineHandling = NewLineHandling.None,
                OmitXmlDeclaration = true
            };

            return XmlWriter.Create(stream, settings);
        }
    }
}

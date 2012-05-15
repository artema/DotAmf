// Copyright (c) 2012 Artem Abashev (http://abashev.me)
// All rights reserved.
// Licensed under the Microsoft Public License (Ms-PL)
// http://opensource.org/licenses/ms-pl.html

using System;

namespace DotAmf.IO
{
    /// <summary>
    /// Prepare bytes after reading or before writing from/to a stream.
    /// </summary>
    /// <param name="bytes">Bytes to convert.</param>
    /// <returns>Converted bytes.</returns>
    internal delegate byte[] PrepareBytes(byte[] bytes);

    /// <summary>
    /// Byte converter.
    /// </summary>
    static internal class ByteConverter
    {
        /// <summary>
        /// Indicates the byte order in which data is stored in this computer architecture.
        /// </summary>
        static public bool IsLittleEndian { get { return BitConverter.IsLittleEndian; } }

        /// <summary>
        /// Change bytes endianness to opposite.
        /// </summary>
        /// <param name="bytes">Bytes in big/little endian order.</param>
        /// <returns>Bytes in little/big endian order.</returns>
        static public byte[] ChangeEndianness(byte[] bytes)
        {
            Array.Reverse(bytes);
            return bytes;
        }

        /// <summary>
        /// A placeholder which doesn't perform any byte convertions.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        static public byte[] KeepEndianness(byte[] bytes)
        {
            return bytes;
        }
    }
}
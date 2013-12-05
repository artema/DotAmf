// Copyright (c) 2012 Artem Abashev (http://abashev.me)
// All rights reserved.
// Licensed under the Microsoft Public License (Ms-PL)
// http://opensource.org/licenses/ms-pl.html

using System;
using System.IO;
using System.Text;

namespace DotAmf.IO
{
    /// <summary>
    /// AMF stream reader.
    /// </summary>
    sealed internal class AmfStreamReader : BinaryReader
    {
        #region .ctor
        public AmfStreamReader(Stream stream)
            : base(stream, Encoding.UTF8)
        {
        }
        #endregion

        #region Data
        /// <summary>
        /// Delegate to use for preparing bytes before reading.
        /// </summary>
        /// <remarks>
        /// AMF messages have a big endian (network) byte order.
        /// </remarks>
        private static readonly PrepareBytes PrepareBytes = ByteConverter.IsLittleEndian ?
                                             (PrepareBytes) ByteConverter.ChangeEndianness :
                                                            ByteConverter.KeepEndianness;
        #endregion

        #region Public methods
        public override byte ReadByte() { return base.ReadByte(); }

        public override sbyte ReadSByte() { throw new NotSupportedException(); }

        public override short ReadInt16() { return BitConverter.ToInt16(PrepareBytes(ReadBytes(2)), 0); }

        public override ushort ReadUInt16() { return BitConverter.ToUInt16(PrepareBytes(ReadBytes(2)), 0); }

        public override int ReadInt32() { return BitConverter.ToInt32(PrepareBytes(ReadBytes(4)), 0); }

        public override uint ReadUInt32() { return BitConverter.ToUInt32(PrepareBytes(ReadBytes(4)), 0); }

        public override long ReadInt64() { throw new NotSupportedException(); }

        public override ulong ReadUInt64() { throw new NotSupportedException(); }

        public override float ReadSingle() { throw new NotSupportedException(); }

        public decimal ReadDecimal() { throw new NotSupportedException(); }

        public override double ReadDouble() { return BitConverter.ToDouble(PrepareBytes(ReadBytes(8)), 0); }

        public override string ReadString() { throw new NotSupportedException(); }
        #endregion
    }
}

﻿using System;
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
            : base(stream)
        {
        }

        public AmfStreamReader(Stream stream, Encoding encoding)
            : base(stream, encoding)
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

        public override decimal ReadDecimal() { throw new NotSupportedException(); }

        public override double ReadDouble() { return BitConverter.ToDouble(PrepareBytes(ReadBytes(8)), 0); }

        public override string ReadString() { throw new NotSupportedException(); }
        #endregion

        #region IDisposable implementation
        /// <summary>
        /// Disposes reader, but leaves the underlying stream open.
        /// </summary>
        public new void Dispose()
        {
        }
        #endregion
    }
}

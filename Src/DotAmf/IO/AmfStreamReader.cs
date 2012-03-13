using System;
using System.IO;
using System.Text;

namespace DotAmf.IO
{
    /// <summary>
    /// AMF stream reader.
    /// </summary>
    sealed public class AmfStreamReader : BinaryReader
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

        #region Public methods
        public override short ReadInt16(){ return BitConverter.ToInt16(PrepareBytes(ReadBytes(2)), 0); }

        public override ushort ReadUInt16() { return BitConverter.ToUInt16(PrepareBytes(ReadBytes(2)), 0); }

        public override int ReadInt32(){ return BitConverter.ToInt32(PrepareBytes(ReadBytes(4)), 0); }

        public override uint ReadUInt32() { return BitConverter.ToUInt32(PrepareBytes(ReadBytes(4)), 0); }

        public override long ReadInt64(){ return BitConverter.ToInt64(PrepareBytes(ReadBytes(8)), 0); }

        public override ulong ReadUInt64() { return BitConverter.ToUInt64(PrepareBytes(ReadBytes(8)), 0); }

        public override float ReadSingle(){ return BitConverter.ToSingle(PrepareBytes(ReadBytes(4)), 0); }

        public override double ReadDouble() { return BitConverter.ToDouble(PrepareBytes(ReadBytes(8)), 0); }
        #endregion

        #region Private methods
        /// <summary>
        /// Prepare bytes before converting.
        /// </summary>
        static private byte[] PrepareBytes(byte[] bytes)
        {
            //AMF messages have a big endian (network) byte order.
            Array.Reverse(bytes);
            return bytes;
        }
        #endregion
    }
}

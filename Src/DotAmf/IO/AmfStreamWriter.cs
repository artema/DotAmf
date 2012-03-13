using System.IO;
using System.Text;

namespace DotAmf.IO
{
    /// <summary>
    /// AMF stream writer.
    /// </summary>
    sealed public class AmfStreamWriter : BinaryWriter
    {
        #region .ctor
        public AmfStreamWriter(Stream stream)
            : base(stream)
        {
        }

        public AmfStreamWriter(Stream stream, Encoding encoding)
            : base(stream, encoding)
        {
        }
        #endregion
    }
}

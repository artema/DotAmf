using System;
using System.IO;

namespace DotAmf.Serialization
{
    /// <summary>
    /// AMF3 serializer.
    /// </summary>
    public class Amf3Serializer : AmfSerializerBase
    {
        #region .ctor
        public Amf3Serializer(BinaryWriter writer)
            : base(writer)
        {
        }
        #endregion

        #region IAmfSerializer implementation
        public override int WriteValue(object value)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}

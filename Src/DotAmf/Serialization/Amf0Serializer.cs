using System;
using System.IO;
using DotAmf.Data;

namespace DotAmf.Serialization
{
    /// <summary>
    /// AMF0 serializer.
    /// </summary>
    public class Amf0Serializer : AmfSerializerBase
    {
        #region .ctor
        public Amf0Serializer(BinaryWriter writer)
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

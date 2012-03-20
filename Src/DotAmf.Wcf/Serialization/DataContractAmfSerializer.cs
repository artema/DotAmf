using System;
using System.Runtime.Serialization;
using System.Xml;

namespace DotAmf.Wcf.Serialization
{
    sealed public class DataContractAmfSerializer : XmlObjectSerializer
    {
        public override bool IsStartObject(XmlDictionaryReader reader)
        {
            throw new NotImplementedException();
        }

        public override object ReadObject(XmlDictionaryReader reader, bool verifyObjectName)
        {
            throw new NotImplementedException();
        }

        public override void WriteEndObject(XmlDictionaryWriter writer)
        {
            throw new NotImplementedException();
        }

        public override void WriteObjectContent(XmlDictionaryWriter writer, object graph)
        {
            throw new NotImplementedException();
        }

        public override void WriteStartObject(XmlDictionaryWriter writer, object graph)
        {
            throw new NotImplementedException();
        }
    }
}

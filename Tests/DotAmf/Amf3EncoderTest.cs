using DotAmf.Data;
using DotAmf.Serialization;
using NUnit.Framework;

namespace DotAmf
{
    [TestFixture(Description = "AMFX to AMF encoding tests.")]
    public class Amf3EncoderTest : AbstractTest
    {
        #region .ctor
        public Amf3EncoderTest()
        {
            _options = new AmfEncodingOptions
                           {
                               AmfVersion = AmfVersion.Amf3,
                               UseContextSwitch = false
                           };
        }
        #endregion

        #region Data
        private readonly AmfEncodingOptions _options;
        #endregion

        #region Test methods
        #region String
        [Test(Description = "Simple string encoding.")]
        public void TestString1()
        {
            PerformTest<string>("String1.amfx", "String1.amf");
        }

        [Test(Description = "Empty string encoding.")]
        public void TestStringEmpty()
        {
            PerformTest<string>("StringEmpty.amfx", "StringEmpty.amf");
        }

        [Test(Description = "String reference test.")]
        public void TestStringReference()
        {
            PerformTest<string[]>("StringReference.amfx", "StringReference.amf");
        } 
        #endregion
        #endregion

        #region Helper methods
        private void PerformTest<T>(string inputName, string sampleName)
        {
            var serializer = CreateSerializer<T>();
            
            using (var input = GetInput(inputName))
            using (var output = GetOutput())
            {
                var reader = GetAmfxReader(input);
                reader.Read();
                serializer.WriteObject(output, reader);

                output.Flush();
                output.Position = 0;

                ValidateAmf(output, sampleName);
            }
        }

        protected override DataContractAmfSerializer CreateSerializer<T>()
        {
            return new DataContractAmfSerializer(typeof(T), _options);
        }
        #endregion
    }
}

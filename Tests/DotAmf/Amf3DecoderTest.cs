using DotAmf.Data;
using DotAmf.Serialization;
using NUnit.Framework;

namespace DotAmf
{
    [TestFixture(Description = "AMF to AMFX decoding tests.")]
    public class Amf3DecoderTest : AbstractTest
    {
        #region .ctor
        public Amf3DecoderTest()
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
        [Test(Description = "Simple string decoding.")]
        public void TestString1()
        {
            PerformTest<string>("String1.amf", "String1.amfx");
        } 
        #endregion

        #region Helper methods
        private void PerformTest<T>(string inputName, string sampleName)
        {
            var serializer = CreateSerializer<T>();
            
            using (var input = GetInput(inputName))
            using (var output = GetOutput())
            {
                var writer = GetAmfxWriter(output);
                serializer.ReadObject(input, writer);

                output.Flush();
                output.Position = 0;

                ValidateAmfx(output, sampleName);
            }
        }

        protected override DataContractAmfSerializer CreateSerializer<T>()
        {
            return new DataContractAmfSerializer(typeof(T), _options);
        }
        #endregion
    }
}

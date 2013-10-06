using System;
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
        #region String
        [Test(Description = "Simple string decoding.")]
        public void TestString1()
        {
            PerformTest<string>("String1.amf", "String1.amfx");
        }

        [Test(Description = "Empty string decoding.")]
        public void TestStringEmpty()
        {
            PerformTest<string>("StringEmpty.amf", "StringEmpty.amfx");
        }

        [Test(Description = "String reference test.")]
        public void TestStringReference()
        {
            PerformTest<string[]>("StringReference.amf", "StringReference.amfx");
        } 
        #endregion

        #region Integer
        [Test(Description = "Simple integer decoding: one byte value.")]
        public void TestInteger1()
        {
            PerformTest<int>("Integer1.amf", "Integer1.amfx");
        }

        [Test(Description = "Simple integer decoding: two bytes value.")]
        public void TestInteger2()
        {
            PerformTest<int>("Integer2.amf", "Integer2.amfx");
        }

        [Test(Description = "Simple integer decoding: three bytes value.")]
        public void TestInteger3()
        {
            PerformTest<int>("Integer3.amf", "Integer3.amfx");
        }

        [Test(Description = "Simple integer decoding: four bytes value.")]
        public void TestInteger4()
        {
            PerformTest<int>("Integer4.amf", "Integer4.amfx");
        }

        [Test(Description = "Negative integer decoding: short value.")]
        public void TestInteger5()
        {
            PerformTest<int>("Integer5.amf", "Integer5.amfx");
        }
        #endregion

        #region Date
        [Test(Description = "Date decoding.")]
        public void TestDate()
        {
            PerformTest<DateTime>("Date.amf", "Date.amfx");
        }

        [Test(Description = "Date reference test.")]
        public void TestDateReference()
        {
            PerformTest<DateTime[]>("DateReference.amf", "DateReference.amfx");
        }
        #endregion

        #region Array
        [Test(Description = "Empty array decoding.")]
        public void TestArray1()
        {
            PerformTest<object[]>("Array1.amf", "Array1.amfx");
        }

        [Test(Description = "Array reference test.")]
        public void TestArrayReference()
        {
            PerformTest<object[]>("ArrayReference.amf", "ArrayReference.amfx");
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

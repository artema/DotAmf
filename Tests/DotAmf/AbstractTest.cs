using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using DotAmf.Serialization;

namespace DotAmf
{
    public abstract class AbstractTest
    {
        protected abstract DataContractAmfSerializer CreateSerializer<T>();

        protected Stream GetInput(string sampleName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "DotAmf.Samples." + sampleName;

            return assembly.GetManifestResourceStream(resourceName);
        }

        protected Stream GetOutput()
        {
            return new MemoryStream();
        }

        protected XmlWriter GetAmfxWriter(Stream output)
        {
            var settings = new XmlWriterSettings
            {
                CloseOutput = false,
                Indent = false,
                NamespaceHandling = NamespaceHandling.OmitDuplicates,
                NewLineHandling = NewLineHandling.Replace,
                NewLineChars = string.Empty,
                OmitXmlDeclaration = true
            };

            return XmlWriter.Create(output, settings);
        }

        protected XmlReader GetAmfxReader(Stream input)
        {
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Ignore,
                IgnoreComments = true,
                IgnoreWhitespace = true,
                IgnoreProcessingInstructions = true
            };

            return XmlReader.Create(input, settings);
        }

        protected void ValidateAmfx(Stream output, string sampleName)
        {
            using (var outputReader = new StreamReader(output))
            using (var sampleReader = new StreamReader(GetInput(sampleName)))
            {
                var result = outputReader.ReadToEnd();
                var sample = sampleReader.ReadToEnd();

                if (result != sample)
                {
                    var message = string.Format(Errors.AmfxMismatch, result, sample);
                    throw new InvalidDataException(message);
                }
            }
        }

        protected void ValidateAmf(Stream output, string sampleName)
        {
            using (var outputReader = new StreamReader(output))
            using (var sampleReader = new StreamReader(GetInput(sampleName)))
            {
                var converter = new Func<string, string>(value => string.Join(" ", value.ToCharArray().Select(x => Convert.ToInt32(x).ToString("X2"))));

                var result = converter(outputReader.ReadToEnd());
                var sample = converter(sampleReader.ReadToEnd());

                if (result != sample)
                {
                    var message = string.Format(Errors.AmfMismatch, result, sample);
                    throw new InvalidDataException(message);
                }
            }
        }
    }
}

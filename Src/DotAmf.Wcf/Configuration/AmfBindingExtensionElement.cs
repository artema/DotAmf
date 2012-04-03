using System;
using System.Configuration;
using System.ServiceModel.Channels;
using System.ServiceModel.Configuration;
using DotAmf.Data;
using DotAmf.ServiceModel.Channels;

namespace DotAmf.ServiceModel.Configuration
{
    /// <summary>
    /// AMF binding extension.
    /// </summary>
    /// <remarks>Enables the use of a custom <c>BindingElement</c> implementation from a machine or application configuration file.</remarks>
    sealed public class AmfBindingExtensionElement : BindingElementExtensionElement
    {
        #region .ctor
        /// <summary>
        /// Constructor.
        /// </summary>
        public AmfBindingExtensionElement()
        {
            Version = AmfVersion.Amf3;
        }
        #endregion

        #region Configuration
        /// <summary>
        /// AMF version.
        /// </summary>
        [ConfigurationProperty("version", DefaultValue = AmfVersion.Amf3)]
        public AmfVersion Version { get; set; }
        #endregion

        #region Overriden methods
        /// <summary>
        /// Gets the <c>System.Type</c> object that represents the custom binding element.
        /// </summary>
        public override Type BindingElementType{ get { return typeof(AmfEncodingBindingElement); } }

        /// <summary>
        /// Returns a custom binding element object.
        /// </summary>
        protected override BindingElement CreateBindingElement()
        {
            var options = new AmfEncodingOptions
            {
                AmfVersion = Version,
                UseContextSwitch = true
            };

            return new AmfEncodingBindingElement(options);
        }
        #endregion
    }
}

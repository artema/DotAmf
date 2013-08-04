// Copyright (c) 2012 Artem Abashev (http://abashev.me)
// All rights reserved.
// Licensed under the Microsoft Public License (Ms-PL)
// http://opensource.org/licenses/ms-pl.html

using System;
using System.ServiceModel.Configuration;
using DotAmf.ServiceModel.Description;

namespace DotAmf.ServiceModel.Configuration
{
    /// <summary>
    /// AMF endpoint behavior extension.
    /// </summary>
    /// <remarks>Represents a configuration element that contains sub-elements that specify behavior extensions, 
    /// which enable the user to customize service or endpoint behaviors.</remarks>
    sealed public class AmfBehaviorExtensionElement : BehaviorExtensionElement
    {
        /// <summary>
        /// Gets the type of behavior.
        /// </summary>
        public override Type BehaviorType { get { return typeof(AmfEndpointBehavior); } }

        /// <summary>
        /// Creates a behavior extension based on the current configuration settings.
        /// </summary>
        protected override object CreateBehavior() { return new AmfEndpointBehavior(); }
    }
}

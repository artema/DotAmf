// Copyright (c) 2012 Artem Abashev (http://abashev.me)
// All rights reserved.
// Licensed under the Microsoft Public License (Ms-PL)
// http://opensource.org/licenses/ms-pl.html

namespace DotAmf.ServiceModel.Configuration
{
    /// <summary>
    /// AMF endpoint capabilities.
    /// </summary>
    internal struct AmfEndpointCapabilities
    {
        /// <summary>
        /// Messaging version.
        /// </summary>
        public uint MessagingVersion;

        /// <summary>
        /// Include exception details in faults.
        /// </summary>
        public bool ExceptionDetailInFaults;
    }
}

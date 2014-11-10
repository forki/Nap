﻿using System.Collections.Generic;
using Nap.Formatters.Base;

namespace Nap.Configuration
{
    /// <summary>
    /// Configuration properties for Nap REST requests.
    /// </summary>
    public interface INapConfig
    {
        /// <summary>
        /// Gets or sets the serializers that can be used to both serialize and deserialize content.
        /// </summary>
        Dictionary<RequestFormat, INapSerializer> Serializers { get; set; }

        /// <summary>
        /// Gets or sets the optional base URL for easy requests.
        /// </summary>
        string BaseUrl { get; set; }

        /// <summary>
        /// Gets or sets the accept-type format.
        /// </summary>
        RequestFormat Serialization { get; set; }

        /// <summary>
        /// Gets or sets the advanced portion of the configuration.
        /// </summary>
        IAdvancedNapConfig Advanced { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not to fill "Special Values" such as StatusCode on the deserialized object.
        /// </summary>
        bool FillMetadata { get; set; }

        /// <summary>
        /// Returns a new instance of the current Nap configuration
        /// </summary>
        /// <returns></returns>
        INapConfig Clone();
    }
}
﻿using System.Collections.Generic;
using System.Linq;

namespace Nap.Configuration
{
    /// <summary>
    /// Class NapConfig.
    /// </summary>
    public static class NapSetup
    {
        private static readonly IList<INapConfig> _enabledConfigs = new List<INapConfig>();
        private static INapConfig _currentConfig;

        /// <summary>
        /// Gets the default (first) configuration loaded into the system.  If none is defined it returns a new <see cref="EmptyNapConfig"/> instance.
        /// </summary>
        /// <value>The default.</value>
        public static INapConfig Default
        {
            get
            {
                if (_currentConfig == null)
                {
                    _currentConfig = _enabledConfigs.Any() ? _enabledConfigs[0] : new EmptyNapConfig();
                }

                return _currentConfig;
            }
        }

        /// <summary>
        /// Adds a configuration into the system.
        /// </summary>
        /// <param name="config">The configuration.</param>
        public static void AddConfig(INapConfig config)
        {
            _enabledConfigs.Add(config);

            if (_currentConfig == null)
            {
                _currentConfig = config;
            }
        }
    }
}
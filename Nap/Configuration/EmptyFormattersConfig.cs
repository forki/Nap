﻿using System;
using System.Collections.Generic;
using System.Linq;

using Nap.Formatters;
using Nap.Formatters.Base;

namespace Nap.Configuration
{
    /// <summary>
    /// An empty implementation of the formatters collection, which does not pull from *.config files.
    /// </summary>
	public class EmptyFormattersConfig : List<IFormatterConfig>, IFormattersConfig
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EmptyFormattersConfig"/> class.
        /// </summary>
        /// <remarks>Initializes the class with 3 formatters already part of it: <see cref="NapFormsFormatter"/>, <see cref="NapJsonFormatter"/> and <see cref="NapXmlFormatter"/>.</remarks>
        public EmptyFormattersConfig()
        {
            Add(new EmptyFormatterConfig(new NapFormsFormatter()));
            Add(new EmptyFormatterConfig(new NapJsonFormatter()));
            Add(new EmptyFormatterConfig(new NapXmlFormatter()));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EmptyFormattersConfig"/> class.
        /// </summary>
        /// <param name="formatterConfigs">The formatter configs to use to initialize the class.</param>
        public EmptyFormattersConfig(IEnumerable<IFormatterConfig> formatterConfigs)
        {
            AddRange(formatterConfigs);
        }

        /// <summary>
        /// Adds the specified formatter by specifying key/value pair.
        /// </summary>
        /// <param name="contentType">The key of the formatter to add (see MIME types).</param>
        /// <param name="formatterType">The full type name of the formatter to add.</param>
        public void Add(string contentType, string formatterType)
        {
            var formatterConfiguration = new EmptyFormatterConfig(contentType, formatterType);
            Add(formatterConfiguration);
        }

        /// <summary>
        /// Adds the specified formatter generically.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="INapFormatter" /> to add.</typeparam>
        public void Add<T>() where T : INapFormatter, new() => Add(new EmptyFormatterConfig(new T()));

        /// <summary>
        /// Adds the specified formatter generically.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="INapFormatter" /> to add.</typeparam>
        /// <param name="contentType">The content type to apply the formatter to.</param>
        public void Add<T>(string contentType) where T : INapFormatter, new() => Add(new EmptyFormatterConfig(contentType, new T()));

        /// <summary>
        /// Adds the specified formatter by specifying an instance of the formatter.
        /// </summary>
        /// <param name="napFormatter">The formatter instance to add to the collection of formatters.</param>
        public void Add(INapFormatter napFormatter) => Add(new EmptyFormatterConfig(napFormatter));

        /// <summary>
        /// Adds the specified formatter by specifying an instance of the formatter.
        /// </summary>
        /// <param name="napFormatter">The formatter instance to add to the collection of formatters.</param>
        /// <param name="contentType">The content type to apply the formatter to.</param>
        public void Add(INapFormatter napFormatter, string contentType) => Add(new EmptyFormatterConfig(contentType, napFormatter));

        /// <summary>
        /// Remvoes the specified formatter by key, a string of appropriate MIME type.
        /// </summary>
        /// <param name="contentType">The MIME type key of the formatter to remove.</param>
        public void Remove(string contentType)
        {
            var toRemove = this.FirstOrDefault(formatters => formatters.ContentType == contentType);
            if (toRemove != null)
                Remove(toRemove);
        }

        /// <summary>
        /// Converts the <see cref="IFormattersConfig"/> interface to a dicitonary.
        /// Note that operations on this object (such as <see cref="IDictionary{T1,T2}.Add(T1, T2)"/>) do not persist.
        /// </summary>
        /// <returns>The <see cref="IFormattersConfig"/> interface as a dictionary.</returns>
        public IDictionary<string, INapFormatter> AsDictionary()
        {
            return this.ToDictionary(formatter => formatter.ContentType, formatter => formatter.GetFormatter());
        }
    }
}
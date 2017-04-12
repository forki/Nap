﻿using System;
using System.Collections.Generic;
using System.Linq;
using Nap.Html.Exceptions;
using Nap.Html.Factories;

using Nap.Exceptions.Base;
using Nap.Serializers.Base;

#if IMMUTABLE
using Microsoft.FSharp.Core;
#endif

namespace Nap.Html
{
    /// <summary>
    /// The main de-serializer for HTML responses to object bindings.
    /// </summary>
    public class NapHtmlSerializer : INapSerializer
    {
        /// <summary>
        /// The content type that the <see cref="NapHtmlSerializer"/> supports is "text/html".
        /// </summary>
        public string ContentType => "text/html";

        /// <summary>
        /// Converts a serialized value to a C# object.  A new object is created in this process.
        /// </summary>
        /// <typeparam name="T">The type of object that should be created from the serializedvalue.</typeparam>
        /// <param name="serialized">The serialized data; such as from a REST request.</param>
        /// <returns>The newly created object of type <typeparamref name="T"/>.</returns>
        /// <remarks>All properties that are being hydrated by deserialization must have public setters.</remarks>
        /// <example><code>
        /// &lt;!DOCTYPE html&gt;
        /// &lt;html lang="en"&gt;
        /// &lt;head&gt;
        ///   &lt;meta charset="UTF-8"&gt;
        ///   &lt;title&gt;Document&lt;/title&gt;
        /// &lt;/head&gt;
        /// &lt;body&gt;
        ///   &lt;span id="firstName"&gt;John&lt;/span&gt;
        ///   &lt;span id="lastName"&gt;Doe&lt;/span&gt;
        /// &lt;/body&gt;
        /// &lt;/html&gt;
        /// </code>
        /// would populate an object like
        /// <code>
        /// public class Person
        /// {
        ///     [HtmlElement("#firstName")]
        ///     public string FirstName { get; set; }
        ///     [HtmlElement("#lastName")]
        ///     public string LastName { get; set; }
        /// }
        /// </code>
        /// for HTML deserialization.
        /// </example>
#if IMMUTABLE
        public FSharpOption<T> Deserialize<T>(string serialized)
        {
            if (serialized == null)
                return FSharpOption<T>.None;

            try
            {
                var result = (T) BinderFactory.Instance.GetBinder<T>().Handle(serialized, null, typeof (T));
                return FSharpOption<T>.Some(result);
            }
            catch (ArgumentNullException)
            {
                return FSharpOption<T>.None;
            }
        }
#else
        public T Deserialize<T>(string serialized)
        {
            if (serialized == null)
                throw new ArgumentNullException(nameof(serialized));

            try
            {
                return (T)BinderFactory.Instance.GetBinder<T>().Handle(serialized, null, typeof(T));
            }
            catch (ArgumentNullException ex)
            {
                throw new NapBindingException("An issue occurred with HTML binding.  See inner exception for details.", ex);
            }
        }
#endif

        /// <summary>
        /// Not implemented for HTML supported.
        /// </summary>
        /// <param name="graph">Unused - not supported for HTML serializaiton.</param>
        /// <returns>Not supported for HTML serialization.</returns>
        public string Serialize(object graph)
        {
            throw new NotSupportedException();
        }
    }
}

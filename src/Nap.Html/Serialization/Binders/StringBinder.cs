﻿using System;
using System.Diagnostics.Contracts;
using CsQuery;
using Nap.Html.Attributes.Base;
using Nap.Html.Enum;
using Nap.Html.Serialization.Binders.Base;

namespace Nap.Html.Serialization.Binders
{
	/// <summary>
	/// A simple binder for string types.
	/// </summary>
	public sealed class StringBinder : BaseBinder<string>
	{
		/// <summary>
		/// Binds the specified input string to an output object of type <see cref="string"/>
		/// The value <paramref name="context" /> is unused.
		/// </summary>
		/// <param name="input">The input string.  See <see cref="BindingBehavior" /> for examples on what types of information may be passed in.</param>
		/// <param name="context">Unused in this case.</param>
		/// <param name="outputType">Unused in this case.</param>
		/// <param name="attribute">Unused in this case.</param>
		/// <returns>
		/// The output type object created, and filled with the parsed version of the <paramref name="input" />.
		/// </returns>
		[Pure]
		public override object Handle(string input, CQ context, Type outputType, BaseHtmlAttribute attribute)
		{
			return input;
		}
	}
}

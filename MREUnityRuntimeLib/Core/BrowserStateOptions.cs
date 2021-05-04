// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace MixedRealityExtension
{
	/// <summary>
	/// Modifiable browser Instance Options
	/// </summary>
	public class BrowserStateOptions
	{
		/// <summary>
		/// Hacky solution for message collation with the server
		/// </summary>
		public Guid? MessageId;

		/// <summary>
		/// The URI to navigate to.
		/// </summary>
		public string Url;
	}
}

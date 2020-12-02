// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;

namespace MixedRealityExtension.Assets
{
	/// <summary>
	/// Contains the assets loaded from a particular container.
	/// </summary>
	public struct AssetGroup
	{
		/// <summary>
		/// The origin of these assets.
		/// </summary>
		public AssetSource Source;

		/// <summary>
		/// The loaded assets.
		/// </summary>
		public Dictionary<Guid, UnityEngine.Object> Assets;
	}
}

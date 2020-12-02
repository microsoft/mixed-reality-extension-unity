// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Core.Interfaces;

namespace MixedRealityExtension.PluginInterfaces
{
	/// <summary>
	/// Classes that implement this interface can be used to generate engine instances of text components
	/// </summary>
	public interface ITextFactory
	{
		/// <summary>
		/// Generate engine text on the given actor with the given properties
		/// </summary>
		/// <param name="actor">The actor acting as anchor</param>
		/// <returns>An engine-specific reference to the created text</returns>
		IText CreateText(IActor actor);
	}
}

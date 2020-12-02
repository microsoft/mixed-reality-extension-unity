// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Core.Interfaces;
using MixedRealityExtension.Core.Types;
using System;
using UnityEngine;

namespace MixedRealityExtension.PluginInterfaces
{
	/// <summary>
	/// Classes that implement this interface can be used to generate engine primitives
	/// </summary>
	public interface IPrimitiveFactory
	{
		/// <summary>
		/// Create a new Unity mesh with a known shape
		/// </summary>
		/// <param name="definition">The shape and size of the primitive to create</param>
		/// <returns>The mesh of the newly created primitive</returns>
		Mesh CreatePrimitive(PrimitiveDefinition definition);
	}
}

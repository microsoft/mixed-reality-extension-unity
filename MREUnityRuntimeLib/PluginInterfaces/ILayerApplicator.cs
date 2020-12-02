// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using CollisionLayer = MixedRealityExtension.Core.CollisionLayer;
using Collider = UnityEngine.Collider;

namespace MixedRealityExtension.PluginInterfaces
{
	/// <summary>
	/// Apply an MRE collider layers to Unity colliders.
	/// </summary>
	public interface ILayerApplicator
	{
		/// <summary>
		/// The Unity layer new actors should be created on.
		/// </summary>
		byte DefaultLayer { get; }

		/// <summary>
		/// Apply a layer to a collider.
		/// </summary>
		/// <param name="layer">An MRE collision layer</param>
		/// <param name="collider">A Unity collider</param>
		void ApplyLayerToCollider(CollisionLayer? layer, Collider collider);

		/// <summary>
		/// Get a collider's layer.
		/// </summary>
		/// <param name="collider">The collider.</param>
		/// <returns>The layer the given collider is on.</returns>
		CollisionLayer DetermineLayerOfCollider(Collider collider);
	}
}

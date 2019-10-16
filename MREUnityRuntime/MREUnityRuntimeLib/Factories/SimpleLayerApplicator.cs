// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using CollisionLayer = MixedRealityExtension.Core.CollisionLayer;
using ILayerApplicator = MixedRealityExtension.PluginInterfaces.ILayerApplicator;
using Collider = UnityEngine.Collider;

namespace MixedRealityExtension.Factories
{
	/// <summary>
	/// A simple implementation of ILayerApplicator that simply sets collision actors' `layer` properties.
	/// </summary>
	public class SimpleLayerApplicator : ILayerApplicator
	{
		protected readonly byte defaultLayer;
		protected readonly byte navigationLayer;
		protected readonly byte hologramLayer;
		protected readonly byte uiLayer;

		/// <inheritdoc />
		public byte DefaultLayer => defaultLayer;

		/// <summary>
		/// Apply the given Unity layers to MRE colliders.
		/// </summary>
		/// <param name="defaultLayer"></param>
		/// <param name="navigationLayer"></param>
		/// <param name="hologramLayer"></param>
		/// <param name="uiLayer"></param>
		public SimpleLayerApplicator(byte defaultLayer, byte navigationLayer, byte hologramLayer, byte uiLayer)
		{
			this.defaultLayer = defaultLayer;
			this.navigationLayer = navigationLayer;
			this.hologramLayer = hologramLayer;
			this.uiLayer = uiLayer;
		}

		/// <inheritdoc />
		public virtual void ApplyLayerToCollider(CollisionLayer? layer, Collider collider)
		{
			if (!layer.HasValue) return;

			switch (layer)
			{
				case CollisionLayer.Default:
					collider.gameObject.layer = defaultLayer;
					break;
				case CollisionLayer.Navigation:
					collider.gameObject.layer = navigationLayer;
					break;
				case CollisionLayer.Hologram:
					collider.gameObject.layer = hologramLayer;
					break;
				case CollisionLayer.UI:
					collider.gameObject.layer = uiLayer;
					break;
			}
		}

		/// <inheritdoc />
		public virtual CollisionLayer DetermineLayerOfCollider(Collider collider)
		{
			if (collider.gameObject.layer == navigationLayer)
			{
				return CollisionLayer.Navigation;
			}
			else if (collider.gameObject.layer == hologramLayer)
			{
				return CollisionLayer.Hologram;
			}
			else if (collider.gameObject.layer == uiLayer)
			{
				return CollisionLayer.UI;
			}
			else
			{
				return CollisionLayer.Default;
			}
		}
	}
}

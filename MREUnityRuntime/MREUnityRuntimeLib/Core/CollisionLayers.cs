// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace MixedRealityExtension.Core
{
	/// <summary>
	/// The layers available for MRE colliders.
	/// </summary>
	public enum CollisionLayer
	{
		/// <summary>
		/// Good for most actors. These will collide with all "physical" things: other default actors,
		/// navigation actors, and the non-MRE environment. It also blocks the UI cursor and receives press/grab events.
		/// </summary>
		Default,
		/// <summary>
		/// For actors considered part of the environment. Can move/teleport onto these colliders,
		/// but cannot click or grab them. For example, the floor, an invisible wall, or an elevator platform.
		/// </summary>
		Navigation,
		/// <summary>
		/// For "non-physical" actors. Only interact with the cursor (with press/grab events) and other holograms.
		/// For example, if you wanted a group of actors to behave as a separate physics simulation
		/// from the main scene.
		/// </summary>
		Hologram,
		/// <summary>
		/// Actors in this layer do not collide with anything but the UI cursor.
		/// </summary>
		UI
	}
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using MixedRealityExtension.Core.Types;
using System;
using System.Collections.Generic;

namespace MixedRealityExtension.Core.Collision
{
	/// <summary>
	/// Class that contains all of the data that is provided during a collision.
	/// </summary>
	public class CollisionData
	{
		/// <summary>
		/// Gets the id of the other actor we have collided with.
		/// </summary>
		public Guid otherActorId { get; internal set; }

		/// <summary>
		/// Gets the enumerable of contact points that happened during the collision.
		/// </summary>
		public IEnumerable<ContactPoint> Contacts { get; internal set; }

		/// <summary>
		/// Gets the impulse of the collision.
		/// </summary>
		public MWVector3 Impulse { get; internal set; }

		/// <summary>
		/// Gets the relative velocity of the collision.
		/// </summary>
		public MWVector3 RelativeVelocity { get; internal set; }
	}
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using MixedRealityExtension.Core.Types;

namespace MixedRealityExtension.Core.Collision
{
	/// <summary>
	/// Class that contains the data from a contact point in a collision.
	/// </summary>
	public class ContactPoint
	{
		/// <summary>
		/// Gets the normal of the collision contact point.
		/// </summary>
		public MWVector3 Normal { get; internal set; }

		/// <summary>
		/// Gets the point of the collision contact point.
		/// </summary>
		public MWVector3 Point { get; internal set; }

		/// <summary>
		/// Gets the separation of the collision contact point.
		/// </summary>
		public float Separation { get; internal set; }
	}
}

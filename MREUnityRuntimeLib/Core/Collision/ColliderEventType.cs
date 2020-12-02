// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace MixedRealityExtension.Core.Collision
{
	[Flags]
	public enum ColliderEventType
	{
		/// <summary>
		/// No collider events.
		/// </summary>
		None = 0,

		/// <summary>
		/// Event fired when a trigger volume is entered by an actor.
		/// </summary>
		TriggerEnter = 1,

		/// <summary>
		/// Event fired when a trigger volume is exited by an actor.
		/// </summary>
		TriggerExit = 2,

		/// <summary>
		/// Event fired when a collision has entered between the attached actor and another actor.
		/// </summary>
		CollisionEnter = 4,

		/// <summary>
		/// Event fired when a collision has exited between the attached actor and another actor.
		/// </summary>
		CollisionExit = 8
	}
}

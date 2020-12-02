// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Patching.Types;

namespace MixedRealityExtension.Animation
{
	/// <summary>
	/// Animation Keyframe
	/// </summary>
	public class MWAnimationKeyframe
	{
		/// <summary>
		/// The time offset (in seconds) from the start of the animation when this keyframe should be applied
		/// </summary>
		public float Time;

		/// <summary>
		/// The value of this keyframe
		/// </summary>
		public ActorPatch Value;
	}
}

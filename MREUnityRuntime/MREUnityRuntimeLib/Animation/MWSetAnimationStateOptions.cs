// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MixedRealityExtension.Animation
{
	/**
	 * Parameters to the `actor.setAnimationState` call. All values are optional. Only supplied value will be applied.
	 */
	public class MWSetAnimationStateOptions
	{
		/**
		 * The current animation time (in seconds).
		 */
		public float? Time { get; set; }
		/**
		 * The speed of animation playback. Negative values go backward. Zero is stopped. Animations stopped this way still
		 * influence the actor transform according to their weight. To remove this animation's influence, set its weight to
		 * zero using the `actor.setAnimationState` call, or disable it using the `actor.disableAnimation` call.
		 */
		public float? Speed { get; set; }
		/**
		 * Whether or not to enable this animation.
		 */
		public bool? Enabled { get; set; }
	}
}

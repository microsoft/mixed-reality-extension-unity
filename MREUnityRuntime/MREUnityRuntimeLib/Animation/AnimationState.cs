// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MixedRealityExtension.Animation
{
	/// <summary>
	/// Class that represents the state of an animation.
	/// </summary>
	public class MWActorAnimationState
	{
		/// <summary>
		/// The id of the actor of the animation.
		/// </summary>
		public Guid ActorId;

		/// <summary>
		/// The name of the animation.
		/// </summary>
		public string AnimationName;

		/// <summary>
		/// All the state options.
		/// </summary>
		public MWSetAnimationStateOptions State;
	}
}

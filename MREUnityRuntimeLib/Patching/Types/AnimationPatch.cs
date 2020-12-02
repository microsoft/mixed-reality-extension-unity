// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Animation;
using MixedRealityExtension.Messaging.Payloads.Converters;
using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace MixedRealityExtension.Patching.Types
{
	/// <summary>
	/// A serialized animation definition
	/// </summary>
	public class AnimationPatch : Patchable<AnimationPatch>
	{
		/// <summary>
		/// Generated unique ID of this animation
		/// </summary>
		public Guid Id;

		/// <summary>
		/// Non-unique name of this animation
		/// </summary>
		[PatchProperty]
		public string Name { get; set; }

		/// <summary>
		/// The server time (milliseconds since the UNIX epoch) when the animation was started
		/// </summary>
		[PatchProperty]
		public long? BasisTime { get; set; }

		/// <summary>
		/// The current playback time, based on basis time and speed
		/// </summary>
		[PatchProperty]
		public float? Time { get; set; }

		/// <summary>
		/// Playback speed multiplier
		/// </summary>
		[PatchProperty]
		public float? Speed { get; set; }

		/// <summary>
		/// When multiple animations play together, this is the relative strength of this instance
		/// </summary>
		[PatchProperty]
		public float? Weight { get; set; }

		/// <summary>
		/// What happens when the animation hits the last frame
		/// </summary>
		[PatchProperty]
		public MWAnimationWrapMode? WrapMode { get; set; }

		/// <summary>
		/// Convenience property for setting Weight and Basis/Time
		/// </summary>
		[PatchProperty]
		public bool? IsPlaying { get; set; }

		/// <summary>
		/// What runtime objects are being animated
		/// </summary>
		public Guid[] TargetIds { get; set; }

		/// <summary>
		/// The ID of the AnimationData bound to this animation
		/// </summary>
		public Guid? DataId { get; set; }

		/// <summary>
		/// The length in seconds of the animation
		/// </summary>
		public float? Duration { get; set; }

		public void Merge(AnimationPatch other)
		{
			if (other.Name != null) Name = other.Name;
			if (other.BasisTime.HasValue) BasisTime = other.BasisTime;
			if (other.Time.HasValue) Time = other.Time;
			if (other.Speed.HasValue) Speed = other.Speed;
			if (other.Weight.HasValue) Weight = other.Weight;
			if (other.WrapMode.HasValue) WrapMode = other.WrapMode;
		}
	}
}

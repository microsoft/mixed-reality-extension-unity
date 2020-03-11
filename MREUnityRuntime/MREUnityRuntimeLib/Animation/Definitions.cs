// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using Newtonsoft.Json.Linq;
using System.Linq;

namespace MixedRealityExtension.Animation
{
	/// <summary>
	/// Keyframe data for an animation (cacheable version)
	/// </summary>
	public class AnimationDataCached : UnityEngine.ScriptableObject
	{
		/// <summary>
		/// The animation keyframe data
		/// </summary>
		public Track[] Tracks;

		public float Duration
		{
			get
			{
				return Tracks.Select(t => t.Keyframes[t.Keyframes.Length - 1].Time).Max();
			}
		}
	}

	/// <summary>
	/// Keyframe data for an animation
	/// </summary>
	public struct AnimationData
	{
		/// <summary>
		/// The animation keyframe data
		/// </summary>
		public Track[] Tracks;
	}

	/// <summary>
	/// The timeline of values for an animation target property
	/// </summary>
	public struct Track
	{
		/// <summary>
		/// A path to the property to animate
		/// </summary>
		public string Target;

		private TargetPath targetPath;
		internal TargetPath TargetPath
		{
			get
			{
				if (targetPath == null)
				{
					targetPath = new TargetPath(Target);
				}
				return targetPath;
			}
		}

		/// <summary>
		/// The values to animate the target through
		/// </summary>
		public Keyframe[] Keyframes;

		/// <summary>
		/// Whether the keyframe values are relative to 0 or to the target's current property value. Defaults to false.
		/// </summary>
		public bool? Relative;
	}

	/// <summary>
	/// The value of an animation property at a moment in time
	/// </summary>
	public struct Keyframe
	{
		/// <summary>
		/// The time in seconds from the start of the animation.
		/// </summary>
		public float Time;

		/// <summary>
		/// The property's value at this instant, or a reference to another property.
		/// </summary>
		public JObject Value;

		/// <summary>
		/// How the value approaches this frame's value. Defaults to linear (0, 0, 1, 1).
		/// </summary>
		public (float, float, float, float)? Easing;
	}
}

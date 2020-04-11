// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;
using CubicBezier = MixedRealityExtension.Util.CubicBezier;

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

		public float Duration => Tracks.Select(t => t.Keyframes[t.Keyframes.Length - 1].Time).Max();

		public bool NeedsImplicitKeyframes => Tracks.Any(track => track.Keyframes[0].Time > 0 || track.Relative == true);
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
	public class Track
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

		/// <summary>
		/// Controls between-frame interpolation. If not provided, frames will not interpolate.
		/// </summary>
		public float[] Easing;

		private CubicBezier cubicBezier;
		/// <summary>
		/// The cubic bezier computer for the track's easing function
		/// </summary>
		public CubicBezier Bezier
		{
			get
			{
				if (cubicBezier == null && Easing != null)
				{
					cubicBezier = new CubicBezier(Easing[0], Easing[1], Easing[2], Easing[3]);
				}
				return cubicBezier;
			}
		}
	}

	/// <summary>
	/// The value of an animation property at a moment in time
	/// </summary>
	public class Keyframe
	{
		/// <summary>
		/// The time in seconds from the start of the animation.
		/// </summary>
		public float Time;

		/// <summary>
		/// The property's value at this instant, or a reference to another property.
		/// </summary>
		public JToken Value;

		private TargetPath valuePath = null;
		private bool valuePathTested = false;

		public TargetPath ValuePath
		{
			get
			{
				if (!valuePathTested)
				{
					valuePathTested = true;
					if (Value.Type == JTokenType.String)
					{
						try
						{
							valuePath = new TargetPath(Value.Value<string>());
						}
						catch { }
					}
				}
				return valuePath;
			}
		}

		/// <summary>
		/// How the value approaches this frame's value. Defaults to linear (0, 0, 1, 1).
		/// </summary>
		public float[] Easing;

		private CubicBezier cubicBezier;
		/// <summary>
		/// The cubic bezier computer for the frame's easing function
		/// </summary>
		public CubicBezier Bezier
		{
			get
			{
				if (cubicBezier == null && Easing != null)
				{
					cubicBezier = new CubicBezier(Easing[0], Easing[1], Easing[2], Easing[3]);
				}
				return cubicBezier;
			}
		}
	}
}

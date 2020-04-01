// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.API;
using MixedRealityExtension.Core;
using MixedRealityExtension.Patching;
using MixedRealityExtension.Patching.Types;
using MixedRealityExtension.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace MixedRealityExtension.Animation
{
	internal class Animation : BaseAnimation
	{
		public Guid DataId { get; }

		public AnimationDataCached Data { get; private set; }

		private bool DataSet = false;

		private Dictionary<string, Guid> TargetMap;

		/// <summary>
		/// Only used for the duration of the Update method
		/// </summary>
		private Dictionary<Guid, ActorPatch> TargetPatches = new Dictionary<Guid, ActorPatch>(2);

		/// <summary>
		/// When an animation is stopping (by weight == 0 || speed == 0), this flag indicates
		/// we should update one last time before stopping
		/// </summary>
		private bool StopUpdating = true;

		/// <summary>
		/// The index of the keyframe used last update
		/// </summary>
		private int[] LastKeyframeIndex;

		/// <summary>
		/// Reference frames for relative or non-zero start animations. Indexed by track index
		/// </summary>
		private Keyframe[] ImplicitStartKeyframes;

		private static JTokenPool TokenPool = new JTokenPool();

		public Animation(AnimationManager manager, Guid id, Guid dataId, Dictionary<string, Guid> targetMap) : base(manager, id)
		{
			DataId = dataId;
			TargetMap = targetMap;

			MREAPI.AppsAPI.AssetCache.OnCached(DataId, cacheData =>
			{
				Data = (AnimationDataCached)cacheData;
				DataSet = true;
				LastKeyframeIndex = new int[Data.Tracks.Length];
				ImplicitStartKeyframes = new Keyframe[Data.Tracks.Length];
				if (Weight > 0 && Data.NeedsImplicitKeyframes)
				{
					InitializeImplicitStartKeyframes();
				}
			});
		}

		public override void ApplyPatch(AnimationPatch patch)
		{
			var wasPlaying = IsPlaying;
			base.ApplyPatch(patch);
			if (!wasPlaying && IsPlaying && (Data?.NeedsImplicitKeyframes ?? false))
			{
				InitializeImplicitStartKeyframes();
			}

			if (IsPlaying)
			{
				// Exec the update at least once after a patch, even if Speed == 0
				StopUpdating = false;
			}
		}

		public override AnimationPatch GeneratePatch()
		{
			var patch = base.GeneratePatch();
			patch.DataId = DataId;
			return patch;
		}

		internal override void Update()
		{
			if (Data == null)
			{
				// only way for Data to be unset is if it's unloaded
				if (DataSet)
				{
					manager.DeregisterAnimation(this);
				}
				return;
			}

			// normalize time to animation length based on wrap settings
			float currentTime;
			if (Weight > 0 && Speed != 0)
			{
				// normal operation
				currentTime = (AnimationManager.LocalUnixNow() - BasisTime) * Speed / 1000;
				StopUpdating = false;
			}
			else if (!StopUpdating)
			{
				// supposed to stop, but run one last update
				currentTime = Time;
				StopUpdating = true;
			}
			else
			{
				// don't update
				return;
			}

			currentTime = ApplyWrapMode(currentTime);

			// process tracks
			for (var ti = 0; ti < Data.Tracks.Length; ti++)
			{
				var track = Data.Tracks[ti];
				(Keyframe prevFrame, Keyframe nextFrame) = GetActiveKeyframes(ti, currentTime);

				// either no keyframes, or time out of range
				if (prevFrame == null)
				{
					continue;
				}

				var linearT = (currentTime - prevFrame.Time) / (nextFrame.Time - prevFrame.Time);

				// compute new value for targeted field
				var interpolatedToken = TokenPool.Lease(prevFrame.Value.Type);
				Interpolations.Interpolate(prevFrame.Value, nextFrame.Value, linearT, ref interpolatedToken, nextFrame.Bezier ?? track.Bezier);

				// mix starting values with relative keyframe values
				JToken mixedToken = interpolatedToken;
				if (track.Relative == true)
				{
					mixedToken = TokenPool.Lease(interpolatedToken.Type);
					Interpolations.ResolveRelativeValue(ImplicitStartKeyframes[ti].Value, interpolatedToken, ref mixedToken);
				}

				// collect track result in a patch
				var targetId = TargetMap[track.TargetPath.Placeholder];
				if (track.TargetPath.AnimatibleType == "actor")
				{
					ActorPatch patch = TargetPatches.GetOrCreate(targetId, () => new ActorPatch(targetId));
					patch.WriteToPath(track.TargetPath, mixedToken, 0);
				}

				// make sure JTokens are reused
				TokenPool.Return(interpolatedToken);
				if (track.Relative == true)
				{
					TokenPool.Return(mixedToken);
				}
			}

			// apply patches to all objects involved
			foreach (var kvp in TargetPatches)
			{
				var actor = (Actor)manager.App.FindActor(kvp.Key);
				actor?.ApplyPatch(kvp.Value);
				kvp.Value.Clear();
			}
		}

		private float ApplyWrapMode(float currentTime)
		{
			// From the documentation:
			//   For the float and double operands, the result of x % y for the finite x and y is the value z such that:
			//     The sign of z, if non-zero, is the same as the sign of x.
			//     The absolute value of z is the value produced by |x| - n * |y|
			//       where n is the largest possible integer that is less than or equal to |x| / |y|
			//       and |x| and |y| are the absolute values of x and y, respectively.
			//
			// We want the result to always be non-negative, so we're using the extended formula
			//   currentTime - rep * Data.Duration
			// everywhere instead of currentTime % Data.Duration.
			var rep = (int)Math.Floor(currentTime / Data.Duration);
			// UnityEngine.Debug.LogFormat("Now: {0}, Basis: {1}", AnimationManager.LocalUnixNow(), BasisTime);
			// UnityEngine.Debug.LogFormat("Time: {0}, Rep: {1}", currentTime, rep);

			if (WrapMode == MWAnimationWrapMode.Loop)
			{
				/*** Loop mode: seamlessly join anim end to the beginning
				 *    /  /  /|  /  /  /
				 *   /  /  / | /  /  /
				 *  /  /  /  |/  /  /
				 *  ---------+---------
				 * -3 -2 -1  0  1  2  3
				 */
				currentTime = currentTime - rep * Data.Duration;
			}
			else if (WrapMode == MWAnimationWrapMode.PingPong)
			{
				/*** PingPong mode: Every other iteration goes backward
				 *  \    /\  |  /\    /
				 *   \  /  \ | /  \  /
				 *    \/    \|/    \/
				 *  ---------+---------
				 * -3 -2 -1  0  1  2  3
				 */
				if (rep % 2 == 0)
				{
					// forward case
					currentTime = currentTime - rep * Data.Duration;
				}
				else
				{
					// backward case
					currentTime = Data.Duration - (currentTime - rep * Data.Duration);
				}
			}
			else if (WrapMode == MWAnimationWrapMode.Once)
			{
				// 
				/*** Once mode: Clamp to range
				 *               ______
				 *           |  /
				 *           | /
				 *           |/
				 *  ---------+---------
				 * -3 -2 -1  0  1  2  3
				 */
				currentTime = Math.Max(0, Math.Min(Data.Duration, currentTime));
			}

			return currentTime;
		}

		private (Keyframe, Keyframe) GetActiveKeyframes(int trackIndex, float currentTime)
		{
			var ti = trackIndex;
			var track = Data.Tracks[ti];

			// grab the leading keyframe from the last update (might be first frame if first update)
			Keyframe nextFrame = track.Keyframes[LastKeyframeIndex[ti]];

			// grab trailing keyframe from last update: frame before nextFrame, or the implicit start frame (might be null)
			Keyframe prevFrame = LastKeyframeIndex[ti] > 0 ? track.Keyframes[LastKeyframeIndex[ti] - 1] : ImplicitStartKeyframes[ti];

			// test to see if current frames are usable
			bool GoodFrames() => prevFrame != null && prevFrame.Time <= currentTime && nextFrame.Time > currentTime;

			// if the current time isn't in that range, try the "next" keyframe based on speed sign
			if (!GoodFrames())
			{
				// going forward
				if (Speed > 0 && LastKeyframeIndex[ti] < track.Keyframes.Length - 1)
				{
					prevFrame = nextFrame;
					nextFrame = track.Keyframes[++LastKeyframeIndex[ti]];
				}
				// going backward
				else if (Speed < 0 && LastKeyframeIndex[ti] > 0)
				{
					nextFrame = prevFrame;
					prevFrame = --LastKeyframeIndex[ti] > 0 ? track.Keyframes[LastKeyframeIndex[ti] - 1] : ImplicitStartKeyframes[ti];
				}
			}

			// if it's still not in range, we just have to search
			if (!GoodFrames())
			{
				prevFrame = ImplicitStartKeyframes[ti];
				nextFrame = track.Keyframes[0];
				LastKeyframeIndex[ti] = 0;
				while (!GoodFrames() && LastKeyframeIndex[ti] < track.Keyframes.Length - 1)
				{
					prevFrame = nextFrame;
					nextFrame = track.Keyframes[++LastKeyframeIndex[ti]];
				}
			}

			// we found the right frame pair, return them
			if (GoodFrames())
			{
				return (prevFrame, nextFrame);
			}
			// the provided time is not between any two frames in this animation
			else
			{
				return (null, null);
			}
		}

		private void InitializeImplicitStartKeyframes()
		{
			var serializer = JsonSerializer.Create(Constants.SerializerSettings);

			for (var ti = 0; ti < Data.Tracks.Length; ti++)
			{
				var track = Data.Tracks[ti];

				// explicit start, no need to generate one
				if (track.Keyframes[0].Time <= 0 && track.Relative != true) continue;

				// get a patch of the target type
				var targetId = TargetMap[track.TargetPath.Placeholder];
				IPatchable patch;
				if (track.TargetPath.AnimatibleType == "actor")
				{
					// pull current value
					var actor = (Actor)manager.App.FindActor(targetId);
					ActorPatch actorPatch = TargetPatches.GetOrCreate(targetId, () => new ActorPatch(targetId));
					actorPatch = actor?.GeneratePatch(actorPatch, track.TargetPath);
					patch = actorPatch;
				}
				else continue;

				// traverse patch for the targeted field
				JToken json = JObject.FromObject(patch, serializer);
				patch.Clear();
				var parseFail = false;
				foreach (var pathPart in track.TargetPath.PathParts)
				{
					if (json.Type == JTokenType.Object)
					{
						json = ((JObject)json).GetValue(pathPart);
					}
					else
					{
						parseFail = true;
						break;
					}
				}
				if (parseFail) continue;

				// generate keyframe
				if (ImplicitStartKeyframes[ti] == null)
				{
					ImplicitStartKeyframes[ti] = new Keyframe()
					{
						Time = 0f,
						Value = json
					};
				}
				else
				{
					ImplicitStartKeyframes[ti].Value = json;
				}
			}
		}

		private class JTokenPool
		{
			private Stack<JObject> jObjectPool = new Stack<JObject>(3);
			private Stack<JValue> jValuePool = new Stack<JValue>(3);

			public JToken Lease(JTokenType type)
			{
				if (type == JTokenType.Object)
				{
					return jObjectPool.Count > 0 ? jObjectPool.Pop() : new JObject();
				}
				else
				{
					return jValuePool.Count > 0 ? jValuePool.Pop() : new JValue(0);
				}
			}

			public void Return(JToken token)
			{
				if (token.Type == JTokenType.Object)
				{
					jObjectPool.Push((JObject)token);
				}
				else
				{
					jValuePool.Push((JValue)token);
				}
			}
		}
	}
}

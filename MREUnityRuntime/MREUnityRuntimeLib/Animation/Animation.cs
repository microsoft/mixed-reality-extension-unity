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

		private TargetPath[] ResolvedTargetPaths;

		private static JTokenPool TokenPool = new JTokenPool();
		private static CubicBezier LinearEasing = new CubicBezier(0, 0, 1, 1);

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

				ResolvedTargetPaths = new TargetPath[Data.Tracks.Length];
				for (var i = 0; i < Data.Tracks.Length; i++)
				{
					var t = Data.Tracks[i];
					ResolvedTargetPaths[i] = t.TargetPath.ResolvePlaceholder(TargetMap[t.TargetPath.Placeholder]);
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

		internal override void Update(long serverTime)
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
				currentTime = (serverTime - BasisTime) * Speed / 1000;
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
			IPatchable patch;
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

				// get realtime value for trailing frame
				JToken prevFrameValue = prevFrame.Value;
				if (
					// if previous frame value is a path
					prevFrame.ValuePath != null && (
					// get the current value from the actor as a patch
					!GetPatchAtPath(prevFrame.ValuePath, out patch) ||
					// convert patch to a JToken
					!GetTokenAtPath(patch, prevFrame.ValuePath, out prevFrameValue)))
				{
					// can't get current value, skip this track
					continue;
				}

				// get realtime value for leading frame (same as above)
				JToken nextFrameValue = nextFrame.Value;
				if (nextFrame.ValuePath != null && (
					!GetPatchAtPath(nextFrame.ValuePath, out patch) ||
					!GetTokenAtPath(patch, nextFrame.ValuePath, out nextFrameValue)))
				{
					continue;
				}

				// compute new value for targeted field
				var outputToken = TokenPool.Lease(prevFrameValue.Type);
				Interpolations.Interpolate(prevFrameValue, nextFrameValue, linearT, ref outputToken, nextFrame.Bezier ?? track.Bezier);

				// mix starting values with relative keyframe values
				if (track.Relative == true)
				{
					var temp = TokenPool.Lease(outputToken.Type);
					Interpolations.ResolveRelativeValue(ImplicitStartKeyframes[ti].Value, outputToken, ref temp);
					TokenPool.Return(outputToken);
					outputToken = temp;
				}

				// mix computed value with the result of any other anims targeting the same property
				var targetId = TargetMap[track.TargetPath.Placeholder];
				var targetPathId = ResolvedTargetPaths[ti];
				var blendData = manager.AnimBlends.GetOrCreate(targetPathId, () => new AnimationManager.AnimBlend(targetPathId));
				if (blendData.TotalWeight == 0)
				{
					blendData.TotalWeight = Weight;
					if (blendData.CurrentValue == null)
					{
						blendData.CurrentValue = outputToken.DeepClone();
					}
					else
					{
						var temp = blendData.CurrentValue;
						blendData.CurrentValue = outputToken;
						outputToken = temp;
					}
				}
				else
				{
					blendData.TotalWeight += Weight;
					var blended = TokenPool.Lease(outputToken.Type);
					Interpolations.Interpolate(outputToken, blendData.CurrentValue, Weight / blendData.TotalWeight, ref blended, LinearEasing);

					TokenPool.Return(blendData.CurrentValue);
					blendData.CurrentValue = blended;
				}

				// make sure JTokens are reused
				TokenPool.Return(outputToken);
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
			bool GoodFrames() => prevFrame != null && prevFrame.Time < nextFrame.Time && prevFrame.Time <= currentTime && nextFrame.Time >= currentTime;

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
			for (var ti = 0; ti < Data.Tracks.Length; ti++)
			{
				var track = Data.Tracks[ti];

				// explicit start, no need to generate one
				if (track.Keyframes[0].Time <= 0 && track.Relative != true) continue;

				// get a patch of the target type, traverse patch for the targeted field
				if (!GetPatchAtPath(track.TargetPath, out IPatchable patch) || !GetTokenAtPath(patch, track.TargetPath, out JToken json))
				{
					continue;
				}

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

		private bool GetPatchAtPath(TargetPath path, out IPatchable patch)
		{
			var targetId = TargetMap[path.Placeholder];
			if (path.AnimatibleType == "actor")
			{
				// pull current value
				var actor = (Actor)manager.App.FindActor(targetId);
				ActorPatch actorPatch = (ActorPatch)manager.AnimInputPatches.GetOrCreate(targetId, () => new ActorPatch(targetId));
				if (actor?.GeneratePatch(actorPatch, path) != null)
				{
					patch = actorPatch;
					return true;
				}
			}

			patch = null;
			return false;
		}

		private static readonly JsonSerializer Serializer = JsonSerializer.Create(Constants.SerializerSettings);

		private static bool GetTokenAtPath(IPatchable patch, TargetPath path, out JToken token)
		{
			// Note: The serializer is supposed to reuse token objects when possible, but I don't have any insight into the reuse algorithm,
			// so this could be generating garbage. TODO: Investigate this implementation.
			token = JObject.FromObject(patch, Serializer);
			foreach (var pathPart in path.PathParts)
			{
				if (token.Type == JTokenType.Object)
				{
					token = ((JObject)token).GetValue(pathPart);
				}
				else
				{
					token = null;
					return false;
				}
			}
			return true;
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

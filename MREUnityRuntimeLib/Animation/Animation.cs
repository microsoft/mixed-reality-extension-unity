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
		/// Reference frames for non-zero start animations. Indexed by track index
		/// </summary>
		private Keyframe[] ImplicitStartKeyframes;

		/// <summary>
		/// Absolute values of relative keyframes. Indexed by track index, then keyframe index
		/// </summary>
		private Keyframe[][] ResolvedRelativeKeyframes;

		/// <summary>
		/// Keyframe target paths, with placeholders swapped for GUIDs. Used for blending output with other anims
		/// </summary>
		private TargetPath[] ResolvedTargetPaths;

		/// <summary>
		/// Animation weight that lags behind by one frame. Used for "one more update" calculation.
		/// </summary>
		private float LastWeight = 0;

		private static JTokenPool TokenPool = new JTokenPool();
		private static CubicBezier LinearEasing = new CubicBezier(0, 0, 1, 1);

		public Animation(AnimationManager manager, Guid id, Guid dataId, Dictionary<string, Guid> targetMap) : base(manager, id)
		{
			DataId = dataId;
			TargetMap = targetMap;

			manager.App.AssetManager.OnSet(DataId, cacheData =>
			{
				DataSet = true;
				if (cacheData.Asset == null)
				{
					Data = null;
					return;
				}

				Data = (AnimationDataCached)cacheData.Asset;
				LastKeyframeIndex = new int[Data.Tracks.Length];
				ImplicitStartKeyframes = new Keyframe[Data.Tracks.Length];
				ResolvedRelativeKeyframes = new Keyframe[Data.Tracks.Length][];
				ResolvedTargetPaths = new TargetPath[Data.Tracks.Length];

				for (var i = 0; i < Data.Tracks.Length; i++)
				{
					var t = Data.Tracks[i];
					ResolvedTargetPaths[i] = t.TargetPath.ResolvePlaceholder(TargetMap[t.TargetPath.Placeholder]);

					if (t.Relative == true)
					{
						ResolvedRelativeKeyframes[i] = new Keyframe[t.Keyframes.Length];
					}
				}

				if (Weight > 0 && Data.NeedsImplicitKeyframes)
				{
					InitializeImplicitKeyframes();
				}
			});
		}

		public override void ApplyPatch(AnimationPatch patch)
		{
			var wasPlaying = IsPlaying;
			base.ApplyPatch(patch);
			if (!wasPlaying && IsPlaying && (Data?.NeedsImplicitKeyframes ?? false))
			{
				InitializeImplicitKeyframes();
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
				LastWeight = Weight;
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
				Track track = Data.Tracks[ti];
				bool usesPrevFrameValue = false, usesNextFrameValue = false;

				(Keyframe prevFrame, Keyframe nextFrame) = GetActiveKeyframes(ti, currentTime);

				// either no keyframes, or time out of range
				if (prevFrame == null)
				{
					continue;
				}

				float linearT = (currentTime - prevFrame.Time) / (nextFrame.Time - prevFrame.Time);

				// get realtime value for trailing frame
				JToken prevFrameValue = prevFrame.Value;
				if (prevFrame.ValuePath != null)
				{
					if (GetPatchAtPath(prevFrame.ValuePath, out IPatchable patch))
					{
						prevFrameValue = TokenPool.Lease(TargetPath.TypeOfPath[prevFrame.ValuePath.Path]);
						if (patch.ReadFromPath(prevFrame.ValuePath, ref prevFrameValue, 0))
						{
							usesPrevFrameValue = true;
						}
						else
						{
							TokenPool.Return(prevFrameValue);
							continue;
						}
					}
					else continue;
				}

				// get realtime value for leading frame (same as above)
				JToken nextFrameValue = nextFrame.Value;
				if (nextFrame.ValuePath != null)
				{
					if (GetPatchAtPath(nextFrame.ValuePath, out IPatchable patch))
					{
						nextFrameValue = TokenPool.Lease(TargetPath.TypeOfPath[nextFrame.ValuePath.Path]);
						if (patch.ReadFromPath(nextFrame.ValuePath, ref nextFrameValue, 0))
						{
							usesNextFrameValue = true;
						}
						else
						{
							TokenPool.Return(nextFrameValue);
							continue;
						}
					}
					else continue;
				}

				// compute new value for targeted field
				JToken outputToken = TokenPool.Lease(prevFrameValue);
				Interpolations.Interpolate(prevFrameValue, nextFrameValue, linearT, ref outputToken, nextFrame.Bezier ?? track.Bezier ?? LinearEasing);
				if (usesPrevFrameValue)
				{
					TokenPool.Return(prevFrameValue);
				}
				if (usesNextFrameValue)
				{
					TokenPool.Return(nextFrameValue);
				}

				// mix computed value with the result of any other anims targeting the same property
				AnimationManager.AnimBlend blendData = manager.AnimBlends.GetOrCreate(
					ResolvedTargetPaths[ti],
					() => new AnimationManager.AnimBlend(ResolvedTargetPaths[ti]));

				blendData.FinalUpdate = blendData.FinalUpdate || StopUpdating;
				if (blendData.TotalWeight == 0)
				{
					blendData.TotalWeight = LastWeight;
					if (blendData.CurrentValue == null)
					{
						blendData.CurrentValue = outputToken.DeepClone();
					}
					else
					{
						JToken temp = blendData.CurrentValue;
						blendData.CurrentValue = outputToken;
						TokenPool.Return(temp);
					}
				}
				else
				{
					blendData.TotalWeight += LastWeight;
					JToken temp = TokenPool.Lease(outputToken);
					Interpolations.Interpolate(outputToken, blendData.CurrentValue, LastWeight / blendData.TotalWeight, ref temp, LinearEasing);

					TokenPool.Return(blendData.CurrentValue);
					blendData.CurrentValue = temp;
				}
			}
		}

		private int WrapRepetition = 0;
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
			var oldRep = WrapRepetition;
			WrapRepetition = (int)Math.Floor(currentTime / Data.Duration);

			if (WrapMode == MWAnimationWrapMode.Loop)
			{
				/*** Loop mode: seamlessly join anim end to the beginning
				 *    /  /  /|  /  /  /
				 *   /  /  / | /  /  /
				 *  /  /  /  |/  /  /
				 *  ---------+---------
				 * -3 -2 -1  0  1  2  3
				 */
				currentTime = currentTime - WrapRepetition * Data.Duration;

				// this is a looping relative animation, reset reference frame for root motion
				if (Data.NeedsImplicitKeyframes && oldRep != WrapRepetition)
				{
					InitializeImplicitKeyframes(true);
				}
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
				if (WrapRepetition % 2 == 0)
				{
					// forward case
					currentTime = currentTime - WrapRepetition * Data.Duration;
				}
				else
				{
					// backward case
					currentTime = Data.Duration - (currentTime - WrapRepetition * Data.Duration);
				}
			}
			else if (WrapMode == MWAnimationWrapMode.Once)
			{
				/*** Once mode: Clamp to range
				 *               ______
				 *           |  /
				 *           | /
				 *           |/
				 *  ---------+---------
				 * -3 -2 -1  0  1  2  3
				 */

				currentTime = Math.Max(0, Math.Min(Data.Duration, currentTime));

				// check for completion
				if (Speed > 0 && currentTime == Data.Duration || Speed < 0 && currentTime == 0)
				{
					StopUpdating = true;
					MarkFinished(currentTime);
				}
			}

			return currentTime;
		}

		private (Keyframe, Keyframe) GetActiveKeyframes(int trackIndex, float currentTime)
		{
			var ti = trackIndex;
			var track = Data.Tracks[ti];
			var keyframes = track.Relative == true ? ResolvedRelativeKeyframes[ti] : track.Keyframes;

			// grab the leading keyframe from the last update (might be first frame if first update)
			Keyframe nextFrame = keyframes[LastKeyframeIndex[ti]];

			// grab trailing keyframe from last update: frame before nextFrame, or the implicit start frame (might be null)
			Keyframe prevFrame = LastKeyframeIndex[ti] > 0 ? keyframes[LastKeyframeIndex[ti] - 1] : ImplicitStartKeyframes[ti];

			// test to see if current frames are usable
			bool GoodFrames() => prevFrame != null && prevFrame.Time < nextFrame.Time && prevFrame.Time <= currentTime && nextFrame.Time >= currentTime;

			if (GoodFrames())
			{
				return (prevFrame, nextFrame);
			}
			// if the current time isn't in that range, try the "next" keyframe based on speed sign
			else
			{
				// going forward
				if (Speed > 0 && LastKeyframeIndex[ti] < keyframes.Length - 1)
				{
					prevFrame = nextFrame;
					nextFrame = keyframes[++LastKeyframeIndex[ti]];
				}
				// going backward
				else if (Speed < 0 && LastKeyframeIndex[ti] > 0)
				{
					nextFrame = prevFrame;
					prevFrame = --LastKeyframeIndex[ti] > 0 ? keyframes[LastKeyframeIndex[ti] - 1] : ImplicitStartKeyframes[ti];
				}
			}

			if (GoodFrames())
			{
				return (prevFrame, nextFrame);
			}
			// if it's still not in range, we just have to search
			else
			{
				prevFrame = ImplicitStartKeyframes[ti];
				nextFrame = keyframes[0];
				LastKeyframeIndex[ti] = 0;
				while (!GoodFrames() && LastKeyframeIndex[ti] < keyframes.Length - 1)
				{
					prevFrame = nextFrame;
					nextFrame = keyframes[++LastKeyframeIndex[ti]];
				}
			}

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

		private void InitializeImplicitKeyframes(bool recycleLastFrames = false)
		{
			for (var ti = 0; ti < Data.Tracks.Length; ti++)
			{
				var track = Data.Tracks[ti];

				// if looping, set implicit start exactly at the last frame of the previous loop
				if (recycleLastFrames && track.Relative == true)
				{
					var temp = ImplicitStartKeyframes[ti].Value;
					ImplicitStartKeyframes[ti].Value = ResolvedRelativeKeyframes[ti][track.Keyframes.Length - 1].Value;
					ResolvedRelativeKeyframes[ti][track.Keyframes.Length - 1].Value = temp;
				}
				// only snapshot start state if we need it
				else if (track.Keyframes[0].Time > 0 || track.Relative == true)
				{
					// generate keyframe
					if (ImplicitStartKeyframes[ti] == null)
					{
						ImplicitStartKeyframes[ti] = new Keyframe()
						{
							Time = 0f,
							Value = TokenPool.Lease(TargetPath.TypeOfPath[track.TargetPath.Path])
						};
					}

					// get a patch of the target type, traverse patch for the targeted field
					JToken json = ImplicitStartKeyframes[ti].Value;
					if (!GetPatchAtPath(track.TargetPath, out IPatchable patch) || !patch.ReadFromPath(track.TargetPath, ref json, 0))
					{
						continue;
					}
				}

				// resolve all relative keyframes now
				if (track.Relative == true)
				{
					for (var ki = 0; ki < track.Keyframes.Length; ki++)
					{
						var keyframe = track.Keyframes[ki];
						if (ResolvedRelativeKeyframes[ti][ki] == null)
						{
							ResolvedRelativeKeyframes[ti][ki] = new Keyframe()
							{
								Time = keyframe.Time,
								Value = TokenPool.Lease(keyframe.Value),
								Easing = keyframe.Easing
							};
						}

						Interpolations.ResolveRelativeValue(
							ImplicitStartKeyframes[ti].Value,
							keyframe.Value,
							ref ResolvedRelativeKeyframes[ti][ki].Value);
					}
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

		/*private static readonly JsonSerializer Serializer = JsonSerializer.Create(Constants.SerializerSettings);

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
		}*/
	}
}

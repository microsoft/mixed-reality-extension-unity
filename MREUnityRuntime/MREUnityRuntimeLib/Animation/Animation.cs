// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.API;
using MixedRealityExtension.Core;
using MixedRealityExtension.Patching;
using MixedRealityExtension.Patching.Types;
using MixedRealityExtension.Util;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;


namespace MixedRealityExtension.Animation
{
	internal class Animation : BaseAnimation
	{
		public Guid DataId { get; }

		public AnimationDataCached Data { get; private set; }

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

		public Animation(AnimationManager manager, Guid id, Guid dataId, Dictionary<string, Guid> targetMap) : base(manager, id)
		{
			DataId = dataId;
			TargetMap = targetMap;

			MREAPI.AppsAPI.AssetCache.OnCached(dataId, cacheData =>
			{
				Data = (AnimationDataCached)cacheData;
			});
		}

		public override void ApplyPatch(AnimationPatch patch)
		{
			base.ApplyPatch(patch);
			if (IsPlaying)
			{
				// Exec the update at least once, even if Speed == 0
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
			if (Data == null) return;

			/*************************************************
			 * Normalize time to animation length based on wrap settings
			 *************************************************/

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

			/*********************************************
			 * Process tracks
			 *********************************************/

			JToken jToken;
			JValue jValue = new JValue(false);
			JObject jObject = new JObject();

			foreach (var track in Data.Tracks)
			{
				Keyframe prevFrame = null, nextFrame = null;
				for (int i = 0; i < (track.Keyframes.Length - 1); i++)
				{
					if (track.Keyframes[i].Time <= currentTime && track.Keyframes[i + 1].Time > currentTime)
					{
						prevFrame = track.Keyframes[i];
						nextFrame = track.Keyframes[i + 1];
						break;
					}
				}
				// either no keyframes, or time out of range
				if (prevFrame == null)
				{
					continue;
				}

				var linearT = (currentTime - prevFrame.Time) / (nextFrame.Time - prevFrame.Time);

				// compute new value for targeted field
				if (prevFrame.Value.Type == JTokenType.Object)
				{
					jToken = jObject;
					Interpolations.Interpolate(prevFrame.Value, nextFrame.Value, linearT, ref jToken, nextFrame.Easing ?? track.Easing);
				}
				else
				{
					jToken = jValue;
					Interpolations.Interpolate(prevFrame.Value, nextFrame.Value, linearT, ref jToken, nextFrame.Easing ?? track.Easing);
				}

				var targetId = TargetMap[track.TargetPath.Placeholder];
				if (track.TargetPath.AnimatibleType == "actor")
				{
					ActorPatch patch = TargetPatches.GetOrCreate(targetId, () => new ActorPatch());
					patch.WriteToPath(track.TargetPath, jToken, 0);
				}
			}

			/***********************************************
			 * Apply patches to all objects involved
			 ***********************************************/

			foreach (var kvp in TargetPatches)
			{
				var actor = (Actor)manager.App.FindActor(kvp.Key);
				actor.ApplyPatch(kvp.Value);
				kvp.Value.Clear();
			}
		}
	}
}

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

		public Animation(AnimationManager manager, Guid id, Guid dataId, Dictionary<string, Guid> targetMap) : base(manager, id)
		{
			DataId = dataId;
			TargetMap = targetMap;

			MREAPI.AppsAPI.AssetCache.OnCached(dataId, cacheData =>
			{
				Data = (AnimationDataCached)cacheData;
			});
		}

		internal override void Update()
		{
			if (Weight <= 0 || Data == null) return;

			var currentTime = (float)(AnimationManager.LocalUnixNow() - BasisTime) / 1000;

			/*************************************************
			 * Normalize time to animation length based on wrap settings
			 *************************************************/

			if (WrapMode == MWAnimationWrapMode.Loop)
			{
				/*** Loop mode: seamlessly join anim end to the beginning
				 *    /  /  /|  /  /  /
				 *   /  /  / | /  /  /
				 *  /  /  /  |/  /  /
				 *  ---------+---------
				 * -3 -2 -1  0  1  2  3
				 */
				currentTime = currentTime % Data.Duration;
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
				var rep = (int)Math.Floor(currentTime / Data.Duration);
				if (rep % 2 == 0)
				{
					// forward case
					currentTime = currentTime % Data.Duration;
				}
				else
				{
					// backward case
					currentTime = Data.Duration - currentTime % Data.Duration;
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
				if (prevFrame == null)
				{
					UnityEngine.Debug.LogFormat("Keyframe not found for time {0}", currentTime);
					continue;
				}
				var timeFraction = (currentTime - prevFrame.Time) / (nextFrame.Time - prevFrame.Time);

				// compute new value for targeted field
				if (prevFrame.Value.Type == JTokenType.Object)
				{
					jToken = jObject;
					Interpolations.Interpolate(prevFrame.Value, nextFrame.Value, timeFraction, ref jToken, nextFrame.Easing);
				}
				else
				{
					jToken = jValue;
					Interpolations.Interpolate(prevFrame.Value, nextFrame.Value, timeFraction, ref jToken, nextFrame.Easing);
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

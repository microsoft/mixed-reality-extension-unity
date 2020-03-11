// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.API;
using MixedRealityExtension.Patching;
using MixedRealityExtension.Patching.Types;
using MixedRealityExtension.Util;
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
			// normalize time to animation length based on wrap settings
			if (WrapMode == MWAnimationWrapMode.Loop)
			{
				currentTime = currentTime % Data.Duration;
				if (currentTime < 0)
				{
					currentTime += Data.Duration;
				}
			}

			foreach (var track in Data.Tracks)
			{
				var targetId = TargetMap[track.TargetPath.Placeholder];
				Keyframe prevFrame, nextFrame;
				for (int i = 0; i < (track.Keyframes.Length - 1); i++)
				{
					if (track.Keyframes[i].Time <= currentTime && track.Keyframes[i + 1].Time > currentTime)
					{
						prevFrame = track.Keyframes[i];
						nextFrame = track.Keyframes[i + 1];
						break;
					}
				}

				// compute new value for targeted field


				if (track.TargetPath.Type == "actor")
				{
					ActorPatch patch = TargetPatches.GetOrCreate(targetId, () => new ActorPatch());
					patch.WriteToPath(track.TargetPath, track.key)
				}
			}
		}
	}
}

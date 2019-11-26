// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.App;
using MixedRealityExtension.Core;
using System;
using System.Collections.Generic;

namespace MixedRealityExtension.Animation
{
	internal class AnimationManager
	{
		private static DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		private readonly long OffsetUpdateThreshold = 50;

		private MixedRealityExtensionApp App;
		private readonly Dictionary<Guid, Animation> Animations = new Dictionary<Guid, Animation>(10);
		private long ServerTimeOffset = 0;

		public AnimationManager(MixedRealityExtensionApp app)
		{
			App = app;
		}

		public void RegisterAnimation(Animation anim)
		{
			Animations[anim.Id] = anim;
		}

		public void UpdateServerTimeOffset(long serverTime)
		{
			var latestOffset = serverTime - UnixNow();
			if (Math.Abs(latestOffset - ServerTimeOffset) > OffsetUpdateThreshold)
			{
				ServerTimeOffset = latestOffset;
			}
		}

		public void Update()
		{
			foreach (var anim in Animations.Values)
			{
				anim.Update();
			}
		}

		public long ServerNow()
		{
			return UnixNow() + ServerTimeOffset;
		}

		public static long UnixNow()
		{
			return (DateTime.UtcNow - Epoch).Ticks / 10_000;
		}
	}
}

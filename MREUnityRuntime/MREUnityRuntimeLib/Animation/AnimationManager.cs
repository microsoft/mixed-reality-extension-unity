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
		private readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		private readonly TimeSpan OffsetUpdateThreshold = new TimeSpan(500_000); // 50ms

		private MixedRealityExtensionApp App;
		private readonly Dictionary<Guid, Animation> Animations = new Dictionary<Guid, Animation>(10);
		private TimeSpan ServerTimeOffset = new TimeSpan(0);

		public AnimationManager(MixedRealityExtensionApp app)
		{
			App = app;
		}

		public void UpdateServerTimeOffset(long serverTime)
		{
			var serverDT = DateTimeOffset.FromUnixTimeMilliseconds(serverTime);
			var latestOffset = serverDT - DateTime.Now;
			if ((latestOffset - ServerTimeOffset).Duration() > OffsetUpdateThreshold)
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
	}
}

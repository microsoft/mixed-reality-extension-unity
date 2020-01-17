// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.App;
using MixedRealityExtension.Core;
using MixedRealityExtension.Core.Components;
using MixedRealityExtension.Messaging;
using MixedRealityExtension.Messaging.Commands;
using MixedRealityExtension.Messaging.Payloads;
using MixedRealityExtension.Patching.Types;
using MixedRealityExtension.Util;
using System;
using System.Collections.Generic;

namespace MixedRealityExtension.Animation
{
	internal class AnimationManager : ICommandHandlerContext
	{
		private static DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		private readonly long OffsetUpdateThreshold = 50;

		private MixedRealityExtensionApp App;
		private readonly Dictionary<Guid, Animation> Animations = new Dictionary<Guid, Animation>(10);
		private readonly Dictionary<Guid, AnimationPatch> PendingPatches = new Dictionary<Guid, AnimationPatch>(10);
		private long ServerTimeOffset = 0;

		public AnimationManager(MixedRealityExtensionApp app)
		{
			App = app;
		}

		public void RegisterAnimation(Animation anim)
		{
			Animations[anim.Id] = anim;
			if (PendingPatches.TryGetValue(anim.Id, out AnimationPatch patch))
			{
				anim.ApplyPatch(patch);
				PendingPatches.Remove(anim.Id);
			}
		}

		public void UpdateServerTimeOffset(long serverTime)
		{
			var latestOffset = serverTime - LocalUnixNow();
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
			return LocalUnixNow() + ServerTimeOffset;
		}

		[CommandHandler(typeof(AnimationUpdate))]
		private void OnAnimationUpdate(AnimationUpdate message, Action onCompleteCallback)
		{
			if (Animations.TryGetValue(message.Animation.Id, out Animation anim))
			{
				anim.ApplyPatch(message.Animation);
			}
			else if (PendingPatches.TryGetValue(message.Animation.Id, out AnimationPatch oldPatch))
			{
				// merge patches
				oldPatch.Merge(message.Animation);
			}
			else
			{
				// just write pending patch
				PendingPatches[message.Animation.Id] = message.Animation;
			}

			onCompleteCallback?.Invoke();
		}

		public static long LocalUnixNow()
		{
			return (DateTime.UtcNow - Epoch).Ticks / 10_000;
		}
	}
}

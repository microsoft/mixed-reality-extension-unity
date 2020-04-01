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
using System.Linq;

namespace MixedRealityExtension.Animation
{
	internal class AnimationManager : ICommandHandlerContext
	{
		private static DateTimeOffset Epoch = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);
		private readonly long OffsetUpdateThreshold = 50;

		public MixedRealityExtensionApp App;
		private readonly Dictionary<Guid, BaseAnimation> Animations = new Dictionary<Guid, BaseAnimation>(10);
		private readonly Dictionary<Guid, AnimationPatch> PendingPatches = new Dictionary<Guid, AnimationPatch>(10);
		private long ServerTimeOffset = 0;

		public AnimationManager(MixedRealityExtensionApp app)
		{
			App = app;
		}

		public void RegisterAnimation(BaseAnimation anim)
		{
			Animations[anim.Id] = anim;
			if (PendingPatches.TryGetValue(anim.Id, out AnimationPatch patch))
			{
				anim.ApplyPatch(patch);
				PendingPatches.Remove(anim.Id);
			}
		}

		public void DeregisterAnimation(BaseAnimation anim)
		{
			Animations.Remove(anim.Id);
			PendingPatches.Remove(anim.Id);
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

		public static long LocalUnixNow()
		{
			return (DateTimeOffset.UtcNow - Epoch).Ticks / 10_000;
		}

		public long ServerNow()
		{
			return LocalUnixNow() + ServerTimeOffset;
		}

		public void CleanUpOrphanedAnimations(params Guid[] destroyedIds)
		{
			CleanUpOrphanedAnimations(destroyedIds);
		}

		public void CleanUpOrphanedAnimations(IEnumerable<Guid> destroyedIds)
		{
			var badIds = new HashSet<Guid>(destroyedIds);
			foreach (var anim in Animations.Values)
			{
				var mreAnim = anim as Animation;
				// anim targets a destroyed object, and all targets of this anim are/were destroyed
				if (anim.TargetIds.Any(id => badIds.Contains(id)) && anim.TargetIds.All(id => App.FindActor(id) == null))
				{
					DeregisterAnimation(anim);
				}
			}
		}

		[CommandHandler(typeof(CreateAnimation2))]
		private void OnCreateAnimation(CreateAnimation2 message, Action onCompleteCallback)
		{
			// the animation already exists, no-op
			if (Animations.ContainsKey(message.Animation.Id)) {
				onCompleteCallback?.Invoke();
				return;
			}

			// create the anim
			var anim = new Animation(this, message.Animation.Id, message.Animation.DataId, message.Targets);
			anim.TargetIds = message.Animation.TargetIds.ToList();
			anim.ApplyPatch(message.Animation);

			RegisterAnimation(anim);

			Trace trace = new Trace()
			{
				Severity = TraceSeverity.Info,
				Message = $"Successfully created animation named {anim.Name}"
			};

			App.Protocol.Send(
				new ObjectSpawned()
				{
					Result = new OperationResult()
					{
						ResultCode = OperationResultCode.Success,
						Message = trace.Message
					},
					Traces = new List<Trace>() { trace },
					Animations = new AnimationPatch[] { anim.GeneratePatch() }
				},
				message.MessageId
			);

			onCompleteCallback?.Invoke();
		}

		[CommandHandler(typeof(AnimationUpdate))]
		private void OnAnimationUpdate(AnimationUpdate message, Action onCompleteCallback)
		{
			if (Animations.TryGetValue(message.Animation.Id, out BaseAnimation anim))
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
	}
}

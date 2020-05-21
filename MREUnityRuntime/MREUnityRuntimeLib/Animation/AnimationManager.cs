// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.App;
using MixedRealityExtension.Core;
using MixedRealityExtension.Messaging;
using MixedRealityExtension.Messaging.Commands;
using MixedRealityExtension.Messaging.Payloads;
using MixedRealityExtension.Patching;
using MixedRealityExtension.Patching.Types;
using MixedRealityExtension.Util;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MixedRealityExtension.Animation
{
	internal class AnimationManager : ICommandHandlerContext
	{
		internal class AnimBlend
		{
			public readonly TargetPath Path;
			public Newtonsoft.Json.Linq.JToken CurrentValue;
			public float TotalWeight;
			public bool FinalUpdate;

			internal AnimBlend(TargetPath path)
			{
				Path = path;
				CurrentValue = null;
				TotalWeight = 0;
				FinalUpdate = false;
			}
		}

		private static DateTimeOffset Epoch = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);
		private readonly long OffsetUpdateThreshold = 50;

		public MixedRealityExtensionApp App;
		private readonly Dictionary<Guid, BaseAnimation> Animations = new Dictionary<Guid, BaseAnimation>(10);
		private readonly Dictionary<Guid, AnimationPatch> PendingPatches = new Dictionary<Guid, AnimationPatch>(10);
		private long ServerTimeOffset = 0;

		// update loop caching
		private List<BaseAnimation> TempUpdateList = new List<BaseAnimation>(10);
		private List<AnimBlend> TempBlendList = new List<AnimBlend>(10);
		private List<Guid> TempPatchList = new List<Guid>(5);
		public Dictionary<Guid, IPatchable> AnimInputPatches = new Dictionary<Guid, IPatchable>(5);
		public Dictionary<Guid, IPatchable> AnimOutputPatches = new Dictionary<Guid, IPatchable>(5);
		public Dictionary<TargetPath, AnimBlend> AnimBlends = new Dictionary<TargetPath, AnimBlend>(10);
		private HashSet<Guid> SendUpdates = new HashSet<Guid>();

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
			foreach (var id in anim.TargetIds)
			{
				AnimOutputPatches.Remove(id);
			}
			anim.OnDestroy();
		}

		public void Reset()
		{
			Animations.Clear();
			PendingPatches.Clear();
			AnimInputPatches.Clear();
			AnimOutputPatches.Clear();
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
			var serverTime = ServerNow();

			// compute individual animation contributions
			TempUpdateList.Clear();
			TempUpdateList.AddRange(Animations.Values);
			foreach (var anim in TempUpdateList)
			{
				anim.Update(serverTime);
			}

			// roll all anim outputs into patches
			SendUpdates.Clear();
			TempBlendList.Clear();
			TempBlendList.AddRange(AnimBlends.Values);
			foreach (var blend in TempBlendList)
			{
				if (blend.TotalWeight == 0)
				{
					AnimBlends.Remove(blend.Path);
					continue;
				}

				var targetId = Guid.Parse(blend.Path.Placeholder);
				if (blend.Path.AnimatibleType == "actor")
				{
					var actorPatch = (ActorPatch)AnimOutputPatches.GetOrCreate(targetId, () => new ActorPatch(targetId));
					actorPatch.WriteToPath(blend.Path, blend.CurrentValue, 0);
				}

				if (blend.FinalUpdate)
				{
					SendUpdates.Add(targetId);
				}

				// reinitialize blend weight
				blend.TotalWeight = 0;
				blend.FinalUpdate = false;
			}

			// apply patches to all objects involved
			TempPatchList.Clear();
			TempPatchList.AddRange(AnimOutputPatches.Keys);
			foreach (var id in TempPatchList)
			{
				var patch = AnimOutputPatches[id];
				if (patch is ActorPatch actorPatch)
				{
					var actor = (Actor)App.FindActor(id);
					if (actorPatch.IsEmpty())
					{
						AnimOutputPatches.Remove(id);
					}
					else if (actor != null)
					{
						actor.ApplyPatch(actorPatch);
						if (SendUpdates.Contains(id))
						{
							App.Protocol.Send(new ActorUpdate() { Actor = actorPatch });
						}
					}
				}
				patch.Clear();
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

		[CommandHandler(typeof(CreateAnimation2))]
		private void OnCreateAnimation(CreateAnimation2 message, Action onCompleteCallback)
		{
			// the animation already exists, no-op
			if (Animations.ContainsKey(message.Animation.Id)) {
				onCompleteCallback?.Invoke();
				return;
			}

			// create the anim
			if (message.Animation.DataId != null)
			{
				var anim = new Animation(this, message.Animation.Id, message.Animation.DataId.Value, message.Targets);
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
			}
			else
			{
				App.Protocol.Send(
					new ObjectSpawned()
					{
						Result = new OperationResult()
						{
							ResultCode = OperationResultCode.Error,
							Message = $"Failed to create new animation {message.Animation.Id}; no data ID provided."
						},
						Traces = new List<Trace>() { new Trace()
						{
							Severity = TraceSeverity.Error,
							Message = $"Failed to create new animation {message.Animation.Id}; no data ID provided."
						} }
					},
					message.MessageId
				);
			}

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

		[CommandHandler(typeof(DestroyAnimations))]
		private void OnDestroyAnimations(DestroyAnimations message, Action onCompleteCallback)
		{
			foreach (var id in message.AnimationIds)
			{
				if (Animations.TryGetValue(id, out BaseAnimation anim))
				{
					DeregisterAnimation(anim);
				}
			}
			onCompleteCallback?.Invoke();
		}
	}
}

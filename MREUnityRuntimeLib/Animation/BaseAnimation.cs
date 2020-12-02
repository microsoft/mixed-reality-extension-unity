// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;

using MixedRealityExtension.Core;
using MixedRealityExtension.Patching.Types;
using AnimationUpdate = MixedRealityExtension.Messaging.Payloads.AnimationUpdate;

namespace MixedRealityExtension.Animation
{
	internal abstract class BaseAnimation
	{
		protected AnimationManager manager;

		public Guid Id { get; protected set; }
		public virtual string Name { get; protected set; }
		public virtual long BasisTime { get; protected set; }
		public virtual float Time { get; protected set; }
		public virtual float Speed { get; protected set; }
		public virtual float Weight { get; protected set; }
		public virtual MWAnimationWrapMode WrapMode { get; protected set; }

		public virtual bool IsPlaying
		{
			get => Weight > 0;
			protected set
			{
				if (IsPlaying == value)
				{
					return;
				}

				var patch = new AnimationPatch() { Id = Id };
				if (value)
				{
					if (WrapMode == MWAnimationWrapMode.Once)
					{
						Time = 0;
						patch.Time = 0;
					}

					BasisTime = Speed == 0 ? 0 : Math.Max(0, manager.ServerNow() - (long)Math.Floor(Time * 1000 / Speed));
					Weight = 1;
					patch.BasisTime = BasisTime;
					patch.Weight = Weight;
				}
				else
				{
					Weight = 0;
					patch.Weight = 0;
				}

				manager.App.Protocol.Send(new AnimationUpdate() { Animation = patch });
			}
		}

		public virtual List<Guid> TargetIds { get; set; }
		protected List<Actor> TargetActors => TargetIds
			.Select(id => manager.App.FindActor(id) as Actor)
			.Where(a => a != null)
			.ToList();

		internal BaseAnimation(AnimationManager manager, Guid id)
		{
			Id = id;
			this.manager = manager;
		}

		public virtual void ApplyPatch(AnimationPatch patch)
		{
			if (patch.Name != null)
			{
				Name = patch.Name;
			}
			if (patch.WrapMode.HasValue)
			{
				WrapMode = patch.WrapMode.Value;
			}
			if (patch.Speed.HasValue)
			{
				Speed = patch.Speed.Value;
			}
			if (patch.Time.HasValue)
			{
				Time = patch.Time.Value;
			}
			if (patch.IsPlaying.HasValue)
			{
				IsPlaying = patch.IsPlaying.Value;
			}
			if (patch.Weight.HasValue)
			{
				Weight = patch.Weight.Value;
			}
			if (patch.BasisTime.HasValue)
			{
				BasisTime = patch.BasisTime.Value;
			}
		}

		public virtual AnimationPatch GeneratePatch()
		{
			var patch = new AnimationPatch()
			{
				Id = Id,
				Name = Name,
				Speed = Speed,
				Weight = Weight,
				WrapMode = WrapMode,
				TargetIds = TargetIds.ToArray()
			};

			if (IsPlaying)
			{
				patch.BasisTime = BasisTime;
			}
			else
			{
				patch.Time = Time;
			}

			return patch;
		}

		protected void MarkFinished(float currentTime)
		{
			Weight = 0;
			Time = currentTime;
			BasisTime = Speed == 0 ? 0 : Math.Max(0, manager.ServerNow() - (long)Math.Floor(currentTime * 1000 / Speed));

			var update = new AnimationUpdate()
			{
				Animation = new AnimationPatch()
				{
					Id = Id,
					Weight = Weight,
					Time = Time,
					BasisTime = BasisTime
				}
			};
			manager.App.Protocol.Send(update);
		}

		internal virtual void Update(long serverTime)
		{

		}

		internal virtual void OnDestroy()
		{

		}
	}
}

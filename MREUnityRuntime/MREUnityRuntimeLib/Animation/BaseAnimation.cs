// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;

using MixedRealityExtension.Core;
using MixedRealityExtension.Patching.Types;

namespace MixedRealityExtension.Animation
{
	internal class BaseAnimation
	{
		protected AnimationManager manager;

		public Guid Id { get; protected set; }
		public virtual string Name { get; protected set; }
		public virtual long BasisTime { get; protected set; }
		public virtual float Time { get; protected set; }
		public virtual float Speed { get; protected set; }
		public virtual float Weight { get; protected set; }
		public virtual MWAnimationWrapMode WrapMode { get; protected set; }

		public virtual List<Guid> TargetIds { get; set; }
		protected List<Actor> TargetActors => TargetIds
			.Select(id => manager.App.FindActor(id) as Actor)
			.Where(a => a != null)
			.ToList();

		public bool IsPlaying => Weight > 0;

		internal BaseAnimation(AnimationManager manager, Guid id)
		{
			Id = id;
			this.manager = manager;
		}

		public virtual void ApplyPatch(AnimationPatch patch)
		{
			var wasMoving = IsPlaying && Speed != 0;

			if (patch.Name != null)
			{
				Name = patch.Name;
			}
			if (patch.Speed.HasValue)
			{
				Speed = patch.Speed.Value;
			}
			if (patch.Weight.HasValue)
			{
				Weight = patch.Weight.Value;
			}
			if (patch.WrapMode.HasValue)
			{
				WrapMode = patch.WrapMode.Value;
			}
			// only patch one of BasisTime and Time, based on play state
			if (patch.BasisTime.HasValue)
			{
				BasisTime = patch.BasisTime.Value;
			}
			if (patch.Time.HasValue)
			{
				Time = patch.Time.Value;
			}

			var isMoving = IsPlaying && Speed != 0;
			// send one transform update when the anim stops
			if (wasMoving && !isMoving)
			{
				foreach (var actor in TargetActors)
				{
					actor.SendActorUpdate(Messaging.Payloads.ActorComponentType.Transform);
				}
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
				TargetIds = TargetIds
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

		internal virtual void Update()
		{

		}
	}
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;

using MixedRealityExtension.Core;
using MixedRealityExtension.Patching.Types;

namespace MixedRealityExtension.Animation
{
	internal class Animation
	{
		protected AnimationManager manager;

		public Guid Id { get; protected set; }
		public virtual string Name { get; protected set; }
		public virtual long BasisTime { get; protected set; }
		public virtual float Time { get; protected set; }
		public virtual float Speed { get; protected set; }
		public virtual float Weight { get; protected set; }
		public virtual MWAnimationWrapMode WrapMode { get; protected set; }

		internal List<Actor> targetActors = new List<Actor>(1);

		public bool isPlaying => Weight > 0 && Speed != 0;

		internal Animation(AnimationManager manager, Guid id)
		{
			Id = id;
			this.manager = manager;
			manager.RegisterAnimation(this);
		}

		public virtual void ApplyPatch(AnimationPatch patch)
		{
			if (patch.Name != null)
			{
				Name = patch.Name;
			}
			if (patch.BasisTime.HasValue)
			{
				BasisTime = patch.BasisTime.Value;
			}
			if (patch.Time.HasValue)
			{
				Time = patch.Time.Value;
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
				TargetActors = targetActors.Select(actor => actor.Id)
			};

			if (isPlaying)
			{
				patch.BasisTime = BasisTime;
			}
			else
			{
				patch.Time = Time;
			}

			return patch;
		}

		public virtual void Update()
		{

		}
	}
}

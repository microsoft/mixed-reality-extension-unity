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
		public Guid Id { get; protected set; }
		public virtual long BasisTime { get; protected set; }
		public virtual float Time { get; protected set; }
		public virtual float Speed { get; protected set; }
		public virtual float Weight { get; protected set; }
		public virtual MWAnimationWrapMode WrapMode { get; protected set; }

		internal List<Actor> targetActors = new List<Actor>(5);

		public virtual void ApplyPatch(AnimationPatch patch)
		{
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
			return new AnimationPatch()
			{
				Id = Id,
				BasisTime = BasisTime,
				Time = Time,
				Speed = Speed,
				Weight = Weight,
				WrapMode = WrapMode,
				TargetIds = targetActors.Select(actor => actor.Id)
			};
		}

		public virtual void Update()
		{

		}
	}
}

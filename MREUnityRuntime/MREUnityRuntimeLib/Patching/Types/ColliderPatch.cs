// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Animation;
using MixedRealityExtension.Core;
using MixedRealityExtension.Core.Collision;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace MixedRealityExtension.Patching.Types
{
	public class ColliderPatch : Patchable<ColliderPatch>
	{
		[PatchProperty]
		public bool? Enabled { get; set; }

		[PatchProperty]
		public bool? IsTrigger { get; set; }

		[PatchProperty]
		public float? Bounciness { get; set; }

		[PatchProperty]
		public float? StaticFriction { get; set; }

		[PatchProperty]
		public float? DynamicFriction { get; set; }

		[PatchProperty]
		public CollisionLayer? Layer { get; set; }

		[PatchProperty]
		public ColliderGeometry Geometry { get; set; }

		[PatchProperty]
		public ColliderEventType[] EventSubscriptions { get; set; }
	}
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Animation;
using MixedRealityExtension.Core;
using MixedRealityExtension.Core.Collision;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace MixedRealityExtension.Patching.Types
{
	public class ColliderPatch : IPatchable
	{
		[PatchProperty]
		public bool? Enabled { get; set; }

		[PatchProperty]
		public bool? IsTrigger { get; set; }

		[PatchProperty]
		public CollisionLayer? Layer { get; set; }

		[PatchProperty]
		public ColliderGeometry Geometry { get; set; }

		[PatchProperty]
		public IEnumerable<ColliderEventType> EventSubscriptions { get; set; }

		void IPatchable.WriteToPath(TargetPath path, JToken value, int depth = 0)
		{

		}

		public void Clear()
		{

		}
	}
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Animation;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MixedRealityExtension.Patching.Types
{
	public class LookAtPatch : IPatchable
	{
		[PatchProperty]
		public Guid? ActorId { get; set; }

		[PatchProperty]
		public LookAtMode? Mode { get; set; }

		[PatchProperty]
		public bool? Backward { get; set; }

		void IPatchable.WriteToPath(TargetPath path, JObject value, int depth = 0)
		{

		}

		public void Clear()
		{

		}
	}
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using MixedRealityExtension.Animation;
using MixedRealityExtension.Messaging.Payloads.Converters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MixedRealityExtension.Patching.Types
{
	public class AppearancePatch : IPatchable
	{
		[PatchProperty]
		[JsonConverter(typeof(UnsignedConverter))]
		public uint? Enabled { get; set; }

		[PatchProperty]
		public Guid? MaterialId { get; set; }

		[PatchProperty]
		public Guid? MeshId { get; set; }

		public void WriteToPath(TargetPath path, JToken value, int depth)
		{

		}

		public bool ReadFromPath(TargetPath path, ref JToken value, int depth)
		{
			return false;
		}

		public void Clear()
		{

		}

		public void Restore(TargetPath path, int depth)
		{

		}

		public void RestoreAll()
		{

		}
	}
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Animation;
using Newtonsoft.Json.Linq;

namespace MixedRealityExtension.Patching
{
	internal interface IPatchable
	{
		void WriteToPath(TargetPath path, JToken value, int depth = 0);
		void Clear();
	}

	internal static class IPatchableExtensions
	{
		internal static void WriteToPath(this IPatchable patch, TargetPath path, JToken value, int depth)
		{
			patch.WriteToPath(path, value, depth);
		}
	}
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Animation;
using MixedRealityExtension.Core.Interfaces;
using MixedRealityExtension.Util;
using Newtonsoft.Json.Linq;

namespace MixedRealityExtension.Patching.Types
{
	public class LightPatch : Patchable<LightPatch>
	{
		[PatchProperty]
		public bool? Enabled { get; set; }

		[PatchProperty]
		public LightType? Type { get; set; }

		[PatchProperty]
		public ColorPatch Color { get; set; }

		[PatchProperty]
		public float? Range { get; set; }

		[PatchProperty]
		public float? Intensity { get; set; }

		[PatchProperty]
		public float? SpotAngle { get; set; }

		public LightPatch()
		{ }

		internal LightPatch(UnityEngine.Light light)
		{
			Enabled = light.enabled;
			Type = UtilMethods.ConvertEnum<LightType, UnityEngine.LightType>(light.type);
			Color = new ColorPatch(light.color);
			Range = light.range;
			Intensity = light.intensity;
			SpotAngle = light.spotAngle;
		}
	}
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Core.Interfaces;
using MixedRealityExtension.Util;

namespace MixedRealityExtension.Patching.Types
{
    public class LightPatch : IPatchable
    {
        [PatchProperty]
        public PatchProperty<bool> Enabled { get; set; }

        [PatchProperty]
        public PatchProperty<LightType> Type { get; set; }

        [PatchProperty]
        public PatchProperty<ColorPatch> Color { get; set; }

        [PatchProperty]
        public PatchProperty<float> Range { get; set; }

        [PatchProperty]
        public PatchProperty<float> Intensity { get; set; }

        [PatchProperty]
        public PatchProperty<float> SpotAngle { get; set; }

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

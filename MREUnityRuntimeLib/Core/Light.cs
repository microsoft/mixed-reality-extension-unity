// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;

using MixedRealityExtension.Core.Interfaces;
using MixedRealityExtension.Core.Types;
using MixedRealityExtension.Patching;
using MixedRealityExtension.Patching.Types;
using MixedRealityExtension.Util.Unity;
using UnityEngine;
using MRELightType = MixedRealityExtension.Core.Interfaces.LightType;
using UnityLight = UnityEngine.Light;

namespace MixedRealityExtension.Core
{
	internal class Light : ILight
	{
		private readonly UnityLight _light;

		// Cached values
		private readonly MWColor _color = new MWColor();

		/// <inheritdoc />
		public bool Enabled => _light.enabled;

		/// <inheritdoc />
		public MRELightType Type => (MRELightType)Enum.Parse(typeof(MRELightType), _light.type.ToString());

		/// <inheritdoc />
		public MWColor Color => _color.FromUnityColor(_light.color);

		/// <inheritdoc />
		public float Range => _light.range;

		/// <inheritdoc />
		public float Intensity => _light.intensity;

		/// <inheritdoc />
		public float SpotAngle => _light.spotAngle * Mathf.Deg2Rad;

		/// <summary>
		/// Initializes a new instance of the <see cref="Light"/> class.
		/// </summary>
		/// <param name="light">The <see cref="Light"/> object to bind to.</param>
		public Light(UnityLight light)
		{
			_light = light;
		}

		/// <inheritdoc />
		public void ApplyPatch(LightPatch patch)
		{
			_light.enabled = _light.enabled.GetPatchApplied(Enabled.ApplyPatch(patch.Enabled));
			_light.type = _light.type.GetPatchApplied(Type.ApplyPatch(patch.Type));
			_light.color = _light.color.GetPatchApplied(Color.ApplyPatch(patch.Color));
			_light.range = _light.range.GetPatchApplied(Range.ApplyPatch(patch.Range));
			_light.intensity = _light.intensity.GetPatchApplied(Intensity.ApplyPatch(patch.Intensity));
			if (patch.SpotAngle.HasValue)
			{
				_light.spotAngle = Mathf.Rad2Deg * patch.SpotAngle.Value;
			}
		}

		/// <inheritdoc />
		public void SynchronizeEngine(LightPatch patch)
		{
			ApplyPatch(patch);
		}
	}
}

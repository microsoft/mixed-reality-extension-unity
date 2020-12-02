// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Animation;
using MixedRealityExtension.Core.Types;
using Newtonsoft.Json.Linq;
using System;

namespace MixedRealityExtension.Patching.Types
{
	public class ColorPatch : Patchable<ColorPatch>, IEquatable<ColorPatch>
	{
		[PatchProperty]
		public float? R { get; set; }

		[PatchProperty]
		public float? G { get; set; }

		[PatchProperty]
		public float? B { get; set; }

		[PatchProperty]
		public float? A { get; set; }

		public ColorPatch()
		{ }

		internal ColorPatch(MWColor color)
		{
			R = color.R;
			G = color.G;
			B = color.B;
			A = color.A;
		}

		internal ColorPatch(UnityEngine.Color color)
		{
			R = color.r;
			G = color.g;
			B = color.b;
			A = color.a;
		}

		public bool Equals(ColorPatch other)
		{
			if (other == null)
			{
				return false;
			}
			else
			{
				return
					R.Equals(other.R) &&
					G.Equals(other.G) &&
					B.Equals(other.B) &&
					A.Equals(other.A);
			}
		}
	}
}

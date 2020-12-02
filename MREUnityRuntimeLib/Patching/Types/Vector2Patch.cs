// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Animation;
using MixedRealityExtension.Core.Types;
using Newtonsoft.Json.Linq;
using System;
using UnityEngine;

namespace MixedRealityExtension.Patching.Types
{
	public class Vector2Patch : Patchable<Vector2Patch>, IEquatable<Vector2Patch>
	{
		[PatchProperty]
		public float? X { get; set; }

		[PatchProperty]
		public float? Y { get; set; }

		public Vector2Patch()
		{

		}

		public Vector2Patch(MWVector2 vector)
		{
			X = vector.X;
			Y = vector.Y;
		}

		public Vector2Patch(Vector2 vector2)
		{
			X = vector2.x;
			Y = vector2.y;
		}

		public Vector2Patch(Vector2Patch other)
		{
			if (other != null)
			{
				X = other.X;
				Y = other.Y;
			}
		}

		public bool Equals(Vector2Patch other)
		{
			if (other == null)
			{
				return false;
			}
			else
			{
				return
					X.Equals(other.X) &&
					Y.Equals(other.Y);
			}
		}
	}
}

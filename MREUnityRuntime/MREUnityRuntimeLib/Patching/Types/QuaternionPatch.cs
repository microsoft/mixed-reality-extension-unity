// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Animation;
using MixedRealityExtension.Core.Types;
using Newtonsoft.Json.Linq;
using System;
using UnityEngine;

namespace MixedRealityExtension.Patching.Types
{
	public class QuaternionPatch : IEquatable<QuaternionPatch>, IPatchable
	{
		[PatchProperty]
		public float? X { get; set; }

		[PatchProperty]
		public float? Y { get; set; }

		[PatchProperty]
		public float? Z { get; set; }
		
		[PatchProperty]
		public float? W { get; set; }

		public QuaternionPatch()
		{

		}

		internal QuaternionPatch(MWQuaternion quaternion)
		{
			X = quaternion.X;
			Y = quaternion.Y;
			Z = quaternion.Z;
			W = quaternion.W;
		}

		internal QuaternionPatch(Quaternion quaternion)
		{
			X = quaternion.x;
			Y = quaternion.y;
			Z = quaternion.z;
			W = quaternion.w;
		}

		internal QuaternionPatch(QuaternionPatch other)
		{
			if (other != null)
			{
				X = other.X;
				Y = other.Y;
				Z = other.Z;
				W = other.W;
			}
		}

		//public QuaternionPatch ToQuaternion()
		//{
		//    return new Quaternion()
		//    {
		//        w = (W != null) ? (float)W : 0.0f,
		//        x = (X != null) ? (float)X : 0.0f,
		//        y = (Y != null) ? (float)Y : 0.0f,
		//        z = (Z != null) ? (float)Z : 0.0f,
		//    };
		//}

		public bool Equals(QuaternionPatch other)
		{
			if (other == null)
			{
				return false;
			}
			else
			{
				return
					X.Equals(other.X) &&
					Y.Equals(other.Y) &&
					Z.Equals(other.Z) &&
					W.Equals(other.W);
			}
		}

		public void WriteToPath(TargetPath path, JToken value, int depth)
		{
			if (depth == path.PathParts.Length)
			{
				X = value.Value<float>("x");
				Y = value.Value<float>("y");
				Z = value.Value<float>("z");
				W = value.Value<float>("w");
			}
			// else
				// an unrecognized path, do nothing
		}

		public void Clear()
		{
			X = null;
			Y = null;
			Z = null;
			W = null;
		}

		public void Restore(TargetPath path, int depth)
		{

		}
	}
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Animation;
using MixedRealityExtension.Core.Types;
using Newtonsoft.Json.Linq;
using System;

namespace MixedRealityExtension.Patching.Types
{
	public class TransformPatch : Patchable<TransformPatch>
	{
		private Vector3Patch position;
		private Vector3Patch savedPosition;
		[PatchProperty]
		public Vector3Patch Position
		{
			get => position;
			set
			{
				if (value == null && position != null)
				{
					savedPosition = position;
					savedPosition.Clear();
				}
				position = value;
			}
		}

		private QuaternionPatch rotation;
		private QuaternionPatch savedRotation;
		[PatchProperty]
		public QuaternionPatch Rotation
		{
			get => rotation;
			set
			{
				if (value == null && rotation != null)
				{
					savedRotation = rotation;
					savedRotation.Clear();
				}
				rotation = value;
			}
		}

		public TransformPatch()
		{

		}

		internal TransformPatch(MWVector3 position, MWQuaternion rotation)
		{
			Position = new Vector3Patch(position);
			Rotation = new QuaternionPatch(rotation);
		}

		/// tests if 2 transforms equal up to eps if all values are defined and non-null
		static public bool areTransformsEqual(TransformPatch a, TransformPatch b, float eps)
		{
			float largeEps = 1000.0F * eps;
			bool ret = (
			  ((a.Position.X.HasValue && b.Position.X.HasValue) ? Math.Abs(a.Position.X.Value - b.Position.X.Value) : largeEps) +
			  ((a.Position.Y.HasValue && b.Position.Y.HasValue) ? Math.Abs(a.Position.Y.Value - b.Position.Y.Value) : largeEps) +
			  ((a.Position.Z.HasValue && b.Position.Z.HasValue) ? Math.Abs(a.Position.Z.Value - b.Position.Z.Value) : largeEps)
			     < eps);
			ret = ret && ((
			   ((a.Rotation.X.HasValue && b.Rotation.X.HasValue) ? Math.Abs(a.Rotation.X.Value - b.Rotation.X.Value) : largeEps) +
			   ((a.Rotation.Y.HasValue && b.Rotation.Y.HasValue) ? Math.Abs(a.Rotation.Y.Value - b.Rotation.Y.Value) : largeEps) +
			   ((a.Rotation.Z.HasValue && b.Rotation.Z.HasValue) ? Math.Abs(a.Rotation.Z.Value - b.Rotation.Z.Value) : largeEps) +
			   ((a.Rotation.W.HasValue && b.Rotation.W.HasValue) ? Math.Abs(a.Rotation.W.Value - b.Rotation.W.Value) : largeEps)
				  ) < 10.0F * eps);
			return ret;
		}

		public override void WriteToPath(TargetPath path, JToken value, int depth)
		{
			if (depth == path.PathParts.Length)
			{
				// transforms are not directly patchable, do nothing
			}
			else if (path.PathParts[depth] == "position")
			{
				if (Position == null)
				{
					if (savedPosition == null)
					{
						savedPosition = new Vector3Patch();
					}
					position = savedPosition;
				}
				Position.WriteToPath(path, value, depth + 1);
			}
			else if (path.PathParts[depth] == "rotation")
			{
				if (Rotation == null)
				{
					if (savedRotation == null)
					{
						savedRotation = new QuaternionPatch();
					}
					rotation = savedRotation;
				}
				Rotation.WriteToPath(path, value, depth + 1);
			}
			// else
				// an unrecognized path, do nothing
		}

		public override bool ReadFromPath(TargetPath path, ref JToken value, int depth)
		{
			if (path.PathParts[depth] == "position")
			{
				return Position?.ReadFromPath(path, ref value, depth + 1) ?? false;
			}
			else if (path.PathParts[depth] == "rotation")
			{
				return Rotation?.ReadFromPath(path, ref value, depth + 1) ?? false;
			}
			return false;
		}

		public override void Clear()
		{
			Position = null;
			Rotation = null;
		}

		public override void Restore(TargetPath path, int depth)
		{
			if (depth >= path.PathParts.Length) return;

			switch (path.PathParts[depth])
			{
				case "position":
					Position = savedPosition ?? new Vector3Patch();
					Position.Restore(path, depth + 1);
					break;
				case "rotation":
					Rotation = savedRotation ?? new QuaternionPatch();
					Rotation.Restore(path, depth + 1);
					break;
			}
		}

		public override void RestoreAll()
		{
			Position = savedPosition ?? new Vector3Patch();
			Position.RestoreAll();
			Rotation = savedRotation ?? new QuaternionPatch();
			Rotation.RestoreAll();
		}
	}
}

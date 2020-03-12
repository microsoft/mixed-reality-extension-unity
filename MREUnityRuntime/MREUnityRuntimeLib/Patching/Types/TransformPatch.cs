// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Animation;
using MixedRealityExtension.Core.Types;
using Newtonsoft.Json.Linq;

namespace MixedRealityExtension.Patching.Types
{
	public class TransformPatch : IPatchable
	{
		[PatchProperty]
		public Vector3Patch Position { get; set; }

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
			this.Position = new Vector3Patch(position);
			this.Rotation = new QuaternionPatch(rotation);
		}

		void IPatchable.WriteToPath(TargetPath path, JToken value, int depth)
		{
			if (depth == path.PathParts.Length)
			{
				// transforms are not directly patchable, do nothing
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

		public void Clear()
		{
			Rotation = null;
		}
	}

	public class ScaledTransformPatch : TransformPatch
	{
		[PatchProperty]
		public Vector3Patch Scale { get; set; }

		public ScaledTransformPatch()
			: base()
		{

		}

		internal ScaledTransformPatch(MWVector3 position, MWQuaternion rotation, MWVector3 scale)
			: base(position, rotation)
		{
			this.Scale = new Vector3Patch(scale);
		}
	}
}

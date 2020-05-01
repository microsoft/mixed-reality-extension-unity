// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Animation;
using MixedRealityExtension.Core.Types;
using Newtonsoft.Json.Linq;

namespace MixedRealityExtension.Patching.Types
{
	public class TransformPatch : Patchable<TransformPatch>, IPatchable
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

		public virtual void WriteToPath(TargetPath path, JToken value, int depth)
		{
			if (depth == path.PathParts.Length)
			{
				// transforms are not directly patchable, do nothing
			}
			else if (_WriteToPath(path, value, depth))
			{
				// handled
			}
			// else
			// an unrecognized path, do nothing
		}

		internal bool _WriteToPath(TargetPath path, JToken value, int depth)
		{
			if (path.PathParts[depth] == "position")
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
				return true;
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
				return true;
			}
			return false;
		}

		public virtual bool ReadFromPath(TargetPath path, ref JToken value, int depth)
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

		public virtual void Clear()
		{
			Position = null;
			Rotation = null;
		}

		public virtual void Restore(TargetPath path, int depth)
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

		public virtual void RestoreAll()
		{
			Position = savedPosition ?? new Vector3Patch();
			Position.RestoreAll();
			Rotation = savedRotation ?? new QuaternionPatch();
			Rotation.RestoreAll();
		}
	}

	/// <note>
	/// Currently duplicates TransformPatch instead of inheriting so we can directly inherit from
	/// Patchable to get those wins. Sadly C# does not support multiple inheritance or templated
	/// mixins.
	/// </note>
	public class ScaledTransformPatch : Patchable<ScaledTransformPatch>, IPatchable
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

		public ScaledTransformPatch()
		{

		}

		internal ScaledTransformPatch(MWVector3 position, MWQuaternion rotation, MWVector3 scale)
		{
			Scale = new Vector3Patch(scale);
			Position = new Vector3Patch(position);
			Rotation = new QuaternionPatch(rotation);
		}

		public virtual void WriteToPath(TargetPath path, JToken value, int depth)
		{
			if (depth == path.PathParts.Length)
			{
				// transforms are not directly patchable, do nothing
			}
			else if (_WriteToPath(path, value, depth))
			{
				// handled
			}
			// else
			// an unrecognized path, do nothing
		}

		internal bool _WriteToPath(TargetPath path, JToken value, int depth)
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
				return true;
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
				return true;
			}
			else if (path.PathParts[depth] == "scale")
			{
				if (Scale == null)
				{
					if (savedScale == null)
					{
						savedScale = new Vector3Patch();
					}
					scale = savedScale;
				}
				scale.WriteToPath(path, value, depth + 1);
			}
			return false;
		}

		public virtual bool ReadFromPath(TargetPath path, ref JToken value, int depth)
		{
			if (path.PathParts[depth] == "position")
			{
				return Position?.ReadFromPath(path, ref value, depth + 1) ?? false;
			}
			else if (path.PathParts[depth] == "rotation")
			{
				return Rotation?.ReadFromPath(path, ref value, depth + 1) ?? false;
			}
			else if (path.PathParts[depth] == "scale")
			{
				return Scale?.ReadFromPath(path, ref value, depth + 1) ?? false;
			}
			return false;
		}

		public virtual void Clear()
		{
			Position = null;
			Rotation = null;
			Scale = null;
		}

		public virtual void Restore(TargetPath path, int depth)
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
				case "scale":
					Scale = savedScale ?? new Vector3Patch();
					Scale.Restore(path, depth + 1);
					break;
			}
		}

		public virtual void RestoreAll()
		{
			Position = savedPosition ?? new Vector3Patch();
			Position.RestoreAll();
			Rotation = savedRotation ?? new QuaternionPatch();
			Rotation.RestoreAll();
			Scale = savedScale ?? new Vector3Patch();
			Scale.RestoreAll();
		}

		private Vector3Patch scale;
		private Vector3Patch savedScale;
		[PatchProperty]
		public Vector3Patch Scale
		{
			get => scale;
			set
			{
				if (value == null && scale != null)
				{
					savedScale = scale;
					savedScale.Clear();
				}
				scale = value;
			}
		}

	}
}

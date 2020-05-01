// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Animation;
using Newtonsoft.Json.Linq;
using System;

namespace MixedRealityExtension.Patching.Types
{
	/// <summary>
	/// Attachment patch.
	/// </summary>
	public class AttachmentPatch : Patchable<AttachmentPatch>, IEquatable<AttachmentPatch>, IPatchable
	{
		public string AttachPoint { get; set; }

		public Guid? UserId { get; set; }

		public bool Equals(AttachmentPatch other)
		{
			return other != null && AttachPoint == other.AttachPoint && UserId == other.UserId;
		}

		public override bool Equals(object obj)
		{
			return Equals(obj as AttachmentPatch);
		}

		// This class is not suitable for use as a hash key or dictionary key.
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		public bool IsPatched()
		{
			return AttachPoint != null || UserId != null;
		}

		public void WriteToPath(TargetPath path, JToken value, int depth)
		{

		}

		public bool ReadFromPath(TargetPath path, ref JToken value, int depth)
		{
			return false;
		}

		public void Clear()
		{

		}

		public void Restore(TargetPath path, int depth)
		{

		}

		public void RestoreAll()
		{

		}
	}
}

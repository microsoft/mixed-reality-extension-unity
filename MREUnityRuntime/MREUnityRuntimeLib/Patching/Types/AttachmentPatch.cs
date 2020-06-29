// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Animation;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MixedRealityExtension.Patching.Types
{
	/// <summary>
	/// Attachment patch.
	/// </summary>
	public class AttachmentPatch : Patchable<AttachmentPatch>, IEquatable<AttachmentPatch>
	{
		[PatchProperty]
		public string AttachPoint { get; set; }

		[PatchProperty]
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
	}
}

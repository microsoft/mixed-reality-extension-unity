// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Patching.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MixedRealityExtension.Core
{
	public class Attachment : IEquatable<Attachment>
	{
		internal string AttachPoint { get; set; } = "none";
		internal Guid UserId { get; set; } = Guid.Empty;

		public bool Equals(Attachment other)
		{
			return other != null && AttachPoint == other.AttachPoint && UserId == other.UserId;
		}

		public override bool Equals(object obj)
		{
			return Equals(obj as Attachment);
		}

		// This class is not suitable for use as a hash key or dictionary key.
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		internal void ApplyPatch(AttachmentPatch patch)
		{
			if (patch != null)
			{
				if (patch.AttachPoint != null)
				{
					AttachPoint = patch.AttachPoint;
				}
				if (patch.UserId.HasValue)
				{
					UserId = patch.UserId.Value;
				}
			}
		}

		internal AttachmentPatch GeneratePatch(Attachment other)
		{
			if (!this.Equals(other))
			{
				return new AttachmentPatch()
				{
					AttachPoint = AttachPoint,
					UserId = UserId
				};
			}
			return null;
		}

		internal void CopyFrom(Attachment other)
		{
			AttachPoint = other.AttachPoint;
			UserId = other.UserId;
		}

		internal void Clear()
		{
			AttachPoint = "none";
			UserId = Guid.Empty;
		}
	}
}

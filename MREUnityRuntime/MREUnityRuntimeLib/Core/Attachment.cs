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
        internal string AttachPoint { get; set; }
        internal Guid UserId { get; set; }

        public bool Equals(Attachment other)
        {
            return AttachPoint == other.AttachPoint && UserId == other.UserId;
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
            if (this != other)
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

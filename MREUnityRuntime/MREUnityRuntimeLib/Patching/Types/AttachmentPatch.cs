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
    public class AttachmentPatch : IEquatable<AttachmentPatch>, IPatchable
    {
        public string AttachPoint { get; set; }

        public Guid? UserId { get; set; }

        public bool Equals(AttachmentPatch other)
        {
            if (other == null)
            {
                return false;
            }

            return AttachPoint == other.AttachPoint && UserId == other.UserId;
        }

        public bool IsPatched()
        {
            return AttachPoint != null || UserId != null;
        }
    }
}

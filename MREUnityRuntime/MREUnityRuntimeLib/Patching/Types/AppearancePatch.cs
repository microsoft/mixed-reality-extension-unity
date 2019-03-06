using System;

namespace MixedRealityExtension.Patching.Types
{
    public class AppearancePatch
    {
        [PatchProperty]
        public bool? Enabled { get; set; }

        [PatchProperty]
        public Guid? MaterialId { get; set; }

        public AppearancePatch() { }
    }
}

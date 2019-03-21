using System;
using MixedRealityExtension.Messaging.Payloads.Converters;
using Newtonsoft.Json;

namespace MixedRealityExtension.Patching.Types
{
    public class AppearancePatch
    {
        [PatchProperty]
        public UInt32? Enabled { get; set; }

        [PatchProperty]
        public Guid? MaterialId { get; set; }
    }
}

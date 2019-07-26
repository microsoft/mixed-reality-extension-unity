using System;
using MixedRealityExtension.Messaging.Payloads.Converters;
using Newtonsoft.Json;

namespace MixedRealityExtension.Patching.Types
{
    public class AppearancePatch
    {
        [PatchProperty]
        [JsonConverter(typeof(UnsignedConverter))]
        public UInt32? Enabled { get; set; }

        [PatchProperty]
        public Guid? MaterialId { get; set; }

        [PatchProperty]
        public Guid? MeshId { get; set; }
    }
}

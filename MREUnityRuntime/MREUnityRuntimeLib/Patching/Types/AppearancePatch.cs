using System;
using MixedRealityExtension.Messaging.Payloads.Converters;
using Newtonsoft.Json;

namespace MixedRealityExtension.Patching.Types
{
    public class AppearancePatch
    {
        [PatchProperty]
        [JsonConverter(typeof(UnsignedConverter))]
        public UInt32? EnabledPacked { get; set; }

        [PatchProperty]
        public Guid? MaterialId { get; set; }
    }
}

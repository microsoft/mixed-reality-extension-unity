using System;
using MixedRealityExtension.Messaging.Payloads.Converters;
using Newtonsoft.Json;

namespace MixedRealityExtension.Patching.Types
{
    public class AppearancePatch
    {
        // This field has the same data as EnabledPacked, but it's sent on actor-create
        // instead of actor-update. This is a consequence of our API initialization struct
        // (ActorLike) being the network transmission struct as well. Couldn't make a clean
        // API without making the transmission layer a little uglier. Sorry :(
        // - Steven, 2019.03.22
        [JsonConverter(typeof(UnsignedConverter))]
        public UInt32? Enabled { get; set; }

        [PatchProperty]
        [JsonConverter(typeof(UnsignedConverter))]
        public UInt32? EnabledPacked { get; set; }

        [PatchProperty]
        public Guid? MaterialId { get; set; }
    }
}

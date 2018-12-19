// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Messaging.Payloads;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MixedRealityExtension
{
    internal static class Constants
    {
        internal static readonly JsonSerializerSettings SerializerSettings;

        static Constants()
        {
            SerializerSettings = new JsonSerializerSettings
            {
                ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver(),
                Formatting = Formatting.None,
                NullValueHandling = NullValueHandling.Ignore,
            };

            SerializerSettings.Converters.Add(new StringEnumConverter() { CamelCaseText = true });
            SerializerSettings.Converters.Add(new PayloadConverter());
        }

        internal const string SessionHeader = "x-ms-mixed-reality-extension-sessionid";
        internal const string PlatformHeader = "x-ms-mixed-reality-extension-platformid";
        internal const string ProtocolVersionHeader = "x-ms-mixed-reality-extension-protocol-version";

        /*
         * PROTOCOL VERSION
         * This is the version of the wire protocol this host library speaks. This value is sent to the MRE app when
         * connecting. If client's protocol version is unsupported, the connection will be rejected.
         */
        internal const int ProtocolVersion = 1;
    }
}

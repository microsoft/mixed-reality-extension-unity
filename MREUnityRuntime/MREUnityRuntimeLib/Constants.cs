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

        /*
         * Headers passed in MRE connect requests.
         */
        internal const string SessionHeader = "x-ms-mixed-reality-extension-sessionid";
        internal const string PlatformHeader = "x-ms-mixed-reality-extension-platformid";
        internal const string CurrentClientVersionHeader = "x-ms-mixed-reality-extension-client-version";
        internal const string MinimumSupportedSDKVersionHeader = "x-ms-mixed-reality-extension-min-sdk-version";

        /*
         * Current Client Version
         * This matches major.minor from the package version number, and is updated as part of the manual
         * SDK release procedures.
         */
        internal const string CurrentClientVersion = "0.5";

        /*
         * Minimum Supported SDK version
         * The oldest SDK version that runs. Since compatibility with older MREs is essential, changing
         * this *is* a big deal, and requires discussion and signoff from the dev team.
         */
        internal const string MinimumSupportedSDKVersion = "0.5";
    }
}

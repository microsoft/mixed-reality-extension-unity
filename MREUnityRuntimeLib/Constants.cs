// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Messaging.Payloads;
using MixedRealityExtension.Messaging.Payloads.Converters;
using Newtonsoft.Json;

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
				FloatParseHandling = FloatParseHandling.Double,
				Formatting = Formatting.None,
				NullValueHandling = NullValueHandling.Ignore,
			};

			SerializerSettings.Converters.Add(new DashFormattedEnumConverter());
			SerializerSettings.Converters.Add(new PayloadConverter());
			SerializerSettings.Converters.Add(new CollisionGeometryConverter());
		}

		/*
		 * Headers passed in MRE connect requests.
		 */
		internal const string SessionHeader = "x-ms-mixed-reality-extension-sessionid";
		internal const string PlatformHeader = "x-ms-mixed-reality-extension-platformid";
		internal const string LegacyProtocolVersionHeader = "x-ms-mixed-reality-extension-protocol-version";
		internal const string CurrentClientVersionHeader = "x-ms-mixed-reality-extension-client-version";
		internal const string MinimumSupportedSDKVersionHeader = "x-ms-mixed-reality-extension-min-sdk-version";

		/*
		 * Legacy Protocol Version - Left over from the old protocol versioning scheme.
		 * Keeping in for now to allow a smoother transition to the new system.
		 * TODO: Remove after a few releases.
		 */
		internal const int LegacyProtocolVersion = 1;

		/*
		 * Current Client Version
		 * This matches major.minor from the package version number, and is updated as part of the manual
		 * SDK release procedures.
		 */
		internal const string CurrentClientVersion = "0.20";

		/*
		 * Minimum Supported SDK version
		 * The oldest SDK version that runs. Since compatibility with older MREs is essential, changing
		 * this *is* a big deal, and requires discussion and signoff from the dev team.
		 */
		internal const string MinimumSupportedSDKVersion = "0.13";

		/*
		 * Enable physics bridge
		 * Work in progress feature to improve shared physics synchronization.
		 * It is NOT compatibile with SDK versions older than 0.19.
		 */
		internal const bool UsePhysicsBridge = true;

		/// <summary>
		/// If we load an asset, and the server response does not contain an ETag, this is the asset's version
		/// </summary>
		internal const string UnversionedAssetVersion = "unversioned";
	}
}

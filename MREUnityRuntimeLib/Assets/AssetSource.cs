// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;

namespace MixedRealityExtension.Assets
{
	/// <summary>
	/// Documents the origin of an asset.
	/// NOTE: The InternalId is not used for equality testing.
	/// </summary>
	public class AssetSource
	{
		/// <summary>
		/// The type of container the asset came from.
		/// </summary>
		public AssetContainerType ContainerType { get; set; }

		/// <summary>
		/// The URL of the asset's container.
		/// </summary>
		public string Uri { get; set; }

		/// <summary>
		/// The version of the resource at the Uri that this asset came from. Will typically be an HTTP ETag.
		/// </summary>
		[Newtonsoft.Json.JsonIgnore]
		public string Version { get; set; }

		private Uri parsedUri;
		/// <summary>
		/// The parsed URI of the asset's container.
		/// </summary>
		[Newtonsoft.Json.JsonIgnore]
		public Uri ParsedUri
		{
			get => parsedUri = parsedUri ?? new Uri(Uri, UriKind.RelativeOrAbsolute);
			set {
				parsedUri = value;
				Uri = parsedUri.ToString();
			}
		}

		/// <summary>
		/// The location of the asset within the container. Type-dependent.
		/// </summary>
		public string InternalId { get; set; }

		public AssetSource() { }

		public AssetSource(
			AssetContainerType containerType = AssetContainerType.GLTF,
			string uri = null,
			string internalId = null,
			string version = null)
		{
			ContainerType = containerType;
			Uri = uri;
			InternalId = internalId;
			Version = version;
		}

		public override bool Equals(object other)
		{
			return other != null &&
				other is AssetSource otherSource &&
				this.ContainerType == otherSource.ContainerType &&
				this.Uri == otherSource.Uri;
		}

		public override int GetHashCode()
		{
			return 313
				^ 317 * ContainerType.GetHashCode()
				^ 317 * Uri.GetHashCode();
		}
	}
}

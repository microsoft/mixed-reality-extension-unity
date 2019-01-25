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

        private Uri parsedUri;
        /// <summary>
        /// The parsed URI of the asset's container.
        /// </summary>
        public Uri ParsedUri => parsedUri = parsedUri ?? new Uri(Uri);

        /// <summary>
        /// The location of the asset within the container. Type-dependent.
        /// </summary>
        public string InternalId { get; set; }

        public AssetSource() { }

        public AssetSource(AssetContainerType containerType = AssetContainerType.GLTF, string uri = null, string internalId = null)
        {
            ContainerType = containerType;
            Uri = uri;
            InternalId = internalId;
        }

        public override bool Equals(object other)
        {
            return other is AssetSource otherSource &&
                this.ContainerType == otherSource.ContainerType &&
                this.Uri.Equals(otherSource.Uri);
        }

        public override int GetHashCode()
        {
            return 313
                ^ 317 * ContainerType.GetHashCode()
                ^ 317 * Uri.GetHashCode();
        }

        public static bool operator ==(AssetSource a, AssetSource b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(AssetSource a, AssetSource b)
        {
            return !a.Equals(b);
        }
    }
}

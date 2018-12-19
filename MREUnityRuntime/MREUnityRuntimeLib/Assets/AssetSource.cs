// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;

namespace MixedRealityExtension.Assets
{
    /// <summary>
    /// Documents the origin of an asset
    /// </summary>
    public struct AssetSource
    {
        /// <summary>
        /// The type of container the asset came from.
        /// </summary>
        public AssetContainerType ContainerType { get; }

        /// <summary>
        /// The URL of the asset's container.
        /// </summary>
        public Uri Uri { get; }

        /// <summary>
        /// The location of the asset within the container. Type-dependent.
        /// </summary>
        public string InternalId { get; }

        public AssetSource(AssetContainerType containerType, Uri uri, string internalId)
        {
            ContainerType = containerType;
            Uri = uri;
            InternalId = internalId;
        }

        public override bool Equals(object other)
        {
            return other is AssetSource otherSource && this == otherSource;
        }

        public override int GetHashCode()
        {
            int hash = 313;
            hash ^= 317 * ContainerType.GetHashCode();
            hash ^= 317 * Uri.GetHashCode();
            hash ^= 317 * (InternalId != null ? InternalId.GetHashCode() : 0);
            return hash;
        }

        public static bool operator ==(AssetSource a, AssetSource b)
        {
            return a.ContainerType == b.ContainerType && a.Uri == b.Uri && a.InternalId == b.InternalId;
        }

        public static bool operator !=(AssetSource a, AssetSource b)
        {
            return !(a == b);
        }
    }
}

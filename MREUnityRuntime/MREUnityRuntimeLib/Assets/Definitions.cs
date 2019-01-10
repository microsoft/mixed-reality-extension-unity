// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using Newtonsoft.Json;

namespace MixedRealityExtension.Assets
{
    /// <summary>
    /// An asset definition for Node use
    /// </summary>
    public struct Asset
    {
        /// <summary>
        /// The unique ID of this asset.
        /// </summary>
        public Guid Id;

        /// <summary>
        /// A human-friendly identifier for this asset. Not guaranteed to be unique.
        /// </summary>
        public string Name;

        /// <summary>
        /// Documents the origin of this asset
        /// </summary>
        [JsonIgnore]
        public AssetSource Source;

        /// <summary>
        /// If this asset is a prefab, contains those properties
        /// </summary>
        public Prefab Prefab;
    }

    /// <summary>
    /// Types of asset containers
    /// </summary>
    public enum AssetContainerType
    {
        /// <summary>
        /// Loaded from a glTF file.
        /// </summary>
        GLTF,

        /// <summary>
        /// Loaded from a host library.
        /// </summary>
        Library
    }

    /// <summary>
    /// Contains prefab asset information.
    /// </summary>
    public struct Prefab
    {
        /// <summary>
        /// The number of actors described in this prefab.
        /// </summary>
        public int ActorCount;
    }
}

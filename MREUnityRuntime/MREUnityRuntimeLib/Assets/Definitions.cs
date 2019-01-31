// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using MixedRealityExtension.Core.Types;
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
        public Prefab? Prefab;

        /// <summary>
        /// If this asset is a material, contains those properties
        /// </summary>
        public Material? Material;

        /// <summary>
        /// If this asset is a texture, contains those properties
        /// </summary>
        public Texture? Texture;
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

    /// <summary>
    /// Contains material asset info
    /// </summary>
    public struct Material
    {
        /// <summary>
        /// The main color of the material
        /// </summary>
        public MWColor Color;

        /// <summary>
        /// The ID of the main texture asset
        /// </summary>
        public Guid? MainTextureId;

        /// <summary>
        /// Offset the texture by this amount as a fraction of the resolution
        /// </summary>
        public MWVector2 MainTextureOffset;

        /// <summary>
        /// Scale the texture by this amount in each axis
        /// </summary>
        public MWVector2 MainTextureScale;
    }

    /// <summary>
    /// Contains a basic texture description
    /// </summary>
    public struct Texture
    {
        /// <summary>
        /// The resolution of the texture
        /// </summary>
        public MWVector2 Resolution;

        /// <summary>
        /// How out-of-range U coordinates should be handled
        /// </summary>
        public UnityEngine.TextureWrapMode? WrapModeU;

        /// <summary>
        /// How out-of-range V coordinates should be handled
        /// </summary>
        public UnityEngine.TextureWrapMode? WrapModeV;
    }
}

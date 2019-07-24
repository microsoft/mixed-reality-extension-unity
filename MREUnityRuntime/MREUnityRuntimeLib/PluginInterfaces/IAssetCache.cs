// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using MixedRealityExtension.Assets;
using UnityEngine;

namespace MixedRealityExtension.PluginInterfaces
{
    /// <summary>
    /// Stores assets loaded by MREs.
    /// </summary>
    public interface IAssetCache
    {
        /// <returns>The GameObject all cache assets should be beneath</returns>
        GameObject CacheRootGO();

        /// <returns>The GameObject cloned by Actor.CreateEmpty calls</returns>
        GameObject EmptyTemplate();

        /// <summary>
        /// Retrieve an asset from the cache by ID, or null if an asset with that ID is not loaded.
        /// </summary>
        /// <param name="id">The ID of a loaded asset.</param>
        /// <returns>A native Unity asset</returns>
        UnityEngine.Object GetAsset(Guid? id);

        /// <summary>
        /// If an asset is in the cache, return its ID. Otherwise return null.
        /// </summary>
        /// <param name="asset">The asset whose ID should be returned</param>
        /// <returns></returns>
        Guid? GetId(UnityEngine.Object asset);

        /// <summary>
        /// Get notified when an asset gets created
        /// </summary>
        /// <param name="id">The ID of an asset</param>
        /// <param name="callback">A function to run once when the asset is registered</param>
        void OnCached(Guid id, Action<UnityEngine.Object> callback);

        /// <summary>
        /// Cache an asset with the given lookup values.
        /// </summary>
        /// <param name="asset">The native Unity asset</param>
        /// <param name="id">The ID of the asset.</param>
        /// <param name="containerId">The container ID of the asset.</param>
        /// <param name="source">The origin container.</param>
        void CacheAsset(UnityEngine.Object asset, Guid id, Guid containerId, AssetSource source = null);

        /// <summary>
        /// Remove assets from the cache with the given container ID.
        /// </summary>
        /// <param name="containerId">The container ID.</param>
        /// <returns>The list of assets removed from the cache</returns>
        IEnumerable<UnityEngine.Object> UncacheAssets(Guid containerId);
    }
}

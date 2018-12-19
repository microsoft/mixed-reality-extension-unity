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

        /// <summary>
        /// Retrieve the IDs of assets loaded from a source.
        /// </summary>
        /// <param name="source">The asset source</param>
        /// <returns>A list of IDs, or null if the given source is not loaded.</returns>
        IEnumerable<Guid> GetAssetIdsInSource(AssetSource source);

        /// <summary>
        /// Retrieve an asset from the cache by ID, or null if an asset with that ID is not loaded.
        /// </summary>
        /// <param name="id">The ID of a loaded asset.</param>
        /// <returns>A native Unity asset</returns>
        UnityEngine.Object GetAsset(Guid id);

        /// <summary>
        /// Cache an asset with the given lookup values.
        /// </summary>
        /// <param name="source">The origin container.</param>
        /// <param name="id">The ID of the asset.</param>
        /// <param name="asset">The native Unity asset</param>
        void CacheAsset(AssetSource source, Guid id, UnityEngine.Object asset);

    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using MixedRealityExtension.PluginInterfaces;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MixedRealityExtension.Assets
{
    /// <summary>
    /// A default implementation of IAssetCache
    /// </summary>
    public class AssetCache : IAssetCache
    {
        private readonly Dictionary<Guid, Object> assets = new Dictionary<Guid, Object>(100);
        private readonly Dictionary<AssetSource, List<Guid>> assetsBySource = new Dictionary<AssetSource, List<Guid>>(10);
        private readonly GameObject cacheRoot;
        private readonly GameObject emptyTemplate;

        public AssetCache(GameObject root = null)
        {
            cacheRoot = root ?? new GameObject("MRE Cache Root");
            cacheRoot.SetActive(false);

            emptyTemplate = new GameObject("Empty");
            emptyTemplate.transform.SetParent(cacheRoot.transform, false);
        }

        /// <inheritdoc cref="CacheRootGO"/>
        public GameObject CacheRootGO()
        {
            return cacheRoot;
        }

        /// <inheritdoc cref="EmptyTemplate"/>
        public GameObject EmptyTemplate()
        {
            return emptyTemplate;
        }

        /// <inheritdoc cref="GetAssetIdsInSource"/>
        public IEnumerable<Guid> GetAssetIdsInSource(AssetSource source)
        {
            assetsBySource.TryGetValue(source, out var guids);
            return guids;
        }

        /// <inheritdoc cref="GetAsset"/>
        public Object GetAsset(Guid id)
        {
            assets.TryGetValue(id, out var asset);
            return asset;
        }

        /// <inheritdoc cref="CacheAsset"/>
        public void CacheAsset(AssetSource source, Guid id, Object asset)
        {
            if (!assetsBySource.ContainsKey(source))
            {
                assetsBySource[source] = new List<Guid>(10);
            }

            assetsBySource[source].Add(id);
            assets[id] = asset;
        }
    }
}

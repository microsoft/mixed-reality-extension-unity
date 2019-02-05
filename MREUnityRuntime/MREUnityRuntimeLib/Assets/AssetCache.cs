// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using MixedRealityExtension.PluginInterfaces;
using MixedRealityExtension.Util;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MixedRealityExtension.Assets
{
    /// <summary>
    /// A default implementation of IAssetCache
    /// </summary>
    public class AssetCache : IAssetCache
    {
        private const int ASSET_DEFAULT_COUNT = 10;
        private const int ASSET_SOURCES_DEFAULT_COUNT = 5;

        private readonly Dictionary<Guid, Object> assets
            = new Dictionary<Guid, Object>(ASSET_SOURCES_DEFAULT_COUNT * ASSET_DEFAULT_COUNT);
        private readonly Dictionary<Object, Guid> ids
            = new Dictionary<Object, Guid>(ASSET_SOURCES_DEFAULT_COUNT * ASSET_DEFAULT_COUNT);
        private readonly Dictionary<AssetSource, List<Guid>> assetsBySource
            = new Dictionary<AssetSource, List<Guid>>(ASSET_SOURCES_DEFAULT_COUNT);
        private readonly List<Guid> manualAssets = new List<Guid>(ASSET_DEFAULT_COUNT);
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
        public IEnumerable<Guid> GetAssetIdsInSource(AssetSource source = null)
        {
            List<Guid> guids;

            if (source != null)
            {
                assetsBySource.TryGetValue(source, out guids);
            }
            else
            {
                guids = manualAssets;
            }

            return guids;
        }

        /// <inheritdoc cref="GetAsset"/>
        public Object GetAsset(Guid? id)
        {
            if (id == null)
                return null;

            assets.TryGetValue(id.Value, out var asset);
            return asset;
        }

        /// <inheritdoc cref="GetId"/>
        public Guid? GetId(Object asset)
        {
            if(asset == null)
            {
                return null;
            }

            ids.TryGetValue(asset, out var guid);
            return guid;
        }

        /// <inheritdoc cref="CacheAsset"/>
        public void CacheAsset(Object asset, Guid id, AssetSource source = null)
        {
            List<Guid> assetList;
            if(source != null)
            {
                assetList = assetsBySource.GetOrCreate(source, () => new List<Guid>(ASSET_DEFAULT_COUNT));
            }
            else
            {
                assetList = manualAssets;
            }

            assetList.Add(id);
            assets[id] = asset;
            ids[asset] = id;
        }
    }
}

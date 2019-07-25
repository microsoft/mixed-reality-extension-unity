// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using MixedRealityExtension.PluginInterfaces;
using MixedRealityExtension.Util;
using UnityEngine;
using Object = UnityEngine.Object;

using CacheCallback = System.Action<UnityEngine.Object>;

namespace MixedRealityExtension.Assets
{
    /// <summary>
    /// A default implementation of IAssetCache
    /// </summary>
    public class AssetCache : IAssetCache
    {
        private readonly struct CacheEntry
        {
            public readonly Guid Id;
            public readonly Guid ContainerId;
            public readonly AssetSource Source;
            public readonly Object Asset;

            public CacheEntry(Guid id, Guid containerId, AssetSource source, Object asset)
            {
                Id = id;
                ContainerId = containerId;
                Source = source;
                Asset = asset;
            }
        }

        private readonly List<CacheEntry> cache = new List<CacheEntry>(50);
        private readonly Dictionary<Guid, List<CacheCallback>> cacheCallbacks = new Dictionary<Guid, List<CacheCallback>>(50);
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

        /// <inheritdoc cref="GetAsset"/>
        public Object GetAsset(Guid? id)
        {
            return id != null ? cache.Find(c => c.Id == id).Asset : null;
        }

        /// <inheritdoc cref="GetId"/>
        public Guid? GetId(Object asset)
        {
            return asset != null ? cache.Find(c => c.Asset == asset).Id : (Guid?)null;
        }

        /// <inheritdoc cref="OnCached"/>
        public void OnCached(Guid id, CacheCallback callback)
        {
            var asset = GetAsset(id);
            if (cache.Any(c => c.Id == id))
            {
                try
                {
                    callback?.Invoke(asset);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
            else
            {
                cacheCallbacks.GetOrCreate(id, () => new List<CacheCallback>(3)).Add(callback);
            }
        }

        /// <inheritdoc cref="CacheAsset"/>
        public void CacheAsset(Object asset, Guid id, Guid containerId, AssetSource source = null)
        {
            if (cache.Any(c => c.Id == id))
            {
                throw new Exception($"An asset with ID {id} is already cached!");
            }

            cache.Add(new CacheEntry(id, containerId, source, asset));
            if (cacheCallbacks.TryGetValue(id, out List<CacheCallback> callbacks))
            {
                cacheCallbacks.Remove(id);
                foreach (var cb in callbacks)
                {
                    try
                    {
                        cb?.Invoke(asset);
                    }
                    catch(Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
            }
        }

        /// <inheritdoc cref="UncacheAssets"/>
        public IEnumerable<Object> UncacheAssets(Guid containerId)
        {
            var assets = cache.Where(c => c.ContainerId == containerId && c.Asset != null).Select(c => c.Asset).ToArray();
            cache.RemoveAll(c => c.ContainerId == containerId);
            return assets;
        }
    }
}

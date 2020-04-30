// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.PluginInterfaces;
using MixedRealityExtension.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityGLTF;
using CacheCallback = System.Action<UnityEngine.Object>;
using ColliderGeometry = MixedRealityExtension.Core.ColliderGeometry;
using Object = UnityEngine.Object;

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
			public readonly ColliderGeometry ColliderGeometry;

			public CacheEntry(Guid id, Guid containerId, AssetSource source, Object asset, ColliderGeometry collider = null)
			{
				Id = id;
				ContainerId = containerId;
				Source = source;
				Asset = asset;
				ColliderGeometry = collider;
			}
		}

		private readonly List<CacheEntry> cache = new List<CacheEntry>(50);
		private readonly Dictionary<Guid, List<CacheCallback>> cacheCallbacks = new Dictionary<Guid, List<CacheCallback>>(50);
		private readonly Dictionary<Guid, Tuple<GLTFSceneImporter, GLTF.Schema.GLTFRoot>> importerCache = new Dictionary<Guid, Tuple<GLTFSceneImporter, GLTF.Schema.GLTFRoot>>(50);
		private readonly GameObject cacheRoot;
		private readonly GameObject emptyTemplate;

		public AssetCache(GameObject root = null)
		{
			cacheRoot = root ?? new GameObject("MRE Cache Root");
			cacheRoot.SetActive(false);

			emptyTemplate = new GameObject("Empty");
			emptyTemplate.transform.SetParent(cacheRoot.transform, false);
		}

		public void CacheGLTFImporter(Guid id, GLTFSceneImporter importer, GLTF.Schema.GLTFRoot root)
		{
			TryGetGLTFImporter(id, out GLTFSceneImporter oldImporter, out GLTF.Schema.GLTFRoot oldRoot);
			if (oldImporter != null)
			{
				oldImporter.Dispose();
				importerCache.Remove(id);
			}
			importerCache.Add(id, Tuple.Create(importer, root));
		}

		public void TryGetGLTFImporter(Guid id, out GLTFSceneImporter importer, out GLTF.Schema.GLTFRoot root)
		{
			Tuple<GLTFSceneImporter, GLTF.Schema.GLTFRoot> result;
			if (importerCache.TryGetValue(id, out result))
			{
				importer = result.Item1;
				root = result.Item2;
			}
			else
			{
				importer = null;
				root = null;
			}
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

		/// <inheritdoc cref="GetColliderGeometry(Guid?)"/>
		public ColliderGeometry GetColliderGeometry(Guid? id)
		{
			return id != null ? cache.Find(c => c.Id == id).ColliderGeometry : null;
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
		public void CacheAsset(Object asset, Guid id, Guid containerId, AssetSource source = null, ColliderGeometry colliderGeo = null)
		{
			if (!cache.Any(c => c.Id == id))
			{
				cache.Add(new CacheEntry(id, containerId, source, asset, colliderGeo));
			}

			if (cacheCallbacks.TryGetValue(id, out List<CacheCallback> callbacks))
			{
				cacheCallbacks.Remove(id);
				foreach (var cb in callbacks)
				{
					try
					{
						cb?.Invoke(asset);
					}
					catch (Exception e)
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

		public void ForceCleanCache()
		{
			foreach (var entry in importerCache)
			{
				entry.Value.Item1.Dispose();
			}
			importerCache.Clear();

			for (int i = 0; i < cache.Count; ++i)
			{
				if (cache[i].Asset is GameObject prefab)
				{
					var filters = prefab.GetComponentsInChildren<MeshFilter>();
					foreach (var f in filters)
					{
						UnityEngine.Object.Destroy(f.sharedMesh);
					}
				}
				UnityEngine.GameObject.Destroy(cache[i].Asset);
			}
			cache.Clear();

			cacheCallbacks.Clear();
		}
	}
}

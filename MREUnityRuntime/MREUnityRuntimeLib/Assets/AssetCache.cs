// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.PluginInterfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MixedRealityExtension.Assets
{
	/// <summary>
	/// Default in-memory implementation of the asset cache interface
	/// </summary>
	public class AssetCache : MonoBehaviour, IAssetCache
	{
		private class CacheItem
		{
			public readonly Uri Uri;
			public readonly IEnumerable<Object> Assets;
			public readonly string Version;
			public int ReferenceCount;

			public CacheItem PreviousVersion;

			public CacheItem(
				Uri uri,
				IEnumerable<Object> assets,
				string version,
				int referenceCount,
				CacheItem previousVersion = null)
			{
				Uri = uri;
				Assets = assets;
				Version = version;
				ReferenceCount = referenceCount;
				PreviousVersion = previousVersion;
			}

			public override string ToString()
			{
				return $"[Uri: {Uri}, {Assets.Count()} assets, Version: {Version}, References: {ReferenceCount}]";
			}

			public CacheItem FindInHistory(string version)
			{
				if (version == Version)
				{
					return this;
				}
				else if (PreviousVersion == null)
				{
					return null;
				}
				else
				{
					return PreviousVersion.FindInHistory(version);
				}
			}
		}

		private readonly Dictionary<Uri, CacheItem> Cache = new Dictionary<Uri, CacheItem>(10);
		private Coroutine CleanTimer = null;

		/// <summary>
		/// The maximum time (in seconds) dereferenced assets are allowed to stay in memory.
		/// </summary>
		public int CleanInterval { get; set; } = 30;

		///<inheritdoc/>
		public GameObject CacheRootGO { get; set; }

		///<inheritdoc/>
		public void StoreAssets(Uri uri, IEnumerable<Object> assets, string version)
		{
			Debug.LogFormat("Storing {0} (version {1})", uri, version);
			// already a cached asset for this uri
			if (Cache.TryGetValue(uri, out CacheItem cacheItem))
			{
				Debug.Log("Cached version found");
				// Note: might be same as cacheItem
				CacheItem oldItem = cacheItem.FindInHistory(version);

				// these assets are already in the cache, just dereference
				if (oldItem != null)
				{
					Debug.Log("Cached version == stored version, updating");
					var newRefCount = oldItem.ReferenceCount - assets.Count();
					oldItem.ReferenceCount = newRefCount >= 0 ? newRefCount : 0;
					if (oldItem.ReferenceCount == 0)
					{
						ScheduleCleanUnusedResources();
					}
				}
				// the submitted version is not in history (i.e. is a new version), update cache
				else
				{
					Debug.Log("Cached version != stored version, replacing");
					Cache[uri] = new CacheItem(uri, assets.ToArray(), version,
						referenceCount: assets.Count(),
						previousVersion: cacheItem);
				}
			}
			// no previously cached assets, just store
			else
			{
				cacheItem = new CacheItem(uri, assets.ToArray(), version, assets.Count());
				Debug.LogFormat("Caching {0}", cacheItem);
				Cache.Add(uri, cacheItem);
			}
		}

		///<inheritdoc/>
		public Task<IEnumerable<Object>> LeaseAssets(Uri uri, string ifMatchesVersion = null)
		{
			if (Cache.TryGetValue(uri, out CacheItem cacheItem))
			{
				cacheItem.ReferenceCount += cacheItem.Assets.Count();
				return Task.FromResult(cacheItem.Assets);
			}
			else
			{
				return Task.FromResult<IEnumerable<Object>>(null);
			}
		}

		///<inheritdoc/>
		public Task<string> GetVersion(Uri uri)
		{
			if (Cache.TryGetValue(uri, out CacheItem cacheItem))
			{
				Debug.LogFormat("Cache hit: {0}", cacheItem);
				return Task.FromResult(cacheItem.Version);
			}
			else
			{
				Debug.LogFormat("{0} not found in cache", uri);
				return Task.FromResult<string>(null);
			}
		}

		/// <summary>
		/// Deallocates any cache items that have zero references.
		/// </summary>
		public void CleanUnusedResources()
		{
			// returns whether the item was deallocated
			bool CleanBottomUp(CacheItem item)
			{
				if (item.PreviousVersion != null && CleanBottomUp(item.PreviousVersion))
				{
					item.PreviousVersion = null;
				}

				if (item.PreviousVersion == null && item.ReferenceCount == 0)
				{
					foreach (var o in item.Assets)
					{
						Object.Destroy(o);
					}
					return true;
				}
				else
				{
					return false;
				}
			}

			foreach (CacheItem cacheItem in Cache.Values.ToArray())
			{
				if (CleanBottomUp(cacheItem))
				{
					Cache.Remove(cacheItem.Uri);
				}
			}
		}

		private void ScheduleCleanUnusedResources()
		{
			if (CleanTimer == null)
			{
				CleanTimer = StartCoroutine(CleanupCoroutine());
			}
		}

		private IEnumerator CleanupCoroutine()
		{
			yield return new WaitForSeconds(CleanInterval);
			CleanUnusedResources();
			CleanTimer = null;
		}
	}
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.PluginInterfaces;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace MixedRealityExtension.Assets
{
	/// <summary>
	/// Default in-memory implementation of the asset cache interface
	/// </summary>
	public class AssetCache : MonoBehaviour, IAssetCache
	{
		private class CacheItem
		{
			public readonly string Uri;
			public readonly IEnumerable<Object> Assets;
			public readonly string Version;
			public int ReferenceCount;

			public CacheItem PreviousVersion;

			public CacheItem(
				string uri,
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

		private readonly Dictionary<string, CacheItem> Cache = new Dictionary<string, CacheItem>(10);
		private Coroutine CleanTimer = null;

		/// <summary>
		/// The maximum time (in seconds) dereferenced assets are allowed to stay in memory.
		/// </summary>
		public int CleanInterval { get; set; } = 30;

		///<inheritdoc/>
		public GameObject CacheRootGO => gameObject;

		///<inheritdoc/>
		public void StoreAssets(string uri, IEnumerable<Object> assets, string version)
		{
			// already a cached asset for this uri
			if (Cache.TryGetValue(uri, out CacheItem cacheItem))
			{
				// Note: might be same as cacheItem
				CacheItem oldItem = cacheItem.FindInHistory(version);

				// these assets are already in the cache, just dereference
				if (oldItem != null)
				{
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
					Cache[uri] = new CacheItem(uri, assets.ToArray(), version,
						referenceCount: assets.Count(),
						previousVersion: cacheItem);
				}

			}
			// no previously cached assets, just store
			else
			{
				Cache[uri] = new CacheItem(uri, assets.ToArray(), version, 0);
			}
		}

		///<inheritdoc/>
		public Task<IEnumerable<Object>> LeaseAssets(string uri, string ifMatchesVersion = null)
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
		public string GetVersion(string uri)
		{
			if (Cache.TryGetValue(uri, out CacheItem cacheItem))
			{
				return cacheItem.Version;
			}
			else
			{
				return null;
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

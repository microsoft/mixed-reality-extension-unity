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
		protected class CacheItem
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

		// needed to prevent UnityEngine.Resources.UnloadUnusedAssets from unloading the cache
		[SerializeField] protected List<Object> CacheInspector = new List<Object>(30);

		protected readonly Dictionary<Uri, CacheItem> Cache = new Dictionary<Uri, CacheItem>(10);
		private Coroutine CleanTimer = null;

		/// <summary>
		/// The maximum time (in seconds) dereferenced assets are allowed to stay in memory.
		/// </summary>
		public int CleanInterval { get; set; } = 30;

		///<inheritdoc/>
		public GameObject CacheRootGO { get; set; }

		[SerializeField]
		private GameObject SerializedCacheRoot;

		/// <inheritdoc />
		public bool SupportsSync { get; protected set; } = true;

		protected virtual void Start()
		{
			if (SerializedCacheRoot != null)
			{
				CacheRootGO = SerializedCacheRoot;
			}
			Application.lowMemory += CleanUnusedResources;
		}

		///<inheritdoc/>
		public virtual void StoreAssets(Uri uri, IEnumerable<Object> assets, string version)
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
					CacheInspector.AddRange(assets);
				}
			}
			// no previously cached assets, just store
			else
			{
				cacheItem = new CacheItem(uri, assets.ToArray(), version, assets.Count());
				Cache.Add(uri, cacheItem);
				CacheInspector.AddRange(assets);
			}
		}

		/// <inheritdoc />
		public virtual Task<IEnumerable<Object>> LeaseAssets(Uri uri, string ifMatchesVersion = null)
		{
			return Task.FromResult(LeaseAssetsSync(uri, ifMatchesVersion));
		}

		///<inheritdoc/>
		public virtual IEnumerable<Object> LeaseAssetsSync(Uri uri, string ifMatchesVersion = null)
		{
			if (Cache.TryGetValue(uri, out CacheItem cacheItem))
			{
				cacheItem.ReferenceCount += cacheItem.Assets.Count();
				return cacheItem.Assets;
			}
			else
			{
				return null;
			}
		}

		/// <inheritdoc />
		public virtual Task<string> GetVersion(Uri uri)
		{
			return Task.FromResult(GetVersionSync(uri));
		}

		///<inheritdoc/>
		public virtual string GetVersionSync(Uri uri)
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
		public virtual void CleanUnusedResources()
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
						CacheInspector.Remove(o);
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

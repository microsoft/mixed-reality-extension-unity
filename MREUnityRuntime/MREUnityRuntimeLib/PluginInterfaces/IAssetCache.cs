// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MixedRealityExtension.PluginInterfaces
{
	/// <summary>
	/// This is a system-wide class instance that is responsible for caching assets beyond the lifetime of a single
	/// MRE instance. This could possibly be backed by persistent storage instead of memory. This is primarily
	/// intended for assets loaded via an HTTP request.
	/// </summary>
	public interface IAssetCache
	{
		/// <summary>
		/// The GameObject that assets requiring a parent should be put.
		/// </summary>
		UnityEngine.GameObject CacheRootGO { get; }

		/// <summary>
		/// Indicate that the assets at the provided URL are loading but not ready yet. Any requests to [[LeaseAssets]]
		/// or [[TryGetVersion]] for these resources will be delayed until loading is complete.
		/// </summary>
		/// <param name="uri">The resource identifier.</param>
		/// <param name="blockingTask">A pending Task that should complete after the assets are loaded and cached.</param>
		void BlockWhileLoading(Uri uri, Task blockingTask);

		/// <summary>
		/// If either the cache contains no assets for the resource, or the cached
		/// version is older than what is provided, this method stores all provided assets in the cache, overwriting
		/// any currently cached assets, and sets the reference count to the number of assets. Otherwise, the internal
		/// reference counter for this resource is decremented by the number of provided assets.
		/// </summary>
		/// <param name="uri">The resource identifier</param>
		/// <param name="assets">The collection of assets generated from the given resource</param>
		/// <param name="version">
		/// The version of the loaded resource. Will typically be the HTTP response's ETag header.
		/// </param>
		void StoreAssets(Uri uri, IEnumerable<UnityEngine.Object> assets, string version);

		/// <summary>
		/// Asynchronously return the cached assets at the given URI, and increment the internal reference counter
		/// for this resource. Will return null if no assets are cached for that resource, or if ifMatchesVersion
		/// does not match the stored assets' version. Will wait for any loading tasks to complete.
		/// </summary>
		/// <param name="uri">The resource identifier</param>
		/// <param name="ifMatchesVersion">Return null if the cached assets are not of this version</param>
		/// <returns></returns>
		Task<IEnumerable<UnityEngine.Object>> LeaseAssets(Uri uri, string ifMatchesVersion = null);

		/// <summary>
		/// Returns the stored version of the given resource, or null if not cached. We'll need this for If-Not-Match
		/// HTTP headers. Will wait for any loading tasks to complete.
		/// </summary>
		/// <param name="uri">The resource identifier</param>
		/// <returns></returns>
		Task<string> TryGetVersion(Uri uri);
	}
}

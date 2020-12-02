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
		/// Specifies whether the cache supports synchronous reads
		/// </summary>
		bool SupportsSync { get; }

		/// <summary>
		/// The GameObject that assets requiring a parent should be put.
		/// </summary>
		UnityEngine.GameObject CacheRootGO { get; }

		/// <summary>
		/// Acquire the exclusive right to load this resource, as the client shouldn't be loading multiple copies
		/// of the same resource. Must call <see cref="ReleaseLoadingLock(Uri)"/> after the cache is populated
		/// to avoid deadlock.
		/// </summary>
		/// <param name="uri">The resource URL.</param>
		/// <returns>
		/// A task that completes with `true` if the lock was successfully acquired; `false` otherwise.
		/// </returns>
		Task<bool> AcquireLoadingLock(Uri uri);

		/// <summary>
		/// Release the exclusive loading rights for this resource. Call this after <see cref="AcquireLoadingLock(Uri)"/>.
		/// </summary>
		/// <param name="uri">The resource URL.</param>
		void ReleaseLoadingLock(Uri uri);

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
		/// does not match the stored assets' version. This should be async in case the asset needs to be loaded
		/// from persistent storage.
		/// </summary>
		/// <param name="uri">The resource identifier</param>
		/// <param name="ifMatchesVersion">Return null if the cached assets are not of this version</param>
		/// <returns>A task that completes with the cached assets, or null if no assets are cached.</returns>
		Task<IEnumerable<UnityEngine.Object>> LeaseAssets(Uri uri, string ifMatchesVersion = null);

		/// <summary>
		/// Same as <see cref="LeaseAssets(Uri, string)"/>, but is only valid if <see cref="SupportsSync"/> is true.
		/// </summary>
		/// <param name="uri">The resource identifier</param>
		/// <param name="ifMatchesVersion">Return null if the cached assets are not of this version</param>
		/// <returns>The cached assets, or null if no assets are cached.</returns>
		IEnumerable<UnityEngine.Object> LeaseAssetsSync(Uri uri, string ifMatchesVersion = null);

		/// <summary>
		/// Returns the stored version of the given resource, or null if not cached. We'll need this for If-Not-Match
		/// HTTP headers.
		/// </summary>
		/// <param name="uri">The resource identifier.</param>
		/// <returns>
		/// A task that completes with the version string of the cached assets, or null if nothing is cached.
		/// </returns>
		Task<string> TryGetVersion(Uri uri);

		/// <summary>
		/// Same as <see cref="TryGetVersion(Uri)"/>, but is only valid if <see cref="SupportsSync"/> is true.
		/// </summary>
		/// <param name="uri">The resource identifier.</param>
		/// <returns>
		/// The version string of the cached assets, or null if nothing is cached.
		/// </returns>
		string TryGetVersionSync(Uri uri);
	}
}

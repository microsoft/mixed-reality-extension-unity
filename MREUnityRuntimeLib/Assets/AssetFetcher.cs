// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.API;
using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace MixedRealityExtension.Assets
{
	public static class AssetFetcher<T> where T : UnityEngine.Object
	{
		public struct FetchResult
		{
			public T Asset;
			public string FailureMessage;
			public long ReturnCode;
			public string ETag;

			public bool IsPopulated => ReturnCode != 0;
		}

		public static async Task<FetchResult> LoadTask(MonoBehaviour runner, Uri uri)
		{
			// acquire the exclusive right to load this asset
			if (!await MREAPI.AppsAPI.AssetCache.AcquireLoadingLock(uri))
			{
				throw new TimeoutException("Failed to acquire exclusive loading rights for " + uri);
			}

			FetchResult result = new FetchResult()
			{
				Asset = null,
				FailureMessage = null
			};
			var ifNoneMatch = MREAPI.AppsAPI.AssetCache.SupportsSync
				? MREAPI.AppsAPI.AssetCache.TryGetVersionSync(uri)
				: await MREAPI.AppsAPI.AssetCache.TryGetVersion(uri);

			// if the cached version is unversioned, i.e. the server doesn't support ETags, don't bother making request
			if (ifNoneMatch == Constants.UnversionedAssetVersion)
			{
				var assets = MREAPI.AppsAPI.AssetCache.SupportsSync
					? MREAPI.AppsAPI.AssetCache.LeaseAssetsSync(uri)
					: await MREAPI.AppsAPI.AssetCache.LeaseAssets(uri);
				result.Asset = assets.FirstOrDefault() as T;

				MREAPI.AppsAPI.AssetCache.ReleaseLoadingLock(uri);
				return result;
			}

			runner.StartCoroutine(LoadCoroutine());

			// Spin asynchronously until the request completes.
			while (!result.IsPopulated)
			{
				await Task.Delay(10);
			}

			// handle caching
			if (!string.IsNullOrEmpty(ifNoneMatch) && result.ReturnCode == 304)
			{
				var assets = MREAPI.AppsAPI.AssetCache.SupportsSync
					? MREAPI.AppsAPI.AssetCache.LeaseAssetsSync(uri)
					: await MREAPI.AppsAPI.AssetCache.LeaseAssets(uri);
				result.Asset = assets.FirstOrDefault() as T;
			}
			else if (result.Asset != null)
			{
				MREAPI.AppsAPI.AssetCache.StoreAssets(
					uri,
					new UnityEngine.Object[] { result.Asset },
					result.ETag);
			}

			MREAPI.AppsAPI.AssetCache.ReleaseLoadingLock(uri);
			return result;

			IEnumerator LoadCoroutine()
			{
				DownloadHandler handler;
				if (typeof(T) == typeof(UnityEngine.AudioClip))
				{
					handler = new DownloadHandlerAudioClip(uri, AudioType.UNKNOWN);
				}
				else if (typeof(T) == typeof(UnityEngine.Texture))
				{
					handler = new DownloadHandlerTexture(false);
				}
				else
				{
					result.FailureMessage = $"Unknown download type: {typeof(T)}";
					yield break;
				}

				// Spin asynchronously until the load throttler would allow us through.
				while (AssetLoadThrottling.WouldThrottle())
				{
					yield return null;
				}

				using (var scope = new AssetLoadThrottling.AssetLoadScope())
				using (var www = new UnityWebRequest(uri, "GET", handler, null))
				{
					if (!string.IsNullOrEmpty(ifNoneMatch))
					{
						www.SetRequestHeader("If-None-Match", ifNoneMatch);
					}

					yield return www.SendWebRequest();
					if (www.isNetworkError)
					{
						result.ReturnCode = -1;
						result.FailureMessage = www.error;
					}
					else
					{
						result.ReturnCode = www.responseCode;
						result.ETag = www.GetResponseHeader("ETag") ?? Constants.UnversionedAssetVersion;

						if (www.isHttpError)
						{
							result.FailureMessage = $"[{www.responseCode}] {uri}";
						}
						else if (www.responseCode >= 200 && www.responseCode <= 299)
						{
							if (typeof(T).IsAssignableFrom(typeof(UnityEngine.AudioClip)))
							{
								result.Asset = ((DownloadHandlerAudioClip)handler).audioClip as T;
							}
							else if (typeof(T).IsAssignableFrom(typeof(UnityEngine.Texture)))
							{
								result.Asset = ((DownloadHandlerTexture)handler).texture as T;
							}
						}
					}
				}
			}
		}
	}
}

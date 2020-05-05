// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace MixedRealityExtension.Assets
{
	public static class AssetFetcher<T> where T : class
	{
		public struct FetchResult
		{
			public T Asset;
			public string FailureMessage;
			public bool IsPopulated => Asset != null || FailureMessage != null;
		}

		public static async Task<FetchResult> LoadTask(MonoBehaviour runner, Uri uri)
		{
			FetchResult result = new FetchResult()
			{
				Asset = null,
				FailureMessage = null
			};

			runner.StartCoroutine(LoadCoroutine());

			// Spin asynchronously until the request completes.
			while (!result.IsPopulated)
			{
				await Task.Delay(10);
			}

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
					yield return www.SendWebRequest();
					if (www.isNetworkError)
					{
						result.FailureMessage = www.error;
					}
					else if (www.isHttpError)
					{
						result.FailureMessage = $"[{www.responseCode}] {uri}";
					}
					else
					{
						if (typeof(T) == typeof(UnityEngine.AudioClip))
						{
							result.Asset = ((DownloadHandlerAudioClip)handler).audioClip as T;
						}
						else if (typeof(T) == typeof(UnityEngine.Texture))
						{
							result.Asset = ((DownloadHandlerTexture)handler).texture as T;
						}
					}
				}
			}
		}
	}
}

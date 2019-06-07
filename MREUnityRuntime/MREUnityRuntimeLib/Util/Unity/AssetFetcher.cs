using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

namespace MixedRealityExtension.Util.Unity
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
            while (!result.IsPopulated)
            {
                await Task.Delay(50);
            }

            return result;

            IEnumerator LoadCoroutine()
            {

                UnityEngine.Networking.UnityWebRequest www = null;
                if (typeof(T) == typeof(AudioClip))
                {
                    www = UnityEngine.Networking.UnityWebRequestMultimedia.GetAudioClip(uri, AudioType.UNKNOWN);
                }
                else if (typeof(T) == typeof(Texture))
                {
                    www = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(uri, true);
                }
                using (www)
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
                        if (typeof(T) == typeof(AudioClip))
                        {
                            result.Asset = UnityEngine.Networking.DownloadHandlerAudioClip.GetContent(www) as T;
                        }
                        else if (typeof(T) == typeof(Texture))
                        {
                            result.Asset = UnityEngine.Networking.DownloadHandlerTexture.GetContent(www) as T;
                        }
                    }
                }
            }
        }
    }
}

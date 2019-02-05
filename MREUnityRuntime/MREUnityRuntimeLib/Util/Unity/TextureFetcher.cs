using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

namespace MixedRealityExtension.Util.Unity
{
    public static class TextureFetcher
    {
        public struct FetchResult
        {
            public Texture Texture;
            public string FailureMessage;
            public bool IsPopulated => Texture != null || FailureMessage != null;
        }

        public static async Task<FetchResult> LoadTextureTask(MonoBehaviour runner, Uri uri)
        {
            FetchResult result = new FetchResult() {
                Texture = null,
                FailureMessage = null
            };

            runner.StartCoroutine(LoadTextureCoroutine());
            while (!result.IsPopulated)
            {
                await Task.Delay(50);
            }

            return result;

            IEnumerator LoadTextureCoroutine()
            {
                using (var www = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(uri, true))
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
                        result.Texture = UnityEngine.Networking.DownloadHandlerTexture.GetContent(www);
                    }
                }
            }
        }
    }
}

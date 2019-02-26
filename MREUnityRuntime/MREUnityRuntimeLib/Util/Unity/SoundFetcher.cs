using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

namespace MixedRealityExtension.Util.Unity
{
    public static class SoundFetcher
    {
        public struct FetchResult
        {
            public AudioClip Sound;
            public string FailureMessage;
            public bool IsPopulated => Sound != null || FailureMessage != null;
        }

        public static async Task<FetchResult> LoadSoundTask(MonoBehaviour runner, Uri uri)
        {
            FetchResult result = new FetchResult() {
                Sound = null,
                FailureMessage = null
            };

            runner.StartCoroutine(LoadSoundCoroutine());
            while (!result.IsPopulated)
            {
                await Task.Delay(50);
            }

            return result;

            IEnumerator LoadSoundCoroutine()
            {
                using (var www = UnityEngine.Networking.UnityWebRequestMultimedia.GetAudioClip(uri, AudioType.UNKNOWN))
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
                        result.Sound = UnityEngine.Networking.DownloadHandlerAudioClip.GetContent(www);
                        result.Sound= result.Sound ;
                    }
                }
            }
        }
    }
}

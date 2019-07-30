using UnityEngine;
using System.Collections;

namespace AvStreamPlugin
{
    // Helper class that makes sure our AVStream plugin is called once a frame if there are any
    // active AVStreams. It has to be a mono behavior that makes the StartCoroutine call and
    // this allows us to avoid lifetime issues associated with potentially cycling AVStreams.
    internal class AvStreamPluginUpdate : MonoBehaviour
    {
        private static int RefCount = 0;
        private static GameObject Owner = null;

        public static void AddRef()
        {
            if (RefCount == 0)
            {
                Owner = new GameObject("AvStreamPluginUpdate Object");
                Owner.AddComponent<AvStreamPluginUpdate>();
            }
            RefCount += 1;
        }

        public static void DecRef()
        {
            if (RefCount > 0)
            {
                RefCount -= 1;
                if (RefCount == 0)
                {
                    Destroy(Owner);
                    Owner = null;
                }
            }
            else
            {
                UnityEngine.Debug.LogWarning("Invalid DecRef call. AvStreamPluginUpdate is already deactivated. Ref counting is broken somewhere.");
            }
        }

        void Start()
        {
            StartCoroutine("CallPluginAtEndOfFrames");
        }

        IEnumerator CallPluginAtEndOfFrames()
        {
            while (true)
            {
                // Wait until all frame rendering is done
                yield return new WaitForEndOfFrame();

                // No specific event ID needed, since we only handle 1 thing.
                GL.IssuePluginEvent(NativeUpdateManager.GetRenderEventFunc(), 0);
            }
        }
    }
}

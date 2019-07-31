using AvStreamPlugin;
using MixedRealityExtension.Core.Interfaces;
using MixedRealityExtension.PluginInterfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;


internal class VideoPlayerFactory : IVideoPlayerFactory
{
    private static GameObject prefab;

    public IVideoPlayer CreateVideoPlayer(IActor parent)
    {
        VideoPlayer videoPlayer = null;

        if (prefab == null)
        {
            prefab = Resources.Load<GameObject>("VideoPlayer");
        }
        if (prefab != null)
        {
            GameObject go = UnityEngine.Object.Instantiate(prefab);
            go.transform.SetParent(parent.GameObject.transform, false);
            videoPlayer = go.GetComponentInChildren<VideoPlayer>();
            videoPlayer.SetActor(parent);
        }

        return videoPlayer;
    }

    public FetchResult PreloadVideoAsset(string uri)
    {
        VideoSourceType type = VideoSourceType.Raw;
        int schemaLength = uri.IndexOf("://");
        if (schemaLength >= 0)
        {
            switch (uri.Substring(0, schemaLength))
            {
                case "youtube":
                    type = VideoSourceType.YouTube;
                    uri = uri.Substring(schemaLength + 3);
                    break;
                case "mixer":
                    type = VideoSourceType.Mixer;
                    uri = uri.Substring(schemaLength + 3);
                    break;
                case "twitch":
                    type = VideoSourceType.Twitch;
                    uri = uri.Substring(schemaLength + 3);
                    break;
                default:
                    break;
            }
        }

        VideoSourceDescription videoSourceDescription = ScriptableObject.CreateInstance<VideoSourceDescription>();
        videoSourceDescription.VideoDetails = VideoSource.GetVideoFromString(type, uri);

        return new FetchResult
        {
            Asset = videoSourceDescription
        };
    }
}

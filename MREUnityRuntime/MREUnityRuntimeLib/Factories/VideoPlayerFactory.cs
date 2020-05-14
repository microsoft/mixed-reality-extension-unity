using MixedRealityExtension.Core;
using MixedRealityExtension.Core.Interfaces;
using MixedRealityExtension.PluginInterfaces;
using UnityEngine;
using UnityVideoPlayer = UnityEngine.Video.VideoPlayer;

namespace MixedRealityExtension.Factories
{
	public class VideoPlayerFactory : IVideoPlayerFactory
	{
		public IVideoPlayer CreateVideoPlayer(IActor parent)
		{
			return new VideoPlayer(parent);
		}

		public FetchResult PreloadVideoAsset(string uri)
		{
			throw new System.NotImplementedException();
		}
	}

	public class VideoPlayer : IVideoPlayer
	{
		private UnityVideoPlayer Player;

		public VideoPlayer(IActor parent)
		{
			Player = parent.GameObject.AddComponent<UnityVideoPlayer>();
		}

		public void ApplyMediaStateOptions(MediaStateOptions options)
		{
			
		}

		public void Destroy()
		{
			Object.Destroy(Player);
		}

		public void Play(VideoStreamDescription description, MediaStateOptions options)
		{
			Player.url = description.Uri;
			Player.Play();
		}
	}
}

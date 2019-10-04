// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;
using Microsoft.MixedReality.WebRTC;

namespace AltspaceVR.WebProjector
{
	/// <summary>
	/// Play video frames received from a WebRTC video track.
	/// </summary>
	/// <remarks>
	/// This component writes to the attached <a href="https://docs.unity3d.com/ScriptReference/Material.html">Material</a>,
	/// via the attached <a href="https://docs.unity3d.com/ScriptReference/Renderer.html">Renderer</a>.
	/// </remarks>
	[RequireComponent(typeof(Renderer))]
	[AddComponentMenu("AltspaceVR.WebProjector/MediaPlayer")]
	public class MediaPlayer : MonoBehaviour
	{
		public WebProjector WebProjector;

		[Tooltip("Max video playback framerate, in frames per second")]
		[Range(0.001f, 120f)]
		public float MaxVideoFramerate = 30f;

		/// <summary>
		/// The frame queue from which frames will be rendered.
		/// </summary>
		public VideoFrameQueue<I420VideoFrameStorage> FrameQueue = null;

		/// <summary>
		/// Internal reference to the attached texture
		/// </summary>
		private Texture2D _textureY = null;
		private Texture2D _textureU = null;
		private Texture2D _textureV = null;

		/// <summary>
		/// Internal timing counter
		/// </summary>
		private float lastUpdateTime = 0.0f;

		private Material videoMaterial;
		private float _minUpdateDelay;

		private void Start()
		{
			CreateEmptyVideoTextures();

			// Leave 3ms of margin, otherwise it misses 1 frame and drops to ~20 FPS
			// when Unity is running at 60 FPS.
			_minUpdateDelay = Mathf.Max(0f, 1f / Mathf.Max(0.001f, MaxVideoFramerate) - 0.003f);

			if (WebProjector.AudioSource != null)
			{
				WebProjector.AudioSource.OnAudioStreamStarted += AudioStreamStarted;
				WebProjector.AudioSource.OnAudioStreamStopped += AudioStreamStopped;
			}
			if (WebProjector.VideoSource != null)
			{
				WebProjector.VideoSource.OnVideoStreamStarted += VideoStreamStarted;
				WebProjector.VideoSource.OnVideoStreamStopped += VideoStreamStopped;
			}

			WebProjector.RoomClient.OnShutdown += RoomClient_OnShutdown;
		}

		private void RoomClient_OnShutdown()
		{
			FrameQueue = null;
			CreateEmptyVideoTextures();
		}

		private void OnDestroy()
		{
			if (WebProjector.AudioSource != null)
			{
				WebProjector.AudioSource.OnAudioStreamStarted -= AudioStreamStarted;
				WebProjector.AudioSource.OnAudioStreamStopped -= AudioStreamStopped;
			}
			if (WebProjector.VideoSource != null)
			{
				WebProjector.VideoSource.OnVideoStreamStarted -= VideoStreamStarted;
				WebProjector.VideoSource.OnVideoStreamStopped -= VideoStreamStopped;
			}
		}

		private void AudioStreamStarted()
		{
		}

		private void AudioStreamStopped()
		{
		}

		private void VideoStreamStarted()
		{
			FrameQueue = WebProjector.VideoSource.FrameQueue;
		}

		private void VideoStreamStopped()
		{
			FrameQueue = null;

			// Clear the video display to not confuse the user who could otherwise
			// think that the video is still playing but is lagging.
			CreateEmptyVideoTextures();
		}

		private void CreateEmptyVideoTextures()
		{
			// Create a default checkboard texture which visually indicates
			// that no data is available. This is useful for debugging and
			// for the user to know about the state of the video.
			_textureY = new Texture2D(2, 2);
			_textureY.SetPixel(0, 0, Color.blue);
			_textureY.SetPixel(1, 1, Color.blue);
			_textureY.Apply();
			_textureU = new Texture2D(2, 2);
			_textureU.SetPixel(0, 0, Color.blue);
			_textureU.SetPixel(1, 1, Color.blue);
			_textureU.Apply();
			_textureV = new Texture2D(2, 2);
			_textureV.SetPixel(0, 0, Color.blue);
			_textureV.SetPixel(1, 1, Color.blue);
			_textureV.Apply();

			// Assign that texture to the video player's Renderer component
			videoMaterial = GetComponent<Renderer>().material;
			videoMaterial.SetTexture("_YPlane", _textureY);
			videoMaterial.SetTexture("_UPlane", _textureU);
			videoMaterial.SetTexture("_VPlane", _textureV);
		}

		/// <summary>
		/// Unity Engine Start() hook
		/// </summary>
		/// <remarks>
		/// https://docs.unity3d.com/ScriptReference/MonoBehaviour.Start.html
		/// </remarks>
		private void Update()
		{
			if (FrameQueue != null)
			{
#if UNITY_EDITOR
				// Inside the Editor, constantly update _minUpdateDelay to
				// react to user changes to MaxFramerate.

				// Leave 3ms of margin, otherwise it misses 1 frame and drops to ~20 FPS
				// when Unity is running at 60 FPS.
				_minUpdateDelay = Mathf.Max(0f, 1f / Mathf.Max(0.001f, MaxVideoFramerate) - 0.003f);
#endif
				var curTime = Time.time;
				if (curTime - lastUpdateTime >= _minUpdateDelay)
				{
					TryProcessFrame();
					lastUpdateTime = curTime;
				}


			}
		}

		/// <summary>
		/// Internal helper that attempts to process frame data in the frame queue
		/// </summary>
		private void TryProcessFrame()
		{
			I420VideoFrameStorage frame;
			if (FrameQueue.TryDequeue(out frame))
			{
				int lumaWidth = (int)frame.Width;
				int lumaHeight = (int)frame.Height;
				if (_textureY == null || (_textureY.width != lumaWidth || _textureY.height != lumaHeight))
				{
					_textureY = new Texture2D(lumaWidth, lumaHeight, TextureFormat.R8, false);
					videoMaterial.SetTexture("_YPlane", _textureY);
				}
				int chromaWidth = lumaWidth / 2;
				int chromaHeight = lumaHeight / 2;
				if (_textureU == null || (_textureU.width != chromaWidth || _textureU.height != chromaHeight))
				{
					_textureU = new Texture2D(chromaWidth, chromaHeight, TextureFormat.R8, false);
					videoMaterial.SetTexture("_UPlane", _textureU);
				}
				if (_textureV == null || (_textureV.width != chromaWidth || _textureV.height != chromaHeight))
				{
					_textureV = new Texture2D(chromaWidth, chromaHeight, TextureFormat.R8, false);
					videoMaterial.SetTexture("_VPlane", _textureV);
				}

				unsafe
				{
					fixed (void* buffer = frame.Buffer)
					{
						var src = new System.IntPtr(buffer);
						int lumaSize = lumaWidth * lumaHeight;
						_textureY.LoadRawTextureData(src, lumaSize);
						src += lumaSize;
						int chromaSize = chromaWidth * chromaHeight;
						_textureU.LoadRawTextureData(src, chromaSize);
						src += chromaSize;
						_textureV.LoadRawTextureData(src, chromaSize);
					}
				}

				_textureY.Apply();
				_textureU.Apply();
				_textureV.Apply();

				// Recycle the video frame packet for a later frame
				FrameQueue.RecycleStorage(frame);
			}
		}
	}
}

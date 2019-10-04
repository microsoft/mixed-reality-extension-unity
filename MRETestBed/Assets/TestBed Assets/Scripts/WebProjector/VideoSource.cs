// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Concurrent;
using UnityEngine;
using Microsoft.MixedReality.WebRTC;

namespace AltspaceVR.WebProjector
{
	/// <summary>
	/// This component represents a remote video source added as a video track to an
	/// existing WebRTC peer connection by a remote peer and received locally.
	/// The video track can optionally be displayed locally with a <see cref="MediaPlayer"/>.
	/// </summary>
	[AddComponentMenu("AltspaceVR.WebProjector/VideoSource")]
	public class VideoSource : MonoBehaviour
	{
		/// <summary>
		/// Frame queue holding the pending frames enqueued by the video source itself,
		/// which a video renderer needs to read and display.
		/// </summary>
		public VideoFrameQueue<I420VideoFrameStorage> FrameQueue;

		public event Action OnVideoStreamStarted;
		public event Action OnVideoStreamStopped;

		/// <summary>
		/// Peer connection this remote video source is extracted from.
		/// </summary>
		[Header("Video track source")]
		public WebProjector WebProjector;

		/// <summary>
		/// Internal queue used to marshal work back to the main Unity thread.
		/// </summary>
		private ConcurrentQueue<Action> mainThreadWorkQueue = new ConcurrentQueue<Action>();

		/// <summary>
		/// Implementation of <a href="https://docs.unity3d.com/ScriptReference/MonoBehaviour.Awake.html">MonoBehaviour.Awake</a>
		/// which registers some handlers with the peer connection to listen to its <see cref="RTCPeer.OnInitialized"/>
		/// and <see cref="RTCPeer.OnShutdown"/> events.
		/// </summary>
		protected void Awake()
		{
			FrameQueue = new VideoFrameQueue<I420VideoFrameStorage>(5);
			WebProjector.RoomClient.I420RemoteVideoFrameReady += I420RemoteVideoFrameReady;
			WebProjector.RoomClient.TrackAdded += TrackAdded;
			WebProjector.RoomClient.TrackRemoved += TrackRemoved;
		}

		/// <summary>
		/// Implementation of <a href="https://docs.unity3d.com/ScriptReference/MonoBehaviour.OnDestroy.html">MonoBehaviour.OnDestroy</a>
		/// which unregisters all listeners from the peer connection.
		/// </summary>
		protected void OnDestroy()
		{
			WebProjector.RoomClient.I420RemoteVideoFrameReady -= I420RemoteVideoFrameReady;
			WebProjector.RoomClient.TrackAdded -= TrackAdded;
			WebProjector.RoomClient.TrackRemoved -= TrackRemoved;
		}

		/// <summary>
		/// Implementation of <a href="https://docs.unity3d.com/ScriptReference/MonoBehaviour.Update.html">MonoBehaviour.Update</a>
		/// to execute from the current Unity main thread any background work enqueued from free-threaded callbacks.
		/// </summary>
		protected void Update()
		{
			// Execute any pending work enqueued by background tasks
			Action action;
			while (mainThreadWorkQueue.TryDequeue(out action))
			{
				try
				{
					action();
				}
				catch (Exception ex)
				{
					Debug.LogError(ex);
				}
			}
		}

		/// <summary>
		/// Internal free-threaded helper callback on track added, which enqueues the
		/// <see cref="VideoSource.VideoStreamStarted"/> event to be fired from the main
		/// Unity thread.
		/// </summary>
		private void TrackAdded(PeerConnection.TrackKind trackKind)
		{
			if (trackKind == PeerConnection.TrackKind.Video)
			{
				// Enqueue invoking the unity event from the main Unity thread, so that listeners
				// can directly access Unity objects from their handler function.
				mainThreadWorkQueue.Enqueue(() => OnVideoStreamStarted?.Invoke());
			}
		}

		/// <summary>
		/// Internal free-threaded helper callback on track added, which enqueues the
		/// <see cref="VideoSource.VideoStreamStopped"/> event to be fired from the main
		/// Unity thread.
		/// </summary>
		private void TrackRemoved(PeerConnection.TrackKind trackKind)
		{
			if (trackKind == PeerConnection.TrackKind.Video)
			{
				// Enqueue invoking the unity event from the main Unity thread, so that listeners
				// can directly access Unity objects from their handler function.
				mainThreadWorkQueue.Enqueue(() => OnVideoStreamStopped?.Invoke());
			}
		}

		/// <summary>
		/// Interal help callback on remote video frame ready. Enqueues the newly-available video
		/// frame into the internal <see cref="VideoSource.FrameQueue"/> for later consumption by
		/// a video renderer.
		/// </summary>
		/// <param name="frame">The newly-available video frame from the remote peer</param>
		private void I420RemoteVideoFrameReady(I420AVideoFrame frame)
		{
			// FrameQueue is thread-safe and can be manipulated from any thread (does not access Unity objects).
			FrameQueue.Enqueue(frame);
		}
	}
}

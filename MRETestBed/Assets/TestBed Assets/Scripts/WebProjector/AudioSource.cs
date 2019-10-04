// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Concurrent;
using UnityEngine;
using Microsoft.MixedReality.WebRTC;

namespace AltspaceVR.WebProjector
{
	/// <summary>
	/// This component represents a remote audio source added as an audio track to an
	/// existing WebRTC peer connection by a remote peer and received locally.
	/// The audio track can optionally be displayed locally with a <see cref="MediaPlayer"/>.
	/// </summary>
	[AddComponentMenu("AltspaceVR.WebProjector/AudioSource")]
	public class AudioSource : MonoBehaviour
	{
		public event Action OnAudioStreamStarted;
		public event Action OnAudioStreamStopped;

		/// <summary>
		/// Peer connection this remote audio source is extracted from.
		/// </summary>
		[Header("Audio track source")]
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
			//FrameQueue = new AudioFrameQueue<AudioFrameStorage>(5);
			WebProjector.RoomClient.TrackAdded += TrackAdded;
			WebProjector.RoomClient.TrackRemoved += TrackRemoved;
		}

		/// <summary>
		/// Implementation of <a href="https://docs.unity3d.com/ScriptReference/MonoBehaviour.OnDestroy.html">MonoBehaviour.OnDestroy</a>
		/// which unregisters all listeners from the peer connection.
		/// </summary>
		protected void OnDestroy()
		{
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
			if (trackKind == PeerConnection.TrackKind.Audio)
			{
				// Enqueue invoking the unity event from the main Unity thread, so that listeners
				// can directly access Unity objects from their handler function.
				mainThreadWorkQueue.Enqueue(() => OnAudioStreamStarted?.Invoke());
			}
		}

		/// <summary>
		/// Internal free-threaded helper callback on track added, which enqueues the
		/// <see cref="UnityEngine.AudioSource.AudioStreamStopped"/> event to be fired from the main
		/// Unity thread.
		/// </summary>
		private void TrackRemoved(PeerConnection.TrackKind trackKind)
		{
			if (trackKind == PeerConnection.TrackKind.Audio)
			{
				// Enqueue invoking the unity event from the main Unity thread, so that listeners
				// can directly access Unity objects from their handler function.
				mainThreadWorkQueue.Enqueue(() => OnAudioStreamStopped?.Invoke());
			}
		}

		//private void RemoteAudioFrameReady(AudioFrame frame)
		//{
		//    FrameQueue.Enqueue(frame);
		//}
	}
}

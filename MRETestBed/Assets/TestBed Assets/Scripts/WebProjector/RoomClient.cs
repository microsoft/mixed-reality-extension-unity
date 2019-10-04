// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Threading.Tasks;
using UnityEngine;
using Microsoft.MixedReality.WebRTC;

namespace AltspaceVR.WebProjector
{
	public class RoomClient : MonoBehaviour
	{
		private RTCPeer peer;

		/// <summary>
		/// Unique ID for this client. Allows the `RoomServer` to multiplex multiple `RoomClient`s over a single WebSocket.
		/// </summary>
		[HideInInspector]
		public string ClientId { get; set; } = Guid.NewGuid().ToString();

		/// <summary>
		/// The ID of the room to stream.
		/// </summary>
		[Tooltip("The ID of the room to join.")]
		public string RoomId;

		/// <summary>
		/// The connection to the mre-multicast-service.
		/// </summary>
		public WebProjector WebProjector;

		public event I420VideoFrameDelegate I420RemoteVideoFrameReady;
		public event Action<PeerConnection.TrackKind> TrackAdded;
		public event Action<PeerConnection.TrackKind> TrackRemoved;
		public event Action OnInitialized;
		public event Action OnShutdown;

		public RoomClient()
		{
		}

		public async void Play()
		{
			await NewPeer();
			WebProjector.RoomServer.JoinRoom();
		}

		public void Stop()
		{
			WebProjector.RoomServer.LeaveRoom();
			DisposePeer();
		}

		void OnDestroy()
		{
			WebProjector.RoomServer.LeaveRoom();
			DisposePeer();
		}

		void Awake()
		{
		}

		void Update()
		{
			if (peer != null)
			{
				peer.Update();
			}
		}

		public void Signal(SignalData signal)
		{
			if (peer != null)
			{
				peer.Signal(signal);
			}
		}

		public async Task NewPeer()
		{
			DisposePeer();
			peer = new RTCPeer();
			peer.OnInitialized += PeerConnection_OnInitialized;
			peer.OnShutdown += PeerConnection_OnShutdown;
			peer.OnError += PeerConnection_OnError;
			peer.OnSignal += PeerConnection_OnSignal;
			peer.I420RemoteVideoFrameReady += I420RemoteVideoFrameReady;
			peer.TrackAdded += TrackAdded;
			peer.TrackRemoved += TrackRemoved;
			await peer.InitializeAsync();
		}

		private void PeerConnection_OnSignal(SignalData signal)
		{
			WebProjector.RoomServer.Signal(signal);
		}

		public void DisposePeer()
		{
			if (peer != null)
			{
				peer.Uninitialize();
				peer.OnInitialized -= PeerConnection_OnInitialized;
				peer.OnShutdown -= PeerConnection_OnShutdown;
				peer.OnError -= PeerConnection_OnError;
				peer.OnSignal -= PeerConnection_OnSignal;
				peer.I420RemoteVideoFrameReady -= I420RemoteVideoFrameReady;
				peer.TrackAdded -= TrackAdded;
				peer.TrackRemoved -= TrackRemoved;
				peer = null;
			}
		}

		private void PeerConnection_OnError(Exception ex)
		{
			Debug.LogError(ex);
		}

		private void PeerConnection_OnShutdown()
		{
			Debug.Log("PeerConnection shutdown");
			OnShutdown?.Invoke();
		}

		private void PeerConnection_OnInitialized()
		{
			Debug.Log("PeerConnection initialized");
			OnInitialized?.Invoke();
		}
	}
}

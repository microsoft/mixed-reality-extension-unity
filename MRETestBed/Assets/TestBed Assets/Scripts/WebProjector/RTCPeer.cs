// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Microsoft.MixedReality.WebRTC;
using System.Text;

#if UNITY_WSA && !UNITY_EDITOR
using Windows.UI.Core;
using Windows.Foundation;
using Windows.Media.Core;
using Windows.Media.Capture;
using Windows.ApplicationModel.Core;
#endif

namespace AltspaceVR.WebProjector
{
	public class RTCPeer
	{
		public event Action OnInitialized;
		public event Action OnShutdown;
		public event Action<Exception> OnError;
		public event Action<SignalData> OnSignal;
		public event I420VideoFrameDelegate I420RemoteVideoFrameReady;
		public event Action<PeerConnection.TrackKind> TrackAdded;
		public event Action<PeerConnection.TrackKind> TrackRemoved;

		private ConcurrentQueue<Action> mainThreadWorkQueue = new ConcurrentQueue<Action>();
		private PeerConnection nativePeer;

		public Task InitializeAsync(CancellationToken token = default(CancellationToken))
		{
			// if the peer is already set, we refuse to initialize again.
			// Note: for multi-peer scenarios, use multiple WebRTC components.
			if (nativePeer != null)
			{
				return Task.CompletedTask;
			}

#if UNITY_ANDROID
            AndroidJavaClass systemClass = new AndroidJavaClass("java.lang.System");
            string libname = "jingle_peerconnection_so";
            systemClass.CallStatic("loadLibrary", new object[1] { libname });
            Debug.Log("loadLibrary loaded : " + libname);

            /*
                * Below is equivalent of this java code:
                * PeerConnectionFactory.InitializationOptions.Builder builder = 
                *   PeerConnectionFactory.InitializationOptions.builder(UnityPlayer.currentActivity);
                * PeerConnectionFactory.InitializationOptions options = 
                *   builder.createInitializationOptions();
                * PeerConnectionFactory.initialize(options);
                */

            AndroidJavaClass playerClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject activity = playerClass.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaClass webrtcClass = new AndroidJavaClass("org.webrtc.PeerConnectionFactory");
            AndroidJavaClass initOptionsClass = new AndroidJavaClass("org.webrtc.PeerConnectionFactory$InitializationOptions");
            AndroidJavaObject builder = initOptionsClass.CallStatic<AndroidJavaObject>("builder", new object[1] { activity });
            AndroidJavaObject options = builder.Call<AndroidJavaObject>("createInitializationOptions");

            if (webrtcClass != null)
            {
                webrtcClass.CallStatic("initialize", new object[1] { options });
            }
#endif

#if UNITY_WSA && !UNITY_EDITOR
            if (UnityEngine.WSA.Application.RunningOnUIThread())
#endif
			{
				return RequestAccessAndInitAsync(token);
			}
#if UNITY_WSA && !UNITY_EDITOR
            else
            {
                UnityEngine.WSA.Application.InvokeOnUIThread(() => RequestAccessAndInitAsync(token), waitUntilDone: true);
                return Task.CompletedTask;
            }
#endif
		}

		public void Signal(SignalData signal)
		{
			if (!string.IsNullOrEmpty(signal.type) && !string.IsNullOrEmpty(signal.sdp))
			{
				nativePeer.SetRemoteDescription(signal.type, signal.sdp);
				if (signal.type == "offer")
				{
					Debug.Log("CreateAnswer");
					Task.Run(() => nativePeer.CreateAnswer()).ConfigureAwait(false);
				}
			}
			else if (signal.candidate != null)
			{
				nativePeer.AddIceCandidate(signal.candidate.sdpMid, signal.candidate.sdpMLineIndex ?? 0, signal.candidate.candidate);
			}
			else
			{
				Debug.LogError($"Unrecognized signal type ${signal}");
			}
		}

		public void Uninitialize()
		{
			Debug.Log("RTCPeer Uninitialize");

			if ((nativePeer != null) && nativePeer.Initialized)
			{
				// Fire signals before doing anything else to allow listeners to clean-up,
				// including un-registering any callback and remove any track from the connection.
				OnShutdown?.Invoke();

				nativePeer.RenegotiationNeeded -= Peer_RenegotiationNeeded;
				nativePeer.I420RemoteVideoFrameReady -= I420RemoteVideoFrameReady;
				nativePeer.TrackAdded -= TrackAdded;
				nativePeer.TrackRemoved -= TrackRemoved;
				nativePeer.IceCandidateReadytoSend -= Peer_IceCandidateReadytoSend;
				nativePeer.LocalSdpReadytoSend -= Peer_LocalSdpReadytoSend;
				nativePeer.IceStateChanged -= Peer_IceStateChanged;
				nativePeer.DataChannelAdded -= Peer_DataChannelAdded;

				// Close the connection and release native resources.
				nativePeer.Close();
			}
		}

		private void Peer_LocalSdpReadytoSend(string type, string sdp)
		{
			SignalData signal = new SignalData
			{
				type = type,
				sdp = sdp
			};
			OnSignal?.Invoke(signal);
		}

		private void Peer_IceCandidateReadytoSend(string candidate, int sdpMlineindex, string sdpMid)
		{
			SignalData signal = new SignalData
			{
				candidate = new IceCandidate
				{
					candidate = candidate,
					sdpMLineIndex = sdpMlineindex,
					sdpMid = sdpMid
				}
			};
			OnSignal?.Invoke(signal);
		}

		internal void Update()
		{
			Action action;
			while (mainThreadWorkQueue.TryDequeue(out action))
			{
				action();
			}
		}

		private Task RequestAccessAndInitAsync(CancellationToken token)
		{
#if UNITY_WSA && !UNITY_EDITOR
            // On UWP the app must have the "webcam" capability, and the user must allow webcam
            // access. So check that access before trying to initialize the WebRTC library, as this
            // may result in a popup window being displayed the first time, which needs to be accepted
            // before the camera can be accessed by WebRTC.
            var mediaAccessRequester = new MediaCapture();
            var mediaSettings = new MediaCaptureInitializationSettings();
            mediaSettings.AudioDeviceId = "";
            mediaSettings.VideoDeviceId = "";
            mediaSettings.StreamingCaptureMode = StreamingCaptureMode.AudioAndVideo;
            mediaSettings.PhotoCaptureSource = PhotoCaptureSource.VideoPreview;
            mediaSettings.SharingMode = MediaCaptureSharingMode.SharedReadOnly; // for MRC and lower res camera
            var accessTask = mediaAccessRequester.InitializeAsync(mediaSettings).AsTask(token);
            return accessTask.ContinueWith(prevTask =>
            {
                token.ThrowIfCancellationRequested();

                if (prevTask.Exception == null)
                {
                    InitializePluginAsync(token);
                }
                else
                {
                    _mainThreadWorkQueue.Enqueue(() =>
                    {
                        OnError.Invoke($"Audio/Video access failure: {prevTask.Exception.Message}.");
                    });
                }
            }, token);
#else
			return InitializePluginAsync(token);
#endif
		}

		private Task InitializePluginAsync(CancellationToken token)
		{
			Debug.Log("Initializing WebRTC plugin...");
			var config = new PeerConnectionConfiguration()
			{
				// BundlePolicy = BundlePolicy.Balanced,
				// IceServers = new List<IceServer>() { new IceServer { Urls = new List<string>() { "stun.l.google.com:19302" } } }
			};

			nativePeer = new PeerConnection();
			return nativePeer.InitializeAsync(config, token).ContinueWith((initTask) =>
			{
				token.ThrowIfCancellationRequested();

				if (initTask.Exception != null)
				{
					mainThreadWorkQueue.Enqueue(() =>
					{
						var errorMessage = new StringBuilder();
						errorMessage.Append("WebRTC plugin initializing failed. See full log for exception details.\n");
						Exception ex = initTask.Exception;
						while (ex is AggregateException)
						{
							var ae = ex as AggregateException;
							errorMessage.Append($"AggregationException: {ae.Message}\n");
							ex = ae.InnerException;
						}
						errorMessage.Append($"Exception: {ex.Message}");
						OnError.Invoke(new Exception(errorMessage.ToString()));
					});
					throw initTask.Exception;
				}

				mainThreadWorkQueue.Enqueue(OnPostInitialize);
			}, token);
		}

		private void OnPostInitialize()
		{
			Debug.Log("WebRTC plugin initialized successfully.");

			nativePeer.RenegotiationNeeded += Peer_RenegotiationNeeded;
			nativePeer.I420RemoteVideoFrameReady += I420RemoteVideoFrameReady;
			nativePeer.TrackAdded += TrackAdded;
			nativePeer.TrackRemoved += TrackRemoved;
			nativePeer.LocalSdpReadytoSend += Peer_LocalSdpReadytoSend;
			nativePeer.IceCandidateReadytoSend += Peer_IceCandidateReadytoSend;
			nativePeer.IceStateChanged += Peer_IceStateChanged;
			nativePeer.DataChannelAdded += Peer_DataChannelAdded;
			OnInitialized?.Invoke();
		}

		private void Peer_DataChannelAdded(DataChannel channel)
		{
			Debug.Log($"Data channel added");
			channel.MessageReceived += Channel_MessageReceived;
		}

		private void Channel_MessageReceived(byte[] bytes)
		{
			string json = Encoding.UTF8.GetString(bytes);
			Debug.Log($"RPC RECV: {json}");
		}

		private void Peer_IceStateChanged(IceConnectionState newState)
		{
			Debug.Log($"Ice state changed: {newState}");
		}

		private void Peer_RenegotiationNeeded()
		{
			// If already connected, update the connection on the fly.
			// If not, wait for user action and don't automatically connect.
			if (nativePeer.IsConnected)
			{
				nativePeer.CreateOffer();
			}
		}
	}
}

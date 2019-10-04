// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

namespace AltspaceVR.WebProjector
{
	/// <summary>
	/// Maintains a connection to the `mre-multicast-service`'s `room-server` endpoint.
	/// </summary>
	public class RoomServer : MonoBehaviour
	{
		private const int ProtocolVersion = 1;

		private WebSocket ws;

		public string ServerUrl;
		public WebProjector WebProjector;

		public void Awake()
		{
		}

		public void Start()
		{
			CheckWebSocket();
		}

		public void Update()
		{
			if (ws != null)
			{
				ws.Update();
			}
		}

		public void OnDestroy()
		{
			if (ws != null)
			{
				ws.Close();
			}
		}

		public void JoinRoom()
		{
			Send(new JoinRoomMessage
			{
				roomId = WebProjector.RoomClient.RoomId,
			});
		}

		public void LeaveRoom()
		{
			Send(new LeaveRoomMessage
			{
				roomId = WebProjector.RoomClient.RoomId,
			});
		}

		public void Signal(SignalData signal)
		{
			Send(new PeerSignalMessage
			{
				signal = signal
			});
		}

		private void Send(RoomMessage roomMessage)
		{
			CheckWebSocket();
			try
			{
				ws.Send(roomMessage);
			}
			catch (Exception ex)
			{
				Debug.LogError(ex);
			}
		}

		private void Recv(string type, string json)
		{
			switch (type)
			{
				case "peer-signal":
					{
						PeerSignalMessage message = JsonConvert.DeserializeObject<PeerSignalMessage>(json);
						Debug.Log(JsonUtility.ToJson(message));
						WebProjector.RoomClient.Signal(message.signal);
						break;
					}

				default:
					{
						Debug.LogError($"Unrecognized room message: {type}");
						break;
					}
			}
		}

		private void Ws_OnConnected()
		{
			Debug.Log("RoomServer connected.");
		}

		private void Ws_OnMessage(string json)
		{
			try
			{
				RoomMessage message = JsonConvert.DeserializeObject<RoomMessage>(json);
				Recv(message.type, json);
			}
			catch (Exception ex)
			{
				Debug.LogError(ex);
			}
		}

		private void CheckWebSocket()
		{
			if (ws == null)
			{
				var headers = new Dictionary<string, string>() { { "x-room-protocol-version", $"{ProtocolVersion}" } };
				ws = new WebSocket(ServerUrl, headers);
				ws.OnMessage += Ws_OnMessage;
				ws.OnConnected += Ws_OnConnected;
				ws.StartConnecting();
			}
		}
	}

	[System.Serializable]
	public class IceCandidate
	{
		public string candidate;
		public int? sdpMLineIndex;
		public string sdpMid;
	}

	[System.Serializable]
	public class SignalData
	{
		public string type;
		public string sdp;
		public IceCandidate candidate;
	}

	[System.Serializable]
	public class RoomMessage
	{
		public RoomMessage(string type)
		{
			this.type = type;
		}

		public string type;
	}

	[System.Serializable]
	public class JoinRoomMessage : RoomMessage
	{
		public JoinRoomMessage() : base("join-room")
		{ }

		public string roomId;
	}

	[System.Serializable]
	public class LeaveRoomMessage : RoomMessage
	{
		public LeaveRoomMessage() : base("leave-room")
		{ }

		public string roomId;
	}

	[System.Serializable]
	public class PeerSignalMessage : RoomMessage
	{
		public PeerSignalMessage() : base("peer-signal")
		{ }

		public SignalData signal;
	}
}

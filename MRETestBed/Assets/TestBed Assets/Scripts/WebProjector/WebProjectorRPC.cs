using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AltspaceVR.WebProjector;
using MixedRealityExtension.App;
using MixedRealityExtension.Core.Interfaces;
using MixedRealityExtension.RPC;
using UnityEngine;


class VideoDimension
{
	public int Width;
	public int Height;
}

internal class WebProjectorRPC
{
	static private readonly Dictionary<string, VideoDimension> VideoDimensions = new Dictionary<string, VideoDimension>()
	{
		{ "1080p", new VideoDimension() { Width = 1920, Height = 1080 } },
		{ "720p", new VideoDimension() { Width = 1280, Height = 720 } },
		{ "480p", new VideoDimension() { Width = 853, Height = 480 } },
		{ "360p", new VideoDimension() { Width = 480, Height = 360 } },
	};

	const string WEBPROJECTOR = "webprojector";

	private IMixedRealityExtensionApp app;
	private RuntimeResources resources;
	private RPCInterface RPC;
	private WebProjector webProjector;
	private IUserInfo userInfo;
	private string videoQuality;
	private string title;

	public IUserInfo UserInfo => userInfo;

	public WebProjectorRPC(IMixedRealityExtensionApp app)
	{
		this.app = app;

		GameObject resourcesGO = GameObject.Find("RuntimeResources");
		if (resourcesGO != null)
		{
			resources = resourcesGO.GetComponent<RuntimeResources>();
		}

		if (resources != null)
		{
			app.OnUserJoined += app_OnUserJoined;
			app.OnUserLeft += app_OnUserLeft;

			RPC = new RPCInterface(app);
			app.RPCChannels.SetChannelHandler(WEBPROJECTOR, RPC);
			RPC.OnReceive("connect:v1", new RPCHandler<string>((roomId) => rpc_OnConnectToRoom(roomId)));
			RPC.OnReceive("disconnect:v1", new RPCHandler<string>((roomId) => rpc_OnDisconnectFromRoom(roomId)));
			RPC.OnReceive("spawn-mediaplayer:v1", new RPCHandler<string>((parentId) => rpc_OnSpawnMediaPlayer(parentId)));
			RPC.OnReceive("room-desc:v1", new RPCHandler<string, string, string>((roomId, videoQuality, title) => rpc_OnRoomDesc(roomId, videoQuality, title)));
		}
	}

	public void StartProjectingMyRoom()
	{
		if (userInfo != null)
		{
			RPC.SendRPC(WEBPROJECTOR, "connect:v1", userInfo.Id.ToString(), userInfo.InvariantId);
		}
	}

	public void StopProjectingMyRoom()
	{
		if (userInfo != null)
		{
			RPC.SendRPC(WEBPROJECTOR, "disconnect:v1", userInfo.Id.ToString(), userInfo.InvariantId);
		}
	}

	private void app_OnUserLeft(IUserInfo userInfo)
	{
		if (this.userInfo != null && this.userInfo.InvariantId == userInfo.InvariantId)
		{
			RPC.SendRPC(WEBPROJECTOR, "user-info:v1", userInfo.Id.ToString(), null);
			this.userInfo = null;
		}
	}

	private void app_OnUserJoined(IUserInfo userInfo)
	{
		RPC.SendRPC(WEBPROJECTOR, "user-info:v1", userInfo.Id.ToString(), userInfo.InvariantId);
		this.userInfo = userInfo;
	}

	private void rpc_OnConnectToRoom(string roomId)
	{
		if (webProjector.RoomClient != null)
		{
			webProjector.RoomClient.RoomId = roomId;
			webProjector.RoomClient.Play();
			webProjector.Controls.SetPowerButton(true);
		}
	}

	private void rpc_OnDisconnectFromRoom(string roomId)
	{
		if (webProjector.RoomClient != null && webProjector.RoomClient.RoomId == roomId)
		{
			webProjector.RoomClient.Stop();
			webProjector.Controls.SetPowerButton(false);
		}
	}

	private void rpc_OnSpawnMediaPlayer(string parentIdStr)
	{
		// WebProjector needs to detect mouse clicks.
		UnityStandardAssets.Characters.FirstPerson.FirstPersonController fpc = UnityEngine.Object.FindObjectOfType<UnityStandardAssets.Characters.FirstPerson.FirstPersonController>();
		if (fpc != null)
		{
			fpc.MouseLook.SetCursorLock(false);
		}

		Guid parentId = new Guid(parentIdStr);
		IActor parent = app.FindActor(parentId);
		if (parent != null)
		{
			GameObject projector = UnityEngine.Object.Instantiate(resources.WebProjectorPrefab, parent.GameObject.transform);
			webProjector = projector.GetComponent<WebProjector>();
			webProjector.RPC = this;
		}
	}

	private void rpc_OnRoomDesc(string roomId, string videoQuality, string title)
	{
		Debug.Log($"rpc_OnRoomDesc {roomId}, {videoQuality}, {title}");
		if (roomId == this.webProjector.RoomClient.RoomId)
		{
			this.videoQuality = videoQuality;
			this.title = title;
			UpdateLayout();
		}
	}

	private void UpdateLayout()
	{
		VideoDimension videoDimension;
		if (VideoDimensions.TryGetValue(videoQuality, out videoDimension))
		{
			Vector3 originalPosition = webProjector.Screen.transform.localPosition;
			float aspectRatio = videoDimension.Width / (float)videoDimension.Height;
			webProjector.Screen.transform.localScale = new Vector3(
				webProjector.Screen.transform.localScale.x,
				webProjector.Screen.transform.localScale.x * -1 / aspectRatio,
				1);
			webProjector.Screen.transform.localPosition = new Vector3(
				webProjector.Screen.transform.localScale.x / 2,
				originalPosition.y,
				originalPosition.z);
		}
	}
}

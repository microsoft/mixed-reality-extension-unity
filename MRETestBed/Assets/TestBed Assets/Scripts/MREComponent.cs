// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using Assets.Scripts.Behaviors;
using Assets.TestBed_Assets.Scripts.Player;
using MixedRealityExtension.API;
using MixedRealityExtension.App;
using MixedRealityExtension.Assets;
using MixedRealityExtension.Core;
using MixedRealityExtension.Core.Interfaces;
using MixedRealityExtension.Factories;
using MixedRealityExtension.PluginInterfaces;
using MixedRealityExtension.RPC;
using TMPro;
using UnityEngine;

class TestLogMessage
{
	public string Message { get; set; }

	public bool TestBoolean { get; set; }
}

public class MRELogger : IMRELogger
{
	public void LogDebug(string message)
	{
		Debug.Log(message);
	}

	public void LogError(string message)
	{
		Debug.LogError(message);
	}

	public void LogWarning(string message)
	{
		Debug.LogWarning(message);
	}
}

public class MREComponent : MonoBehaviour
{
	public delegate void AppEventHandler(MREComponent app);

	public string MREURL;

	public string SessionID;

	public string AppID;

	public string EphemeralAppID;

	[Serializable]
	public class UserProperty
	{
		public string Name;
		public string Value;
	}

	public UserProperty[] UserProperties;

	public bool AutoStart = false;

	public bool AutoJoin = true;

	[SerializeField]
	private Permissions GrantedPermissions;

	public Transform SceneRoot;

	public GameObject PlaceholderObject;

	public GameObject UserGameObject;

	public IMixedRealityExtensionApp MREApp { get; private set; }

	public event AppEventHandler OnConnecting;

	public event AppEventHandler OnConnected;

	public event AppEventHandler OnDisconnected;

	public event AppEventHandler OnAppStarted;

	public event AppEventHandler OnAppShutdown;

	private Guid _appId;

	private static bool _apiInitialized = false;

	[SerializeField]
	private TMP_FontAsset DefaultFont;

	[SerializeField]
	private TMP_FontAsset SerifFont;

	[SerializeField]
	private TMP_FontAsset SansSerifFont;

	[SerializeField]
	private TMP_FontAsset MonospaceFont;

	[SerializeField]
	private TMP_FontAsset CursiveFont;

	[SerializeField]
	private UnityEngine.Material DefaultPrimMaterial;

	[SerializeField]
	private DialogFactory DialogFactory;

	private Dictionary<Guid, HostAppUser> hostAppUsers = new Dictionary<Guid, HostAppUser>();

	void Start()
	{
		if (!_apiInitialized)
		{
			var assetCacheGo = new GameObject("MRE Asset Cache");
			var assetCache = assetCacheGo.AddComponent<AssetCache>();
			assetCache.CacheRootGO = new GameObject("Assets");
			assetCache.CacheRootGO.transform.SetParent(assetCacheGo.transform, false);
			assetCache.CacheRootGO.SetActive(false);

			MREAPI.InitializeAPI(
				defaultMaterial: DefaultPrimMaterial,
				layerApplicator: new SimpleLayerApplicator(0, 9, 10, 5),
				assetCache: assetCache,
				textFactory: new TmpTextFactory()
				{
					DefaultFont = DefaultFont,
					SerifFont = SerifFont,
					SansSerifFont = SansSerifFont,
					MonospaceFont = MonospaceFont,
					CursiveFont = CursiveFont
				},
				permissionManager: new SimplePermissionManager(GrantedPermissions),
				behaviorFactory: new BehaviorFactory(),
				dialogFactory: DialogFactory,
				libraryFactory: new ResourceFactory(),
				gltfImporterFactory: new VertexShadedGltfImporterFactory(),
				materialPatcher: new VertexMaterialPatcher(),
				logger: new MRELogger()
			);
			_apiInitialized = true;
		}

		MREApp = MREAPI.AppsAPI.CreateMixedRealityExtensionApp(this, EphemeralAppID, AppID);

		if (SceneRoot == null)
		{
			SceneRoot = transform;
		}

		MREApp.SceneRoot = SceneRoot.gameObject;

		if (AutoStart)
		{
			EnableApp();
		}

		MREApp.RPC.OnReceive("log", new RPCHandler<TestLogMessage>(
			(logMessage) => Debug.Log($"Log RPC of type {logMessage.GetType()} called with args [ {logMessage.Message}, {logMessage.TestBoolean} ]")
		));

		// Functional test commands
		MREApp.RPC.OnReceive("functional-test:test-started", new RPCHandler<string>((testName) =>
		{
			Debug.Log($"Test started: {testName}.");
		}));

		MREApp.RPC.OnReceive("functional-test:test-complete", new RPCHandler<string, bool>((testName, success) =>
		{
			Debug.Log($"Test complete: {testName}. Success: {success}.");
		}));

		MREApp.RPC.OnReceive("functional-test:close-connection", new RPCHandler(() =>
		{
			MREApp.Shutdown();
		}));

		MREApp.RPC.OnReceive("functional-test:trace-message", new RPCHandler<string, string>((testName, message) =>
		{
			Debug.Log($"{testName}: {message}");
		}));
	}

	private void MREApp_OnAppShutdown()
	{
		Debug.Log("AppShutdown");
		OnAppShutdown?.Invoke(this);
	}

	private void MREApp_OnAppStarted()
	{
		Debug.Log("AppStarted");
		OnAppStarted?.Invoke(this);

		if (AutoJoin)
		{
			UserJoin();
		}
	}

	private void MREApp_OnDisconnected()
	{
		Debug.Log("Disconnected");
		OnDisconnected?.Invoke(this);
	}

	private void MREApp_OnConnected()
	{
		Debug.Log("Connected");
		OnConnected?.Invoke(this);
	}

	private void MREApp_OnConnecting()
	{
		Debug.Log("Connecting");
		OnConnecting?.Invoke(this);
	}

	private void MREApp_OnConnectFailed(MixedRealityExtension.IPC.ConnectFailedReason reason)
	{
		Debug.Log($"ConnectFailed. reason: {reason}");
		if (reason == MixedRealityExtension.IPC.ConnectFailedReason.UnsupportedProtocol)
		{
			DisableApp();
		}
	}

	private void MRE_OnUserJoined(IUser user, bool isLocalUser)
	{
		Debug.Log($"User joined with host id: {user.HostAppUser.HostUserId} and mre user id: {user.Id}");
		hostAppUsers[user.Id] = (HostAppUser)user.HostAppUser;
	}

	private void MRE_OnUserLeft(IUser user, bool isLocalUser)
	{
		hostAppUsers.Remove(user.Id);
	}

	private void FixedUpdate()
	{
		MREApp?.FixedUpdate();
	}

	void Update()
	{
		if (Input.GetButtonUp("Jump"))
		{
			MREApp?.RPC.SendRPC("button-up", "space", false);
		}
	}

	void LateUpdate()
	{
		MREApp?.Update();
	}

	void OnApplicationQuit()
	{
		DisableApp();
	}

	public void ToggleApp()
	{
		if (MREApp.IsActive)
		{
			DisableApp();
		}
		else
		{
			EnableApp();
		}
	}

	public void EnableApp()
	{
		if (PlaceholderObject != null)
		{
			PlaceholderObject.gameObject.SetActive(false);
		}

		Debug.Log("Connecting to MRE App.");

		var args = System.Environment.GetCommandLineArgs();
		Uri overrideUri = null;
		try
		{
			overrideUri = new Uri(args[args.Length - 1], UriKind.Absolute);
		}
		catch { }

		var uri = overrideUri != null && overrideUri.Scheme.StartsWith("ws") ? overrideUri.AbsoluteUri : MREURL;
		try
		{
			MREApp.OnConnecting += MREApp_OnConnecting;
			MREApp.OnConnectFailed += MREApp_OnConnectFailed;
			MREApp.OnConnected += MREApp_OnConnected;
			MREApp.OnDisconnected += MREApp_OnDisconnected;
			MREApp.OnAppStarted += MREApp_OnAppStarted;
			MREApp.OnAppShutdown += MREApp_OnAppShutdown;
			MREApp.OnUserJoined += MRE_OnUserJoined;
			MREApp.OnUserLeft += MRE_OnUserLeft;
			MREApp?.Startup(uri, SessionID);
		}
		catch (Exception e)
		{
			Debug.Log($"Failed to connect to MRE App.  Exception thrown: {e.Message}\nStack trace: {e.StackTrace}");
		}
	}

	public void DisableApp()
	{
		MREApp?.Shutdown();
		MREApp.OnConnecting -= MREApp_OnConnecting;
		MREApp.OnConnectFailed -= MREApp_OnConnectFailed;
		MREApp.OnConnected -= MREApp_OnConnected;
		MREApp.OnDisconnected -= MREApp_OnDisconnected;
		MREApp.OnAppStarted -= MREApp_OnAppStarted;
		MREApp.OnAppShutdown -= MREApp_OnAppShutdown;
		MREApp.OnUserJoined -= MRE_OnUserJoined;
		MREApp.OnUserLeft -= MRE_OnUserLeft;

		if (PlaceholderObject != null)
		{
			PlaceholderObject.gameObject.SetActive(true);
		}
	}

	public void UserJoin()
	{
		var hostAppUser = new HostAppUser(LocalPlayer.PlayerId, $"TestBed User: {LocalPlayer.PlayerId}")
		{
			UserGO = UserGameObject
		};

		foreach (var kv in UserProperties)
		{
			hostAppUser.Properties[kv.Name] = kv.Value;
		}

		MREApp?.UserJoin(UserGameObject, hostAppUser, true);
	}

	public void UserLeave()
	{
		MREApp?.UserLeave(UserGameObject);
	}
}

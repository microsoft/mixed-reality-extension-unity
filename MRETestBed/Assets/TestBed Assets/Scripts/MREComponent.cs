// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using Assets.Scripts.Behaviors;
using MixedRealityExtension.API;
using MixedRealityExtension.App;
using MixedRealityExtension.Assets;
using MixedRealityExtension.Core.Interfaces;
using MixedRealityExtension.Factories;
using MixedRealityExtension.PluginInterfaces;
using MixedRealityExtension.RPC;
using Newtonsoft.Json.Linq;
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

	[Serializable]
	public class UserProperty
	{
		public string Name;
		public string Value;
	}

	public UserProperty[] UserProperties;

	public bool AutoStart = false;

	public bool AutoJoin = true;

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

	private static Dictionary<Guid, UserInfo> joinedUsers = new Dictionary<Guid, UserInfo>();

	internal static UserInfo GetUserInfo(Guid userId)
	{
		UserInfo result;
		if (joinedUsers.TryGetValue(userId, out result))
		{
			return result;
		}
		return null;
	}

	void Start()
	{
		if (!_apiInitialized)
		{
			MREAPI.InitializeAPI(
				defaultMaterial: DefaultPrimMaterial,
				layerApplicator: new SimpleLayerApplicator(0, 9, 10, 5),
				behaviorFactory: new BehaviorFactory(),
				textFactory: new TmpTextFactory()
				{
					DefaultFont = DefaultFont,
					SerifFont = SerifFont,
					SansSerifFont = SansSerifFont,
					MonospaceFont = MonospaceFont,
					CursiveFont = CursiveFont
				},
				libraryFactory: new ResourceFactory(),
				userInfoProvider: new UserInfoProvider(),
				dialogFactory: DialogFactory,
				logger: new MRELogger(),
				materialPatcher: new VertexMaterialPatcher(),
				gltfImporterFactory: new VertexShadedGltfImporterFactory()
			);
			_apiInitialized = true;
		}

		MREApp = MREAPI.AppsAPI.CreateMixedRealityExtensionApp(AppID, this);

		if (SceneRoot == null)
		{
			SceneRoot = transform;
		}

		MREApp.SceneRoot = SceneRoot.gameObject;

		MREApp.OnConnecting += MREApp_OnConnecting;
		MREApp.OnConnectFailed += MREApp_OnConnectFailed;
		MREApp.OnConnected += MREApp_OnConnected;
		MREApp.OnDisconnected += MREApp_OnDisconnected;
		MREApp.OnAppStarted += MREApp_OnAppStarted;
		MREApp.OnAppShutdown += MREApp_OnAppShutdown;

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

		try
		{
			MREApp?.Startup(MREURL, SessionID, "MRETestBed");
		}
		catch (Exception e)
		{
			Debug.Log($"Failed to connect to MRE App.  Exception thrown: {e.Message}\nStack trace: {e.StackTrace}");
		}
	}

	public void DisableApp()
	{
		MREApp?.Shutdown();

		if (PlaceholderObject != null)
		{
			PlaceholderObject.gameObject.SetActive(true);
		}
	}

	public void UserJoin()
	{
		var rng = new System.Random();
		string invariantId = rng.Next().ToString("X8");
		string source = $"{invariantId}-{AppID}-{SessionID}-{gameObject.GetInstanceID()}";
		Guid userId = UtilMethods.StringToGuid(source);
		UserInfo userInfo = new UserInfo(userId, "TestBed User", invariantId)
		{
			UserGO = UserGameObject
		};

		foreach (var kv in UserProperties)
		{
			userInfo.Properties[kv.Name] = kv.Value;
		}

		joinedUsers[userInfo.Id] = userInfo;
		MREApp?.UserJoin(UserGameObject, userInfo);
	}

	public void UserLeave()
	{
		MREApp?.UserLeave(UserGameObject);
	}
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using Assets.Scripts.Behaviors;
using MixedRealityExtension.API;
using MixedRealityExtension.App;
using MixedRealityExtension.Assets;
using MixedRealityExtension.Factories;
using MixedRealityExtension.PluginInterfaces;
using MixedRealityExtension.RPC;
using Newtonsoft.Json.Linq;
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

    private const string UserId = "ReadyPlayerOne";

    private Guid _appId;

    private static bool _apiInitialized = false;

    [SerializeField]
    private Font SerifFont;

    [SerializeField]
    private Font SansSerifFont;

    [SerializeField]
    private Material DefaultPrimMaterial;

    void Start()
    {
        if (!_apiInitialized)
        {
            MREAPI.InitializeAPI(
                behaviorFactory: new BehaviorFactory(),
                textFactory: new MWTextFactory(SerifFont, SansSerifFont),
                primitiveFactory: new MWPrimitiveFactory(DefaultPrimMaterial),
                libraryFactory: new ResourceFactory(),
                assetCache: new AssetCache(new GameObject("MRE Asset Cache")),
                logger: new MRELogger());
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

        // Below is work in progress: building up an informative platformId string.
        /*
        JObject j = new JObject
        {
            ["host"] = new JObject
            {
                ["name"] = "mre-testbed",
                ["version"] = "1.0"
            },
            ["system"] = new JObject
            {
                ["deviceModel"] = SystemInfo.deviceModel,
                ["operatingSystem"] = SystemInfo.operatingSystem
            }
        };
        */

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
        string userIdSource = $"{UserId}-{AppID}-{gameObject.GetInstanceID()}";
        Guid userId = UtilMethods.StringToGuid(userIdSource);
        UserInfo userInfo = new UserInfo()
        {
            UserId = userId,
            UserGO = UserGameObject
        };
        MREApp?.UserJoin(UserGameObject, userInfo);
    }

    public void UserLeave()
    {
        MREApp?.UserLeave(UserGameObject);
    }
}

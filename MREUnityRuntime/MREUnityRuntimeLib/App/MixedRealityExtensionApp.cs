// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using MixedRealityExtension.Animation;
using MixedRealityExtension.API;
using MixedRealityExtension.Assets;
using MixedRealityExtension.Core;
using MixedRealityExtension.Core.Components;
using MixedRealityExtension.Core.Interfaces;
using MixedRealityExtension.IPC;
using MixedRealityExtension.IPC.Connections;
using MixedRealityExtension.Messaging;
using MixedRealityExtension.Messaging.Commands;
using MixedRealityExtension.Messaging.Events;
using MixedRealityExtension.Messaging.Events.Types;
using MixedRealityExtension.Messaging.Payloads;
using MixedRealityExtension.Messaging.Protocols;
using MixedRealityExtension.Patching.Types;
using MixedRealityExtension.PluginInterfaces;
using MixedRealityExtension.RPC;
using MixedRealityExtension.Util;
using MixedRealityExtension.Util.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using Trace = MixedRealityExtension.Messaging.Trace;
using Regex = System.Text.RegularExpressions.Regex;
using System.Text;
using System.Security.Cryptography;
using System.Threading;

namespace MixedRealityExtension.App
{
	internal sealed class MixedRealityExtensionApp : IMixedRealityExtensionApp, ICommandHandlerContext
	{
		private readonly AssetLoader _assetLoader;
		private readonly UserManager _userManager;
		private readonly ActorManager _actorManager;
		private readonly CommandManager _commandManager;
		internal readonly AnimationManager AnimationManager;
		private readonly AssetManager _assetManager;
		private readonly MonoBehaviour _ownerScript;

		private IConnectionInternal _conn;

		private ISet<Guid> _interactingUserIds = new HashSet<Guid>();
		private IList<Action> _executionProtocolActionQueue = new List<Action>();
		private IList<GameObject> _ownedGameObjects = new List<GameObject>();

		// If physics simulation time step is larger than specified value, physics update will be sent with
		// the same time step. If smaller, physics update will be send with closest smaller multiple time step.
		// For example if update time-step is 0.33, and if simulation time step is 40ms then update step is also 40ms,
		// or if simulation step is 16ms then update step is 32ms.
		private float _physicsUpdateTimestep = 0.034f;

		private float _timeSinceLastPhysicsUpdate = 0.0f;
		private bool _shouldSendPhysicsUpdate = false;

		private enum AppState
		{
			Stopped,
			/// <summary>
			/// Startup has been called, but we might be waiting for permission to run.
			/// </summary>
			WaitingForPermission,
			Starting,
			Running
		}

		private AppState _appState = AppState.Stopped;
		private int generation = 0;

		public IMRELogger Logger { get; private set; }

		private CancellationTokenSource permissionRequestCancelSource;

		#region Events - Public

		/// <inheritdoc />
		public event MWEventHandler OnWaitingForPermission;

		/// <inheritdoc />
		public event MWEventHandler OnPermissionDenied;

		/// <inheritdoc />
		public event MWEventHandler OnConnecting;

		/// <inheritdoc />
		public event MWEventHandler<ConnectFailedReason> OnConnectFailed;

		/// <inheritdoc />
		public event MWEventHandler OnConnected;

		/// <inheritdoc />
		public event MWEventHandler OnDisconnected;

		/// <inheritdoc />
		public event MWEventHandler OnAppStarted;

		/// <inheritdoc />
		public event MWEventHandler OnAppShutdown;

		/// <inheritdoc />
		public event MWEventHandler<IActor> OnActorCreated
		{
			add { _actorManager.OnActorCreated += value; }
			remove { _actorManager.OnActorCreated -= value; }
		}

		/// <inheritdoc />
		public event MWEventHandler<IUser, bool> OnUserJoined;

		/// <inheritdoc />
		public event MWEventHandler<IUser, bool> OnUserLeft;

		#endregion

		#region Properties - Public

		/// <inheritdoc />
		public string GlobalAppId { get; }

		public string EphemeralAppId { get; }

		/// <inheritdoc />
		public string SessionId { get; private set; }

		/// <inheritdoc />
		public bool IsActive => _conn?.IsActive ?? false;

		/// <inheritdoc />
		public Uri ServerUri { get; private set; }

		/// <summary>
		/// Same as ServerUri, but with ws(s): substituted for http(s):
		/// </summary>
		public Uri ServerAssetUri { get; private set; }

		/// <inheritdoc />
		public GameObject SceneRoot { get; set; }

		/// <inheritdoc />
		public IUser LocalUser { get; private set; }

		/// <inheritdoc />
		public RPCInterface RPC { get; }

		/// <inheritdoc />
		public RPCChannelInterface RPCChannels { get; }

		public AssetManager AssetManager => _assetManager;

		#endregion

		#region Properties - Internal

		internal MWEventManager EventManager { get; }

		internal Guid InstanceId { get; set; }

		internal OperatingModel OperatingModel { get; set; }

		internal bool IsAuthoritativePeer { get; set; }

		internal IProtocol Protocol { get; set; }

		internal IConnectionInternal Conn => _conn;

		internal SoundManager SoundManager { get; private set; }

		internal AssetLoader AssetLoader => _assetLoader;

		internal Permissions GrantedPermissions = Permissions.None;

		internal PhysicsBridge PhysicsBridge { get; } = null;

		internal bool UsePhysicsBridge => PhysicsBridge != null;

		#endregion

		/// <summary>
		/// Initializes a new instance of the class <see cref="MixedRealityExtensionApp"/>
		/// </summary>
		/// <param name="globalAppId">A string uniquely identifying the MRE behind the server URL. Used for generating
		/// consistent user IDs when user tracking is enabled.</param>
		/// <param name="ephemeralAppId">A string uniquely identifying the MRE instance in the shared space across
		/// all clients. Used for generating user IDs when user tracking is disabled.</param>
		/// <param name="ownerScript">The owner mono behaviour script for the app.</param>
		internal MixedRealityExtensionApp(string globalAppId, string ephemeralAppId, MonoBehaviour ownerScript, IMRELogger logger = null)
		{
			GlobalAppId = globalAppId;
			EphemeralAppId = ephemeralAppId;
			_ownerScript = ownerScript;
			EventManager = new MWEventManager(this);
			_assetLoader = new AssetLoader(ownerScript, this);
			_userManager = new UserManager(this);
			_actorManager = new ActorManager(this);

			if (Constants.UsePhysicsBridge)
			{
				PhysicsBridge = new PhysicsBridge();
			}

			SoundManager = new SoundManager(this);
			AnimationManager = new AnimationManager(this);
			_commandManager = new CommandManager(new Dictionary<Type, ICommandHandlerContext>()
			{
				{ typeof(MixedRealityExtensionApp), this },
				{ typeof(Actor), null },
				{ typeof(AssetLoader), _assetLoader },
				{ typeof(ActorManager), _actorManager },
				{ typeof(AnimationManager), AnimationManager }
			});

			var cacheRoot = new GameObject("MRE Cache");
			cacheRoot.transform.SetParent(_ownerScript.gameObject.transform);
			cacheRoot.SetActive(false);
			_assetManager = new AssetManager(this, cacheRoot);

			RPC = new RPCInterface(this);
			RPCChannels = new RPCChannelInterface();
			// RPC messages without a ChannelName will route to the "global" RPC handlers.
			RPCChannels.SetChannelHandler(null, RPC);
#if ANDROID_DEBUG
			Logger = logger ?? new UnityLogger(this);
#else
			Logger = logger ?? new ConsoleLogger(this);
#endif
		}

		/// <inheritdoc />
		public async void Startup(string url, string sessionId)
		{
			if (_appState != AppState.Stopped)
			{
				Shutdown();
			}

			ServerUri = new Uri(url, UriKind.Absolute);
			ServerAssetUri = new Uri(Regex.Replace(ServerUri.AbsoluteUri, "^ws(s?):", "http$1:"));
			SessionId = sessionId;

			_appState = AppState.WaitingForPermission;
			OnWaitingForPermission?.Invoke();

			// download manifest
			var manifestUri = new Uri(ServerAssetUri, "./manifest.json");
			AppManifest manifest;
			try
			{
				manifest = await AppManifest.DownloadManifest(manifestUri);
			}
			catch (Exception e)
			{
				Debug.LogErrorFormat("Error downloading MRE manifest \"{0}\":\n{1}", manifestUri, e.ToString());
				manifest = new AppManifest()
				{
					Permissions = new Permissions[] { Permissions.UserTracking, Permissions.UserInteraction }
				};
			}

			var neededFlags = Permissions.Execution | (manifest.Permissions?.ToFlags() ?? Permissions.None);
			var wantedFlags = manifest.OptionalPermissions?.ToFlags() ?? Permissions.None;

			// set up cancel source
			if (permissionRequestCancelSource != null)
			{
				permissionRequestCancelSource.Cancel();
			}
			permissionRequestCancelSource = new CancellationTokenSource();

			// get permission to run from host app
			var grantedPerms = await MREAPI.AppsAPI.PermissionManager.PromptForPermissions(
				appLocation: ServerUri,
				permissionsNeeded: new HashSet<Permissions>(manifest.Permissions ?? new Permissions[0]) { Permissions.Execution },
				permissionsWanted: manifest.OptionalPermissions,
				permissionFlagsNeeded: neededFlags,
				permissionFlagsWanted: wantedFlags,
				appManifest: manifest,
				cancellationToken: permissionRequestCancelSource.Token);

			// clear cancel source once we don't need it anymore
			permissionRequestCancelSource = null;

			// only use permissions that are requested, even if the user offers more
			GrantedPermissions = grantedPerms & (neededFlags | wantedFlags);

			MREAPI.AppsAPI.PermissionManager.OnPermissionDecisionsChanged += OnPermissionsUpdated;

			// make sure all needed perms are granted
			if (!GrantedPermissions.HasFlag(neededFlags))
			{
				OnPermissionDenied?.Invoke();
				Shutdown(reactivateOnPermissions: true);
				return;
			}

			_appState = AppState.Starting;

			var connection = new WebSocket();
			connection.Url = url;
			connection.Headers.Add(Constants.SessionHeader, SessionId);
			connection.Headers.Add(Constants.LegacyProtocolVersionHeader, $"{Constants.LegacyProtocolVersion}");
			connection.Headers.Add(Constants.CurrentClientVersionHeader, Constants.CurrentClientVersion);
			connection.Headers.Add(Constants.MinimumSupportedSDKVersionHeader, Constants.MinimumSupportedSDKVersion);
			connection.OnConnecting += Conn_OnConnecting;
			connection.OnConnectFailed += Conn_OnConnectFailed;
			connection.OnConnected += Conn_OnConnected;
			connection.OnDisconnected += Conn_OnDisconnected;
			connection.OnError += Connection_OnError;
			_conn = connection;
			_conn.Open();
		}

		private void OnPermissionsUpdated(Uri updatedUrl, Permissions oldPermissions, Permissions newPermissions)
		{
			// updated URI matches protocol, hostname, and port, and if it has a path, that matches too
			if (updatedUrl.Scheme == ServerUri.Scheme && updatedUrl.Authority == ServerUri.Authority
				&& (updatedUrl.AbsolutePath == "/" || updatedUrl.AbsolutePath == ServerUri.AbsolutePath)
				&& _appState != AppState.Stopped)
			{
				Startup(ServerUri.ToString(), SessionId);
			}
		}

		/// <inheritdoc />
		private void Disconnect()
		{
			try
			{
				if (Protocol != null)
				{
					Protocol.Stop();
					Protocol = new Idle(this);
				}

				if (_conn != null)
				{
					_conn.OnConnecting -= Conn_OnConnecting;
					_conn.OnConnectFailed -= Conn_OnConnectFailed;
					_conn.OnConnected -= Conn_OnConnected;
					_conn.OnDisconnected -= Conn_OnDisconnected;
					_conn.OnError -= Connection_OnError;
					_conn.Dispose();
				}
			}
			catch { }
			finally
			{
				_conn = null;
			}
		}

		/// <inheritdoc />
		public void Shutdown()
		{
			Shutdown(false);
		}

		private void Shutdown(bool reactivateOnPermissions)
		{
			Disconnect();
			FreeResources();

			if (!reactivateOnPermissions)
			{
				MREAPI.AppsAPI.PermissionManager.OnPermissionDecisionsChanged -= OnPermissionsUpdated;
				if (permissionRequestCancelSource != null)
				{
					permissionRequestCancelSource.Cancel();
					permissionRequestCancelSource = null;
				}
			}

			if (_appState != AppState.Stopped)
			{
				_appState = reactivateOnPermissions ? AppState.WaitingForPermission : AppState.Stopped;
				OnAppShutdown?.Invoke();
			}
		}

		private void FreeResources()
		{
			foreach (GameObject go in _ownedGameObjects)
			{
				UnityEngine.Object.Destroy(go);
			}

			_ownedGameObjects.Clear();
			_actorManager.Reset();
			AnimationManager.Reset();
			PhysicsBridge.Reset();

			foreach (Guid id in _assetLoader.ActiveContainers)
			{
				AssetManager.Unload(id);
			}
			_assetLoader.ActiveContainers.Clear();
		}

		/// <inheritdoc />
		public void FixedUpdate()
		{
			if (_appState == AppState.Stopped)
			{
				return;
			}

			if (UsePhysicsBridge)
			{
				if (_shouldSendPhysicsUpdate)
				{
					// Sending snapshot which represents the state before physics step is done,
					// hence we are using time stamp from the start of the fixed update.
					SendPhysicsUpdate(Time.fixedTime);
				}

				PhysicsBridge.FixedUpdate(SceneRoot.transform);

				// add delta for next step
				_timeSinceLastPhysicsUpdate += Time.fixedDeltaTime;

				float updateDeltaTime = Math.Max(Time.fixedDeltaTime,
					Time.fixedDeltaTime * (float)Math.Floor(_physicsUpdateTimestep / Time.fixedDeltaTime));

				if (updateDeltaTime - _timeSinceLastPhysicsUpdate < 0.001f)
				{
					_shouldSendPhysicsUpdate = true;
				}
			}
		}

		private void SendPhysicsUpdate(float timestamp)
		{
			if (LocalUser == null)
			{
				return;
			}

			PhysicsBridgePatch physicsPatch = new PhysicsBridgePatch(LocalUser.Id,
					PhysicsBridge.GenerateSnapshot(timestamp, SceneRoot.transform));
			// send only updates if there are any, to save band with
			// in order to produce any updates for settled bodies this should be handled within the physics bridge
			if (physicsPatch.DoSendThisPatch())
			{
				EventManager.QueueEvent(new PhysicsBridgeUpdated(InstanceId, physicsPatch));
			}
			
			//low frequency server upload transform stream
			{
				float systemTime = UnityEngine.Time.time;
				if (PhysicsBridge.shouldSendLowFrequencyTransformUpload(systemTime))
				{
					PhysicsTranformServerUploadPatch serverUploadPatch =
						PhysicsBridge.GenerateServerTransformUploadPatch(InstanceId, systemTime);
					// upload only if there is a real difference in the transforms
					if (serverUploadPatch.IsPatched())
					{
						EventManager.QueueEvent(new PhysicsTranformServerUploadUpdated(InstanceId, serverUploadPatch));
					}
				}
			}

			_shouldSendPhysicsUpdate = false;
			_timeSinceLastPhysicsUpdate = 0.0f;

		}

		/// <inheritdoc />
		public void Update()
		{
			// Process events then we will update the connection.
			EventManager.ProcessEvents();
			EventManager.ProcessLateEvents();

			if (_conn != null)
			{
				// Read and process or queue incoming messages.
				_conn.Update();
			}
			// Process actor queues after connection update.
			_actorManager.Update();

			if (UsePhysicsBridge)
			{
				if (_shouldSendPhysicsUpdate)
				{
					// Sending snapshot which represents the state after physics step is done,
					// hence we need to add time step to the time stamp from the start of the fixed update.
					SendPhysicsUpdate(Time.fixedTime + Time.fixedDeltaTime);
				}
			}

			SoundManager.Update();
			_commandManager.Update();
			AnimationManager.Update();
		}

		/// <inheritdoc />
		public void UserJoin(GameObject userGO, IHostAppUser hostAppUser, bool isLocalUser)
		{
			void PerformUserJoin()
			{
				// only join the user if required
				if (isLocalUser
					&& !GrantedPermissions.HasFlag(Permissions.UserInteraction)
					&& !GrantedPermissions.HasFlag(Permissions.UserTracking))
				{
					return;
				}

				var user = userGO.GetComponents<User>()
					.FirstOrDefault(_user => _user.AppInstanceId == this.InstanceId);

				if (user == null)
				{
					user = userGO.AddComponent<User>();

					// Generate the obfuscated user ID based on user tracking permission.
					Guid instancedUserId = GenerateObfuscatedUserId(hostAppUser, EphemeralAppId);
					Guid userId = instancedUserId;
					if ((!isLocalUser || GrantedPermissions.HasFlag(Permissions.UserTracking)) && !string.IsNullOrEmpty(GlobalAppId))
						userId = GenerateObfuscatedUserId(hostAppUser, GlobalAppId);

					user.Initialize(hostAppUser, userId, instancedUserId, this);
				}

				// TODO @tombu - Wait for the app to send back a success for join?
				_userManager.AddUser(user);

				if (isLocalUser)
				{
					Protocol.Send(new UserJoined()
					{
						User = new UserPatch(user)
					});

					LocalUser = user;

					PhysicsBridge.LocalUserId = LocalUser.Id;

					// Enable interactions for the user if given the UserInteraction permission.
					if (GrantedPermissions.HasFlag(Permissions.UserInteraction))
					{
						EnableUserInteraction(user);
					}
				}

				try
				{
					OnUserJoined?.Invoke(user, isLocalUser);
				}
				catch (Exception e)
				{
					Debug.LogException(e);
				}
			}

			if (Protocol is Execution)
			{
				PerformUserJoin();
			}
			else
			{
				_executionProtocolActionQueue.Add(() => PerformUserJoin());
			}
		}

		/// <inheritdoc />
		public void UserLeave(GameObject userGO)
		{
			var user = userGO.GetComponents<User>()
				.FirstOrDefault(_user => _user.AppInstanceId == this.InstanceId);

			if (user != null)
			{
				if (IsInteractableForUser(user))
				{
					DisableUserInteration(user);
				}

				_userManager.RemoveUser(user);
				_interactingUserIds.Remove(user.Id);

				var isLocalUser = (IUser)user == LocalUser;
				if (isLocalUser)
				{
					LocalUser = null;
					PhysicsBridge.LocalUserId = null;

					if (Protocol is Execution)
					{
						Protocol.Send(new UserLeft() { UserId = user.Id });
					}
				}

				try
				{
					OnUserLeft?.Invoke(user, isLocalUser);
				}
				catch (Exception e)
				{
					Debug.LogException(e);
				}
			}
		}

		/// <inheritdoc />
		public bool IsInteractableForUser(IUser user) => _interactingUserIds.Contains(user.Id);

		/// <inheritdoc />
		public IActor FindActor(Guid id)
		{
			return _actorManager.FindActor(id);
		}

		public IEnumerable<Actor> FindChildren(Guid id)
		{
			return _actorManager.FindChildren(id);
		}

		/// <inheritdoc />
		public void OnActorDestroyed(Guid actorId)
		{
			if (_actorManager.OnActorDestroy(actorId))
			{
				Protocol.Send(new DestroyActors()
				{
					ActorIds = new Guid[] { actorId }
				});
			}
		}

		public IUser FindUser(Guid id)
		{
			return _userManager.FindUser(id);
		}

		public void UpdateServerTimeOffset(long currentServerTime)
		{
			AnimationManager.UpdateServerTimeOffset(currentServerTime);
		}

		private HashSet<string> usedPreallocSeeds = new HashSet<string>();
		/// <inheritdoc />
		public void DeclarePreallocatedActors(GameObject[] objects, string guidSeed)
		{
			if (!(Protocol is Execution))
			{
				throw new Exception($"Preallocated actors can only be declared after the app is Started");
			}

			// guarantee a given seed is only used once per session
			if (usedPreallocSeeds.Contains(guidSeed))
			{
				throw new ArgumentOutOfRangeException(nameof(guidSeed),
					$"Preallocated actor seed [{guidSeed}] has already been used this session, choose a different one!");
			}
			usedPreallocSeeds.Add(guidSeed);

			var goIds = new HashSet<int>(objects.Select(go => go.GetInstanceID()));
			var rootGos = GetDistinctTreeRoots(objects);

			// add actor components to all gos beneath a tagged go
			var taggedActors = new List<Actor>(objects.Length);
			foreach (var root in rootGos)
			{
				addActors(root);
			}

			ProcessCreatedActors(null, taggedActors, null, guidSeed);

			void addActors(GameObject go)
			{
				var oldActor = go.GetComponent<Actor>();
				if (oldActor != null)
				{
					MREAPI.Logger.LogError($"GameObject {go.name} is already in use by another MRE session, skipping.");
					return;
				}

				var actor = go.AddComponent<Actor>();
				if (goIds.Contains(go.GetInstanceID()))
				{
					taggedActors.Add(actor);
				}

				foreach (Transform child in go.transform)
				{
					addActors(child.gameObject);
				}
			}
		}

		#region Methods - Internal

		internal void OnReceive(Message message)
		{
			if (message.Payload is NetworkCommandPayload ncp)
			{
				ncp.MessageId = message.Id;
				_commandManager.ExecuteCommandPayload(ncp, null);
			}
			else
			{
				throw new Exception("Unexpected message.");
			}
		}

		internal void SynchronizeUser(UserPatch userPatch)
		{
			if (userPatch.IsPatched())
			{
				var payload = new UserUpdate() { User = userPatch };
				EventManager.QueueLateEvent(new UserEvent(userPatch.Id, payload));
			}
		}

		internal void ExecuteCommandPayload(ICommandPayload commandPayload, Action onCompleteCallback)
		{
			ExecuteCommandPayload(this, commandPayload, onCompleteCallback);
		}

		internal void ExecuteCommandPayload(ICommandHandlerContext handlerContext, ICommandPayload commandPayload, Action onCompleteCallback)
		{
			_commandManager.ExecuteCommandPayload(handlerContext, commandPayload, onCompleteCallback);
		}

		/// <summary>
		/// Used to set actor parents when the parent is pending
		/// </summary>
		internal void ProcessActorCommand(Guid actorId, NetworkCommandPayload payload, Action onCompleteCallback)
		{
			_actorManager.ProcessActorCommand(actorId, payload, onCompleteCallback);
		}

		internal bool OwnsActor(IActor actor)
		{
			return FindActor(actor.Id) != null;
		}

		internal void EnableUserInteraction(IUser user)
		{
			if (_userManager.HasUser(user.Id))
			{
				_interactingUserIds.Add(user.Id);
			}
			else
			{
				throw new Exception("Enabling interaction on this app for a user that has not joined the app.");
			}
		}

		/// <inheritdoc />
		internal void DisableUserInteration(IUser user)
		{
			_interactingUserIds.Remove(user.Id);
		}

		internal bool InteractionEnabled() => _interactingUserIds.Count != 0;

		#endregion

		#region Methods - Private

		private void Conn_OnConnecting()
		{
			OnConnecting?.Invoke();
		}

		private void Conn_OnConnectFailed(ConnectFailedReason reason)
		{
			OnConnectFailed?.Invoke(reason);
		}

		private void Conn_OnConnected()
		{
			OnConnected?.Invoke();

			if (_appState != AppState.Stopped)
			{
				IsAuthoritativePeer = false;

				var handshake = new Messaging.Protocols.Handshake(this);
				handshake.OnComplete += Handshake_OnComplete;
				handshake.OnReceive += OnReceive;
				handshake.OnOperatingModel += Handshake_OnOperatingModel;
				Protocol = handshake;
				handshake.Start();
			}
		}

		private void Conn_OnDisconnected()
		{
			generation++;
			if (Protocol != null)
			{
				Protocol.Stop();
				Protocol = new Idle(this);
			}

			FreeResources();

			this.OnDisconnected?.Invoke();
		}

		private void Connection_OnError(Exception ex)
		{
			Logger.LogError($"Exception: {ex.Message}\nStack Trace: {ex.StackTrace}");
		}

		private void Handshake_OnOperatingModel(OperatingModel operatingModel)
		{
			this.OperatingModel = operatingModel;
		}

		private void Handshake_OnComplete()
		{
			if (_appState != AppState.Stopped)
			{
				var sync = new Messaging.Protocols.Sync(this);
				sync.OnComplete += Sync_OnComplete;
				sync.OnReceive += OnReceive;
				Protocol = sync;
				sync.Start();
			}
		}

		private void Sync_OnComplete()
		{
			if (_appState != AppState.Stopped)
			{
				var execution = new Messaging.Protocols.Execution(this);
				execution.OnReceive += OnReceive;
				Protocol = execution;
				execution.Start();

				foreach (var action in _executionProtocolActionQueue)
				{
					action();
				}

				_appState = AppState.Running;
				OnAppStarted?.Invoke();
			}
		}

		private GameObject[] GetDistinctTreeRoots(GameObject[] gos)
		{
			// identify game objects whose ancestors are not also flagged to be actors
			var goIds = new HashSet<int>(gos.Select(go => go.GetInstanceID()));
			var rootGos = new List<GameObject>(gos.Length);
			foreach (var go in gos)
			{
				if (!ancestorInList(go))
				{
					rootGos.Add(go);
				}
			}

			return rootGos.ToArray();

			bool ancestorInList(GameObject go)
			{
				return go != null && go.transform.parent != null && (
					goIds.Contains(go.transform.parent.gameObject.GetInstanceID()) ||
					ancestorInList(go.transform.parent.gameObject));
			}
		}

		private Guid GenerateObfuscatedUserId(IHostAppUser hostAppUser, string salt)
		{
			using (SHA256 hasher = SHA256.Create())
			{
				string hashString = $"{hostAppUser.HostUserId}:{salt}";

				var encoder = new UTF8Encoding();
				var hashedId = Convert.ToBase64String(
					hasher.ComputeHash(encoder.GetBytes(hashString))
				);

				return UtilMethods.StringToGuid(hashedId);
			}
		}

		#endregion

		#region Command Handlers

		[CommandHandler(typeof(AppToEngineRPC))]
		private void OnRPCReceived(AppToEngineRPC payload, Action onCompleteCallback)
		{
			RPCChannels.ReceiveRPC(payload);
			onCompleteCallback?.Invoke();
		}

		[CommandHandler(typeof(UserUpdate))]
		private void OnUserUpdate(UserUpdate payload, Action onCompleteCallback)
		{
			try
			{
				((User)LocalUser).SynchronizeEngine(payload.User);
				_actorManager.UpdateAllVisibility();
				onCompleteCallback?.Invoke();
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}
		}

		[CommandHandler(typeof(CreateFromLibrary))]
		private async void OnCreateFromLibrary(CreateFromLibrary payload, Action onCompleteCallback)
		{
			try
			{
				var actors = await _assetLoader.CreateFromLibrary(payload.ResourceId, payload.Actor?.ParentId);
				ProcessCreatedActors(payload, actors, onCompleteCallback);
			}
			catch (Exception e)
			{
				SendCreateActorResponse(payload, failureMessage: e.ToString(), onCompleteCallback: onCompleteCallback);
				Debug.LogException(e);
			}
		}

		[CommandHandler(typeof(CreateEmpty))]
		private void OnCreateEmpty(CreateEmpty payload, Action onCompleteCallback)
		{
			try
			{
				var actors = _assetLoader.CreateEmpty(payload.Actor?.ParentId);
				ProcessCreatedActors(payload, actors, onCompleteCallback);
			}
			catch (Exception e)
			{
				SendCreateActorResponse(payload, failureMessage: e.ToString(), onCompleteCallback: onCompleteCallback);
				Debug.LogException(e);
			}
		}

		[CommandHandler(typeof(CreateFromPrefab))]
		private void OnCreateFromPrefab(CreateFromPrefab payload, Action onCompleteCallback)
		{
			try
			{
				var curGeneration = generation;
				AssetManager.OnSet(payload.PrefabId, prefab =>
				{
					if (this == null || _conn == null || !_conn.IsActive || generation != curGeneration) return;
					if (prefab.Asset != null)
					{
						var createdActors = _assetLoader.CreateFromPrefab(payload.PrefabId, payload.Actor?.ParentId, payload.CollisionLayer);
						ProcessCreatedActors(payload, createdActors, onCompleteCallback);
					}
					else
					{
						var message = $"Prefab {payload.PrefabId} failed to load, canceling actor creation";
						SendCreateActorResponse(payload, failureMessage: message, onCompleteCallback: onCompleteCallback);
					}
				});
			}
			catch (Exception e)
			{
				SendCreateActorResponse(payload, failureMessage: e.ToString(), onCompleteCallback: onCompleteCallback);
				Debug.LogException(e);
			}
		}

		private void ProcessCreatedActors(CreateActor originalMessage, IList<Actor> createdActors, Action onCompleteCallback, string guidSeed = null)
		{
			Guid guidGenSeed;
			if (originalMessage != null)
			{
				guidGenSeed = originalMessage.Actor.Id;
			}
			else
			{
				guidGenSeed = UtilMethods.StringToGuid(guidSeed);
			}
			var guids = new DeterministicGuids(guidGenSeed);

			// find the actors with no actor parents
			var rootActors = GetDistinctTreeRoots(
				createdActors.Select(a => a.gameObject).ToArray()
			).Select(go => go.GetComponent<Actor>()).ToArray();

			var rootActor = createdActors.FirstOrDefault();
			var createdAnims = new List<Animation.BaseAnimation>(5);

			if (rootActors.Length == 1 && rootActor.transform.parent == null)
			{
				// Delete entire hierarchy as we no longer have a valid parent actor for the root of this hierarchy.  It was likely
				// destroyed in the process of the async operation before this callback was called.
				foreach (var actor in createdActors)
				{
					actor.Destroy();
				}

				createdActors.Clear();

				SendCreateActorResponse(
					originalMessage,
					failureMessage: "Parent for the actor being created no longer exists.  Cannot create new actor.");
				return;
			}

			var secondPassXfrms = new List<Transform>(2);
			foreach (var root in rootActors)
			{
				ProcessActors(root.transform, root.transform.parent != null ? root.transform.parent.GetComponent<Actor>() : null);
			}
			// some things require the whole hierarchy to have actors on it. run those here
			foreach (var pass2 in secondPassXfrms)
			{
				ProcessActors2(pass2);
			}

			if (originalMessage != null && rootActors.Length == 1)
			{
				rootActor?.ApplyPatch(originalMessage.Actor);
			}
			Actor.ApplyVisibilityUpdate(rootActor);

			_actorManager.UponStable(
				() => SendCreateActorResponse(originalMessage, actors: createdActors, anims: createdAnims, onCompleteCallback: onCompleteCallback));

			void ProcessActors(Transform xfrm, Actor parent)
			{
				// Generate actors for all GameObjects, even if the loader didn't. Only loader-generated
				// actors are returned to the app though. We do this so library objects get enabled/disabled
				// correctly, even if they're not tracked by the app.
				var actor = xfrm.gameObject.GetComponent<Actor>() ?? xfrm.gameObject.AddComponent<Actor>();

				_actorManager.AddActor(guids.Next(), actor);
				_ownedGameObjects.Add(actor.gameObject);

				actor.ParentId = parent?.Id ?? actor.ParentId;
				if (actor.Renderer != null)
				{
					// only overwrite material if there's something in the cache, i.e. not a random library material
					var matId = AssetManager.GetByObject(actor.Renderer.sharedMaterial)?.Id;
					if (matId.HasValue)
					{
						actor.MaterialId = matId.Value;
					}

					actor.MeshId = AssetManager.GetByObject(actor.UnityMesh)?.Id ?? Guid.Empty;
				}

				// native animation construction requires the whole actor hierarchy to already exist. defer to second pass
				var nativeAnim = xfrm.gameObject.GetComponent<UnityEngine.Animation>();
				if (nativeAnim != null && createdActors.Contains(actor))
				{
					secondPassXfrms.Add(xfrm);
				}

				foreach (Transform child in xfrm)
				{
					ProcessActors(child, actor);
				}
			}

			void ProcessActors2(Transform xfrm)
			{
				var actor = xfrm.gameObject.GetComponent<Actor>();
				var nativeAnim = xfrm.gameObject.GetComponent<UnityEngine.Animation>();
				if (nativeAnim != null && createdActors.Contains(actor))
				{
					var animTargets = xfrm.gameObject.GetComponent<PrefabAnimationTargets>();
					int stateIndex = 0;
					foreach (AnimationState state in nativeAnim)
					{
						var anim = new NativeAnimation(AnimationManager, guids.Next(), nativeAnim, state);
						anim.TargetIds = animTargets != null
							? animTargets.GetTargets(xfrm, stateIndex++, addRootToTargets: true).Select(a => a.Id).ToList()
							: new List<Guid>() { actor.Id };

						AnimationManager.RegisterAnimation(anim);
						createdAnims.Add(anim);
					}
				}
			}
		}

		private void SendCreateActorResponse(
			CreateActor originalMessage,
			IList<Actor> actors = null,
			IList<Animation.BaseAnimation> anims = null,
			string failureMessage = null,
			Action onCompleteCallback = null)
		{
			Trace trace = new Trace()
			{
				Severity = (actors != null) ? TraceSeverity.Info : TraceSeverity.Error,
				Message = (actors != null) ?
					$"Successfully created {actors?.Count ?? 0} objects." :
					failureMessage
			};
			Protocol.Send(
				new ObjectSpawned()
				{
					Result = new OperationResult()
					{
						ResultCode = (actors != null) ? OperationResultCode.Success : OperationResultCode.Error,
						Message = trace.Message
					},
					Traces = new List<Trace>() { trace },
					Actors = actors?.Select((actor) => actor.GeneratePatch()).ToArray() ?? new ActorPatch[] { },
					Animations = anims?.Select(anim => anim.GeneratePatch()).ToArray() ?? new AnimationPatch[] { }
				},
				originalMessage?.MessageId
			);

			onCompleteCallback?.Invoke();
		}

		[CommandHandler(typeof(SyncAnimations))]
		private void OnSyncAnimations(SyncAnimations payload, Action onCompleteCallback)
		{
			if (payload.AnimationStates == null)
			{
				_actorManager.UponStable(() =>
				{
					// Gather and send the animation states of all actors.
					var animationStates = new List<MWActorAnimationState>();
					foreach (var actor in _actorManager.Actors)
					{
						if (actor != null)
						{
							var actorAnimationStates = actor.GetOrCreateActorComponent<AnimationComponent>().GetAnimationStates();
							if (actorAnimationStates != null)
							{
								animationStates.AddRange(actorAnimationStates);
							}
						}
					}
					Protocol.Send(new SyncAnimations()
					{
						AnimationStates = animationStates.ToArray()
					}, payload.MessageId);
					onCompleteCallback?.Invoke();
				});
			}
			else
			{
				// Apply animation states to the actors.
				foreach (var animationState in payload.AnimationStates)
				{
					SetAnimationState setAnimationState = new SetAnimationState();
					setAnimationState.ActorId = animationState.ActorId;
					setAnimationState.AnimationName = animationState.AnimationName;
					setAnimationState.State = animationState.State;
					_actorManager.ProcessActorCommand(animationState.ActorId, setAnimationState, null);
				}
				onCompleteCallback?.Invoke();
			}
		}

		[CommandHandler(typeof(SetAuthoritative))]
		private void OnSetAuthoritative(SetAuthoritative payload, Action onCompleteCallback)
		{
			IsAuthoritativePeer = payload.Authoritative;
			onCompleteCallback?.Invoke();
		}

		[CommandHandler(typeof(ShowDialog))]
		private void OnShowDialog(ShowDialog payload, Action onCompleteCallback)
		{
			if (MREAPI.AppsAPI.DialogFactory == null)
			{
				Protocol.Send(
					new DialogResponse() { FailureMessage = "This client has not implemented dialogs" },
					payload.MessageId
				);
				onCompleteCallback?.Invoke();
			}
			else if (!GrantedPermissions.HasFlag(Permissions.UserInteraction))
			{
				Protocol.Send(
					new DialogResponse() { FailureMessage = "The user has refused the MRE permission to open dialogs" },
					payload.MessageId
				);
				onCompleteCallback?.Invoke();
			}
			else
			{
				MREAPI.AppsAPI.DialogFactory.ShowDialog(this, payload.Text, payload.AcceptInput, (submitted, text) =>
				{
					Protocol.Send(
						new DialogResponse() { Submitted = submitted, Text = text },
						payload.MessageId
					);
					onCompleteCallback?.Invoke();
				});
			}
		}

		[CommandHandler(typeof(PhysicsBridgeUpdate))]
		private void OnTransformsUpdate(PhysicsBridgeUpdate payload, Action onCompleteCallback)
		{
			if (UsePhysicsBridge)
			{
				PhysicsBridge.addSnapshot(payload.PhysicsBridgePatch.Id, payload.PhysicsBridgePatch.ToSnapshot());
				onCompleteCallback?.Invoke();
			}
		}

		[CommandHandler(typeof(PhysicsTranformServerUpload))]
		private void OnTransformsServerUpload(PhysicsTranformServerUpload payload, Action onCompleteCallback)
		{
			if (UsePhysicsBridge)
			{
				onCompleteCallback?.Invoke();
			}
		}

		#endregion
	}
}

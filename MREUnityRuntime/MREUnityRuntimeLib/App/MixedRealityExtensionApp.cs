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
using MixedRealityExtension.Patching;
using MixedRealityExtension.Patching.Types;
using MixedRealityExtension.RPC;
using MixedRealityExtension.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

using Trace = MixedRealityExtension.Messaging.Trace;

namespace MixedRealityExtension.App
{
    internal sealed class MixedRealityExtensionApp : IMixedRealityExtensionApp, ICommandHandlerContext
    {
        private readonly AssetLoader _assetLoader;
        private readonly UserManager _userManager;
        private readonly ActorManager _actorManager;
        private readonly CommandManager _commandManager;

        private readonly MonoBehaviour _ownerScript;

        private IConnectionInternal _conn;

        private ISet<Guid> _interactingUserIds = new HashSet<Guid>();
        private IList<Action> _executionProtocolActionQueue = new List<Action>();
        private IList<GameObject> _ownedGameObjects = new List<GameObject>();
        private Queue<CreateFromGLTF> _createFromGLTFQueue = new Queue<CreateFromGLTF>();

        private enum AppState
        {
            Stopped,
            Starting,
            Running
        }

        private AppState _appState = AppState.Stopped;

        #region Events - Public

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

        #endregion

        #region Properties - Public

        /// <inheritdoc />
        public string GlobalAppId { get; }

        /// <inheritdoc />
        public string SessionId { get; private set; }

        /// <inheritdoc />
        public bool IsActive => _conn?.IsActive ?? false;

        /// <inheritdoc />
        public GameObject SceneRoot { get; set; }

        /// <inheritdoc />
        public IUser LocalUser { get; private set; }

        /// <inheritdoc />
        public RPCInterface RPC { get; }

        #endregion

        #region Properties - Internal

        internal MWEventManager EventManager { get; }

        internal Guid InstanceId { get; set; }

        internal OperatingModel OperatingModel { get; set; }

        internal bool IsAuthoritativePeer { get; set; }

        internal IProtocol Protocol { get; set; }

        internal IConnectionInternal Conn => _conn;

        internal SoundManager SoundManager { get; private set; }

        #endregion

        /// <summary>
        /// Initializes a new instance of the class <see cref="MixedRealityExtensionApp"/>
        /// </summary>
        /// <param name="globalAppId">The global id of the app.</param>
        /// <param name="ownerScript">The owner mono behaviour script for the app.</param>
        internal MixedRealityExtensionApp(string globalAppId, MonoBehaviour ownerScript)
        {
            GlobalAppId = globalAppId;
            _ownerScript = ownerScript;
            EventManager = new MWEventManager(this);
            _assetLoader = new AssetLoader(ownerScript, this);
            _userManager = new UserManager(this);
            _actorManager = new ActorManager(this);
            SoundManager = new SoundManager(this);
            _commandManager = new CommandManager(new Dictionary<Type, ICommandHandlerContext>()
            {
                { typeof(MixedRealityExtensionApp), this },
                { typeof(Actor), null },
                { typeof(AssetLoader), _assetLoader },
                { typeof(ActorManager), _actorManager }
            });

            RPC = new RPCInterface(this);
        }

        /// <inheritdoc />
        public void Startup(string url, string sessionId, string platformId)
        {
            if (_conn == null)
            {
                if (_appState == AppState.Stopped)
                {
                    _appState = AppState.Starting;
                }

                SessionId = sessionId;

                var connection = new WebSocket();

                connection.Url = url;
                connection.Headers.Add(Constants.SessionHeader, sessionId);
                connection.Headers.Add(Constants.PlatformHeader, platformId);
                connection.Headers.Add(Constants.LegacyProtocolVersionHeader, $"{Constants.LegacyProtocolVersion}");
                connection.Headers.Add(Constants.CurrentClientVersionHeader, Constants.CurrentClientVersion);
                connection.Headers.Add(Constants.MinimumSupportedSDKVersionHeader, Constants.MinimumSupportedSDKVersion);
                connection.OnConnecting += Conn_OnConnecting;
                connection.OnConnectFailed += Conn_OnConnectFailed;
                connection.OnConnected += Conn_OnConnected;
                connection.OnDisconnected += Conn_OnDisconnected;
                connection.OnError += Connection_OnError;
                _conn = connection;
            }
            _conn.Open();
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
            Disconnect();
            FreeResources();

            if (_appState != AppState.Stopped)
            {
                _appState = AppState.Stopped;
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
            SoundManager.Update();
            _commandManager.Update();
        }

        /// <inheritdoc />
        public void UserJoin(GameObject userGO, IUserInfo userInfo)
        {
            void PerformUserJoin()
            {
                var user = userGO.GetComponents<User>()
                    .FirstOrDefault(_user => _user.AppInstanceId == this.InstanceId);

                if (user == null)
                {
                    user = userGO.AddComponent<User>();
                    user.Initialize(userInfo, this);
                }

                Protocol.Send(new UserJoined()
                {
                    User = new UserPatch(user)
                });

                LocalUser = user;

                // TODO @tombu - Wait for the app to send back a success for join?
                _userManager.AddUser(user);
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
                // TODO @tombu - Wait for app to send success that the user has left the app?
                _userManager.RemoveUser(user);
                _interactingUserIds.Remove(user.Id);

                if (Protocol is Execution)
                {
                    Protocol.Send(new UserLeft() { UserId = user.Id });
                }
            }
        }

        /// <inheritdoc />
        public void EnableUserInteraction(IUser user)
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
        public void DisableUserInteration(IUser user)
        {
            _interactingUserIds.Remove(user.Id);
        }

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
                    ActorIds = new List<Guid>() { actorId }
                });
            }
        }

        public IUser FindUser(Guid id)
        {
            return _userManager.FindUser(id);
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

        internal bool IsInteractable(IUser user) => _interactingUserIds.Contains(user.Id);
        
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
            MREAPI.Logger.LogError($"Exception: {ex.Message}\nStack Trace: {ex.StackTrace}");
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

        #endregion

        #region Command Handlers

        [CommandHandler(typeof(AppToEngineRPC))]
        private void OnRPCReceived(AppToEngineRPC payload, Action onCompleteCallback)
        {
            RPC.ReceiveRPC(payload);
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
            catch(Exception e)
            {
                Debug.LogException(e);
            }
        }

        [CommandHandler(typeof(CreateFromGLTF))]
        private async Task OnCreateFromGLTF(CreateFromGLTF payload, Action onCompleteCallback)
        {
            IList<Actor> createdActors;
            try
            {
                createdActors = await _assetLoader.CreateFromGLTF(payload.ResourceUrl, payload.AssetName,
                    payload.Actor?.ParentId, payload.ColliderType);
                ProcessCreatedActors(payload, createdActors, onCompleteCallback);
            }
            catch (Exception e)
            {
                SendCreateActorResponse(payload,
                    failureMessage: $"An unexpected error occurred while loading glTF model [{payload.ResourceUrl}].\n{e.ToString()}",
                    onCompleteCallback: onCompleteCallback);
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

        [CommandHandler(typeof(CreatePrimitive))]
        private void OnCreatePrimitive(CreatePrimitive payload, Action onCompleteCallback)
        {
            try
            {
                var actors = _assetLoader.CreatePrimitive(payload.Definition, payload.Actor?.ParentId, payload.AddCollider);
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
                MREAPI.AppsAPI.AssetCache.OnCached(payload.PrefabId, _ =>
                {
                    if (this == null || _appState != AppState.Running) return;
                    var createdActors = _assetLoader.CreateFromPrefab(payload.PrefabId, payload.Actor?.ParentId);
                    ProcessCreatedActors(payload, createdActors, onCompleteCallback);
                });
            }
            catch (Exception e)
            {
                SendCreateActorResponse(payload, failureMessage: e.ToString(), onCompleteCallback: onCompleteCallback);
                Debug.LogException(e);
            }
        }

        private void ProcessCreatedActors(CreateActor originalMessage, IList<Actor> createdActors, Action onCompleteCallback)
        {
            var guids = new DeterministicGuids(originalMessage.Actor?.Id);
            var rootActor = createdActors.FirstOrDefault();

            if (rootActor.transform.parent == null)
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

            ProcessActors(rootActor.transform, rootActor.transform.parent.GetComponent<Actor>());

            rootActor?.ApplyPatch(originalMessage.Actor);
            Actor.ApplyVisibilityUpdate(rootActor);

            _actorManager.UponStable(
                () => SendCreateActorResponse(originalMessage, actors: createdActors, onCompleteCallback: onCompleteCallback));

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
                    actor.MaterialId = MREAPI.AppsAPI.AssetCache.GetId(actor.Renderer.sharedMaterial) ?? Guid.Empty;
                }

                foreach (Transform child in xfrm)
                {
                    ProcessActors(child, actor);
                }
            }
        }

        private void SendCreateActorResponse(CreateActor originalMessage, IList<Actor> actors = null, string failureMessage = null, Action onCompleteCallback = null)
        {
            Trace trace = new Trace()
            {
                Severity = (actors != null) ? TraceSeverity.Info : TraceSeverity.Error,
                Message = (actors != null) ?
                    $"Successfully created {actors?.Count ?? 0} objects." :
                    failureMessage
            };

            Protocol.Send(new ObjectSpawned()
            {
                Result = new OperationResult()
                {
                    ResultCode = (actors != null) ? OperationResultCode.Success : OperationResultCode.Error,
                    Message = trace.Message
                },

                Traces = new List<Trace>() { trace },
                Actors = actors?.Select((actor) => actor.GenerateInitialPatch()) ?? new ActorPatch[] { }
            },
                originalMessage.MessageId);

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
                        AnimationStates = animationStates.ToList()
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

        #endregion
    }
}

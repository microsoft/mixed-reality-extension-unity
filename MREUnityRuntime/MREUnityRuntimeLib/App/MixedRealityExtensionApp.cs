// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Animation;
using MixedRealityExtension.API;
using MixedRealityExtension.Assets;
using MixedRealityExtension.Behaviors;
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
            _commandManager = new CommandManager(new[]
            {
                typeof(MixedRealityExtensionApp),
                typeof(Actor),
                typeof(AssetLoader)
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
                connection.Headers.Add(Constants.ProtocolVersionHeader, $"{Constants.ProtocolVersion}");
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
                _conn.Update();
            }
        }

        /// <inheritdoc />
        public void UserJoin(GameObject userGO, IUserInfo userInfo)
        {
            void PerformUserJoin()
            {
                var user = userGO.GetComponents<User>()
                    .Where(_user => _user.AppInstanceId == this.InstanceId)
                    .FirstOrDefault()
                    ??
                    userGO.AddComponent<User>();

                user.Initialize(userInfo, this);
                var userPatch = new UserPatch(user);

                Protocol.Send(new UserJoined() { User = userPatch });

                this.LocalUser = user;

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
                var userPatch = new UserPatch(user);

                // TODO @tombu - Wait for app to send success that the user has left the app?
                _userManager.RemoveUser(user);
                _interactingUserIds.Remove(user.Id);

                if (Protocol is Execution)
                {
                    Protocol.Send(new UserLeft() { UserId = userPatch.Id });
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
            if (message.Payload is LoadAssets loadAssets)
            {
                loadAssets.MessageId = message.Id;
                ExecuteCommandPayload(this._assetLoader, loadAssets);
            }
            else if (message.Payload is NetworkCommandPayload commandPayload)
            {
                commandPayload.MessageId = message.Id;
                ExecuteCommandPayload(commandPayload);
            }
            else
            {
                MREAPI.Logger.LogError("Unexpected message");
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

        internal void ExecuteCommandPayload(ICommandPayload commandPayload)
        {
            ExecuteCommandPayload(this, commandPayload);
        }

        internal void ExecuteCommandPayload(ICommandHandlerContext handlerContext, ICommandPayload commandPayload)
        {
            _commandManager.ExecuteCommandPayload(handlerContext, commandPayload);
        }

        internal bool OwnsActor(IActor actor)
        {
            return FindActor(actor.Id) != null;
        }

        internal bool IsInteractable(IUser user) => _interactingUserIds.Contains(user.Id);

        #endregion

        #region Methods - Private

        [CommandHandler(typeof(CreateFromGLTF))]
        private async Task OnCreateFromGLTF(CreateFromGLTF payload)
        {
            if (_actorManager.HasActor(payload.Actor?.Id) && _actorManager.IsActorReserved(payload.Actor?.Id))
            {
                SendCreateActorResponse(payload, failureMessage: $"An actor with ID {payload.Actor?.Id} already exists");
                return;
            }

            _actorManager.Reserve(payload.Actor?.Id);

            IList<Actor> createdActors;
            try
            {
                createdActors = await _assetLoader.CreateFromGLTF(payload.ResourceUrl, payload.AssetName,
                    payload.Actor?.ParentId, payload.ColliderType);
            }
            catch (Exception ex)
            {
                EndCreateFromGLTF(failureMessage: UtilMethods.FormatException(
                    $"An unexpected error occurred while loading glTF model [{payload.ResourceUrl}].", ex));
                return;
            }

            DeterministicGuids guids = new DeterministicGuids(payload.Actor?.Id);
            foreach (var createdActor in createdActors)
            {
                _ownedGameObjects.Add(createdActor.gameObject);
                _actorManager.AddActor(guids.Next(), createdActor);
                createdActor.AddSubscriptions(payload.Subscriptions);
            }

            createdActors.FirstOrDefault()?.ApplyPatch(payload.Actor);

            EndCreateFromGLTF(actors: createdActors);

            void EndCreateFromGLTF(IList<Actor> actors = null, string failureMessage = null)
            {
                OperationResultCode resultCode = (actors != null) ? OperationResultCode.Success: OperationResultCode.Error;
                Trace trace = new Trace()
                {
                    Severity = (resultCode == OperationResultCode.Success) ? TraceSeverity.Info : TraceSeverity.Error,
                    Message = (resultCode == OperationResultCode.Success) ?
                        $"Successfully created {actors.Count} objects from glTF." :
                        failureMessage
                };

                Protocol.Send(new ObjectSpawned()
                    {
                        Result = new OperationResult()
                        {
                            ResultCode = resultCode,
                            Message = trace.Message
                        },
                        Traces = new List<Trace>() {trace},
                        Actors = actors?.Select((actor) => actor.GeneratePatch(SubscriptionType.All)).ToList() ?? new List<ActorPatch>()
                    },
                    payload.MessageId);
            }
        }

        private OperationResult EnableRigidBody(Guid actorId, RigidBodyPatch rigidBodyPatch)
        {
            var actor = (Actor)FindActor(actorId);
            if (actor == null)
            {
                return new OperationResult()
                {
                    ResultCode = OperationResultCode.Error,
                    Message = string.Format("Could not find an actor with id {0} to enable a rigidbody on.", actorId)
                };
            }
            else
            {
                return actor.EnableRigidBody(rigidBodyPatch);
            }
        }

        private OperationResult EnableLight(Guid actorId, LightPatch lightPatch)
        {
            var actor = (Actor)FindActor(actorId);

            if (actor == null)
            {
                return new OperationResult()
                {
                    Message = String.Format("PatchLight: Actor {0} not found", actorId),
                    ResultCode = OperationResultCode.Error
                };
            }
            else
            {
                return actor.EnableLight(lightPatch);
            }
        }

        private OperationResult EnableText(Guid actorId, TextPatch textPatch)
        {
            var actor = (Actor)FindActor(actorId);

            if (actor == null)
            {
                return new OperationResult()
                {
                    Message = String.Format("PatchText: Actor {0} not found", actorId),
                    ResultCode = OperationResultCode.Error
                };
            }
            else
            {
                return actor.EnableText(textPatch);
            }
        }

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

        private void UpdateActorSubsciptions(UpdateSubscriptions payload)
        {
            var actor = _actorManager.FindActor(payload.Id);
            if (actor != null)
            {
                actor.RemoveSubscriptions(payload.Removes);
                actor.AddSubscriptions(payload.Adds);
            }
        }

        private void UpdateUserSubsciptions(UpdateSubscriptions payload)
        {
            var user = _userManager.FindUser(payload.Id);
            if (user != null)
            {
                user.RemoveSubscriptions(this.InstanceId, payload.Removes);
                user.AddSubscriptions(this.InstanceId, payload.Adds);
            }
        }

        #endregion

        #region Command Handlers

        [CommandHandler(typeof(AppToEngineRPC))]
        private void OnRPCReceived(AppToEngineRPC payload)
        {
            RPC.ReceiveRPC(payload);
        }

        [CommandHandler(typeof(CreateFromLibrary))]
        private async void OnCreateFromLibrary(CreateFromLibrary payload)
        {
            if (_actorManager.HasActor(payload.Actor?.Id) && _actorManager.IsActorReserved(payload.Actor?.Id))
            {
                SendCreateActorResponse(payload, failureMessage: $"An actor with ID {payload.Actor?.Id} already exists");
            }
            else
            {
                _actorManager.Reserve(payload.Actor?.Id);
                try
                {
                    var actors = await _assetLoader.CreateFromLibrary(payload.ResourceId, payload.Actor?.ParentId);
                    ProcessCreatedActors(payload, actors, actors?[0].gameObject);
                }
                catch (Exception e)
                {
                    SendCreateActorResponse(payload, failureMessage: e.ToString());
                    Debug.LogException(e);
                }
            }
        }

        [CommandHandler(typeof(CreatePrimitive))]
        private void OnCreatePrimitive(CreatePrimitive payload)
        {
            if (_actorManager.HasActor(payload.Actor?.Id) && _actorManager.IsActorReserved(payload.Actor?.Id))
            {
                SendCreateActorResponse(payload, failureMessage: $"An actor with ID {payload.Actor?.Id} already exists");
            }
            else
            {
                _actorManager.Reserve(payload.Actor?.Id);
                try
                {
                    var actors = _assetLoader.CreatePrimitive(payload.Definition, payload.Actor?.ParentId, payload.AddCollider);
                    ProcessCreatedActors(payload, actors, actors?[0].gameObject);
                }
                catch (Exception e)
                {
                    SendCreateActorResponse(payload, failureMessage: e.ToString());
                    Debug.LogException(e);
                }
            }
        }

        [CommandHandler(typeof(CreateEmpty))]
        private void OnCreateEmpty(CreateEmpty payload)
        {
            if (_actorManager.HasActor(payload.Actor?.Id) && _actorManager.IsActorReserved(payload.Actor?.Id))
            {
                SendCreateActorResponse(payload, failureMessage: $"An actor with ID {payload.Actor?.Id} already exists");
            }
            else
            {
                _actorManager.Reserve(payload.Actor?.Id);
                try
                {
                    var actors = _assetLoader.CreateEmpty(payload.Actor?.ParentId);
                    ProcessCreatedActors(payload, actors, actors?[0].gameObject);
                }
                catch (Exception e)
                {
                    SendCreateActorResponse(payload, failureMessage: e.ToString());
                    Debug.LogException(e);
                }
            }
        }

        [CommandHandler(typeof(CreateFromPrefab))]
        private void OnCreateFromPrefab(CreateFromPrefab payload)
        {
            if (_actorManager.HasActor(payload.Actor?.Id) && _actorManager.IsActorReserved(payload.Actor?.Id))
            {
                SendCreateActorResponse(payload, failureMessage: $"An actor with ID {payload.Actor?.Id} already exists");
            }
            else
            {
                _actorManager.Reserve(payload.Actor?.Id);
                try
                {
                    var createdActors = _assetLoader.CreateFromPrefab(payload.PrefabId, payload.Actor?.ParentId);
                    ProcessCreatedActors(payload, createdActors, createdActors?[0].gameObject);
                }
                catch (Exception e)
                {
                    SendCreateActorResponse(payload, failureMessage: e.ToString());
                    Debug.LogException(e);
                }
            }
        }

        private void ProcessCreatedActors(CreateActor originalMessage, IList<Actor> createdActors, GameObject rootGO)
        {
            if (rootGO != null)
            {
                _ownedGameObjects.Add(rootGO);
            }

            var guids = new DeterministicGuids(originalMessage.Actor?.Id);
            foreach (var createdActor in createdActors)
            {
                _actorManager.AddActor(guids.Next(), createdActor);
            }

            createdActors.FirstOrDefault()?.ApplyPatch(originalMessage.Actor);
            foreach (var actor in createdActors)
            {
                actor.AddSubscriptions(originalMessage.Subscriptions);
            }

            SendCreateActorResponse(originalMessage, actors: createdActors);
        }

        private void SendCreateActorResponse(CreateActor originalMessage, IList<Actor> actors = null, string failureMessage = null)
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
                Actors = actors?.Select((actor) => actor.GeneratePatch(SubscriptionType.All)) ?? new ActorPatch[] { }
            },
                originalMessage.MessageId);
        }

        [CommandHandler(typeof(EnableRigidBody))]
        private void OnEnableRigidBody(EnableRigidBody payload)
        {
            var result = EnableRigidBody(payload.ActorId, payload.RigidBody);
            EventManager.QueueLateEvent(new ResponseEvent(payload.ActorId, payload.MessageId, result));
        }

        [CommandHandler(typeof(StateUpdate))]
        private void OnStateUpdate(StateUpdate payload)
        {
            foreach (var updatePayload in payload.Payloads)
            {
                OnActorUpdate((ActorUpdate)updatePayload);
            }
        }

        [CommandHandler(typeof(ActorCorrection))]
        private void OnActorCorrection(ActorCorrection payload)
        {
            // TODO: Interpolate this change onto the actor.
            var actor = _actorManager.FindActor(payload.Actor.Id);
            if (actor != null)
            {
                actor.SynchronizeEngine(payload.Actor);
            }
        }

        [CommandHandler(typeof(ActorUpdate))]
        private void OnActorUpdate(ActorUpdate payload)
        {
            var actor = _actorManager.FindActor(payload.Actor.Id);
            if (actor != null)
            {
                actor.SynchronizeEngine(payload.Actor);
            }
        }

        [CommandHandler(typeof(DestroyActors))]
        private void OnDestroyActors(DestroyActors payload)
        {
            _actorManager.DestroyActors(payload.ActorIds, payload.Traces);
        }

        [CommandHandler(typeof(EnableLight))]
        private void OnEnableLight(EnableLight payload)
        {
            OperationResult result = EnableLight(payload.ActorId, payload.Light);
            Protocol.Send(result, payload.MessageId);
        }

        [CommandHandler(typeof(EnableText))]
        private void OnEnableText(EnableText payload)
        {
            OperationResult result = EnableText(payload.ActorId, payload.Text);
            Protocol.Send(result, payload.MessageId);
        }

        [CommandHandler(typeof(UpdateSubscriptions))]
        private void OnUpdateSubscriptions(UpdateSubscriptions payload)
        {
            switch (payload.OwnerType)
            {
                case SubscriptionOwnerType.Actor:
                    UpdateActorSubsciptions(payload);
                    break;
                case SubscriptionOwnerType.User:
                    UpdateUserSubsciptions(payload);
                    break;
                default:
                    MREAPI.Logger.LogError($"Invalid subscription owner type: {payload.OwnerType}");
                    break;
            }
        }

        [CommandHandler(typeof(RigidBodyCommands))]
        private void OnRigidBodyCommands(RigidBodyCommands payload)
        {
            _actorManager.FindActor(payload.ActorId)?.ExecuteRigidBodyCommands(payload);
        }

        [CommandHandler(typeof(CreateAnimation))]
        private void OnCreateAnimation(CreateAnimation payload)
        {
            var actor = _actorManager.FindActor(payload.ActorId);
            if (actor != null)
            {
                actor.GetOrCreateActorComponent<AnimationComponent>()
                    .CreateAnimation(
                        payload.AnimationName,
                        payload.Keyframes,
                        payload.Events,
                        payload.WrapMode,
                        payload.InitialState,
                        isInternal: false,
                        onCreatedCallback: () =>
                        {
                            Protocol.Send(new OperationResult()
                            {
                                ResultCode = OperationResultCode.Success
                            }, payload.MessageId);
                        },
                        onCompleteCallback: null);
            }
            else
            {
                Protocol.Send(new OperationResult()
                {
                    ResultCode = OperationResultCode.Error,
                    Message = $"Actor {payload.ActorId} not found"
                }, payload.MessageId);
            }
        }

        [CommandHandler(typeof(DEPRECATED_StartAnimation))]
        private void OnStartAnimation(DEPRECATED_StartAnimation payload)
        {
            bool enabled = payload.Paused.HasValue && payload.Paused.Value;
            _actorManager.FindActor(payload.ActorId)?.GetOrCreateActorComponent<AnimationComponent>()
                .SetAnimationState(payload.AnimationName, payload.AnimationTime, speed: null, enabled);
        }

        [CommandHandler(typeof(DEPRECATED_StopAnimation))]
        private void OnStopAnimation(DEPRECATED_StopAnimation payload)
        {
            _actorManager.FindActor(payload.ActorId)?.GetOrCreateActorComponent<AnimationComponent>()
                .SetAnimationState(payload.AnimationName, payload.AnimationTime, speed: null, false);
        }

        [CommandHandler(typeof(DEPRECATED_PauseAnimation))]
        private void OnPauseAnimation(DEPRECATED_PauseAnimation payload)
        {
            _actorManager.FindActor(payload.ActorId)?.GetOrCreateActorComponent<AnimationComponent>()
                .SetAnimationState(payload.AnimationName, time: null, speed: null, false);
        }

        [CommandHandler(typeof(DEPRECATED_ResumeAnimation))]
        private void OnResumeAnimation(DEPRECATED_ResumeAnimation payload)
        {
            _actorManager.FindActor(payload.ActorId)?.GetOrCreateActorComponent<AnimationComponent>()
                .SetAnimationState(payload.AnimationName, time: null, speed: null, true);
        }

        [CommandHandler(typeof(DEPRECATED_ResetAnimation))]
        private void OnResetAnimation(DEPRECATED_ResetAnimation payload)
        {
            _actorManager.FindActor(payload.ActorId)?.GetOrCreateActorComponent<AnimationComponent>()
                .SetAnimationState(payload.AnimationName, time: 0, speed: null, null);
        }

        [CommandHandler(typeof(SyncAnimations))]
        private void OnSyncAnimations(SyncAnimations payload)
        {
            if (OperatingModel == OperatingModel.PeerAuthoritative)
            {
                if (IsAuthoritativePeer)
                {
                    // The authoritative peer gathers and sends the animation states of all actors.
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
                }
                else
                {
                    // The non-authoritative peer updates the animation states of all actors.
                    foreach (var animationState in payload.AnimationStates)
                    {
                        _actorManager.FindActor(animationState.ActorId)?.GetOrCreateActorComponent<AnimationComponent>()
                            .ApplyAnimationState(animationState);
                    }
                }
            }
            else
            {
                Protocol.Send(new OperationResult()
                {
                    ResultCode = OperationResultCode.Error,
                    Message = $"Received unexpected SyncAnimation payload"
                }, payload.MessageId);
            }
        }

        [CommandHandler(typeof(SetAnimationState))]
        private void OnSetAnimationState(SetAnimationState payload)
        {
            _actorManager.FindActor(payload.ActorId)?.GetOrCreateActorComponent<AnimationComponent>()
                .SetAnimationState(payload.AnimationName, payload.State.Time, payload.State.Speed, payload.State.Enabled);
        }

        [CommandHandler(typeof(InterpolateActor))]
        private void OnInterpolateActor(InterpolateActor payload)
        {
            var actor = _actorManager.FindActor(payload.ActorId);
            if (actor != null)
            {
                actor.GetOrCreateActorComponent<AnimationComponent>()
                    .Interpolate(
                        payload.Value,
                        payload.AnimationName,
                        payload.Duration,
                        payload.Curve,
                        payload.Enabled,
                        onCompleteCallback: () =>
                    {
                        Protocol.Send(new OperationResult()
                        {
                            ResultCode = OperationResultCode.Success
                        }, payload.MessageId);
                    });
            }
            else
            {
                Protocol.Send(new OperationResult()
                {
                    ResultCode = OperationResultCode.Error,
                    Message = $"Actor {payload.ActorId} not found"
                }, payload.MessageId);
            }
        }

        [CommandHandler(typeof(SetBehavior))]
        private void OnSetBehavior(SetBehavior payload)
        {
            var actor = _actorManager.FindActor(payload.ActorId);
            if (actor != null)
            {
                var behaviorComponent = actor.GetOrCreateActorComponent<BehaviorComponent>();

                if (payload.BehaviorType == BehaviorType.None && behaviorComponent.ContainsBehaviorHandler())
                {
                    behaviorComponent.ClearBehaviorHandler();

                    return;
                }

                var handler = BehaviorHandlerFactory.CreateBehaviorHandler(payload.BehaviorType, actor, new WeakReference<MixedRealityExtensionApp>(this));
                behaviorComponent.SetBehaviorHandler(handler);
            }
        }

        [CommandHandler(typeof(SetAuthoritative))]
        private void OnSetAuthoritative(SetAuthoritative payload)
        {
            IsAuthoritativePeer = payload.Authoritative;
        }

        [CommandHandler(typeof(LookAt))]
        private void OnLookAt(LookAt payload)
        {
            var actor = _actorManager.FindActor(payload.ActorId);
            if (actor != null)
            {
                actor.LookAt(payload.TargetId ?? Guid.Empty, payload.LookAtMode);
            }
        }

        #endregion
    }
}

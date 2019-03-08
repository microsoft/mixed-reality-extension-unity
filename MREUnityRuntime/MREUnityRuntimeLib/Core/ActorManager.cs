// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.API;
using MixedRealityExtension.App;
using MixedRealityExtension.Core.Interfaces;
using MixedRealityExtension.IPC;
using MixedRealityExtension.Messaging.Commands;
using MixedRealityExtension.Messaging.Payloads;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MixedRealityExtension.Core
{
    internal class ActorManager : ICommandHandlerContext
    {
        private MixedRealityExtensionApp _app;
        private Dictionary<Guid, Actor> _actorMapping = new Dictionary<Guid, Actor>();
        private Dictionary<Guid, ActorCommandQueue> _actorCommandQueues = new Dictionary<Guid, ActorCommandQueue>();
        private List<Action> _uponStable = new List<Action>();

        internal event MWEventHandler<IActor> OnActorCreated;

        internal Dictionary<Guid, Actor>.ValueCollection Actors => _actorMapping.Values;

        internal ActorManager(MixedRealityExtensionApp app)
        {
            _app = app;
        }

        internal Actor AddActor(Guid id, Actor actor)
        {
            actor.Initialize(id, _app);
            _actorMapping[id] = actor;
            OnActorCreated?.Invoke(actor);
            return actor;
        }

        internal void DestroyActors(IEnumerable<Guid> ids)
        {
            foreach (var id in ids)
            {
                var destroyActorCommand = new LocalCommand
                {
                    Command = () =>
                    {
                        if (_actorCommandQueues.TryGetValue(id, out ActorCommandQueue queue))
                        {
                            // Clear the queue so that pending messages are canceled.
                            queue.Clear();
                            _actorCommandQueues.Remove(id);
                        }

                        if (!_actorMapping.ContainsKey(id))
                        {
                            var message = "destroy-actors: Actor not found: " + id.ToString() + ".";
                            MREAPI.Logger.LogError(message);
                        }
                        else
                        {
                            var actor = _actorMapping[id];
                            _actorMapping.Remove(id);
                            try
                            {
                                actor.Destroy();
                            }
                            catch (Exception e)
                            {
                                MREAPI.Logger.LogError(e.ToString());
                            }
                            // Is there any other cleanup?  Do it here.
                        }
                    }
                };
                ProcessActorCommand(id, destroyActorCommand, null);
            }
        }

        internal void Reset()
        {
            _actorMapping.Clear();
        }

        internal Actor FindActor(Guid id)
        {
            if (_actorMapping.ContainsKey(id))
            {
                return _actorMapping[id];
            }
            else
            {
                return null;
            }
        }

        internal IEnumerable<Actor> FindChildren(Guid id)
        {
            return _actorMapping.Values.Where(a => a.ParentId == id);
        }

        internal bool HasActor(Guid? id)
        {
            return id.HasValue && _actorMapping.ContainsKey(id.Value);
        }

        internal void ProcessActorCommand(Guid actorId, NetworkCommandPayload payload, Action onCompleteCallback)
        {
            if (!_actorCommandQueues.TryGetValue(actorId, out ActorCommandQueue queue))
            {
                queue = new ActorCommandQueue(actorId, _app);
                _actorCommandQueues.Add(actorId, queue);
            }
            queue.Enqueue(payload, onCompleteCallback);
        }

        private List<ActorCommandQueue> _queuesForUpdate = new List<ActorCommandQueue>();

        internal void Update()
        {
            // _actorCommandQueues can be modified during the iteration below, so make a shallow copy.
            _queuesForUpdate.Clear();
            _queuesForUpdate.AddRange(_actorCommandQueues.Values);

            int totalPendingCount = 0;
            foreach (var queue in _queuesForUpdate)
            {
                queue.Update();
                totalPendingCount += queue.Count;
            }

            if (totalPendingCount == 0 && _uponStable.Count > 0)
            {
                var uponStable = new List<Action>(_uponStable);
                _uponStable.Clear();
                foreach (var callback in uponStable)
                {
                    callback?.Invoke();
                }
            }
        }

        internal void UponStable(Action callback)
        {
            _uponStable.Add(callback);
        }

        internal bool OnActorDestroy(Guid id)
        {
            bool removed = false;
            if (_actorMapping.ContainsKey(id))
            {
                _actorMapping.Remove(id);
                removed = true;
            }

            if (_actorCommandQueues.TryGetValue(id, out ActorCommandQueue queue))
            {
                // Clear the queue so that pending messages are canceled.
                queue.Clear();
                _actorCommandQueues.Remove(id);
            }

            return removed;
        }

        #region Command Handlers

        [CommandHandler(typeof(ActorCorrection))]
        private void OnActorCorrection(ActorCorrection payload, Action onCompleteCallback)
        {
            ProcessActorCommand(payload.Actor.Id, payload, onCompleteCallback);
        }

        [CommandHandler(typeof(ActorUpdate))]
        private void OnActorUpdate(ActorUpdate payload, Action onCompleteCallback)
        {
            ProcessActorCommand(payload.Actor.Id, payload, onCompleteCallback);
        }

        [CommandHandler(typeof(DestroyActors))]
        private void OnDestroyActors(DestroyActors payload, Action onCompleteCallback)
        {
            DestroyActors(payload.ActorIds);
            onCompleteCallback?.Invoke();
        }

        [CommandHandler(typeof(DEPRECATED_EnableRigidBody))]
        private void OnEnableRigidBody(DEPRECATED_EnableRigidBody payload, Action onCompleteCallback)
        {
            ProcessActorCommand(payload.ActorId, payload, onCompleteCallback);
        }

        [CommandHandler(typeof(DEPRECATED_EnableLight))]
        private void OnEnableLight(DEPRECATED_EnableLight payload, Action onCompleteCallback)
        {
            ProcessActorCommand(payload.ActorId, payload, onCompleteCallback);
        }

        [CommandHandler(typeof(DEPRECATED_EnableText))]
        private void OnEnableText(DEPRECATED_EnableText payload, Action onCompleteCallback)
        {
            ProcessActorCommand(payload.ActorId, payload, onCompleteCallback);
        }

        [CommandHandler(typeof(UpdateSubscriptions))]
        private void OnUpdateSubscriptions(UpdateSubscriptions payload, Action onCompleteCallback)
        {
            ProcessActorCommand(payload.Id, payload, onCompleteCallback);
        }

        [CommandHandler(typeof(RigidBodyCommands))]
        private void OnRigidBodyCommands(RigidBodyCommands payload, Action onCompleteCallback)
        {
            ProcessActorCommand(payload.ActorId, payload, onCompleteCallback);
        }

        [CommandHandler(typeof(CreateAnimation))]
        private void OnCreateAnimation(CreateAnimation payload, Action onCompleteCallback)
        {
            ProcessActorCommand(payload.ActorId, payload, onCompleteCallback);
        }

        [CommandHandler(typeof(DEPRECATED_StartAnimation))]
        private void OnStartAnimation(DEPRECATED_StartAnimation payload, Action onCompleteCallback)
        {
            ProcessActorCommand(payload.ActorId, payload, onCompleteCallback);
        }

        [CommandHandler(typeof(DEPRECATED_StopAnimation))]
        private void OnStopAnimation(DEPRECATED_StopAnimation payload, Action onCompleteCallback)
        {
            ProcessActorCommand(payload.ActorId, payload, onCompleteCallback);
        }

        [CommandHandler(typeof(DEPRECATED_PauseAnimation))]
        private void OnPauseAnimation(DEPRECATED_PauseAnimation payload, Action onCompleteCallback)
        {
            ProcessActorCommand(payload.ActorId, payload, onCompleteCallback);
        }

        [CommandHandler(typeof(DEPRECATED_ResumeAnimation))]
        private void OnResumeAnimation(DEPRECATED_ResumeAnimation payload, Action onCompleteCallback)
        {
            ProcessActorCommand(payload.ActorId, payload, onCompleteCallback);
        }

        [CommandHandler(typeof(DEPRECATED_ResetAnimation))]
        private void OnResetAnimation(DEPRECATED_ResetAnimation payload, Action onCompleteCallback)
        {
            ProcessActorCommand(payload.ActorId, payload, onCompleteCallback);
        }

        [CommandHandler(typeof(SetAnimationState))]
        private void OnSetAnimationState(SetAnimationState payload, Action onCompleteCallback)
        {
            ProcessActorCommand(payload.ActorId, payload, onCompleteCallback);
        }

        [CommandHandler(typeof(SetSoundState))]
        private void OnSetSoundState(SetSoundState payload, Action onCompleteCallback)
        {
            ProcessActorCommand(payload.ActorId, payload, onCompleteCallback);
        }

        [CommandHandler(typeof(InterpolateActor))]
        private void OnInterpolateActor(InterpolateActor payload, Action onCompleteCallback)
        {
            ProcessActorCommand(payload.ActorId, payload, onCompleteCallback);
        }

        [CommandHandler(typeof(SetBehavior))]
        private void OnSetBehavior(SetBehavior payload, Action onCompleteCallback)
        {
            ProcessActorCommand(payload.ActorId, payload, onCompleteCallback);
        }

        #endregion
    }
}

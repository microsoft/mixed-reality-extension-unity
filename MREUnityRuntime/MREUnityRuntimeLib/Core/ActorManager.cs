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
        private List<ActorCommandQueue> _queuesForUpdate = new List<ActorCommandQueue>(10);
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
                if (_actorCommandQueues.TryGetValue(id, out ActorCommandQueue queue))
                {
                    // Clear the queue so that pending messages are canceled.
                    queue.Clear();
                    _actorCommandQueues.Remove(id);
                }

                // Ignore missing actors in destroy calls
                if (_actorMapping.ContainsKey(id))
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

        internal void UpdateAllVisibility()
        {
            foreach(var actor in _actorMapping.Values.Where(a => a.Parent == null))
            {
                Actor.ApplyVisibilityUpdate(actor, force: true);
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
            ProcessActorCommand(payload.ActorId, payload, onCompleteCallback);
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

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using MixedRealityExtension.API;
using MixedRealityExtension.App;
using MixedRealityExtension.Core.Interfaces;
using MixedRealityExtension.IPC;
using MixedRealityExtension.Messaging;

namespace MixedRealityExtension.Core
{
    internal class ActorManager
    {
        private MixedRealityExtensionApp _app;
        private Dictionary<Guid, Actor> _actorMapping = new Dictionary<Guid, Actor>();
        private HashSet<Guid> _reservations = new HashSet<Guid>();

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
            _reservations.Remove(id);
            OnActorCreated?.Invoke(actor);
            return actor;
        }

        internal void DestroyActors(IEnumerable<Guid> ids, IList<Trace> traces)
        {
            foreach (var id in ids)
            {
                if (!_actorMapping.ContainsKey(id))
                {
                    var message = "destroy-actors: Actor not found: " + id.ToString() + ".";
                    if (traces != null)
                    {
                        traces.Add(new Trace()
                        {
                            Severity = TraceSeverity.Warning,
                            Message = message
                        });
                    }

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
                    catch { }
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

        internal bool HasActor(Guid? id)
        {
            return id.HasValue && _actorMapping.ContainsKey(id.Value);
        }

        internal bool IsActorReserved(Guid? id)
        {
            return id.HasValue && _reservations.Contains(id.Value);
        }

        internal void Reserve(Guid? id)
        {
            if (id.HasValue)
            {
                _reservations.Add(id.Value);
            }
        }

        internal bool OnActorDestroy(Guid id)
        {
            if (_actorMapping.ContainsKey(id))
            {
                _actorMapping.Remove(id);
                return true;
            }

            return false;
        }
    }
}

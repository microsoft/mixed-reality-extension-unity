// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.API;
using MixedRealityExtension.App;
using MixedRealityExtension.Core.Interfaces;
using MixedRealityExtension.IPC;
using MixedRealityExtension.Messaging.Commands;
using MixedRealityExtension.Messaging.Payloads;
using MixedRealityExtension.Util;
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

		public delegate void RigidBodyAddedHandler(Guid id, UnityEngine.Rigidbody rigidbody, Guid? owner);
		internal event RigidBodyAddedHandler RigidBodyAdded;

		public delegate void RigidBodyRemovedHandler(Guid id);
		internal event RigidBodyRemovedHandler RigidBodyRemoved;

		public delegate void RigidBodyKinematicsChangedHandler(Guid id, bool isKinematic);
		internal event RigidBodyKinematicsChangedHandler RigidBodyKinematicsChanged;

		public delegate void RigidBodyOwnerChangedHandler(Guid id, Guid? owner);
		public event RigidBodyOwnerChangedHandler RigidBodyOwnerChanged;

		internal ActorManager(MixedRealityExtensionApp app)
		{
			_app = app;
		}

		private void OnRigidBodyAdded(Guid id, UnityEngine.Rigidbody rigidbody, Guid? owner)
		{
			RigidBodyAdded?.Invoke(id, rigidbody, owner);
		}

		private void OnRigidBodyRemoved(Guid id)
		{
			RigidBodyRemoved?.Invoke(id);
		}

		private void OnRigidBodyKinematicsChanged(Guid id, bool isKinematic)
		{
			RigidBodyKinematicsChanged?.Invoke(id, isKinematic);
		}

		private void OnRigidBodyOwnerChanged(Guid id, Guid? owner)
		{
			RigidBodyOwnerChanged?.Invoke(id, owner);
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
						_app.Logger.LogError(e.ToString());
					}
					// Is there any other cleanup?  Do it here.
				}
			}
		}

		internal void Reset()
		{
			_actorMapping.Clear();
			_actorCommandQueues.Clear();
			_uponStable.Clear();
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
			_actorCommandQueues.GetOrCreate(actorId, () => new ActorCommandQueue(actorId, _app))
				.Enqueue(payload, onCompleteCallback);
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

		[CommandHandler(typeof(SetMediaState))]
		private void OnSetMediaState(SetMediaState payload, Action onCompleteCallback)
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

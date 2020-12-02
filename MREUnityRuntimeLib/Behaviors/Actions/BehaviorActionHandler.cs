// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.App;
using MixedRealityExtension.Behaviors.ActionData;
using MixedRealityExtension.Core.Interfaces;
using MixedRealityExtension.Messaging.Events.Types;
using MixedRealityExtension.Messaging.Payloads;
using System;

namespace MixedRealityExtension.Behaviors.Actions
{
	internal sealed class BehaviorActionHandler
	{
		private readonly string _actionName;
		private readonly BehaviorType _behaviorType;
		private readonly WeakReference<MixedRealityExtensionApp> _appRef;
		private readonly Guid _attachedActorId;

		internal BehaviorActionHandler(
			BehaviorType behaviorType, 
			string actionName, 
			WeakReference<MixedRealityExtensionApp> appRef, 
			Guid attachedActorId)
		{
			_actionName = actionName;
			_behaviorType = behaviorType;
			_appRef = appRef;
			_attachedActorId = attachedActorId;
		}

		internal void HandleActionPerforming(IUser user, BaseActionData actionData)
		{
			HandleActionStateChanged(user, ActionState.Performing, ActionState.Performing, actionData);
		}

		internal void HandleActionStateChanged(
			IUser user,
			ActionState oldState,
			ActionState newState,
			BaseActionData actionData)
		{
			MixedRealityExtensionApp app;
			if (!_appRef.TryGetTarget(out app))
			{
				return;
			}

			if (!app.IsInteractableForUser(user))
			{
				return;
			}

			var actionPerformed = new ActionPerformed()
			{
				UserId = user.Id,
				TargetId = _attachedActorId,
				BehaviorType = _behaviorType,
				ActionName = _actionName,
				ActionState = newState,
				ActionData = actionData
			};

			app.EventManager.QueueLateEvent(new BehaviorEvent(actionPerformed));
		}
	}
}

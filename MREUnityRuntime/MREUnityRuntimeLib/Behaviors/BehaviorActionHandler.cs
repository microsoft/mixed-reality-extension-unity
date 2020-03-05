// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.App;
using MixedRealityExtension.Behaviors.Actions;
using MixedRealityExtension.Core.Interfaces;
using MixedRealityExtension.Messaging.Events.Types;
using MixedRealityExtension.Messaging.Payloads;
using System;

namespace MixedRealityExtension.Behaviors
{
	internal sealed class BehaviorActionHandler : IActionHandler
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

		void IActionHandler.HandleActionStateChanged(IUser user, ActionState oldState, ActionState newState)
		{
			MixedRealityExtensionApp app;
			if (!_appRef.TryGetTarget(out app))
			{
				return;
			}

			var actionPerformed = new ActionPerformed()
			{
				UserId = user.Id,
				TargetId = _attachedActorId,
				BehaviorType = _behaviorType,
				ActionName = _actionName,
				ActionState = newState
			};

			app.EventManager.QueueLateEvent(new BehaviorEvent(actionPerformed));
		}
	}
}

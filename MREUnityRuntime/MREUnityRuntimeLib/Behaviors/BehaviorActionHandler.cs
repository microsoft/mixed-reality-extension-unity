// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.App;
using MixedRealityExtension.Behaviors.Actions;
using MixedRealityExtension.Core.Interfaces;
using MixedRealityExtension.Messaging.Events.Types;
using MixedRealityExtension.Messaging.Payloads;
using MixedRealityExtension.Triggers.TriggeredActions;
using System;
using System.Collections.Generic;

namespace MixedRealityExtension.Behaviors
{
    internal sealed class BehaviorActionHandler : IActionHandler
    {
        private readonly string _actionName;
        private readonly BehaviorType _behaviorType;
        private readonly WeakReference<MixedRealityExtensionApp> _appRef;
        private readonly Guid _attachedActorId;

        private ITriggeredAction _actionStartedTriggeredAction;
        private ITriggeredAction _actionStoppedTriggeredAction;

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
            if (!_appRef.TryGetTarget(out MixedRealityExtensionApp app))
            {
                return;
            }

            ProcessActionHandlers(user, newState);
            
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

        public void SetActionHandler(ActionState actionState, ITriggeredAction triggeredAction)
        {
            if (actionState == ActionState.Started)
            {
                _actionStartedTriggeredAction = triggeredAction;
            }
            else
            {
                _actionStoppedTriggeredAction = triggeredAction;
            }
        }

        private void ProcessActionHandlers(IUser user, ActionState actionState)
        {
            if (!_appRef.TryGetTarget(out MixedRealityExtensionApp app))
            {
                return;
            }

            var actionStateHandler = (actionState == ActionState.Started) ? _actionStartedTriggeredAction : _actionStoppedTriggeredAction;

            if (actionStateHandler != null)
            {
                actionStateHandler.OnTriggered(app, user, _attachedActorId);
            }
        }
    }
}

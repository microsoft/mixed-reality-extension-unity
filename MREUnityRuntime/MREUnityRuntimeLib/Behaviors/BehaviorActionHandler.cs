// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.App;
using MixedRealityExtension.Behaviors.Actions;
using MixedRealityExtension.Behaviors.Handlers.ActionStateHandlers;
using MixedRealityExtension.Core.Interfaces;
using MixedRealityExtension.Messaging.Events.Types;
using MixedRealityExtension.Messaging.Payloads;
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

        private List<IActionStateHandler> _actionStartedHandlers = new List<IActionStateHandler>();
        private List<IActionStateHandler> _actionStoppedHandlers = new List<IActionStateHandler>();

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

        public void AddActionHandler(ActionState actionState, IActionStateHandler actionStateHandler)
        {
            if (actionState == ActionState.Started)
            {
                _actionStartedHandlers.Add(actionStateHandler);
            }
            else
            {
                _actionStoppedHandlers.Add(actionStateHandler);
            }
        }

        private void ProcessActionHandlers(IUser user, ActionState actionState)
        {
            var actionStateHandlers = (actionState == ActionState.Started) ? _actionStartedHandlers : _actionStoppedHandlers;

            foreach (var handler in actionStateHandlers)
            {
                handler.OnActionStateTriggered(user, _attachedActorId);
            }
        }
    }
}

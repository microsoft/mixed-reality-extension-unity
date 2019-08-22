// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.App;
using MixedRealityExtension.Behaviors.Actions;
using MixedRealityExtension.Core.Interfaces;
using MixedRealityExtension.PluginInterfaces.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MixedRealityExtension.Behaviors
{
    internal abstract class BehaviorHandlerBase : IBehaviorHandler
    {
        private readonly WeakReference<MixedRealityExtensionApp> _appRef;
        private readonly Guid _attachedActorId;

        private BehaviorType? _behaviorType;
        private Dictionary<string, BehaviorActionHandler> _actionHandlers =
            new Dictionary<string, BehaviorActionHandler>();

        protected IBehavior Behavior { get; private set; }

        BehaviorType IBehaviorHandler.BehaviorType
        {
            get
            {
                _behaviorType = _behaviorType ?? GetBehaviorType();
                return _behaviorType.Value;
            }
        }

        IBehavior IBehaviorHandler.Behavior => Behavior;

        internal BehaviorHandlerBase(
            IBehavior behavior,
            WeakReference<MixedRealityExtensionApp> appRef, 
            IActor attachedActor)
        {
            Behavior = behavior;
            _appRef = appRef;
            _attachedActorId = attachedActor.Id;

            Behavior.Actor = attachedActor;
        }

        protected void RegisterActionHandler(MWAction action, string name)
        {
            var handler = new BehaviorActionHandler(((IBehaviorHandler)this).BehaviorType, name, _appRef, _attachedActorId);
            action.Handler = handler;
            _actionHandlers[name.ToLower()] = handler;
        }

        public BehaviorActionHandler GetActionHandler(string actionName)
        {
            if (_actionHandlers.ContainsKey(actionName.ToLower())
            {
                return _actionHandlers[actionName.ToLower()];
            }

            return null;
        }

        public bool Equals(IBehaviorHandler other)
        {
            return ((IBehaviorHandler)this).BehaviorType == other.BehaviorType;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as IBehaviorHandler);
        }

        public override int GetHashCode()
        {
            return ((IBehaviorHandler)this).BehaviorType.GetHashCode();
        }

        void IBehaviorHandler.CleanUp()
        {
            var behavior = Behavior;
            Behavior = null;

            behavior.CleanUp();
        }

        bool IEquatable<IBehaviorHandler>.Equals(IBehaviorHandler other)
        {
            return ((IBehaviorHandler)this).BehaviorType == other.BehaviorType;
        }

        private BehaviorType GetBehaviorType()
        {
            var behaviorEnumType = typeof(BehaviorType);
            foreach (var name in behaviorEnumType.GetEnumNames())
            {
                var behaviorHandlerTypeAttr = behaviorEnumType.GetField(name)
                    .GetCustomAttributes(false)
                    .OfType<BehaviorHandlerType>()
                    .SingleOrDefault();
        
                if (this.GetType() == behaviorHandlerTypeAttr?.HandlerType)
                {
                    return (BehaviorType)Enum.Parse(typeof(BehaviorType), name);
                }
            }
        
            return BehaviorType.None;
        }
    }
}

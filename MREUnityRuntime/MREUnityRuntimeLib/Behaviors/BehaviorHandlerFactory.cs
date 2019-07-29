// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.API;
using MixedRealityExtension.App;
using MixedRealityExtension.Behaviors.Handlers;
using MixedRealityExtension.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace MixedRealityExtension.Behaviors
{
    internal static class BehaviorHandlerFactory
    {
        private static Dictionary<BehaviorType, Type> s_behaviorHandlerTypeLookup = new Dictionary<BehaviorType, Type>();

        static BehaviorHandlerFactory()
        {
            s_behaviorHandlerTypeLookup.Add(BehaviorType.Target, typeof(TargetHandler));
            s_behaviorHandlerTypeLookup.Add(BehaviorType.Button, typeof(ButtonHandler));
        }

        internal static IBehaviorHandler CreateBehaviorHandler(BehaviorType behaviorType, IActor actor, WeakReference<MixedRealityExtensionApp> appRef)
        {
            if (s_behaviorHandlerTypeLookup.ContainsKey(behaviorType))
            {
                var methodInfo = s_behaviorHandlerTypeLookup[behaviorType].GetMethod("Create", BindingFlags.Static | BindingFlags.NonPublic);
                if (methodInfo != null)
                {
                    return (IBehaviorHandler)methodInfo.Invoke(null, new object[] { actor, appRef });
                }
                else
                {
                    throw new NotImplementedException($"A handler for the behavior type {behaviorType.ToString()} exists but does not have a static create method implemented on it.");
                }
            }

            MixedRealityExtensionApp app;
            if (appRef.TryGetTarget(out app))
            {
                app.Logger.LogError($"Trying to create a behavior of type {behaviorType.ToString()}, but no handler is registered for the given type.");
            }
            return null;
        }
    }
}

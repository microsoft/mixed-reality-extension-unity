// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.API;
using MixedRealityExtension.App;
using MixedRealityExtension.Core.Interfaces;
using MixedRealityExtension.PluginInterfaces.Behaviors;
using System;

namespace MixedRealityExtension.Behaviors.Handlers
{
    internal class TargetHandler : BehaviorHandlerBase
    {
        internal TargetHandler(ITargetBehavior target, WeakReference<MixedRealityExtensionApp> appRef, IActor attachedActor)
            : base(target, appRef, attachedActor)
        {
            RegisterActionHandler(target.Target, nameof(target.Target));
            RegisterActionHandler(target.Grab, nameof(target.Grab));
        }

        internal static TargetHandler Create(IActor actor, WeakReference<MixedRealityExtensionApp> appRef)
        {
            var behaviorFactory = MREAPI.AppsAPI.BehaviorFactory;
            var targetBehavior = behaviorFactory.CreateTargetBehavior(actor);
            return new TargetHandler(targetBehavior, appRef, actor);
        }
    }
}

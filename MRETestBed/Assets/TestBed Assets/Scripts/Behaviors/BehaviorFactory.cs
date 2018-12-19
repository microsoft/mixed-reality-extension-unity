// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Core.Interfaces;
using MixedRealityExtension.PluginInterfaces.Behaviors;

namespace Assets.Scripts.Behaviors
{
    public class BehaviorFactory : IBehaviorFactory
    {
        public IButtonBehavior CreateButtonBehavior(IActor actor)
        {
            return actor.GameObject.AddComponent<ButtonBehavior>();
        }

        public ITargetBehavior CreateTargetBehavior(IActor actor)
        {
            return actor.GameObject.AddComponent<TargetBehavior>();
        }
    }
}

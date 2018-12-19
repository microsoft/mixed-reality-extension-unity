// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using Assets.Scripts.Tools;
using MixedRealityExtension.Behaviors.Actions;
using MixedRealityExtension.PluginInterfaces.Behaviors;
using System;

namespace Assets.Scripts.Behaviors
{
    public class TargetBehavior : BehaviorBase, ITargetBehavior
    {
        public MWAction Target { get; } = new MWAction();

        public override Type GetDesiredToolType()
        {
            return typeof(TargetTool);
        }
    }
}

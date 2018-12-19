// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using Assets.Scripts.Tools;
using MixedRealityExtension.Behaviors.Actions;
using MixedRealityExtension.PluginInterfaces.Behaviors;
using System;

namespace Assets.Scripts.Behaviors
{
    public class ButtonBehavior : TargetBehavior, IButtonBehavior
    {
        public MWAction Hover { get; } = new MWAction();

        public MWAction Click { get; } = new MWAction();

        public override Type GetDesiredToolType()
        {
            return typeof(ButtonTool);
        }
    }
}

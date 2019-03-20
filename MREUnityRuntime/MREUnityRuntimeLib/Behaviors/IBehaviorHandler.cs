// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.PluginInterfaces.Behaviors;
using System;

namespace MixedRealityExtension.Behaviors
{
    internal interface IBehaviorHandler : IEquatable<IBehaviorHandler>
    {
        IBehavior Behavior { get; }

        BehaviorType BehaviorType { get; }

        void CleanUp();
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;

namespace MixedRealityExtension.Behaviors
{
    internal interface IBehaviorHandler : IEquatable<IBehaviorHandler>
    {
        BehaviorType BehaviorType { get; }

        void CleanUp();
    }
}

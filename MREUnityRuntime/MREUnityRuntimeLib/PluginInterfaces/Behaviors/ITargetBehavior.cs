// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Behaviors.Actions;

namespace MixedRealityExtension.PluginInterfaces.Behaviors
{
    /// <summary>
    /// The interface that represents the target behavior in the target platform for MWI Apps.
    /// </summary>
    public interface ITargetBehavior : IBehavior
    {
        /// <summary>
        /// The target action in the target platform..
        /// </summary>
        MWAction Target { get; }
    }
}

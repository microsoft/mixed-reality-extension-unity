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
        /// Whether the target is grabbable or not.
        /// </summary>
        bool Grabbable { get; set; }

        /// <summary>
        /// The target action in the target platform.
        /// </summary>
        MWAction Target { get; }

        /// <summary>
        /// The grab action in the target platform.
        /// </summary>
        MWAction Grab { get; }
    }
}

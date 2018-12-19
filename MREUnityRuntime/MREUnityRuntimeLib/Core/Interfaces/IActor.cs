// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
namespace MixedRealityExtension.Core.Interfaces
{
    /// <summary>
    /// The interface that represents an actor within the mixed reality extension runtime.
    /// </summary>
    public interface IActor : IMixedRealityExtensionObject
    {
        /// <summary>
        /// Gets the ID of the actor's parent.
        /// </summary>
        IActor Parent { get; }

        /// <summary>
        /// Gets the name of the actor.
        /// </summary>
        string Name { get; set; }
    }
}

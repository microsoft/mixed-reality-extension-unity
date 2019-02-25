// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Core.Types;
using System;
using UnityEngine;

namespace MixedRealityExtension.Core.Interfaces
{
    /// <summary>
    /// The interface that represents a basic mixed reality extension object within the mixed reality extension runtime.
    /// </summary>
    public interface IMixedRealityExtensionObject
    {
        /// <summary>
        /// The id of the mixed reality object.
        /// </summary>
        Guid Id { get; }

        /// <summary>
        /// The instance id of the app that is the owner of this mixed reality extension object.
        /// </summary>
        Guid AppInstanceId { get; }

        /// <summary>
        /// The local user.
        /// </summary>
        IUser LocalUser { get; }

        /// <summary>
        /// The transform of this mixed reality extension object.
        /// </summary>
        MWTransform Transform { get; }

        /// <summary>
        /// The unity game object that the actor is associated with.
        /// </summary>
        GameObject GameObject { get; }

        /// <summary>
        /// The world-space position to be the look-at target by other objects.
        /// </summary>
        Vector3? LookAtPosition { get; }
    }
}

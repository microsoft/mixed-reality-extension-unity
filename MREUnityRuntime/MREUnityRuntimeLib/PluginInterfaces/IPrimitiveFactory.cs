// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Core.Interfaces;
using MixedRealityExtension.Core.Types;
using System;
using UnityEngine;

namespace MixedRealityExtension.PluginInterfaces
{
    /// <summary>
    /// Classes that implement this interface can be used to generate engine primitives
    /// </summary>
    public interface IPrimitiveFactory
    {
        /// <summary>
        /// Spawn a primitive
        /// </summary>
        /// <param name="definition">The shape and size of the primitive to spawn</param>
        /// <param name="parent">If provided, the prim actor will be spawned as a child of this game object. If not, the scene root.</param>
        /// <param name="addCollider">If true, add a collider of a matching shape and size.</param>
        /// <returns>The GameObject of the newly created primitive</returns>
        GameObject CreatePrimitive(PrimitiveDefinition definition, GameObject parent, bool addCollider);
    }
}

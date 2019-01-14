// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Core.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MixedRealityExtension.Core
{
    /// <summary>
    /// The type of the collider.
    /// </summary>
    public enum ColliderType
    {
        /// <summary>
        /// No collider.
        /// </summary>
        None = 0,

        /// <summary>
        /// Box shaped collider.
        /// </summary>
        Box = 1,

        /// <summary>
        /// Sphere shaped collider.
        /// </summary>
        Sphere = 2,

        /// <summary>
        /// Mesh based collider.
        /// </summary>
        Mesh = 3
    }

    public class Collider
    {
        public static bool ColliderTypeToPrimitiveShape(ColliderType colliderType, out PrimitiveShape primitiveShape)
        {
            switch (colliderType)
            {
                case ColliderType.Box:
                    primitiveShape = PrimitiveShape.Box;
                    return true;
                case ColliderType.Sphere:
                    primitiveShape = PrimitiveShape.Sphere;
                    return true;
                default:
                    primitiveShape = PrimitiveShape.InnerSphere; // need a None value
                    return false;
            }
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using MixedRealityExtension.Core.Types;
using MixedRealityExtension.Util.Unity;
using UnityEngine;

using UnityCollider = UnityEngine.Collider;

namespace MixedRealityExtension.Core
{
    /// <summary>
    /// Abstract class that represents the collider geometry.
    /// </summary>
    public abstract class ColliderGeometry
    {
        /// <summary>
        /// The type of the collider.  <see cref="ColliderType"/>
        /// </summary>
        public abstract ColliderType ColliderType { get; }

        internal abstract void Patch(UnityCollider collider);
    }

    /// <summary>
    /// Class that represents the sphere geometry for a sphere collider.
    /// </summary>
    public class SphereColliderGeometry : ColliderGeometry
    {
        /// <inheritdoc />
        public override ColliderType ColliderType => ColliderType.Sphere;

        /// <summary>
        /// The radius of the sphere collider geometry.
        /// </summary>
        public float? Radius { get; set; }

        /// <summary>
        /// The center of the sphere collider geometry.
        /// </summary>
        public MWVector3 Center { get; set; }

        internal override void Patch(UnityCollider collider)
        {
            if (collider is SphereCollider sphereCollider)
            {
                Patch(sphereCollider);
            }
        }

        private void Patch(SphereCollider collider)
        {
            if (Center != null)
            {
                collider.center = Center.ToVector3();
            }

            if (Radius != null)
            {
                collider.radius = Radius.Value;
            }
        }
    }

    /// <summary>
    /// Class that represents the box geometry of a box collider.
    /// </summary>
    public class BoxColliderGeometry : ColliderGeometry
    {
        /// <inheritdoc />
        public override ColliderType ColliderType => ColliderType.Box;

        /// <summary>
        /// The size of the box collider geometry.
        /// </summary>
        public MWVector3 Size { get; set; }

        /// <summary>
        /// The center of the box collider geometry.
        /// </summary>
        public MWVector3 Center { get; set; }

        internal override void Patch(UnityCollider collider)
        {
            if (collider is BoxCollider boxCollider)
            {
                Patch(boxCollider);
            }
        }

        private void Patch(BoxCollider collider)
        {
            if (Center != null)
            {
                collider.center = Center.ToVector3();
            }

            if (Size != null)
            {
                collider.size = Size.ToVector3();
            }
        }
    }

    /// <summary>
    /// Class that represents the mesh geometry of a mesh collider.
    /// </summary>
    public class MeshColliderGeometry : ColliderGeometry
    {
        /// <inheritdoc />
        public override ColliderType ColliderType => ColliderType.Mesh;

        internal override void Patch(UnityCollider collider)
        {
            // We do not accept patching for mesh colliders from the app.
        }
    }
}

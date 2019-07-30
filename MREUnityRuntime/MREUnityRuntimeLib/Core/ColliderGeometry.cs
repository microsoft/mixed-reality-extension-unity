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
        /// The type of the collider.  <see cref="Shape"/>
        /// </summary>
        public abstract ColliderType Shape { get; }

        internal abstract void Patch(UnityCollider collider);
    }

    /// <summary>
    /// Class that represents the sphere geometry for a sphere collider.
    /// </summary>
    public class SphereColliderGeometry : ColliderGeometry
    {
        /// <inheritdoc />
        public override ColliderType Shape => ColliderType.Sphere;

        /// <summary>
        /// The center of the sphere collider geometry.
        /// </summary>
        public MWVector3 Center { get; set; }

        /// <summary>
        /// The radius of the sphere collider geometry.
        /// </summary>
        public float? Radius { get; set; }

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
                Vector3 newCenter;
                newCenter.x = Center.X;
                newCenter.y = Center.Y;
                newCenter.z = Center.Z;
                collider.center = newCenter;
            }

            if (Radius != null)
            {
                collider.radius = Mathf.Abs(Radius.Value);
            }
        }
    }

    /// <summary>
    /// Class that represents the box geometry of a box collider.
    /// </summary>
    public class BoxColliderGeometry : ColliderGeometry
    {
        /// <inheritdoc />
        public override ColliderType Shape => ColliderType.Box;

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
                Vector3 newCenter;
                newCenter.x = Center.X;
                newCenter.y = Center.Y;
                newCenter.z = Center.Z;
                collider.center = newCenter;
            }

            if (Size != null)
            {
                Vector3 newSize;
                newSize.x = Mathf.Abs(Size.X);
                newSize.y = Mathf.Abs(Size.Y);
                newSize.z = Mathf.Abs(Size.Z);
                collider.size = newSize;
            }
        }
    }

    /// <summary>
    /// Class that represents the mesh geometry of a mesh collider.
    /// </summary>
    public class MeshColliderGeometry : ColliderGeometry
    {
        /// <inheritdoc />
        public override ColliderType Shape => ColliderType.Mesh;

        internal override void Patch(UnityCollider collider)
        {
            // We do not accept patching for mesh colliders from the app.
        }
    }

    public class CapsuleColliderGeometry : ColliderGeometry
    {
        public override ColliderType Shape => ColliderType.Capsule;

        public MWVector3 Center { get; set; }

        /// <summary>
        /// The dimensions of the collider, with the largest component of the vector being the
        /// primary axis and height of the capsule, and the second largest the radius.
        /// </summary>
        public MWVector3 Size { get; set; }

        public int? Direction
        {
            get => Size?.LargestComponentIndex();

        }

        public float? Height
        {
            get => Size?.LargestComponentValue();
        }

        public float? Radius
        {
            get => Size != null ? Size.SmallestComponentValue() / 2 : (float?) null;
        }

        internal override void Patch(UnityCollider collider)
        {
            if (collider is CapsuleCollider capsuleCollider)
            {
                Patch(capsuleCollider);
            }
        }

        private void Patch(CapsuleCollider collider)
        {
            if (Center != null)
            {
                Vector3 newCenter;
                newCenter.x = Center.X;
                newCenter.y = Center.Y;
                newCenter.z = Center.Z;
                collider.center = newCenter;
            }

            if (Size != null)
            {
                collider.radius = Mathf.Abs(Radius.Value);
                collider.height = Mathf.Abs(Height.Value);
                collider.direction = Direction.Value;
            }
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using MixedRealityExtension.API;
using MixedRealityExtension.Core.Interfaces;
using MixedRealityExtension.Patching;
using MixedRealityExtension.Patching.Types;
using MixedRealityExtension.Util.Unity;
using UnityEngine;

using UnityCollider = UnityEngine.Collider;

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

    internal class Collider : ICollider
    {
        private readonly UnityCollider _collider;

        /// <inheritdoc />
        public bool IsEnabled => _collider.enabled;

        /// <inheritdoc />
        public bool IsTrigger => _collider.isTrigger;

        /// <inheritdoc />
        //public CollisionLayer CollisionLayer { get; set; }

        /// <inheritdoc />
        public ColliderType ColliderType { get; private set; }

        internal Collider(UnityCollider unityCollider)
        {
            _collider = unityCollider;
        }

        internal void ApplyPatch(ColliderPatch patch)
        {
            _collider.enabled = _collider.enabled.GetPatchApplied(IsEnabled.ApplyPatch(patch.IsEnabled));
            _collider.isTrigger = _collider.isTrigger.GetPatchApplied(IsTrigger.ApplyPatch(patch.IsTrigger));
        }

        internal void SynchronizeEngine(ColliderPatch patch)
        {
            ApplyPatch(patch);
        }

        internal ColliderPatch GenerateInitialPatch()
        {
            ColliderGeometry colliderGeo = null;

            if (_collider is SphereCollider sphereCollider)
            {
                colliderGeo = new SphereColliderGeometry()
                {
                    Radius = sphereCollider.radius,
                    Center = sphereCollider.center.ToMWVector3()
                };
            }
            else if (_collider is BoxCollider boxCollider)
            {
                colliderGeo = new BoxColliderGeometry()
                {
                    Size = boxCollider.size.ToMWVector3(),
                    Center = boxCollider.center.ToMWVector3()
                };
            }
            else if (_collider is MeshCollider meshCollider)
            {
                colliderGeo = new MeshColliderGeometry();
            }
            else
            {
                MREAPI.Logger.LogWarning($"MRE SDK does not support the following Unity collider and will not " +
                    $"be available in the MRE app.  Collider Type: {_collider.GetType()}");
            }

            return colliderGeo == null ? null : new ColliderPatch()
                {
                    IsEnabled = _collider.enabled,
                    IsTrigger = _collider.isTrigger,
                    ColliderGeometry = colliderGeo
                };
        }
    }
}

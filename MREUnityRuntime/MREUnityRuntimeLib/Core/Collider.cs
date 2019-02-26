// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Core.Interfaces;
using MixedRealityExtension.Patching;
using MixedRealityExtension.Patching.Types;
using MixedRealityExtension.Util.Unity;
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

    public class Collider : ICollider
    {
        private readonly UnityEngine.Collider _collider;

        public bool IsEnabled => _collider.enabled;

        public bool IsTrigger => _collider.isTrigger;

        //public CollisionLayer CollisionLayer { get; set; }

        public ColliderType ColliderType { get; private set; }

        internal Collider(UnityEngine.Collider unityCollider)
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
    }
}

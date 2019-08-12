// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using MixedRealityExtension.API;
using MixedRealityExtension.Core.Collision;
using MixedRealityExtension.Core.Interfaces;
using MixedRealityExtension.Core.Types;
using MixedRealityExtension.Messaging.Events.Types;
using MixedRealityExtension.Patching;
using MixedRealityExtension.Patching.Types;
using MixedRealityExtension.Util.Unity;
using System;
using System.Linq;
using UnityEngine;

using MREContactPoint = MixedRealityExtension.Core.Collision.ContactPoint;

using UnityCollider = UnityEngine.Collider;
using UnityCollision = UnityEngine.Collision;

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
        /// Choose best collider shape for mesh
        /// </summary>
        Auto,

        /// <summary>
        /// Box shaped collider.
        /// </summary>
        Box,

        /// <summary>
        /// Sphere shaped collider.
        /// </summary>
        Sphere,

        /// <summary>
        /// Capsule shaped collider.
        /// </summary>
        Capsule,

        /// <summary>
        /// Mesh collider.
        /// </summary>
        Mesh
    }

    internal class Collider : MonoBehaviour, ICollider
    {
        private UnityCollider _collider;
        private ColliderEventType _colliderEventSubscriptions = ColliderEventType.None;
        private int colliderGeneration = -1;

        private Actor _actor;
        private Actor Actor
        {
            get
            {
                if (_actor == null)
                {
                    _actor = gameObject.GetComponent<Actor>();
                }
                return _actor;
            }
        }

        /// <inheritdoc />
        public bool IsEnabled { get; private set; } = true;

        /// <inheritdoc />
        public bool IsTrigger { get; private set; } = false;

        // /// <inheritdoc />
        //public CollisionLayer CollisionLayer { get; set; }

        /// <inheritdoc />
        public ColliderType Shape { get; private set; }

        // cannot be Auto
        private ColliderType _actualShape;

        internal void ApplyPatch(ColliderPatch patch)
        {
            IsEnabled = IsEnabled.GetPatchApplied(IsEnabled.ApplyPatch(patch.IsEnabled));
            IsTrigger = IsTrigger.GetPatchApplied(IsTrigger.ApplyPatch(patch.IsTrigger));

            PatchGeometry(patch);

            if (_collider != null)
            {
                _collider.enabled = IsEnabled;
                _collider.isTrigger = IsTrigger;
            }

            if (patch.EventSubscriptions != null)
            {
                // Clear existing subscription flags and set them to the new values.  We do not patch arrays,
                // and thus we will always send the entire value down for all of the subscriptions.
                _colliderEventSubscriptions = ColliderEventType.None;
                foreach (var sub in patch.EventSubscriptions)
                {
                    _colliderEventSubscriptions |= sub;
                }
            }
        }

        internal void SynchronizeEngine(ColliderPatch patch)
        {
            ApplyPatch(patch);
        }

        private void PatchGeometry(ColliderPatch patch)
        {
            if (patch == null || patch.Geometry == null)
            {
                return;
            }

            colliderGeneration++;
            Shape = patch.Geometry.Shape;

            // must wait for mesh load before auto type will work
            if (Shape == ColliderType.Auto)
            {
                if (Actor.App.AssetLoader.GetPreferredColliderShape(Actor.MeshId) == null)
                {
                    var runningGeneration = colliderGeneration;
                    var runningMeshId = Actor.MeshId;
                    MREAPI.AppsAPI.AssetCache.OnCached(runningMeshId, _ =>
                    {
                        if (runningMeshId != Actor.MeshId || runningGeneration != colliderGeneration) return;
                        PatchGeometry(patch);
                    });
                    return;
                }
                else
                {
                    patch.Geometry = Actor.App.AssetLoader.GetPreferredColliderShape(Actor.MeshId);
                }
            }

            if (_collider != null)
            {
                if (_actualShape == patch.Geometry.Shape)
                {
                    // We have a collider already of the same type as the desired new geometry.
                    // Update its values instead of removing and adding a new one.
                    patch.Geometry.Patch(_collider);
                    return;
                }
                else
                {
                    Destroy(_collider);
                    _collider = null;
                }
            }

            switch (patch.Geometry.Shape)
            {
                case ColliderType.Box:
                    var boxCollider = gameObject.AddComponent<BoxCollider>();
                    patch.Geometry.Patch(boxCollider);
                    _collider = boxCollider;
                    break;
                case ColliderType.Sphere:
                    var sphereCollider = gameObject.AddComponent<SphereCollider>();
                    patch.Geometry.Patch(sphereCollider);
                    _collider = sphereCollider;
                    break;
                case ColliderType.Capsule:
                    var capsuleCollider = gameObject.AddComponent<CapsuleCollider>();
                    patch.Geometry.Patch(capsuleCollider);
                    _collider = capsuleCollider;
                    break;
                case ColliderType.Mesh:
                    var meshCollider = gameObject.AddComponent<MeshCollider>();
                    meshCollider.convex = true;
                    patch.Geometry.Patch(meshCollider);
                    _collider = meshCollider;
                    break;
                default:
                    Actor.App.Logger.LogWarning("Cannot add the given collider type to the actor " +
                        $"during runtime.  Collider Type: {patch.Geometry.Shape}");
                    break;
            }

            _actualShape = patch.Geometry.Shape;
            _collider.enabled = IsEnabled;
            _collider.isTrigger = IsTrigger;
        }

        internal ColliderPatch GenerateInitialPatch()
        {
            ColliderGeometry colliderGeo = null;

            // Note: SDK has no "mesh" collider type
            if (Shape == ColliderType.Auto || Shape == ColliderType.Mesh)
            {
                colliderGeo = new AutoColliderGeometry();
            }
            else if (_collider is SphereCollider sphereCollider)
            {
                colliderGeo = new SphereColliderGeometry()
                {
                    Radius = sphereCollider.radius,
                    Center = sphereCollider.center.CreateMWVector3()
                };
            }
            else if (_collider is BoxCollider boxCollider)
            {
                colliderGeo = new BoxColliderGeometry()
                {
                    Size = boxCollider.size.CreateMWVector3(),
                    Center = boxCollider.center.CreateMWVector3()
                };
            }
            else if (_collider is CapsuleCollider capsuleCollider)
            {
                MWVector3 size;
                if (capsuleCollider.direction == 0)
                {
                    size = new MWVector3(capsuleCollider.height, 2 * capsuleCollider.radius, 2 * capsuleCollider.radius);
                }
                else if (capsuleCollider.direction == 1)
                {
                    size = new MWVector3(2 * capsuleCollider.radius, capsuleCollider.height, 2 * capsuleCollider.radius);
                }
                else
                {
                    size = new MWVector3(2 * capsuleCollider.radius, 2 * capsuleCollider.radius, capsuleCollider.height);
                }

                colliderGeo = new CapsuleColliderGeometry()
                {
                    Center = capsuleCollider.center.CreateMWVector3(),
                    Size = size
                };
            }
            else
            {
                Actor.App.Logger.LogWarning($"MRE SDK does not support the following Unity collider and will not " +
                    $"be available in the MRE app.  Collider Type: {_collider.GetType()}");
            }

            return colliderGeo == null ? null : new ColliderPatch()
            {
                IsEnabled = IsEnabled,
                IsTrigger = IsTrigger,
                Geometry = colliderGeo
            };
        }

        private void OnTriggerEnter(UnityCollider other)
        {
            if (_colliderEventSubscriptions.HasFlag(ColliderEventType.TriggerEnter))
            {
                SendTriggerEvent(ColliderEventType.TriggerEnter, other);
            }
        }

        private void OnTriggerExit(UnityCollider other)
        {
            if (_colliderEventSubscriptions.HasFlag(ColliderEventType.TriggerExit))
            {
                SendTriggerEvent(ColliderEventType.TriggerExit, other);
            }
        }

        private void OnCollisionEnter(UnityCollision collision)
        {
            if (_colliderEventSubscriptions.HasFlag(ColliderEventType.CollisionEnter))
            {
                SendCollisionEvent(ColliderEventType.CollisionEnter, collision);
            }
        }

        private void OnCollisionExit(UnityCollision collision)
        {
            if (_colliderEventSubscriptions.HasFlag(ColliderEventType.CollisionExit))
            {
                SendCollisionEvent(ColliderEventType.CollisionExit, collision);
            }
        }

        private void SendTriggerEvent(ColliderEventType eventType, UnityCollider otherCollider)
        {
            var otherActor = otherCollider.gameObject.GetComponent<Actor>();
            if (otherActor != null && otherActor.App.InstanceId == Actor.App.InstanceId)
            {
                Actor.App.EventManager.QueueEvent(
                    new TriggerEvent(Actor.Id, eventType, otherActor.Id));
            }
        }

        private void SendCollisionEvent(ColliderEventType eventType, UnityCollision collision)
        {
            var otherActor = collision.collider.gameObject.GetComponent<Actor>();
            if (otherActor != null && otherActor.App.InstanceId == Actor.App.InstanceId)
            {
                var sceneRoot = Actor.App.SceneRoot.transform;

                var contacts = collision.contacts.Select((contact) =>
                {
                    return new MREContactPoint()
                    {
                        Normal = sceneRoot.InverseTransformDirection(contact.normal).CreateMWVector3(),
                        Point = sceneRoot.InverseTransformPoint(contact.point).CreateMWVector3(),
                        Separation = contact.separation
                    };
                });

                var collisionData = new CollisionData()
                {
                    otherActorId = otherActor.Id,
                    Contacts = contacts,
                    Impulse = sceneRoot.InverseTransformDirection(collision.impulse).CreateMWVector3(),
                    RelativeVelocity = collision.relativeVelocity.CreateMWVector3()
                };

                Actor.App.EventManager.QueueEvent(
                    new CollisionEvent(Actor.Id, eventType, collisionData));
            }
        }
    }
}

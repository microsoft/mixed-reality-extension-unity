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
        private Actor _ownerActor;
        private ColliderEventType _colliderEventSubscriptions = ColliderEventType.None;

        /// <inheritdoc />
        public bool IsEnabled => _collider.enabled;

        /// <inheritdoc />
        public bool IsTrigger => _collider.isTrigger;

        // /// <inheritdoc />
        //public CollisionLayer CollisionLayer { get; set; }

        /// <inheritdoc />
        public ColliderType ColliderType { get; private set; }

        internal void Initialize(UnityCollider unityCollider)
        {
            _ownerActor = unityCollider.gameObject.GetComponent<Actor>()
                ?? throw new Exception("An MRE collider must be associated with a Unity game object that is an MRE actor.");
            _collider = unityCollider;
        }

        internal void ApplyPatch(ColliderPatch patch)
        {
            _collider.enabled = _collider.enabled.GetPatchApplied(IsEnabled.ApplyPatch(patch.IsEnabled));
            _collider.isTrigger = _collider.isTrigger.GetPatchApplied(IsTrigger.ApplyPatch(patch.IsTrigger));

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

        internal ColliderPatch GenerateInitialPatch()
        {
            ColliderGeometry colliderGeo = null;

            // Note: SDK has no "mesh" collider type
            if (ColliderType == ColliderType.Auto || ColliderType == ColliderType.Mesh)
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
                MREAPI.Logger.LogWarning($"MRE SDK does not support the following Unity collider and will not " +
                    $"be available in the MRE app.  Collider Type: {_collider.GetType()}");
            }

            return colliderGeo == null ? null : new ColliderPatch()
            {
                IsEnabled = _collider.enabled,
                IsTrigger = _collider.isTrigger,
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
            if (otherActor != null && otherActor.App.InstanceId == _ownerActor.App.InstanceId)
            {
                _ownerActor.App.EventManager.QueueEvent(
                    new TriggerEvent(_ownerActor.Id, eventType, otherActor.Id));
            }
        }

        private void SendCollisionEvent(ColliderEventType eventType, UnityCollision collision)
        {
            var otherActor = collision.collider.gameObject.GetComponent<Actor>();
            if (otherActor != null && otherActor.App.InstanceId == _ownerActor.App.InstanceId)
            {
                var sceneRoot = _ownerActor.App.SceneRoot.transform;

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

                _ownerActor.App.EventManager.QueueEvent(
                    new CollisionEvent(_ownerActor.Id, eventType, collisionData));
            }
        }
    }
}

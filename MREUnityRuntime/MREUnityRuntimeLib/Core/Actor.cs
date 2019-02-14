// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.API;
using MixedRealityExtension.Controllers;
using MixedRealityExtension.Core.Components;
using MixedRealityExtension.Core.Interfaces;
using MixedRealityExtension.Core.Types;
using MixedRealityExtension.Messaging.Commands;
using MixedRealityExtension.Messaging.Events.Types;
using MixedRealityExtension.Messaging.Payloads;
using MixedRealityExtension.Patching;
using MixedRealityExtension.Patching.Types;
using MixedRealityExtension.Util.Unity;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using UnityLight = UnityEngine.Light;

namespace MixedRealityExtension.Core
{
    /// <summary>
    /// Class that represents an actor in a mixed reality extension app.
    /// </summary>
    internal sealed class Actor : MixedRealityExtensionObject, ICommandHandlerContext, IActor
    {
        private Rigidbody _rigidbody;
        private UnityLight _light;
        private LookAtController _lookAtController;
        private float _nextUpdateTime;

        private Dictionary<Type, ActorComponentBase> _components = new Dictionary<Type, ActorComponentBase>();

        private Queue<Action<Actor>> _updateActions = new Queue<Action<Actor>>();

        private SubscriptionType _subscriptions = SubscriptionType.None;

        public override Vector3 LookAtPosition => transform.position;

        private new Renderer renderer = null;
        private Renderer Renderer => renderer = renderer ?? GetComponent<Renderer>();

        #region IActor Properties - Public

        /// <inheritdoc />
        [HideInInspector]
        public IActor Parent => transform.parent?.GetComponent<Actor>();

        /// <inheritdoc />
        [HideInInspector]
        public new string Name
        {
            get => transform.name;
            set => transform.name = value;
        }

        #endregion

        #region Properties - Internal

        internal Guid? ParentId { get; private set; }

        internal RigidBody RigidBody { get; private set; }

        internal Light Light { get; private set; }

        internal IText Text { get; private set; }

        internal MWTransform LocalTransform => transform.ToMWTransform();

        internal Guid? MaterialId { get; set; }
        private UnityEngine.Material originalMaterial;

        #endregion

        #region Methods - Internal

        internal ComponentT GetActorComponent<ComponentT>() where ComponentT : ActorComponentBase
        {
            if (_components.ContainsKey(typeof(ComponentT)))
            {
                return (ComponentT)_components[typeof(ComponentT)];
            }

            return null;
        }

        internal ComponentT GetOrCreateActorComponent<ComponentT>() where ComponentT : ActorComponentBase, new()
        {
            var component = GetActorComponent<ComponentT>();
            if (component == null)
            {
                component = gameObject.AddComponent<ComponentT>();
                component.AttachedActor = this;
                _components[typeof(ComponentT)] = component;
            }

            return component;
        }

        internal void SynchronizeApp()
        {
            if (CanSync())
            {
                // Handle changes in game state and raise appropriate events for network updates.
                var actorPatch = new ActorPatch(Id);

                // We need to detect for changes in parent on the client, and handle updating the server.
                var parentId = Parent?.Id ?? Guid.Empty;
                if (ParentId != parentId)
                {
                    // TODO @tombu - Determine if the new parent is an actor in OUR MRE.
                    // TODO: Add in MRE ID's to help identify whether the new parent is in our MRE or not, not just
                    // whether it is a MRE actor.
                    ParentId = parentId;
                    actorPatch.ParentId = ParentId;
                }

                if (ShouldSync(_subscriptions, SubscriptionType.Transform))
                {
                    actorPatch.Transform = SynchronizeTransform(gameObject.transform);
                }

                if (ShouldSync(_subscriptions, SubscriptionType.Rigidbody))
                {
                    GenerateRigidBodyPatch(actorPatch);
                }

                if (actorPatch.IsPatched())
                {
                    App.EventManager.QueueEvent(new ActorChangedEvent(Id, actorPatch));
                }
            }
        }

        internal void ApplyPatch(ActorPatch actorPatch)
        {
            PatchParent(actorPatch.ParentId);
            PatchName(actorPatch.Name);
            PatchMaterial(actorPatch.MaterialId);
            PatchTransform(actorPatch.Transform);
            PatchLight(actorPatch.Light);
            PatchRigidBody(actorPatch.RigidBody);
            PatchText(actorPatch.Text);
        }

        internal void SynchronizeEngine(ActorPatch actorPatch)
        {
            _updateActions.Enqueue((actor) => ApplyPatch(actorPatch));
        }

        internal void ExecuteRigidBodyCommands(RigidBodyCommands commandPayload)
        {
            foreach (var command in commandPayload.CommandPayloads.OfType<ICommandPayload>())
            {
                App.ExecuteCommandPayload(this, command);
            }
        }

        internal void Destroy()
        {
            CleanUp();
            Destroy(gameObject);
        }

        internal ActorPatch GeneratePatch(SubscriptionType? interests = null)
        {
            if (ParentId == null)
            {
                ParentId = Parent?.Id ?? Guid.Empty;
            }

            SubscriptionType subs = interests ?? _subscriptions;

            var transform = ((subs & SubscriptionType.Transform) != SubscriptionType.None) ?
                new TransformPatch()
                {
                    Position = new Vector3Patch(Transform.Position),
                    Rotation = new QuaternionPatch(Transform.Rotation),
                    Scale = new Vector3Patch(Transform.Scale)
                } : null;

            var rigidBody = ((subs & SubscriptionType.Rigidbody) != SubscriptionType.None) ?
                PatchingUtilMethods.GeneratePatch(RigidBody, (Rigidbody)null, App.SceneRoot.transform) : null;

            /*
            var light = ((subs & SubscriptionType.light) != SubscriptionType.none) ?
                UnityHelpers.ReadLight(_cachedLight) : null;
            */

            var actorPatch = new ActorPatch(Id)
            {
                ParentId = ParentId,
                Name = Name,
                Transform = transform,
                RigidBody = rigidBody,
                // Light = light
                // Text = text
                MaterialId = MaterialId
            };

            return (!actorPatch.IsPatched()) ? null : actorPatch;
        }

        internal OperationResult EnableRigidBody(RigidBodyPatch rigidBodyPatch)
        {
            if (AddRigidBody() != null)
            {
                if (rigidBodyPatch != null)
                {
                    PatchRigidBody(rigidBodyPatch);
                }

                return new OperationResult()
                {
                    ResultCode = OperationResultCode.Success
                };
            }

            return new OperationResult()
            {
                ResultCode = OperationResultCode.Error,
                Message = string.Format("Failed to create and enable the rigidbody for actor with id {0}", Id)
            };
        }

        internal OperationResult EnableLight(LightPatch lightPatch)
        {
            if (AddLight() != null)
            {
                if (lightPatch != null)
                {
                    PatchLight(lightPatch);
                }

                return new OperationResult()
                {
                    ResultCode = OperationResultCode.Success
                };
            }

            return new OperationResult()
            {
                ResultCode = OperationResultCode.Error,
                Message = string.Format("Failed to create and enable the light for actor with id {0}", Id)
            };
        }

        internal OperationResult EnableText(TextPatch textPatch)
        {
            if (AddText() != null)
            {
                if (textPatch != null)
                {
                    PatchText(textPatch);
                }

                return new OperationResult()
                {
                    ResultCode = OperationResultCode.Success
                };
            }

            return new OperationResult()
            {
                ResultCode = OperationResultCode.Error,
                Message = string.Format("Failed to create and enable the text object for actor with id {0}", Id)
            };
        }

        internal IActor GetParent()
        {
            return ParentId != null ? App.FindActor(ParentId.Value) : Parent;
        }

        internal void AddSubscriptions(IEnumerable<SubscriptionType> adds)
        {
            if (adds != null)
            {
                foreach (var subscription in adds)
                {
                    _subscriptions |= subscription;
                }
            }
        }

        internal void RemoveSubscriptions(IEnumerable<SubscriptionType> removes)
        {
            if (removes != null)
            {
                foreach (var subscription in removes)
                {
                    _subscriptions &= ~subscription;
                }
            }
        }

        internal void SendActorUpdate(SubscriptionType flags)
        {
            ActorPatch actorPatch = new ActorPatch(Id);

            if ((flags & SubscriptionType.Transform) != SubscriptionType.None)
            {
                actorPatch.Transform = transform.ToMWTransform().AsPatch();
            }

            //if ((flags & SubscriptionType.Rigidbody) != SubscriptionType.None)
            //{
            //    actorPatch.Transform = this.RigidBody.AsPatch();
            //}

            if (actorPatch.IsPatched())
            {
                App.EventManager.QueueEvent(new ActorChangedEvent(Id, actorPatch));
            }
        }

        #endregion

        #region MonoBehaviour Virtual Methods

        protected override void OnStart()
        {
            _rigidbody = gameObject.GetComponent<Rigidbody>();
            _light = gameObject.GetComponent<UnityLight>();
        }

        protected override void OnDestroyed()
        {
            // TODO @tombu, @eanders - We need to decide on the correct cleanup timing here for multiplayer, as this could cause a potential
            // memory leak if the engine deletes game objects, and we don't do proper cleanup here.
            //CleanUp();
            //App.OnActorDestroyed(this.Id);
        }

        protected override void InternalUpdate()
        {
            try
            {
                while (_updateActions.Count > 0)
                {
                    _updateActions.Dequeue()(this);
                }

                // TODO: Add ability to flag an actor for "high-frequency" updates
                if (Time.time >= _nextUpdateTime)
                {
                    _nextUpdateTime = Time.time + 0.2f + UnityEngine.Random.Range(-0.1f, 0.1f);
                    SynchronizeApp();
                }
            }
            catch (Exception e)
            {
                MREAPI.Logger.LogError($"Failed to synchronize app.  Exception: {e.Message}\nStackTrace: {e.StackTrace}");
            }
        }

        protected override void InternalFixedUpdate()
        {
            try
            {
                if (_rigidbody == null)
                {
                    return;
                }

                RigidBody = RigidBody ?? new RigidBody(_rigidbody, App.SceneRoot.transform);
                RigidBody.Update();
                // TODO: Send this update if actor is set to "high-frequency" updates
                //Actor.SynchronizeApp();
            }
            catch (Exception e)
            {
                MREAPI.Logger.LogError($"Failed to update rigid body.  Exception: {e.Message}\nStackTrace: {e.StackTrace}");
            }
        }

        #endregion

        #region Methods - Private

        private IText AddText()
        {
            Text = MREAPI.AppsAPI.TextFactory.CreateText(this);
            return Text;
        }

        private Light AddLight()
        {
            if (_light == null)
            {
                _light = gameObject.AddComponent<UnityLight>();
                Light = new Light(_light);
            }
            return Light;
        }

        private RigidBody AddRigidBody()
        {
            if (_rigidbody == null)
            {
                _rigidbody = gameObject.AddComponent<Rigidbody>();
                RigidBody = new RigidBody(_rigidbody, App.SceneRoot.transform);
            }
            return RigidBody;
        }

        private void PatchParent(Guid? parentIdOrNull)
        {
            var parentId = parentIdOrNull ?? ParentId;
            var parent = parentId != null ? App.FindActor(parentId.Value) : Parent;
            if (parent != null && (Parent == null || (Parent.Id != parent.Id)))
            {
                transform.SetParent(((Actor)parent).transform, false);
            }
            else if (parent == null && Parent != null)
            {
                // TODO: Unparent?
            }
        }

        private void PatchName(string nameOrNull)
        {
            if (nameOrNull != null)
            {
                Name = nameOrNull;
            }
        }

        private void PatchMaterial(Guid? materialIdOrNull)
        {
            if (Renderer != null)
            {
                if (originalMaterial == null)
                {
                    originalMaterial = Instantiate(Renderer.sharedMaterial);
                }

                if (materialIdOrNull == Guid.Empty)
                {
                    Renderer.sharedMaterial = originalMaterial;
                }
                else if (materialIdOrNull != null)
                {
                    MaterialId = materialIdOrNull.Value;
                    var sharedMat = MREAPI.AppsAPI.AssetCache.GetAsset(MaterialId) as Material;
                    if (sharedMat != null)
                    {
                        Renderer.sharedMaterial = sharedMat;
                    }
                    else
                    {
                        MREAPI.Logger.LogWarning($"Material {MaterialId} not found, cannot assign to actor {Id}");
                    }
                }
            }
        }

        private void PatchTransform(TransformPatch transformPatch)
        {
            if (transformPatch != null)
            {
                if (RigidBody == null)
                {
                    transform.localPosition = transform.localPosition.GetPatchApplied(LocalTransform.Position.ApplyPatch(transformPatch.Position));
                    transform.localRotation = transform.localRotation.GetPatchApplied(LocalTransform.Rotation.ApplyPatch(transformPatch.Rotation));
                    transform.localScale = transform.localScale.GetPatchApplied(LocalTransform.Scale.ApplyPatch(transformPatch.Scale));
                }
                else
                {
                    // In case of rigid body:
                    // - Apply scale directly.
                    transform.localScale = transform.localScale.GetPatchApplied(LocalTransform.Scale.ApplyPatch(transformPatch.Scale));
                    // - Apply position and rotation via rigid body.
                    var position = transform.localPosition.GetPatchApplied(LocalTransform.Position.ApplyPatch(transformPatch.Position));
                    var rotation = transform.localRotation.GetPatchApplied(LocalTransform.Rotation.ApplyPatch(transformPatch.Rotation));
                    RigidBodyPatch rigidBodyPatch = new RigidBodyPatch()
                    {
                        Position = new Vector3Patch(position),
                        Rotation = new QuaternionPatch(rotation)
                    };
                    // Queue update to happen in the fixed update
                    RigidBody.SynchronizeEngine(rigidBodyPatch);
                }
            }
        }

        private void PatchLight(LightPatch lightPatch)
        {
            if (lightPatch != null)
            {
                if (Light == null)
                {
                    AddLight();
                }
                Light.SynchronizeEngine(lightPatch);
            }
        }

        private void PatchRigidBody(RigidBodyPatch rigidBodyPatch)
        {
            if (rigidBodyPatch != null)
            {
                if (RigidBody == null)
                {
                    AddRigidBody();
                    RigidBody.ApplyPatch(rigidBodyPatch);
                }
                else
                {
                    // Queue update to happen in the fixed update
                    RigidBody.SynchronizeEngine(rigidBodyPatch);
                }
            }

        }

        private void PatchText(TextPatch textPatch)
        {
            if (textPatch != null)
            {
                if (Text == null)
                {
                    AddText();
                }
                Text.SynchronizeEngine(textPatch);
            }
        }

        private void GenerateRigidBodyPatch(ActorPatch actorPatch)
        {
            if (_rigidbody != null && RigidBody != null)
            {
                // convert to a RigidBody and build a patch from the old one to this one.
                var rigidBodyPatch = PatchingUtilMethods.GeneratePatch(RigidBody, _rigidbody, App.SceneRoot.transform);
                if (rigidBodyPatch != null && rigidBodyPatch.IsPatched())
                {
                    actorPatch.RigidBody = rigidBodyPatch;
                }

                RigidBody.Update(_rigidbody);
            }
        }

        private void CleanUp()
        {
            foreach (var component in _components.Values)
            {
                component.CleanUp();
            }
        }

        /*
        private void GenerateLightPatch(ActorPatch actorPatch)
        {
            var lightPatch = PatchingUtilMethods.GeneratePatch(_light, EngineActor.Light);
            if (lightPatch != null && lightPatch.IsPatched())
            {
                actorPatch.Light = lightPatch;
            }

            _light = _light ?? new Light();
            _light.Update(EngineActor.Light);
        }
        */

        private bool ShouldSync(SubscriptionType subscriptions, SubscriptionType flag)
        {
            // We do not want to send actor updates until we're fully joined to the app.
            // TODO: We shouldn't need to do this check. The engine shouldn't try to send
            // updates until we're fully joined to the app.
            if (!(App.Protocol is Messaging.Protocols.Execution))
            {
                return false;
            }

            // If we have a rigid body then sync the transform.
            if (RigidBody != null)
            {
                subscriptions |= SubscriptionType.Transform;
                subscriptions |= SubscriptionType.Rigidbody;
            }

            if ((subscriptions & flag) != SubscriptionType.None)
            {
                return
                    (App.OperatingModel == OperatingModel.ServerAuthoritative) ||
                    (App.OperatingModel == OperatingModel.PeerAuthoritative && App.IsAuthoritativePeer);
            }

            return false;
        }

        private bool CanSync()
        {
            // We do not want to send actor updates until we're fully joined to the app.
            // TODO: We shouldn't need to do this check. The engine shouldn't try to send
            // updates until we're fully joined to the app.
            if (!(App.Protocol is Messaging.Protocols.Execution))
            {
                return false;
            }

            // We can send actor updates to the app if we're operating in a server-authoritative model,
            // or if we're in a peer-authoritative model and we've been designated the authoritative peer.
            // Note: This is just a hint to the system to reduce the amount of overall network traffic sent
            // to theapp, since the app only needs to receive updates from one of the connected peers. The
            // app checks in
            if (App.OperatingModel == OperatingModel.ServerAuthoritative || App.IsAuthoritativePeer)
            {
                return true;
            }

            return false;
        }

        internal void LookAt(Guid targetId, LookAtMode lookAtMode)
        {
            IMixedRealityExtensionObject targetObject = null;

            if (lookAtMode != LookAtMode.None)
            {
                if (targetObject == null)
                {
                    targetObject = App.FindUser(targetId);
                }
                if (targetObject == null)
                {
                    targetObject = App.FindActor(targetId);
                }

                if (_lookAtController == null)
                {
                    _lookAtController = gameObject.AddComponent<LookAtController>();
                }
            }

            if (_lookAtController != null)
            {
                _lookAtController.Configure(targetObject, lookAtMode);
            }
        }

        #endregion

        #region Command Handlers - Rigid Body Commands

        [CommandHandler(typeof(RBMovePosition))]
        private void OnRBMovePosition(RBMovePosition payload)
        {
            RigidBody?.RigidBodyMovePosition(new MWVector3().ApplyPatch(payload.Position));
        }

        [CommandHandler(typeof(RBMoveRotation))]
        private void OnRBMoveRotation(RBMoveRotation payload)
        {
            RigidBody?.RigidBodyMoveRotation(new MWQuaternion().ApplyPatch(payload.Rotation));
        }

        [CommandHandler(typeof(RBAddForce))]
        private void OnRBAddForce(RBAddForce payload)
        {
            RigidBody?.RigidBodyAddForce(new MWVector3().ApplyPatch(payload.Force));
        }

        [CommandHandler(typeof(RBAddForceAtPosition))]
        private void OnRBAddForceAtPosition(RBAddForceAtPosition payload)
        {
            var force = new MWVector3().ApplyPatch(payload.Force);
            var position = new MWVector3().ApplyPatch(payload.Position);
            RigidBody?.RigidBodyAddForceAtPosition(force, position);
        }

        [CommandHandler(typeof(RBAddTorque))]
        private void OnRBAddTorque(RBAddTorque payload)
        {
            RigidBody?.RigidBodyAddTorque(new MWVector3().ApplyPatch(payload.Torque));
        }

        [CommandHandler(typeof(RBAddRelativeTorque))]
        private void OnRBAddRelativeTorque(RBAddRelativeTorque payload)
        {
            RigidBody?.RigidBodyAddRelativeTorque(new MWVector3().ApplyPatch(payload.RelativeTorque));
        }

        #endregion
    }
}

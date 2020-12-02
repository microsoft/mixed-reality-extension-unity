// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using MixedRealityExtension.Animation;
using MixedRealityExtension.API;
using MixedRealityExtension.App;
using MixedRealityExtension.Behaviors;
using MixedRealityExtension.Behaviors.Actions;
using MixedRealityExtension.Core.Components;
using MixedRealityExtension.Core.Interfaces;
using MixedRealityExtension.Core.Types;
using MixedRealityExtension.Messaging;
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
using UnityCollider = UnityEngine.Collider;
using MixedRealityExtension.PluginInterfaces.Behaviors;
using MixedRealityExtension.Util;
using IVideoPlayer = MixedRealityExtension.PluginInterfaces.IVideoPlayer;
using MixedRealityExtension.Behaviors.Contexts;
using MixedRealityExtension.PluginInterfaces;

namespace MixedRealityExtension.Core
{
	/// <summary>
	/// Class that represents an actor in a mixed reality extension app.
	/// </summary>
	internal sealed class Actor : MixedRealityExtensionObject, ICommandHandlerContext, IActor
	{
		private Rigidbody _rigidbody;
		private UnityLight _light;
		private UnityCollider _collider;
		private ColliderPatch _pendingColliderPatch;
		private LookAtComponent _lookAt;
		class MediaInstance
		{
			public Guid MediaAssetId { get; }

			//null until asset has finished loading
			public System.Object Instance { get; set; }

			public MediaInstance(Guid MediaAssetId)
			{
				this.MediaAssetId = MediaAssetId;
			}
		};

		private Dictionary<Guid, MediaInstance> _mediaInstances;
		private float _nextUpdateTime;
		private bool _grabbedLastSync = false;

		private MWScaledTransform _localTransform;
		private MWTransform _appTransform;

		private TransformLerper _transformLerper;

		private Dictionary<Type, ActorComponentBase> _components = new Dictionary<Type, ActorComponentBase>();

		private ActorComponentType _subscriptions = ActorComponentType.None;

		private ActorTransformPatch _rbTransformPatch;

		private new Renderer renderer = null;
		internal Renderer Renderer
		{
			get
			{
				if (renderer == null)
				{
					var t = GetComponent<Renderer>();
					renderer = t;
				}
				return renderer;
			}
		}
		private MeshFilter meshFilter = null;
		internal MeshFilter MeshFilter
		{
			get
			{
				if (meshFilter == null)
				{
					var t = GetComponent<MeshFilter>();
					meshFilter = t;
				}
				return meshFilter;
			}
		}

		/// <summary>
		/// Checks if rigid body is simulated locally.
		/// </summary>
		internal bool IsSimulatedByLocalUser
		{
			get
			{
				return _isExclusiveToUser
					|| Owner.HasValue && App.LocalUser != null && Owner.Value == App.LocalUser.Id;
			}
		}

		private bool _isExclusiveToUser = false;

		#region IActor Properties - Public

		/// <inheritdoc />
		[HideInInspector]
		public IActor Parent => App.FindActor(ParentId);

		/// <inheritdoc />
		[HideInInspector]
		public new string Name
		{
			get => transform.name;
			set => transform.name = value;
		}

		private Guid? Owner = null;

		/// <inheritdoc />
		IMixedRealityExtensionApp IActor.App => base.App;

		/// <inheritdoc />
		[HideInInspector]
		public MWScaledTransform LocalTransform
		{
			get
			{
				if (_localTransform == null)
				{
					_localTransform = new MWScaledTransform();
					_localTransform.ToLocalTransform(transform);
				}

				return _localTransform;
			}

			private set
			{
				_localTransform = value;
			}
		}

		/// <inheritdoc />
		[HideInInspector]
		public MWTransform AppTransform
		{
			get
			{
				if (_appTransform == null)
				{
					_appTransform = new MWTransform();
					_appTransform.ToAppTransform(transform, App.SceneRoot.transform);
				}

				return _appTransform;
			}

			private set
			{
				_appTransform = value;
			}
		}

		#endregion

		#region Properties - Internal

		internal Guid ParentId { get; set; } = Guid.Empty;

		internal RigidBody RigidBody { get; private set; }

		internal Light Light { get; private set; }

		internal IText Text { get; private set; }

		internal Collider Collider { get; private set; }

		internal Attachment Attachment { get; } = new Attachment();
		private Attachment _cachedAttachment = new Attachment();

		private Guid _materialId = Guid.Empty;
		private bool ListeningForMaterialChanges = false;

		internal Guid MaterialId
		{
			get
			{
				return _materialId;
			}
			set
			{
				_materialId = value;
				if (!Renderer) return;

				// look up and assign material, or default if none assigned
				if (_materialId != Guid.Empty)
				{
					var updatedMaterialId = _materialId;
					App.AssetManager.OnSet(_materialId, sharedMat =>
					{
						if (!this || !Renderer || _materialId != updatedMaterialId) return;

						Renderer.sharedMaterial = (Material)sharedMat.Asset ?? MREAPI.AppsAPI.DefaultMaterial;

						// keep this material up to date
						if (!ListeningForMaterialChanges)
						{
							App.AssetManager.AssetReferenceChanged += CheckMaterialReferenceChanged;
							ListeningForMaterialChanges = true;
						}
					});
				}
				else
				{
					Renderer.sharedMaterial = MREAPI.AppsAPI.DefaultMaterial;
					if (ListeningForMaterialChanges)
					{
						App.AssetManager.AssetReferenceChanged -= CheckMaterialReferenceChanged;
						ListeningForMaterialChanges = false;
					}
				}
			}
		}

		internal Guid MeshId { get; set; } = Guid.Empty;

		internal Mesh UnityMesh
		{
			get
			{
				if (Renderer is SkinnedMeshRenderer skinned)
				{
					return skinned.sharedMesh;
				}
				else if (MeshFilter != null)
				{
					return MeshFilter.sharedMesh;
				}
				else
				{
					return null;
				}
			}
			set
			{
				if (Renderer is SkinnedMeshRenderer skinned)
				{
					skinned.sharedMesh = value;
				}
				else
				{
					MeshFilter.sharedMesh = value;
				}
			}
		}

		internal bool Grabbable { get; private set; }

		internal bool IsGrabbed
		{
			get
			{
				var behaviorComponent = GetActorComponent<BehaviorComponent>();
				if (behaviorComponent != null && behaviorComponent.Behavior is ITargetBehavior targetBehavior)
				{
					return targetBehavior.IsGrabbed;
				}

				return false;
			}
		}

		internal UInt32 appearanceEnabled = UInt32.MaxValue;
		internal bool activeAndEnabled
		{
			get
			{
				bool parentEnabled = (Parent != null) ? (Parent as Actor).activeAndEnabled : true;
				uint userGroups = (App.LocalUser?.Groups ?? 1);
				return parentEnabled && (userGroups & appearanceEnabled) > 0;
			}
		}

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

		internal void SynchronizeApp(ActorComponentType? subscriptionsOverride = null)
		{
			if (CanSync())
			{
				var subscriptions = subscriptionsOverride.HasValue ? subscriptionsOverride.Value : _subscriptions;

				// Handle changes in game state and raise appropriate events for network updates.
				var actorPatch = new ActorPatch(Id);

				// We need to detect for changes in parent on the client, and handle updating the server.
				// But only update if the identified parent is not pending.
				var parentId = (Parent != null) ? Parent.Id : Guid.Empty;
				if (ParentId != parentId && App.FindActor(ParentId) != null)
				{
					// TODO @tombu - Determine if the new parent is an actor in OUR MRE.
					// TODO: Add in MRE ID's to help identify whether the new parent is in our MRE or not, not just
					// whether it is a MRE actor.
					ParentId = parentId;
					actorPatch.ParentId = ParentId;
				}

				if (!App.UsePhysicsBridge || RigidBody == null)
				{
					if (ShouldSync(subscriptions, ActorComponentType.Transform))
					{
						GenerateTransformPatch(actorPatch);
					}
				}

				if (ShouldSync(subscriptions, ActorComponentType.Rigidbody))
				{
					// we should include the velocities either when the old sync model is used
					// OR when there is an explicit subscription to it.
					GenerateRigidBodyPatch(actorPatch,
						(!App.UsePhysicsBridge || subscriptions.HasFlag(ActorComponentType.RigidbodyVelocity)));
				}

				if (ShouldSync(ActorComponentType.Attachment, ActorComponentType.Attachment))
				{
					GenerateAttachmentPatch(actorPatch);
				}

				if (actorPatch.IsPatched())
				{
					App.EventManager.QueueEvent(new ActorChangedEvent(Id, actorPatch));
				}

				// If the actor is grabbed or was grabbed last time we synced and is not grabbed any longer,
				// then we always need to sync the transform.
				if (IsGrabbed || _grabbedLastSync)
				{
					var appTransform = new MWTransform();
					appTransform.ToAppTransform(transform, App.SceneRoot.transform);

					var actorCorrection = new ActorCorrection()
					{
						ActorId = Id,
						AppTransform = appTransform
					};

					App.EventManager.QueueEvent(new ActorCorrectionEvent(Id, actorCorrection));
				}

				// We update whether the actor was grabbed this sync to ensure we send one last transform update
				// on the sync when they are no longer grabbed.  This is the final transform update after the grab
				// is completed.  This should always be cached at the very end of the sync to ensure the value is valid
				// for any test calls to ShouldSync above.
				_grabbedLastSync = IsGrabbed;
			}
		}

		internal void ApplyPatch(ActorPatch actorPatch)
		{
			PatchExclusive(actorPatch.ExclusiveToUser);
			PatchName(actorPatch.Name);
			PatchOwner(actorPatch.Owner);
			PatchParent(actorPatch.ParentId);
			PatchAppearance(actorPatch.Appearance);
			PatchTransform(actorPatch.Transform);
			PatchLight(actorPatch.Light);
			PatchRigidBody(actorPatch.RigidBody);
			PatchCollider(actorPatch.Collider);
			PatchText(actorPatch.Text);
			PatchAttachment(actorPatch.Attachment);
			PatchLookAt(actorPatch.LookAt);
			PatchGrabbable(actorPatch.Grabbable);
			PatchSubscriptions(actorPatch.Subscriptions);
		}

		internal void ApplyCorrection(ActorCorrection actorCorrection)
		{
			CorrectAppTransform(actorCorrection.AppTransform);
		}

		internal void SynchronizeEngine(ActorPatch actorPatch)
		{
			ApplyPatch(actorPatch);
		}

		internal void EngineCorrection(ActorCorrection actorCorrection)
		{
			ApplyCorrection(actorCorrection);
		}

		internal void ExecuteRigidBodyCommands(RigidBodyCommands commandPayload, Action onCompleteCallback)
		{
			foreach (var command in commandPayload.CommandPayloads.OfType<ICommandPayload>())
			{
				App.ExecuteCommandPayload(this, command, null);
			}
			onCompleteCallback?.Invoke();
		}

		internal void Destroy()
		{
			CleanUp();

			Destroy(gameObject);
		}

		internal ActorPatch GeneratePatch(ActorPatch output = null, TargetPath path = null)
		{
			if (output == null)
			{
				output = new ActorPatch(Id);
			}

			var generateAll = path == null;
			if (!generateAll)
			{
				if (path.AnimatibleType != "actor") return output;
				output.Restore(path, 0);
			}
			else
			{
				output.RestoreAll();
			}

			if (generateAll || path.PathParts[0] == "transform")
			{
				if (generateAll || path.PathParts[1] == "local")
				{
					LocalTransform.ToLocalTransform(transform);
					if (generateAll || path.PathParts[2] == "position")
					{
						var localPos = transform.localPosition;
						if (generateAll || path.PathParts.Length == 3 || path.PathParts[3] == "x")
						{
							output.Transform.Local.Position.X = localPos.x;
						}
						if (generateAll || path.PathParts.Length == 3 || path.PathParts[3] == "y")
						{
							output.Transform.Local.Position.Y = localPos.y;
						}
						if (generateAll || path.PathParts.Length == 3 || path.PathParts[3] == "z")
						{
							output.Transform.Local.Position.Z = localPos.z;
						}
					}
					if (generateAll || path.PathParts[2] == "rotation")
					{
						var localRot = transform.localRotation;
						output.Transform.Local.Rotation.X = localRot.x;
						output.Transform.Local.Rotation.Y = localRot.y;
						output.Transform.Local.Rotation.Z = localRot.z;
						output.Transform.Local.Rotation.W = localRot.w;
					}
					if (generateAll || path.PathParts[2] == "scale")
					{
						var localScale = transform.localScale;
						if (generateAll || path.PathParts.Length == 3 || path.PathParts[3] == "x")
						{
							output.Transform.Local.Scale.X = localScale.x;
						}
						if (generateAll || path.PathParts.Length == 3 || path.PathParts[3] == "y")
						{
							output.Transform.Local.Scale.Y = localScale.y;
						}
						if (generateAll || path.PathParts.Length == 3 || path.PathParts[3] == "z")
						{
							output.Transform.Local.Scale.Z = localScale.z;
						}
					}
				}
				if (generateAll || path.PathParts[1] == "app")
				{
					AppTransform.ToAppTransform(transform, App.SceneRoot.transform);
					if (generateAll || path.PathParts[2] == "position")
					{
						if (generateAll || path.PathParts.Length == 3 || path.PathParts[3] == "x")
						{
							output.Transform.App.Position.X = AppTransform.Position.X;
						}
						if (generateAll || path.PathParts.Length == 3 || path.PathParts[3] == "y")
						{
							output.Transform.App.Position.Y = AppTransform.Position.Y;
						}
						if (generateAll || path.PathParts.Length == 3 || path.PathParts[3] == "z")
						{
							output.Transform.App.Position.Z = AppTransform.Position.Z;
						}
					}
					if (generateAll || path.PathParts[2] == "rotation")
					{
						var localRot = transform.localRotation;
						output.Transform.App.Rotation.X = AppTransform.Rotation.X;
						output.Transform.App.Rotation.Y = AppTransform.Rotation.Y;
						output.Transform.App.Rotation.Z = AppTransform.Rotation.Z;
						output.Transform.App.Rotation.W = AppTransform.Rotation.W;
					}
				}
			}

			if (generateAll)
			{
				var rigidBody = PatchingUtilMethods.GeneratePatch(RigidBody, (Rigidbody)null,
					App.SceneRoot.transform, !App.UsePhysicsBridge);

				ColliderPatch collider = null;
				_collider = gameObject.GetComponent<UnityCollider>();
				if (_collider != null)
				{
					if (Collider == null)
					{
						Collider = gameObject.AddComponent<Collider>();
					}
					Collider.Initialize(_collider);
					collider = Collider.GenerateInitialPatch();
				}

				output.ParentId = ParentId;
				output.Name = Name;
				output.RigidBody = rigidBody;
				output.Collider = collider;
				output.Appearance = new AppearancePatch()
				{
					Enabled = appearanceEnabled,
					MaterialId = MaterialId,
					MeshId = MeshId
				};
			}

			return output;
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

		// These two variables are for local use in the SendActorUpdate method to prevent unnecessary allocations.  Their
		// user should be limited to this function.
		private MWScaledTransform __methodVar_localTransform = new MWScaledTransform();
		private MWTransform __methodVar_appTransform = new MWTransform();
		internal void SendActorUpdate(ActorComponentType flags)
		{
			ActorPatch actorPatch = new ActorPatch(Id);

			if (flags.HasFlag(ActorComponentType.Transform))
			{
				__methodVar_localTransform.ToLocalTransform(transform);
				__methodVar_appTransform.ToAppTransform(transform, App.SceneRoot.transform);

				actorPatch.Transform = new ActorTransformPatch()
				{
					Local = __methodVar_localTransform.AsPatch(),
					App = __methodVar_appTransform.AsPatch()
				};
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

			IHostAppUser hostAppUser = App.FindUser(Attachment.UserId)?.HostAppUser;
			if (hostAppUser != null)
			{
				hostAppUser.BeforeAvatarDestroyed -= UserInfo_BeforeAvatarDestroyed;
			}

			if (_mediaInstances != null)
			{
				foreach (KeyValuePair<Guid, MediaInstance> mediaInstance in _mediaInstances)
				{
					DestroyMediaById(mediaInstance.Key, mediaInstance.Value);
				}
			}

			if (App.UsePhysicsBridge)
			{
				if (RigidBody != null)
				{
					App.PhysicsBridge.removeRigidBody(Id);
				}
			}

			if (ListeningForMaterialChanges)
			{
				App.AssetManager.AssetReferenceChanged -= CheckMaterialReferenceChanged;
			}
		}

		protected override void InternalUpdate()
		{
			try
			{
				// TODO: Add ability to flag an actor for "high-frequency" updates
				if (Time.time >= _nextUpdateTime)
				{
					_nextUpdateTime = Time.time + 0.2f + UnityEngine.Random.Range(-0.1f, 0.1f);
					SynchronizeApp();

					// Give components the opportunity to synchronize the app.
					foreach (var component in _components.Values)
					{
						component.SynchronizeComponent();
					}
				}
			}
			catch (Exception e)
			{
				App?.Logger.LogError($"Failed to synchronize app.  Exception: {e.Message}\nStackTrace: {e.StackTrace}");
			}

			_transformLerper?.Update();
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
				App.Logger.LogError($"Failed to update rigid body.  Exception: {e.Message}\nStackTrace: {e.StackTrace}");
			}
		}

		#endregion

		#region Methods - Private

		private Attachment FindAttachmentInHierarchy()
		{
			Attachment FindAttachmentRecursive(Actor actor)
			{
				if (actor == null)
				{
					return null;
				}
				if (actor.Attachment.AttachPoint != null && actor.Attachment.UserId != Guid.Empty)
				{
					return actor.Attachment;
				}
				return FindAttachmentRecursive(actor.Parent as Actor);
			};
			return FindAttachmentRecursive(this);
		}

		private void DetachFromAttachPointParent()
		{
			try
			{
				if (transform != null)
				{
					var attachmentComponent = transform.parent.GetComponents<MREAttachmentComponent>()
						.FirstOrDefault(component =>
							component.Actor != null &&
							component.Actor.Id == Id &&
							component.Actor.AppInstanceId == AppInstanceId &&
							component.UserId == _cachedAttachment.UserId);

					if (attachmentComponent != null)
					{
						attachmentComponent.Actor = null;
						Destroy(attachmentComponent);

						var parent = Parent != null ? (Parent as Actor).transform : App.SceneRoot.transform;
						transform.SetParent(parent, false);
					}
				}
			}
			catch (Exception e)
			{
				App.Logger.LogError($"Exception: {e.Message}\nStackTrace: {e.StackTrace}");
			}
		}

		private bool PerformAttach()
		{
			// Assumption: Attachment state has changed and we need to (potentially) detach and (potentially) reattach.
			try
			{
				DetachFromAttachPointParent();

				IHostAppUser hostAppUser = App.FindUser(Attachment.UserId)?.HostAppUser;
				if (hostAppUser != null &&
					(Attachment.UserId != App.LocalUser?.Id || App.GrantedPermissions.HasFlag(Permissions.UserInteraction)))
				{
					hostAppUser.BeforeAvatarDestroyed -= UserInfo_BeforeAvatarDestroyed;

					Transform attachPoint = hostAppUser.GetAttachPoint(Attachment.AttachPoint);
					if (attachPoint != null)
					{
						var attachmentComponent = attachPoint.gameObject.AddComponent<MREAttachmentComponent>();
						attachmentComponent.Actor = this;
						attachmentComponent.UserId = Attachment.UserId;
						transform.SetParent(attachPoint, false);
						hostAppUser.BeforeAvatarDestroyed += UserInfo_BeforeAvatarDestroyed;
						return true;
					}
				}
			}
			catch (Exception e)
			{
				App.Logger.LogError($"Exception: {e.Message}\nStackTrace: {e.StackTrace}");
			}

			return false;
		}

		private void UserInfo_BeforeAvatarDestroyed()
		{
			// Remember the original local transform.
			MWScaledTransform cachedTransform = LocalTransform;

			// Detach from parent. This will preserve the world transform (changing the local transform).
			// This is desired so that the actor doesn't change position, but we must restore the local
			// transform when reattaching.
			DetachFromAttachPointParent();

			IHostAppUser hostAppUser = App.FindUser(Attachment.UserId)?.HostAppUser;
			if (hostAppUser != null)
			{
				void Reattach()
				{
					// Restore the local transform and reattach.
					hostAppUser.AfterAvatarCreated -= Reattach;
					// In the interim time this actor might have been destroyed.
					if (transform != null)
					{
						transform.localPosition = cachedTransform.Position.ToVector3();
						transform.localRotation = cachedTransform.Rotation.ToQuaternion();
						transform.localScale = cachedTransform.Scale.ToVector3();
						PerformAttach();
					}
				}

				// Register for a callback once the avatar is recreated.
				hostAppUser.AfterAvatarCreated += Reattach;
			}
		}

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

		void OnRigidBodyGrabbed(object sender, ActionStateChangedArgs args)
		{
			if (App.UsePhysicsBridge)
			{
				if (args.NewState != ActionState.Performing)
				{
					if (_isExclusiveToUser)
					{
						// if rigid body is exclusive to user, manage rigid body directly
						_rigidbody.isKinematic = args.NewState == ActionState.Started ? true : RigidBody.IsKinematic;
					}
					else
					{
						// if rigid body needs to be synchronized, handle it through physics bridge
						if (args.NewState == ActionState.Started)
						{
							// set to kinematic when grab starts
							App.PhysicsBridge.setKeyframed(Id, true);
						}
						else
						{
							// on end of grab, return to original value
							App.PhysicsBridge.setKeyframed(Id, RigidBody.IsKinematic);
						}
					}
				}
			}
		}

		private RigidBody AddRigidBody()
		{
			if (_rigidbody == null)
			{
				_rigidbody = gameObject.AddComponent<Rigidbody>();
				RigidBody = new RigidBody(_rigidbody, App.SceneRoot.transform);

				if (App.UsePhysicsBridge)
				{
					// Add rigid body to physics bridge only when source is known.
					// Otherwise, do it once source is provided.
					if (Owner.HasValue && !_isExclusiveToUser)
					{
						App.PhysicsBridge.addRigidBody(Id, _rigidbody, Owner.Value, RigidBody.IsKinematic);
					}
				}

				var behaviorComponent = GetActorComponent<BehaviorComponent>();
				if (behaviorComponent != null && behaviorComponent.Context is TargetBehaviorContext targetContext)
				{
					var targetBehavior = (ITargetBehavior)targetContext.Behavior;
					if (targetBehavior.Grabbable)
					{
						targetContext.GrabAction.ActionStateChanged += OnRigidBodyGrabbed;
					}
				}
			}
			return RigidBody;
		}

		/// <summary>
		/// Precondition: The mesh referred to by MeshId is loaded and available for use.
		/// </summary>
		/// <param name="colliderPatch"></param>
		private void SetCollider(ColliderPatch colliderPatch)
		{
			if (colliderPatch == null || colliderPatch.Geometry == null)
			{
				return;
			}

			var colliderGeometry = colliderPatch.Geometry;
			var colliderType = colliderGeometry.Shape;

			if (colliderType == ColliderType.Auto)
			{
				colliderGeometry = App.AssetManager.GetById(MeshId).Value.ColliderGeometry;
				colliderType = colliderGeometry.Shape;
			}

			if (_collider != null)
			{
				if (Collider.Shape == colliderType)
				{
					// We have a collider already of the same type as the desired new geometry.
					// Update its values instead of removing and adding a new one.
					colliderGeometry.Patch(App, _collider);
					return;
				}
				else
				{
					Destroy(_collider);
					_collider = null;
				}
			}

			UnityCollider unityCollider = null;

			switch (colliderType)
			{
				case ColliderType.Box:
					var boxCollider = gameObject.AddComponent<BoxCollider>();
					colliderGeometry.Patch(App, boxCollider);
					unityCollider = boxCollider;
					break;
				case ColliderType.Sphere:
					var sphereCollider = gameObject.AddComponent<SphereCollider>();
					colliderGeometry.Patch(App, sphereCollider);
					unityCollider = sphereCollider;
					break;
				case ColliderType.Capsule:
					var capsuleCollider = gameObject.AddComponent<CapsuleCollider>();
					colliderGeometry.Patch(App, capsuleCollider);
					unityCollider = capsuleCollider;
					break;
				case ColliderType.Mesh:
					var meshCollider = gameObject.AddComponent<MeshCollider>();
					colliderGeometry.Patch(App, meshCollider);
					unityCollider = meshCollider;
					break;
				default:
					App.Logger.LogWarning("Cannot add the given collider type to the actor " +
						$"during runtime.  Collider Type: {colliderPatch.Geometry.Shape}");
					break;
			}

			_collider = unityCollider;

			// update bounciness and frictions 
			if (colliderPatch.Bounciness.HasValue)
				_collider.material.bounciness = colliderPatch.Bounciness.Value;
			if (colliderPatch.StaticFriction.HasValue)
				_collider.material.staticFriction = colliderPatch.StaticFriction.Value;
			if (colliderPatch.DynamicFriction.HasValue)
				_collider.material.dynamicFriction = colliderPatch.DynamicFriction.Value;

			if (Collider == null)
			{
				Collider = gameObject.AddComponent<Collider>();
			}
			Collider.Initialize(_collider, colliderPatch.Geometry.Shape);
			return;
		}

		private void PatchParent(Guid? parentId)
		{
			if (!parentId.HasValue)
			{
				return;
			}

			var newParent = App.FindActor(parentId.Value);
			if (parentId.Value != ParentId && parentId.Value == Guid.Empty)
			{
				// clear parent
				ParentId = Guid.Empty;
				transform.SetParent(App.SceneRoot.transform, false);
			}
			else if (parentId.Value != ParentId && newParent != null)
			{
				// reassign parent
				ParentId = parentId.Value;
				transform.SetParent(((Actor)newParent).transform, false);
			}
			else if (parentId.Value != ParentId)
			{
				// queue parent reassignment
				ParentId = parentId.Value;
				App.ProcessActorCommand(ParentId, new LocalCommand()
				{
					Command = () =>
					{
						var freshParent = App.FindActor(ParentId) as Actor;
						if (this != null && freshParent != null && transform.parent != freshParent.transform)
						{
							transform.SetParent(freshParent.transform, false);
						}
					}
				}, null);
			}
		}

		private void PatchName(string nameOrNull)
		{
			if (nameOrNull != null)
			{
				Name = nameOrNull;
				name = Name;
			}
		}

		private void PatchExclusive(Guid? exclusiveToUser)
		{
			if (App.UsePhysicsBridge && exclusiveToUser.HasValue)
			{
				// Should be set only once when actor is initialized
				// and only for single user who receives the patch.
				// The comparison check is not actually required.
				_isExclusiveToUser = App.LocalUser.Id == exclusiveToUser.Value;
			}
		}

		private void PatchOwner(Guid? ownerOrNull)
		{
			if (App.UsePhysicsBridge)
			{
				if (ownerOrNull.HasValue)
				{
					if (RigidBody != null)
					{
						if (!_isExclusiveToUser)
						{
							if (Owner.HasValue) // test the old value
							{
								// if body is already registered to physics bridge, just set the new owner
								App.PhysicsBridge.setRigidBodyOwnership(Id, ownerOrNull.Value, RigidBody.IsKinematic);
							}
							else
							{
								// if this is first time owner is set, add body to physics bridge
								App.PhysicsBridge.addRigidBody(Id, _rigidbody, ownerOrNull.Value, RigidBody.IsKinematic);
							}

							Owner = ownerOrNull;

							// If object is grabbed make it kinematic
							if (IsSimulatedByLocalUser && IsGrabbed)
							{
								App.PhysicsBridge.setKeyframed(Id, true);
							}
						}
					}
					else
					{
						Owner = ownerOrNull;
					}

				}
			}
		}

		private void PatchAppearance(AppearancePatch appearance)
		{
			if (appearance == null)
			{
				return;
			}

			bool forceUpdateRenderer = false;

			// update renderers
			if (appearance.MaterialId != null || appearance.MeshId != null)
			{
				// patch mesh
				if (appearance.MeshId != null)
				{
					MeshId = appearance.MeshId.Value;
				}

				// apply mesh/material to game object
				if (MeshId != Guid.Empty)
				{
					// guarantee renderer component
					if (renderer == null)
					{
						renderer = gameObject.AddComponent<MeshRenderer>();
						renderer.sharedMaterial = MREAPI.AppsAPI.DefaultMaterial;
						forceUpdateRenderer = true;
					}
					// guarantee mesh filter (unless it has a skinned mesh renderer)
					if (renderer is MeshRenderer && meshFilter == null)
					{
						meshFilter = gameObject.AddComponent<MeshFilter>();
					}

					// look up and assign mesh
					var updatedMeshId = MeshId;
					App.AssetManager.OnSet(MeshId, sharedMesh =>
					{
						if (!this || MeshId != updatedMeshId) return;
						UnityMesh = (Mesh)sharedMesh.Asset;
						if (Collider != null && Collider.Shape == ColliderType.Auto)
						{
							SetCollider(new ColliderPatch()
							{
								Geometry = new AutoColliderGeometry()
							});
						}
					});

					// patch material
					if (appearance.MaterialId != null)
					{
						MaterialId = appearance.MaterialId.Value;
					}
				}
				// clean up unused components
				else
				{
					Destroy(Renderer);
					Destroy(MeshFilter);
					if (Collider != null && Collider.Shape == ColliderType.Auto)
					{
						Destroy(_collider);
						Destroy(Collider);
						_collider = null;
						Collider = null;
					}
				}
			}

			// apply visibility after renderer updated
			if (appearance.Enabled != null || forceUpdateRenderer)
			{
				if (appearance.Enabled != null)
				{
					appearanceEnabled = appearance.Enabled.Value;
				}
				ApplyVisibilityUpdate(this);
			}
		}

		internal static void ApplyVisibilityUpdate(Actor actor, bool force = false)
		{
			// Note: MonoBehaviours don't support conditional access (actor.Renderer?.enabled)
			if (actor != null)
			{
				if (actor.Renderer != null && ((actor.Renderer.enabled != actor.activeAndEnabled) || force))
				{
					actor.Renderer.enabled = actor.activeAndEnabled;
				}

				foreach (var child in actor.App.FindChildren(actor.Id))
				{
					ApplyVisibilityUpdate(child, force);
				}
			}
		}

		/// <summary>
		/// Precondition: Asset identified by `id` exists, and is a material.
		/// </summary>
		/// <param name="id"></param>
		private void CheckMaterialReferenceChanged(Guid id)
		{
			if (this != null && MaterialId == id && Renderer != null)
			{
				Renderer.sharedMaterial = (Material)App.AssetManager.GetById(id).Value.Asset;
			}
		}

		private void PatchTransform(ActorTransformPatch transformPatch)
		{
			if (transformPatch != null)
			{
				if (RigidBody == null)
				{
					// Apply local first.
					if (transformPatch.Local != null)
					{
						transform.ApplyLocalPatch(LocalTransform, transformPatch.Local);
					}

					// Apply app patch second to ensure it overrides any duplicate values from the local patch.
					// App transform patching always wins over local, except for scale.
					if (transformPatch.App != null)
					{
						transform.ApplyAppPatch(App.SceneRoot.transform, AppTransform, transformPatch.App);
					}
				}
				else
				{
					// We need to update transform only for the simulation owner,
					// others will get update through PhysicsBridge.
					if (!App.UsePhysicsBridge || IsSimulatedByLocalUser)
					{
						PatchTransformWithRigidBody(transformPatch);
					}
				}
			}
		}

		private void PatchTransformWithRigidBody(ActorTransformPatch transformPatch)
		{
			if (_rigidbody == null)
			{
				return;
			}

			RigidBody.RigidBodyTransformUpdate transformUpdate = new RigidBody.RigidBodyTransformUpdate();
			if (transformPatch.Local != null)
			{
				// In case of rigid body:
				// - Apply scale directly.
				transform.localScale = transform.localScale.GetPatchApplied(LocalTransform.Scale.ApplyPatch(transformPatch.Local.Scale));

				// - Apply position and rotation via rigid body from local to world space.
				if (transformPatch.Local.Position != null)
				{
					var localPosition = transform.localPosition.GetPatchApplied(LocalTransform.Position.ApplyPatch(transformPatch.Local.Position));
					transformUpdate.Position = transform.parent.TransformPoint(localPosition);
				}

				if (transformPatch.Local.Rotation != null)
				{
					var localRotation = transform.localRotation.GetPatchApplied(LocalTransform.Rotation.ApplyPatch(transformPatch.Local.Rotation));
					transformUpdate.Rotation = transform.parent.rotation * localRotation;
				}
			}

			if (transformPatch.App != null)
			{
				var appTransform = App.SceneRoot.transform;

				if (transformPatch.App.Position != null)
				{
					// New app space position.
					var newAppPos = appTransform.InverseTransformPoint(transform.position)
						.GetPatchApplied(AppTransform.Position.ApplyPatch(transformPatch.App.Position));

					// Transform new position to world space.
					transformUpdate.Position = appTransform.TransformPoint(newAppPos);
				}

				if (transformPatch.App.Rotation != null)
				{
					// New app space rotation
					var newAppRot = (transform.rotation * appTransform.rotation)
						.GetPatchApplied(AppTransform.Rotation.ApplyPatch(transformPatch.App.Rotation));

					// Transform new app rotation to world space.
					transformUpdate.Rotation = newAppRot * transform.rotation;
				}
			}

			// Queue update to happen in the fixed update
			RigidBody.SynchronizeEngine(transformUpdate);
		}

		private void CorrectAppTransform(MWTransform transform)
		{
			if (transform == null)
			{
				return;
			}

			if (RigidBody == null)
			{
				// We need to lerp at the transform level with the transform lerper.
				if (_transformLerper == null)
				{
					_transformLerper = new TransformLerper(gameObject.transform);
				}

				// Convert the app relative transform for the correction to world position relative to our app root.
				Vector3? newPos = null;
				Quaternion? newRot = null;

				if (transform.Position != null)
				{
					Vector3 appPos;
					appPos.x = transform.Position.X;
					appPos.y = transform.Position.Y;
					appPos.z = transform.Position.Z;
					newPos = App.SceneRoot.transform.TransformPoint(appPos);
				}

				if (transform.Rotation != null)
				{
					Quaternion appRot;
					appRot.w = transform.Rotation.W;
					appRot.x = transform.Rotation.X;
					appRot.y = transform.Rotation.Y;
					appRot.z = transform.Rotation.Z;
					newRot = App.SceneRoot.transform.rotation * appRot;
				}

				// We do not pass in a value for the update period at this point.  We will be adding in lag
				// prediction for the network here in the future once that is more fully fleshed out.
				_transformLerper.SetTarget(newPos, newRot);
			}
			else
			{
				// nothing to do this should be handled by the physics channel

				if (!App.UsePhysicsBridge)
				{
					// Lerping and correction needs to happen at the rigid body level here to
					// not interfere with physics simulation.  This will change with kinematic being
					// enabled on a rigid body for when it is grabbed.  We do not support this currently,
					// and thus do not interpolate the actor.  Just set the position for the rigid body.

					_rbTransformPatch = _rbTransformPatch ?? new ActorTransformPatch()
					{
						App = new TransformPatch()
						{
							Position = new Vector3Patch(),
							Rotation = new QuaternionPatch()
						}
					};

					if (transform.Position != null)
					{
						_rbTransformPatch.App.Position.X = transform.Position.X;
						_rbTransformPatch.App.Position.Y = transform.Position.Y;
						_rbTransformPatch.App.Position.Z = transform.Position.Z;
					}
					else
					{
						_rbTransformPatch.App.Position = null;
					}

					if (transform.Rotation != null)
					{
						_rbTransformPatch.App.Rotation.W = transform.Rotation.W;
						_rbTransformPatch.App.Rotation.X = transform.Rotation.X;
						_rbTransformPatch.App.Rotation.Y = transform.Rotation.Y;
						_rbTransformPatch.App.Rotation.Z = transform.Rotation.Z;
					}
					else
					{
						_rbTransformPatch.App.Rotation = null;
					}

					PatchTransformWithRigidBody(_rbTransformPatch);
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
				bool patchVelocities = !App.UsePhysicsBridge || IsSimulatedByLocalUser;

				bool wasKinematic;

				if (RigidBody == null)
				{
					AddRigidBody();

					wasKinematic = RigidBody.IsKinematic;

					RigidBody.ApplyPatch(rigidBodyPatch, patchVelocities);
				}
				else
				{
					wasKinematic = RigidBody.IsKinematic;

					// Queue update to happen in the fixed update
					RigidBody.SynchronizeEngine(rigidBodyPatch, patchVelocities);
				}

				if (App.UsePhysicsBridge)
				{
					if (rigidBodyPatch.IsKinematic.HasValue && rigidBodyPatch.IsKinematic.Value != wasKinematic)
					{
						App.PhysicsBridge.setKeyframed(Id, rigidBodyPatch.IsKinematic.Value);
					}
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

		private int colliderGeneration = 0;
		private void PatchCollider(ColliderPatch colliderPatch)
		{
			if (colliderPatch != null)
			{
				// A collider patch that contains collider geometry signals that we need to update the
				// collider to match the desired geometry.
				if (colliderPatch.Geometry != null)
				{
					var runningGeneration = ++colliderGeneration;

					// must wait for mesh load before auto type will work
					if (colliderPatch.Geometry.Shape == ColliderType.Auto && App.AssetManager.GetById(MeshId) == null)
					{
						var runningMeshId = MeshId;
						_pendingColliderPatch = colliderPatch;
						App.AssetManager.OnSet(MeshId, _ =>
						{
							if (runningMeshId != MeshId || runningGeneration != colliderGeneration) return;
							SetCollider(_pendingColliderPatch);
							Collider?.SynchronizeEngine(_pendingColliderPatch);
							_pendingColliderPatch = null;
						});
					}
					// every other kind of geo patch
					else
					{
						_pendingColliderPatch = null;
						SetCollider(colliderPatch);
					}
				}

				// If we're waiting for the auto mesh, don't apply any patches until it completes.
				// Instead, accumulate changes in the pending collider patch
				if (_pendingColliderPatch != null && _pendingColliderPatch != colliderPatch)
				{
					if (colliderPatch.Enabled.HasValue)
						_pendingColliderPatch.Enabled = colliderPatch.Enabled.Value;
					if (colliderPatch.IsTrigger.HasValue)
						_pendingColliderPatch.IsTrigger = colliderPatch.IsTrigger.Value;

					// update bounciness and frictions 
					if (colliderPatch.Bounciness.HasValue)
						_collider.material.bounciness = colliderPatch.Bounciness.Value;
					if (colliderPatch.StaticFriction.HasValue)
						_collider.material.staticFriction = colliderPatch.StaticFriction.Value;
					if (colliderPatch.DynamicFriction.HasValue)
						_collider.material.dynamicFriction = colliderPatch.DynamicFriction.Value;
				}
				else if (_pendingColliderPatch == null)
				{
					Collider?.SynchronizeEngine(colliderPatch);
				}
			}
		}

		private void PatchAttachment(AttachmentPatch attachmentPatch)
		{
			if (attachmentPatch != null && attachmentPatch.IsPatched() && !attachmentPatch.Equals(Attachment))
			{
				Attachment.ApplyPatch(attachmentPatch);
				if (!PerformAttach())
				{
					Attachment.Clear();
				}
			}
		}

		private void PatchLookAt(LookAtPatch lookAtPatch)
		{
			if (lookAtPatch != null)
			{
				if (_lookAt == null)
				{
					_lookAt = GetOrCreateActorComponent<LookAtComponent>();
				}
				_lookAt.ApplyPatch(lookAtPatch);
			}
		}

		private void PatchGrabbable(bool? grabbable)
		{
			if (grabbable != null && grabbable.Value != Grabbable)
			{
				// Update existing behavior or add a basic target behavior if there isn't one already.
				var behaviorComponent = GetActorComponent<BehaviorComponent>();
				if (behaviorComponent == null)
				{
					// NOTE: We need to have the default behavior on an actor be a button for now in the case we want the actor
					// to be able to be grabbed on all controller types for host apps.  This will be a base Target behavior once we
					// update host apps to handle button conflicts.
					behaviorComponent = GetOrCreateActorComponent<BehaviorComponent>();
					var context = BehaviorContextFactory.CreateContext(BehaviorType.Button, this, new WeakReference<MixedRealityExtensionApp>(App));

					if (context == null)
					{
						Debug.LogError("Failed to create a behavior context.  Grab will not work without one.");
						return;
					}

					behaviorComponent.SetBehaviorContext(context);
				}

				if (behaviorComponent.Context is TargetBehaviorContext targetContext)
				{
					if (RigidBody != null)
					{
						// for rigid body we need callbacks for the physics bridge
						var targetBehavior = (ITargetBehavior)targetContext.Behavior;
						bool wasGrabbable = targetBehavior.Grabbable;
						targetBehavior.Grabbable = grabbable.Value;

						if (wasGrabbable != grabbable.Value)
						{
							if (grabbable.Value)
							{
								targetContext.GrabAction.ActionStateChanged += OnRigidBodyGrabbed;
							}
							else
							{
								targetContext.GrabAction.ActionStateChanged -= OnRigidBodyGrabbed;
							}
						}
					}
					else
					{
						// non-rigid body context
						((ITargetBehavior)behaviorComponent.Behavior).Grabbable = grabbable.Value;
					}
				}
				Grabbable = grabbable.Value;
			}
		}

		private void PatchSubscriptions(IEnumerable<ActorComponentType> subscriptions)
		{
			if (subscriptions != null)
			{
				_subscriptions = ActorComponentType.None;
				foreach (var subscription in subscriptions)
				{
					_subscriptions |= subscription;
				}
			}
		}

		private void GenerateTransformPatch(ActorPatch actorPatch)
		{
			var transformPatch = new ActorTransformPatch()
			{
				Local = PatchingUtilMethods.GenerateLocalTransformPatch(LocalTransform, transform),
				App = PatchingUtilMethods.GenerateAppTransformPatch(AppTransform, transform, App.SceneRoot.transform)
			};

			LocalTransform.ToLocalTransform(transform);
			AppTransform.ToAppTransform(transform, App.SceneRoot.transform);

			actorPatch.Transform = transformPatch.IsPatched() ? transformPatch : null;
		}

		private void GenerateRigidBodyPatch(ActorPatch actorPatch, bool addVelocities)
		{
			if (_rigidbody != null && RigidBody != null)
			{
				// convert to a RigidBody and build a patch from the old one to this one.
				var rigidBodyPatch = PatchingUtilMethods.GeneratePatch(RigidBody, _rigidbody,
					App.SceneRoot.transform, addVelocities);

				if (rigidBodyPatch != null && rigidBodyPatch.IsPatched())
				{
					actorPatch.RigidBody = rigidBodyPatch;
				}

				RigidBody.Update(_rigidbody);
			}
		}

		private void GenerateAttachmentPatch(ActorPatch actorPatch)
		{
			actorPatch.Attachment = Attachment.GeneratePatch(_cachedAttachment);
			if (actorPatch.Attachment != null)
			{
				_cachedAttachment.CopyFrom(Attachment);
			}
		}

		private void CleanUp()
		{
			var behaviorComponent = GetActorComponent<BehaviorComponent>();
			if (behaviorComponent != null && behaviorComponent.Context is TargetBehaviorContext targetContext)
			{
				var targetBehavior = (ITargetBehavior)targetContext.Behavior;
				if (RigidBody != null && Grabbable)
				{
					targetContext.GrabAction.ActionStateChanged -= OnRigidBodyGrabbed;
				}
			}

			if (App.UsePhysicsBridge)
			{
				if (RigidBody != null)
				{
					App.PhysicsBridge.removeRigidBody(Id);
				}
			}

			foreach (var component in _components.Values)
			{
				component.CleanUp();
			}
		}

		private bool ShouldSync(ActorComponentType subscriptions, ActorComponentType flag)
		{
			// We do not want to send actor updates until we're fully joined to the app.
			// TODO: We shouldn't need to do this check. The engine shouldn't try to send
			// updates until we're fully joined to the app.
			if (!(App.Protocol is Messaging.Protocols.Execution))
			{
				return false;
			}

			// If the actor has a rigid body then always sync the transform and the rigid body.
			// but not the velocities (due to bandwidth), sync only when there is an explicit subscription for the velocities
			if (RigidBody != null)
			{
				subscriptions |= ActorComponentType.Transform;
				subscriptions |= ActorComponentType.Rigidbody;
			}

			Attachment attachmentInHierarchy = FindAttachmentInHierarchy();
			bool inAttachmentHeirarchy = (attachmentInHierarchy != null);
			bool inOwnedAttachmentHierarchy = (inAttachmentHeirarchy && LocalUser != null && attachmentInHierarchy.UserId == LocalUser.Id);

			// Don't sync anything if the actor is in an attachment hierarchy on a remote avatar.
			if (inAttachmentHeirarchy && !inOwnedAttachmentHierarchy)
			{
				subscriptions = ActorComponentType.None;
			}

			if (subscriptions.HasFlag(flag))
			{
				return
					((App.OperatingModel == OperatingModel.ServerAuthoritative) ||
					(RigidBody == null &&
						((App.IsAuthoritativePeer ||
						inOwnedAttachmentHierarchy) && !IsGrabbed)) ||
					(RigidBody != null &&
						IsSimulatedByLocalUser));
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

			Attachment attachmentInHierarchy = FindAttachmentInHierarchy();
			bool inAttachmentHeirarchy = (attachmentInHierarchy != null);
			bool inOwnedAttachmentHierarchy = (inAttachmentHeirarchy && LocalUser != null && attachmentInHierarchy.UserId == LocalUser.Id);

			// We can send actor updates to the app if we're operating in a server-authoritative model,
			// or if we're in a peer-authoritative model and we've been designated the authoritative peer.
			// Override the previous rules if this actor is grabbed by the local user or is in an attachment
			// hierarchy owned by the local user.
			if (App.OperatingModel == OperatingModel.ServerAuthoritative ||
				(RigidBody == null &&
					(App.IsAuthoritativePeer ||
					IsGrabbed ||
					_grabbedLastSync ||
					inOwnedAttachmentHierarchy)) ||
				(RigidBody != null &&
					(IsSimulatedByLocalUser || IsGrabbed ||
					_grabbedLastSync )))
			{
				return true;
			}

			return false;
		}

		#endregion

		#region Command Handlers

		[CommandHandler(typeof(LocalCommand))]
		private void OnLocalCommand(LocalCommand payload, Action onCompleteCallback)
		{
			payload.Command?.Invoke();
			onCompleteCallback?.Invoke();
		}

		[CommandHandler(typeof(ActorCorrection))]
		private void OnActorCorrection(ActorCorrection payload, Action onCompleteCallback)
		{
			EngineCorrection(payload);
			onCompleteCallback?.Invoke();
		}

		[CommandHandler(typeof(ActorUpdate))]
		private void OnActorUpdate(ActorUpdate payload, Action onCompleteCallback)
		{
			SynchronizeEngine(payload.Actor);
			onCompleteCallback?.Invoke();
		}

		[CommandHandler(typeof(RigidBodyCommands))]
		private void OnRigidBodyCommands(RigidBodyCommands payload, Action onCompleteCallback)
		{
			ExecuteRigidBodyCommands(payload, onCompleteCallback);
		}

		[CommandHandler(typeof(CreateAnimation))]
		private void OnCreateAnimation(CreateAnimation payload, Action onCompleteCallback)
		{
			var animComponent = GetOrCreateActorComponent<AnimationComponent>();
			animComponent.CreateAnimation(payload.AnimationName, payload.Keyframes, payload.Events, payload.WrapMode, payload.InitialState,
				isInternal: false,
				managed: payload.AnimationId.HasValue,
				onCreatedCallback: () =>
				{
					if (payload.AnimationId.HasValue)
					{
						var unityAnim = GetComponent<UnityEngine.Animation>();
						var unityState = unityAnim[payload.AnimationName];
						var nativeAnim = new NativeAnimation(
							App.AnimationManager,
							payload.AnimationId.Value,
							unityAnim,
							unityState);
						nativeAnim.TargetIds = new List<Guid>() { Id };
						App.AnimationManager.RegisterAnimation(nativeAnim);

						Trace trace = new Trace()
						{
							Severity = TraceSeverity.Info,
							Message = $"Successfully created animation named {nativeAnim.Name}"
						};

						App.Protocol.Send(
							new ObjectSpawned()
							{
								Result = new OperationResult()
								{
									ResultCode = OperationResultCode.Success,
									Message = trace.Message
								},
								Traces = new List<Trace>() { trace },
								Animations = new AnimationPatch[] { nativeAnim.GeneratePatch() }
							},
							payload.MessageId
						);
					}
					onCompleteCallback?.Invoke();
				}
			);
		}

		[Obsolete]
		[CommandHandler(typeof(SetAnimationState))]
		private void OnSetAnimationState(SetAnimationState payload, Action onCompleteCallback)
		{
			var actor = (Actor)App.FindActor(payload.ActorId);
			actor.GetOrCreateActorComponent<AnimationComponent>()
				.SetAnimationState(payload.AnimationName, payload.State.Time, payload.State.Speed, payload.State.Enabled);
			onCompleteCallback?.Invoke();
		}

		[CommandHandler(typeof(SetMediaState))]
		private void OnSetMediaState(SetMediaState payload, Action onCompleteCallback)
		{
			if (_mediaInstances == null)
			{
				_mediaInstances = new Dictionary<Guid, MediaInstance>();
			}
			switch (payload.MediaCommand)
			{
				case MediaCommand.Start:
					{
						MediaInstance mediaInstance = new MediaInstance(payload.MediaAssetId);
						_mediaInstances.Add(payload.Id, mediaInstance);

						App.AssetManager.OnSet(payload.MediaAssetId, asset =>
						{
							if (asset.Asset is AudioClip audioClip)
							{
								AudioSource soundInstance = App.SoundManager.AddSoundInstance(this, payload.Id, audioClip, payload.Options);
								if (soundInstance)
								{
									mediaInstance.Instance = soundInstance;
								}
								else
								{
									App.Logger.LogError($"Trying to start sound instance that should already have completed for: {payload.MediaAssetId}\n");
									_mediaInstances.Remove(payload.Id);
								}
							}
							else if (asset.Asset is VideoStreamDescription videoStreamDescription)
							{
								var factory = MREAPI.AppsAPI.VideoPlayerFactory
									?? throw new ArgumentException("Cannot start video stream - VideoPlayerFactory not implemented.");
								IVideoPlayer videoPlayer = factory.CreateVideoPlayer(this);
								videoPlayer.Play(videoStreamDescription, payload.Options);
								mediaInstance.Instance = videoPlayer;
							}
							else
							{
								App.Logger.LogError($"Failed to start media instance with asset id: {payload.MediaAssetId}\n");
								_mediaInstances.Remove(payload.Id);
							}
						});
					}
					break;
				case MediaCommand.Stop:
					{
						if (_mediaInstances.TryGetValue(payload.Id, out MediaInstance mediaInstance))
						{
							App.AssetManager.OnSet(mediaInstance.MediaAssetId, _ =>
							{
								_mediaInstances.Remove(payload.Id);
								DestroyMediaById(payload.Id, mediaInstance);
							});
						}
					}
					break;
				case MediaCommand.Update:
					{
						if (_mediaInstances.TryGetValue(payload.Id, out MediaInstance mediaInstance))
						{
							App.AssetManager.OnSet(mediaInstance.MediaAssetId, _ =>
							{
								if (mediaInstance.Instance != null)
								{
									if (mediaInstance.Instance is AudioSource soundInstance)
									{
										App.SoundManager.ApplyMediaStateOptions(this, soundInstance, payload.Options, payload.Id, false);
									}
									else if (mediaInstance.Instance is IVideoPlayer videoPlayer)
									{
										videoPlayer.ApplyMediaStateOptions(payload.Options);
									}
								}
							});
						}
					}
					break;
			}
			onCompleteCallback?.Invoke();
		}

		public bool CheckIfSoundExpired(Guid id)
		{
			if (_mediaInstances != null && _mediaInstances.TryGetValue(id, out MediaInstance mediaInstance))
			{
				if (mediaInstance.Instance != null)
				{
					if (mediaInstance.Instance is AudioSource soundInstance)
					{
						if (soundInstance.isPlaying)
						{
							return false;
						}
						DestroyMediaById(id, mediaInstance);
					}
				}
			}
			return true;
		}

		private void DestroyMediaById(Guid id, MediaInstance mediaInstance)
		{
			if (mediaInstance.Instance != null)
			{
				if (mediaInstance.Instance is AudioSource soundInstance)
				{
					App.SoundManager.DestroySoundInstance(soundInstance, id);
				}
				else if (mediaInstance.Instance is IVideoPlayer videoPlayer)
				{
					videoPlayer.Destroy();
				}
				mediaInstance.Instance = null;
			}
		}


		[CommandHandler(typeof(InterpolateActor))]
		private void OnInterpolateActor(InterpolateActor payload, Action onCompleteCallback)
		{
			GetOrCreateActorComponent<AnimationComponent>()
				.Interpolate(
					payload.Value,
					payload.AnimationName,
					payload.Duration,
					payload.Curve,
					payload.Enabled);
			onCompleteCallback?.Invoke();
		}

		[CommandHandler(typeof(SetBehavior))]
		private void OnSetBehavior(SetBehavior payload, Action onCompleteCallback)
		{
			// Don't create a behavior at all for this actor if the app is not interactable for any users.
			if (!App.InteractionEnabled())
			{
				onCompleteCallback?.Invoke();
				return;
			}

			var behaviorComponent = GetOrCreateActorComponent<BehaviorComponent>();

			if (behaviorComponent.ContainsBehaviorContext())
			{
				behaviorComponent.ClearBehaviorContext();
			}

			if (payload.BehaviorType != BehaviorType.None)
			{
				var context = BehaviorContextFactory.CreateContext(payload.BehaviorType, this, new WeakReference<MixedRealityExtensionApp>(App));

				if (context == null)
				{
					Debug.LogError($"Failed to create behavior for behavior type {payload.BehaviorType.ToString()}");
					onCompleteCallback?.Invoke();
					return;
				}

				behaviorComponent.SetBehaviorContext(context);

				// We need to update the new behavior's grabbable flag from the actor so that it can be grabbed in the case we cleared the previous behavior.
				((ITargetBehavior)context.Behavior).Grabbable = Grabbable;
			}

			onCompleteCallback?.Invoke();
		}

		#endregion

		#region Command Handlers - Rigid Body Commands

		[CommandHandler(typeof(RBMovePosition))]
		private void OnRBMovePosition(RBMovePosition payload, Action onCompleteCallback)
		{
			RigidBody?.RigidBodyMovePosition(new MWVector3().ApplyPatch(payload.Position));
			onCompleteCallback?.Invoke();
		}

		[CommandHandler(typeof(RBMoveRotation))]
		private void OnRBMoveRotation(RBMoveRotation payload, Action onCompleteCallback)
		{
			RigidBody?.RigidBodyMoveRotation(new MWQuaternion().ApplyPatch(payload.Rotation));
			onCompleteCallback?.Invoke();
		}

		[CommandHandler(typeof(RBAddForce))]
		private void OnRBAddForce(RBAddForce payload, Action onCompleteCallback)
		{
			bool isOwner = Owner.HasValue ? Owner.Value == App.LocalUser.Id : CanSync();
			if (isOwner)
			{
				RigidBody?.RigidBodyAddForce(new MWVector3().ApplyPatch(payload.Force));
			}

			onCompleteCallback?.Invoke();
		}

		[CommandHandler(typeof(RBAddForceAtPosition))]
		private void OnRBAddForceAtPosition(RBAddForceAtPosition payload, Action onCompleteCallback)
		{
			var force = new MWVector3().ApplyPatch(payload.Force);
			var position = new MWVector3().ApplyPatch(payload.Position);
			RigidBody?.RigidBodyAddForceAtPosition(force, position);
			onCompleteCallback?.Invoke();
		}

		[CommandHandler(typeof(RBAddTorque))]
		private void OnRBAddTorque(RBAddTorque payload, Action onCompleteCallback)
		{
			RigidBody?.RigidBodyAddTorque(new MWVector3().ApplyPatch(payload.Torque));
			onCompleteCallback?.Invoke();
		}

		[CommandHandler(typeof(RBAddRelativeTorque))]
		private void OnRBAddRelativeTorque(RBAddRelativeTorque payload, Action onCompleteCallback)
		{
			RigidBody?.RigidBodyAddRelativeTorque(new MWVector3().ApplyPatch(payload.RelativeTorque));
			onCompleteCallback?.Invoke();
		}

		#endregion
	}
}

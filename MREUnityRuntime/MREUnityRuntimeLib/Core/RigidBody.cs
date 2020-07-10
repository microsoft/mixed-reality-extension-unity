// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.API;
using MixedRealityExtension.Core.Interfaces;
using MixedRealityExtension.Core.Types;
using MixedRealityExtension.Patching;
using MixedRealityExtension.Patching.Types;
using MixedRealityExtension.Util.Unity;
using System;
using System.Collections.Generic;
using UnityEngine;

using MRECollisionDetectionMode = MixedRealityExtension.Core.Interfaces.CollisionDetectionMode;
using MRERigidBodyConstraints = MixedRealityExtension.Core.Interfaces.RigidBodyConstraints;

namespace MixedRealityExtension.Core
{
	internal class RigidBody : IRigidBody
	{
		private readonly Transform _sceneRoot;
		private readonly Rigidbody _rigidbody;

		private Queue<Action<Rigidbody>> _updateActions = new Queue<Action<Rigidbody>>();

		/// <inheritdoc />
		public MWVector3 Velocity { get; set; } = new MWVector3();

		/// <inheritdoc />
		public MWVector3 AngularVelocity { get; set; } = new MWVector3();

		/// <inheritdoc />
		public float Mass { get; set; }

		/// <inheritdoc />
		public bool DetectCollisions { get; set; }

		/// <inheritdoc />
		public MRECollisionDetectionMode CollisionDetectionMode { get; set; }

		/// <inheritdoc />
		public bool UseGravity { get; set; }

		/// <inheritdoc />
		public bool IsKinematic { get; set; }

		/// <inheritdoc />
		public MRERigidBodyConstraints ConstraintFlags { get; set; }

		internal RigidBody(Rigidbody rigidbody, Transform sceneRoot)
		{
			_sceneRoot = sceneRoot;
			_rigidbody = rigidbody;
			// Read initial values
			Update(rigidbody);
		}

		/// <inheritdoc />
		public void RigidBodyMovePosition(MWVector3 position)
		{
			_updateActions.Enqueue(
				(rigidBody) =>
				{
					rigidBody.MovePosition(_sceneRoot.TransformPoint(position.ToVector3()));
				});
		}

		/// <inheritdoc />
		public void RigidBodyMoveRotation(MWQuaternion rotation)
		{
			_updateActions.Enqueue(
				(rigidBody) =>
				{
					rigidBody.MoveRotation(_sceneRoot.rotation * rotation.ToQuaternion());
				});
		}

		/// <inheritdoc />
		public void RigidBodyAddForce(MWVector3 force)
		{
			_updateActions.Enqueue(
				(rigidBody) =>
				{
					rigidBody.AddForce(_sceneRoot.TransformDirection(force.ToVector3()));
				});
		}

		/// <inheritdoc />
		public void RigidBodyAddForceAtPosition(MWVector3 force, MWVector3 position)
		{
			_updateActions.Enqueue(
				(rigidBody) =>
				{
					rigidBody.AddForceAtPosition(_sceneRoot.TransformDirection(force.ToVector3()), _sceneRoot.TransformPoint(position.ToVector3()));
				});
		}

		/// <inheritdoc />
		public void RigidBodyAddTorque(MWVector3 torque)
		{
			_updateActions.Enqueue(
				(rigidBody) =>
				{
					rigidBody.AddTorque(_sceneRoot.TransformDirection(torque.ToVector3()));
				});
		}

		/// <inheritdoc />
		public void RigidBodyAddRelativeTorque(MWVector3 relativeTorque)
		{
			_updateActions.Enqueue(
				(rigidBody) =>
				{
					rigidBody.AddRelativeTorque(_sceneRoot.TransformDirection(relativeTorque.ToVector3()));
				});
		}

		internal void Update()
		{
			if (_rigidbody == null)
			{
				return;
			}

			try
			{
				while (_updateActions.Count > 0)
				{
					_updateActions.Dequeue()(_rigidbody);
				}
			}
			catch (Exception e)
			{
				MREAPI.Logger.LogError($"Failed to perform async update of rigid body.  Exception: {e.Message}\nStack Trace: {e.StackTrace}");
			}
		}

		internal void Update(Rigidbody rigidbody)
		{
			Velocity.FromUnityVector3(_sceneRoot.InverseTransformDirection(rigidbody.velocity));
			AngularVelocity.FromUnityVector3(_sceneRoot.InverseTransformDirection(rigidbody.angularVelocity));

			// No need to read Position or Rotation. They're write-only from the patch to the component.
			Mass = rigidbody.mass;
			DetectCollisions = rigidbody.detectCollisions;
			CollisionDetectionMode = (MRECollisionDetectionMode)Enum.Parse(typeof(MRECollisionDetectionMode), rigidbody.collisionDetectionMode.ToString());
			UseGravity = rigidbody.useGravity;
			ConstraintFlags = (MRERigidBodyConstraints)Enum.Parse(typeof(MRERigidBodyConstraints), rigidbody.constraints.ToString());
		}

		internal void ApplyPatch(RigidBodyPatch patch, bool patchVelocities)
		{
			// Apply any changes made to the state of the mixed reality extension runtime version of the rigid body.

			if (patchVelocities)
			{
				if (patch.Velocity != null && patch.Velocity.IsPatched())
				{
					_rigidbody.velocity = _rigidbody.velocity.GetPatchApplied(_sceneRoot.TransformDirection(Velocity.ApplyPatch(patch.Velocity).ToVector3()));
				}
				if (patch.AngularVelocity != null && patch.AngularVelocity.IsPatched())
				{
					_rigidbody.angularVelocity = _rigidbody.angularVelocity.GetPatchApplied(_sceneRoot.TransformDirection(AngularVelocity.ApplyPatch(patch.AngularVelocity).ToVector3()));
				}
			}

			if (patch.Mass.HasValue)
			{
				_rigidbody.mass = _rigidbody.mass.GetPatchApplied(Mass.ApplyPatch(patch.Mass));
			}
			if (patch.DetectCollisions.HasValue)
			{
				_rigidbody.detectCollisions = _rigidbody.detectCollisions.GetPatchApplied(DetectCollisions.ApplyPatch(patch.DetectCollisions));
			}
			if (patch.CollisionDetectionMode.HasValue)
			{
				_rigidbody.collisionDetectionMode = _rigidbody.collisionDetectionMode.GetPatchApplied(CollisionDetectionMode.ApplyPatch(patch.CollisionDetectionMode));
			}
			if (patch.UseGravity.HasValue)
			{
				_rigidbody.useGravity = _rigidbody.useGravity.GetPatchApplied(UseGravity.ApplyPatch(patch.UseGravity));
			}
			if (patch.IsKinematic.HasValue)
			{
				IsKinematic = patch.IsKinematic.Value;
			}
			_rigidbody.constraints = (RigidbodyConstraints)((int)_rigidbody.constraints).GetPatchApplied((int)ConstraintFlags.ApplyPatch(patch.ConstraintFlags));
		}

		internal void UpdateTransform(RigidBodyTransformUpdate update)
		{
			if (update.Position != null)
			{
				_rigidbody.position = update.Position.Value;
			}
			if (update.Rotation != null)
			{
				_rigidbody.rotation = update.Rotation.Value;
			}
		}

		internal void SynchronizeEngine(RigidBodyTransformUpdate update)
		{
			_updateActions.Enqueue((rigidBody) => UpdateTransform(update));
		}

		internal void SynchronizeEngine(RigidBodyPatch patch, bool patchVelocities)
		{
			_updateActions.Enqueue((rigidbody) => ApplyPatch(patch, patchVelocities));
		}

		internal struct RigidBodyTransformUpdate
		{
			internal Vector3? Position { get; set; }

			internal Quaternion? Rotation { get; set; }
		}
	}
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Core.Types;
using System;

using MRECollisionDetectionMode = MixedRealityExtension.Core.Interfaces.CollisionDetectionMode;
using MRERigidBodyConstraints = MixedRealityExtension.Core.Interfaces.RigidBodyConstraints;

namespace MixedRealityExtension.Core.Interfaces
{
	/// <summary>
	/// The rigid body constraints applied to the rigid body.
	/// </summary>
	[Flags]
	public enum RigidBodyConstraints
	{
		/// <summary>
		/// No constraints.
		/// </summary>
		None = 0,

		/// <summary>
		/// Freeze motion along the X-axis.
		/// </summary>
		FreezePositionX = 2,
		
		/// <summary>
		/// Freeze motion along the Y-axis.
		/// </summary>
		FreezePositionY = 4,
		
		/// <summary>
		/// Freeze motion along the Z-axis.
		/// </summary>
		FreezePositionZ = 8,
		
		/// <summary>
		/// Freeze motion along all axes.
		/// </summary>
		FreezePosition = 14,
		
		/// <summary>
		/// Freeze rotation along the X-axis.
		/// </summary>
		FreezeRotationX = 16,
		
		/// <summary>
		/// Freeze rotation along the Y-axis.
		/// </summary>
		FreezeRotationY = 32,
		
		/// <summary>
		/// Freeze rotation along the Z-axis.
		/// </summary>
		FreezeRotationZ = 64,
		
		/// <summary>
		/// Freeze rotation along all axes.
		/// </summary>
		FreezeRotation = 112,
		
		/// <summary>
		/// Freeze rotation and motion along all axes.
		/// </summary>
		FreezeAll = 126
	}

	/// <summary>
	/// The type of collision detection mode used by the rigid body.
	/// </summary>
	public enum CollisionDetectionMode
	{
		/// <summary>
		/// Discrete collision detection mode.
		/// </summary>
		Discrete,

		/// <summary>
		/// Continuous collision mode.
		/// </summary>
		Continuous,

		/// <summary>
		/// Continuous and dynamic collision mode.
		/// </summary>
		ContinuousDynamic
	}

	/// <summary>
	/// The interface that represents a rigid body within the mixed reality extension runtime.
	/// </summary>
	public interface IRigidBody
	{
		/// <summary>
		/// Gets the velocity of the rigid body.
		/// </summary>
		MWVector3 Velocity { get; }

		/// <summary>
		/// Gets the angular velocity of the rigid body.
		/// </summary>
		MWVector3 AngularVelocity { get; }

		/// <summary>
		/// Gets the mass of the rigid body.
		/// </summary>
		float Mass { get; }

		/// <summary>
		/// Gets whether collisions are to be deteced with the rigid body.
		/// </summary>
		bool DetectCollisions { get; }

		/// <summary>
		/// Gets the collision detection mode for the rigid body.
		/// </summary>
		MRECollisionDetectionMode CollisionDetectionMode { get; }

		/// <summary>
		/// Gets whether the rigid body uses gravity.
		/// </summary>
		bool UseGravity { get; }

		/// <summary>
		/// Gets whether the rigid body is kinematic or not.  Kinematic rigid bodies are not
		/// simulated by the physics engine.
		/// </summary>
		bool IsKinematic { get; }

		/// <summary>
		/// Gets the constraint flags applied to the rigid body.
		/// </summary>
		MRERigidBodyConstraints ConstraintFlags { get; }

		/// <summary>
		/// Move the position of the rigid body to the new position.
		/// </summary>
		/// <param name="position">The position to move the rigid body to.</param>
		void RigidBodyMovePosition(MWVector3 position);

		/// <summary>
		/// Move the rotation of the rigid body to the new rotation.
		/// </summary>
		/// <param name="rotation">The rotation to rotate the rigid body to.</param>
		void RigidBodyMoveRotation(MWQuaternion rotation);

		/// <summary>
		/// Apply a force to the rigid body.
		/// </summary>
		/// <param name="force">The force to apply to the rigid body.</param>
		void RigidBodyAddForce(MWVector3 force);

		/// <summary>
		/// Apply a force at a specific position of the rigid body.
		/// </summary>
		/// <param name="force">The force to apply to the rigid body.</param>
		/// <param name="position">The position at which to apply the force.</param>
		void RigidBodyAddForceAtPosition(MWVector3 force, MWVector3 position);

		/// <summary>
		/// Apply a torque to the rigid body.
		/// </summary>
		/// <param name="torque">The torque to apply to the rigid body.</param>
		void RigidBodyAddTorque(MWVector3 torque);

		/// <summary>
		/// Apply a relative torque to the rigid body.
		/// </summary>
		/// <param name="relativeTorque">The relative torque to apply to the rigid body.</param>
		void RigidBodyAddRelativeTorque(MWVector3 relativeTorque);
	}
}

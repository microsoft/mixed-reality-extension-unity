// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MixedRealityExtension.Core.Physics
{

	/// to monitor the collisions between frames this we need to store per contact pair. 
	public class CollisionMonitorInfo
	{
		public float timeFromStartCollision = 0.0f;
		public float relativeDistance = float.MaxValue;
		/// how much we interpolate between the jitter buffer and the simulation
		public float keyframedInterpolationRatio = 0.0f;
	}

	/// for interactive collisions we need to store the implicit velocities and other information	
	public class CollisionSwitchInfo
	{
		public UnityEngine.Vector3 startPosition;
		public UnityEngine.Quaternion startOrientation;
		public UnityEngine.Vector3 linearVelocity;
		public UnityEngine.Vector3 angularVelocity;
		public Guid rigidBodyId;
		public CollisionMonitorInfo monitorInfo;
		/// if a body is really key framed on the owner side then we should not turn it to dynamic on the remote side
		public bool isKeyframed; 
	}

	/// This class implements one strategy for the prediction where in the neighborhood of the
	/// remote-owned body collision switches to both dynamic strategies and after some time tries to
	/// interpolate back to the streamed transform positions. 
	class PredictionInterpolation : IPrediction
	{
		/// when we update a body we compute the implicit velocity. This velocity is needed, in case of collisions to switch from kinematic to kinematic=false
		List<CollisionSwitchInfo> _switchCollisionInfos = new List<CollisionSwitchInfo>();

		/// input is Guid from the remote body (in case of multiple collisions take the minimum
		Dictionary<Guid, CollisionMonitorInfo> _monitorCollisionInfo = new Dictionary<Guid, CollisionMonitorInfo>();

		// ---------- all the method specific parameters ---------
		const float startInterpolatingBack = 0.8f; // time in seconds
		const float endInterpolatingBack = 2.0f; // time in seconds
		const float invInterpolationTimeWindows = 1.0f / (endInterpolatingBack - startInterpolatingBack);
		const float limitCollisionInterpolation = 0.2f;
		const float collisionRelDistLimit = 0.8f;

		const float radiusExpansionFactor = 1.3f; // to detect potential collisions
		const float inCollisionRangeRelativeDistanceFactor = 1.25f;

		const float interpolationPosEpsilon = 0.01f;
		const float interpolationAngularEps = 3.0f;

		const float velocityDampingForInterpolation = 0.95f; //damp velocities during interpolation
		const float velocityDampingInterpolationValueStart = 0.1f; // starting from this interpolation value we start velocity damping (should be smaller than 0.2)


		/// empty Ctor
		public PredictionInterpolation()
		{

		}

		public void StartBodyPredicitonForNextFrame()
		{
#if MRE_PHYSICS_DEBUG
			Debug.Log(" ------------ ");
#endif
			// delete all the previous collisions
			_switchCollisionInfos.Clear();
		}


		public void AddAndProcessRemoteBodyForPrediction(RigidBodyPhysicsBridgeInfo rb,
			RigidBodyTransform transform, UnityEngine.Vector3 keyFramedPos,
			UnityEngine.Quaternion keyFramedOrientation, float timeOfSnapshot,
			PredictionTimeParameters timeInfo)
		{
			var collisionInfo = new CollisionSwitchInfo();
			collisionInfo.startPosition = rb.RigidBody.transform.position;
			collisionInfo.startOrientation = rb.RigidBody.transform.rotation;
			collisionInfo.rigidBodyId = rb.Id;
			collisionInfo.isKeyframed = rb.IsKeyframed;
			// test is this remote body is in the monitor stream or if this is grabbed & key framed then this should not be dynamic
			if (_monitorCollisionInfo.ContainsKey(rb.Id) && !rb.IsKeyframed)
			{
				// dynamic
				rb.RigidBody.isKinematic = false;
				collisionInfo.monitorInfo = _monitorCollisionInfo[rb.Id];
				collisionInfo.monitorInfo.timeFromStartCollision += timeInfo.DT;
				collisionInfo.linearVelocity = rb.RigidBody.velocity;
				collisionInfo.angularVelocity = rb.RigidBody.angularVelocity;
#if MRE_PHYSICS_DEBUG
						Debug.Log(" Remote body: " + rb.Id.ToString() + " is dynamic since:"
							+ collisionInfo.monitorInfo.timeFromStartCollision + " relative distance:" + collisionInfo.monitorInfo.relativeDistance
							+ " interpolation:" + collisionInfo.monitorInfo.keyframedInterpolationRatio);
#endif
				// if time passes by then make a transformation between key framed and dynamic
				// but only change the positions not the velocity
				if (collisionInfo.monitorInfo.keyframedInterpolationRatio > 0.05f)
				{
					// interpolate between key framed and dynamic transforms
					float t = collisionInfo.monitorInfo.keyframedInterpolationRatio;
					UnityEngine.Vector3 interpolatedPos;
					UnityEngine.Quaternion interpolatedQuad;
					interpolatedPos = t * keyFramedPos + (1.0f - t) * rb.RigidBody.transform.position;
					interpolatedQuad = UnityEngine.Quaternion.Slerp(keyFramedOrientation, rb.RigidBody.transform.rotation, t);
#if MRE_PHYSICS_DEBUG
							Debug.Log(" Interpolate body " + rb.Id.ToString() + " t=" + t
								+ " time=" + UnityEngine.Time.time
								+ " pos KF:" + keyFramedPos
								+ " dyn:" + rb.RigidBody.transform.position
							    + " interp pos:" + interpolatedPos
								+ " rb vel:" + rb.RigidBody.velocity
								+ " KF vel:" + rb.lastValidLinerVelocityOrPos);
#endif
					// apply these changes only if they are significant in order to not to bother the physics engine
					// for settled objects
					UnityEngine.Vector3 posdiff = rb.RigidBody.transform.position - interpolatedPos;
					if (posdiff.magnitude > interpolationPosEpsilon)
					{
						rb.RigidBody.transform.position = interpolatedPos;
					}
					float angleDiff = Math.Abs(
						UnityEngine.Quaternion.Angle(rb.RigidBody.transform.rotation, interpolatedQuad));
					if (angleDiff > interpolationAngularEps)
					{
						rb.RigidBody.transform.rotation = interpolatedQuad;
					}

					// apply velocity damping if we are in the interpolation phase 
					if (collisionInfo.monitorInfo.keyframedInterpolationRatio >= velocityDampingInterpolationValueStart)
					{
						rb.RigidBody.velocity *= velocityDampingForInterpolation;
						rb.RigidBody.angularVelocity *= velocityDampingForInterpolation;
					}
				}
			}
			else
			{
				// 100% key framing
				rb.RigidBody.isKinematic = true;
				rb.RigidBody.transform.position = keyFramedPos;
				rb.RigidBody.transform.rotation = keyFramedOrientation;
				rb.RigidBody.velocity.Set(0.0f, 0.0f, 0.0f);
				rb.RigidBody.angularVelocity.Set(0.0f, 0.0f, 0.0f);
				collisionInfo.linearVelocity = rb.lastValidLinerVelocityOrPos;
				collisionInfo.angularVelocity = rb.lastValidAngularVelocityorAng;
				collisionInfo.monitorInfo = new CollisionMonitorInfo();
#if MRE_PHYSICS_DEBUG
				if (rb.IsKeyframed)
				{
						Debug.Log(" Remote body: " + rb.Id.ToString() + " is key framed:"
							+ " linvel:" + collisionInfo.linearVelocity
							+ " angvel:" + collisionInfo.angularVelocity);
				}
#endif
			}
			// <todo> add more filtering here to cancel out unnecessary items,
			// but for small number of bodies should be OK
			_switchCollisionInfos.Add(collisionInfo);
		}


		public void PredictAllRemoteBodiesWithOwnedBodies(
			ref SortedList<Guid, RigidBodyPhysicsBridgeInfo> allRigidBodiesOfThePhysicsBridge,
			PredictionTimeParameters timeInfo)
		{
			// clear here all the monitoring since we will re add them
			_monitorCollisionInfo.Clear();
#if MRE_PHYSICS_DEBUG
			Debug.Log(" ===================== ");
#endif

			// test collisions of each owned body with each not owned body
			foreach (var rb in allRigidBodiesOfThePhysicsBridge.Values)
			{
				if (rb.Ownership)
				{
					// test here all the remote-owned collisions and those should be turned to dynamic again.
					foreach (var remoteBodyInfo in _switchCollisionInfos)
					{
						var remoteBody = allRigidBodiesOfThePhysicsBridge[remoteBodyInfo.rigidBodyId].RigidBody;

						// if this is key framed then also on the remote side it will stay key framed
						if (remoteBodyInfo.isKeyframed)
						{
							continue;
						}

						var comDist = (remoteBody.transform.position - rb.RigidBody.transform.position).magnitude;

						var remoteHitPoint = remoteBody.ClosestPointOnBounds(rb.RigidBody.transform.position);
						var ownedHitPoint = rb.RigidBody.ClosestPointOnBounds(remoteBody.transform.position);

						var remoteRelativeHitP = (remoteHitPoint - remoteBody.transform.position);
						var ownedRelativeHitP = (ownedHitPoint - rb.RigidBody.transform.position);

						var radiousRemote = radiusExpansionFactor * remoteRelativeHitP.magnitude;
						var radiusOwnedBody = radiusExpansionFactor * ownedRelativeHitP.magnitude;

						var totalDistance = radiousRemote + radiusOwnedBody + 0.0001f; // avoid division by zero

						// project the linear velocity of the body
						var projectedOwnedBodyPos = rb.RigidBody.transform.position + rb.RigidBody.velocity * timeInfo.DT;
						var projectedComDist = (remoteBody.transform.position - projectedOwnedBodyPos).magnitude;

						var collisionMonitorInfo = remoteBodyInfo.monitorInfo;
						float lastApproxDistance = Math.Min(comDist, projectedComDist);
						collisionMonitorInfo.relativeDistance = lastApproxDistance / totalDistance;

						bool addToMonitor = false;
						bool isWithinCollisionRange = (projectedComDist < totalDistance || comDist < totalDistance);
						float timeSinceCollisionStart = collisionMonitorInfo.timeFromStartCollision;
						bool isAlreadyInMonitor = _monitorCollisionInfo.ContainsKey(remoteBodyInfo.rigidBodyId);
#if MRE_PHYSICS_DEBUG
						//if (remoteBodyInfo.isKeyframed)
						{
							Debug.Log("prprojectedComDistoj: " + projectedComDist + " comDist:" + comDist
								+ " totalDistance:" + totalDistance + " remote body pos:" + remoteBodyInfo.startPosition.ToString()
								+ "input lin vel:" + remoteBodyInfo.linearVelocity + " radiousRemote:" + radiousRemote +
								" radiusOwnedBody:" + radiusOwnedBody + " relative dist:" + collisionMonitorInfo.relativeDistance
								+ " timeSinceCollisionStart:" + timeSinceCollisionStart + " isAlreadyInMonitor:" + isAlreadyInMonitor);
						}
#endif

						// unconditionally add to the monitor stream if this is a reasonable collision and we are only at the beginning
						if (collisionMonitorInfo.timeFromStartCollision > timeInfo.halfDT &&
							timeSinceCollisionStart <= startInterpolatingBack &&
							collisionMonitorInfo.relativeDistance < inCollisionRangeRelativeDistanceFactor)
						{
#if MRE_PHYSICS_DEBUG
							//if (remoteBodyInfo.isKeyframed)
							{
								Debug.Log(" unconditionally add to collision stream time:" + timeSinceCollisionStart +
									" relative dist:" + collisionMonitorInfo.relativeDistance);
							}
#endif
							addToMonitor = true;
						}

						// switch over smoothly to the key framing
						if (!addToMonitor &&
							timeSinceCollisionStart > 5 * timeInfo.DT && // some basic check for lower limit
																		 //(!isAlreadyInMonitor || collisionMonitorInfo.relativeDistance > 1.2f) && // if this is already added then ignore large values
							timeSinceCollisionStart <= endInterpolatingBack)
						{
							addToMonitor = true;
							float oldVal = collisionMonitorInfo.keyframedInterpolationRatio;
							// we might enter here before startInterpolatingBack
							timeSinceCollisionStart = Math.Max(timeSinceCollisionStart, startInterpolatingBack + timeInfo.DT);
							// progress the interpolation
							collisionMonitorInfo.keyframedInterpolationRatio =
								invInterpolationTimeWindows * (timeSinceCollisionStart - startInterpolatingBack);
#if MRE_PHYSICS_DEBUG
							Debug.Log(" doing interpolation new:" + collisionMonitorInfo.keyframedInterpolationRatio +
								" old:" + oldVal + " relative distance:" + collisionMonitorInfo.relativeDistance);
#endif
							// stop interpolation
							if (collisionMonitorInfo.relativeDistance < collisionRelDistLimit
								&& collisionMonitorInfo.keyframedInterpolationRatio > limitCollisionInterpolation)
							{
#if MRE_PHYSICS_DEBUG
								Debug.Log(" Stop interpolation time with DT:" + timeInfo.DT +
									" Ratio:" + collisionMonitorInfo.keyframedInterpolationRatio +
									" relDist:" + collisionMonitorInfo.relativeDistance +
									" t=" + collisionMonitorInfo.timeFromStartCollision);
#endif
								// --- this is a different way to drop the percentage for key framing ---
								//collisionMonitorInfo.timeFromStartCollision -=
								//	Math.Min( 4.0f, ( (1.0f/limitCollisionInterpolation) * collisionMonitorInfo.keyframedInterpolationRatio) )* DT;

								// this version just drops the percentage back to the limit 
								collisionMonitorInfo.timeFromStartCollision = startInterpolatingBack
									+ limitCollisionInterpolation * (endInterpolatingBack - startInterpolatingBack);
								collisionMonitorInfo.keyframedInterpolationRatio = limitCollisionInterpolation;
							}
						}

						// we add to the collision stream either when it is within range or 
						if (isWithinCollisionRange || addToMonitor)
						{
							// add to the monitor stream
							if (isAlreadyInMonitor)
							{
								// if there is existent collision already with this remote body, then build the minimum
								var existingMonitorInfo = _monitorCollisionInfo[remoteBodyInfo.rigidBodyId];
#if MRE_PHYSICS_DEBUG
								Debug.Log(" START merge collision info: " + remoteBodyInfo.rigidBodyId.ToString() +
									" r:" + existingMonitorInfo.keyframedInterpolationRatio + " d:" + existingMonitorInfo.relativeDistance);
#endif
								existingMonitorInfo.timeFromStartCollision =
									Math.Min(existingMonitorInfo.timeFromStartCollision, collisionMonitorInfo.timeFromStartCollision);
								existingMonitorInfo.relativeDistance =
									Math.Min(existingMonitorInfo.relativeDistance, collisionMonitorInfo.relativeDistance);
								existingMonitorInfo.keyframedInterpolationRatio =
									Math.Min(existingMonitorInfo.keyframedInterpolationRatio, collisionMonitorInfo.keyframedInterpolationRatio);
								collisionMonitorInfo = existingMonitorInfo;
#if MRE_PHYSICS_DEBUG
								// <todo> check why is this working sometimes weired.
								_monitorCollisionInfo[remoteBodyInfo.rigidBodyId] = collisionMonitorInfo;
								existingMonitorInfo = _monitorCollisionInfo[remoteBodyInfo.rigidBodyId];
								Debug.Log(" merge collision info: " + remoteBodyInfo.rigidBodyId.ToString() +
									" r:" + existingMonitorInfo.keyframedInterpolationRatio + " d:" + existingMonitorInfo.relativeDistance);
#endif
							}
							else
							{
								_monitorCollisionInfo.Add(remoteBodyInfo.rigidBodyId, collisionMonitorInfo);
#if MRE_PHYSICS_DEBUG
								//if (remoteBodyInfo.isKeyframed)
								{
									var existingMonitorInfo = _monitorCollisionInfo[remoteBodyInfo.rigidBodyId];
									Debug.Log(" Add collision info: " + remoteBodyInfo.rigidBodyId.ToString() +
										" r:" + existingMonitorInfo.keyframedInterpolationRatio + " d:" + existingMonitorInfo.relativeDistance);
								}
#endif
							}

							// this is a new collision
							if (collisionMonitorInfo.timeFromStartCollision < timeInfo.halfDT)
							{
								// switch back to dynamic
								remoteBody.isKinematic = false;
								remoteBody.transform.position = remoteBodyInfo.startPosition;
								remoteBody.transform.rotation = remoteBodyInfo.startOrientation;
								remoteBody.velocity = remoteBodyInfo.linearVelocity;
								remoteBody.angularVelocity = remoteBodyInfo.angularVelocity;
#if MRE_PHYSICS_DEBUG
								//if (remoteBodyInfo.isKeyframed)
								{
									Debug.Log(" remote body velocity SWITCH to collision: " + remoteBody.velocity.ToString()
									   + "  start position:" + remoteBody.transform.position.ToString()
									   + " linVel:" + remoteBody.velocity + " angVel:" + remoteBody.angularVelocity);
								}
#endif
							}
							else
							{
#if MRE_PHYSICS_DEBUG
								// this is a previous collision so do nothing just leave dynamic
								Debug.Log(" remote body velocity stay in collision: " + remoteBody.velocity.ToString() +
									"  start position:" + remoteBody.transform.position.ToString() +
									" relative dist:" + collisionMonitorInfo.relativeDistance +
									" Ratio:" + collisionMonitorInfo.keyframedInterpolationRatio );
#endif
							}
						}
					}
				}
			} // end for each 

		} // end of PredictAllRemoteBodiesWithOwnedBodies

		public void Clear()
		{
			_switchCollisionInfos.Clear();
			_monitorCollisionInfo.Clear();
		}
	}
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using MixedRealityExtension.Core.Physics;
using System;
using System.Collections.Generic;
using MixedRealityExtension.Util;
using UnityEngine;
using MixedRealityExtension.Patching.Types;
using MixedRealityExtension.Patching;

namespace MixedRealityExtension.Core
{
	public class RigidBodyPhysicsBridgeInfo
	{
		public RigidBodyPhysicsBridgeInfo(Guid id, UnityEngine.Rigidbody rb, bool ownership)
		{
			Id = id;
			RigidBody = rb;
			Ownership = ownership;
			lastTimeKeyFramedUpdate = 0.0f;
			lastValidLinerVelocityOrPos.Set(0.0f, 0.0f, 0.0f);
			lastValidAngularVelocityorAng.Set(0.0f, 0.0f, 0.0f);
			numOfConsequentSleepingFrames = 0;
			IsKeyframed = false;
		}

		/// the rigid body identifier
		public Guid Id;

		/// Unity rigid body 
		public UnityEngine.Rigidbody RigidBody;

		/// these 3 fields are used to store the actual velocities ,
		/// IF this body is owned then here we store the transform that last time was sent
		public float lastTimeKeyFramedUpdate;
		public UnityEngine.Vector3 lastValidLinerVelocityOrPos;
		public UnityEngine.Vector3 lastValidAngularVelocityorAng;

		/// true if this rigid body is owned by this client
		public bool Ownership;
		/// if the body is moved by key-framing then this is true
		public bool IsKeyframed;

		// <todo> add owner of the body for faster access ?

		/// the sleeping condition needs to be fulfilled for a couple of frames such that we mark this body as sleeping
		/// this sill be capped at the maximum to avoid overflow
		public short numOfConsequentSleepingFrames;
		/// when transmitting the transforms we store if this body is sleeping
		public Patching.Types.MotionType sendMotionType;
	}

	/// <summary>
	/// the main class that is the bridge between the MRE Unity and the networked physics logic
	/// </summary>
	public class PhysicsBridge
	{
		/// <summary>
		///  Local user, upload transforms owned by this user.
		/// </summary>
		public Guid? LocalUserId { get; set; } = null;

		/// stores the number of transforms that should be have been sent (without cap) to all consumers
		private int _lastNumberOfTransformsToBeSent = 0;

		// indicates if all local rigid bodies are sleeping.
		// if so, we can send updates less frequently.
		bool _allOwnedBodiesAreSleeping = true;

		// list of tracked rigid bodies
		private SortedList<Guid, RigidBodyPhysicsBridgeInfo> _rigidBodies = new SortedList<Guid, RigidBodyPhysicsBridgeInfo>();

		// provides transforms and timestamp for rigid bodies
		TimeSnapshotManager _snapshotManager = new TimeSnapshotManager();

		/// the prediction object
		IPrediction _predictor = new PredictionInterpolation();

		/// maximal estimated linear velocity
		private const float _maxEstimatedLinearVelocity = 30.0F;
		/// maximal estimated angular velocity
		private const float _maxEstimatedAngularVelocity = 5.0F;

		// ---- fields to be used to check for update the server side updates ----
		private Dictionary<Guid, PhysicsTranformServerUploadPatch.OneActorUpdate> _lastServerUploadedTransforms =

			new Dictionary<Guid, PhysicsTranformServerUploadPatch.OneActorUpdate>();
		/// for the low frequency server transforms upload the last time when the update happened
		private float _lastServerTransformUploadSentTime = 0.0F;

		#region Rigid Body Management

		public void addRigidBody(Guid id, UnityEngine.Rigidbody rigidbody, Guid source, bool isKinematic)
		{
			UnityEngine.Debug.Assert(!_rigidBodies.ContainsKey(id), "PhysicsBridge already has an entry for rigid body with specified ID.");

			bool isLocalSumulationAuthoritative = LocalUserId == source;

			var rb = new RigidBodyPhysicsBridgeInfo(id, rigidbody, isLocalSumulationAuthoritative);

			_rigidBodies.Add(id, rb);

			if (isLocalSumulationAuthoritative)
			{
				rb.IsKeyframed = isKinematic;
				rigidbody.isKinematic = isKinematic;
			}
			else
			{
				rigidbody.isKinematic = true;

				_snapshotManager.RegisterOrUpateRigidBody(id, source);
			}
		}

		public void removeRigidBody(Guid id)
		{
			if (!_rigidBodies.ContainsKey(id))
			{
				return;
			}

			var rb = _rigidBodies[id];

			if (!rb.Ownership)
			{
				_snapshotManager.UnregisterRigidBody(id);
			}

			_rigidBodies.Remove(id);
		}

		public void setRigidBodyOwnership(Guid id, Guid sourceId, bool isKinematic)
		{
			if (!_rigidBodies.ContainsKey(id))
			{
				return;
			}

			bool isLocalSumulationAuthoritative = LocalUserId == sourceId;

			UnityEngine.Debug.Assert(_rigidBodies[id].Ownership != isLocalSumulationAuthoritative, "Rigid body with specified ID is already registered with same ownership flag.");

			if (isLocalSumulationAuthoritative)
			{
				_rigidBodies[id].IsKeyframed = isKinematic;
				_rigidBodies[id].RigidBody.isKinematic = isKinematic;

				_snapshotManager.UnregisterRigidBody(id);
			}
			else
			{
				_snapshotManager.RegisterOrUpateRigidBody(id, sourceId);
			}

			_rigidBodies[id].Ownership = isLocalSumulationAuthoritative;
		}

		public void setKeyframed(Guid id, bool isKeyFramed)
		{
			if (_rigidBodies.ContainsKey(id))
			{
				var rb = _rigidBodies[id];
				if (rb.Ownership)
				{
					rb.IsKeyframed = isKeyFramed;
				}
			}
		}

		#endregion

		#region Transform Streaming

		/// <summary>
		/// Add transform snapshot from specified source.
		/// </summary>
		/// <param name="sourceId">Snapshot source identifier.</param>
		/// <param name="snapshot">List of transform at specified timestamp.</param>
		public void addSnapshot(Guid sourceId, Snapshot snapshot)
		{
			_snapshotManager.addSnapshot(sourceId, snapshot);
		}

		#endregion

		public void FixedUpdate(UnityEngine.Transform rootTransform)
		{
			// - physics rigid body management
			// -set transforms/velocities for key framed bodies

			// get all the prediction time infos in this struct
			PredictionTimeParameters timeInfo = new PredictionTimeParameters(UnityEngine.Time.fixedDeltaTime);

			// start the predictor
			_predictor.StartBodyPredicitonForNextFrame();

			int index = 0;
			MultiSourceCombinedSnapshot snapshot;
			_snapshotManager.Step(timeInfo.DT, out snapshot);
			_snapshotManager.UpdateDebugDisplay(rootTransform); // guarded by ifdef internally

			foreach (var rb in _rigidBodies.Values)
			{
				// if the body is owned then we only set the kinematic flag for the physics
				if (rb.Ownership)
				{
					if (rb.IsKeyframed)
					{
						rb.RigidBody.isKinematic = true;
					}
					else
					{
						rb.RigidBody.isKinematic = false;
					}
					continue;
				}

				// Find corresponding rigid body info.
				// since both are sorted list this should hit without index=0 at the beginning
				while (index < snapshot.RigidBodies.Count && rb.Id.CompareTo(snapshot.RigidBodies.Values[index].Id) > 0)
				{
					index++;
				}

				if (index < snapshot.RigidBodies.Count && rb.Id == snapshot.RigidBodies.Values[index].Id)
				{
					// todo: kick-in prediction if we are missing an update for this rigid body
					//if (!snapshot.RigidBodies.Values[index].HasUpdate)
					//{
					//	rb.RigidBody.isKinematic = false;
					//	continue;
					//}

					RigidBodyTransform transform = snapshot.RigidBodies.Values[index].Transform;
					float timeOfSnapshot = snapshot.RigidBodies.Values[index].LocalTime;

					// get the key framed stream, and compute implicit velocities
					UnityEngine.Vector3 keyFramedPos = rootTransform.TransformPoint(transform.Position);
					UnityEngine.Quaternion keyFramedOrientation = rootTransform.rotation * transform.Rotation;
					UnityEngine.Vector3 JBLinearVelocity =
						rootTransform.rotation * snapshot.RigidBodies.Values[index].LinearVelocity;
					UnityEngine.Vector3 JBAngularVelocity =
						rootTransform.rotation * snapshot.RigidBodies.Values[index].AngularVelocity;
					// if there is a really new update then also store the implicit velocity
					if (rb.lastTimeKeyFramedUpdate < timeOfSnapshot)
					{
						// we moved the velocity estimation into the jitter buffer 
						rb.lastValidLinerVelocityOrPos = JBLinearVelocity;
						rb.lastValidAngularVelocityorAng = JBAngularVelocity;

#if MRE_PHYSICS_DEBUG
						// test the source of large velocities
						if (rb.lastValidLinerVelocityOrPos.sqrMagnitude > _maxEstimatedLinearVelocity * _maxEstimatedLinearVelocity)
						{
							// limited debug version
							Debug.Log(" ACTIVE SPEED LIMIT TRAP RB: " //+ rb.Id.ToString() + " got update lin vel:"
								+ rb.lastValidLinerVelocityOrPos + " ang vel:" + rb.lastValidAngularVelocityorAng
								+ " time:" + timeOfSnapshot
								+ " newR:" + rb.lastTimeKeyFramedUpdate
								+ " hasupdate:" + snapshot.RigidBodies.Values[index].HasUpdate
								+  " DangE:" + eulerAngles + " DangR:" + radianAngles );
						}
#endif

						// cap the velocities
						rb.lastValidLinerVelocityOrPos = UnityEngine.Vector3.ClampMagnitude(
							rb.lastValidLinerVelocityOrPos, _maxEstimatedLinearVelocity);
						rb.lastValidAngularVelocityorAng = UnityEngine.Vector3.ClampMagnitude(
							rb.lastValidAngularVelocityorAng, _maxEstimatedAngularVelocity);
						// if body is sleeping then all velocities are zero
						if (snapshot.RigidBodies.Values[index].motionType == Patching.Types.MotionType.Sleeping)
						{
							rb.lastValidLinerVelocityOrPos.Set(0.0F, 0.0F, 0.0F);
							rb.lastValidAngularVelocityorAng.Set(0.0F, 0.0F, 0.0F);
						}
#if MRE_PHYSICS_DEBUG
						if (true)
						{
						    // limited debug version
							Debug.Log(" Remote body: " + rb.Id.ToString() + " got update lin vel:"
								+ rb.lastValidLinerVelocityOrPos + " ang vel:" + rb.lastValidAngularVelocityorAng
								+ " time:" + timeOfSnapshot + " newR:" + rb.lastTimeKeyFramedUpdate);						    
						}
						else
						{
							Debug.Log(" Remote body: " + rb.Id.ToString() + " got update lin vel:"
								+ rb.lastValidLinerVelocityOrPos + " ang vel:" + rb.lastValidAngularVelocityorAng
								//+ " DangE:" + eulerAngles + " DangR:" + radianAngles
								+ " time:" + timeOfSnapshot + " newp:" + keyFramedPos
								+ " newR:" + keyFramedOrientation
								+ " oldP:" + rb.RigidBody.transform.position
								+ " oldR:" + rb.RigidBody.transform.rotation
								+ " OriginalRot:" + transform.Rotation
								+ " keyF:" + rb.RigidBody.isKinematic
								+ " KF:" + rb.IsKeyframed);
						}
#endif
						// cap the velocities
						rb.lastValidLinerVelocityOrPos = UnityEngine.Vector3.ClampMagnitude(
							rb.lastValidLinerVelocityOrPos, _maxEstimatedLinearVelocity);
						rb.lastValidAngularVelocityorAng = UnityEngine.Vector3.ClampMagnitude(
							rb.lastValidAngularVelocityorAng, _maxEstimatedAngularVelocity);
						// if body is sleeping then all velocities are zero
						if (snapshot.RigidBodies.Values[index].motionType == Patching.Types.MotionType.Sleeping)
						{
							rb.lastValidLinerVelocityOrPos.Set(0.0F, 0.0F, 0.0F);
							rb.lastValidAngularVelocityorAng.Set(0.0F, 0.0F, 0.0F);
						}
#if MRE_PHYSICS_DEBUG
						Debug.Log(" Remote body: " + rb.Id.ToString() + " got update lin vel:"
							+ rb.lastValidLinerVelocityOrPos + " ang vel:" + rb.lastValidAngularVelocityorAng
							//+ " DangE:" + eulerAngles + " DangR:" + radianAngles
							+ " time:" + timeOfSnapshot + " newp:" + keyFramedPos
							+ " newR:" + keyFramedOrientation
							+ " incUpdateDt:" + invUpdateDT
							+ " oldP:" + rb.RigidBody.transform.position
							+ " oldR:" + rb.RigidBody.transform.rotation
							+ " OriginalRot:" + transform.Rotation
							+ " keyF:" + rb.RigidBody.isKinematic
							+ " KF:" + rb.IsKeyframed);
#endif
					}

					rb.lastTimeKeyFramedUpdate = timeOfSnapshot;
					rb.IsKeyframed = (snapshot.RigidBodies.Values[index].motionType == Patching.Types.MotionType.Keyframed);

					// code to disable prediction and to use just key framing (and comment out the prediction)
					//rb.RigidBody.isKinematic = true;
					//rb.RigidBody.transform.position = keyFramedPos;
					//rb.RigidBody.transform.rotation = keyFramedOrientation;
					//rb.RigidBody.velocity.Set(0.0f, 0.0f, 0.0f);
					//rb.RigidBody.angularVelocity.Set(0.0f, 0.0f, 0.0f);

					// call the predictor with this remotely owned body
					_predictor.AddAndProcessRemoteBodyForPrediction(rb, transform,
						keyFramedPos, keyFramedOrientation, timeOfSnapshot, timeInfo);
				}
			}

			// call the predictor
			_predictor.PredictAllRemoteBodiesWithOwnedBodies(ref _rigidBodies, timeInfo);
		}

		/// <summary>
		/// Generate rigid body transform snapshot for owned transforms with specified timestamp.
		/// </summary>
		/// <param name="time">Snapshot timestamp.</param>
		/// <param name="rootTransform">Root transform.</param>
		/// <returns>Generated snapshot.</returns>
		public Snapshot GenerateSnapshot(float time, UnityEngine.Transform rootTransform)
		{
			// collect transforms from owned rigid bodies
			// and generate update packet/snapshot
			
			// these constants define when a body is considered to be sleeping
			const float globalToleranceMultipier = 1.0F;
			const float maxSleepingSqrtLinearVelocity = 0.1F * globalToleranceMultipier;
			const float maxSleepingSqrtAngularVelocity = 0.1F * globalToleranceMultipier;
			const float maxSleepingSqrtPositionDiff = 0.02F * globalToleranceMultipier;
			const float maxSleepingSqrtAngularEulerDiff = 0.15F * globalToleranceMultipier;

			const short limitNoUpdateForSleepingBodies = 500;
			const short numConsecutiveSleepingTrueConditionForNoUpdate = 5;

			int numSleepingBodies = 0;
			int numOwnedBodies = 0;

			List<Snapshot.TransformInfo> transforms = new List<Snapshot.TransformInfo>(_rigidBodies.Count);

			foreach (var rb in _rigidBodies.Values)
			{
				if (!rb.Ownership)
				{
					rb.numOfConsequentSleepingFrames = 0;
					continue;
				}

				RigidBodyTransform transform;
				{
					transform.Position = rootTransform.InverseTransformPoint(rb.RigidBody.transform.position);
					transform.Rotation = UnityEngine.Quaternion.Inverse(rootTransform.rotation) * rb.RigidBody.transform.rotation;
				}

				numOwnedBodies++;
				Patching.Types.MotionType mType = (rb.IsKeyframed) ? (Patching.Types.MotionType.Keyframed)
					: (Patching.Types.MotionType.Dynamic);

				UnityEngine.Vector3 posDiff = rb.lastValidLinerVelocityOrPos - transform.Position;
				UnityEngine.Vector3 rotDiff = UtilMethods.TransformEulerAnglesToRadians(rb.lastValidAngularVelocityorAng - transform.Rotation.eulerAngles);

				bool isBodySleepingInThisFrame =
					(!rb.IsKeyframed) && // if body is key framed and owned then we should just feed the jitter buffer
					(rb.RigidBody.velocity.sqrMagnitude < maxSleepingSqrtLinearVelocity
					  && rb.RigidBody.angularVelocity.sqrMagnitude < maxSleepingSqrtAngularVelocity
					  && posDiff.sqrMagnitude < maxSleepingSqrtPositionDiff
					  && rotDiff.sqrMagnitude < maxSleepingSqrtAngularEulerDiff);

				if (isBodySleepingInThisFrame)
				{
					rb.numOfConsequentSleepingFrames++;
					rb.numOfConsequentSleepingFrames = (rb.numOfConsequentSleepingFrames > (short)limitNoUpdateForSleepingBodies) ?
						(short)limitNoUpdateForSleepingBodies : rb.numOfConsequentSleepingFrames;
				}
				else
				{
					rb.numOfConsequentSleepingFrames = 0;
				}

				// this is the real condition to put a body to sleep
				bool isBodySleeping = (rb.numOfConsequentSleepingFrames > numConsecutiveSleepingTrueConditionForNoUpdate);

				// test if this is sleeping, and when this was newly added then 
				if (rb.lastTimeKeyFramedUpdate > 0.001F && isBodySleeping &&
					rb.sendMotionType == Patching.Types.MotionType.Sleeping &&
					rb.numOfConsequentSleepingFrames < limitNoUpdateForSleepingBodies)
				{
					// condition for velocity and positions are triggered and we already told the consumers to make body sleep, so just skip this update
					numSleepingBodies++;
					continue;
				}

				mType = (isBodySleeping) ? (Patching.Types.MotionType.Sleeping) : mType;
				// here we handle the case when after of 300 frames with no update we should send one update at least
				if (rb.numOfConsequentSleepingFrames >= limitNoUpdateForSleepingBodies)
				{
					rb.numOfConsequentSleepingFrames = numConsecutiveSleepingTrueConditionForNoUpdate + 1;
				}

				// store the last update informations
				rb.sendMotionType = mType;
				rb.lastTimeKeyFramedUpdate = time;
				rb.lastValidLinerVelocityOrPos = transform.Position;
				rb.lastValidAngularVelocityorAng = transform.Rotation.eulerAngles;

				transforms.Add(new Snapshot.TransformInfo(rb.Id, transform, mType));
#if MRE_PHYSICS_DEBUG
				Debug.Log(" SEND Remote body: " + rb.Id.ToString() + " OriginalRot:" + transform.Rotation
					+ " RigidBodyRot:" + rb.RigidBody.transform.rotation
					+ " lv:" + rb.RigidBody.velocity + " av:" + rb.RigidBody.angularVelocity
					+ " posDiff:" + posDiff + " rotDiff:" + rotDiff + " isKeyF:" + rb.IsKeyframed);
#endif
			}

			Snapshot.SnapshotFlags snapshotFlag = Snapshot.SnapshotFlags.NoFlags;

			bool allBodiesAreSleepingNew = numOwnedBodies == numSleepingBodies;
			if (allBodiesAreSleepingNew != _allOwnedBodiesAreSleeping)
			{
				_allOwnedBodiesAreSleeping = allBodiesAreSleepingNew;
				snapshotFlag = Snapshot.SnapshotFlags.ResetJitterBuffer;
			}

			_lastNumberOfTransformsToBeSent = numOwnedBodies;

			var ret = new Snapshot(time, transforms, snapshotFlag);

#if MRE_PHYSICS_DEBUG
			Debug.Log(" Client:" + " Total number of sleeping bodies: " + numSleepingBodies + " total RBs" + _rigidBodies.Count
			 + " num owned " + numOwnedBodies + " num sent transforms " + transforms.Count
			 + " send:"  +  ret.DoSendThisSnapshot() );
#endif

			return ret;
		}

		internal void Reset()
		{
			_lastNumberOfTransformsToBeSent = 0;
			_allOwnedBodiesAreSleeping = true;

			_rigidBodies.Clear();
			_snapshotManager.Clear();
			_predictor.Clear();
		}

		/// returns true if the low frequency upload should be sent to the server
		public bool shouldSendLowFrequencyTransformUpload(float systemTime)
		{
			const float sendPeriod = 3.0F; // time period in seconds
			return (systemTime - _lastServerTransformUploadSentTime > sendPeriod);
		}

		/// generates the message that updates the transforms on the server side (this is done in a low frequency manner)
		/// <returns> message that should be sent to the server</returns>
		public PhysicsTranformServerUploadPatch GenerateServerTransformUploadPatch(Guid instanceId, float systemTime)
		{
			var ret = new PhysicsTranformServerUploadPatch();
			int numownedbodies = 0;
			int numUpdatedTransform = 0;
			List<PhysicsTranformServerUploadPatch.OneActorUpdate> allUpdates = new List<PhysicsTranformServerUploadPatch.OneActorUpdate>();

			// first loop counts how many RBs do we own
			foreach (var rb in _rigidBodies.Values)
			{
				if (rb.Ownership)
				{
					numownedbodies++;
					var actor = rb.RigidBody.gameObject.GetComponent<Actor>();
					if (actor != null)
					{
						// MUST be the same as  PatchingUtilMethods.GenerateLocalTransformPatch
						// and  PatchingUtilMethods.GenerateAppTransformPatch
						//update.localTransforms.Position = actor.transform.position;

						var update = new PhysicsTranformServerUploadPatch.OneActorUpdate(
							actor.Id,
							actor.transform.position, actor.transform.rotation,
							actor.App.SceneRoot.transform.InverseTransformPoint(actor.transform.position),
							Quaternion.Inverse(actor.App.SceneRoot.transform.rotation) * actor.transform.rotation
							);

						// todo see if we sent this update already
						if (_lastServerUploadedTransforms.ContainsKey(rb.Id))
						{
							var lastUpdate = _lastServerUploadedTransforms[rb.Id];
							if (!lastUpdate.isEqual(update))
							{
								allUpdates.Add(update);
								numUpdatedTransform++;
								_lastServerUploadedTransforms[rb.Id] = update;
							}
						}
						else
						{
							// add an update anyway
							allUpdates.Add(update);
							_lastServerUploadedTransforms.Add(rb.Id, update);
							numUpdatedTransform++;
						}
					}
				}
				else
				{
					// remove if this is in this list
					if (_lastServerUploadedTransforms.ContainsKey(rb.Id))
					{
						_lastServerUploadedTransforms.Remove(rb.Id);
					}
				}
			}

			ret.updates = new PhysicsTranformServerUploadPatch.OneActorUpdate[numownedbodies];
			ret.TransformCount = numUpdatedTransform;
			ret.Id = instanceId;

			numownedbodies = 0;
			foreach (var update in allUpdates)
			{
				// add the updates
				ret.updates[numownedbodies++] = update;
			}
			// store the last time
			_lastServerTransformUploadSentTime = systemTime;
			return ret;
		}
	}
}

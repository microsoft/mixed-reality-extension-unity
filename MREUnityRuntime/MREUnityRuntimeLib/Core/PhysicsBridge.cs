// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using MixedRealityExtension.Core.Physics;
using System;
using System.Collections.Generic;

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
			lastValidLinerVelocity.Set(0.0f, 0.0f, 0.0f);
			lastValidAngularVelocity.Set(0.0f, 0.0f, 0.0f);

			IsKeyframed = false;
		}

		public Guid Id;

		public UnityEngine.Rigidbody RigidBody;

		/// these 3 fields are used to store the actual velocities
		public float lastTimeKeyFramedUpdate;
		public UnityEngine.Vector3 lastValidLinerVelocity;
		public UnityEngine.Vector3 lastValidAngularVelocity;

		/// true if this rigid body is owned by this client
		public bool Ownership;

		public bool IsKeyframed;
	}

	/// <summary>
	/// the main class that is the bridge between the MRE Unity and the networked physics logic
	/// </summary>
	public class PhysicsBridge
	{

		private int _countOwnedTransforms = 0;
		private int _countStreamedTransforms = 0;

		private SortedList<Guid, RigidBodyPhysicsBridgeInfo> _rigidBodies = new SortedList<Guid, RigidBodyPhysicsBridgeInfo>();

		TimeSnapshotManager _snapshotManager = new TimeSnapshotManager();

		/// the prediction object
		IPrediction _predictor = new PredictionInterpolation();

		public PhysicsBridge()
		{
		}

		#region Rigid Body Management

		public void addRigidBody(Guid id, UnityEngine.Rigidbody rigidbody, bool ownership)
		{
			UnityEngine.Debug.Assert(!_rigidBodies.ContainsKey(id), "PhysicsBridge already has an entry for rigid body with specified ID.");

			_rigidBodies.Add(id, new RigidBodyPhysicsBridgeInfo(id, rigidbody, ownership));

			if (ownership)
			{
				rigidbody.isKinematic = false;
				_countOwnedTransforms++;
			}
			else
			{
				rigidbody.isKinematic = true;
				_countStreamedTransforms++;
			}
		}

		public void removeRigidBody(Guid id)
		{
			UnityEngine.Debug.Assert(_rigidBodies.ContainsKey(id), "PhysicsBridge don't have rigid body with specified ID.");

			var rb = _rigidBodies[id];

			if (rb.Ownership)
			{
				_countOwnedTransforms--;
			}
			else
			{
				_countStreamedTransforms--;
			}

			_rigidBodies.Remove(id);
		}

		public void setRigidBodyOwnership(Guid id, bool ownership)
		{
			UnityEngine.Debug.Assert(_rigidBodies.ContainsKey(id), "PhysicsBridge don't have rigid body with specified ID.");
			UnityEngine.Debug.Assert(_rigidBodies[id].Ownership != ownership, "Rigid body with specified ID is already registered with same ownership flag.");

			if (ownership)
			{
				_countOwnedTransforms++;
				_countStreamedTransforms--;
			}
			else
			{
				_countOwnedTransforms--;
				_countStreamedTransforms++;
			}

			_rigidBodies[id].Ownership = ownership;
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
			PreditionTimeInterface timeInfo = new PreditionTimeInterface(UnityEngine.Time.fixedDeltaTime);

			// start the predictor
			_predictor.StartBodyPredicitonForNextFrame();

			int index = 0;
			MultiSourceCombinedSnapshot snapshot = _snapshotManager.GetNextSnapshot(timeInfo.DT);

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

				// <todo> make sure that this will always hit this loop ???
				// Find corresponding rigid body info.
				index = 0;
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
					// if there is a really new update then also store the implicit velocity
					if (rb.lastTimeKeyFramedUpdate < timeOfSnapshot)
					{
						// <todo> for long running times this could be a problem 
						float invUpdateDT = 1.0f / (timeOfSnapshot - rb.lastTimeKeyFramedUpdate);
						rb.lastValidLinerVelocity = (keyFramedPos - rb.RigidBody.transform.position) * invUpdateDT;
						rb.lastValidAngularVelocity = (
							UnityEngine.Quaternion.Inverse(rb.RigidBody.transform.rotation)
						 * keyFramedOrientation).eulerAngles * invUpdateDT;
					}
					rb.lastTimeKeyFramedUpdate = timeOfSnapshot;

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

			List<Snapshot.TransformInfo> transforms = new List<Snapshot.TransformInfo>(_rigidBodies.Count);

			foreach (var rb in _rigidBodies.Values)
			{
				if (!rb.Ownership)
				{
					continue;
				}

				RigidBodyTransform transform;
				{
					transform.Position = rootTransform.InverseTransformPoint(rb.RigidBody.transform.position);
					transform.Rotation = UnityEngine.Quaternion.Inverse(rootTransform.rotation) * rb.RigidBody.transform.rotation;
				}

				transforms.Add(new Snapshot.TransformInfo(rb.Id, transform));
			}

			return new Snapshot(time, transforms);
		}

		public void LateUpdate()
		{
			// smooth transform update to hide artifacts for rendering
		}
	}
}

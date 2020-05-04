using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MixedRealityExtension.Core
{
	public class PhysicsBridge
	{
		public class SnapshotBuffer
		{
			/// <summary>
			/// this stores the snapshots received from all other clients, each client sends the owned updates of the actors (RBs)
			/// </summary>
			/// If one wants an updated for a remote body then needs to go though all these separate updates and one of the updates from one client we should find the actor ID
			Dictionary<Guid, Snapshot> _snapshots = new Dictionary<Guid, Snapshot>();

			public void addSnapshot(Snapshot snapshot)
			{
				// currently keep only the latest snapshot from same source

				if (_snapshots.ContainsKey(snapshot.SourceAppId))
				{
					var oldSnapshot = _snapshots[snapshot.SourceAppId];

					if (snapshot.Time > oldSnapshot.Time)
					{
						_snapshots[snapshot.SourceAppId] = snapshot;
					}
				}
				else
				{
					_snapshots.Add(snapshot.SourceAppId, snapshot);
				}
			}

			public Snapshot.SnapshotTransform getTransform(Guid rbId)
			{
				foreach (var s in _snapshots.Values)
				{
					foreach (var t in s.snapshotTransforms)
					{
						if (t.Id == rbId)
						{
							return t.sTransform;
						}
					}
				}
				return null;
			}
		}

		public class Snapshot
		{
			public class SnapshotTransform
			{
				public UnityEngine.Vector3 Position;
				public UnityEngine.Quaternion Rotation;
			}

			public class SnapshotTrasformInfo
			{
				public Guid Id;
				public SnapshotTransform sTransform = new SnapshotTransform();
			}

			public Guid SourceAppId;

			public float Time;

			public List<SnapshotTrasformInfo> snapshotTransforms = new List<SnapshotTrasformInfo>();
		}

		/// <summary>
		/// to monitor the collisions between frames this we need to store per contact pair. 
		/// </summary>
		public class CollisionMonitorInfo
		{
			public float timeFromStartCollision = 0.0f;
			public float lastApproxDistance = float.MaxValue;
			public float relativeApproachingVel = -float.MaxValue;
		}

		/// <summary>
		///  for interactive collisions we need to store the implicit velocities and other stuff to know
		/// </summary>
		public class CollisionSwitchInfo
		{
			public UnityEngine.Vector3 startPosition;
			public UnityEngine.Quaternion startOrientation;
			public UnityEngine.Vector3 linearVelocity;
			public UnityEngine.Vector3 angularVelocity;
			public Guid rigidBodyId;
			public CollisionMonitorInfo monitorInfo;
		}

		private class RigidBodyInfo
		{
			public RigidBodyInfo(Guid id, UnityEngine.Rigidbody rb, bool ownership)
			{
				Id = id;
				RigidBody = rb;
				Ownership = ownership;
			}

			public Guid Id;

			public UnityEngine.Rigidbody RigidBody;

			public bool Ownership; ///< true if this rigid body is owned by this client
		}

		public Guid _appId;

		private int _countOwnedTransforms = 0;
		private int _countStreamedTransforms = 0;

		private Dictionary<Guid, RigidBodyInfo> _rigidBodies = new Dictionary<Guid, RigidBodyInfo>();

		private SnapshotBuffer _snapshotBuffer = new SnapshotBuffer();

		/// <summary>
		/// when we update a body we compute the implicit velocity. This velocity is needed, in case of collisions to switch from kinematic to kinematic=false
		/// </summary>
		List<CollisionSwitchInfo> _switchCollisionInfos = new List<CollisionSwitchInfo>();

		/// <summary>
		/// <todo> the input should be GuidXGuid since one body could collide with multiple other, or one collision is enough (?)
		/// </summary>
		Dictionary<Guid, CollisionMonitorInfo> _monitorCollisionInfo = new Dictionary<Guid, CollisionMonitorInfo>();

		public PhysicsBridge()
		{
		}

		#region Rigid Body Management

		public void addRigidBody(Guid id, UnityEngine.Rigidbody rigidbody, bool ownership)
		{
			UnityEngine.Debug.Assert(!_rigidBodies.ContainsKey(id), "PhysicsBridge already has an entry for rigid body with specified ID.");

			_rigidBodies.Add(id, new RigidBodyInfo(id, rigidbody, ownership));

			if (ownership)
			{
				_countOwnedTransforms++;
			}
			else
			{
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

		#endregion

		#region Transform Streaming

		public void addSnapshot(Snapshot snapshot)
		{
			_snapshotBuffer.addSnapshot(snapshot);
		}

		#endregion

		public void FixedUpdate(UnityEngine.Transform rootTransform)
		{
			// physics rigid body management
			// set transforms/velocities for key framed bodies
			float DT = UnityEngine.Time.fixedDeltaTime;
			float halfDT = 0.5f * DT;
			float invDT = (1.0f / DT);

			// <todo> this is wrong we should keep 
			_switchCollisionInfos.Clear();

			foreach (var rb in _rigidBodies.Values)
			{
				if (rb.Ownership)
				{
					continue;
				}

				Snapshot.SnapshotTransform transform = _snapshotBuffer.getTransform(rb.Id);

				if (transform != null)
				{
					var collisionInfo = new CollisionSwitchInfo();

					collisionInfo.startPosition = rb.RigidBody.transform.position;
					collisionInfo.startOrientation = rb.RigidBody.transform.rotation;
					collisionInfo.rigidBodyId = rb.Id;

					if (_monitorCollisionInfo.ContainsKey(rb.Id))
					{
						rb.RigidBody.isKinematic = false;
						collisionInfo.monitorInfo = _monitorCollisionInfo[rb.Id];
						collisionInfo.monitorInfo.timeFromStartCollision += DT;
						collisionInfo.linearVelocity = rb.RigidBody.velocity;
						collisionInfo.angularVelocity = rb.RigidBody.angularVelocity;

						Debug.Log(" Remote body: " + rb.Id.ToString() + " is dynamic since:" + collisionInfo.monitorInfo.timeFromStartCollision);

						// <todo> if time passes by then make a transformation between key framed and dynamic
						// but only change the positions not the velocity
					}
					else
					{
						rb.RigidBody.isKinematic = true;
						rb.RigidBody.transform.position = /*rootTransform.position +*/ rootTransform.TransformPoint(transform.Position);
						rb.RigidBody.transform.rotation = rootTransform.rotation * transform.Rotation;
						collisionInfo.linearVelocity = (rb.RigidBody.transform.position - collisionInfo.startPosition) * invDT;
						collisionInfo.angularVelocity =
							(UnityEngine.Quaternion.Inverse(collisionInfo.startOrientation)
							 * rb.RigidBody.transform.rotation).eulerAngles * invDT;
						Debug.Log(" Remote body: " + rb.Id.ToString() + " is key framed:");
					}

					_switchCollisionInfos.Add(collisionInfo);
				}
			}

			// clear here all the monitoring since we will re add them
			_monitorCollisionInfo.Clear();

			// test collisions of each owned body with each not owned body
			foreach (var rb in _rigidBodies.Values)
			{
				if (rb.Ownership)
				{
					// <todo> test here all the remote-owned collisions and those should be turned to dynamic again.
					foreach (var remoteBodyInfo in _switchCollisionInfos)
					{
						var remoteBody = _rigidBodies[remoteBodyInfo.rigidBodyId].RigidBody;
						var comDist = (remoteBody.transform.position - rb.RigidBody.transform.position).magnitude;

						var remoteHitPoint = remoteBody.ClosestPointOnBounds(rb.RigidBody.transform.position);
						var ownedHitPoint = rb.RigidBody.ClosestPointOnBounds(remoteBody.transform.position);

						var radiousRemote = 1.3f * (remoteHitPoint - remoteBody.transform.position).magnitude;
						var radiusOwnedBody = 1.3f * (ownedHitPoint - rb.RigidBody.transform.position).magnitude;
						var totalDistance = radiousRemote + radiusOwnedBody;

						// project the linear velocity of the body
						var projectedOwnedBodyPos = rb.RigidBody.transform.position + rb.RigidBody.velocity * DT;
						var projectedComDist = (remoteBody.transform.position - projectedOwnedBodyPos).magnitude;

						Debug.Log("prprojectedComDistoj: " + projectedComDist + " comDist:" + comDist
							+ " totalDistance:" + totalDistance + " remote body pos:" + remoteBodyInfo.startPosition.ToString()
							+ "inpul lin vel:" + remoteBodyInfo.linearVelocity);

						// <todo> this is here a hard switch, alter it should be smooth
						if (projectedComDist < totalDistance || comDist < totalDistance)
						{
							var collisionMonitorInfo = remoteBodyInfo.monitorInfo;
							collisionMonitorInfo.relativeApproachingVel = (comDist - projectedComDist) * invDT;
							collisionMonitorInfo.lastApproxDistance = Math.Min(comDist, projectedComDist);

							// add to the monitor stream 
							_monitorCollisionInfo.Add(remoteBodyInfo.rigidBodyId, collisionMonitorInfo);

							// this is a new collision
							if (collisionMonitorInfo.timeFromStartCollision < halfDT)
							{
								// switch back to dynamic
								remoteBody.isKinematic = false;
								remoteBody.transform.position = remoteBodyInfo.startPosition;
								remoteBody.transform.rotation = remoteBodyInfo.startOrientation;
								remoteBody.velocity = remoteBodyInfo.linearVelocity;
								remoteBody.angularVelocity = remoteBodyInfo.angularVelocity;
								Debug.Log(" remote body velocity SWITCH collision: " + remoteBody.velocity.ToString() +
	                               "  start position:" + remoteBody.transform.position.ToString());
							}
							else
							{
								// do nothing just leave dynamic
								// <todo> make a smoother transition as time passes and consider
								Debug.Log(" remote body velocity stay collision: " + remoteBody.velocity.ToString() +
									"  start position:" + remoteBody.transform.position.ToString());
							}
						}
					}
				}
			}

		}

		public Snapshot Update(UnityEngine.Transform rootTransform)
		{
			// collect transforms from owned rigid bodies
			// and generate update packet/snapshot

			Snapshot snapshot = new Snapshot();
			snapshot.SourceAppId = _appId;
			snapshot.Time = UnityEngine.Time.fixedTime + UnityEngine.Time.fixedDeltaTime;
			snapshot.snapshotTransforms.Capacity = _countOwnedTransforms;

			foreach (var rb in _rigidBodies.Values)
			{
				if (!rb.Ownership)
				{
					continue;
				}

				var ti = new Snapshot.SnapshotTrasformInfo();
				ti.Id = rb.Id;

				//Position = new Vector3Patch(App.SceneRoot.transform.InverseTransformPoint(transform.position)),
				//Rotation = new QuaternionPatch(Quaternion.Inverse(App.SceneRoot.transform.rotation) * transform.rotation)

				//ti.Transform.Position = rb.RigidBody.transform.position - rootTransform.position;
				//ti.Transform.Rotation = UnityEngine.Quaternion.Inverse(rootTransform.rotation) * rb.RigidBody.transform.rotation;

				ti.sTransform.Position = rootTransform.InverseTransformPoint(rb.RigidBody.transform.position);
				ti.sTransform.Rotation = UnityEngine.Quaternion.Inverse(rootTransform.rotation) * rb.RigidBody.transform.rotation;

				snapshot.snapshotTransforms.Add(ti);
			}

			return snapshot;
		}

		public void LateUpdate()
		{
			// smooth transform update to hide artifacts for rendering
		}
	}
}

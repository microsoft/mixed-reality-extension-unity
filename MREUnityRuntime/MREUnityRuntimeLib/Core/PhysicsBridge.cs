using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
		///  for interactive collisions we need to store the implicit velocities and other stuff to know
		/// </summary>
		public class CollisionSwitchInfo
		{
			public UnityEngine.Vector3 startPosition;
			public UnityEngine.Quaternion startOrientation;
			public UnityEngine.Vector3 linearVelocity;
			public UnityEngine.Vector3 angularVelocity;
			public Guid rigidBodyId;
		}


		private class RigidBodyInfo
		{
			public RigidBodyInfo(Guid id, UnityEngine.Rigidbody rb, /*UnityEngine.Collider collider,*/ bool ownership)
			{
				Id = id;
				RigidBody = rb;
				//rbCollider = collider;
				Ownership = ownership;
			}

			public Guid Id;

			public UnityEngine.Rigidbody RigidBody;

			//public UnityEngine.Collider rbCollider;

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
		List<CollisionSwitchInfo> _snapshotCollisionInfos = new List<CollisionSwitchInfo>();

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


			// <todo> this is wrong we should keep 
			_snapshotCollisionInfos.Clear();

			//const float invDeltaTime = 1.0f / UnityEngine.Time.fixedDeltaTime;

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

					rb.RigidBody.isKinematic = true;
					rb.RigidBody.transform.position = /*rootTransform.position +*/ rootTransform.TransformPoint(transform.Position);
					rb.RigidBody.transform.rotation = rootTransform.rotation * transform.Rotation;

					collisionInfo.linearVelocity = rb.RigidBody.transform.position - collisionInfo.startPosition;
					collisionInfo.angularVelocity = (UnityEngine.Quaternion.Inverse(collisionInfo.startOrientation) * rb.RigidBody.transform.rotation).eulerAngles;
					collisionInfo.rigidBodyId = rb.Id;

					_snapshotCollisionInfos.Add(collisionInfo);
				}
			}

			// test collisions of each owned body with each not owned body
			foreach (var rb in _rigidBodies.Values)
			{
				if (rb.Ownership)
				{
					// <todo> test here all the 
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

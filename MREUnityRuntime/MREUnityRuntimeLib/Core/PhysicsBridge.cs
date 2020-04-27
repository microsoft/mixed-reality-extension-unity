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
			Dictionary<Guid, Snapshot> _snapshots = new Dictionary<Guid, Snapshot>();

			public void addSnapshot(Snapshot snapshot)
			{
				// currently keep only the latest snapshot from same source

				if (_snapshots.ContainsKey(snapshot.SourceId))
				{
					var oldSnapshot = _snapshots[snapshot.SourceId];

					if (snapshot.Time > oldSnapshot.Time)
					{
						_snapshots[snapshot.SourceId] = snapshot;
					}
				}
				else
				{
					_snapshots.Add(snapshot.SourceId, snapshot);
				}
			}

			public Snapshot.Transform getTransform(Guid rbId)
			{
				foreach (var s in _snapshots.Values)
				{
					foreach (var t in s.Transforms)
					{
						if (t.Id == rbId)
						{
							return t.Transform;
						}
					}
				}

				return null;
			}
		}

		public class Snapshot
		{
			public class Transform
			{
				public UnityEngine.Vector3 Position;
				public UnityEngine.Quaternion Rotation;
			}

			public class TrasformInfo
			{
				public Guid Id;
				public Transform Transform = new Transform();
			}

			public Guid SourceId;

			public float Time;

			public List<TrasformInfo> Transforms = new List<TrasformInfo>();
		}

		private class RigidBodyInfo
		{
			public RigidBodyInfo(Guid id, UnityEngine.Rigidbody rb, bool ownership)
			{
				Id = id;
				RigidBody = rb;
				Ownership = ownership;
			}

			public Guid  Id;

			public UnityEngine.Rigidbody RigidBody;

			public bool Ownership;
		}

		public Guid _appId;

		private int _countOwnedTransforms = 0;
		private int _countStreamedTransforms = 0;

		private Dictionary<Guid, RigidBodyInfo> _rigidBodies = new Dictionary<Guid, RigidBodyInfo>();

		private SnapshotBuffer _snapshotBuffer = new SnapshotBuffer();

		public PhysicsBridge()
		{
		}

		#region Rigid Body Management

		public void addRigidBody(Guid id, UnityEngine.Rigidbody rigidbody, bool ownership)
		{
			UnityEngine.Debug.Assert(!_rigidBodies.ContainsKey(id), "PhysicsBridge already ahs an entry for rigid body with specified ID.");

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
			// set transforms/velocities for keyframed bodies

			foreach (var rb in _rigidBodies.Values)
			{
				if (rb.Ownership)
				{
					continue;
				}

				Snapshot.Transform transform = _snapshotBuffer.getTransform(rb.Id);

				if (transform != null)
				{
					rb.RigidBody.isKinematic = true;
					rb.RigidBody.transform.position = /*rootTransform.position +*/ rootTransform.TransformPoint(transform.Position);
					rb.RigidBody.transform.rotation = rootTransform.rotation * transform.Rotation;
				}
			}
		}

		public Snapshot Update(UnityEngine.Transform rootTransform)
		{
			// collect transforms from owned rigid bodies
			// and generate update packet/snapshot

			Snapshot snapshot = new Snapshot();
			snapshot.SourceId = _appId;
			snapshot.Time = UnityEngine.Time.fixedTime + UnityEngine.Time.fixedDeltaTime;
			snapshot.Transforms.Capacity = _countOwnedTransforms;

			foreach (var rb in _rigidBodies.Values)
			{
				if (!rb.Ownership)
				{
					continue;
				}

				var ti = new Snapshot.TrasformInfo();
				ti.Id = rb.Id;

				//Position = new Vector3Patch(App.SceneRoot.transform.InverseTransformPoint(transform.position)),
				//Rotation = new QuaternionPatch(Quaternion.Inverse(App.SceneRoot.transform.rotation) * transform.rotation)

				//ti.Transform.Position = rb.RigidBody.transform.position - rootTransform.position;
				//ti.Transform.Rotation = UnityEngine.Quaternion.Inverse(rootTransform.rotation) * rb.RigidBody.transform.rotation;

				ti.Transform.Position = rootTransform.InverseTransformPoint(rb.RigidBody.transform.position);
				ti.Transform.Rotation = UnityEngine.Quaternion.Inverse(rootTransform.rotation) * rb.RigidBody.transform.rotation;

				snapshot.Transforms.Add(ti);
			}

			return snapshot;
		}

		public void LateUpdate()
		{
			// smooth transform update to hide artifacts for rendering
		}
	}
}

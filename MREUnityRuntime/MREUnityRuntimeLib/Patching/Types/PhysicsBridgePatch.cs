using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MixedRealityExtension.Animation;
using MixedRealityExtension.Core;
using Newtonsoft.Json.Linq;

namespace MixedRealityExtension.Patching.Types
{

	public class TransformPatchInfo
	{
		public TransformPatchInfo() { }

		internal TransformPatchInfo(Guid id, Experimental.RBTransform transform)
		{
			Id = id;

			Transform = new TransformPatch();
			Transform.Position = new Vector3Patch(transform.Position);
			Transform.Rotation = new QuaternionPatch(transform.Rotation);
		}

		/// <summary>
		/// ID of the actor (of the RB)
		/// </summary>
		public Guid Id { get; set; }

		public TransformPatch Transform { get; set; }
	}

	public class PhysicsBridgePatch : IPatchable
	{
		public PhysicsBridgePatch() { }

		internal PhysicsBridgePatch(Guid sourceId, Experimental.Snapshot snapshot)
		{
			Id = sourceId;
			Time = snapshot.Time;
			bridgeTransforms = new List<TransformPatchInfo>(snapshot.RigidBodies.Count);

			foreach (var ti in snapshot.RigidBodies)
			{
				bridgeTransforms.Add(new TransformPatchInfo(ti.ID, ti.Transform));
			}
		}

		internal Experimental.Snapshot ToSnapshot()
		{
			var snapshot = new Experimental.Snapshot();

			snapshot.Time = Time;

			if (bridgeTransforms != null)
			{
				snapshot.RigidBodies = new List<Experimental.RBInfo>(bridgeTransforms.Count);

				foreach (var ti in bridgeTransforms)
				{
					var t = new Experimental.RBInfo();
					t.ID = ti.Id;

					t.Transform = new Experimental.RBTransform();
					t.Transform.Position = new UnityEngine.Vector3(ti.Transform.Position.X.Value, ti.Transform.Position.Y.Value, ti.Transform.Position.Z.Value);
					t.Transform.Rotation = new UnityEngine.Quaternion(ti.Transform.Rotation.X.Value, ti.Transform.Rotation.Y.Value, ti.Transform.Rotation.Z.Value, ti.Transform.Rotation.W.Value);

					snapshot.RigidBodies.Add(t);
				}
			}
			else
			{
				snapshot.RigidBodies = new List<Experimental.RBInfo>();
			}

			return snapshot;
		}

		/// <summary>
		/// source app id (of the sender)
		/// </summary>
		public Guid Id { get; set; }

		public float Time { get; set; }

		public List<TransformPatchInfo> bridgeTransforms { get; set; }

		public void WriteToPath(TargetPath path, JToken value, int depth)
		{

		}

		public bool ReadFromPath(TargetPath path, ref JToken value, int depth)
		{
			return false;
		}

		public void Clear()
		{

		}

		public void Restore(TargetPath path, int depth)
		{

		}

		public void RestoreAll()
		{

		}
	}
}

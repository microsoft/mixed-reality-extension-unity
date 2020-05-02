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

		internal TransformPatchInfo(Guid id, PhysicsBridge.Snapshot.SnapshotTransform transform)
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

		internal PhysicsBridgePatch(PhysicsBridge.Snapshot snapshot)
		{
			Id = snapshot.SourceAppId;
			Time = snapshot.Time;
			bridgeTransforms = new List<TransformPatchInfo>(snapshot.snapshotTransforms.Count);

			foreach (var ti in snapshot.snapshotTransforms)
			{
				bridgeTransforms.Add(new TransformPatchInfo(ti.Id, ti.sTransform));
			}
		}

		internal PhysicsBridge.Snapshot ToSnapshot()
		{
			var snapshot = new PhysicsBridge.Snapshot();

			snapshot.SourceAppId = Id;
			snapshot.Time = Time;

			if (bridgeTransforms != null)
			{
				snapshot.snapshotTransforms = new List<PhysicsBridge.Snapshot.SnapshotTrasformInfo>(bridgeTransforms.Count);

				foreach (var ti in bridgeTransforms)
				{
					var t = new PhysicsBridge.Snapshot.SnapshotTrasformInfo();
					t.Id = ti.Id;

					t.sTransform = new PhysicsBridge.Snapshot.SnapshotTransform();
					t.sTransform.Position = new UnityEngine.Vector3(ti.Transform.Position.X.Value, ti.Transform.Position.Y.Value, ti.Transform.Position.Z.Value);
					t.sTransform.Rotation = new UnityEngine.Quaternion(ti.Transform.Rotation.X.Value, ti.Transform.Rotation.Y.Value, ti.Transform.Rotation.Z.Value, ti.Transform.Rotation.W.Value);

					snapshot.snapshotTransforms.Add(t);
				}
			}
			else
			{
				snapshot.snapshotTransforms = new List<PhysicsBridge.Snapshot.SnapshotTrasformInfo>();
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

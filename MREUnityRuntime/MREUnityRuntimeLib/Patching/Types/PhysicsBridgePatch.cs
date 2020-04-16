using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MixedRealityExtension.Core;

namespace MixedRealityExtension.Patching.Types
{

	public class TransformPatchInfo
	{
		public TransformPatchInfo() { }

		internal TransformPatchInfo(Guid id, PhysicsBridge.Snapshot.Transform transform)
		{
			Id = id;

			Transform = new TransformPatch();
			Transform.Position = new Vector3Patch(transform.Position);
			Transform.Rotation = new QuaternionPatch(transform.Rotation);
		}

		public Guid Id { get; set; }

		public TransformPatch Transform { get; set; }
	}

	public class PhysicsBridgePatch : IPatchable
	{
		public PhysicsBridgePatch() { }

		internal PhysicsBridgePatch(PhysicsBridge.Snapshot snapshot)
		{
			Id = snapshot.SourceId;
			Time = snapshot.Time;
			Transforms = new List<TransformPatchInfo>(snapshot.Transforms.Count);

			foreach (var ti in snapshot.Transforms)
			{
				Transforms.Add(new TransformPatchInfo(ti.Id, ti.Transform));
			}
		}

		internal PhysicsBridge.Snapshot ToSnapshot()
		{
			var snapshot = new PhysicsBridge.Snapshot();

			snapshot.SourceId = Id;
			snapshot.Time = Time;

			if (Transforms != null)
			{
				snapshot.Transforms = new List<PhysicsBridge.Snapshot.TrasformInfo>(Transforms.Count);

				foreach (var ti in Transforms)
				{
					var t = new PhysicsBridge.Snapshot.TrasformInfo();
					t.Id = ti.Id;

					t.Transform = new PhysicsBridge.Snapshot.Transform();
					t.Transform.Position = new UnityEngine.Vector3(ti.Transform.Position.X.Value, ti.Transform.Position.Y.Value, ti.Transform.Position.Z.Value);
					t.Transform.Rotation = new UnityEngine.Quaternion(ti.Transform.Rotation.X.Value, ti.Transform.Rotation.Y.Value, ti.Transform.Rotation.Z.Value, ti.Transform.Rotation.W.Value);

					snapshot.Transforms.Add(t);
				}
			}
			else
			{
				snapshot.Transforms = new List<PhysicsBridge.Snapshot.TrasformInfo>();
			}

			return snapshot;

		}

		public Guid Id { get; set; }

		public float Time { get; set; }

		public List<TransformPatchInfo> Transforms { get; set; }
	}
}

using System;
using System.Collections.Generic;
using MixedRealityExtension.Animation;
using MixedRealityExtension.Core.Physics;
using Newtonsoft.Json.Linq;

namespace MixedRealityExtension.Patching.Types
{
	public class TransformPatchInfo
	{
		public TransformPatchInfo() { }

		internal TransformPatchInfo(Guid id, RigidBodyTransform transform)
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
		public PhysicsBridgePatch()
		{
			bridgeTransforms = new List<TransformPatchInfo>();
		}

		internal PhysicsBridgePatch(Guid sourceId, Snapshot snapshot)
		{
			Id = sourceId;
			Time = snapshot.Time;

			bridgeTransforms = new List<TransformPatchInfo>(snapshot.Transforms.Count);
			foreach (var snapshotTransform in snapshot.Transforms)
			{
				bridgeTransforms.Add(new TransformPatchInfo(snapshotTransform.Id, snapshotTransform.Transform));
			}
		}

		internal Snapshot ToSnapshot()
		{
			List<Snapshot.TransformInfo> transforms = new List<Snapshot.TransformInfo>(bridgeTransforms.Count);

			if (bridgeTransforms != null)
			{
				foreach (var bridgeTransform in bridgeTransforms)
				{
					RigidBodyTransform snapshotTranform;
					{
						snapshotTranform.Position = new UnityEngine.Vector3(
							bridgeTransform.Transform.Position.X.Value,
							bridgeTransform.Transform.Position.Y.Value,
							bridgeTransform.Transform.Position.Z.Value);

						snapshotTranform.Rotation = new UnityEngine.Quaternion(
							bridgeTransform.Transform.Rotation.X.Value,
							bridgeTransform.Transform.Rotation.Y.Value,
							bridgeTransform.Transform.Rotation.Z.Value,
							bridgeTransform.Transform.Rotation.W.Value);
					}

					transforms.Add(new Snapshot.TransformInfo(bridgeTransform.Id, snapshotTranform));
				}
			}

			return new Snapshot(Time, transforms);
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

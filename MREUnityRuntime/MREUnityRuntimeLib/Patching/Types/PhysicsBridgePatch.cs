// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using MixedRealityExtension.Animation;
using MixedRealityExtension.Core.Physics;
using Newtonsoft.Json.Linq;
using Unity.Collections;

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
			TransformCount = 0;
			TransformsBLOB = null;
		}

		internal PhysicsBridgePatch(Guid sourceId, Snapshot snapshot)
		{
			Id = sourceId;
			Time = snapshot.Time;

			TransformCount = snapshot.Transforms.Count;

			if (TransformCount > 0)
			{
				// copy transform to native array to reinterpret it to byte array without 'unsafe' code
				// todo: we should use native array in snapshot anyway.
				Unity.Collections.NativeArray<Snapshot.TransformInfo> transforms =
						new Unity.Collections.NativeArray<Snapshot.TransformInfo>(snapshot.Transforms.ToArray(), Unity.Collections.Allocator.Temp);

				NativeSlice<byte> blob = new NativeSlice<Snapshot.TransformInfo>(transforms).SliceConvert<byte>();
				TransformsBLOB = blob.ToArray();
			}
		}

		internal Snapshot ToSnapshot()
		{
			if (TransformCount > 0)
			{
				Unity.Collections.NativeArray<byte> blob =
				new Unity.Collections.NativeArray<byte>(TransformsBLOB, Unity.Collections.Allocator.Temp);

				// todo: use native array in snapshot
				Unity.Collections.NativeSlice<Snapshot.TransformInfo> transforms = new NativeSlice<byte>(blob).SliceConvert<Snapshot.TransformInfo>();
				return new Snapshot(Time, new List<Snapshot.TransformInfo>(transforms.ToArray()));
			}

			return new Snapshot(Time, new List<Snapshot.TransformInfo>());
		}

		/// <summary>
		/// source app id (of the sender)
		/// </summary>
		public Guid Id { get; set; }

		public float Time { get; set; }

		public int TransformCount { get; set; }

		public byte[] TransformsBLOB { get; set; }

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

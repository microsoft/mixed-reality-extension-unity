// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Animation;
using MixedRealityExtension.Messaging.Payloads;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace MixedRealityExtension.Patching.Types
{
	public class ActorPatch : Patchable<ActorPatch>
	{
		public Guid Id { get; set; }

		[PatchProperty]
		public string Name { get; set; }

		private ActorTransformPatch transform;
		private ActorTransformPatch savedTransform;

		[PatchProperty]
		public ActorTransformPatch Transform
		{
			get => transform;
			set
			{
				if (value == null && transform != null)
				{
					savedTransform = transform;
					savedTransform.Clear();
				}
				transform = value;
			}
		}


		[PatchProperty]
		public Guid? ExclusiveToUser { get; set; }

		[PatchProperty]
		public Guid? Owner { get; set; }

		[PatchProperty]
		public Guid? ParentId { get; set; }

		[PatchProperty]
		public AppearancePatch Appearance { get; set; }

		[PatchProperty]
		public RigidBodyPatch RigidBody { get; set; }

		[PatchProperty]
		public ColliderPatch Collider { get; set; }

		[PatchProperty]
		public LightPatch Light { get; set; }

		[PatchProperty]
		public TextPatch Text { get; set; }

		[PatchProperty]
		public AttachmentPatch Attachment { get; set; }

		[PatchProperty]
		public LookAtPatch LookAt { get; set; }

		[PatchProperty]
		public bool? Grabbable { get; set; }

		[PatchProperty]
		public List<ActorComponentType> Subscriptions { get; set; }

		public ActorPatch()
		{
		}

		internal ActorPatch(Guid id)
		{
			Id = id;
		}

		public override void WriteToPath(TargetPath path, JToken value, int depth)
		{
			if (depth == path.PathParts.Length)
			{
				// actors are not directly patchable, do nothing
			}
			else if (path.PathParts[depth] == "transform")
			{
				if (Transform == null)
				{
					if (savedTransform == null)
					{
						savedTransform = new ActorTransformPatch();
					}
					transform = savedTransform;
				}
				Transform.WriteToPath(path, value, depth + 1);
			}
			// else
				// an unrecognized path, do nothing
		}

		public override bool ReadFromPath(TargetPath path, ref JToken value, int depth)
		{
			if (path.PathParts[depth] == "transform")
			{
				return Transform?.ReadFromPath(path, ref value, depth + 1) ?? false;
			}
			return false;
		}

		public override void Clear()
		{
			Transform = null;
		}

		public bool IsEmpty()
		{
			return Name == null
				&& Transform == null
				&& ParentId == null
				&& Appearance == null
				&& RigidBody == null
				&& Collider == null
				&& Light == null
				&& Text == null
				&& Attachment == null
				&& LookAt == null
				&& Grabbable == null
				&& Subscriptions == null;
		}

		public override void Restore(TargetPath path, int depth)
		{
			if (path.AnimatibleType != "actor" || depth >= path.PathParts.Length) return;

			switch (path.PathParts[depth])
			{
				case "transform":
					Transform = savedTransform ?? new ActorTransformPatch();
					Transform.Restore(path, depth + 1);
					break;
			}
		}

		public override void RestoreAll()
		{
			Transform = savedTransform ?? new ActorTransformPatch();
			Transform.RestoreAll();
		}
	}
}

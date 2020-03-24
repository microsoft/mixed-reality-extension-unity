// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Animation;
using MixedRealityExtension.Messaging.Payloads;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace MixedRealityExtension.Patching.Types
{
	public class ActorTransformPatch : IPatchable
	{
		private TransformPatch app;
		private TransformPatch savedApp;
		[PatchProperty]
		public TransformPatch App {
			get => app;
			set
			{
				if (value == null && app != null)
				{
					savedApp = app;
					savedApp.Clear();
				}
				app = value;
			}
		}

		private ScaledTransformPatch local;
		private ScaledTransformPatch savedLocal;
		[PatchProperty]
		public ScaledTransformPatch Local {
			get => local;
			set
			{
				if (value == null && local != null)
				{
					savedLocal = local;
					savedLocal.Clear();
				}
				local = value;
			}
		}

		public void WriteToPath(TargetPath path, JToken value, int depth)
		{
			if (depth == path.PathParts.Length)
			{
				// actor transforms are not directly patchable, do nothing
			}
			else if (path.PathParts[depth] == "local")
			{
				if (local == null)
				{
					if (savedLocal == null)
					{
						savedLocal = new ScaledTransformPatch();
					}
					local = savedLocal;
				}
				Local.WriteToPath(path, value, depth + 1);
			}
			else if (path.PathParts[depth] == "app")
			{
				if (app == null)
				{
					if (savedApp == null)
					{
						savedApp = new TransformPatch();
					}
					app = savedApp;
				}
				App.WriteToPath(path, value, depth + 1);
			}
			// else
				// an unrecognized path, do nothing
		}

		public void Clear()
		{
			App = null;
			Local = null;
		}

		public void Restore(TargetPath path, int depth)
		{
			if (depth >= path.PathParts.Length) return;

			switch (path.PathParts[depth])
			{
				case "local":
					Local = savedLocal ?? new ScaledTransformPatch();
					Local.Restore(path, depth + 1);
					break;
				case "app":
					App = savedApp ?? new TransformPatch();
					App.Restore(path, depth + 1);
					break;
			}
		}
	}

	public class ActorPatch : IPatchable
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

		public void WriteToPath(TargetPath path, JToken value, int depth)
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

		public void Clear()
		{
			Transform = null;
		}

		public void Restore(TargetPath path, int depth)
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
	}
}

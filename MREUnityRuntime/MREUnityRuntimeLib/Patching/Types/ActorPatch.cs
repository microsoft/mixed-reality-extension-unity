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

		void IPatchable.WriteToPath(TargetPath path, JObject value, int depth = 0)
		{

		}

		public void Clear()
		{
			App = null;
			Local = null;
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

		void IPatchable.WriteToPath(TargetPath path, JObject value, int depth = 0)
		{

		}

		public void Clear()
		{
			Transform = null;
		}
	}
}

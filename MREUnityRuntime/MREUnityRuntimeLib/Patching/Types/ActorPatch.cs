// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Messaging.Payloads;
using System;
using System.Collections.Generic;

namespace MixedRealityExtension.Patching.Types
{
    public class ActorTransformPatch: IPatchable
    {
        [PatchProperty]
        public TransformPatch App { get; set; }

        [PatchProperty]
        public ScaledTransformPatch Local { get; set; }
    }

    public class ActorPatch: IPatchable
    {
        public Guid Id { get; set; }

        [PatchProperty]
        public string Name { get; set; }

        [PatchProperty]
        public ActorTransformPatch Transform { get; set; }

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
    }
}

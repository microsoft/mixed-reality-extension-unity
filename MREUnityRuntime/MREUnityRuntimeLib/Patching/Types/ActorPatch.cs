// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;

namespace MixedRealityExtension.Patching.Types
{
    public class ActorPatch : MixedRealityExtensionObjectPatch
    {
        [PatchProperty]
        public Guid? ParentId { get; set; }

        [PatchProperty]
        public Guid? MaterialId { get; set; }

        [PatchProperty]
        public RigidBodyPatch RigidBody { get; set; }

        [PatchProperty]
        public LightPatch Light { get; set; }

        [PatchProperty]
        public TextPatch Text { get; set; }

        [PatchProperty]
        public AttachmentPatch Attachment { get; set; }

        public ActorPatch()
        {
        }

        internal ActorPatch(Guid id)
            : base(id)
        {
            
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;

namespace MixedRealityExtension.Patching.Types
{
    public class MixedRealityExtensionObjectPatch : IPatchable
    {
        public Guid Id { get; set; }

        [PatchProperty]
        public string Name { get; set; }

        [PatchProperty]
        public TransformPatch Transform { get; set; }

        public MixedRealityExtensionObjectPatch()
        {

        }

        internal MixedRealityExtensionObjectPatch(Guid id)
        {
            this.Id = id;
        }
    }
}

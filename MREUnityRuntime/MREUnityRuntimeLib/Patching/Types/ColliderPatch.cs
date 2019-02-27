// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using MixedRealityExtension.Core;

namespace MixedRealityExtension.Patching.Types
{
    public class ColliderPatch : IPatchable
    {
        [PatchProperty]
        public bool? IsEnabled { get; set; }

        [PatchProperty]
        public bool? IsTrigger { get; set; }

        //[PatchProperty]
        //public CollisionLayer? CollisionLayer { get; set; }

        [PatchProperty]
        public ColliderGeometry ColliderGeometry { get; set; }
    }
}

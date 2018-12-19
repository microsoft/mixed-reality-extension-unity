// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Core.Types;

namespace MixedRealityExtension.Patching.Types
{
    public class TransformPatch : IPatchable
    {
        [PatchProperty]
        public Vector3Patch Position { get; set; }

        [PatchProperty]
        public QuaternionPatch Rotation { get; set; }

        [PatchProperty]
        public Vector3Patch Scale { get; set; }

        public TransformPatch()
        {
            
        }

        internal TransformPatch(MWVector3 position, MWQuaternion rotation, MWVector3 scale)
        {
            this.Position = new Vector3Patch(position);
            this.Rotation = new QuaternionPatch(rotation);
            this.Scale = new Vector3Patch(scale);
        }

        public bool IsPatched()
        {
            return PatchingUtils.IsPatched(this);
        }
    }
}

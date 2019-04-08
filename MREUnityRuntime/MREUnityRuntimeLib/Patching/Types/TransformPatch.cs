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

        public TransformPatch()
        {

        }

        internal TransformPatch(MWVector3 position, MWQuaternion rotation)
        {
            this.Position = new Vector3Patch(position);
            this.Rotation = new QuaternionPatch(rotation);
        }
    }

    public class ScaledTransformPatch: TransformPatch
    {
        [PatchProperty]
        public Vector3Patch Scale { get; set; }

        public ScaledTransformPatch()
            : base()
        {

        }

        internal ScaledTransformPatch(MWVector3 position, MWQuaternion rotation, MWVector3 scale)
            : base(position, rotation)
        {
            this.Scale = new Vector3Patch(scale);
        }
    }
}

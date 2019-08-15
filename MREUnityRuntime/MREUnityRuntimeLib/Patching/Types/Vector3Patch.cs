// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Core.Types;
using System;
using UnityEngine;

namespace MixedRealityExtension.Patching.Types
{
    public class Vector3Patch : Patch, IEquatable<Vector3Patch>
    {
        [PatchProperty]
        public PatchProperty<float> X { get; set; }

        [PatchProperty]
        public PatchProperty<float> Y { get; set; }

        [PatchProperty]
        public PatchProperty<float> Z { get; set; }

        public Vector3Patch()
        {

        }

        public Vector3Patch(MWVector3 vector)
        {
            X.Value = vector.X;
            Y.Value = vector.Y;
            Z.Value = vector.Z;
        }

        public Vector3Patch(Vector3 vector3)
        {
            X.Value = vector3.x;
            Y.Value = vector3.y;
            Z.Value = vector3.z;
        }

        public Vector3Patch(Vector3Patch other)
        {
            if (other != null)
            {
                X.Value = other.X.Value;
                Y.Value = other.Y.Value;
                Z.Value = other.Z.Value;
            }
        }

        public bool Equals(Vector3Patch other)
        {
            if (other == null)
            {
                return false;
            }
            else
            {
                return
                    X.Equals(other.X) &&
                    Y.Equals(other.Y) &&
                    Z.Equals(other.Z);
            }
        }

        public override bool ShouldSerialize()
        {
            return ShouldSerializeX() || ShouldSerializeY() || ShouldSerializeZ();
        }

        public bool ShouldSerializeX()
        {
            return X.Write;
        }

        public bool ShouldSerializeY()
        {
            return Y.Write;
        }

        public bool ShouldSerializeZ()
        {
            return Z.Write;
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Core.Types;
using System;
using UnityEngine;

namespace MixedRealityExtension.Patching.Types
{
    public class QuaternionPatch : Patch, IEquatable<QuaternionPatch>
    {
        [PatchProperty]
        public PatchProperty<float> X { get; set; }

        [PatchProperty]
        public PatchProperty<float> Y { get; set; }

        [PatchProperty]
        public PatchProperty<float> Z { get; set; }

        [PatchProperty]
        public PatchProperty<float> W { get; set; }

        public QuaternionPatch()
        {

        }

        internal QuaternionPatch(MWQuaternion quaternion)
        {
            X.Value = quaternion.X;
            Y.Value = quaternion.Y;
            Z.Value = quaternion.Z;
            W.Value = quaternion.W;
        }

        internal QuaternionPatch(Quaternion quaternion)
        {
            X.Value = quaternion.x;
            Y.Value = quaternion.y;
            Z.Value = quaternion.z;
            W.Value = quaternion.w;
        }

        internal QuaternionPatch(QuaternionPatch other)
        {
            if (other != null)
            {
                X.Value = other.X.Value;
                Y.Value = other.Y.Value;
                Z.Value = other.Z.Value;
                W.Value = other.W.Value;
            }
        }

        //public QuaternionPatch ToQuaternion()
        //{
        //    return new Quaternion()
        //    {
        //        w = (W != null) ? (float)W : 0.0f,
        //        x = (X != null) ? (float)X : 0.0f,
        //        y = (Y != null) ? (float)Y : 0.0f,
        //        z = (Z != null) ? (float)Z : 0.0f,
        //    };
        //}

        public bool Equals(QuaternionPatch other)
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
                    Z.Equals(other.Z) &&
                    W.Equals(other.W);
            }
        }

        public override bool ShouldSerialize()
        {
            return ShouldSerializeW() || ShouldSerializeX() || ShouldSerializeY() || ShouldSerializeZ();
        }

        public bool ShouldSerializeW()
        {
            return W.Write;
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

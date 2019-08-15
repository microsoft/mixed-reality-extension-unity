// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Core.Types;
using System;
using UnityEngine;

namespace MixedRealityExtension.Patching.Types
{
    public class Vector2Patch : Patch, IEquatable<Vector2Patch>
    {
        [PatchProperty]
        public PatchProperty<float> X { get; set; }

        [PatchProperty]
        public PatchProperty<float> Y { get; set; }

        public Vector2Patch()
        {

        }

        public Vector2Patch(MWVector2 vector)
        {
            X.Value = vector.X;
            Y.Value = vector.Y;
        }

        public Vector2Patch(Vector2 vector2)
        {
            X.Value = vector2.x;
            Y.Value = vector2.y;
        }

        public Vector2Patch(Vector2Patch other)
        {
            if (other != null)
            {
                X.Value = other.X.Value;
                Y.Value = other.Y.Value;
            }
        }

        public bool Equals(Vector2Patch other)
        {
            if (other == null)
            {
                return false;
            }
            else
            {
                return
                    X.Equals(other.X) &&
                    Y.Equals(other.Y);
            }
        }

        public override bool ShouldSerialize()
        {
            return ShouldSerializeX() || ShouldSerializeY();
        }

        public bool ShouldSerializeX()
        {
            return X.Write;
        }

        public bool ShouldSerializeY()
        {
            return Y.Write;
        }
    }
}

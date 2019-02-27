// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Core.Types;
using System;
using UnityEngine;

namespace MixedRealityExtension.Patching.Types
{
    public class Vector3Patch : IEquatable<Vector3Patch>, IPatchable
    {
        [PatchProperty]
        public float? X { get; set; }

        [PatchProperty]
        public float? Y { get; set; }

        [PatchProperty]
        public float? Z { get; set; }

        public Vector3Patch()
        {

        }

        public Vector3Patch(MWVector3 vector)
        {
            X = vector.X;
            Y = vector.Y;
            Z = vector.Z;
        }

        public Vector3Patch(Vector3 vector3)
        {
            X = vector3.x;
            Y = vector3.y;
            Z = vector3.z;
        }

        public Vector3Patch(Vector3Patch other)
        {
            if (other != null)
            {
                X = other.X;
                Y = other.Y;
                Z = other.Z;
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
    }
}

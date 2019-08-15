// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Core.Types;
using System;

namespace MixedRealityExtension.Patching.Types
{
    public class ColorPatch : Patch, IEquatable<ColorPatch>
    {
        [PatchProperty]
        public PatchProperty<float> R { get; set; }

        [PatchProperty]
        public PatchProperty<float> G { get; set; }

        [PatchProperty]
        public PatchProperty<float> B { get; set; }

        [PatchProperty]
        public PatchProperty<float> A { get; set; }

        public ColorPatch()
        { }

        internal ColorPatch(MWColor color)
        {
            R.Value = color.R;
            G.Value = color.G;
            B.Value = color.B;
            A.Value = color.A;
        }

        internal ColorPatch(UnityEngine.Color color)
        {
            R.Value = color.r;
            G.Value = color.g;
            B.Value = color.b;
            A.Value = color.a;
        }

        public bool Equals(ColorPatch other)
        {
            if (other == null)
            {
                return false;
            }
            else
            {
                return
                    R.Equals(other.R) &&
                    G.Equals(other.G) &&
                    B.Equals(other.B) &&
                    A.Equals(other.A);
            }
        }

        public override bool ShouldSerialize()
        {
            return ShouldSerializeR() || ShouldSerializeG() || ShouldSerializeB() || ShouldSerializeA();
        }

        public bool ShouldSerializeR()
        {
            return R.Write;
        }

        public bool ShouldSerializeG()
        {
            return G.Write;
        }

        public bool ShouldSerializeB()
        {
            return B.Write;
        }

        public bool ShouldSerializeA()
        {
            return A.Write;
        }
    }
}

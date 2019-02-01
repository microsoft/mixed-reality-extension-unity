// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.API;
using MixedRealityExtension.Core;
using MixedRealityExtension.Core.Types;
using UnityEngine;
using UnityGLTF;

namespace MixedRealityExtension.Util.Unity
{
    internal static class MWUnityTypeExtensions
    {
        public static MWVector3 SetValue(this MWVector3 _this, Vector3 value)
        {
            _this.X = value.x;
            _this.Y = value.y;
            _this.Z = value.z;
            return _this;
        }

        public static MWQuaternion SetValue(this MWQuaternion _this, Quaternion value)
        {
            _this.W = value.w;
            _this.X = value.x;
            _this.Y = value.y;
            _this.Z = value.z;
            return _this;
        }

        public static MWColor SetValue(this MWColor _this, Color value)
        {
            _this.R = value.r;
            _this.G = value.g;
            _this.B = value.b;
            _this.A = value.a;
            return _this;
        }

        public static MWVector2 ToMWVector2(this Vector2 _this)
        {
            return new MWVector2()
            {
                X = _this.x,
                Y = _this.y
            };
        }

        public static MWVector3 ToMWVector3(this Vector3 _this)
        {
            return new MWVector3()
            {
                X = _this.x,
                Y = _this.y,
                Z = _this.z
            };
        }

        public static MWQuaternion ToMWQuaternion(this Quaternion _this)
        {
            return new MWQuaternion()
            {
                W = _this.w,
                X = _this.x,
                Y = _this.y,
                Z = _this.z
            };
        }

        public static MWTransform ToMWTransform(this Transform transform)
        {
            return new MWTransform()
            {
                Position = transform.localPosition.ToMWVector3(),
                Rotation = transform.localRotation.ToMWQuaternion(),
                Scale = transform.localScale.ToMWVector3()
            };
        }

        public static MWColor ToMWColor(this Color color)
        {
            return new MWColor(color.r, color.g, color.b, color.a);
        }

        public static Vector2 ToVector2(this MWVector2 _this)
        {
            return new Vector2(_this.X, _this.Y);
        }

        public static Vector3 ToVector3(this MWVector3 _this)
        {
            return new Vector3()
            {
                x = _this.X,
                y = _this.Y,
                z = _this.Z
            };
        }

        public static Quaternion ToQuaternion(this MWQuaternion _this)
        {
            return new Quaternion()
            {
                w = _this.W,
                x = _this.X,
                y = _this.Y,
                z = _this.Z
            };
        }

        public static Color ToColor(this MWColor _this)
        {
            return new Color(_this.R, _this.G, _this.B, _this.A);
        }

        public static GLTFSceneImporter.ColliderType ToGLTFColliderType(this ColliderType _this)
        {
            switch (_this)
            {
                case ColliderType.Mesh:
                    return GLTFSceneImporter.ColliderType.MeshConvex;
                case ColliderType.Sphere:
                    MREAPI.Logger.LogWarning("Sphere colliders are not supported in UnityGLTF yet.  Downgrading to a box collider.");
                    goto case ColliderType.Box;
                case ColliderType.Box:
                    return GLTFSceneImporter.ColliderType.Box;
                default:
                    return GLTFSceneImporter.ColliderType.None;
            }
        }
    }
}

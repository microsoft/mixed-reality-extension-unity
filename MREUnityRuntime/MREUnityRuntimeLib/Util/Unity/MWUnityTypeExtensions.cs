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

        public static Vector3 SetValue(ref this Vector3 _this, MWVector3 value)
        {
            _this.x = value.X;
            _this.y = value.Y;
            _this.z = value.Z;
            return _this;
        }

        public static Quaternion SetValue(ref this Quaternion _this, MWQuaternion value)
        {
            _this.w = value.W;
            _this.x = value.X;
            _this.y = value.Y;
            _this.z = value.Z;
            return _this;
        }

        public static Color SetValue(ref this Color _this, MWColor value)
        {
            _this.r = value.R;
            _this.g = value.G;
            _this.b = value.B;
            _this.a = value.A;
            return _this;
        }

        public static void FromUnityVector2(this MWVector2 _this, Vector2 other)
        {
            _this.X = other.x;
            _this.Y = other.y;
        }

        public static void FromUnityVector3(this MWVector3 _this, Vector3 other)
        {
            _this.X = other.x;
            _this.Y = other.y;
            _this.Z = other.z;
        }

        public static void FromUnityQuaternion(this MWQuaternion _this, Quaternion other)
        {
            _this.W = other.w;
            _this.X = other.x;
            _this.Y = other.y;
            _this.Z = other.z;
        }

        public static MWScaledTransform ToLocalTransform(this Transform transform)
        {
            return new MWScaledTransform()
            {
                Position = transform.localPosition.ToMWVector3(),
                Rotation = transform.localRotation.ToMWQuaternion(),
                Scale = transform.localScale.ToMWVector3()
            };
        }

        public static MWTransform ToAppTransform(this Transform transform, Transform appRoot)
        {
            return new MWTransform()
            {
                Position = appRoot.InverseTransformPoint(transform.position).ToMWVector3(),
                Rotation = (Quaternion.Inverse(appRoot.rotation) * transform.rotation).ToMWQuaternion()
            };
        }

        public static MWColor ToMWColor(this Color color)
        {
            return new MWColor(color.r, color.g, color.b, color.a);
        }

        public static void FromMWVector2(ref this Vector2 _this, MWVector2 vector2)
        {
            _this.x = vector2.X;
            _this.y = vector2.Y;
        }

        public static void FromMWVector3(ref this Vector3 _this, MWVector3 vector3)
        {
            _this.x = vector3.X;
            _this.y = vector3.Y;
            _this.z = vector3.Z;
        }

        public static void FromMWQuaternion(ref this Quaternion _this, MWQuaternion quaternion)
        {
            _this.w = _this.W;
            _this.x = _this.X;
            _this.y = _this.Y;
            _this.z = _this.Z;
        }

        public static void FromMWColor(ref this Color _this, MWColor color)
        {
            _this.r = color.R;
            _this.g = color.G;
            _this.b = color.B;
            _this.a = color.A;
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

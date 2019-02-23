// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Core;
using MixedRealityExtension.Core.Types;
using MixedRealityExtension.Patching.Types;
using MixedRealityExtension.Util;
using MixedRealityExtension.Util.Unity;
using UnityEngine;

using MRECollisionDetectionMode = MixedRealityExtension.Core.Interfaces.CollisionDetectionMode;
using MRERigidBodyConstraints = MixedRealityExtension.Core.Interfaces.RigidBodyConstraints;
using MRELight = MixedRealityExtension.Core.Light;
using UnityCollisionDetectionMode = UnityEngine.CollisionDetectionMode;
using UnityRigidBodyConstraints = UnityEngine.RigidbodyConstraints;
using UnityLight = UnityEngine.Light;

namespace MixedRealityExtension.Patching
{
    internal static class PatchingUtilMethods
    {
        public static T? GeneratePatch<T>(T _old, T _new) where T : struct
        {
            T? ret = null;
            if (!_old.Equals(_new))
            {
                ret = _new;
            }

            return ret;
        }

        public static T[] GeneratePatch<T>(T[] _old, T[] _new) where T : struct
        {
            if ((_old == null && _new != null) || _new != null)
            {
                return _new;
            }
            else
            {
                return null;
            }
        }

        public static string GeneratePatch(string _old, string _new)
        {
            if (_old == null && _new != null)
            {
                return _new;
            }
            else if (_new == null)
            {
                return null;
            }
            if (_old != _new)
            {
                return _new;
            }
            else
            {
                return null;
            }
        }

        public static Vector3Patch GeneratePatch(MWVector3 _old, Vector3 _new)
        {
            MWVector3 _mwNew = null;
            if (_new != null)
            {
                _mwNew = _new.ToMWVector3();
            }
            return GeneratePatch(_old, _mwNew);
        }

        public static Vector3Patch GeneratePatch(MWVector3 _old, MWVector3 _new)
        {
            if (_old == null && _new != null)
            {
                return new Vector3Patch(_new);
            }
            else if (_new == null)
            {
                return null;
            }

            var patch = new Vector3Patch()
            {
                X = _old.X != _new.X ? (float?)_new.X : null,
                Y = _old.Y != _new.Y ? (float?)_new.Y : null,
                Z = _old.Z != _new.Z ? (float?)_new.Z : null
            };

            if (patch.IsPatched())
            {
                return patch;
            }
            else
            {
                return null;
            }
        }

        public static QuaternionPatch GeneratePatch(MWQuaternion _old, Quaternion _new)
        {
            MWQuaternion _mwNew = null;
            if (_new != null)
            {
                _mwNew = _new.ToMWQuaternion();
            }
            return GeneratePatch(_old, _mwNew);
        }

        public static QuaternionPatch GeneratePatch(MWQuaternion _old, MWQuaternion _new)
        {
            if (_old == null && _new != null)
            {
                return new QuaternionPatch(_new);
            }
            else if (_new == null)
            {
                return null;
            }

            var patch = new QuaternionPatch()
            {
                X = _old.X != _new.X ? (float?)_new.X : null,
                Y = _old.Y != _new.Y ? (float?)_new.Y : null,
                Z = _old.Z != _new.Z ? (float?)_new.Z : null,
                W = _old.W != _new.W ? (float?)_new.W : null
            };

            if (patch.IsPatched())
            {
                return patch;
            }
            else
            {
                return null;
            }
        }

        public static TransformPatch GeneratePatch(MWTransform _old, Transform _new)
        {
            MWTransform _mwNew = null;
            if (_new != null)
            {
                _mwNew = _new.ToMWTransform();
            }
            return GeneratePatch(_old, _mwNew);
        }

        public static TransformPatch GeneratePatch(MWTransform _old, MWTransform _new)
        {
            if (_old == null && _new != null)
            {
                return new TransformPatch()
                {
                    Position = GeneratePatch(null, _new.Position),
                    Rotation = GeneratePatch(null, _new.Rotation)
                };
            }
            else if (_new == null)
            {
                return null;
            }

            TransformPatch transform = new TransformPatch()
            {
                Position = GeneratePatch(_old.Position, _new.Position),
                Rotation = GeneratePatch(_old.Rotation, _new.Rotation),
                Scale = GeneratePatch(_old.Scale, _new.Scale)
            };

            return transform.IsPatched() ? transform : null;
        }

        public static ColorPatch GeneratePatch(MWColor _old, Color _new)
        {
            if (_old == null && _new != null)
            {
                return new ColorPatch(_new);
            }
            else if (_new == null)
            {
                return null;
            }

            var patch = new ColorPatch()
            {
                R = _old.R != _new.r ? (float?)_new.r : null,
                G = _old.G != _new.g ? (float?)_new.g : null,
                B = _old.B != _new.b ? (float?)_new.b : null,
                A = _old.A != _new.a ? (float?)_new.a : null
            };

            if (patch.IsPatched())
            {
                return patch;
            }
            else
            {
                return null;
            }
        }

        public static RigidBodyPatch GeneratePatch(RigidBody _old, Rigidbody _new, Transform sceneRoot)
        {
            if (_old == null && _new != null)
            {
                return new RigidBodyPatch(_new, sceneRoot);
            }
            else if (_new == null)
            {
                return null;
            }

            var patch = new RigidBodyPatch()
            {
                // Do not include Position or Rotation in the patch.
                Velocity = GeneratePatch(_old.Velocity, sceneRoot.InverseTransformDirection(_new.velocity)),
                AngularVelocity = GeneratePatch(_old.AngularVelocity, sceneRoot.InverseTransformDirection(_new.angularVelocity)),
                CollisionDetectionMode = GeneratePatch(
                    _old.CollisionDetectionMode,
                    UtilMethods.ConvertEnum<MRECollisionDetectionMode, UnityCollisionDetectionMode>(_new.collisionDetectionMode)),
                ConstraintFlags = GeneratePatch(
                    _old.ConstraintFlags,
                    UtilMethods.ConvertEnum<MRERigidBodyConstraints, UnityRigidBodyConstraints>(_new.constraints)),
                DetectCollisions = GeneratePatch(_old.DetectCollisions, _new.detectCollisions),
                Mass = GeneratePatch(_old.Mass, _new.mass),
                UseGravity = GeneratePatch(_old.UseGravity, _new.useGravity),
            };

            if (patch.IsPatched())
            {
                return patch;
            }
            else
            {
                return null;
            }
        }

        public static LightPatch GeneratePatch(MRELight _old, UnityLight _new)
        {
            if (_old == null && _new != null)
            {
                return new LightPatch(_new);
            }
            else if (_new == null)
            {
                return null;
            }

            var patch = new LightPatch()
            {
                Enabled = _new.enabled,
                Type = UtilMethods.ConvertEnum<Core.Interfaces.LightType, UnityEngine.LightType>(_new.type),
                Color = new ColorPatch(_new.color),
                Range = _new.range,
                Intensity = _new.intensity,
                SpotAngle = _new.spotAngle
            };

            if (patch.IsPatched())
            {
                return patch;
            }
            else
            {
                return null;
            }
        }
    }

    public static class PatchingUtilsExtensions
    {
        public static T ApplyPatch<T>(this T _this, T? patch) where T : struct
        {
            if (patch.HasValue)
            {
                _this = patch.Value;
            }

            return _this;
        }

        public static string ApplyPatch(this string _this, string patch)
        {
            if (patch != null)
            {
                _this = patch;
            }

            return _this;
        }

        public static MWVector2 ApplyPatch(this MWVector2 _this, Vector2Patch vector)
        {
            if (vector == null)
            {
                return _this;
            }

            if (vector.X != null)
            {
                _this.X = vector.X.Value;
            }

            if (vector.Y != null)
            {
                _this.Y = vector.Y.Value;
            }

            return _this;
        }

        public static MWVector3 ApplyPatch(this MWVector3 _this, Vector3Patch vector)
        {
            if (vector == null)
            {
                return _this;
            }

            if (vector.X != null)
            {
                _this.X = vector.X.Value;
            }

            if (vector.Y != null)
            {
                _this.Y = vector.Y.Value;
            }

            if (vector.Z != null)
            {
                _this.Z = vector.Z.Value;
            }

            return _this;
        }

        public static MWQuaternion ApplyPatch(this MWQuaternion _this, QuaternionPatch quaternion)
        {
            if (quaternion == null)
            {
                return _this;
            }

            if (quaternion.W != null)
            {
                _this.W = quaternion.W.Value;
            }

            if (quaternion.X != null)
            {
                _this.X = quaternion.X.Value;
            }

            if (quaternion.Y != null)
            {
                _this.Y = quaternion.Y.Value;
            }

            if (quaternion.Z != null)
            {
                _this.Z = quaternion.Z.Value;
            }

            return _this;
        }

        public static MWColor ApplyPatch(this MWColor _this, ColorPatch color)
        {
            if (color == null)
            {
                return _this;
            }

            if (color.A != null)
            {
                _this.A = color.A.Value;
            }

            if (color.R != null)
            {
                _this.R = color.R.Value;
            }

            if (color.G != null)
            {
                _this.G = color.G.Value;
            }

            if (color.B != null)
            {
                _this.B = color.B.Value;
            }

            return _this;
        }

        public static MWTransform ApplyPatch(this MWTransform _this, TransformPatch transform)
        {
            if (transform == null || !transform.IsPatched())
            {
                return _this;
            }

            if (transform.Position != null && transform.Position.IsPatched())
            {
                _this.Position.ApplyPatch(transform.Position);
            }

            if (transform.Rotation != null && transform.Rotation.IsPatched())
            {
                _this.Rotation.ApplyPatch(transform.Rotation);
            }

            if (transform.Scale != null && transform.Scale.IsPatched())
            {
                _this?.Scale.ApplyPatch(transform.Scale);
            }

            return _this;
        }
    }
}

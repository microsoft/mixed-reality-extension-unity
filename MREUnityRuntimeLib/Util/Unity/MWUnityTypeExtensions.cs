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
		public static MWVector2 FromUnityVector2(this MWVector2 _this, Vector2 other)
		{
			_this.X = other.x;
			_this.Y = other.y;
			return _this;
		}

		public static MWVector3 FromUnityVector3(this MWVector3 _this, Vector3 other)
		{
			_this.X = other.x;
			_this.Y = other.y;
			_this.Z = other.z;
			return _this;
		}

		public static MWQuaternion FromUnityQuaternion(this MWQuaternion _this, Quaternion other)
		{
			_this.W = other.w;
			_this.X = other.x;
			_this.Y = other.y;
			_this.Z = other.z;
			return _this;
		}

		public static MWColor FromUnityColor(this MWColor _this, Color other)
		{
			_this.R = other.r;
			_this.G = other.g;
			_this.B = other.b;
			_this.A = other.a;
			return _this;
		}

		public static MWVector2 CreateMWVector2(this Vector2 _this)
		{
			return new MWVector2()
			{
				X = _this.x,
				Y = _this.y
			};
		}

		public static MWVector3 CreateMWVector3(this Vector3 _this)
		{
			return new MWVector3()
			{
				X = _this.x,
				Y = _this.y,
				Z = _this.z
			};
		}

		public static MWQuaternion CreateMWQuaternion(this Quaternion _this)
		{
			return new MWQuaternion()
			{
				W = _this.w,
				X = _this.x,
				Y = _this.y,
				Z = _this.z
			};
		}

		public static void ToLocalTransform(this MWScaledTransform _this, Transform transform)
		{
			if (_this.Position == null)
			{
				_this.Position = new MWVector3();
			}

			if (_this.Rotation == null)
			{
				_this.Rotation = new MWQuaternion();
			}

			if (_this.Scale == null)
			{
				_this.Scale = new MWVector3();
			}

			_this.Position.FromUnityVector3(transform.localPosition);
			_this.Rotation.FromUnityQuaternion(transform.localRotation);
			_this.Scale.FromUnityVector3(transform.localScale);
		}

		public static void ToAppTransform(this MWTransform _this, Transform transform, Transform appRoot)
		{
			if (_this.Position == null)
			{
				_this.Position = new MWVector3();
			}

			if (_this.Rotation == null)
			{
				_this.Rotation = new MWQuaternion();
			}

			_this.Position.FromUnityVector3(appRoot.InverseTransformPoint(transform.position));
			_this.Rotation.FromUnityQuaternion(Quaternion.Inverse(appRoot.rotation) * transform.rotation);
		}

		public static MWVector3 ToLocalMWVector3(this MWVector3 _this, Vector3 point, Transform objectRoot)
		{
			_this.FromUnityVector3(objectRoot.InverseTransformPoint(point));
			return _this;
		}

		public static Vector2 ToVector2(this MWVector2 _this)
		{
			return new Vector2()
			{
				x = _this.X,
				y = _this.Y
			};
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
			return new Color()
			{
				r = _this.R,
				g = _this.G,
				b = _this.B,
				a = _this.A
			};
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

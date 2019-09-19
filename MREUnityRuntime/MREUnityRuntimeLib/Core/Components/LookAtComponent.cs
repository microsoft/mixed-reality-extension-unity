// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using MixedRealityExtension.Core.Interfaces;
using MixedRealityExtension.Patching.Types;
using MixedRealityExtension.Util.Unity;
using System;
using UnityEngine;

namespace MixedRealityExtension.Core.Components
{
	/// <summary>
	/// Unity Behaviour to face toward a given target object
	/// </summary>
	[DisallowMultipleComponent]
	internal class LookAtComponent : ActorComponentBase
	{
		private GameObject _targetObject;
		private LookAtMode _lookAtMode;
		private bool _backward;

		internal void ApplyPatch(LookAtPatch patch)
		{
			if (patch.ActorId.HasValue)
			{
				IActor targetActor = AttachedActor.App.FindActor(patch.ActorId.Value);
				if (targetActor != null)
				{
					_targetObject = targetActor.GameObject;
				}
				else
				{
					_targetObject = null;
				}
			}
			if (patch.Mode.HasValue)
			{
				_lookAtMode = patch.Mode.Value;
			}
			if (patch.Backward.HasValue)
			{
				_backward = patch.Backward.Value;
			}
		}

		void Update()
		{
			if (_lookAtMode != LookAtMode.None && _targetObject != null)
			{
				var rotation = CalcRotation();
				if (rotation.HasValue)
				{
					transform.rotation = rotation.Value;
				}
			}
		}

		private Quaternion? CalcRotation()
		{
			Vector3 pos = _targetObject.transform.position;
			Vector3 delta = pos - transform.position;

			if (delta == Vector3.zero)
			{
				// In case of zero-length, don't change our rotation.
				return null;
			}

			if (_backward)
			{
				delta *= -1;
			}

			Quaternion look = Quaternion.LookRotation(delta, Vector3.up);

			if (_lookAtMode == LookAtMode.TargetY)
			{
				look = Quaternion.Euler(0, look.eulerAngles.y, look.eulerAngles.z);
			}

			return look;
		}
	}
}

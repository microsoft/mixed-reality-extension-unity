// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using MixedRealityExtension.Core.Interfaces;
using MixedRealityExtension.Util.Unity;
using System;
using UnityEngine;

namespace MixedRealityExtension.Controllers
{
    /// <summary>
    /// Unity Behaviour to face toward a given target object
    /// </summary>
    [DisallowMultipleComponent]
    internal class LookAtController : MonoBehaviour
    {
        private IMixedRealityExtensionObject _trackedObject;
        private LookAtMode _lookAtMode;

        internal void Configure(IMixedRealityExtensionObject trackedObject, LookAtMode lookAtMode)
        {
            _trackedObject = trackedObject;
            _lookAtMode = lookAtMode;
            enabled = lookAtMode != LookAtMode.None;
        }

        void Update()
        {
            if (_trackedObject != null)
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
            if (_trackedObject.LookAtPosition.HasValue)
            {
                Vector3 pos = _trackedObject.LookAtPosition.Value;
                Vector3 delta = pos - transform.position;

                if (delta == Vector3.zero)
                {
                    return Quaternion.identity;
                }

                Quaternion look = Quaternion.LookRotation(delta, Vector3.up);

                switch (_lookAtMode)
                {
                    case LookAtMode.TargetXY:
                        return look;

                    case LookAtMode.TargetY:
                        return Quaternion.Euler(0, look.eulerAngles.y, look.eulerAngles.z);

                    default:
                        throw new ArgumentException(nameof(LookAtMode));
                }
            }
            return null;
        }
    }
}

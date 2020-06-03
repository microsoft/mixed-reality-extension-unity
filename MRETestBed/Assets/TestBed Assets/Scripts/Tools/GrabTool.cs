// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using Assets.Scripts.Behaviors;
using Assets.Scripts.User;
using System;
using UnityEngine;

namespace Assets.Scripts.Tools
{
	public enum GrabState
	{
		Grabbed,
		Released
	}

	public class GrabStateChangedArgs
	{
		public GrabState OldGrabState { get; }
		public GrabState NewGrabState { get; }
		public InputSource InputSource { get; }

		public GrabStateChangedArgs(GrabState oldGrabState, GrabState newGrabState, InputSource inputSource)
		{
			OldGrabState = oldGrabState;
			NewGrabState = newGrabState;
			InputSource = inputSource;
		}
	}

	public class GrabTool: IDisposable
	{
		private GameObject _manipulator;
		private Transform _previousParent;
		private Vector3 _manipulatorPosInToolSpace;
		private Vector3 _manipulatorupInToolSpace;
		private Vector3 _manipulatorLookAtPosInToolSpace;
		private InputSource _currentInputSource;

		public bool GrabActive => CurrentGrabbedTarget != null;

		public GameObject CurrentGrabbedTarget { get; private set; }

		public EventHandler<GrabStateChangedArgs> GrabStateChanged { get; set; }

		public void Update(InputSource inputSource, GameObject target)
		{
			if (target == null)
			{
				return;
			}

			if (Input.GetButtonDown("Fire2"))
			{
				var grabBehavior = target.GetBehavior<TargetBehavior>();
				if (grabBehavior != null)
				{
					var mwUser = grabBehavior.GetMWUnityUser(inputSource.UserGameObject);
					if (mwUser != null)
					{
						grabBehavior.Context.StartGrab(mwUser);
						grabBehavior.IsGrabbed = true;
					}
				}

				StartGrab(inputSource, target);
				GrabStateChanged?.Invoke(this, new GrabStateChangedArgs(GrabState.Released, GrabState.Grabbed, inputSource));
			}
			else if (Input.GetButtonUp("Fire2"))
			{
				var grabBehavior = target.GetBehavior<TargetBehavior>();
				if (grabBehavior != null)
				{
					var mwUser = grabBehavior.GetMWUnityUser(inputSource.UserGameObject);
					if (mwUser != null)
					{
						grabBehavior.Context.EndGrab(mwUser);
						grabBehavior.IsGrabbed = false;
					}
				}

				EndGrab();
				GrabStateChanged?.Invoke(this, new GrabStateChangedArgs(GrabState.Grabbed, GrabState.Released, inputSource));
			}

			if (GrabActive)
			{
				UpdatePosition();
				UpdateRotation();
			}
		}

		private void StartGrab(InputSource inputSource, GameObject target)
		{
			if (GrabActive ||target == null)
			{
				return;
			}

			CurrentGrabbedTarget = target;
			_currentInputSource = inputSource;

			var targetTransform = CurrentGrabbedTarget.transform;
			var inputTransform = _currentInputSource.transform;

			_manipulator = new GameObject("manipulator");
			_manipulator.transform.parent = null;
			_manipulator.transform.position = targetTransform.position;
			_manipulator.transform.rotation = targetTransform.rotation;

			_previousParent = targetTransform.parent;
			targetTransform.SetParent(_manipulator.transform, worldPositionStays: true);

			_manipulatorPosInToolSpace = inputTransform.InverseTransformPoint(_manipulator.transform.position);
			_manipulatorupInToolSpace = inputTransform.InverseTransformDirection(_manipulator.transform.up);
			_manipulatorLookAtPosInToolSpace = inputTransform.InverseTransformPoint(_manipulator.transform.position + _manipulator.transform.forward);
		}

		private void EndGrab()
		{
			if (!GrabActive)
			{
				return;
			}

			CurrentGrabbedTarget.transform.SetParent(_previousParent, worldPositionStays: true);
			CurrentGrabbedTarget = null;

			UnityEngine.Object.Destroy(_manipulator);
			_manipulator = null;
		}

		private void UpdatePosition()
		{
			Vector3 targetPosition = _currentInputSource.transform.TransformPoint(_manipulatorPosInToolSpace);
			_manipulator.transform.position = targetPosition;
		}

		private void UpdateRotation()
		{
			Vector3 targetLookAtPos = _currentInputSource.transform.TransformPoint(_manipulatorLookAtPosInToolSpace);
			Vector3 targetUp = _currentInputSource.transform.TransformDirection(_manipulatorupInToolSpace);
			_manipulator.transform.rotation = Quaternion.LookRotation(targetLookAtPos - _manipulator.transform.position, targetUp);
		}

		public void Dispose()
		{
			if (_manipulator != null)
			{
				UnityEngine.Object.Destroy(_manipulator.gameObject);
			}
		}
	}
}

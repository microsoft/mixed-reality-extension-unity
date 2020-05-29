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
		private Transform _manipulator;
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

			_manipulator = _manipulator ?? new GameObject("manipulator").transform;
			_manipulator.parent = null;
			_manipulator.position = targetTransform.position;
			_manipulator.rotation = targetTransform.rotation;

			_previousParent = targetTransform.parent;
			targetTransform.SetParent(_manipulator, worldPositionStays: true);

			_manipulatorPosInToolSpace = inputTransform.InverseTransformPoint(_manipulator.position);
			_manipulatorupInToolSpace = inputTransform.InverseTransformDirection(_manipulator.up);
			_manipulatorLookAtPosInToolSpace = inputTransform.InverseTransformPoint(_manipulator.position + _manipulator.forward);
		}

		private void EndGrab()
		{
			if (!GrabActive)
			{
				return;
			}

			CurrentGrabbedTarget.transform.SetParent(_previousParent, worldPositionStays: true);
			CurrentGrabbedTarget = null;

			_manipulator.localPosition = Vector3.zero;
			_manipulator.localRotation = Quaternion.identity;
			_manipulator.localScale = Vector3.one;
		}

		private void UpdatePosition()
		{
			Vector3 targetPosition = _currentInputSource.transform.TransformPoint(_manipulatorPosInToolSpace);
			_manipulator.position = targetPosition;
		}

		private void UpdateRotation()
		{
			Vector3 targetLookAtPos = _currentInputSource.transform.TransformPoint(_manipulatorLookAtPosInToolSpace);
			Vector3 targetUp = _currentInputSource.transform.TransformDirection(_manipulatorupInToolSpace);
			_manipulator.rotation = Quaternion.LookRotation(targetLookAtPos - _manipulator.position, targetUp);
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

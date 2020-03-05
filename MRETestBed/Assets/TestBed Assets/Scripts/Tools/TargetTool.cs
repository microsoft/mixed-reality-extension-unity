// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using Assets.Scripts.Behaviors;
using Assets.Scripts.User;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.Tools
{
	public class TargetTool : Tool
	{
		private GrabTool _grabTool = new GrabTool();
		private TargetBehavior _currentTargetBehavior;

		public GameObject Target { get; private set; }

		protected override void UpdateTool(InputSource inputSource)
		{
			if (_currentTargetBehavior?.Grabbable ?? false)
			{
				_grabTool.Update(inputSource, Target);
				if (_grabTool.GrabActive)
				{
					// If a grab is active, nothing should change about the current target.
					return;
				}
			}

			var newTarget = FindTarget(inputSource);
			if (Target == newTarget)
			{
				return;
			}

			if (Target != null && _currentTargetBehavior != null)
			{
				var mwUser = _currentTargetBehavior.GetMWUnityUser(inputSource.UserGameObject);
				if (mwUser != null)
				{
					_currentTargetBehavior.Target.StopAction(mwUser);
				}
			}

			TargetBehavior newBehavior = null;
			if (newTarget != null)
			{
				newBehavior = newTarget.GetBehavior<TargetBehavior>();
				var mwUser = newBehavior.GetMWUnityUser(inputSource.UserGameObject);
				if (mwUser != null)
				{
					newBehavior.Target.StartAction(mwUser);
				}
			}

			OnTargetChanged(Target, newTarget, inputSource);
			Target = newTarget;

			if (newBehavior != null)
			{
				if (newBehavior.GetDesiredToolType() != inputSource.CurrentTool.GetType())
				{
					inputSource.HoldTool(newBehavior.GetDesiredToolType());
				}

				_currentTargetBehavior = newBehavior;
			}
		}

		protected virtual void OnTargetChanged(GameObject oldTarget, GameObject newTarget, InputSource inputSource)
		{

		}

		private GameObject FindTarget(InputSource inputSource)
		{
			RaycastHit hitInfo;
			var gameObject = inputSource.gameObject;

			// Only target layers 0 (Default), 5 (UI), and 10 (Hologram).
			// You still want to hit all layers, but only interact with these.
			int layerMask = (1 << 0) | (1 << 5) | (1 << 10);

			if (Physics.Raycast(gameObject.transform.position, gameObject.transform.forward, out hitInfo, Mathf.Infinity))
			{
				for (var transform = hitInfo.transform; transform; transform = transform.parent)
				{
					if (transform.GetComponents<TargetBehavior>().FirstOrDefault() != null
						&& ((1 << transform.gameObject.layer) | layerMask) != 0)
					{
						return transform.gameObject;
					}
				}
			}

			return null;
		}

		void OnDestroy()
		{
			_grabTool.Dispose();
		}
	}
}

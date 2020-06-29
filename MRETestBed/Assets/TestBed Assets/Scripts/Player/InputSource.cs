// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using Assets.Scripts.Tools;
using System;
using UnityEngine;

namespace Assets.Scripts.User
{
	public class InputSource : MonoBehaviour
	{
		private Tool _currentTool;

		public GameObject UserGameObject;

		public Tool CurrentTool => _currentTool;

		public static readonly Guid UserId = new Guid();

		private void Start()
		{
			_currentTool = ToolCache.GetOrCreateTool<TargetTool>();
			_currentTool.OnToolHeld(this);
		}

		public void HoldTool(Type toolType)
		{
			if (UserGameObject != null)
			{
				_currentTool.OnToolDropped(this);
				ToolCache.StowTool(_currentTool);
				_currentTool = ToolCache.GetOrCreateTool(toolType);
				_currentTool.OnToolHeld(this);
			}
		}

		public void DropTool()
		{
			// We only drop a tool is it isn't the default target tool.
			if (UserGameObject != null && _currentTool.GetType() != typeof(TargetTool))
			{
				_currentTool.OnToolDropped(this);
				ToolCache.StowTool(_currentTool);
				_currentTool = ToolCache.GetOrCreateTool<TargetTool>();
				_currentTool.OnToolHeld(this);
			}
		}

		private void Awake()
		{
			if (UserGameObject == null)
			{
				throw new Exception("Input source must have a MWUnityUser assigned to it.");
			}
		}

		private void Update()
		{
			_currentTool.Update(this);
		}
	}
}

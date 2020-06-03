// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Assets.TestBed_Assets.Scripts.UserInput;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Physics;
using MixedRealityExtension.Behaviors.Contexts;
using MixedRealityExtension.PluginInterfaces.Behaviors;
using System;
using UnityEngine;

namespace Assets.Scripts.Behaviors
{
	public class TargetChangedEventArgs
	{
		public Vector3 Point { get; }

		public TargetChangedEventArgs(Vector3 point)
		{
			Point = point;
		}
	}

	public class MRTKTargetBehavior : MRTKBehaviorBase, ITargetBehavior
	{
		private MREFocusHandler _focusHandler;

		protected IMixedRealityPointer _pointer;
		protected TargetBehaviorContext _context;

		public bool Grabbable { get; set; }

		public bool IsGrabbed { get; set; }

		public TargetBehaviorContext Context => _context; 

		public void SetContext(TargetBehaviorContext context)
		{
			_context = context;
		}

		protected EventHandler<TargetChangedEventArgs> TargetEntered;
		protected EventHandler<TargetChangedEventArgs> TargetExited;

		protected Vector3 CurrentFocusedPoint { get; private set; }

		protected override void InitializeActions()
		{
			_focusHandler = gameObject.GetComponent<MREFocusHandler>() ?? gameObject.AddComponent<MREFocusHandler>();
			_focusHandler.OnFocusEntered += OnFocusEnter;
			_focusHandler.OnFocusExited += OnFocusExit;
		}

		protected override void DisposeActions()
		{
			_focusHandler.OnFocusEntered -= OnFocusEnter;
			_focusHandler.OnFocusExited -= OnFocusExit;
		}

		protected FocusDetails? GetFocusDetails(IMixedRealityPointer pointer)
		{
			if (pointer == null)
			{
				return null;
			}

			FocusDetails focusDetails;
			if (!CoreServices.InputSystem.FocusProvider.TryGetFocusDetails(pointer, out focusDetails))
			{
				if (CoreServices.InputSystem.FocusProvider.IsPointerRegistered(pointer))
				{
					Debug.LogError($"{name}: Unable to get focus details for {pointer.GetType().Name}!");
				}
			}

			return focusDetails;
		}

		private void OnFocusEnter(object sender, FocusChangedArgs args)
		{
			_pointer = args.Pointer;

			var focusDetails = GetFocusDetails(_pointer);
			if (focusDetails != null)
			{
				var focusPoint = focusDetails.Value.Point;
				Context.StartTargeting(GetMWUnityUser(), focusPoint);
				TargetEntered?.Invoke(this, new TargetChangedEventArgs(focusPoint));

				CurrentFocusedPoint = focusPoint;
			}
		}

		private void OnFocusExit(object sender, FocusChangedArgs args)
		{
			var focusDetails = GetFocusDetails(_pointer);
			if (focusDetails != null)
			{
				var focusPoint = focusDetails.Value.Point;
				Context.EndTargeting(GetMWUnityUser(), focusPoint);
				TargetExited?.Invoke(this, new TargetChangedEventArgs(focusPoint));
			}

			_pointer = null;
			CurrentFocusedPoint = Vector3.zero;
		}

		private void Update()
		{
			if (_pointer != null)
			{
				var focusDetails = GetFocusDetails(_pointer);
				if (focusDetails != null)
				{
					Context.UpdateTargetPoint(GetMWUnityUser(), focusDetails.Value.Point);
					CurrentFocusedPoint = focusDetails.Value.Point;
				}
			}
		}
	}
}

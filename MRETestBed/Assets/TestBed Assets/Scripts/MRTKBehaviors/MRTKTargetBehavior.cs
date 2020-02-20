// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using Microsoft.MixedReality.Toolkit.Input;
using MixedRealityExtension.Behaviors.Actions;
using MixedRealityExtension.PluginInterfaces.Behaviors;

namespace Assets.Scripts.Behaviors
{
	public delegate void TargetEvenHandler();

	public class MRTKTargetBehavior : MRTKBehaviorBase, ITargetBehavior
	{
		private FocusHandler _focusHandler;

		public bool Grabbable { get; set; }

		public bool IsGrabbed { get; set; }

		public MWAction Target { get; } = new MWAction();

		public MWAction Grab { get; } = new MWAction();

		protected event TargetEvenHandler TargetEntered;
		protected event TargetEvenHandler TargetExited;

		protected override void InitializeActions()
		{
			_focusHandler = gameObject.GetComponent<FocusHandler>() ?? gameObject.AddComponent<FocusHandler>();
			_focusHandler.OnFocusEnterEvent.AddListener(OnFocusEnter);
			_focusHandler.OnFocusExitEvent.AddListener(OnFocusExit);
		}

		protected override void DisposeActions()
		{
			_focusHandler.OnFocusEnterEvent.RemoveListener(OnFocusEnter);
			_focusHandler.OnFocusEnterEvent.RemoveListener(OnFocusExit);
		}

		private void OnFocusEnter()
		{
			Target.StartAction(GetMWUnityUser());
			TargetEntered?.Invoke();
		}

		private void OnFocusExit()
		{
			Target.StopAction(GetMWUnityUser());
			TargetExited?.Invoke();
		}
	}
}

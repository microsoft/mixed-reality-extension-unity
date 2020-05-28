// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using Assets.TestBed_Assets.Scripts.UserInput;
using Microsoft.MixedReality.Toolkit.Input;
using MixedRealityExtension.Behaviors.Actions;
using MixedRealityExtension.PluginInterfaces.Behaviors;

namespace Assets.Scripts.Behaviors
{
	public class MRTKButtonBehavior : MRTKTargetBehavior, IButtonBehavior
	{
		private MREInputActionHandler _primaryActionHandler;

		public MWAction Hover { get; } = new MWAction();

		public MWAction Click { get; } = new MWAction();

		public MWAction Button { get; } = new MWAction();

		protected override void InitializeActions()
		{
			base.InitializeActions();

			TargetEntered += OnHoverStarted;
			TargetExited += OnHoverEnded;

			_primaryActionHandler = gameObject.AddComponent<MREInputActionHandler>();
			_primaryActionHandler.InputAction = MREInputManager.Instance.GetMRTKInputAction(MREInputAction.PrimaryAction);
			_primaryActionHandler.OnInputActionStarted.AddListener(OnPrimaryActionStarted);
			_primaryActionHandler.OnInputActionEnded.AddListener(OnPrimaryActionEnded);
		}

		protected override void DisposeActions()
		{
			base.DisposeActions();

			TargetEntered -= OnHoverStarted;
			TargetExited -= OnHoverEnded;

			_primaryActionHandler.OnInputActionStarted.RemoveListener(OnPrimaryActionStarted);
			_primaryActionHandler.OnInputActionEnded.RemoveListener(OnPrimaryActionEnded);
			Destroy(_primaryActionHandler);
		}

		private void OnPrimaryActionStarted(BaseInputEventData eventData)
		{
			var user = GetMWUnityUser();
			Button.StartAction(user);
		}

		private void OnPrimaryActionEnded(BaseInputEventData eventData)
		{
			var user = GetMWUnityUser();
			Button.StopAction(user);
			Click.StartAction(user);
		}

		private void OnHoverStarted()
		{
			Hover.StartAction(GetMWUnityUser());
		}

		private void OnHoverEnded()
		{
			Hover.StopAction(GetMWUnityUser());
		}
	}
}

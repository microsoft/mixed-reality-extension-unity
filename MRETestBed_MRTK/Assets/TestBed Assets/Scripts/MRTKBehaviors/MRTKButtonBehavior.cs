// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Assets.TestBed_Assets.Scripts.UserInput;
using Microsoft.MixedReality.Toolkit.Input;
using MixedRealityExtension.Behaviors.Contexts;
using MixedRealityExtension.PluginInterfaces.Behaviors;

namespace Assets.Scripts.Behaviors
{
	public class MRTKButtonBehavior : MRTKTargetBehavior, IButtonBehavior
	{
		private MREInputActionHandler _primaryActionHandler;

		public new ButtonBehaviorContext Context => _context as ButtonBehaviorContext;

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
			Context.StartButton(GetMWUnityUser(), CurrentFocusedPoint);
		}

		private void OnPrimaryActionEnded(BaseInputEventData eventData)
		{
			var user = GetMWUnityUser();
			Context.EndButton(user, CurrentFocusedPoint);
			Context.Click(user, CurrentFocusedPoint);
		}

		private void OnHoverStarted(object sender, TargetChangedEventArgs args)
		{
			Context.StartHover(GetMWUnityUser(), args.Point);
		}

		private void OnHoverEnded(object sender, TargetChangedEventArgs args)
		{
			Context.EndHover(GetMWUnityUser(), args.Point);
		}
	}
}

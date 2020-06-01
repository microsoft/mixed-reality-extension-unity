// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;

namespace Assets.TestBed_Assets.Scripts.UserInput
{
	class MREInputActionHandler : MREInputHandler, IMixedRealityInputActionHandler
	{
		public MixedRealityInputAction InputAction { get; set; } = MixedRealityInputAction.None;

		public bool MarkEventsAsUsed { get; set; } = false;

		/// <summary>
		/// Unity event raised on action start, e.g. button pressed or gesture started. 
		/// Includes the input event that triggered the action.
		/// </summary>
		public InputActionUnityEvent OnInputActionStarted = new InputActionUnityEvent();

		/// <summary>
		/// Unity event raised on action end, e.g. button released or gesture completed.
		/// Includes the input event that triggered the action.
		/// </summary>
		public InputActionUnityEvent OnInputActionEnded = new InputActionUnityEvent();

		protected override void RegisterHandlers()
		{
			CoreServices.InputSystem?.RegisterHandler<IMixedRealityInputActionHandler>(this);
		}

		protected override void UnregisterHandlers()
		{
			CoreServices.InputSystem?.UnregisterHandler<IMixedRealityInputActionHandler>(this);
		}

		void IMixedRealityInputActionHandler.OnActionStarted(BaseInputEventData eventData)
		{
			if (eventData.MixedRealityInputAction == InputAction && !eventData.used)
			{
				OnInputActionStarted.Invoke(eventData);
				if (MarkEventsAsUsed)
				{
					eventData.Use();
				}
			}
		}
		void IMixedRealityInputActionHandler.OnActionEnded(BaseInputEventData eventData)
		{
			if (eventData.MixedRealityInputAction == InputAction && !eventData.used)
			{
				OnInputActionEnded.Invoke(eventData);
				if (MarkEventsAsUsed)
				{
					eventData.Use();
				}
			}
		}
	}
}

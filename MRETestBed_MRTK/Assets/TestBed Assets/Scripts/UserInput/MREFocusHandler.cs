// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.MixedReality.Toolkit.Input;
using System;
using UnityEngine;

namespace Assets.TestBed_Assets.Scripts.UserInput
{
	public class FocusChangedArgs
	{
		public IMixedRealityPointer Pointer { get; }

		public Vector3 FocusPoint { get; }

		public FocusChangedArgs(IMixedRealityPointer pointer)
		{
			Pointer = pointer;
		}
	}

	public class MREFocusHandler : MonoBehaviour, IMixedRealityFocusHandler
	{
		public EventHandler<FocusChangedArgs> OnFocusEntered { get; set; }

		public EventHandler<FocusChangedArgs> OnFocusExited { get; set; }

		public bool MarkEventAsUsed { get; set; } = false;

		public void OnFocusEnter(FocusEventData eventData)
		{
			if (!eventData.used)
			{

				OnFocusEntered?.Invoke(this, new FocusChangedArgs(eventData.Pointer));

				if (MarkEventAsUsed)
				{
					eventData.Use();
				}
			}
		}

		public void OnFocusExit(FocusEventData eventData)
		{
			if (!eventData.used)
			{
				OnFocusExited?.Invoke(this, new FocusChangedArgs(eventData.Pointer));

				if (MarkEventAsUsed)
				{
					eventData.Use();
				}
			}
		}
	}
}

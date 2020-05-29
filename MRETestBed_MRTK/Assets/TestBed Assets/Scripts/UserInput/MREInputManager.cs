// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.TestBed_Assets.Scripts.UserInput
{
	public enum MREInputAction
	{
		PrimaryAction,
		Grip,
		Teleport,
		Walk,
		Rotate
	}

	public class MREInputManager
	{
		private Dictionary<MREInputAction, MixedRealityInputAction> _inputActions =
			new Dictionary<MREInputAction, MixedRealityInputAction>();

		public GameObject UserGameObject { get; set; }

		public bool IsInitialized { get; private set; } = false;

		public MixedRealityInputAction GetMRTKInputAction(MREInputAction mreInputAction)
		{
			if (_inputActions.TryGetValue(mreInputAction, out MixedRealityInputAction mrtkInputAction))
			{
				return mrtkInputAction;
			}

			return MixedRealityInputAction.None;
		}

		public static async Task CreateManager()
		{
			await new WaitUntil(() => CoreServices.InputSystem != null);

			var manager = new MREInputManager();

			// Build mapping for MRTK MixedRealityInputAction and MRE Input Actions we support.
			var mrtkInputActions = CoreServices.InputSystem.InputSystemProfile.InputActionsProfile.InputActions;
			foreach (var mrtkInputAction in mrtkInputActions)
			{
				if (Enum.TryParse(mrtkInputAction.Description, out MREInputAction mreInputAction))
				{
					manager._inputActions.Add(mreInputAction, mrtkInputAction);
				}
			}

			manager.IsInitialized = true;
			_instance = manager;
		}

		private static MREInputManager _instance;
		public static MREInputManager Instance => _instance;
	}
}

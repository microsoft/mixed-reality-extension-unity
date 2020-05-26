// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using Assets.TestBed_Assets.Scripts.UserInput;

namespace Assets.Scripts.User
{
	public class MRTKInputSource : InputSourceBase
	{
		private void Start()
		{
			Initialize();
		}

		private async void Initialize()
		{
			await MREInputManager.CreateManager();
			MREInputManager.Instance.UserGameObject = UserGameObject;
		}
	}
}

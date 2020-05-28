// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Assets.TestBed_Assets.Scripts.UserInput
{
	class MREGlobalInputActionHandler : MREInputActionHandler
	{
		private void OnEnable()
		{
			EnableAsGlobalHandler();
		}

		private void Start()
		{
			StartAsGlobalHandler();
		}

		private void OnDisable()
		{
			OnDisableAsGlobalHandler();
		}
	}
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using Assets.Scripts.User;

namespace Assets.Scripts.Tools
{
	public abstract class Tool
	{
		public bool IsHeld { get; set; }

		public void Update(InputSource inputSource)
		{
			if (IsHeld)
			{
				UpdateTool(inputSource);
			}
		}

		protected abstract void UpdateTool(InputSource inputSource);
	}
}

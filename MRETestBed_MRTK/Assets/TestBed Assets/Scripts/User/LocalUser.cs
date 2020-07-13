// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Assets.TestBed_Assets.Scripts.User
{
	public static class LocalUser
	{
		static public string UserId { get; }

		static LocalUser()
		{
			var rng = new System.Random();
			UserId = rng.Next().ToString("X8");
		}
	}
}

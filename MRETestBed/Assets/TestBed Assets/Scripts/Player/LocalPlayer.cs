// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Assets.TestBed_Assets.Scripts.Player
{
	public static class LocalPlayer
	{
		static public string PlayerId { get; }

		static LocalPlayer()
		{
			var rng = new System.Random();
			PlayerId = rng.Next().ToString("X8");
		}
	}
}

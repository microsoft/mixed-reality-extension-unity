// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Threading.Tasks;
using UnityEngine;

namespace MixedRealityExtension.Assets
{
	/// <summary>
	/// Global asset load web request throttler.
	/// </summary>
	internal static class AssetLoadThrottling
	{
		// Global: Keep track of the number of active asset fetch web requests.
		const int MaxActiveLoads = 4;
		private static int ActiveLoads = 0;

		public static bool WouldThrottle()
		{
			return ActiveLoads >= MaxActiveLoads;
		}

		public static async Task<AssetLoadScope> AcquireLoadScope()
		{
			// Spin asynchronously until there is room for this load scope.
			while (WouldThrottle())
			{
				await Task.Delay(10);
			}

			return new AssetLoadScope();
		}

		public class AssetLoadScope : IDisposable
		{
            public AssetLoadScope()
			{
				++ActiveLoads;
			}

			private bool disposedValue = false;

			protected virtual void Dispose(bool disposing)
			{
				if (!disposedValue)
				{
					if (disposing)
					{
						--ActiveLoads;
					}
					disposedValue = true;
				}
			}

			public void Dispose()
			{
				Dispose(true);
			}
		}
	}
}

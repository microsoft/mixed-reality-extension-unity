// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Linq;

namespace MixedRealityExtension.Patching
{
	internal static class PatchingUtils
	{
		public static bool IsPatched<T>(this T patch) where T : IPatchable
		{
			var properties = patch.GetType().GetProperties();
			foreach (var property in properties)
			{
				if (property.GetCustomAttributes(false).Any(attr => attr is PatchProperty))
				{
					var val = property.GetValue(patch);
					if (val is IPatchable)
					{
						if (IsPatched(val as IPatchable))
						{
							return true;
						}
					}
					else if (val != null)
					{
						return true;
					}
				}
			}

			return false;
		}
	}

	[AttributeUsage(AttributeTargets.Property)]
	internal class PatchProperty : Attribute
	{
		public PatchProperty()
		{

		}
	}
}

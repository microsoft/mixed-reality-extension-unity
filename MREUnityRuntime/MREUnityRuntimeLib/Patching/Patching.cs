// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;

namespace MixedRealityExtension.Patching
{
	internal static class PatchingUtils
	{
		public static bool IsPatched<T>(this T patch) where T : IPatchable
		{
			foreach (System.Reflection.PropertyInfo property in patch.GetPatchableProperties())
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

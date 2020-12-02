// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;

namespace MixedRealityExtension.Core
{
	// Note: Unity serialization will break with unsigned types, or types larger than 32 bits
	[Flags]
	public enum Permissions : int
	{
		None = 0,
		Execution = 1,
		UserTracking = 2,
		UserInteraction = 4
	}

	public static class PermissionsExtensions
	{
		/// <summary>
		/// Convenience method to convert an enumerable of permissions into a bitfield
		/// </summary>
		/// <param name="enumerable"></param>
		/// <returns></returns>
		public static Permissions ToFlags(this IEnumerable<Permissions> enumerable)
		{
			var aggregate = Permissions.None;
			foreach (var perm in enumerable)
			{
				aggregate |= perm;
			}
			return aggregate;
		}

		/// <summary>
		/// Convenience method to convert a bitfield of Permissions into an enumerable
		/// </summary>
		/// <param name="flags"></param>
		/// <returns></returns>
		public static IEnumerable<Permissions> ToEnumerable(this Permissions flags)
		{
			var allPerms = Enum.GetValues(typeof(Permissions));
			var aggregate = new List<Permissions>(allPerms.Length);
			foreach (Permissions perm in allPerms)
			{
				if ((flags & perm) != 0)
				{
					aggregate.Add(perm);
				}
			}
			return aggregate;
		}
	}
}

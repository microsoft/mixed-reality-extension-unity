// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using MixedRealityExtension.IPC;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MixedRealityExtension.PluginInterfaces
{
	/// <summary>
	/// Interface for providing information about a user.
	/// </summary>
	public interface IHostAppUser
	{
		/// <summary>
		/// The unobfuscated id of the user for the host app. Handle with care.
		/// </summary>
		string HostUserId { get; }

		/// <summary>
		/// The user's display name. Null if user not found.
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Generic user properties. Usually informational only.
		/// </summary>
		Dictionary<string, string> Properties { get; }

		/// <summary>
		/// Gets the transform of the specified attach point.
		/// </summary>
		/// <param name="attachPointName">The name of the attach point to retrieve.</param>
		/// <returns>The attach point transform, or null if not found.</returns>
		Transform GetAttachPoint(string attachPointName);

		/// <summary>
		/// Called before the user's avatar is destroyed.
		/// </summary>
		event MWEventHandler BeforeAvatarDestroyed;

		/// <summary>
		/// Called after the user's avatar is recreated.
		/// </summary>
		event MWEventHandler AfterAvatarCreated;
	}
}

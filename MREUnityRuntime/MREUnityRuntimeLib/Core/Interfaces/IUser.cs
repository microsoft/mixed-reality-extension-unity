// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.PluginInterfaces;
using System;

namespace MixedRealityExtension.Core.Interfaces
{
	/// <summary>
	/// The interface that represents a user within the mixed reality extension runtime.
	/// </summary>
	public interface IUser : IMixedRealityExtensionObject, IEquatable<IUser>
	{
		/// <summary>
		/// Host-provided host app user instance.
		/// </summary>
		IHostAppUser HostAppUser { get; }

		/// <summary>
		/// The group mask for this user.
		/// </summary>
		UInt32 Groups { get; }
	}
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;

namespace MixedRealityExtension.Core.Interfaces
{
	/// <summary>
	/// The interface that represents a user within the mixed reality extension runtime.
	/// </summary>
	public interface IUser : IMixedRealityExtensionObject, IEquatable<IUser>
	{
		/// <summary>
		/// Host-provided IUserInfo instance.
		/// </summary>
		IUserInfo UserInfo { get; }

		/// <summary>
		/// The group mask for this user.
		/// </summary>
		UInt32 Groups { get; }
	}
}

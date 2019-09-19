// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
namespace MixedRealityExtension
{
	/// <summary>
	/// Special commands to change the mode of the sound instance?
	/// </summary>
	public enum MediaCommand
	{
		/// <summary>
		/// Start a new sound instance
		/// </summary>
		Start,

		/// <summary>
		/// Modify an active sound instance
		/// </summary>
		Update,

		/// <summary>
		/// Destroy an active sound instance
		/// </summary>
		Stop
	}
}

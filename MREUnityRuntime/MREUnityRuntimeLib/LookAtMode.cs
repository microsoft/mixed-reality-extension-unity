// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
namespace MixedRealityExtension
{
	/// <summary>
	/// What is the actor facing?
	/// </summary>
	public enum LookAtMode
	{
		/// <summary>
		/// The actor's orientation is determined entirely by its transform
		/// </summary>
		None,

		/// <summary>
		/// The actor rotates on its Y axis to point toward the target
		/// </summary>
		TargetY,

		/// <summary>
		/// The actor rotates on its X and Y axes to point toward the target
		/// </summary>
		TargetXY
	}
}

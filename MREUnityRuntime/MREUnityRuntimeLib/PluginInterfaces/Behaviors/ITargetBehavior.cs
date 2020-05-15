// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using MixedRealityExtension.Behaviors.Contexts;

namespace MixedRealityExtension.PluginInterfaces.Behaviors
{
	/// <summary>
	/// The interface that represents the target behavior in the target platform for MWI Apps.
	/// </summary>
	public interface ITargetBehavior : IBehavior
	{
		/// <summary>
		/// Whether the target is grabbable or not.
		/// </summary>
		bool Grabbable { get; set; }

		/// <summary>
		/// Whether the target behavior grab is active.
		/// </summary>
		bool IsGrabbed { get; set; }
	}
}

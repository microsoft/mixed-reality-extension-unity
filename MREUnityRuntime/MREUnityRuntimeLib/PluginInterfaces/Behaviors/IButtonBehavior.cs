// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Behaviors.Actions;

namespace MixedRealityExtension.PluginInterfaces.Behaviors
{
	/// <summary>
	/// The interface that represents the button behavior in the target platform for MWI Apps.
	/// </summary>
	public interface IButtonBehavior : ITargetBehavior
	{
		/// <summary>
		/// The hover action in the target platform..
		/// </summary>
		MWAction Hover { get; }

		/// <summary>
		/// The click action in the target platform.
		/// </summary>
		MWAction Click { get; }

		/// <summary>
		/// The button down/up action in the target platform.
		/// </summary>
		MWAction Button { get; }
	}
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Core.Interfaces;

namespace MixedRealityExtension.PluginInterfaces.Behaviors
{
	/// <summary>
	/// The base interface for a behavior.
	/// </summary>
	public interface IBehavior
	{
		/// <summary>
		/// The actor that the behavior is attached to.
		/// </summary>
		IActor Actor { get; set; }

		/// <summary>
		/// Called to cleanup the behavior, as it is being removed from the actor by the app.
		/// </summary>
		void CleanUp();
	}
}

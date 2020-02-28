// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Behaviors.ActionData;
using MixedRealityExtension.Behaviors.Actions;

namespace MixedRealityExtension.PluginInterfaces.Behaviors
{
	/// <summary>
	/// The interface that represents a tool behavior in the MRE runtime.
	/// </summary>
	/// <typeparam name="ToolDataT"></typeparam>
	public interface IToolBehavior<ToolDataT> : IBehavior
		where ToolDataT : BaseToolData
	{
		/// <summary>
		/// The holding action in the target platform.
		/// </summary>
		MWAction<ToolDataT> Holding { get; }

		/// <summary>
		/// The using action in the target platform.
		/// </summary>
		MWAction<ToolDataT> Using { get; }
	}
}
